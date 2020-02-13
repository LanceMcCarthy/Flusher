using System;
using System.Windows.Input;
using CommonHelpers.Common;
using Flusher.Common.Helpers;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Flusher.Forms.ViewModels
{
    public class AboutPageViewModel : ViewModelBase
    {
        private string appDescription;

        public AboutPageViewModel()
        {
            Title = "About";
            AppDescription = "A Windows IoT on Raspberry Pi, SignalR and XamarinForms project that uses Azure Custom Vision AI " +
                             "to automatically flush a human toilet for my cat while I'm on vacation 😎";

            OpenWebCommand = new Command(async (obj) =>
            {
                if (obj is string target)
                {
                    string url;

                    switch (target)
                    {
                        case "WebPortal":
                            url = Secrets.WebHomePageUrl;
                            break;
                        case "Thingiverse":
                            url = "https://www.thingiverse.com/make:760269";
                            break;
                        case "SourceCode":
                            url = "https://github.com/LanceMcCarthy/Flusher";
                            break;
                        default:
                            url = "https://github.com/LanceMcCarthy/Flusher";
                            break;
                    }

                    await Launcher.OpenAsync(new Uri(url));
                }
            });
        }

        public string AppDescription
        {
            get => appDescription;
            set => SetProperty(ref appDescription, value);
        }

        public ICommand OpenWebCommand { get; }
    }
}
