using System.IO;
using System.Linq;
using Extreal.Core.Logging;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.Exceptions;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using AsyncOperation = UnityEngine.AsyncOperation;


namespace Extreal.Integration.AssetWorkflow.Addressables.Custom.ResourceProviders
{
    /// <summary>
    /// Class that handles encrypted asset bundle.
    /// </summary>
    public class CryptoAssetBundleResource : IAssetBundleResource
    {
        private enum LoadType
        {
            None,
            Local,
            Web
        }

        private AssetBundle assetBundle;
        private UnityWebRequestAsyncOperation uwrAsyncOperation;
        private ProvideHandle provideHandle;
        private readonly AssetBundleRequestOptions options;
        private readonly ICryptoStreamFactory cryptoStreamFactory;
        private string transformedInternalId;
        private string bundleFilePath;

        private long bytesToDownload = -1;
        private long BytesToDownload
        {
            get
            {
                if (bytesToDownload == -1)
                {
                    bytesToDownload
                        = options?.ComputeSize(provideHandle.Location, provideHandle.ResourceManager) ?? 0L;
                }
                return bytesToDownload;
            }
        }

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(CryptoAssetBundleResource));

        /// <summary>
        /// Creates a CryptoAssetBundleResource.
        /// </summary>
        /// <param name="provideHandle">Container for all data need by providers to fulfill requests.</param>
        /// <param name="cryptoStreamFactory">Factory that creates CryptoStream.</param>
        public CryptoAssetBundleResource(ProvideHandle provideHandle, ICryptoStreamFactory cryptoStreamFactory)
        {
            this.provideHandle = provideHandle;
            this.cryptoStreamFactory = cryptoStreamFactory;
            this.provideHandle.SetProgressCallback(GetProgress);
            this.provideHandle.SetDownloadProgressCallbacks(GetDownloadStatus);
            options = this.provideHandle.Location?.Data as AssetBundleRequestOptions;
        }

        private DownloadStatus GetDownloadStatus()
        {
            if (options == null)
            {
                return default;
            }

            var status = new DownloadStatus
            {
                TotalBytes = BytesToDownload,
                IsDone = GetProgress() >= 1f
            };

            var downloadedBytes = 0L;
            if (BytesToDownload > 0L && uwrAsyncOperation != null
                && string.IsNullOrEmpty(uwrAsyncOperation.webRequest.error))
            {
                downloadedBytes = (long)uwrAsyncOperation.webRequest.downloadedBytes;
            }

            status.DownloadedBytes = downloadedBytes;
            return status;
        }

        /// <summary>
        /// Fetches and decrypts encrypted asset bundles.
        /// </summary>
        public void Fetch()
        {
            GetLoadInfo(provideHandle, out var loadType, out transformedInternalId);
            if (loadType == LoadType.Local)
            {
                AssetBundle.LoadFromFileAsync(transformedInternalId, options?.Crc ?? 0)
                    .completed += RequestOperationToGetAssetBundleCompleted;
            }
            else if (loadType == LoadType.Web)
            {
                CreateAndSendWebRequest(transformedInternalId);
            }
            else
            {
                var exception = new RemoteProviderException
                (
                    $"Invalid path in AssetBundleProvider: '{transformedInternalId}'.",
                    provideHandle.Location
                );
                provideHandle.Complete<CryptoAssetBundleResource>(null, false, exception);
            }
        }

