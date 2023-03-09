namespace Extreal.Integration.Assets.Addressables.ResourceProviders.Test
{
    [System.ComponentModel.DisplayName("AES-CBC AssetBundle Provider")]
    public class AesCbcAssetBundleProvider : CryptoAssetBundleProviderBase
    {
        public override ICryptoStreamFactory CryptoStreamFactory => new AesCbcStreamFactory();
    }
}
