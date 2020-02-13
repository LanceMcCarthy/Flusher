using Flusher.Forms.Views;
using Xamarin.Forms;

namespace Flusher.Forms
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
           // MainPage = new NavigationPage(new OperationsPage());
            MainPage = new HomePage();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
