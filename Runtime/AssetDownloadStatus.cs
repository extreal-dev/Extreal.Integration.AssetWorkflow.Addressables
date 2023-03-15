using UnityEngine.ResourceManagement.AsyncOperations;

namespace Extreal.Integration.AssetWorkflow.Addressables
{
    /// <summary>
    /// Struct that holds the download status with asset name.
    /// </summary>
    public readonly struct AssetDownloadStatus
    {
        /// <summary>
        /// Asset name to be downloaded.
        /// </summary>
        /// <value>Asset name to be downloaded.</value>
        public string AssetName { get; }

        /// <summary>
        /// Download status.
        /// </summary>
        /// <value>Download status.</value>
        public DownloadStatus Status { get; }

        /// <summary>
        /// Creates AssetDownloadStatus.
        /// </summary>
        /// <param name="assetName">Asset name to be downloaded.</param>
        /// <param name="downloadStatus">Download status.</param>
        public AssetDownloadStatus(string assetName, DownloadStatus downloadStatus)
        {
            AssetName = assetName;
            Status = downloadStatus;
        }
    }
}
