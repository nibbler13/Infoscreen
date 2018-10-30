using Microsoft.WindowsAPICodePack.Dialogs;
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
	/// Логика взаимодействия для PageInternalSettings.xaml
	/// </summary>
	public partial class PageInternalSettings : Page {
		private Infoscreen.FirebirdClient firebirdClient;
		private Infoscreen.Configuration configuration;




		public PageInternalSettings(Infoscreen.Configuration configuration) {
			InitializeComponent();
			this.configuration = configuration;
			DataContext = configuration;
			Infoscreen.Logging.ToLog("Отображение раздела внутренних настроек");
		}









		private void ButtonCheckDBConnect_Click(object sender, RoutedEventArgs e) {
			if (firebirdClient == null)
				firebirdClient = new Infoscreen.FirebirdClient(
					configuration.DataBaseAddress,
					configuration.DataBaseName,
					configuration.DataBaseUserName,
					configuration.DataBasePassword);

			if (firebirdClient.IsDbAvailable())
				MessageBox.Show(Window.GetWindow(this), "Соединение выполнено успешно", "",
					MessageBoxButton.OK, MessageBoxImage.Information);
			else
				MessageBox.Show(Window.GetWindow(this), "Не удалось выполнить тестовый запрос. " +
					"Подробности можно узнать в журнале работы, расположенном в папке с программой.", "",
					MessageBoxButton.OK, MessageBoxImage.Error);
		}

		private void ButtonSelectPhotosFolder_Click(object sender, RoutedEventArgs e) {
			using (CommonOpenFileDialog openFileDialog = new CommonOpenFileDialog()) {
				openFileDialog.IsFolderPicker = true;

				if (!string.IsNullOrEmpty(configuration.PhotosFolderPath))
					openFileDialog.InitialDirectory = configuration.PhotosFolderPath;

				if (openFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
					configuration.PhotosFolderPath = openFileDialog.FileName;
			}
		}

		private void ButtonSelectActiveDirectoryOU_Click(object sender, RoutedEventArgs e) {
			if (!string.IsNullOrEmpty(configuration.ActiveDirectoryOU)) {
				if (MessageBox.Show(Window.GetWindow(this), "При смене подразделения ActiveDirectory будет заменен весь текущий " +
					"список соответствия кресел. Продолжить?", "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
					return;
			}

			WindowSelectAdOu windowSelectAdOu = new WindowSelectAdOu(configuration.ActiveDirectoryOU);
			windowSelectAdOu.Owner = Window.GetWindow(this);

			if (windowSelectAdOu.ShowDialog() == true) {
				configuration.ActiveDirectoryOU = windowSelectAdOu.SelectedOU;
				//UpdateSystemItems();
			}
		}

		private void ButtonSaveConfig_Click(object sender, RoutedEventArgs e) {
			string errorMsg = string.Empty;

			if (string.IsNullOrEmpty(configuration.DataBaseAddress) ||
				string.IsNullOrWhiteSpace(configuration.DataBaseAddress))
				errorMsg += Environment.NewLine + "Поле 'подключение к БД МИС Инфоклиника - адрес' не может быть пустым";


			if (string.IsNullOrEmpty(configuration.DataBaseName) ||
				string.IsNullOrWhiteSpace(configuration.DataBaseName))
				errorMsg += Environment.NewLine + "Поле 'подключение к БД МИС Инфоклиника - имя базы' не может быть пустым";

			if (string.IsNullOrEmpty(configuration.DataBaseUserName) ||
				string.IsNullOrWhiteSpace(configuration.DataBaseUserName))
				errorMsg += Environment.NewLine + "Поле 'подключение к БД МИС Инфоклиника - имя пользователя' не может быть пустым";

			if (string.IsNullOrEmpty(configuration.DataBasePassword) ||
				string.IsNullOrWhiteSpace(configuration.DataBasePassword))
				errorMsg += Environment.NewLine + "Поле 'подключение к БД МИС Инфоклиника - пароль' не может быть пустым";

			if (firebirdClient != null && !firebirdClient.IsDbAvailable())
				errorMsg += Environment.NewLine + "Не удается подключиться к БД, используя текущие параметры";


			if (string.IsNullOrEmpty(configuration.ActiveDirectoryOU) ||
				string.IsNullOrWhiteSpace(configuration.ActiveDirectoryOU))
				errorMsg += Environment.NewLine + "Поле 'соответствие кресел - подразделение в ActiveDirectory' не может быть пустым";

			if (!string.IsNullOrEmpty(errorMsg)) {
				errorMsg = "Невозможно продолжить сохранение по следующим причинам:" + Environment.NewLine + errorMsg;
				MessageBox.Show(Window.GetWindow(this), errorMsg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			try {
				if (Infoscreen.Configuration.SaveConfiguration(configuration)) {
					MessageBox.Show(Window.GetWindow(this), "Конфигурация успешно сохранена в файл: " + configuration.ConfigFilePath,
						"", MessageBoxButton.OK, MessageBoxImage.Information);
					Infoscreen.Logging.ToLog("Сохранение внутренних настроек");
				}
				else
					MessageBox.Show(Window.GetWindow(this), "Возникла ошибка при сохранении конфигурации в файл: " + configuration.ConfigFilePath +
						". Подробности в журнале работы.", "", MessageBoxButton.OK, MessageBoxImage.Error);
			} catch (Exception exc) {
				Infoscreen.Logging.ToLog(exc.Message + Environment.NewLine + exc.StackTrace);
				MessageBox.Show(Window.GetWindow(this), "Возникла ошибка при сохранении конфигурации в файл: " + configuration.ConfigFilePath +
					". " + exc.Message + Environment.NewLine + exc.StackTrace, "", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ButtonBack_Click(object sender, RoutedEventArgs e) {
			NavigationService.GoBack();
		}
	}
}
