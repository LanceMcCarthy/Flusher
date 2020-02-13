using CommonHelpers.Common;
using Flusher.Common.Helpers;
using Flusher.Common.Models;
using Flusher.Common.Services;
using Flusher.Forms.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Telerik.XamarinForms.Primitives;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Flusher.Forms.ViewModels
{
    public class OperationsViewModel : ViewModelBase, IDisposable
    {
        private FlusherService flusherService;

        private ObservableCollection<OperationMessage> operations;
        private OperationMessage selectedOperation;
        private string requester;
        private string currentStatus;
        private bool isServerConnected;
        private bool isDetailPopupOpen;
        private AnimationType busyAnimationType;

        public OperationsViewModel()
        {
            Title = "Operations";
            BusyAnimationType = AnimationType.Animation8;

            RequestFlushCommand = new Command(async () => await RequestFlushAsync());
            RequestPhotoCommand = new Command(async()=> await RequestPhotoAsync());
            RequestAnalyzeCommand = new Command(async () => await RequestAnalyzeAsync());

            ShareOperationCommand = new Command(async () => await ShareAsync());
            TogglePopupCommand = new Command(() => IsDetailPopupOpen = !IsDetailPopupOpen);
            DeleteOperationCommand = new Command(async () => await DeleteOperationAsync());
        }

        public ObservableCollection<OperationMessage> Operations
        {
            get => operations;
            set => SetProperty(ref operations, value);
        }

        public OperationMessage SelectedOperation
        {
            get => selectedOperation;
            set => SetProperty(ref selectedOperation, value);
        }

        public string Requester
        {
            get => requester;
            private set => SetProperty(ref requester, value);
        }

        public string CurrentStatus
        {
            get => currentStatus;
            set => SetProperty(ref currentStatus, value);
        }

        public bool IsServerConnected
        {
            get => isServerConnected;
            set => SetProperty(ref isServerConnected, value);
        }

        public bool IsDetailPopupOpen
        {
            get => isDetailPopupOpen;
            set => SetProperty(ref isDetailPopupOpen, value);
        }

        public AnimationType BusyAnimationType
        {
            get => busyAnimationType;
            set => SetProperty(ref busyAnimationType, value);
        }

        public Command RequestFlushCommand { get; set; }

        public Command RequestPhotoCommand { get; set; }

        public Command RequestAnalyzeCommand { get; set; }

        public Command ShareOperationCommand { get; }

        public Command DeleteOperationCommand { get; set; }

        public Command TogglePopupCommand { get; set; }

        #region Initialization

        public async Task InitializeAsync()
        {
            if (flusherService != null && IsServerConnected)
            {
                Log("[INFO] Already initialized.");
                return;
            }

            IsBusy = true;

            try
            {
                Log("loading cached data...");
                this.Operations = CacheHelper.LoadOperations();

                Log("initializing user...");
                await InitializeUserAsync();

                Log("initializing SignalR server connection...");
                await InitializeSignalRAsync();

                CurrentStatus = $"Hi {Requester}, you're connected.";
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Initialization error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                IsBusyMessage = "";
            }
        }

        private async Task InitializeUserAsync()
        {
            var user = $"{Device.RuntimePlatform} UI";

            try
            {
                using (var client = new HttpClient())
                using (var response = await client.GetAsync("https://api.ipify.org"))
                {
                    var ip = await response.Content.ReadAsStringAsync();

                    if (!string.IsNullOrEmpty(ip))
                    {
                        user = $"{Device.RuntimePlatform} UI ({ip})";
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[Error] User initialization: {ex.Message}");
                throw;
            }
            finally
            {
                Requester = user;
            }
        }

        private async Task InitializeSignalRAsync()
        {
            try
            {
                Log("[INFO] Initializing SignalR...");

                flusherService = new FlusherService(Secrets.SignalRServerEndpoint);

                flusherService.ConnectionChanged += Service_ConnectionChanged;
                flusherService.MessageReceived += Service_MessageReceived;
                flusherService.PhotoReceived += Service_PhotoReceived;
                flusherService.AnalyzeResultReceived += Service_AnalyzeResultReceived;

                await flusherService.StartAsync();

                IsServerConnected = true;

                Log("[INFO] SignalR connected.");
            }
            catch (Exception ex)
            {
                Log($"[ERROR] SignalR Initialization: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region SignalR Service Methods

        private void Service_ConnectionChanged(object sender, string status)
        {
            Log($"[INFO] Connection Changed. Status: {status}");

            IsServerConnected = status == "Connected";
        }

        /// <summary>
        /// This method will be invoked when the service has any outgoing messages (i.e. status changes).
        /// </summary>
        /// <param name="message"></param> 
        private void Service_MessageReceived(string message)
        {
            Log($"[INFO] {message}");

            CurrentStatus = message;
        }

        /// <summary>
        /// This method will be invoked when the service sends a photo.
        /// </summary>
        /// <param name="message">This will be the message from the service</param>
        /// <param name="imageUrl">This will be the URL to the photo.</param>
        private void Service_PhotoReceived(string message, string imageUrl)
        {
            Log($"[INFO] Photo received: {message}.");

            CurrentStatus = message;

            Operations.Add(new OperationMessage
            {
                Message = message,
                ImageUrl = imageUrl,
                TimeStamp = DateTime.Now
            });

            Save();
        }

        /// <summary>
        /// This method will be invoked when the service sends the result of an analyze.
        /// </summary>
        /// <param name="message">This will be the message from the service</param>
        /// <param name="imageUrl">This will be the URL to the photo.</param>
        private void Service_AnalyzeResultReceived(string message, string imageUrl)
        {
            Log($"[INFO] Presence Detection: {message}.");

            CurrentStatus = message;

            Operations.Add(new OperationMessage
            {
                Message = message,
                ImageUrl = imageUrl,
                TimeStamp = DateTime.Now
            });

            Save();
        }

        #endregion

        #region Tasks

        private async Task RequestFlushAsync()
        {
            try
            {
                IsBusy = true;

                Log("Flushing, please wait...");

                await flusherService.SendFlushRequestAsync(this.Requester);
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Request flush error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                IsBusyMessage = "";

                CurrentStatus = "Flush requested.";
            }
        }

        private async Task RequestPhotoAsync()
        {
            try
            {
                IsBusy = true;

                Log("Requesting photo, please wait...");

                await flusherService.SendPhotoRequestAsync(this.Requester);
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Request flush error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                IsBusyMessage = "";

                CurrentStatus = "Photo requested.";
            }
        }

        private async Task RequestAnalyzeAsync()
        {
            try
            {
                IsBusy = true;

                Log("Analyzing, please wait...");

                await flusherService.SendAnalyzeRequestAsync(this.Requester);
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Analyze error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                IsBusyMessage = "";

                CurrentStatus = "Analyze requested.";
            }
        }

        private async Task DeleteOperationAsync()
        {
            try
            {
                var result = await Application.Current.MainPage.DisplayAlert(
                    "Confirm Delete",
                    "Are you sure you want to delete this from the local cache?",
                    "DELETE",
                    "cancel");

                if (result)
                {
                    IsBusy = true;
                    
                    Log("Removing operation...");
                    Operations.Remove(SelectedOperation);

                    Log("Deleting local image file...");
                    if (CacheHelper.DeleteLocalImageFile(SelectedOperation))
                    {
                        Log("Local image file deleted.");
                    }

                    Log("Updating cache...");
                    if (CacheHelper.SaveOperations(Operations))
                    {
                        Log("Cache updated.");
                    }

                    SelectedOperation = null;
                    IsDetailPopupOpen = false;
                }
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Delete operation error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                IsBusyMessage = "";
            }
        }

        public async Task ShareAsync()
        {
            if (string.IsNullOrEmpty(SelectedOperation?.ImageUrl))
            {
                return;
            }

            try
            {
                IsBusy = true;
                Log("[INFO] Starting share...");

                var fileName = Path.GetFileName(SelectedOperation.ImageUrl);
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                using (var client = new HttpClient())
                {
                    Log("[INFO] Downloading image...");
                    var imgBytes = await client.GetByteArrayAsync(SelectedOperation.ImageUrl);

                    Log("[INFO] Saving image...");
                    File.WriteAllBytes(filePath, imgBytes);
                }

                Log("[INFO] Sharing image...");
                await Share.RequestAsync(new ShareFileRequest(fileName, new ShareFile(filePath)));
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Problem sharing image: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                IsBusyMessage = "";
            }
        }

        #endregion

        #region Utilities

        private void Log(string message)
        {
            Debug.WriteLine(message);

            if (IsBusy)
            {
                IsBusyMessage = message;
            }
        }

        private void Save()
        {
            Log("Updating cache...");

            if (CacheHelper.SaveOperations(Operations))
            {
                Log("Cache updated.");
            }
        }

        public void Dispose()
        {
            flusherService.ConnectionChanged -= Service_ConnectionChanged;
            flusherService.MessageReceived -= Service_MessageReceived;
            flusherService.AnalyzeResultReceived -= Service_AnalyzeResultReceived;

            flusherService.Dispose();
        }

        #endregion
    }
}