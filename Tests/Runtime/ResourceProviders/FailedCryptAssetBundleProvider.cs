namespace Extreal.Integration.Assets.Addressables.ResourceProviders.Test
{
    [System.ComponentModel.DisplayName("Failed Crypto AssetBundle Provider")]
    public class FailedCryptoAssetBundleProvider : CryptoAssetBundleProviderBase
    {
        public override ICryptoStreamFactory CryptoStreamFactory => new FailedCryptoStreamFactory();
    }
}
