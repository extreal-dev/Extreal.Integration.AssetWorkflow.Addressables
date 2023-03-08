using System.Security.Cryptography;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Extreal.Integration.Assets.Addressables.ResourceProviders
{
    public class DownloadHandlerFileWithDecryption : DownloadHandlerScript
    {
        private readonly FileStream fileStream;
        private readonly MemoryStream memoryStream = new MemoryStream();
        private readonly ICryptoStreamFactory cryptoStreamFactory;
        private readonly AssetBundleRequestOptions options;

        private CryptoStream decryptor;
        private bool isInit = true;
        private long readPosition;

        private const int BufferSize = 4096;

        public DownloadHandlerFileWithDecryption
        (
            string path,
            ICryptoStreamFactory cryptoStreamFactory,
            AssetBundleRequestOptions options
        )
        {
            var bundleDirectoryPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(bundleDirectoryPath))
            {
                _ = Directory.CreateDirectory(bundleDirectoryPath);
            }

            fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
            this.cryptoStreamFactory = cryptoStreamFactory;
            this.options = options;
        }

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            UnityEngine.Debug.LogWarning($"Data received: {dataLength}");
            memoryStream.Seek(0, SeekOrigin.End);
            memoryStream.Write(data, 0, dataLength);
            if (isInit)
            {
                if (memoryStream.Length >= 16)
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    decryptor = cryptoStreamFactory.CreateDecryptStream(memoryStream, options);
                    readPosition = memoryStream.Position;
                    isInit = false;
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"Total data length is less than 16: {memoryStream.Length}");
                }
            }

            var buffer = new byte[BufferSize];
            memoryStream.Seek(readPosition, SeekOrigin.Begin);
            while (memoryStream.Length - memoryStream.Position >= BufferSize)
            {
                decryptor.Read(buffer, 0, BufferSize);
                fileStream.Write(buffer, 0, BufferSize);
            }
            readPosition = memoryStream.Position;

            return true;
        }

        protected override void CompleteContent()
        {
            UnityEngine.Debug.LogWarning("Finish read");
            if (readPosition != memoryStream.Length)
            {
                memoryStream.Seek(readPosition, SeekOrigin.Begin);
                decryptor.CopyTo(fileStream);
            }
            fileStream.Dispose();
            memoryStream.Dispose();
            decryptor.Dispose();
        }
    }
}
