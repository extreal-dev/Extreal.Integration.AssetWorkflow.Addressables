using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.Retry;
using Extreal.Core.Common.System;
using UniRx;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Extreal.Integration.AssetWorkflow.Addressables
{
#pragma warning disable IDE0065
    using Addressables = UnityEngine.AddressableAssets.Addressables;
#pragma warning restore IDE0065

    [SuppressMessage("Design", "CC0091")]
    public class AssetProvider : DisposableBase
    {
        public IObservable<string> OnDownloading => onDownloading.AddTo(disposables);
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<string> onDownloading = new Subject<string>();

        public IObservable<NamedDownloadStatus> OnDownloaded => onDownloaded.AddTo(disposables);
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<NamedDownloadStatus> onDownloaded = new Subject<NamedDownloadStatus>();

        /// <summary>
        /// <para>Invokes just before retrying to connect to the server.</para>
        /// Arg: Retry count
        /// </summary>
        public IObservable<int> OnConnectRetrying => onConnectRetrying.AddTo(disposables);
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<int> onConnectRetrying = new Subject<int>();

        /// <summary>
        /// <para>Invokes immediately after finishing retrying to connect to the server.</para>
        /// Arg: Final result of retry. True for success, false for failure.
        /// </summary>
        public IObservable<bool> OnConnectRetried => onConnectRetried.AddTo(disposables);
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<bool> onConnectRetried = new Subject<bool>();

        [SuppressMessage("Usage", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private readonly IRetryStrategy retryStrategy;

        [SuppressMessage("Usage", "CC0057")]
        public AssetProvider(IRetryStrategy retryStrategy = null)
            => this.retryStrategy = retryStrategy ?? NoRetryStrategy.Instance;

        protected override void ReleaseManagedResources()
            => disposables.Dispose();

        public async UniTask DownloadAsync
        (
            string assetName,
            TimeSpan downloadStatusInterval = default,
            Func<UniTask> nextFunc = default
        )
        {
            if (await GetDownloadSizeAsync(assetName) != 0L)
            {
                Func<UniTask> func = ()
                    => DownloadDependenciesAsync(assetName, downloadStatusInterval);
                using var handler = RetryHandler<UniTask>.Of(func, _ => true, retryStrategy);
                await HandleWithSubscribeAsync(handler);
            }
            nextFunc?.Invoke().Forget();
        }

        private async UniTask<T> HandleWithSubscribeAsync<T>(RetryHandler<T> handler)
        {
            using var retrying = handler.OnRetrying.Subscribe(onConnectRetrying.OnNext);
            using var retried = handler.OnRetried.Subscribe(onConnectRetried.OnNext);
            var result = await handler.HandleAsync();
            return result;
        }

        public async UniTask<long> GetDownloadSizeAsync(string assetName)
        {
            var handle = Addressables.GetDownloadSizeAsync(assetName);
            var size = await handle.Task.ConfigureAwait(true);
            ReleaseHandle(handle);
            return size;
        }

        private async UniTask DownloadDependenciesAsync(string assetName, TimeSpan interval = default)
        {
            onDownloading.OnNext(assetName);

            var handle = Addressables.DownloadDependenciesAsync(assetName);

            var downloadStatus = handle.GetDownloadStatus();
            onDownloaded.OnNext(new NamedDownloadStatus(assetName, downloadStatus));
            while (handle.Status == AsyncOperationStatus.None)
            {
                var prevDownloadStatus = downloadStatus;
                downloadStatus = handle.GetDownloadStatus();
                if (prevDownloadStatus.DownloadedBytes != downloadStatus.DownloadedBytes)
                {
                    onDownloaded.OnNext(new NamedDownloadStatus(assetName, downloadStatus));
                }
                if (interval == default)
                {
                    await UniTask.Yield();
                }
                else
                {
                    await UniTask.Delay(interval);
                }
            }
            onDownloaded.OnNext(new NamedDownloadStatus(assetName, handle.GetDownloadStatus()));

            ReleaseHandle(handle);
        }

        public async UniTask<AssetDisposable<T>> LoadAssetAsync<T>(string assetName)
        {
            Func<UniTask<AssetDisposable<T>>> func = async () =>
            {
                var handle = Addressables.LoadAssetAsync<T>(assetName);
                var asset = await handle.Task.ConfigureAwait(true);
                if (handle.Status == AsyncOperationStatus.Failed)
                {
                    ReleaseHandle(handle);
                }
                return new AssetDisposable<T>(asset);
            };
            using var handler = RetryHandler<AssetDisposable<T>>.Of(func, _ => true, retryStrategy);
            return await HandleWithSubscribeAsync(handler);
        }

        public UniTask<AssetDisposable<T>> LoadAssetAsync<T>()
            => LoadAssetAsync<T>(typeof(T).Name);

        public async UniTask<AssetDisposable<SceneInstance>> LoadSceneAsync
        (
            string assetName,
            LoadSceneMode loadMode = LoadSceneMode.Additive
        )
        {
            Func<UniTask<AssetDisposable<SceneInstance>>> func = async () =>
            {
                var handle = Addressables.LoadSceneAsync(assetName, loadMode);
                var scene = await handle.Task.ConfigureAwait(true);
                if (handle.Status == AsyncOperationStatus.Failed)
                {
                    ReleaseHandle(handle);
                }
                return new AssetDisposable<SceneInstance>(scene);
            };
            using var handler = RetryHandler<AssetDisposable<SceneInstance>>.Of(func, _ => true, retryStrategy);
            return await HandleWithSubscribeAsync(handler);
        }

        private static void ReleaseHandle(AsyncOperationHandle handle)
        {
            var exception = handle.OperationException;
            Addressables.Release(handle);
            if (exception != null)
            {
                ExceptionDispatchInfo.Throw(exception);
            }
        }
    }
}
