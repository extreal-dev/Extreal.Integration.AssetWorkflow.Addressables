using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Extreal.Integration.Assets.Addressables.ResourceProviders.Test
{
    public class CryptoAssetBundleProviderBaseTest
    {
        [Test]
        public void ReleaseWithLocationNull()
        {
            var provider = new AesCbcAssetBundleProvider();
            Assert.That(() => provider.Release(null, "some asset"),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contain("location"));
        }

        [Test]
        public void ReleaseWithAssetNull()
        {
            var provider = new AesCbcAssetBundleProvider();
            provider.Release(new TestLocation(), null);

            LogAssert.Expect(LogType.Warning, $"Releasing null asset bundle from location {nameof(TestLocation)}.  This is an indication that the bundle failed to load.");
        }
    }
}
