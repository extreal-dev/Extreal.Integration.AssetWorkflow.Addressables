using System;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Extreal.Integration.Assets.Addressables.ResourceProviders
{
    public abstract class CryptoAssetBundleProviderBase : ResourceProviderBase
    {
        public abstract ICryptoStreamFactory CryptoStreamFactory { get; }

        public override void Provide(ProvideHandle providerInterface)
        {
            var res = new CryptoAssetBundleResource(providerInterface, CryptoStreamFactory);
            res.Fetch();
        }

        public override Type GetDefaultType(IResourceLocation location) => typeof(IAssetBundleResource);

        public override void Release(IResourceLocation location, object asset)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (asset == null)
            {
                Debug.LogWarningFormat("Releasing null asset bundle from location {0}.  This is an indication that the bundle failed to load.", location);
                return;
            }

            if (asset is CryptoAssetBundleResource bundle)
            {
                bundle.Unload();
            }
        }
    }
}
