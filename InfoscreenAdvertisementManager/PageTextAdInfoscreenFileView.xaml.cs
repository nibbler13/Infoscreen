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
			DataGridItemAdvertisement.DataContext = advertisement;
			CheckBoxDisableAdDisplay_Checked(CheckBoxDisableAdDisplay, new RoutedEventArgs());

            Loaded += PageAdvertisementFileView_Loaded;
		}

        private void PageAdvertisementFileView_Loaded(object sender, RoutedEventArgs e) {
			advertisement.MarkAsSaved();
			Console.WriteLine("page advertisement.MarkAsSaved();");
			ButtonDeleteAllAd.IsEnabled = advertisement.AdvertisementItems.Count > 0;
			Loaded -= PageAdvertisementFileView_Loaded;
		}

        private void CheckBoxDisableAdDisplay_Checked(object sender, RoutedEventArgs e) {
			BorderDisableAdDisplay.Background = advertisement.DisableAdDisplay ? Brushes.Yellow : Brushes.Transparent;
		}

		private void DataGridItemAdvertisement_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			ButtonDeleteAd.IsEnabled = DataGridItemAdvertisement.SelectedItems.Count > 0;
		}

		private void ButtonSaveChanges_Click(object sender, RoutedEventArgs e) {
			if (!advertisement.IsAdItemsCorrect(out string incorrectItems)) {
				MessageBox.Show(
					Application.Current.MainWindow,
					"Имеются сообщения, требующие корректировки: " +
					Environment.NewLine + incorrectItems, "Сохранение невозможно",
					MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			if (advertisement.SaveAdvertisements(out string error)) {
				MessageBox.Show(Application.Current.MainWindow, "Изменения успешно сохранены", "",
					MessageBoxButton.OK, MessageBoxImage.Information);
				Infoscreen.Logging.ToLog("Сохранение изменений");
			} else
				MessageBox.Show(Application.Current.MainWindow, "При сохранении возникла ошибка: " + error, "",
					MessageBoxButton.OK, MessageBoxImage.Error);
		}

		private void ButtonDeleteAllAd_Click(object sender, RoutedEventArgs e) {
			ButtonDeleteAd_Click(null, null);
		}

		private void ButtonDeleteAd_Click(object sender, RoutedEventArgs e) {
			string question = "Вы действительно хотите удалить выделенные строки?";
			if (sender == null)
				question = "Вы действительно хотите удалить все строки?";

			MessageBoxResult result = MessageBox.Show(Application.Current.MainWindow,
				question, "", MessageBoxButton.YesNo, MessageBoxImage.Question);

			if (result != MessageBoxResult.Yes)
				return;

			List<Infoscreen.Advertisement.ItemAdvertisement> itemsToDelete =
				new List<Infoscreen.Advertisement.ItemAdvertisement>();

			if (sender == null)
				foreach (Infoscreen.Advertisement.ItemAdvertisement item in advertisement.AdvertisementItems)
					itemsToDelete.Add(item);
			else
				foreach (Infoscreen.Advertisement.ItemAdvertisement item in DataGridItemAdvertisement.SelectedItems)
					itemsToDelete.Add(item);
            
			foreach (Infoscreen.Advertisement.ItemAdvertisement item in itemsToDelete) {
				Infoscreen.Logging.ToLog("Удаление сообщения: " + item.Title + ", " + item.Body + ", " + item.PostScriptum);
				advertisement.AdvertisementItems.Remove(item);
			}

			ButtonDeleteAllAd.IsEnabled = advertisement.AdvertisementItems.Count > 0;
		}

		private void ButtonAddAd_Click(object sender, RoutedEventArgs e) {
			Infoscreen.Logging.ToLog("Добавление нового сообщения");
			Infoscreen.Advertisement.ItemAdvertisement itemAdvertisement = 
				new Infoscreen.Advertisement.ItemAdvertisement(
					advertisement.AdvertisementItems.Count > 0 ? 
					advertisement.AdvertisementItems.Last().Index + 1 : 1);
			advertisement.AdvertisementItems.Add(itemAdvertisement);
			DataGridItemAdvertisement.ScrollIntoView(itemAdvertisement);
			ButtonDeleteAllAd.IsEnabled = true;
		}
	}
}
