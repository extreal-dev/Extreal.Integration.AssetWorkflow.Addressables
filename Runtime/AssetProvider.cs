using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using UniRx;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Extreal.Integration.Assets.Addressables
{
#pragma warning disable IDE0065
    using Addressables = UnityEngine.AddressableAssets.Addressables;
#pragma warning restore IDE0065

    /// <summary>
    /// Extreal.Integration.Assets.Addressablesモジュールに入るクラス。
    /// </summary>
    [SuppressMessage("Design", "CC0091")]
    public class AssetProvider : DisposableBase
    {
        public IObservable<string> OnDownloading => onDownloading.AddTo(disposables);
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<string> onDownloading = new Subject<string>();

        public IObservable<NamedDownloadStatus> OnDownloaded => onDownloaded.AddTo(disposables);
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<NamedDownloadStatus> onDownloaded = new Subject<NamedDownloadStatus>();

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        protected override void ReleaseManagedResources()
            => disposables.Dispose();

        public async UniTask DownloadAsync
        (
            string assetName,
            TimeSpan downloadStatusInterval = default,
            Func<UniTask> nextFunc = null
        )
        {
            if (await GetDownloadSizeAsync(assetName) != 0)
            {
                await DownloadDependenciesAsync(assetName, downloadStatusInterval);
            }
            nextFunc?.Invoke().Forget();
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

            onDownloaded.OnNext(new NamedDownloadStatus(assetName, handle.GetDownloadStatus()));
            var downloadStatus = default(DownloadStatus);
            while (handle.Status == AsyncOperationStatus.None) // None: the operation is still in progress.
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
            var handle = Addressables.LoadAssetAsync<T>(assetName);
            var asset = await handle.Task.ConfigureAwait(true);
            if (handle.Status == AsyncOperationStatus.Failed)
            {
                ReleaseHandle(handle);
            }
            return new AssetDisposable<T>(asset);
        }

        public UniTask<AssetDisposable<T>> LoadAssetAsync<T>()
            => LoadAssetAsync<T>(typeof(T).Name);

        public async UniTask<AssetDisposable<SceneInstance>> LoadSceneAsync
        (
            string assetName,
            LoadSceneMode loadMode = LoadSceneMode.Additive
        )
        {
            var handle = Addressables.LoadSceneAsync(assetName, loadMode);
            var scene = await handle.Task.ConfigureAwait(true);
            if (handle.Status == AsyncOperationStatus.Failed)
            {
                ReleaseHandle(handle);
            }
            return new AssetDisposable<SceneInstance>(scene);
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
