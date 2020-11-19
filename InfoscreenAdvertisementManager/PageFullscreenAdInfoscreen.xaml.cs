using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Infoscreen;

namespace InfoscreenAdvertisementManager {
    /// <summary>
    /// Interaction logic for PageFullscreenAdInfoscreen.xaml
    /// </summary>
    public partial class PageFullscreenAdInfoscreen : Page {
        public ObservableCollection<FullScreenAd.ItemAd> Items { get; set; } = 
            new ObservableCollection<FullScreenAd.ItemAd>();
        public PageFullscreenAdInfoscreen(List<FullScreenAd.ItemAd> itemAds) {
            InitializeComponent();
            itemAds.ForEach(Items.Add);
            DataContext = this;
            ButtonDeleteAll.IsEnabled = Items.Count > 0;
        }

        private void ButtonDeleteAll_Click(object sender, RoutedEventArgs e) {
            ButtonDeleteAll.IsEnabled = Items.Count > 0;
        }

        private void ButtonDeleteSelected_Click(object sender, RoutedEventArgs e) {
        }

        private void ButtonAdd_Click(object sender, RoutedEventArgs e) {
            ButtonDeleteAll.IsEnabled = Items.Count > 0;
        }

        private void DataGridItems_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            ButtonDeleteSelected.IsEnabled = DataGridItems.SelectedItems.Count > 0;
            ButtonDeleteAll.IsEnabled = Items.Count > 0;
        }

        private void ButtonOpenFile_Click(object sender, RoutedEventArgs e) {
            Button button = sender as Button;
            Infoscreen.FullScreenAd.ItemAd itemAd = button.DataContext as Infoscreen.FullScreenAd.ItemAd;
            if (itemAd == null) {
                MessageBox.Show("Неизвестный элемент", string.Empty, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Process.Start(itemAd.OptimalImage);
        }

        private void ButtonSaveChanges_Click(object sender, RoutedEventArgs e) {

        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            Infoscreen.FullScreenAd.ItemImage itemImage = (sender as DataGrid).CurrentItem as Infoscreen.FullScreenAd.ItemImage;
            if (itemImage == null)
                return;

            Process.Start(itemImage.FullPath);
        }
    }
}
