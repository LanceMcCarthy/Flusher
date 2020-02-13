using Flusher.Forms.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Flusher.Forms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AboutPage : ContentPage
    {
        public AboutPage()
        {
            InitializeComponent();
            BindingContext = new AboutPageViewModel();
        }
    }
}