        private void GetLoadInfo(ProvideHandle handle, out LoadType loadType, out string path)
        {
            if (options == null)
            {
                loadType = LoadType.None;
                path = null;
                return;
            }

            path = handle.ResourceManager.TransformInternalId(handle.Location);
            if (ResourceManagerConfig.ShouldPathUseWebRequest(path))
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"Download from remote for the Asset Bundle in {transformedInternalId}");
                }

                loadType = LoadType.Web;
            }
            else if (options.UseUnityWebRequestForLocalBundles)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"Download from local using UnityWebRequest for the Asset Bundle in {transformedInternalId}");
                }

                path = "file:///" + Path.GetFullPath(path);
                loadType = LoadType.Web;
            }
            else
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"Load from local for the Asset Bundle in {transformedInternalId}");
                }

                loadType = LoadType.Local;
            }

            bundleFilePath = Application.temporaryCachePath + $"/{nameof(Extreal)}/Decrypted/" + Path.GetFileName(path);
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"path: {path}");
                Logger.LogDebug($"bundleFilePath: {bundleFilePath}");
            }
        }

        private void RequestOperationToGetAssetBundleCompleted(AsyncOperation op)
        {
            if (op is AssetBundleCreateRequest assetBundleCreateRequest)
            {
                CompleteBundleLoad(assetBundleCreateRequest.assetBundle);
            }
            else if (op is UnityWebRequestAsyncOperation uwrAsyncOp
                        && uwrAsyncOp.webRequest.downloadHandler is DownloadHandlerAssetBundle dhAssetBundle)
            {
                CompleteBundleLoad(dhAssetBundle.assetBundle);
                uwrAsyncOp.webRequest.Dispose();
                uwrAsyncOperation?.webRequest.Dispose();
                uwrAsyncOperation = null;

                if (File.Exists(bundleFilePath))
                {
                    File.Delete(bundleFilePath);

                    var dir = Path.GetDirectoryName(bundleFilePath);
                    if (!Directory.EnumerateFileSystemEntries(dir).Any())
                    {
                        Directory.Delete(dir);
                    }
                }
            }
        }

        private void CompleteBundleLoad(AssetBundle bundle)
        {
            assetBundle = bundle;
            if (assetBundle != null)
            {
                provideHandle.Complete(this, true, null);

#if ENABLE_CACHING
                if (!string.IsNullOrEmpty(options.Hash) && options.ClearOtherCachedVersionsWhenLoaded)
                {
                    if (Logger.IsDebug())
                    {
                        Logger.LogDebug($"Cache Clear When New Version Loaded for the Asset Bundle in {transformedInternalId}");
                    }
                    Caching.ClearOtherCachedVersions(options.BundleName, Hash128.Parse(options.Hash));
                }
                else if (Logger.IsDebug())
                {
                    Logger.LogDebug($"Cache Clear When Space Is Needed In Cache for the Asset Bundle in {transformedInternalId}");
                }
#endif
            }
            else
            {
                var exception = new RemoteProviderException
                (
                    $"Invalid path in AssetBundleProvider: '{transformedInternalId}'.",
                    provideHandle.Location
                );
                provideHandle.Complete<CryptoAssetBundleResource>(null, false, exception);
            }
        }

        private void CreateAndSendWebRequest(string path)
        {
            if (Caching.IsVersionCached(new CachedAssetBundle(options.BundleName, Hash128.Parse(options.Hash))))
            {
                GetAssetBundleFromCacheOrFile();
                return;
            }

            var uwr = new UnityWebRequest(path)
            {
                disposeDownloadHandlerOnDispose = false,
                downloadHandler = new DownloadHandlerFileWithDecryption(bundleFilePath, cryptoStreamFactory, options)
            };

            if (options.RedirectLimit > 0)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"HTTP Redirect Limit is specified: {options.RedirectLimit}");
                }

                uwr.redirectLimit = options.RedirectLimit;
            }
            else if (Logger.IsDebug())
            {
                Logger.LogDebug($"HTTP Redirect Limit is not specified");
            }
            provideHandle.ResourceManager.WebRequestOverride?.Invoke(uwr);

            uwrAsyncOperation = uwr.SendWebRequest();
            uwrAsyncOperation.completed += _ =>
            {
                uwr.downloadHandler.Dispose();
                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    var exception = new RemoteProviderException
                    (
                        $"Download has failed. result:{uwr.result} path:{path}",
                        provideHandle.Location
                    );
                    provideHandle.Complete<CryptoAssetBundleResource>(null, false, exception);
                    uwr.Dispose();
                    return;
                }
                GetAssetBundleFromCacheOrFile();
            };
        }

        private void GetAssetBundleFromCacheOrFile()
        {
            if (Logger.IsDebug())
            {
                var abilityMessage = default(string);
                abilityMessage = options.Crc == 0u
                    ? "Disabled"
                    : "Enabled, " + (options.UseCrcForCachedBundle ? "Including" : "Excluding") + " Cached";
                Logger.LogDebug($"The Asset Bundle CRC option is {abilityMessage} for the Asset Bundle in {transformedInternalId}");
            }

            var localBundleFilePath = "file:///" + bundleFilePath;
            UnityWebRequest uwr;
            if (!string.IsNullOrEmpty(options.Hash))
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"The Asset Bundle Cache option is enabled for the Asset Bundle in {transformedInternalId}");
                }

                var cachedBundle = new CachedAssetBundle(options.BundleName, Hash128.Parse(options.Hash));
#if ENABLE_CACHING
                uwr = options.UseCrcForCachedBundle || !Caching.IsVersionCached(cachedBundle)
                    ? UnityWebRequestAssetBundle.GetAssetBundle(localBundleFilePath, cachedBundle, options.Crc)
                    : UnityWebRequestAssetBundle.GetAssetBundle(localBundleFilePath, cachedBundle);
#else
                uwr = UnityWebRequestAssetBundle.GetAssetBundle(localBundleFilePath, cachedBundle, options.Crc);
#endif
            }
            else
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"The Asset Bundle Cache option is disabled for the Asset Bundle in {transformedInternalId}");
                }

                uwr = UnityWebRequestAssetBundle.GetAssetBundle(localBundleFilePath, options.Crc);
            }

            uwr.SendWebRequest().completed += RequestOperationToGetAssetBundleCompleted;
        }

        /// <summary>
        /// Unloads the asset bundle this instance holds.
        /// </summary>
        public void Unload()
        {
            if (assetBundle != null)
            {
                assetBundle.Unload(true);
                assetBundle = null;
            }
        }

        /// <inheritdoc/>
        public AssetBundle GetAssetBundle()
            => assetBundle;

        private float GetProgress()
            => uwrAsyncOperation?.webRequest.downloadProgress ?? 0f;
    }
}
