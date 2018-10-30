using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Infoscreen {
	class ChairsHandler {
		private DataProvider dataProvider;
		private string chairs;
		private MainWindow mainWindow;
		private bool isLiveQueue;
		private readonly int chairUpdateIntervalInSeconds;
		private readonly int chairRotateIntervalInSeconds;
		private readonly int photoUpdateIntervalInSeconds;
		private bool isChairPagesCreationCompleted;
		private Dictionary<PageChair, Border> chairPages;
		private int currentPageIndex;

		public ChairsHandler(DataProvider dataProvider, string chairs, bool isLiveQueue, 
			int chairUpdateIntervalInSeconds, int chairRotateIntervalInSeconds, int photoUpdateIntervalInSeconds) {
			this.dataProvider = dataProvider;
			this.chairs = chairs;
			mainWindow = Application.Current.MainWindow as MainWindow;
			this.isLiveQueue = isLiveQueue;
			this.chairUpdateIntervalInSeconds = chairUpdateIntervalInSeconds;
			this.chairRotateIntervalInSeconds = chairRotateIntervalInSeconds;
			this.photoUpdateIntervalInSeconds = photoUpdateIntervalInSeconds;
			isChairPagesCreationCompleted = false;
			chairPages = new Dictionary<PageChair, Border>();
			currentPageIndex = 0;
		}

		public void Start() {
			if (string.IsNullOrEmpty(chairs)) {
				mainWindow.SetupTitle("Кабинет не выбран");
				Logging.ToLog("MainWindow - Не заполнен список кабинок");
				return;
			}

			StartUpdateChairs();
			StartUpdateDoctorsPhotos();

			dataProvider.OnUpdateCompleted += DataProvider_OnUpdateCompleted;
			dataProvider.UpdateData();
			dataProvider.UpdateDoctorsPhoto();
		}

		private void StartUpdateChairs() {
			Logging.ToLog("MainWindow - Запуск таймера обновления данных о кабинках");
			DispatcherTimer timerUpdateData = new DispatcherTimer {
				Interval = TimeSpan.FromSeconds(chairUpdateIntervalInSeconds)
			};

			timerUpdateData.Tick += (s, ev) => { dataProvider.UpdateData(); };
			timerUpdateData.Start();
		}

		private void StartUpdateDoctorsPhotos() {
			Logging.ToLog("MainWindow - Запуск таймера обновления фотографий");
			DispatcherTimer timerUpdatePhotos = new DispatcherTimer {
				Interval = TimeSpan.FromSeconds(photoUpdateIntervalInSeconds)
			};

			timerUpdatePhotos.Tick += (s, ev) => { dataProvider.UpdateDoctorsPhoto(); };
			timerUpdatePhotos.Start();
		}

		private void StartChairSwitch() {
			DispatcherTimer timerChangePage = new DispatcherTimer();
			timerChangePage = new DispatcherTimer {
				Interval = TimeSpan.FromSeconds(chairRotateIntervalInSeconds)
			};

			timerChangePage.Tick += TimerChangePage_Tick;
			timerChangePage.Start();
		}


		private void DataProvider_OnUpdateCompleted(object sender, EventArgs e) {
			if (!dataProvider.IsUpdateSuccessfull) {
				mainWindow.ShowErrorPage();
				return;
			}

			if (isChairPagesCreationCompleted)
				return;

			Logging.ToLog("MainWindow - Создание страниц для кресел");
			if (dataProvider.ChairsDict.Count == 0) {
				Logging.ToLog("MainWindow - Отсутствует информация о креслах");
				mainWindow.SetupTitle("Кабинет не выбран");
				return;
			} else {
				foreach (DataProvider.ItemChair itemChair in dataProvider.ChairsDict.Values) {
					PageChair pageChair = new PageChair(itemChair.ChID, itemChair.RNum, isLiveQueue, dataProvider);

					Border border = MainWindow.CreateIndicator();
					if (dataProvider.ChairsDict.Count > 1)
						mainWindow.StackPanelPageIndicator.Children.Add(border);

					chairPages.Add(pageChair, border);
				}

				if (chairPages.Count > 1)
					StartChairSwitch();

				NavigateToPage();
			}

			isChairPagesCreationCompleted = true;
		}




		private void TimerChangePage_Tick(object sender, EventArgs e) {
			Logging.ToLog("MainWindow - Смена страницы с креслом");
			chairPages.Values.ToList()[currentPageIndex].Background = Brushes.LightGray;
			chairPages.Values.ToList()[currentPageIndex].Height = 5;
			currentPageIndex++;

			if (currentPageIndex == chairPages.Count)
				currentPageIndex = 0;

			NavigateToPage();
		}

		private void NavigateToPage() {
			PageChair pageToShow = chairPages.Keys.ToList()[currentPageIndex];
			chairPages[pageToShow].Background = Brushes.Gray;
			chairPages[pageToShow].Height = 10;

			if (chairPages.Keys.Where(x => x.IsReceptionConducted).Count() > 0 &&
				!pageToShow.IsReceptionConducted) {
				TimerChangePage_Tick(null, null);
				return;
			}

			if (mainWindow.FrameMain.Content == pageToShow)
				return;

			Logging.ToLog("MainWindow - Новое значение: " + pageToShow.ToString());
			mainWindow.SetupTitle("Кабинет " + pageToShow.RNum);
			mainWindow.FrameMain.Navigate(pageToShow);
		}
	}
}
