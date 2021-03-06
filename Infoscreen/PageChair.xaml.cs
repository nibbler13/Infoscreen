﻿using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace Infoscreen
{
    public partial class PageChair : Page {
		private string ChID { get; set; }
		public string RNum { get; private set; }
		private bool isLiveQueue;
		private DataProvider dataProvider;
		public bool IsReceptionConducted { get; private set; } = false;
		
        public PageChair(string chid, string rnum, bool isLiveQueue, DataProvider dataProvider) {
			Logging.ToLog("PageChair - Создание страницы кресла, chid - " + chid + ", rnum - " + rnum);
            InitializeComponent();

			ChID = chid;
			RNum = rnum;
			this.isLiveQueue = isLiveQueue;
			this.dataProvider = dataProvider;
			
			dataProvider.OnUpdateCompleted += DataProvider_OnUpdateCompleted;
			UpdateState();
        }

		private void DataProvider_OnUpdateCompleted(object sender, EventArgs e) {
			Logging.ToLog("PageChair - Обновление состояния, chid - " + ChID);
			UpdateState();
		}

		private void UpdateState() {
			try {
				if (!dataProvider.ChairsDict.ContainsKey(ChID)) {
					Logging.ToLog("PageChair - отсутствует chid " + ChID + " в результате запроса DataProvider, пропуск обновления");
					return;
				}

				DataProvider.ItemChair.StatusInfo statusInfo = dataProvider.ChairsDict[ChID].CurrentState;

				switch (statusInfo.Status) {
					case DataProvider.ItemChair.Status.Free:
					case DataProvider.ItemChair.Status.Invitation:
					case DataProvider.ItemChair.Status.Underway:
					case DataProvider.ItemChair.Status.Delayed:
						IsReceptionConducted = true;
						ShowReceptionIsConducted(statusInfo);
						break;
					case DataProvider.ItemChair.Status.NotConducted:
					default:
						IsReceptionConducted = false;
						ShowReceptionIsNotConducted();
						break;
				}
			} catch (Exception e) {
				Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
			}
		}

		private void ShowReceptionIsNotConducted() {
			GridReceptionIsConducted.Visibility = Visibility.Hidden;
			GridReceptionIsNotConducted.Visibility = Visibility.Visible;
		}

		private void ShowReceptionIsConducted(DataProvider.ItemChair.StatusInfo statusInfo) {
			TextBlockNoEmployee.Visibility = statusInfo.employees.Count == 0 ? Visibility.Visible : Visibility.Hidden;
			StackPanelDoc.Visibility = statusInfo.employees.Count == 1 ? Visibility.Visible : Visibility.Hidden;
			//StackPanelMultipleEmployees.Visibility = statusInfo.employees.Count > 1 ? Visibility.Visible : Visibility.Hidden;
			ViewBoxMultipleEmployees.Visibility = statusInfo.employees.Count > 1 ? Visibility.Visible : Visibility.Hidden;

			if (statusInfo.employees.Count == 1) {
				DataProvider.ItemChair.Employee employee = statusInfo.employees[0];
				TextBlockDepartment.Text = employee.Department;
				TextBlockEmployeeName.Text = employee.Name;
				TextBlockEmployeePosition.Text = employee.Position;
				TextBlockWorkingTime.Text = "Приём ведётся с " + DataProvider.ClearTimeString(employee.WorkingTime);
				ImageEmployee.Source = DataProvider.GetImageForDoctor(employee.Name);
			} else if (statusInfo.employees.Count > 1) {
				StackPanelMultipleEmployees.Children.Clear();
				TextBlockDepartment.Text = string.Empty;

				foreach (DataProvider.ItemChair.Employee employee in statusInfo.employees) {
					if (!TextBlockDepartment.Text.Contains(employee.Department))
						TextBlockDepartment.Text += employee.Department + ", ";

					TextBlock textBlockDocName = new TextBlock {
						Text = employee.Name,
						TextWrapping = TextWrapping.Wrap,
						FontFamily = new FontFamily("Franklin Gothic Book"),
						HorizontalAlignment = HorizontalAlignment.Center
					};

					TextBlock textBlockDocPosition = new TextBlock {
						Text = employee.Position,
						TextWrapping = TextWrapping.Wrap,
						FontFamily = new FontFamily("Franklin Gothic Book"),
						Foreground = new SolidColorBrush(Colors.Gray),
						HorizontalAlignment = HorizontalAlignment.Center,
						FontSize = 30
					};

					TextBlock textBlockWorkingTime = new TextBlock {
						Text = "Приём ведётся с " + employee.WorkingTime,
						TextWrapping = TextWrapping.Wrap,
						FontFamily = new FontFamily("Franklin Gothic Book"),
						FontSize = 30,
						Foreground = new SolidColorBrush(Colors.Gray),
						HorizontalAlignment = HorizontalAlignment.Center
					};

					if (!employee.Equals(statusInfo.employees.Last()))
						textBlockWorkingTime.Margin = new Thickness(0, 0, 0, 10);

					StackPanelMultipleEmployees.Children.Add(textBlockDocName);
					StackPanelMultipleEmployees.Children.Add(textBlockDocPosition);
					StackPanelMultipleEmployees.Children.Add(textBlockWorkingTime);
				}

				TextBlockDepartment.Text = TextBlockDepartment.Text.TrimEnd(new char[] { ',', ' ' });
			}

			string state = string.Empty;

			if (isLiveQueue)
				state = "Приём ведётся в порядке живой очереди";
			else
				switch (statusInfo.Status) {
					case DataProvider.ItemChair.Status.Free:
						state = "Кабинет свободен";
						break;
					case DataProvider.ItemChair.Status.Invitation:
						state = statusInfo.PatientToInviteName + "," + Environment.NewLine +
							"Пройдите на приём";
						break;
					case DataProvider.ItemChair.Status.Underway:
						state = "Идет приём";
						if (!string.IsNullOrEmpty(statusInfo.ReceptionTimeLeft))
							state += Environment.NewLine +
								"Осталось " + statusInfo.ReceptionTimeLeft + " минут";
						break;
					case DataProvider.ItemChair.Status.Delayed:
						state = "Приём задерживается";
						break;
					case DataProvider.ItemChair.Status.NotConducted:
					default:
						break;
				}

			TextBlockState.Text = state;
			
			GridReceptionIsNotConducted.Visibility = Visibility.Hidden;
			GridReceptionIsConducted.Visibility = Visibility.Visible;
		}
	}
}
