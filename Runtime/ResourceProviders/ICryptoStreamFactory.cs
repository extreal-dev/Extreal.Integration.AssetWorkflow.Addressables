using System.IO;
using System.Security.Cryptography;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Extreal.Integration.Assets.Addressables.ResourceProviders
{
    public interface ICryptoStreamFactory
    {
        CryptoStream CreateEncryptStream(Stream baseStream, AssetBundleRequestOptions options);
        CryptoStream CreateDecryptStream(Stream baseStream, AssetBundleRequestOptions options);
    }
}
