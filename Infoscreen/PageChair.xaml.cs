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

			ItemChair.StatusInfo statusInfo = dataProvider.ChairsDict[ChID].CurrentState;

			switch (statusInfo.Status) {
				case ItemChair.Status.Free:
				case ItemChair.Status.Invitation:
				case ItemChair.Status.Underway:
				case ItemChair.Status.Delayed:
					ShowReceptionIsConducted(statusInfo);
					break;
				case ItemChair.Status.NotConducted:
				default:
					ShowReceptionIsNotConducted();
					break;
			}
		}

		private void ShowReceptionIsNotConducted() {
			GridReceptionIsConducted.Visibility = Visibility.Hidden;
			GridReceptionIsNotConducted.Visibility = Visibility.Visible;
		}

		private void ShowReceptionIsConducted(ItemChair.StatusInfo statusInfo) {
			TextBlockNoEmployee.Visibility = statusInfo.employees.Count == 0 ? Visibility.Visible : Visibility.Hidden;
			StackPanelDoc.Visibility = statusInfo.employees.Count == 1 ? Visibility.Visible : Visibility.Hidden;
			StackPanelMultipleEmployees.Visibility = statusInfo.employees.Count > 1 ? Visibility.Visible : Visibility.Hidden;

			if (statusInfo.employees.Count == 1) {
				ItemChair.Employee employee = statusInfo.employees[0];
				TextBlockDepartment.Text = employee.Department;
				TextBlockEmployeeName.Text = employee.Name;
				TextBlockEmployeePosition.Text = employee.Position;
				TextBlockWorkingTime.Text = "Приём ведётся с " + employee.WorkingTime;
				string photo = dataProvider.GetImageForDoctor(employee.Name);

				if (string.IsNullOrEmpty(photo)) {
					var logo = new BitmapImage();
					logo.BeginInit();
					logo.UriSource = new Uri("pack://application:,,,/Infoscreen;component/Images/DoctorWithoutAPhoto.png");
					logo.EndInit();
					ImageEmployee.Source = logo;
				} else {
					ImageEmployee.Source = new BitmapImage(new Uri(photo));
				}
			} else if (statusInfo.employees.Count > 1) {
				StackPanelMultipleEmployees.Children.Clear();
				TextBlockDepartment.Text = string.Empty;

				foreach (ItemChair.Employee employee in statusInfo.employees) {
					TextBlockDepartment.Text += employee.Department + ", ";

					TextBlock textBlockDoc = new TextBlock();
					textBlockDoc.Text = employee.Name + " - " + employee.Position;
					textBlockDoc.TextWrapping = TextWrapping.Wrap;
					textBlockDoc.FontFamily = new FontFamily("Franklin Gothic Book");
					textBlockDoc.HorizontalAlignment = HorizontalAlignment.Center;


					TextBlock textBlockWorkingTime = new TextBlock();
					textBlockWorkingTime.Text = "Приём ведётся с " + employee.WorkingTime;
					textBlockWorkingTime.TextWrapping = TextWrapping.Wrap;
					textBlockWorkingTime.FontFamily = new FontFamily("Franklin Gothic Book");
					textBlockWorkingTime.FontSize = 40;
					textBlockWorkingTime.Foreground = new SolidColorBrush(Colors.Gray);
					textBlockWorkingTime.HorizontalAlignment = HorizontalAlignment.Center;

					if (!employee.Equals(statusInfo.employees.Last()))
						textBlockWorkingTime.Margin = new Thickness(0, 0, 0, 10);

					StackPanelMultipleEmployees.Children.Add(textBlockDoc);
					StackPanelMultipleEmployees.Children.Add(textBlockWorkingTime);
				}

				TextBlockDepartment.Text = TextBlockDepartment.Text.TrimEnd(new char[] { ',', ' ' });
			}

			string state = string.Empty;

			if (isLiveQueue)
				state = "Приём ведётся в порядке живой очереди";
			else
				switch (statusInfo.Status) {
					case ItemChair.Status.Free:
						state = "Кабинет свободен";
						break;
					case ItemChair.Status.Invitation:
						state = statusInfo.PatientToInviteName + "," + Environment.NewLine +
							"Пройдите на приём";
						break;
					case ItemChair.Status.Underway:
						state = "Идет приём";
						if (!string.IsNullOrEmpty(statusInfo.ReceptionTimeLeft))
							state += Environment.NewLine +
								"Осталось " + statusInfo.ReceptionTimeLeft + " минут";
						break;
					case ItemChair.Status.Delayed:
						state = "Приём задерживается";
						break;
					case ItemChair.Status.NotConducted:
					default:
						break;
				}

			TextBlockState.Text = state;
			
			GridReceptionIsNotConducted.Visibility = Visibility.Hidden;
			GridReceptionIsConducted.Visibility = Visibility.Visible;
		}
	}
}
