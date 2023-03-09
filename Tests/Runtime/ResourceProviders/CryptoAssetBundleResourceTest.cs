using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.ResourceManagement.Exceptions;
using UnityEngine.TestTools;
using UniRx;

namespace Extreal.Integration.Assets.Addressables.ResourceProviders.Test
{
    public class CryptoAssetBundleResourceTest
    {
        private AssetProvider assetProvider;

        private const string RemoteName = "RemoteCryptoCube";
        private const string LocalName = "LocalCryptoCube";
        private const string UncompressedName = "UncompressedCryptoCube";
        private const string Lz4Name = "Lz4CryptoCube";
        private const string NotUseAbcName = "NotUseAbcCryptoCube";
        private const string AssetBundleCrcDisabledName = "AssetBundleCrcDisabledCryptoCube";
        private const string AssetBundleCrcEnabledExcludingCachedName
            = "AssetBundleCrcEnabledExcludingCachedCryptoCube";
        private const string UseUwrForLocalName = "UseUwrForLocalCryptoCube";
        private const string HttpRedirectLimitName = "HttpRedirectLimitCryptoCube";
        private const string CacheClearWhenSpaceIsNeededInCacheName = "CacheClearWhenSpaceIsNeededInCacheCryptoCube";
        private const string FailedCryptoName = "FailedCryptoCube";
        private const string NoOptionsName = "NoOptionsCryptoCube";
        private const string AcquisitionFailureName = "AcquisitionFailureCryptoCube";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            const string bundleDirectoryPath = "Temp/com.unity.addressables/Decrypted/";
            if (Directory.Exists(bundleDirectoryPath))
            {
                Directory.Delete(bundleDirectoryPath);
            }
        }

        [SetUp]
        public void Initialize()
        {
            LoggingManager.Initialize(LogLevel.Debug);

            _ = Caching.ClearCache();

            assetProvider = new AssetProvider();
        }

        [TearDown]
        public void Dispose()
            => assetProvider.Dispose();

        [UnityTest]
        public IEnumerator LoadAssetFromRemote() => UniTask.ToCoroutine(async () =>
        {
            using var disposableCube = await assetProvider.LoadAssetAsync<GameObject>(RemoteName);

            Assert.That(disposableCube.Result, Is.Not.Null);
            LogAssert.Expect(LogType.Log, new Regex("Download from remote for the Asset Bundle"));
        });

        [UnityTest]
        public IEnumerator LoadAssetFromLocal() => UniTask.ToCoroutine(async () =>
        {
            using var disposableCube = await assetProvider.LoadAssetAsync<GameObject>(LocalName);

            Assert.That(disposableCube.Result, Is.Not.Null);
            LogAssert.Expect(LogType.Log, new Regex("Load from local for the Asset Bundle"));
        });

        [UnityTest]
        public IEnumerator LoadAssetWithUncompressed() => UniTask.ToCoroutine(async () =>
        {
            using var disposableCube = await assetProvider.LoadAssetAsync<GameObject>(UncompressedName);

            Assert.That(disposableCube.Result, Is.Not.Null);
        });

        [UnityTest]
        public IEnumerator LoadAssetWithLz4() => UniTask.ToCoroutine(async () =>
        {
            using var disposableCube = await assetProvider.LoadAssetAsync<GameObject>(Lz4Name);

            Assert.That(disposableCube.Result, Is.Not.Null);
        });

        [UnityTest]
        public IEnumerator LoadAssetWithLzma() => UniTask.ToCoroutine(async () =>
        {
            using var disposableCube = await assetProvider.LoadAssetAsync<GameObject>(RemoteName);

            Assert.That(disposableCube.Result, Is.Not.Null);
        });

        [UnityTest]
        public IEnumerator LoadAssetNotUsingAbc() => UniTask.ToCoroutine(async () =>
        {
            using var disposableCube = await assetProvider.LoadAssetAsync<GameObject>(NotUseAbcName);

            Assert.That(disposableCube.Result, Is.Not.Null);
            LogAssert.Expect(LogType.Log, new Regex("The Asset Bundle Cache option is disabled"));
        });

        [UnityTest]
        public IEnumerator LoadAssetUsingAbc() => UniTask.ToCoroutine(async () =>
        {
            using var disposableCube = await assetProvider.LoadAssetAsync<GameObject>(RemoteName);

            Assert.That(disposableCube.Result, Is.Not.Null);
            LogAssert.Expect(LogType.Log, new Regex("The Asset Bundle Cache option is enabled"));
        });

        [UnityTest]
        public IEnumerator LoadAssetWithAssetBundleCrcDisabled() => UniTask.ToCoroutine(async () =>
        {
            using var disposableCube = await assetProvider.LoadAssetAsync<GameObject>(AssetBundleCrcDisabledName);

            Assert.That(disposableCube.Result, Is.Not.Null);
            LogAssert.Expect(LogType.Log, new Regex("The Asset Bundle CRC option is disabled"));
        });

        [UnityTest]
        public IEnumerator LoadAssetWithAssetBundleCrcEnabledExcludingCached() => UniTask.ToCoroutine(async () =>
        {
            using var disposableCube = await assetProvider.LoadAssetAsync<GameObject>(AssetBundleCrcEnabledExcludingCachedName);

            Assert.That(disposableCube.Result, Is.Not.Null);
            LogAssert.Expect(LogType.Log, new Regex("The Asset Bundle CRC option is enabled"));
        });

