using Xamarin.Forms.Platform.UWP;

namespace Flusher.Forms.Uwp
{
    public sealed partial class MainPage : WindowsPage
    {
        public MainPage()
        {
            InitializeComponent();
            LoadApplication(new Flusher.Forms.App());
        }
    }
}
