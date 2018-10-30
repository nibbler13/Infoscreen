using System;
using System.Collections.Generic;
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
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		public MainWindow() {
			InitializeComponent();

			Infoscreen.Logging.ToLog("=== Запуск приложения ConfigManager, пользователь: " + Environment.UserName);

			Loaded += MainWindow_Loaded;
		}

		private async void MainWindow_Loaded(object sender, RoutedEventArgs e) {
			string cofigFilePath = Infoscreen.Logging.ASSEMBLY_DIRECTORY + "InfoscreenConfig.xml";
			TextBlockMain.Text = "Считывание файла конфигурации: " + cofigFilePath;

			Infoscreen.Configuration configuration = null;
			await Task.Run(() => {
				Infoscreen.Configuration.LoadConfiguration(
					cofigFilePath, out configuration);
			});

			TextBlockMain.Visibility = Visibility.Hidden;
			FrameMain.Visibility = Visibility.Visible;

			if (configuration.IsConfigReadedSuccessfull) {
				PageConfigView pageConfigView = new PageConfigView(configuration);
				FrameMain.Navigate(pageConfigView);
			} else {
				PageConfigNotFound pageConfigNotFound = new PageConfigNotFound();
				FrameMain.Navigate(pageConfigNotFound);
			}
		}
	}
}
