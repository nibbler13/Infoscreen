using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
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

			DispatcherUnhandledException += App_DispatcherUnhandledException;

			string configPath = string.Empty;

			if (e.Args.Length == 1) {
				try {
					configPath = e.Args[0].ToString().TrimStart('"').TrimEnd('"');
					Logging.ToLog("App - Переданный параметр: " + configPath);
				} catch (Exception exc) {
					Logging.ToLog("App - " + exc.Message + Environment.NewLine + exc.StackTrace);
				}
			} else
				Logging.ToLog("App - приложение принимает в качестве первого параметра " +
					"путь к файлу конфигурации, в качестве второго параметра путь к файлу с " +
					"информационными сообщениями");
			
			MainWindow window = new MainWindow(configPath);
			window.Show();
		}

		private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
			if (Debugger.IsAttached)
				return;

			Logging.ToLog(e.Exception.Message + Environment.NewLine + e.Exception.StackTrace);
			SystemMail.SendMail(e.Exception.Message + Environment.NewLine + e.Exception.StackTrace);
			Logging.ToLog("!!!App - Аварийное завершение работы");
			Process.GetCurrentProcess().Kill();
		}
	}
}
