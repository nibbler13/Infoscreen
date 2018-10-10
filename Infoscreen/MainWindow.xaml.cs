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
using System.Windows.Threading;

namespace Infoscreen {
	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		public static readonly RoutedEvent ShowAdvertisementEvent = 
			EventManager.RegisterRoutedEvent("ShowAdvertisement", RoutingStrategy.Bubble, 
			typeof(RoutedEventHandler), typeof(MainWindow));

		private Dictionary<PageChair, Border> chairPages = new Dictionary<PageChair, Border>();
		private int currentPageIndex = 0;
		private bool isErrorPageShowing = false;
		private bool isChairPagesCreationCompleted = false;
		private bool isLiveQueue = false;

		public event RoutedEventHandler ShowAdvertisement {
			add { AddHandler(ShowAdvertisementEvent, value); }
			remove { RemoveHandler(ShowAdvertisementEvent, value); }
		}

		private string configFilePath;
		private string advertisementFilePath;
		private Configuration configuration;
		private DataProvider dataProvider;
		private Advertisement advertisement;


		public MainWindow(string configFilePath, string advertisementFilePath) {
			Logging.ToLog("MainWindow - Создание основного окна приложения");

			InitializeComponent();
			this.configFilePath = configFilePath;
			this.advertisementFilePath = advertisementFilePath;

			PreviewKeyDown += (s, e) => {
				if (e.Key.Equals(Key.Escape)) {
					Logging.ToLog("MainWindow - ===== Завершение работы по нажатию клавиши ESC");
					Application.Current.Shutdown();
				}
			};

			Loaded += MainWindow_Loaded;

			if (Debugger.IsAttached) {
				Topmost = false;
				Cursor = Cursors.Arrow;
				WindowState = WindowState.Normal;
			}
		}

		private async void MainWindow_Loaded(object sender, RoutedEventArgs e) {
			DispatcherTimer timerSecondsTick = new DispatcherTimer();
			timerSecondsTick.Interval = TimeSpan.FromSeconds(1);
			timerSecondsTick.Tick += (s, ev) => {
				Application.Current.Dispatcher.Invoke((Action)delegate {
					TextBlockTimeSplitter.Visibility =
						TextBlockTimeSplitter.Visibility == Visibility.Visible ?
						Visibility.Hidden : Visibility.Visible;
					TextBlockTimeHours.Text = DateTime.Now.Hour.ToString();
					TextBlockTimeMinutes.Text = DateTime.Now.ToString("mm");
				});
			};
			timerSecondsTick.Start();

			await Task.Run(() => {
				Configuration.LoadConfiguration(configFilePath, out configuration);
			});

			if (!configuration.IsConfigReadedSuccessfull) {
				TextBlockRoom.Visibility = Visibility.Hidden;
				Logging.ToLog("MainWindow - Во время считывания настроек возникла ошибка, переход на страницу с ошибкой");
				FrameChair.Navigate(new PageError());
				return;
			}
			
			dataProvider = new DataProvider(configuration);

			if (configuration.IsSystemWorkAsTimetable()) {
				TextBlockRoom.Text = "Расписание работы сотрудников";
				DispatcherTimer timerUpdateTimetable = new DispatcherTimer();
				timerUpdateTimetable.Interval = TimeSpan.FromSeconds(configuration.TimetableRotateIntervalInSeconds);
				timerUpdateTimetable.Tick += TimerUpdateTimetable_Tick;
				timerUpdateTimetable.Start();
				TimerUpdateTimetable_Tick(timerUpdateTimetable, new EventArgs());

				if (Debugger.IsAttached)
					WindowState = WindowState.Maximized;

				return;
			}

			await Task.Run(() => {
				Advertisement.LoadAdvertisement(advertisementFilePath, out advertisement);
			});

			if (!advertisement.DisableAdDisplay && advertisement.AdvertisementItemsToShow.Count > 0) {
				Logging.ToLog("MainWindow - Запуск таймера отображения рекламы");
				DispatcherTimer timerShowAdvertisement = new DispatcherTimer();
				timerShowAdvertisement.Interval = TimeSpan.FromSeconds(25 + advertisement.PauseBetweenAdInSeconds);
				timerShowAdvertisement.Tick += TimerShowAdvertisement_Tick;
				timerShowAdvertisement.Start();
				TimerShowAdvertisement_Tick(timerShowAdvertisement, new EventArgs());
			} else
				Logging.ToLog("MainWindow - пропуск отображения рекламы в соответствии с настройками");


			if (string.IsNullOrEmpty(configuration.GetChairsIdForSystem(Environment.MachineName))) {
				TextBlockRoom.Text = "Кабинет не выбран";
				Logging.ToLog("MainWindow - Не заполнен список кабинок");
				return;
			}

			isLiveQueue = configuration.IsSystemHasLiveQueue();

			Logging.ToLog("MainWindow - Запуск таймера обновления данных");
			DispatcherTimer timerUpdateData = new DispatcherTimer();
			timerUpdateData.Interval = TimeSpan.FromSeconds(configuration.DatabaseQueryExecutionIntervalInSeconds);
			timerUpdateData.Tick += (s, ev) => { dataProvider.UpdateData(); };
			timerUpdateData.Start();

			Logging.ToLog("MainWindow - Запуск таймера обновления фотографий");
			DispatcherTimer timerUpdatePhotos = new DispatcherTimer();
			timerUpdatePhotos.Interval = TimeSpan.FromSeconds(configuration.DoctorsPhotoUpdateIntervalInSeconds);
			timerUpdatePhotos.Tick += (s, ev) => { dataProvider.UpdateDoctorsPhoto(); };
			timerUpdatePhotos.Start();

			dataProvider.OnUpdateCompleted += DataProvider_OnUpdateCompleted;
			dataProvider.UpdateData();
			dataProvider.UpdateDoctorsPhoto();
		}



