using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace InfoscreenAdvertisementManager {
	/// <summary>
	/// Логика взаимодействия для PageAdvertisementFileView.xaml
	/// </summary>
	public partial class PageAdvertisementFileView : Page {
		private Infoscreen.Advertisement advertisement;

		public PageAdvertisementFileView(Infoscreen.Advertisement advertisement) {
			InitializeComponent();
			this.advertisement = advertisement;
			DataContext = advertisement;
			DataGridItemAdvertisement.Items.Clear();
			DataGridItemAdvertisement.DataContext = advertisement;
			CheckBoxDisableAdDisplay_Checked(CheckBoxDisableAdDisplay, new RoutedEventArgs());
		}

		private void ButtonSaveChanges_Click(object sender, RoutedEventArgs e) {
			if (advertisement.SaveAdvertisements(out string error))
				MessageBox.Show(Application.Current.MainWindow, "Изменения успешно сохранены", "",
					MessageBoxButton.OK, MessageBoxImage.Information);
			else
				MessageBox.Show(Application.Current.MainWindow, "При сохранении возникла ошибка: " + error, "", 
					MessageBoxButton.OK, MessageBoxImage.Error);
		}

		private void ButtonDeleteAd_Click(object sender, RoutedEventArgs e) {
			MessageBoxResult result = MessageBox.Show(Application.Current.MainWindow, 
				"Вы действительно хотите удалить выделенные строки?", "", MessageBoxButton.YesNo, MessageBoxImage.Question);

			if (result != MessageBoxResult.Yes)
				return;

			List<Infoscreen.Advertisement.ItemAdvertisement> itemsToDelete = 
				new List<Infoscreen.Advertisement.ItemAdvertisement>();

			foreach (Infoscreen.Advertisement.ItemAdvertisement item in DataGridItemAdvertisement.SelectedItems)
				itemsToDelete.Add(item);

			foreach (Infoscreen.Advertisement.ItemAdvertisement item in itemsToDelete)
				advertisement.AdvertisementItems.Remove(item);
		}

		private void ButtonAddAd_Click(object sender, RoutedEventArgs e) {
			advertisement.AdvertisementItems.Add(new Infoscreen.Advertisement.ItemAdvertisement());
		}

		private void DataGridItemAdvertisement_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			ButtonDeleteAd.IsEnabled = DataGridItemAdvertisement.SelectedItems.Count > 0;
		}

		private void CheckBoxDisableAdDisplay_Checked(object sender, RoutedEventArgs e) {
			BorderDisableAdDisplay.Background = advertisement.DisableAdDisplay ? Brushes.Yellow : Brushes.Transparent;
		}
	}
}
