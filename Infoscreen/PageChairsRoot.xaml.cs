﻿using System;
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
	/// Interaction logic for PageChairsRoot.xaml
	/// </summary>
	public partial class PageChairsRoot : Page {
		public static readonly RoutedEvent ShowAdvertisementEvent =
			EventManager.RegisterRoutedEvent("ShowAdvertisement", RoutingStrategy.Bubble,
			typeof(RoutedEventHandler), typeof(PageChairsRoot));

		private bool isErrorPageShowing = false;

		public event RoutedEventHandler ShowAdvertisement {
			add { AddHandler(ShowAdvertisementEvent, value); }
			remove { RemoveHandler(ShowAdvertisementEvent, value); }
		}

		private readonly Configuration configuration;
		private readonly Advertisement advertisement;
		private readonly DataProvider dataProvider;


		public PageChairsRoot(Configuration configuration, Advertisement advertisement) {
			InitializeComponent();

			this.configuration = configuration;
			this.advertisement = advertisement;

			StartTimeDelimiterTick();

			FrameMain.Navigated += (s, e) => {
				if (!(e.Content is PageError))
					isErrorPageShowing = false;

				FrameMain.NavigationService.RemoveBackEntry();
			};

			if (!configuration.IsConfigReadedSuccessfull) {
				TextBlockTitle.Visibility = Visibility.Hidden;
				Logging.ToLog("MainWindow - Во время считывания настроек возникла ошибка, переход на страницу с ошибкой");
				ShowErrorPage();
				return;
			}

			dataProvider = new DataProvider(configuration);

			if (configuration.IsSystemWorkAsTimetable()) {
				TimetableHandler timetableHandler = new TimetableHandler(dataProvider, configuration.TimetableRotateIntervalInSeconds, this);
				timetableHandler.Start();
				return;
			}


			if (advertisement.DisableAdDisplay || advertisement.AdvertisementItemsToShow.Count == 0)
				Logging.ToLog("MainWindow - пропуск отображения рекламы в соответствии с настройками");
			else
				StartAdvertisement();

			ChairsHandler chairsHandler = new ChairsHandler(
				dataProvider,
				configuration.GetChairsIdForSystem(Environment.MachineName),
				configuration.IsSystemHasLiveQueue(),
				configuration.DatabaseQueryExecutionIntervalInSeconds,
				configuration.ChairPagesRotateIntervalInSeconds,
				configuration.DoctorsPhotoUpdateIntervalInSeconds,
				this);
			chairsHandler.Start();
		}




		private void StartTimeDelimiterTick() {
			DispatcherTimer timerTimeDilimeterTick = new DispatcherTimer {
				Interval = TimeSpan.FromSeconds(1)
			};

			timerTimeDilimeterTick.Tick += (s, ev) => {
				Application.Current.Dispatcher.Invoke((Action)delegate {
					TextBlockTimeSplitter.Visibility =
						TextBlockTimeSplitter.Visibility == Visibility.Visible ?
						Visibility.Hidden : Visibility.Visible;
					TextBlockTimeHours.Text = DateTime.Now.Hour.ToString();
					TextBlockTimeMinutes.Text = DateTime.Now.ToString("mm");
				});
			};
			timerTimeDilimeterTick.Start();
		}

		private void StartAdvertisement() {
			Logging.ToLog("MainWindow - Запуск таймера отображения рекламы");
			DispatcherTimer timerShowAdvertisement = new DispatcherTimer {
				Interval = TimeSpan.FromSeconds(25 + advertisement.PauseBetweenAdInSeconds)
			};

			timerShowAdvertisement.Tick += TimerShowAdvertisement_Tick;
			timerShowAdvertisement.Start();
			TimerShowAdvertisement_Tick(timerShowAdvertisement, new EventArgs());
		}





		public static Border CreateIndicator() {
			Border border = new Border {
				Margin = new Thickness(3, 0, 3, 0),
				Height = 5,
				Width = 30,
				Background = Brushes.LightGray,
				VerticalAlignment = VerticalAlignment.Center
			};

			return border;
		}

		public void SetupTitle(string title) {
			TextBlockTitle.Visibility = Visibility.Visible;
			TextBlockTitle.Text = title;
		}

		public void ShowErrorPage() {
			TextBlockTitle.Visibility = Visibility.Hidden;
			isErrorPageShowing = true;

			if (FrameMain.Content is PageError)
				return;

			FrameMain.Navigate(new PageError());
		}

		public void ClearPageIndicator() {
			StackPanelPageIndicator.Children.Clear();
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

			StackPanelAdvertisement.Visibility = Visibility.Visible;

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

			StackPanelAdvertisement.Visibility = Visibility.Hidden;
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