		private async void TimerUpdateTimetable_Tick(object sender, EventArgs e) {
			Logging.ToLog("MainWindow - Обновление расписания");
			StackPanelPageIndicator.Children.Clear();
			DataProvider.Timetable timetableInitial = dataProvider.GetTimeTable();
			if (timetableInitial == null) {
				Logging.ToLog("MainWindow - не удалось получить информацию о расписании, " +
					"пропуск показа, переход на страницу с ошибкой");
				FrameChair.Navigate(new PageError());
				isErrorPageShowing = true;
				return;
			}
			
			if (isErrorPageShowing && FrameChair.CanGoBack) {
				try {
					FrameChair.GoBack();
					isErrorPageShowing = false;
				} catch (Exception exc) {
					Logging.ToLog("MainWindow - " + exc.Message + Environment.NewLine + exc.StackTrace);
					return;
				}
			}

			DispatcherTimer dispatcherTimer = sender as DispatcherTimer;

			if (dispatcherTimer == null)
				return;

			dispatcherTimer.Stop();
			
			DataProvider.Timetable timetableToShow = new DataProvider.Timetable();
			Dictionary<PageTimetable, Border> pagesTimetable = new Dictionary<PageTimetable, Border>();
			int row = 0;

			foreach (KeyValuePair<string, DataProvider.Timetable.Department> departmentPair in timetableInitial.departments) {
				if (row >= 12) {
					pagesTimetable.Add(new PageTimetable(timetableToShow), CreateIndicator());
					row = 0;
					timetableToShow.departments.Clear();
				}

				timetableToShow.departments.Add(departmentPair.Key, new DataProvider.Timetable.Department());
				row++;

				foreach (KeyValuePair<string, DataProvider.Timetable.DocInfo> docInfoPair in departmentPair.Value.doctors) {
					timetableToShow.departments[departmentPair.Key].doctors.Add(docInfoPair.Key, docInfoPair.Value);
					row++;

					if (row == 13) {
						pagesTimetable.Add(new PageTimetable(timetableToShow), CreateIndicator());
						row = 0;
						timetableToShow.departments.Clear();

						if (!docInfoPair.Equals(departmentPair.Value.doctors.Last())) {
							timetableToShow.departments.Add(departmentPair.Key, new DataProvider.Timetable.Department());
							row++;
						}
					}
				}
			}

			if (pagesTimetable.Count > 1)
				foreach (Border border in pagesTimetable.Values) 
					StackPanelPageIndicator.Children.Add(border);

			foreach (KeyValuePair<PageTimetable, Border> pagePair in pagesTimetable) {
				pagesTimetable.Values.ToList()[currentPageIndex].Background = Brushes.Gray;
				pagesTimetable.Values.ToList()[currentPageIndex].Height = 10;
				
				FrameChair.Navigate(pagePair.Key);
				await PutTaskDelay();

				pagesTimetable.Values.ToList()[currentPageIndex].Background = Brushes.LightGray;
				pagesTimetable.Values.ToList()[currentPageIndex].Height = 5;
				currentPageIndex++;

				if (currentPageIndex == pagesTimetable.Count)
					currentPageIndex = 0;
			}
			
			dispatcherTimer.Start();
		}


		private async Task PutTaskDelay() {
			await Task.Delay(TimeSpan.FromSeconds(configuration.TimetableRotateIntervalInSeconds));
		}

		private Border CreateIndicator() {
			Border border = new Border {
				Margin = new Thickness(3, 0, 3, 0),
				Height = 5,
				Width = 30,
				Background = Brushes.LightGray,
				VerticalAlignment = VerticalAlignment.Center
			};

			return border;
		}


		private void RaiseShowAdvertisementEvent() {
			BorderAdvertisementFirstPart.HorizontalAlignment = HorizontalAlignment.Right;
			BorderAdvertisementSecondPart.HorizontalAlignment = HorizontalAlignment.Right;
			BorderAdvertisementThirdPart.HorizontalAlignment = HorizontalAlignment.Right;

			BorderAdvertisementFirstPart.Width = 0;
			BorderAdvertisementSecondPart.Width = 0;
			BorderAdvertisementThirdPart.Width = 0;

			RoutedEventArgs routedEventArgs = new RoutedEventArgs(ShowAdvertisementEvent);
			BorderAdvertisementFirstPart.RaiseEvent(routedEventArgs);
		}

