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
		private Dictionary<PageChair, Border> chairPages = new Dictionary<PageChair, Border>();
		private int currentPageIndex = 0;

		public MainWindow() {
			InitializeComponent();

			PreviewKeyDown += (s, e) => {
				if (e.Key.Equals(Key.Escape))
					Application.Current.Shutdown();
			};

			DispatcherTimer timerSeconds = new DispatcherTimer();
			timerSeconds.Interval = TimeSpan.FromSeconds(1);
			timerSeconds.Tick += (s, e) => {
				Application.Current.Dispatcher.Invoke((Action)delegate {
					TextBlockTimeSplitter.Visibility = 
						TextBlockTimeSplitter.Visibility == Visibility.Visible ? 
						Visibility.Hidden : Visibility.Visible;
					TextBlockTimeHours.Text = DateTime.Now.Hour.ToString();
					TextBlockTimeMinutes.Text = DateTime.Now.ToString("mm");
				});
			};
			timerSeconds.Start();

			DispatcherTimer timerUpdateData = new DispatcherTimer();
			timerUpdateData.Interval = TimeSpan.FromSeconds(10);
			timerUpdateData.Tick += (s, e) => { DataProvider.UpdateData(); };
			timerUpdateData.Start();

			DispatcherTimer timerUpdatePhotos = new DispatcherTimer();
			timerUpdatePhotos.Interval = TimeSpan.FromHours(1);
			timerUpdatePhotos.Tick += (s, e) => { DataProvider.UpdateDoctorsPhoto(); };
			timerUpdatePhotos.Start();

			DataProvider.OnUpdateCompleted += DataProvider_OnUpdateCompleted;
			DataProvider.UpdateData();
			DataProvider.UpdateDoctorsPhoto();
		}

		private void DataProvider_OnUpdateCompleted(object sender, EventArgs e) {
			if (DataProvider.ChairsDict.Count == 0) {

			} else {
				//int columnIndex = 0;
				foreach (ItemChair itemChair in DataProvider.ChairsDict.Values) {
					//GridPageIndicator.ColumnDefinitions.Add(new ColumnDefinition());
					PageChair pageChair = new PageChair(itemChair.ChID, itemChair.RNum);
					Border border = new Border {
						Margin = new Thickness(3,0,3,0),
						Height = 5,
						Width = 30,
						Background = Brushes.LightGray,
						VerticalAlignment = VerticalAlignment.Center
					};
					//Grid.SetColumn(border, columnIndex);
					//columnIndex++;

					if (DataProvider.ChairsDict.Count > 1)
						StackPanelPageIndicator.Children.Add(border);

					chairPages.Add(pageChair, border);
				}

				if (chairPages.Count > 1) {
					DispatcherTimer timerChangePage = new DispatcherTimer();
					timerChangePage = new DispatcherTimer {
						Interval = TimeSpan.FromSeconds(7)
					};
					timerChangePage.Tick += TimerChangePage_Tick;
					timerChangePage.Start();
				}

				NavigateToPage();
			}

			DataProvider.OnUpdateCompleted -= DataProvider_OnUpdateCompleted;
		}

		private void TimerChangePage_Tick(object sender, EventArgs e) {
			chairPages.Values.ToList()[currentPageIndex].Background = Brushes.LightGray;
			chairPages.Values.ToList()[currentPageIndex].Height = 5;
			currentPageIndex++;

			if (currentPageIndex == chairPages.Count)
				currentPageIndex = 0;

			NavigateToPage();
		}

		private void NavigateToPage() {
			PageChair pageToShow = chairPages.Keys.ToList()[currentPageIndex];
			TextBlockRoom.Text = "Кабинет " + pageToShow.RNum;
			chairPages[pageToShow].Background = Brushes.Gray;
			chairPages[pageToShow].Height = 10;
			FrameChair.Navigate(pageToShow);
		}
	}
}
