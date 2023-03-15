using System.Diagnostics.CodeAnalysis;
using Extreal.Core.Common.System;
using Extreal.Core.Logging;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Extreal.Integration.AssetWorkflow.Addressables
{
#pragma warning disable IDE0065
    using Addressables = UnityEngine.AddressableAssets.Addressables;
#pragma warning restore IDE0065

    /// <summary>
    /// Class that enables to dispose the asset loaded by Addressables.
    /// </summary>
    /// <typeparam name="TResult">Type of the asset.</typeparam>
    public class AssetDisposable<TResult> : DisposableBase
    {
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(AssetDisposable<TResult>));

        /// <summary>
        /// Downloaded asset.
        /// </summary>
        /// <value>Downloaded asset.</value>
        public TResult Result { get; }

        [SuppressMessage("CodeCracker", "CC0057")]
        internal AssetDisposable(TResult result)
            => Result = result;

        /// <inheritdoc/>
        protected override void ReleaseManagedResources()
        {
            if (Result is SceneInstance result)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"Unload scene: {result.Scene.name}");
                }
                _ = Addressables.UnloadSceneAsync(result);
            }
            else
            {
                if (Logger.IsDebug())
                {
                    var name = Result is GameObject gameObject ? gameObject.name : Result.ToString();
                    Logger.LogDebug($"Release: {name}");
                }
                Addressables.Release(Result);
            }
        }
    }
}
