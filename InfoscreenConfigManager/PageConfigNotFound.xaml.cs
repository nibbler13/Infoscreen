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

namespace InfoscreenConfigManager {
	/// <summary>
	/// Логика взаимодействия для PageConfigNotFound.xaml
	/// </summary>
	public partial class PageConfigNotFound : Page {
		public PageConfigNotFound() {
			InitializeComponent();
		}

		private void ButtonSelectFile_Click(object sender, RoutedEventArgs e) {
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.CheckFileExists = true;
			openFileDialog.Filter = "InfoscreenConfig.xml (*.xml)|*.xml";
			openFileDialog.Multiselect = false;

			if (openFileDialog.ShowDialog() != true)
				return;

			Infoscreen.ConfigReader configReader = new Infoscreen.ConfigReader();
			configReader.ReadConfigFile(openFileDialog.FileName, false);

			if (configReader.IsConfigReadedSuccessfull) {
				PageConfigView pageConfigView = new PageConfigView(configReader);
				NavigationService.Navigate(pageConfigView);
			} else {
				MessageBox.Show("Не удалось корректно прочитать файл с конфигурацией", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				try {
					Process.Start(Infoscreen.Logging.GetCurrentLogFileName());
				} catch (Exception exc) {
					MessageBox.Show("", exc.Message + Environment.NewLine + exc.StackTrace, MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void ButtonCreateNewFile_Click(object sender, RoutedEventArgs e) {
			Infoscreen.ConfigReader configReader = new Infoscreen.ConfigReader();
			configReader.InitializeConfig(
				Properties.Settings.Default.DataBaseUsername, 
				Properties.Settings.Default.DataBaseUserPassword, 
				Properties.Settings.Default.DataBaseSqlQuery);

			PageConfigView pageConfigView = new PageConfigView(configReader);
			NavigationService.Navigate(pageConfigView);
		}
	}
}
