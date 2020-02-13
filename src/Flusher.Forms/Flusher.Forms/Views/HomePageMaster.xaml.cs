using Flusher.Forms.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Flusher.Forms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HomePageMaster : ContentPage
    {
        public ListView ListView;

        public HomePageMaster()
        {
            InitializeComponent();

            BindingContext = new HomePageMasterViewModel();
            ListView = MenuItemsListView;
        }
    }
}