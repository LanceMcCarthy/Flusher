using CommonHelpers.Common;

namespace Flusher.Common.Models
{
    public class PhotoCaptureInfo : BindableBase
    {
        private string localFilePath;
        private string blobStorageUrl;

        public PhotoCaptureInfo() { }

        public PhotoCaptureInfo(string path, string url)
        {
            localFilePath = path;
            blobStorageUrl = url;
        }

        public string LocalFilePath
        {
            get => localFilePath;
            set => SetProperty(ref localFilePath, value);
        }

        public string BlobStorageUrl
        {
            get => blobStorageUrl;
            set => SetProperty(ref blobStorageUrl, value);
        }
    }
}
