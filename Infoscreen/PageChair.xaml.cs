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
		private bool? isReceptionConducted = null;
		
        public PageChair(string chid, string rnum) {
            InitializeComponent();

			ChID = chid;
			RNum = rnum;
			
			DataProvider.OnUpdateCompleted += DataProvider_OnUpdateCompleted;
			UpdateState();
        }

		private void DataProvider_OnUpdateCompleted(object sender, EventArgs e) {
			UpdateState();
		}

		private void UpdateState() {
			if (!DataProvider.ChairsDict.ContainsKey(ChID))
				return;

			ItemChair.StatusInfo statusInfo = DataProvider.ChairsDict[ChID].CurrentState;

			if (statusInfo.Status == ItemChair.Status.NotConducted &&
				(isReceptionConducted.HasValue && !isReceptionConducted.Value))
				return;

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
			isReceptionConducted = false;
		}

		private void ShowReceptionIsConducted(ItemChair.StatusInfo statusInfo) {
			if (statusInfo.employees.Count > 0) {
				ItemChair.Employee employee = statusInfo.employees[0];
				TextBlockDepartment.Text = employee.Department;
				//TextBlockEmployeeName.Text = employee.Position + ": " + employee.Name;
				TextBlockEmployeeName.Text = employee.Name;
				TextBlockEmployeePosition.Text = employee.Position;
				TextBlockWorkingTime.Text = "Приём ведётся с " + employee.WorkingTime;
				string photo = DataProvider.GetImageForDoctor(employee.Name);
				if (string.IsNullOrEmpty(photo)) {
					var logo = new BitmapImage();
					logo.BeginInit();
					logo.UriSource = new Uri("pack://application:,,,/Infoscreen;component/Images/DoctorWithoutAPhoto.png");
					logo.EndInit();
					ImageEmployee.Source = logo;
				} else {
					ImageEmployee.Source = new BitmapImage(new Uri(photo));
				}
			}

			string state = string.Empty;
			Color color = Color.FromRgb(0, 169, 220);

			switch (statusInfo.Status) {
				case ItemChair.Status.Free:
					state = "Кабинет свободен";
					color = Color.FromRgb(78, 155, 68);
					break;
				case ItemChair.Status.Invitation:
					state = statusInfo.PatientToInviteName + "," + Environment.NewLine +
						"Пройдите на приём";
					color = Color.FromRgb(249, 141, 60);
					break;
				case ItemChair.Status.Underway:
					state = "Идет приём";
					if (!string.IsNullOrEmpty(statusInfo.ReceptionTimeLeft))
						state += Environment.NewLine +
							"Осталось " + statusInfo.ReceptionTimeLeft + " минут";
					break;
				case ItemChair.Status.Delayed:
					state = "Приём задерживается";
					color = Color.FromRgb(140, 95, 168);
					break;
				case ItemChair.Status.NotConducted:
				default:
					break;
			}

			BorderDepartment.Background = new SolidColorBrush(color);

			TextBlockState.Text = state;

			if (isReceptionConducted.HasValue && isReceptionConducted.Value)
				return;

			isReceptionConducted = true;
			GridReceptionIsNotConducted.Visibility = Visibility.Hidden;
			GridReceptionIsConducted.Visibility = Visibility.Visible;
		}
	}
}
