using System;
using System.Security.Cryptography;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.ResourceProviders;
using Extreal.Core.Logging;

namespace Extreal.Integration.AssetWorkflow.Addressables.Custom.ResourceProviders
{
    /// <summary>
    /// Class for downloading and decrypting encrypted files.
    /// </summary>
    public class DownloadHandlerFileWithDecryption : DownloadHandlerScript
    {
        private readonly string path;
        private readonly ICryptoStreamFactory cryptoStreamFactory;
        private readonly AssetBundleRequestOptions options;
        private readonly MemoryStream memoryStream;
        private readonly CryptoStream decryptor;

        private FileStream fileStream;
        private long readPosition;

        private const int BufferSize = 4096;

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(DownloadHandlerFileWithDecryption));

        /// <summary>
        /// Creates a DownloadHandlerFileWithDecryption.
        /// </summary>
        /// <param name="path">Path to file to be written.</param>
        /// <param name="cryptoStreamFactory">Factory that creates CryptoStream.</param>
        /// <param name="options">Contains cache information to be used by the AssetBundleProvider.</param>
        public DownloadHandlerFileWithDecryption
        (
            string path,
            ICryptoStreamFactory cryptoStreamFactory,
            AssetBundleRequestOptions options
        )
        {
            var bundleDirectoryPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(bundleDirectoryPath) && bundleDirectoryPath != null)
            {
                _ = Directory.CreateDirectory(bundleDirectoryPath);
            }

            this.path = path;
            this.cryptoStreamFactory = cryptoStreamFactory;
            this.options = options;

            fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
            memoryStream = new MemoryStream();
            decryptor = cryptoStreamFactory.CreateDecryptStream(memoryStream, options);
        }

        /// <inheritdoc/>
        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            _ = memoryStream.Seek(0, SeekOrigin.End);
            memoryStream.Write(data, 0, dataLength);

            var buffer = new byte[BufferSize];
            _ = memoryStream.Seek(readPosition, SeekOrigin.Begin);
            while (memoryStream.Length - memoryStream.Position > BufferSize)
            {
                _ = decryptor.Read(buffer, 0, BufferSize);
                fileStream.Write(buffer, 0, BufferSize);
            }
            readPosition = memoryStream.Position;

            return true;
        }

        /// <inheritdoc/>
        protected override void CompleteContent()
        {
            if (readPosition != memoryStream.Length)
            {
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

            fileStream.Flush();
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            var shouldDeleteFile = fileStream.Length == 0;

            try
            {
                decryptor.Dispose();
            }
            catch (Exception e)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("Failed to dispose CryptoStream", e);
                }
            }
            memoryStream.Dispose();
            fileStream.Dispose();

            if (shouldDeleteFile)
            {
                File.Delete(path);
            }

            base.Dispose();
        }
    }
}
