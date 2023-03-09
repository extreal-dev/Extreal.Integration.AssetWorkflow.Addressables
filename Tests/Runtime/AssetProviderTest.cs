using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UniRx;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.ResourceManagement.Exceptions;
using UnityEngine.AddressableAssets;
using System.Linq;
using Extreal.Core.Logging;

namespace Extreal.Integration.Assets.Addressables.Test
{
    public class AssetProviderTest
    {
        private AssetProvider assetProvider;

        private string downloadingAssetName;
        private readonly Dictionary<string, List<NamedDownloadStatus>> downloadedStatuses
            = new Dictionary<string, List<NamedDownloadStatus>>();

        [SuppressMessage("Design", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private const string CubeName = "Cube";
        private const string AdditiveSceneName = "AdditiveScene";
        private const string NotExistedName = "NotExisted";

        [SetUp]
        public void Initialize()
        {
            LoggingManager.Initialize(LogLevel.Debug);

            _ = Caching.ClearCache();

            assetProvider = new AssetProvider().AddTo(disposables);

            downloadingAssetName = default;
            downloadedStatuses.Clear();

            _ = assetProvider.OnDownloading
                .Subscribe(assetName => downloadingAssetName = assetName)
                .AddTo(disposables);

            _ = assetProvider.OnDownloaded
                .Subscribe(downloadStatus =>
                {
                    if (downloadedStatuses.TryGetValue(downloadStatus.AssetName, out var namedDownloadStatuses))
                    {
                        namedDownloadStatuses.Add(downloadStatus);
                    }
                    else
                    {
                        downloadedStatuses[downloadStatus.AssetName]
                            = new List<NamedDownloadStatus> { downloadStatus };
                    }
                })
                .AddTo(disposables);
        }

        [TearDown]
        public void Dispose()
            => disposables.Clear();

        [OneTimeTearDown]
        public void OneTimeTeatDown()
            => disposables.Dispose();

        [UnityTest]
        public IEnumerator GetDownloadSize() => UniTask.ToCoroutine(async () =>
        {
            var cubeSize = await assetProvider.GetDownloadSizeAsync(CubeName);

            Assert.That(cubeSize, Is.Not.Zero);
        });

        [UnityTest]
        public IEnumerator Download() => UniTask.ToCoroutine(async () =>
        {
            await assetProvider.DownloadAsync(CubeName);

            Assert.That(downloadingAssetName, Is.EqualTo(CubeName));
            Assert.That(downloadedStatuses.Keys, Does.Contain(CubeName));
            var lastDownloadStatus = downloadedStatuses[CubeName].Last();
            Assert.That(lastDownloadStatus.DownloadedBytes, Is.EqualTo(lastDownloadStatus.TotalBytes));
            Assert.That(lastDownloadStatus.IsDone, Is.True);
            Assert.That(lastDownloadStatus.Percent, Is.EqualTo(1f));
        });

        [UnityTest]
        public IEnumerator DownloadWithInterval() => UniTask.ToCoroutine(async () =>
        {
            await assetProvider.DownloadAsync(CubeName, TimeSpan.FromSeconds(0.5f));

            Assert.That(downloadingAssetName, Is.EqualTo(CubeName));
            Assert.That(downloadedStatuses.Keys, Does.Contain(CubeName));
            Assert.That(downloadedStatuses[CubeName].Last().Percent, Is.EqualTo(1f));
        });

        [UnityTest]
        public IEnumerator DownloadWithNextFunc() => UniTask.ToCoroutine(async () =>
        {
            var isCalled = false;
#pragma warning disable CS1998
            Func<UniTask> nextFunc = async () => isCalled = true;
#pragma warning restore CS1998

            await assetProvider.DownloadAsync(CubeName, nextFunc: nextFunc);

            Assert.That(downloadingAssetName, Is.EqualTo(CubeName));
            Assert.That(downloadedStatuses.Keys, Does.Contain(CubeName));
            Assert.That(downloadedStatuses[CubeName].Last().Percent, Is.EqualTo(1f));
            Assert.That(isCalled, Is.True);
        });

        [UnityTest]
        public IEnumerator LoadAssetWithAssetNameSuccess() => UniTask.ToCoroutine(async () =>
        {
            using var disposableCube = await assetProvider.LoadAssetAsync<GameObject>(CubeName);

            Assert.That(disposableCube.Result, Is.Not.Null);
        });

        [UnityTest]
        public IEnumerator LoadAssetWithoutAssetNameSuccess() => UniTask.ToCoroutine(async () =>
        {
            using var disposableTestObject = await assetProvider.LoadAssetAsync<TestObject>();

            Assert.That(disposableTestObject.Result, Is.Not.Null);
            Assert.That(disposableTestObject.Result.Tag, Is.EqualTo("Default"));
        });

        [UnityTest]
        public IEnumerator LoadAssetFailure() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.ignoreFailingMessages = true;
            try
            {
                _ = await assetProvider.LoadAssetAsync<GameObject>(NotExistedName);
                Assert.Fail("This test must raise exceptions");
            }
            catch (InvalidKeyException e)
            {
                Assert.That(e.Message, Does.Contain($"Key={NotExistedName}"));
            }
            catch (OperationException e)
            {
                Assert.That(e, Has.InnerException);
                Assert.That(e.InnerException.GetType(), Is.EqualTo(typeof(InvalidKeyException)));
                Assert.That(e.InnerException.Message, Does.Contain($"Key={NotExistedName}"));
            }
        });

        [UnityTest]
        public IEnumerator LoadSceneSuccess() => UniTask.ToCoroutine(async () =>
        {
            using var disposableScene = await assetProvider.LoadSceneAsync(AdditiveSceneName);

            Assert.That(disposableScene.Result, Is.Not.Null);
        });

        [UnityTest]
        public IEnumerator LoadSceneFailure() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.ignoreFailingMessages = true;
            try
            {
                _ = await assetProvider.LoadSceneAsync(NotExistedName);
                Assert.Fail("This test must raise exceptions");
            }
            catch (InvalidKeyException e)
            {
                Assert.That(e.Message, Does.Contain($"Key={NotExistedName}"));
            }
            catch (OperationException e)
            {
                Assert.That(e, Has.InnerException);
                Assert.That(e.InnerException.GetType(), Is.EqualTo(typeof(InvalidKeyException)));
                Assert.That(e.InnerException.Message, Does.Contain($"Key={NotExistedName}"));
            }
        });
    }
}
