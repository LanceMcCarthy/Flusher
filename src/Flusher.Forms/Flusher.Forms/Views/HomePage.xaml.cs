using System;
using Flusher.Forms.Models;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Flusher.Forms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HomePage : MasterDetailPage
    {
        public HomePage()
        {
            InitializeComponent();
            MasterPage.ListView.ItemSelected += ListView_ItemSelected;
        }

        private void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var item = e.SelectedItem as NavigationMenuItem;

            if (item == null)
                return;

            (App.Current.MainPage as NavigationPage).Navigation.

            var page = (Page)Activator.CreateInstance(item.TargetType);
            page.Title = item.Title;

            Detail = new NavigationPage(page);
            IsPresented = false;

            MasterPage.ListView.SelectedItem = null;

        }
    }
}