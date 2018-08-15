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
		public bool ShowAd { get; set; }
		public string AdDurationInSeconds { get; set; }
		public string PauseBetweenAdInSeconds { get; set; }

		public ObservableCollection<Infoscreen.Advertisement.ItemAdvertisement> AdvertisementItems { get; set; } =
			new ObservableCollection<Infoscreen.Advertisement.ItemAdvertisement>();

		public PageAdvertisementFileView(Infoscreen.Advertisement advertisement) {
			InitializeComponent();
			this.advertisement = advertisement;
			DataContext = this;
			DataGridItemAdvertisement.Items.Clear();
			DataGridItemAdvertisement.DataContext = this;

			AdDurationInSeconds = advertisement.AdDurationInSeconds.ToString();
			PauseBetweenAdInSeconds = advertisement.PauseBetweenAdInSeconds.ToString();
			ShowAd = advertisement.ShowAd;

			advertisement.AdvertisementItems.ForEach(AdvertisementItems.Add);
		}

		private void ButtonSaveChanges_Click(object sender, RoutedEventArgs e) {

		}

		private void ButtonDeleteAd_Click(object sender, RoutedEventArgs e) {

		}

		private void ButtonAddAd_Click(object sender, RoutedEventArgs e) {
			AdvertisementItems.Add(new Infoscreen.Advertisement.ItemAdvertisement());
		}

		private void DataGridItemAdvertisement_SelectionChanged(object sender, SelectionChangedEventArgs e) {

		}
	}
}
