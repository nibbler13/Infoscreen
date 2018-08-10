using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Infoscreen {
	/// <summary>
	/// Логика взаимодействия для App.xaml
	/// </summary>
	public partial class App : Application {
		private void Application_Startup(object sender, StartupEventArgs e) {
			Logging.ToLog("App - ===== Запуск приложения");

			string configFilePath = string.Empty;

			if (e.Args.Length != 1) {
				Logging.ToLog("App - Количество переданных параметров не равно одному, " +
					"пропуск считывания файла с настройками");
			} else
				configFilePath = e.Args[0].ToString();

			MainWindow window = new MainWindow(configFilePath);
			window.Show();
		}
	}
}
