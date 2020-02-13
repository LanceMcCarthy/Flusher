using System.ComponentModel;
using Flusher.Common.Models;
using Xamarin.Forms;
using Flusher.Forms.ViewModels;
using Telerik.XamarinForms.DataControls.ListView;

namespace Flusher.Forms.Views
{
    [DesignTimeVisible(true)]
    public partial class OperationsPage : ContentPage
    {
        private readonly OperationsViewModel viewModel;

        public OperationsPage()
        {
            InitializeComponent();
            BindingContext = viewModel = new OperationsViewModel();
        }

        private void ListView_OnItemTapped(object sender, ItemTapEventArgs e)
        {
            if (e.Item is OperationMessage item)
            {
                viewModel.SelectedOperation = item;
                viewModel.IsDetailPopupOpen = true;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing(); 

            await viewModel.InitializeAsync();
        }
    }
}