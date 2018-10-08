using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace InfoscreenAdvertisementManager {
	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		public MainWindow() {
			InitializeComponent();
			Loaded += MainWindow_Loaded;
		}

		private async void MainWindow_Loaded(object sender, RoutedEventArgs e) {
			string adFilePath = Infoscreen.Logging.ASSEMBLY_DIRECTORY + "Advertisement.xml";
			TextBlockMain.Text = "Считывание файла с информацией о рекламе: " + adFilePath;

			Infoscreen.Advertisement advertisement = null;
			await Task.Run(() => {
				Infoscreen.Advertisement.LoadAdvertisement(
					adFilePath, out advertisement);
			});

			TextBlockMain.Visibility = Visibility.Hidden;
			FrameMain.Visibility = Visibility.Visible;

			if (advertisement.IsReadedSuccessfully) {
				PageAdvertisementFileView pageAdvertisementFileView = 
					new PageAdvertisementFileView(advertisement);
				FrameMain.Navigate(pageAdvertisementFileView);
			} else {
				PageAdvertisementFileNotFound pageAdvertisementFileNotFound = 
					new PageAdvertisementFileNotFound();
				FrameMain.Navigate(pageAdvertisementFileNotFound);
			}
		}
	}
}
