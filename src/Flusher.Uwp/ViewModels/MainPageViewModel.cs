using CommonHelpers.Common;
using CommonHelpers.Models;
using CommonHelpers.Mvvm;
using Flusher.Common.Helpers;
using Flusher.Common.Models;
using Flusher.Common.Services;
using Flusher.Uwp.Common;
using Flusher.Uwp.Interfaces;
using Flusher.Uwp.Services;
using Flusher.Uwp.Vision;
using Microsoft.IoT.Lightning.Providers;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.Devices.Pwm;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;

namespace Flusher.Uwp.ViewModels
{
    public class MainPageViewModel : ViewModelBase, IDisposable
    {
        #region Fields

        // GPIO pins, see https://docs.microsoft.com/en-us/windows/iot-core/learn-about-hardware/pinmappings/pinmappingsrpi
        private const int ButtonGpioPinNumber = 19;
        private const int ServoGpioPinNumber = 26;
        private const int FlashLedGpioPinNumber = 12;
        private const int RedLedGpioPinNumber = 21;
        private const int GreenLedGpioPinNumber = 20;
        private const int BlueLedGpioPinNumber = 16;

        private PwmController pwmController;
        private PwmPin pwmPin;
        private GpioController gpioController;
        private GpioPin buttonPin;
        private GpioPin redLedPin;
        private GpioPin greenLedPin;
        private GpioPin blueLedPin;
        private GpioPin flashLedPin;

        private AzureCustomVisionService visionService;
        private AzureStorageService storageService;
        private CameraService camService;
        private FlusherService flusherService;

        private ObjectDetection onyxObjectDetection;
        private BitmapImage lastImage;
        private double minDutyCyclePercent = 0.04;
        private double maxDutyCyclePercent = 0.10;
        private double dutyCyclePercentage;
        private string outputText = "Starting up, please wait...";
        private bool isServerConnected;

        #endregion

        public MainPageViewModel()
        {
            AnalyzeCommand = new DelegateCommand(async () => await AnalyzeAsync());
            FlushRequestCommand = new DelegateCommand(async () => await FlushAsync(this.Requester));
        }

        #region Properties

        public ObservableCollection<ChartDataPoint> AngleAdjustmentHistory { get; set; } = new ObservableCollection<ChartDataPoint>();

        public BitmapImage LastImage
        {
            get => lastImage;
            set => SetProperty(ref lastImage, value);
        }

        public double MinDutyCyclePercent
        {
            get => minDutyCyclePercent;
            set => SetProperty(ref minDutyCyclePercent, value);
        }

        public double MaxDutyCyclePercent
        {
            get => maxDutyCyclePercent;
            set => SetProperty(ref maxDutyCyclePercent, value);
        }

        public string Requester { get; } = "IoT Client";

        public string OutputText
        {
            get => outputText;
            set => SetProperty(ref outputText, value);
        }

        public bool IsServerConnected
        {
            get => isServerConnected;
            set => SetProperty(ref isServerConnected, value);
        }

        
        public double DutyCyclePercentage
        {
            get => dutyCyclePercentage;
            set
            {
                if (SetProperty(ref dutyCyclePercentage, value))
                {
                    if (pwmPin != null && pwmPin.IsStarted)
                    {
                        pwmPin.SetActiveDutyCyclePercentage(value);
                        this.DutyCyclePercentChanged(value);
                    }
                }
            }
        }

        public DelegateCommand AnalyzeCommand { get; set; }

        public DelegateCommand FlushRequestCommand { get; set; }

        public IScrollable ScrollableView { get; set; }

        #endregion

        #region Initialization

        public async Task InitializeAsync()
        {
            IsBusy = true;

            Log("[INFO] Initializing MainPageViewModel...");

            await InitializeAzureStorageService();
            await InitializeCameraServiceAsync();
            await InitializePwmAsync();
            await InitializeSignalRServiceAsync();
            await InitializeGpioAsync();

            await flusherService.SendMessageAsync($"Initialized! Camera Ready? {camService?.IsReady}.");

            SetLedColor(LedColor.Green);

            IsBusy = false;
        }

        private async Task InitializeAzureStorageService()
        {
            Log($"[INFO] Initializing Azure storage service...");

            try
            {
                storageService = new AzureStorageService { ImageFileLifeSpan = TimeSpan.FromDays(30) };

                await storageService.InitializeAsync();

                Log($"[INFO] Azure storage service ready.");
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Azure storage initialization error: {ex.Message}");
                throw;
            }
        }

