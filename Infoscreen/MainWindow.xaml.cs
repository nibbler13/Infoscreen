using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Threading;

namespace Infoscreen {
	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		private readonly string configPath;
		private PageChairsRoot pageChairsRoot;
		private PageAdvertisement pageAdvertisement;
		private DispatcherTimer timerMain;
		private DispatcherTimer timerAdShow;

		public MainWindow(string configPath) {
			Logging.ToLog("MainWindow - Создание основного окна приложения");

			InitializeComponent();

			this.configPath = configPath;

			if (Debugger.IsAttached) {
				Topmost = false;
				Cursor = Cursors.Arrow;
				WindowState = WindowState.Normal;
			}

			PreviewKeyDown += (s, e) => {
				if (e.Key.Equals(Key.Escape)) {
					Logging.ToLog("MainWindow - ===== Завершение работы по нажатию клавиши ESC");
					Application.Current.Shutdown();
				}
			};

			Loaded += MainWindow_Loaded;
		}

		private async void MainWindow_Loaded(object sender, RoutedEventArgs e) {
			string configFilePath = Path.Combine(configPath, "InfoscreenConfig.xml");
			string advertisementFilePath = Path.Combine(configPath, "Advertisement.xml");
			string fullScreenAdPath = Path.Combine(configPath, "FullScreenAdvertisements");

			Logging.ToLog("App - путь к файлу настроек: " + configFilePath);
			Logging.ToLog("App - путь к файлу информационных сообщений: " + advertisementFilePath);
			Logging.ToLog("App - путь к файлам полноэкранных информационных изображений: " + advertisementFilePath);

			Configuration configuration = new Configuration();
			Advertisement advertisement = new Advertisement();

			await Task.Run(() => {
				Configuration.LoadConfiguration(configFilePath, out configuration);
			});

			await Task.Run(() => {
				Advertisement.LoadAdvertisement(advertisementFilePath, out advertisement);
			});

			if (configuration.IsSystemWorkAsTimetable() && Debugger.IsAttached)
				WindowState = WindowState.Maximized;

			pageChairsRoot = new PageChairsRoot(configuration, advertisement);
			List<string> fullScreenAd = FullScreenAd.GetAvailableAd(fullScreenAdPath);

            Logging.ToLog("Список изображений для показа: " + Environment.NewLine +
                string.Join(Environment.NewLine, fullScreenAd));

			if (fullScreenAd.Count > 0) {
				int secondsRoomStatus = 60;
				int secondsFullscreenAd = 20;

                if (Debugger.IsAttached) {
                    secondsRoomStatus = 20;
                    secondsFullscreenAd = 5;
                }

				Logging.ToLog("Запуск таймера отображения полноэкранных информационных сообщений");
				Logging.ToLog("Значения длительности отображения в секундах, статус кабинета - " + 
					secondsRoomStatus + ", полноэкранные информационные сообщения - " + secondsFullscreenAd);

				pageAdvertisement = new PageAdvertisement(fullScreenAd);
				timerMain = new DispatcherTimer();
				timerMain.Interval = TimeSpan.FromSeconds(secondsRoomStatus);
				timerMain.Tick += TimerBackToMainWindow_Tick;
				timerMain.Start();

				timerAdShow = new DispatcherTimer();
				timerAdShow.Interval = TimeSpan.FromSeconds(secondsFullscreenAd);
				timerAdShow.Tick += TimerFullscreenAdShow_Tick;
			}

			FrameMain.Navigate(pageChairsRoot);
		}

		private void TimerFullscreenAdShow_Tick(object sender, EventArgs e) {
			timerAdShow.Stop();
			timerMain.Start();
			TimerBackToMainWindow_Tick(timerMain, new EventArgs());
		}

		private void TimerBackToMainWindow_Tick(object sender, EventArgs e) {
			if (FrameMain.Content == pageChairsRoot) {
				Logging.ToLog("Переключение на страницу полноэкранных информационных сообщений");
				FrameMain.Navigate(pageAdvertisement);
				timerAdShow.Start();
				timerMain.Stop();
			} else {
				Logging.ToLog("Переключение на страницу статуса кабинета");
				FrameMain.Navigate(pageChairsRoot);
			}
		}
	}
}
