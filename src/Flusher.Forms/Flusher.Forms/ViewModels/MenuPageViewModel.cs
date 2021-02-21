using System.Collections.ObjectModel;
using CommonHelpers.Common;
using Flusher.Forms.Models;
using Flusher.Forms.Views;

namespace Flusher.Forms.ViewModels
{
    public class MenuPageViewModel : ViewModelBase
    {
        private ObservableCollection<NavigationMenuItem> menuItems;

        public MenuPageViewModel()
        {
        }

        public ObservableCollection<NavigationMenuItem> MenuItems
        {
            get => menuItems ?? (menuItems = new ObservableCollection<NavigationMenuItem> 
            {
                new NavigationMenuItem { Id = 0, Title = "Operations", TargetType = typeof(OperationsPage)},
                new NavigationMenuItem { Id = 1, Title = "About", TargetType = typeof(AboutPage) }
            });
            set => SetProperty(ref menuItems, value);
        }
    }
}
