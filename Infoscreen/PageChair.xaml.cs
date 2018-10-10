using System;
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
    /// <summary>
    /// Логика взаимодействия для PageCabin.xaml
    /// </summary>
    public partial class PageChair : Page {
		private string ChID { get; set; }
		public string RNum { get; private set; }
		private bool isLiveQueue;
		private DataProvider dataProvider;
		
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
					ShowReceptionIsConducted(statusInfo);
					break;
				case DataProvider.ItemChair.Status.NotConducted:
				default:
					ShowReceptionIsNotConducted();
					break;
			}
		}

		private void ShowReceptionIsNotConducted() {
			GridReceptionIsConducted.Visibility = Visibility.Hidden;
			GridReceptionIsNotConducted.Visibility = Visibility.Visible;
		}

		private void ShowReceptionIsConducted(DataProvider.ItemChair.StatusInfo statusInfo) {
			TextBlockNoEmployee.Visibility = statusInfo.employees.Count == 0 ? Visibility.Visible : Visibility.Hidden;
			StackPanelDoc.Visibility = statusInfo.employees.Count == 1 ? Visibility.Visible : Visibility.Hidden;
			StackPanelMultipleEmployees.Visibility = statusInfo.employees.Count > 1 ? Visibility.Visible : Visibility.Hidden;

			if (statusInfo.employees.Count == 1) {
				DataProvider.ItemChair.Employee employee = statusInfo.employees[0];
				TextBlockDepartment.Text = employee.Department;
				TextBlockEmployeeName.Text = employee.Name;
				TextBlockEmployeePosition.Text = employee.Position;
				TextBlockWorkingTime.Text = "Приём ведётся с " + employee.WorkingTime;
				ImageEmployee.Source = DataProvider.GetImageForDoctor(employee.Name);
			} else if (statusInfo.employees.Count > 1) {
				StackPanelMultipleEmployees.Children.Clear();
				TextBlockDepartment.Text = string.Empty;

				foreach (DataProvider.ItemChair.Employee employee in statusInfo.employees) {
					if (!TextBlockDepartment.Text.Contains(employee.Department))
						TextBlockDepartment.Text += employee.Department + ", ";

					TextBlock textBlockDocName = new TextBlock();
					textBlockDocName.Text = employee.Name;
					textBlockDocName.TextWrapping = TextWrapping.Wrap;
					textBlockDocName.FontFamily = new FontFamily("Franklin Gothic Book");
					textBlockDocName.HorizontalAlignment = HorizontalAlignment.Center;


					TextBlock textBlockDocPosition = new TextBlock();
					textBlockDocPosition.Text = employee.Position;
					textBlockDocPosition.TextWrapping = TextWrapping.Wrap;
					textBlockDocPosition.FontFamily = new FontFamily("Franklin Gothic Book");
					textBlockDocPosition.Foreground = new SolidColorBrush(Colors.Gray);
					textBlockDocPosition.HorizontalAlignment = HorizontalAlignment.Center;
					textBlockDocPosition.FontSize = 30;

					TextBlock textBlockWorkingTime = new TextBlock();
					textBlockWorkingTime.Text = "Приём ведётся с " + employee.WorkingTime;
					textBlockWorkingTime.TextWrapping = TextWrapping.Wrap;
					textBlockWorkingTime.FontFamily = new FontFamily("Franklin Gothic Book");
					textBlockWorkingTime.FontSize = 30;
					textBlockWorkingTime.Foreground = new SolidColorBrush(Colors.Gray);
					textBlockWorkingTime.HorizontalAlignment = HorizontalAlignment.Center;

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
