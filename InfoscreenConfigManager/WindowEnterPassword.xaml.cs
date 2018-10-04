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
using System.Windows.Shapes;

namespace InfoscreenConfigManager {
	/// <summary>
	/// Логика взаимодействия для WindowEnterPassword.xaml
	/// </summary>
	public partial class WindowEnterPassword : Window {
		public WindowEnterPassword() {
			InitializeComponent();
			PasswordBoxMain.Focus();
		}

		private void Button_Click(object sender, RoutedEventArgs e) {
			if (PasswordBoxMain.Password.Equals("idadmin")) {
				DialogResult = true;
				Close();
				return;
			}

			MessageBox.Show(this, "Пароль введен неверно", "", MessageBoxButton.OK, MessageBoxImage.Warning);
		}

		private void PasswordBoxMain_KeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Enter)
				Button_Click(null, null);
		}
	}
}