		private void TimerShowAdvertisement_Tick(object sender, EventArgs e) {
			Logging.ToLog("MainWindow - Отображение рекламного сообщения");

			if (isErrorPageShowing) {
				Logging.ToLog("MainWindow - пропуск отображения, т.к. в данный момент отображается сообщение об ошибке в работе");
				return;
			}

			if (advertisement.AdvertisementItems.Count == 0) {
				Logging.ToLog("MainWindow - пропуск отображения, т.к. отсутствуют доступные сообщения");
				return;
			}

			try {
				Advertisement.ItemAdvertisement itemAd = advertisement.GetNextAdItem();

				TextBlockAdvertisementTitle.Text = itemAd.Title;
				TextBlockAdvertisementBody.Text = itemAd.Body;
				TextBlockAdvertisementPostScriptum.Text = itemAd.PostScriptum;

				DocPanelAdvertisementTitle.Visibility = itemAd.DisplayTitle ? Visibility.Visible : Visibility.Collapsed;
				ImageBodyIcon.Visibility = itemAd.DisplayBodyIcon ? Visibility.Visible : Visibility.Collapsed;
				TextBlockAdvertisementPostScriptum.Visibility = itemAd.DisplayPostScriptum ? Visibility.Visible : Visibility.Collapsed;

				Logging.ToLog("MainWindow - Отображение сообщения: " + itemAd.Title + ", " +
					itemAd.Body + ", " + itemAd.PostScriptum);

				RaiseShowAdvertisementEvent();
			} catch (Exception exc) {
				Logging.ToLog("MainWindow - " + exc.Message + Environment.NewLine + exc.StackTrace);
			}
		}



		private void DataProvider_OnUpdateCompleted(object sender, EventArgs e) {
			if (!dataProvider.IsUpdateSuccessfull) {
				if (isErrorPageShowing)
					return;

				isErrorPageShowing = true;
				PageError pageError = new PageError();
				FrameChair.Navigate(pageError);
				return;
			}

			if (isErrorPageShowing) {
				isErrorPageShowing = false;
				if (FrameChair.CanGoBack)
					FrameChair.GoBack();
			}

			if (isChairPagesCreationCompleted)
				return;

			Logging.ToLog("MainWindow - Создание страниц для кресел");
			if (dataProvider.ChairsDict.Count == 0) {
				Logging.ToLog("MainWindow - Отсутствует информация о креслах");
				TextBlockRoom.Text = "Кабинет не выбран";
				return;
			} else {
				foreach (DataProvider.ItemChair itemChair in dataProvider.ChairsDict.Values) {
					PageChair pageChair = new PageChair(itemChair.ChID, itemChair.RNum, isLiveQueue, dataProvider);

					Border border = CreateIndicator();
					if (dataProvider.ChairsDict.Count > 1)
						StackPanelPageIndicator.Children.Add(border);

					chairPages.Add(pageChair, border);
				}

				if (chairPages.Count > 1) {
					DispatcherTimer timerChangePage = new DispatcherTimer();
					timerChangePage = new DispatcherTimer();
					timerChangePage.Interval = TimeSpan.FromSeconds(configuration.ChairPagesRotateIntervalInSeconds);
					timerChangePage.Tick += TimerChangePage_Tick;
					timerChangePage.Start();
				}

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
			Logging.ToLog("MainWindow - Прошлое значение: " + TextBlockRoom.Text + ", новое значение: " + pageToShow.RNum);
			TextBlockRoom.Text = "Кабинет " + pageToShow.RNum;
			chairPages[pageToShow].Background = Brushes.Gray;
			chairPages[pageToShow].Height = 10;
			FrameChair.Navigate(pageToShow);
		}


		
		private void DoubleAnimation_CurrentStateInvalidated_Start(object sender, EventArgs e) {
			BorderAdvertisementFirstPart.HorizontalAlignment = 
				BorderAdvertisementFirstPart.HorizontalAlignment == HorizontalAlignment.Left ?
				HorizontalAlignment.Right : HorizontalAlignment.Left;
		}

		private void DoubleAnimation_CurrentStateInvalidated_Second(object sender, EventArgs e) {
			BorderAdvertisementSecondPart.HorizontalAlignment =
				BorderAdvertisementSecondPart.HorizontalAlignment == HorizontalAlignment.Left ?
				HorizontalAlignment.Right : HorizontalAlignment.Left;
		}

		private void DoubleAnimation_CurrentStateInvalidated_Third(object sender, EventArgs e) {
			BorderAdvertisementThirdPart.HorizontalAlignment = 
				BorderAdvertisementThirdPart.HorizontalAlignment == HorizontalAlignment.Left ?
				HorizontalAlignment.Right : HorizontalAlignment.Left;
		}
	}
}
