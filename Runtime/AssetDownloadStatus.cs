using UnityEngine.ResourceManagement.AsyncOperations;

namespace Extreal.Integration.AssetWorkflow.Addressables
{
    public readonly struct AssetDownloadStatus
    {
        public string AssetName { get; }
        public DownloadStatus Status { get; }

        public AssetDownloadStatus(string assetName, DownloadStatus downloadStatus)
        {
            AssetName = assetName;
            Status = downloadStatus;
        }
    }
}
