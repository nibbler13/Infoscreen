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
		private List<FullScreenAd.ItemAd> fullScreenAdList;
		private int secondsRoomStatus;
		private int secondsFullscreenAd;
		private int currentAdId = 0;

		public MainWindow(string configPath) {
			Logging.ToLog("MainWindow - Создание основного окна приложения");

			InitializeComponent();

			this.configPath = configPath;

			if (Debugger.IsAttached || Environment.MachineName.ToLower().Equals("mssu-dev")) {
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
			Logging.ToLog("App - путь к файлам полноэкранных информационных изображений: " + fullScreenAdPath);

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
			fullScreenAdList = FullScreenAd.GetAdItems(fullScreenAdPath, true);

            Logging.ToLog("Список изображений для показа: " + Environment.NewLine +
                string.Join(Environment.NewLine, fullScreenAdList));

			if (fullScreenAdList.Count > 0) {
				secondsRoomStatus = 30;
				secondsFullscreenAd = 10;
				Random random = new Random();
				currentAdId = random.Next(0, fullScreenAdList.Count - 1);

                if (Debugger.IsAttached) {
                    secondsRoomStatus = 5;
					secondsFullscreenAd = 5;
				}

				Logging.ToLog("Запуск таймера отображения полноэкранных информационных сообщений");
				Logging.ToLog("Значения длительности отображения в секундах, статус кабинета - " + 
					secondsRoomStatus + ", полноэкранные информационные сообщения - " + secondsFullscreenAd);

				timerMain = new DispatcherTimer();
				timerMain.Interval = TimeSpan.FromSeconds(random.Next(0, secondsRoomStatus));
				timerMain.Tick += TimerMain_Tick;
				timerMain.Start();
			}

			FrameMain.Navigate(pageChairsRoot);
		}

		private async Task PutTaskDelay() {
			await Task.Delay(TimeSpan.FromSeconds(secondsFullscreenAd));
		}

		private async void TimerMain_Tick(object sender, EventArgs e) {
			timerMain.Interval = TimeSpan.FromSeconds(secondsRoomStatus);
			timerMain.Stop();

			Logging.ToLog("Переключение на страницу полноэкранных информационных сообщений");
			Logging.ToLog("Изображение: " + Path.GetFileName(fullScreenAdList[currentAdId].OptimalImage));
			pageAdvertisement = new PageAdvertisement(fullScreenAdList[currentAdId].OptimalImage);
			FrameMain.Navigate(pageAdvertisement);

			await PutTaskDelay();

			currentAdId++;
			if (currentAdId == fullScreenAdList.Count)
				currentAdId = 0;

			Logging.ToLog("Переключение на страницу статуса кабинета");
			FrameMain.Navigate(pageChairsRoot);
			timerMain.Start();
		}
	}
}
