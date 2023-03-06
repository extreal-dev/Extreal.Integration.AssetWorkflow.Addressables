using System.IO;
using System.Security.Cryptography;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Extreal.Integration.Assets.Addressables
{
    public interface ICryptoStreamGetter
    {
        CryptoStream GetEncryptStream(Stream baseStream, AssetBundleRequestOptions options);
        CryptoStream GetDecryptStream(Stream baseStream, AssetBundleRequestOptions options);
    }
}
