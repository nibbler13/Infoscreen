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
	class TimetableHandler {
		DataProvider dataProvider;
		MainWindow mainWindow;
		int updateIntervalInSeconds;

		public TimetableHandler(DataProvider dataProvider, int updateIntervalInSeconds) {
			this.dataProvider = dataProvider;
			this.updateIntervalInSeconds = updateIntervalInSeconds;
			mainWindow = Application.Current.MainWindow as MainWindow;
		}

		public void Start() {
			DispatcherTimer timerUpdateTimetable = new DispatcherTimer {
				Interval = TimeSpan.FromSeconds(updateIntervalInSeconds)
			};

			timerUpdateTimetable.Tick += TimerUpdateTimetable_Tick;
			timerUpdateTimetable.Start();
			TimerUpdateTimetable_Tick(timerUpdateTimetable, new EventArgs());
		}

		private async void TimerUpdateTimetable_Tick(object sender, EventArgs e) {
			Logging.ToLog("TimetableHandler - Обновление расписания");
			mainWindow.ClearPageIndicator();
			DataProvider.Timetable timetableInitial = dataProvider.GetTimeTable();

			if (timetableInitial.departments.Count == 0) {
				Logging.ToLog("TimetableHandler - не удалось получить информацию о расписании");
				mainWindow.ShowErrorPage();
				return;
			}

			mainWindow.SetupTitle("Расписание работы сотрудников");

			if (!(sender is DispatcherTimer dispatcherTimer))
				return;

			dispatcherTimer.Stop();

			DataProvider.Timetable timetableToShow = new DataProvider.Timetable();
			Dictionary<PageTimetable, Border> pagesTimetable = new Dictionary<PageTimetable, Border>();
			int row = 0;

			Logging.ToLog("MainWindow - Создание страниц расписания");
			try {
				foreach (KeyValuePair<string, DataProvider.Timetable.Department> departmentPair in timetableInitial.departments) {
					if (row >= 12) {
						pagesTimetable.Add(new PageTimetable(timetableToShow), MainWindow.CreateIndicator());
						row = 0;
						timetableToShow.departments.Clear();
					}

					timetableToShow.departments.Add(departmentPair.Key, new DataProvider.Timetable.Department());
					row++;

					foreach (KeyValuePair<string, DataProvider.Timetable.DocInfo> docInfoPair in departmentPair.Value.doctors) {
						timetableToShow.departments[departmentPair.Key].doctors.Add(docInfoPair.Key, docInfoPair.Value);
						row++;

						if (row == 13) {
							pagesTimetable.Add(new PageTimetable(timetableToShow), MainWindow.CreateIndicator());
							row = 0;
							timetableToShow.departments.Clear();

							if (!docInfoPair.Equals(departmentPair.Value.doctors.Last())) {
								timetableToShow.departments.Add(departmentPair.Key, new DataProvider.Timetable.Department());
								row++;
							}
						}
					}
				}
			} catch (Exception exc) {
				Logging.ToLog(exc.Message + Environment.NewLine + exc.StackTrace);
				return;
			}

			if (pagesTimetable.Count > 1)
				foreach (Border border in pagesTimetable.Values)
					mainWindow.StackPanelPageIndicator.Children.Add(border);

			Logging.ToLog("MainWindow - Отображение страниц расписания");
			int currentPageIndex = 0;
			try {
				foreach (KeyValuePair<PageTimetable, Border> pagePair in pagesTimetable) {
					pagesTimetable.Values.ToList()[currentPageIndex].Background = Brushes.Gray;
					pagesTimetable.Values.ToList()[currentPageIndex].Height = 10;

					mainWindow.FrameMain.Navigate(pagePair.Key);
					await PutTaskDelay();

					pagesTimetable.Values.ToList()[currentPageIndex].Background = Brushes.LightGray;
					pagesTimetable.Values.ToList()[currentPageIndex].Height = 5;
					currentPageIndex++;

					if (currentPageIndex == pagesTimetable.Count)
						currentPageIndex = 0;
				}
			} catch (Exception exc) {
				Logging.ToLog(exc.Message + Environment.NewLine + exc.StackTrace);
			}

			dispatcherTimer.Start();
		}
		
		private async Task PutTaskDelay() {
			await Task.Delay(TimeSpan.FromSeconds(updateIntervalInSeconds));
		}
	}
}
