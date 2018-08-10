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

		public event RoutedEventHandler ShowAdvertisement {
			add { AddHandler(ShowAdvertisementEvent, value); }
			remove { RemoveHandler(ShowAdvertisementEvent, value); }
		}

		private string configFilePath;
		private ConfigReader configReader;
		private DataProvider dataProvider;


		public MainWindow(string configFilePath) {
			Logging.ToLog("MainWindow - Создание основного окна приложения");

			InitializeComponent();
			this.configFilePath = configFilePath;

			PreviewKeyDown += (s, e) => {
				if (e.Key.Equals(Key.Escape)) {
					Logging.ToLog("MainWindow - ===== Завершение работы по нажатию клавиши ESC");
					Application.Current.Shutdown();
				}
			};

			Loaded += MainWindow_Loaded;
		}

		private async void MainWindow_Loaded(object sender, RoutedEventArgs e) {
			await Task.Run(() => {
				configReader = new ConfigReader();
				configReader.ReadConfigFile(configFilePath);
			});

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

			if (!configReader.IsConfigReadedSuccessfull) {
				TextBlockRoom.Visibility = Visibility.Hidden;
				Logging.ToLog("MainWindow - Во время считывания настроек возникла ошибка, переход на страницу с ошибкой");
				FrameChair.Navigate(new PageError());
				return;
			}

			if (string.IsNullOrEmpty(configReader.Chairs)) {
				TextBlockRoom.Text = "Кабинет не выбран";
				Logging.ToLog("MainWindow - Не заполнен список кабинок");
				return;
			}

			dataProvider = new DataProvider(configReader);

			Logging.ToLog("MainWindow - Запуск таймера обновления данных");
			DispatcherTimer timerUpdateData = new DispatcherTimer();
			timerUpdateData.Interval = TimeSpan.FromSeconds(configReader.DatabaseQueryExecutionIntervalInSeconds);
			timerUpdateData.Tick += (s, ev) => { dataProvider.UpdateData(); };
			timerUpdateData.Start();

			Logging.ToLog("MainWindow - Запуск таймера обновления фотографий");
			DispatcherTimer timerUpdatePhotos = new DispatcherTimer();
			timerUpdatePhotos.Interval = TimeSpan.FromSeconds(configReader.DoctorsPhotoUpdateIntervalInSeconds);
			timerUpdatePhotos.Tick += (s, ev) => { dataProvider.UpdateDoctorsPhoto(); };
			timerUpdatePhotos.Start();

			Logging.ToLog("MainWindow - Запуск таймера отображения рекламы");
			DispatcherTimer timerShowAdvertisement = new DispatcherTimer();
			timerShowAdvertisement.Interval = TimeSpan.FromSeconds(25 + configReader.PauseBetweenAdvertisementsInSeconds);
			timerShowAdvertisement.Tick += TimerShowAdvertisement_Tick;
			timerShowAdvertisement.Start();
			TimerShowAdvertisement_Tick(null, null);

			dataProvider.OnUpdateCompleted += DataProvider_OnUpdateCompleted;
			dataProvider.UpdateData();
			dataProvider.UpdateDoctorsPhoto();
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

			if (!configReader.ShowAdvertisement) {
				Logging.ToLog("MainWindow - пропуск отображения в соответствии с настройками");
				return;
			}

			if (isErrorPageShowing) {
				Logging.ToLog("MainWindow - пропуск отображения, т.к. в данный момент отображается сообщение об ошибке в работе");
				return;
			}

			if (configReader.AdvertisementItems.Count == 0) {
				Logging.ToLog("MainWindow - пропуск отображения, т.к. отсутствуют доступные сообщения");
				return;
			}

			try {
				ConfigReader.ItemAdvertisement advertisement = configReader.AdvertisementItems[configReader.CurrentAdvertisementIndex];
				configReader.CurrentAdvertisementIndex++;
				if (configReader.CurrentAdvertisementIndex == configReader.AdvertisementItems.Count)
					configReader.CurrentAdvertisementIndex = 0;

				TextBlockAdvertisementTitle.Text = advertisement.Title;
				TextBlockAdvertisementBody.Text = advertisement.Body;
				TextBlockAdvertisementPostScriptum.Text = advertisement.PostScriptum;

				DocPanelAdvertisementTitle.Visibility = string.IsNullOrEmpty(advertisement.Title) ? Visibility.Collapsed : Visibility.Visible;
				ImageBodyIcon.Visibility = string.IsNullOrEmpty(advertisement.Title) ? Visibility.Visible : Visibility.Collapsed;
				TextBlockAdvertisementBody.Visibility = string.IsNullOrEmpty(advertisement.Body) ? Visibility.Collapsed : Visibility.Visible;
				TextBlockAdvertisementPostScriptum.Visibility = string.IsNullOrEmpty(advertisement.PostScriptum) ? Visibility.Collapsed : Visibility.Visible;

				Logging.ToLog("MainWindow - Отображение сообщения: " + advertisement.Title + ", " +
					advertisement.Body + ", " + advertisement.PostScriptum);

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
				foreach (ItemChair itemChair in dataProvider.ChairsDict.Values) {
					PageChair pageChair = new PageChair(itemChair.ChID, itemChair.RNum, configReader.IsLiveQueue, dataProvider);
					Border border = new Border {
						Margin = new Thickness(3,0,3,0),
						Height = 5,
						Width = 30,
						Background = Brushes.LightGray,
						VerticalAlignment = VerticalAlignment.Center
					};

					if (dataProvider.ChairsDict.Count > 1)
						StackPanelPageIndicator.Children.Add(border);

					chairPages.Add(pageChair, border);
				}

				if (chairPages.Count > 1) {
					DispatcherTimer timerChangePage = new DispatcherTimer();
					timerChangePage = new DispatcherTimer();
					timerChangePage.Interval = TimeSpan.FromSeconds(configReader.ChairPagesRotateIntervalInSeconds);
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