        private async Task InitializeCameraServiceAsync()
        {
            Log($"[INFO] Initializing camera...");

            try
            {
                camService = new CameraService();
                await camService.InitializeAsync();

                Log($"[INFO] Camera service ready.");
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Camera initialization error: {ex.Message}");
                throw;
            }
        }

        private async Task InitializePwmAsync()
        {
            Log($"[INFO] Initializing PWM...");

            try
            {
                // If LightningController is available, use it.
                if (LightningProvider.IsLightningEnabled)
                {
                    LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();
                    Log("[INFO] PWM - LightningProvider is Enabled");
                }

                var pwmProvider = LightningPwmProvider.GetPwmProvider();
                var pwmControllers = await PwmController.GetControllersAsync(pwmProvider);
                pwmController = pwmControllers[1];

                pwmPin = pwmController.OpenPin(ServoGpioPinNumber);
                pwmController.SetDesiredFrequency(50);

                pwmPin.Start();
                pwmPin.SetActiveDutyCyclePercentage(this.MinDutyCyclePercent);

                Log("[INFO] PWM - Pin started.");
            }
            catch (Exception ex)
            {
                Log($"[ERROR] PWM initialization error: {ex.Message}");
                throw;
            }
        }

        private async Task InitializeSignalRServiceAsync()
        {
            Log($"[INFO] Initializing SignalR...");

            try
            {
                flusherService = new FlusherService(Secrets.SignalRServerEndpoint);
                flusherService.ConnectionChanged += FlusherService_ConnectionChanged;

                flusherService.MessageReceived += FlusherService_MessageReceived;
                flusherService.FlushRequested += FlusherService_FlushRequested;
                flusherService.PhotoRequested += FlusherService_PhotoRequested;
                flusherService.PhotoReceived += FlusherService_PhotoReceived;
                flusherService.AnalyzeRequested += FlusherService_AnalyzeRequested;
                flusherService.AnalyzeResultReceived += FlusherService_AnalyzeResultReceived;

                Log($"[INFO] Starting SignalR...");

                await flusherService.StartAsync();

                IsServerConnected = true;

                Log($"[INFO] SignalR connected and listening for messages.");
            }
            catch (Exception ex)
            {
                Log($"[ERROR] SignalR initialization error: {ex.Message}");
                throw;
            }
        }

        private async Task InitializeGpioAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    Log($"[INFO] Initializing GPIO...");

                    gpioController = GpioController.GetDefault();

                    if (gpioController == null)
                    {
                        return;
                    }

                    flashLedPin = gpioController.OpenPin(FlashLedGpioPinNumber);
                    redLedPin = gpioController.OpenPin(RedLedGpioPinNumber);
                    greenLedPin = gpioController.OpenPin(GreenLedGpioPinNumber);
                    blueLedPin = gpioController.OpenPin(BlueLedGpioPinNumber);
                    buttonPin = gpioController.OpenPin(ButtonGpioPinNumber);

                    flashLedPin.SetDriveMode(GpioPinDriveMode.Output);
                    redLedPin.SetDriveMode(GpioPinDriveMode.Output);
                    greenLedPin.SetDriveMode(GpioPinDriveMode.Output);
                    blueLedPin.SetDriveMode(GpioPinDriveMode.Output);
                    buttonPin.SetDriveMode(buttonPin.IsDriveModeSupported(GpioPinDriveMode.InputPullUp)
                        ? GpioPinDriveMode.InputPullUp
                        : GpioPinDriveMode.Input);

                    flashLedPin.Write(GpioPinValue.Low);
                    redLedPin.Write(GpioPinValue.Low);
                    greenLedPin.Write(GpioPinValue.Low);
                    blueLedPin.Write(GpioPinValue.Low);
                    buttonPin.Write(GpioPinValue.High);

                    buttonPin.DebounceTimeout = TimeSpan.FromMilliseconds(1000);
                    buttonPin.ValueChanged += ButtonGpioPinValueChanged;

