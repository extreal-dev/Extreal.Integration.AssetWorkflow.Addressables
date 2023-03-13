using System;
using System.Security.Cryptography;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.ResourceProviders;
using Extreal.Core.Logging;

namespace Extreal.Integration.AssetWorkflow.Addressables.Custom.ResourceProviders
{
    public class DownloadHandlerFileWithDecryption : DownloadHandlerScript
    {
        private readonly string path;
        private readonly ICryptoStreamFactory cryptoStreamFactory;
        private readonly AssetBundleRequestOptions options;

        private FileStream fileStream;
        private MemoryStream memoryStream;
        private CryptoStream decryptor;
        private bool isInit = true;
        private long readPosition;

        private const int BufferSize = 4096;

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(DownloadHandlerFileWithDecryption));

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

            this.path = path;
            this.cryptoStreamFactory = cryptoStreamFactory;
            this.options = options;

            memoryStream = new MemoryStream();
        }

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            _ = memoryStream.Seek(0, SeekOrigin.End);
            memoryStream.Write(data, 0, dataLength);
            if (isInit)
            {
                _ = memoryStream.Seek(0, SeekOrigin.Begin);
                fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
                decryptor = cryptoStreamFactory.CreateDecryptStream(memoryStream, options);
                readPosition = memoryStream.Position;
                isInit = false;
            }

            var buffer = new byte[BufferSize];
            _ = memoryStream.Seek(readPosition, SeekOrigin.Begin);
            while (memoryStream.Length - memoryStream.Position >= BufferSize)
            {
                _ = decryptor.Read(buffer, 0, BufferSize);
                fileStream.Write(buffer, 0, BufferSize);
            }
            readPosition = memoryStream.Position;

            return true;
        }

        protected override void CompleteContent()
        {
            if (readPosition != memoryStream.Length)
            {
                _ = memoryStream.Seek(readPosition, SeekOrigin.Begin);
                try
                {
                    decryptor.CopyTo(fileStream);
                }
                catch (Exception e)
                {
                    if (Logger.IsDebug())
                    {
                        Logger.LogDebug("Failed to decrypt", e);
                    }
                }
            }

            fileStream.Dispose();
            decryptor.Dispose();
        }

        public override void Dispose()
        {
            memoryStream.Dispose();
            base.Dispose();
        }
    }
}
