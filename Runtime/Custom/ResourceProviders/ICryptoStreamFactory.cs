using System.IO;
using System.Security.Cryptography;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Extreal.Integration.AssetWorkflow.Addressables.Custom.ResourceProviders
{
    /// <summary>
    /// Interface for the factory class to creates CryptoStream.
    /// </summary>
    public interface ICryptoStreamFactory
    {
        /// <summary>
        /// Creates a CryptoStream to be used for encryption.
        /// </summary>
        /// <param name="baseStream">Base stream.</param>
        /// <param name="options">Contains cache information to be used by the AssetBundleProvider.</param>
        /// <returns>CryptoStream.</returns>
        CryptoStream CreateEncryptStream(Stream baseStream, AssetBundleRequestOptions options);

        /// <summary>
        /// Creates a CryptoStream to be used for decryption.
        /// </summary>
        /// <param name="baseStream">Base stream.</param>
        /// <param name="options">Contains cache information to be used by the AssetBundleProvider.</param>
        /// <returns>CryptoStream.</returns>
        CryptoStream CreateDecryptStream(Stream baseStream, AssetBundleRequestOptions options);
    }
}
