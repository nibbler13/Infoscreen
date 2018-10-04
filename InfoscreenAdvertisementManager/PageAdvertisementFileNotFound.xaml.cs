using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
	/// Логика взаимодействия для PageAdvertisementFileNotFound.xaml
	/// </summary>
	public partial class PageAdvertisementFileNotFound : Page {
		public PageAdvertisementFileNotFound() {
			InitializeComponent();
		}

		private void ButtonSelectFile_Click(object sender, RoutedEventArgs e) {
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.CheckFileExists = true;
			openFileDialog.Filter = "Advertisement.xml (*.xml)|*.xml";
			openFileDialog.Multiselect = false;

			if (openFileDialog.ShowDialog() != true)
				return;

			Infoscreen.Advertisement advertisement = new Infoscreen.Advertisement();
			Infoscreen.Advertisement.LoadAdvertisement(openFileDialog.FileName, out advertisement);

			if (advertisement.IsReadedSuccessfully) {
				PageAdvertisementFileView pageAdvertisementFileView = 
					new PageAdvertisementFileView(advertisement);
				NavigationService.Navigate(pageAdvertisementFileView);
			} else {
				MessageBox.Show("Не удалось корректно прочитать файл с информацией о рекламе", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
				try {
					Process.Start(Infoscreen.Logging.GetCurrentLogFileName());
				} catch (Exception exc) {
					MessageBox.Show("", exc.Message + Environment.NewLine + exc.StackTrace,
						MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void ButtonCreateNewFile_Click(object sender, RoutedEventArgs e) {
			Infoscreen.Advertisement advertisement = new Infoscreen.Advertisement();
			PageAdvertisementFileView pageAdvertisementFileView = 
				new PageAdvertisementFileView(advertisement);
			NavigationService.Navigate(pageAdvertisementFileView);
		}
	}
}
