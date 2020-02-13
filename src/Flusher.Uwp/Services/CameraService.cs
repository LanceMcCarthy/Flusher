using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using CommonHelpers.Common;

namespace Flusher.Uwp.Services
{
    public class CameraService : BindableBase, IDisposable
    {
        private bool isReady;

        public CameraService()
        {

        }

        public MediaCapture MediaCapture { get; private set; }

        public bool IsReady
        {
            get => isReady;
            set => SetProperty(ref isReady, value);
        }

        public async Task InitializeAsync()
        {
            if (MediaCapture == null)
            {
                var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                var firstCamera = allVideoDevices.Count > 0 ? allVideoDevices[0] : null;

                if (firstCamera != null)
                {
                    MediaCapture = new MediaCapture();
                    await MediaCapture.InitializeAsync(new MediaCaptureInitializationSettings { VideoDeviceId = firstCamera.Id });
                    IsReady = true;
                }
                else
                {
                    IsReady = false;
                }
            }
        }

        public void Dispose()
        {
            MediaCapture?.Dispose();
            MediaCapture = null;
        }
    }
}