        [UnityTest]
        public IEnumerator LoadAssetWithAssetBundleCrcEnabledIncludingCached() => UniTask.ToCoroutine(async () =>
        {
            using var disposableCube = await assetProvider.LoadAssetAsync<GameObject>(RemoteName);

            Assert.That(disposableCube.Result, Is.Not.Null);
            LogAssert.Expect(LogType.Log, new Regex("The Asset Bundle CRC option is enabled"));
        });

        [UnityTest]
        public IEnumerator LoadAssetFromLocalUsingUwr() => UniTask.ToCoroutine(async () =>
        {
            using var disposableCube = await assetProvider.LoadAssetAsync<GameObject>(UseUwrForLocalName);

            Assert.That(disposableCube.Result, Is.Not.Null);
            LogAssert.Expect(LogType.Log, new Regex("Download from local using UnityWebRequest for the Asset Bundle"));
        });

        [UnityTest]
        public IEnumerator LoadAssetWithHttpRedirectLimit() => UniTask.ToCoroutine(async () =>
        {
            using var disposableCube = await assetProvider.LoadAssetAsync<GameObject>(HttpRedirectLimitName);

            Assert.That(disposableCube.Result, Is.Not.Null);
            LogAssert.Expect(LogType.Log, new Regex("HTTP Redirect Limit is specified"));
        });

        [UnityTest]
        public IEnumerator LoadAssetWithoutHttpRedirectLimit() => UniTask.ToCoroutine(async () =>
        {
            using var disposableCube = await assetProvider.LoadAssetAsync<GameObject>(RemoteName);

            Assert.That(disposableCube.Result, Is.Not.Null);
            LogAssert.Expect(LogType.Log, new Regex("HTTP Redirect Limit is not specified"));
        });

        [UnityTest]
        public IEnumerator LoadAssetWithCacheClearWhenSpaceIsNeededInCache() => UniTask.ToCoroutine(async () =>
        {
            using var disposableCube = await assetProvider.LoadAssetAsync<GameObject>(CacheClearWhenSpaceIsNeededInCacheName);

            Assert.That(disposableCube.Result, Is.Not.Null);
            LogAssert.Expect(LogType.Log, new Regex("Cache Clear When Space Is Needed In Cache"));
        });

        [UnityTest]
        public IEnumerator LoadAssetWithCacheClearWhenNewVersionLoaded() => UniTask.ToCoroutine(async () =>
        {
            using var disposableCube = await assetProvider.LoadAssetAsync<GameObject>(RemoteName);

            Assert.That(disposableCube.Result, Is.Not.Null);
            LogAssert.Expect(LogType.Log, new Regex("Cache Clear When New Version Loaded"));
        });

        [UnityTest]
        public IEnumerator LoadAssetFromCache() => UniTask.ToCoroutine(async () =>
        {
            await assetProvider.DownloadAsync(RemoteName);
            using var disposableCube = await assetProvider.LoadAssetAsync<GameObject>(RemoteName);

            Assert.That(disposableCube.Result, Is.Not.Null);
        });

        [UnityTest]
        public IEnumerator GetDownloadStatus() => UniTask.ToCoroutine(async () =>
        {
            var downloadingAssetName = default(string);
            var downloadedStatuses = new Dictionary<string, List<NamedDownloadStatus>>();

            using var onDownloadingDisposable = assetProvider.OnDownloading
                .Subscribe(assetName => downloadingAssetName = assetName);

            using var onDownloadedDisposable = assetProvider.OnDownloaded
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
                });

            await assetProvider.DownloadAsync(RemoteName);

            Assert.That(downloadingAssetName, Is.EqualTo(RemoteName));
            Assert.That(downloadedStatuses.Keys, Does.Contain(RemoteName));
            var lastDownloadStatus = downloadedStatuses[RemoteName].Last();
            Assert.That(lastDownloadStatus.DownloadedBytes, Is.EqualTo(lastDownloadStatus.TotalBytes));
            Assert.That(lastDownloadStatus.IsDone, Is.True);
            Assert.That(lastDownloadStatus.Percent, Is.EqualTo(1f));
        });

        [UnityTest]
        public IEnumerator LoadAssetWithFailedCrypto() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.ignoreFailingMessages = true;
            try
            {
                using var disposableCube = await assetProvider.LoadAssetAsync<GameObject>(FailedCryptoName);
                Assert.Fail("This test must raise exceptions");
            }
            catch (OperationException e)
            {
                Assert.That(e, Has.InnerException);
                Assert.That(e.InnerException.GetType(), Is.EqualTo(typeof(Exception)));
                Assert.That(e.InnerException.Message, Is.EqualTo("Dependency Exception"));
            }
            catch (Exception e)
            {
                Assert.That(e.Message, Is.EqualTo("Dependency Exception"));
            }
        });

        [UnityTest]
        public IEnumerator FetchWithoutOptions() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.ignoreFailingMessages = true;
            try
            {
                using var disposableCube = await assetProvider.LoadAssetAsync<GameObject>(NoOptionsName);
                Debug.LogWarning("No exception is raised");
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
        });

        [UnityTest]
        public IEnumerator LoadAssetFromAcquisitionFailure() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.ignoreFailingMessages = true;
            try
            {
                _ = await assetProvider.LoadAssetAsync<GameObject>(AcquisitionFailureName);
                Assert.Fail("This test must raise exceptions");
            }
            catch (OperationException e)
            {
                Assert.That(e, Has.InnerException);
                Assert.That(e.InnerException.GetType(), Is.EqualTo(typeof(Exception)));
                Assert.That(e.InnerException.Message, Does.Contain("Dependency Exception"));
            }
            catch (Exception e)
            {
                Assert.That(e.Message, Does.Contain("Dependency Exception"));
            }
        });
    }
}
