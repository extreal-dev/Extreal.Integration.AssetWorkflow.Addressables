using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Extreal.Integration.Assets.Addressables.ResourceProviders.Test
{
    public class AesCbcStreamFactory : ICryptoStreamFactory
    {
        private const string Password = "password";
        private const int KeyLength = 16;

        public CryptoStream CreateEncryptStream(Stream baseStream, AssetBundleRequestOptions options)
        {
            using var aes = CreateAesManaged(options);

            var dummy = aes.IV;
            aes.GenerateIV();
            var encryptStream = new CryptoStream(baseStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
            encryptStream.Write(dummy, 0, KeyLength);

            return encryptStream;
        }

        public CryptoStream CreateDecryptStream(Stream baseStream, AssetBundleRequestOptions options)
        {
            using var aes = CreateAesManaged(options);

            var decryptStream = new CryptoStream(baseStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            _ = decryptStream.Read(new byte[KeyLength], 0, KeyLength);

            return decryptStream;
        }

        [SuppressMessage("CodeCracker", "CC0022")]
        private static AesManaged CreateAesManaged(AssetBundleRequestOptions options)
        {
            var salt = Encoding.UTF8.GetBytes(options.BundleName);
            using var key = new Rfc2898DeriveBytes(Password, salt);
            return new AesManaged
            {
                BlockSize = 128,
                KeySize = 128,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                Key = key.GetBytes(KeyLength)
            };
        }
    }
}
