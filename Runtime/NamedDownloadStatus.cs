using UnityEngine.ResourceManagement.AsyncOperations;

namespace Extreal.Integration.AssetWorkflow.Addressables
{
    public readonly struct NamedDownloadStatus
    {
        public string AssetName { get; }
        public long TotalBytes { get; }
        public long DownloadedBytes { get; }
        public bool IsDone { get; }
        public float Percent { get; }

        public NamedDownloadStatus(string assetName, DownloadStatus downloadStatus)
        {
            AssetName = assetName;
            TotalBytes = downloadStatus.TotalBytes;
            DownloadedBytes = downloadStatus.DownloadedBytes;
            IsDone = downloadStatus.IsDone;
            Percent = downloadStatus.Percent;
        }
    }
}