                    Log($"[INFO] GPIO ready!");
                }
                catch (Exception ex)
                {
                    Log($"[ERROR] GPIO initialization error: {ex.Message}");
                    throw;
                }
            });
        }
        
        #endregion

        #region SignalR Operations

        private void FlusherService_ConnectionChanged(object sender, string status)
        {
            try
            {
                IsBusy = true;

                Log($"[INFO] SignalR Connection Changed: {status}");

                IsServerConnected = status == "Connected";

                SetLedColor(IsServerConnected ? LedColor.Green : LedColor.Red);
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void FlusherService_MessageReceived(string message)
        {
            Log($"[INFO] {message}");
        }

        private async void FlusherService_FlushRequested(string requester)
        {
            if (IsBusy)
                return;
             
            try
            {
                IsBusy = true;

                await flusherService.SendMessageAsync($"Flush requested by {requester}.");

                await FlushAsync(requester);

                await flusherService.SendMessageAsync("Flush complete");
            }
            catch (Exception ex)
            {
                Log($"[Error] in FlusherService_FlushRequested: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                IsBusyMessage = "";
            }
        }

        private async void FlusherService_PhotoRequested(string requester)
        {
            Log($"[INFO] Photo Requested by {requester}.");

            SetLedColor(LedColor.Blue);

            var photoResult = await GeneratePhotoAsync(requester);

            SetLedColor(LedColor.Green);

            await flusherService.SendPhotoResultAsync("Photo request complete.", photoResult.BlobStorageUrl);
        }

        private void FlusherService_PhotoReceived(string message, string imageUrl)
        {
            Log($"[INFO] Photo received: {message}.");

            LastImage = new BitmapImage { UriSource = new Uri(imageUrl) };
        }

        private async void FlusherService_AnalyzeRequested(string requester)
        {
            try
            {
                Log($"[INFO] Analyze Requested by {requester}.");

                SetLedColor(LedColor.Blue);

                await flusherService.SendMessageAsync("Analyzing...");

                var result = await AnalyzeAsync();

                if (result.DidOperationComplete)
                {
                    // Inform subscribers of negative/positive result along with photo used for analyzing.
                    await flusherService.SendAnalyzeResultAsync(result.Message, result.PhotoResult.BlobStorageUrl);

                    // If there was a positive detection, invoke Flush and send email.
                    if (result.IsPositiveResult)
                    {
                        Log("[DETECTION] Poop detected!");
                        FlusherService_FlushRequested(Requester);

                        Log("[INFO] Alerting email subscribers");
                        await SendEmailAsync(result.PhotoResult.BlobStorageUrl);
                    }
                    else
                    {
                        Log("[DETECTION] No objects detected.");
                    }
                }
                else
                {
                    // Inform subscribers of error
                    await flusherService.SendMessageAsync("Analyze operation did not complete, please try again later. If this continues to happen, check server or IoT implementation..");
                }
            }
            catch (Exception ex)
            {
                Log($"[Error] in FlusherService_AnalyzeRequested: {ex.Message}");
                SetLedColor(LedColor.Red);
            }
            finally
            {
                SetLedColor(LedColor.Green);
            }
        }

        private void FlusherService_AnalyzeResultReceived(string message, string imageUrl)
        {
            Log($"[INFO] Analyze Result: {message}.");

            LastImage = new BitmapImage { UriSource = new Uri(imageUrl) };
        }

        #endregion

        #region Value Changed Event Handlers

        private void DutyCyclePercentChanged(double percentage)
        {
            try
            {
                // Update chart
                if (AngleAdjustmentHistory.Count > 60) AngleAdjustmentHistory.RemoveAt(0);
                
                AngleAdjustmentHistory.Add(new ChartDataPoint
                {
                    Date = DateTime.Now,
                    Value = percentage
                });
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
        }

        private async void ButtonGpioPinValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            if (IsBusy || args.Edge != GpioPinEdge.FallingEdge)
                return;

            try
            {
                IsBusy = true;

                await FlushAsync("Button");
            }
            catch (Exception ex)
            {
                Log($"[Error] in ButtonGpioPinValueChanged: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                IsBusyMessage = "";
            }
        }

        #endregion

        #region Custom Vision

        /// <summary>
        /// The workhorse of the application. This will take a photo, save it, upload it to Azure, and have CustomVision analyze it.
        /// Depending on a successful identification, the flush will be initiated.
        /// The result will be pushed back to SignalR to inform user of outcome and photo.
        /// </summary>
        /// <param name="useOnline">Setting this to false will use analyze the locally stored image file via ONYX file and WinML. This is useful for no/slow internet scenarios.</param>
        /// <returns></returns>
        private async Task<AnalyzeResult> AnalyzeAsync(bool useOnline = true)
        {
            try
            {
                // Turn on LED to full brightness to act as camera flash (Cyan appears to be the brightest)
                SetLedColor(LedColor.Blue);

                var analyzeResult = new AnalyzeResult();

                await flusherService.SendMessageAsync("Generating photo...");

                analyzeResult.PhotoResult = await GeneratePhotoAsync(Requester);

                bool poopDetected;

                if (useOnline)
                {
                    Log("[INFO] Analyzing photo using Vision API...");

                    await flusherService.SendMessageAsync("Analyzing photo using Vision API...");

                    // Option 1 - Online REST API for the model
                    poopDetected = await EvaluateImageAsync(analyzeResult.PhotoResult.BlobStorageUrl);
                }
                else
                {
                    Log("[INFO] Analyzing photo offline with Windows ML...");

                    await flusherService.SendMessageAsync("Analyzing image with Windows ML...");

                    // Option 2 - Local ONYX file support
                    poopDetected = await EvaluateImageOfflineAsync(analyzeResult.PhotoResult.LocalFilePath);
                }

                analyzeResult.DidOperationComplete = true;
                analyzeResult.IsPositiveResult = poopDetected;
                analyzeResult.Message = poopDetected ? "Poop detected!" : "No detection, flush skipped.";

                // Indicate normal status
                SetLedColor(LedColor.Green);

                return analyzeResult;
            }
            catch (Exception ex)
            {
                SetLedColor(LedColor.Red);

                return new AnalyzeResult
                {
                    IsPositiveResult = false,
                    DidOperationComplete = false,
                    Message = $"Error! Analyze operation did not complete: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Uses UWP MediaCapture to take a photo using the first camera attached.
        /// </summary>
        /// <param name="requester">The name of the client requesting the photo. This is used to create the image file name.</param>
        /// <returns>A online URL to Azure blob file and a path to the locally saved file.</returns>
        private async Task<PhotoCaptureInfo> GeneratePhotoAsync(string requester)
        {
            try
            {
                // Turn on the bright LED for flash
                flashLedPin.Write(GpioPinValue.High);

                Log($"[INFO] Photo requested by {requester}.");

                // TODO determine if DateTime ISO 8601 format is the best option to use for file name
                var informativeFileName = $"{DateTime.UtcNow:yyyyMMddTHHmmss} {requester}.jpg";
                var file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(informativeFileName, CreationCollisionOption.ReplaceExisting);

                Log("[INFO] Capturing photo, saving to temporary storage...");
                await camService.MediaCapture.CapturePhotoToStorageFileAsync(ImageEncodingProperties.CreateJpeg(), file);

                Log($"[INFO] Capture successful!  Uploading file to Azure blob ({file.Name})...");
                string imgUrl = null;

                using (Stream stream = await file.OpenStreamForReadAsync())
                {
                    //var bytes = new byte[(int)stream.Length];
                    //await stream.ReadAsync(bytes, 0, (int)stream.Length);


                    var storageUri = await storageService.UploadPhotoAsync(informativeFileName, stream);

                    imgUrl = storageUri.OriginalString;

                    // If something went wrong with PrimaryUri.OriginalString, manually create the URL with a known root url, container name and file name
                    if (string.IsNullOrEmpty(imgUrl)) imgUrl = $"{Secrets.AzureStorageRootUrl}/{Secrets.BlobContainerName}/{file.Name}";
                }

                flashLedPin.Write(GpioPinValue.Low);

                return new PhotoCaptureInfo(file.Path, imgUrl);
            }
            catch (Exception ex)
            {
                flashLedPin.Write(GpioPinValue.Low);
                Log($"[Error] in GeneratePhotoAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Determines if there is a positive detection in the image.
        /// </summary>
        /// <param name="imgUrl">Url to the online image to evaluate (i.e. path to Azure blob)</param>
        /// <param name="successThreshold">Cut-off for successful detection. Default is 0.85 (85%)</param>
        /// <returns>True if there was a high confidence of poop presence.</returns>
        private async Task<bool> EvaluateImageAsync(string imgUrl, double successThreshold = 0.85)
        {
            try
            {
                // Initialize on-demand
                if (visionService == null)
                {
                    visionService = new AzureCustomVisionService(Secrets.CustomVisionApiKey);
                }

                var results = await visionService.EvaluateImageUrlAsync(imgUrl);

                if (results == null)
                {
                    Log($"[INFO] Vision API - results were null");
                    return false;
                }

                if (!results.Predictions.Any())
                {
                    Log($"[INFO] Vision API - No predictions.");
                    return false;
                }

                var bowls = results.Predictions.Where(r => r.TagName == "Bowl").ToList();

                if (bowls.Count == 0)
                {
                    Log($"[INFO] Vision API - No bowls detected.");
                    return false;
                }

                var hits = results.Predictions.Where(r => r.TagName == "Poop").ToList();

                if (hits.Count == 0)
                {
                    Log($"[INFO] Vision API - No poops detected.");
                    return false;
                }

                var highestHits = hits.Where(r => r.Probability > successThreshold).ToList();

                if (highestHits.Count == 0)
                {
                    Log($"[INFO] Vision API - No high confidence poops");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log($"[Error] in EvaluateImageAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Determines if there is a positive detection in the image using offline support via ONYX model file.
        /// </summary>
        /// <param name="filePath">File path for the saved image.</param>
        /// <param name="successThreshold">Cut-off for successful detection. Default is 0.85 (85%)</param>
        /// <returns>True if there was a high confidence of poop presence.</returns>
        private async Task<bool> EvaluateImageOfflineAsync(string filePath, double successThreshold = 0.85)
        {
            // Initialize on-demand
            if (onyxObjectDetection == null)
            {
                onyxObjectDetection = new ObjectDetection(new List<string> { "Poop", "Pee", "Bowl" });

                var modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/Flusher.onyx"));

                await onyxObjectDetection.Init(modelFile);
            }

            VideoFrame videoFrame = null;

            var file = await StorageFile.GetFileFromPathAsync(filePath);

            if (!file.IsAvailable)
            {
                Debug.Write($"[ERROR] StorageFile for image is not available!");
                return false;
            }

            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
            {
                var decoder = await BitmapDecoder.CreateAsync(stream);
                var softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                videoFrame = VideoFrame.CreateWithSoftwareBitmap(softwareBitmap);
            }

            if (videoFrame == null)
            {
                Debug.Write($"[WARNING] VideoFrame Could not be created!");
                return false;
            }

            IList<PredictionModel> results = await onyxObjectDetection.PredictImageAsync(videoFrame);

            if (results == null)
            {
                Log($"[INFO] Vision ONYX - Results were null");
                return false;
            }

            if (!results.Any())
            {
                Log($"[INFO] Vision ONYX - No prediction results.");
                return false;
            }

            var bowls = results.Where(r => r.TagName == "Bowl").ToList();

            if (bowls.Count == 0)
            {
                Log($"[INFO] Vision ONYX - No bowls detected.");
                return false;
            }

            var hits = results.Where(r => r.TagName == "Poop").ToList();

            if (hits.Count == 0)
            {
                Log($"[INFO] Vision ONYX - No poops detected.");
                return false;
            }

            var highestHits = hits.Where(r => r.Probability > successThreshold).ToList();

            if (highestHits.Count == 0)
            {
                Log($"[INFO] Vision ONYX - No high confidence poops.");
                return false;
            }

            return true;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Common utility that invokes the servo movement to physically flush the toilet.
        /// </summary>
        /// <param name="requester">The name of the device or service requesting the flush.</param>
        /// <param name="duration">Length of time (in milliseconds) to hold the servo in the open position.</param>
        /// <returns></returns>
        private async Task FlushAsync(string requester, int duration = 8000)
        {
            try
            {
                Log($"Flush requested by {requester}, in progress...");

                SetLedColor(LedColor.Blue);

                DutyCyclePercentage = this.MaxDutyCyclePercent;

                Log($"Flush started, waiting {duration / 1000} seconds...");

                await Task.Delay(duration);

                DutyCyclePercentage = this.MinDutyCyclePercent;

                Log("Flush complete.");

                SetLedColor(LedColor.Green);
            }
            catch (Exception ex)
            {
                Log($"[Error] in FlushAsync: {ex.Message}");
                throw ex;
            }
        }

        /// <summary>
        /// Sends email to users upon successful detection and flush.
        /// </summary>
        /// <param name="imageUrl">Url to the image</param>
        /// <returns>Task</returns>
        private async Task SendEmailAsync(string imageUrl)
        {
            try
            {
                Log($"[INFO] Sending email notification...");

                var client = new SendGridClient(Secrets.SendGridApiKey);

                var from = new EmailAddress("noreply@dvlup.com", "Flusher");
                var tos = new List<EmailAddress>
                {
                    new EmailAddress(Secrets.EmailAddressOne, Secrets.EmailAddressOneName),
                    new EmailAddress(Secrets.EmailAddressTwo, Secrets.EmailAddressTwoName)
                };

                var htmlContent = $@"<h3>Presence Detected</h3><div><p>See the latest image capture below. If you want to pull a newer image or manually invoke a command, go to <strong><a href=""https://flusher.azurewebsites.net"">Flusher Command And Control</strong> or open the app.</a></div><div><img src=""{imageUrl}""/></div>";

                var msg = MailHelper.CreateSingleEmailToMultipleRecipients(
                    from,
                    tos,
                    "Detection Alert",
                    "",
                    htmlContent,
                    false);

                var response = await client.SendEmailAsync(msg);

                Log($"[INFO] Email sent! Response Status: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Log($"[Error] in SendEmailAsync: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the value of the RGB status LED.
        /// </summary>
        /// <param name="color">Color to set the LED to. Black is off.</param>
        private void SetLedColor(LedColor color)
        {
            if (gpioController == null)
            {
                Debug.WriteLine($"GPIO is not ready.");
                return;
            }

            if (redLedPin == null || greenLedPin == null || blueLedPin == null)
            {
                Debug.WriteLine($"LED pins are not ready.");
                return;
            }

            try
            {
                switch (color)
                {
                    case LedColor.White:
                        redLedPin.Write(GpioPinValue.High);
                        greenLedPin.Write(GpioPinValue.High);
                        blueLedPin.Write(GpioPinValue.High);
                        break;
                    case LedColor.Green:
                        redLedPin.Write(GpioPinValue.Low);
                        greenLedPin.Write(GpioPinValue.High);
                        blueLedPin.Write(GpioPinValue.Low);
                        break;
                    case LedColor.Red:
                        redLedPin.Write(GpioPinValue.High);
                        greenLedPin.Write(GpioPinValue.Low);
                        blueLedPin.Write(GpioPinValue.Low);
                        break;
                    case LedColor.Blue:
                        redLedPin.Write(GpioPinValue.Low);
                        greenLedPin.Write(GpioPinValue.Low);
                        blueLedPin.Write(GpioPinValue.High);
                        break;
                    case LedColor.Black:
                        redLedPin.Write(GpioPinValue.Low);
                        greenLedPin.Write(GpioPinValue.Low);
                        blueLedPin.Write(GpioPinValue.Low);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(color), color, null);
                }
            }
            catch (Exception ex)
            {
                Log($"[Error] in SetLedColor: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs messages to debug console and the user.
        /// </summary>
        /// <param name="message">Text to output.</param>
        /// <param name="showToUser">Show the messages to the user when the BusyIndicator is visible.</param>
        private async void Log(string message, bool showToUser = true)
        {
            try
            {
                Debug.WriteLine(message);

                var dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

                if (dispatcher.HasThreadAccess)
                {
                    OutputText = message;
                    ScrollableView?.ScrollToEnd();

                    if (IsBusy && showToUser)
                    {
                        IsBusyMessage = message;
                    }

                    if (message.Contains("[Error]"))
                    {
                        SetLedColor(LedColor.Red);
                    }
                }
                else
                {
                    await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        OutputText = message;
                        ScrollableView?.ScrollToEnd();

                        if (IsBusy && showToUser)
                        {
                            IsBusyMessage = message;
                        }

                        if (message.Contains("[Error]"))
                        {
                            SetLedColor(LedColor.Red);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void Dispose()
        {
            Log("[INFO] Disposing Camera Service...");
            camService?.Dispose();

            Log("[INFO] Disposing Flusher SignalR Service...");
            flusherService?.Dispose();

            Log("[INFO] Disposing Servo...");
            pwmPin.Dispose();
            pwmPin = null;
            pwmController = null;

            Log("[INFO] Disposing ONYX object detection...");
            onyxObjectDetection?.Dispose();

            Log("[INFO] Disposing Vision Service...");
            visionService?.Dispose();

            Log("[INFO] Disposing GPIO Pins...");
            SetLedColor(LedColor.Black);
            flashLedPin.Dispose();
            redLedPin?.Dispose();
            greenLedPin?.Dispose();
            blueLedPin?.Dispose();
            buttonPin?.Dispose();

            Log("[INFO] Disposing GPIO controller...");
            gpioController = null;

            Log($"[INFO] MainPageViewModel Dispose complete.");
        }

        #endregion
    }
}
