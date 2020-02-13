using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Flusher.Uwp
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();

            Loaded += MainPage_Loaded;
            Unloaded += MainPage_Unloaded; 
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.InitializeAsync();

            // TODO app suspension consideration.
            // App.ViewModel = this.ViewModel;
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel?.Dispose();
        }
    }
}
