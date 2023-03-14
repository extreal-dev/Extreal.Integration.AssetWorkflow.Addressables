using System;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Extreal.Integration.AssetWorkflow.Addressables.Custom.ResourceProviders
{
    /// <summary>
    /// Class that provides encrypted asset bundles.
    /// </summary>
    public abstract class CryptoAssetBundleProviderBase : ResourceProviderBase
    {
        /// <summary>
        /// Factory that generates CryptoStream.
        /// </summary>
        public abstract ICryptoStreamFactory CryptoStreamFactory { get; }

        /// <inheritdoc/>
        public override void Provide(ProvideHandle providerInterface)
        {
            var res = new CryptoAssetBundleResource(providerInterface, CryptoStreamFactory);
            res.Fetch();
        }

        /// <inheritdoc/>
        public override Type GetDefaultType(IResourceLocation location) => typeof(IAssetBundleResource);

        /// <inheritdoc/>
        public override void Release(IResourceLocation location, object asset)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (asset == null)
            {
                Debug.LogWarning($"Releasing null asset bundle from location {location}.  This is an indication that the bundle failed to load.");
                return;
            }

            if (asset is CryptoAssetBundleResource bundle)
            {
                bundle.Unload();
            }
        }
    }
}
