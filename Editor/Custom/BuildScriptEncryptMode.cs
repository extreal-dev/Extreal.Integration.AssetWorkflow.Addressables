using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Extreal.Integration.AssetWorkflow.Addressables.Custom.ResourceProviders;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.Build.Pipeline;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace Extreal.Integration.AssetWorkflow.Addressables.Editor.Custom
{
    /// <summary>
    /// Build scripts used for player builds and running with bundles in the editor.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildScriptEncryptMode.asset", menuName = "Extreal/Integration.AssetWorkflow.Addressables.Editor/Encrypt Build Script")]
    public class BuildScriptEncryptMode : BuildScriptPackedMode
    {
        private readonly List<BundleResult> bundleResults = new List<BundleResult>();
        private readonly HashSet<string> doneAssetGroups = new HashSet<string>();

        /// <inheritdoc />
        public override string Name => "Encrypt Build Script";

        /// <inheritdoc />
        protected override TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput builderInput)
        {
            bundleResults.Clear();
            doneAssetGroups.Clear();
            return base.BuildDataImplementation<TResult>(builderInput);
        }

        /// <inheritdoc />
        protected override TResult DoBuild<TResult>(
            AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext)
        {
            var result = base.DoBuild<TResult>(builderInput, aaContext);

            try
            {
                Encrypt();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }

            return result;
        }

        /// <inheritdoc />
        protected override string ConstructAssetBundleName(
            AddressableAssetGroup assetGroup, BundledAssetGroupSchema schema,
            BundleDetails info, string assetBundleName)
        {
            var outputBundleName = base.ConstructAssetBundleName(assetGroup, schema, info, assetBundleName);

            if (assetGroup != null && schema != null)
            {
                bundleResults.Add(new BundleResult(assetGroup, schema, info, assetBundleName, outputBundleName));
            }

            return outputBundleName;
        }

        private void Encrypt()
        {
            foreach (var bundleResult in bundleResults)
            {
                var isEncrypt = IsEncrypt(bundleResult) && IsUseWebRequest(bundleResult);
                LogEncrypt(bundleResult, isEncrypt);

                if (isEncrypt)
                {
                    Encrypt(bundleResult);
                }
            }
        }

        private void LogEncrypt(BundleResult bundleResult, bool encrypted)
        {
            var assetGroupName = bundleResult.AssetGroup.Name;
            if (doneAssetGroups.Contains(assetGroupName))
            {
                return;
            }
            doneAssetGroups.Add(assetGroupName);

            var message = encrypted ? "Encrypted" : "Not encrypted";
            Debug.Log($"<color=cyan>{message} '{assetGroupName}' {bundleResult.OutputBundleName}</color>");
        }

        private bool IsEncrypt(BundleResult bundleResult)
            => typeof(CryptoAssetBundleProviderBase)
                .IsAssignableFrom(bundleResult.Schema.AssetBundleProviderType.Value);

        private bool IsUseWebRequest(BundleResult bundleResult)
        {
            var loadPath = bundleResult.Schema.LoadPath.GetValue(bundleResult.AssetGroup.Settings);
            return ResourceManagerConfig.ShouldPathUseWebRequest(loadPath)
                   || bundleResult.Schema.UseUnityWebRequestForLocalBundles;
        }

        [SuppressMessage("CodeCracker", "CC0022")]
        private static void Encrypt(BundleResult bundleResult)
        {
            var builtBundlePath = ToBuiltBundlePath(bundleResult);
            var srcPath = builtBundlePath + ".src";
            CreateSrcFile(builtBundlePath, srcPath);

            var destPath = builtBundlePath;

            var dirName = Path.GetDirectoryName(destPath);
            if (!Directory.Exists(dirName))
            {
                _ = Directory.CreateDirectory(dirName);
            }

            if (File.Exists(destPath))
            {
                File.Delete(destPath);
            }

            {
                using var srcStream = new FileStream(srcPath, FileMode.Open, FileAccess.Read);
                using var destStream = new FileStream(destPath, FileMode.OpenOrCreate, FileAccess.Write);
                using var encryptor = ToFactory(bundleResult).CreateEncryptStream(destStream, ToOptions(bundleResult));
                srcStream.CopyTo(encryptor);
            }

            File.Delete(srcPath);
        }

        private static void CreateSrcFile(string builtBundlePath, string srcPath)
        {
            if (File.Exists(srcPath))
            {
                File.Delete(srcPath);
            }
            File.Move(builtBundlePath, srcPath);
        }

        private static string ToBuiltBundlePath(BundleResult bundleResult)
        {
            var buildPath = bundleResult.Schema.BuildPath.GetValue(bundleResult.AssetGroup.Settings);
            return Path.Combine(buildPath, bundleResult.OutputBundleName);
        }

        private static ICryptoStreamFactory ToFactory(BundleResult bundleResult) =>
            (bundleResult.Schema.AssetBundleProviderType.Value
                .GetConstructor(Array.Empty<Type>())
                .Invoke(default) as CryptoAssetBundleProviderBase).CryptoStreamFactory;

        private class BundleResult
        {
            public AddressableAssetGroup AssetGroup { get; }
            public BundledAssetGroupSchema Schema { get; }
            public BundleDetails Info { get; }
            public string AssetBundleName { get; }
            public string OutputBundleName { get; }

            public BundleResult(
                AddressableAssetGroup assetGroup, BundledAssetGroupSchema schema,
                BundleDetails info, string assetBundleName, string outputBundleName)
            {
                AssetGroup = assetGroup;
                Schema = schema;
                Info = info;
                AssetBundleName = assetBundleName;
                OutputBundleName = outputBundleName;
            }
        }

        private static AssetBundleRequestOptions ToOptions(BundleResult bundleResult)
        {
            var schema = bundleResult.Schema;
            var info = bundleResult.Info;
            return new AssetBundleRequestOptions
            {
                Crc = schema.UseAssetBundleCrc ? info.Crc : 0,
                UseCrcForCachedBundle = schema.UseAssetBundleCrcForCachedBundles,
                UseUnityWebRequestForLocalBundles = schema.UseUnityWebRequestForLocalBundles,
                Hash = schema.UseAssetBundleCache ? info.Hash.ToString() : "",
                ChunkedTransfer = schema.ChunkedTransfer,
                RedirectLimit = schema.RedirectLimit,
                RetryCount = schema.RetryCount,
                Timeout = schema.Timeout,
                BundleName = Path.GetFileNameWithoutExtension(info.FileName),
                AssetLoadMode = schema.AssetLoadMode,
                BundleSize = GetFileSize(ToBuiltBundlePath(bundleResult)),
                ClearOtherCachedVersionsWhenLoaded = schema.AssetBundledCacheClearBehavior ==
                                                     BundledAssetGroupSchema.CacheClearBehavior
                                                         .ClearWhenWhenNewVersionLoaded
            };
        }

        private static long GetFileSize(string fileName)
        {
            try
            {
                return new FileInfo(fileName).Length;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return 0;
            }
        }
    }
}
