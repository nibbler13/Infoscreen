using Microsoft.Win32;
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
using Microsoft.WindowsAPICodePack.Dialogs;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace InfoscreenConfigManager {
	/// <summary>
	/// Логика взаимодействия для PageConfigView.xaml
	/// </summary>
	public partial class PageConfigView : Page, INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public string ChairsRotateIntervalInSeconds { get; set; }
		public string TimetableRotateIntervalInSeconds { get; set; }

		private string photosFolderPath;
		public string PhotosFolderPath {
			get { return photosFolderPath; }
			set {
				if (value != photosFolderPath) {
					photosFolderPath = value;
					NotifyPropertyChanged();
				}
			}
		}

		public string PhotosUpdateIntervalInSeconds { get; set; }
		public string DatabaseAddress { get; set; }
		public string DatabaseName { get; set; }
		public string DatabaseUpdateIntervalInSeconds { get; set; }

		public ObservableCollection<Infoscreen.ConfigReader.ItemSystem> SystemItems { get; set; } =
			new ObservableCollection<Infoscreen.ConfigReader.ItemSystem>();

		private static readonly Regex regexDigitOnly = new Regex("[^0-9]");

		private string activeDirectoryOU;
		public string ActiveDirectoryOU {
			get { return activeDirectoryOU; }
			set {
				if (value != activeDirectoryOU) {
					activeDirectoryOU = value;
					NotifyPropertyChanged();
				}
			}
		}

		public PageConfigView(string configFileFullPath) {
			InitializeComponent();
			
			DataGridItemSystem.DataContext = this;
			
			if (Infoscreen.ConfigReader.IsConfigReadedSuccessfull) {
				ChairsRotateIntervalInSeconds = Infoscreen.ConfigReader.ChairPagesRotateIntervalInSeconds.ToString();
				TimetableRotateIntervalInSeconds = Infoscreen.ConfigReader.TimetableRotateIntervalInSeconds.ToString();
				PhotosFolderPath = Infoscreen.ConfigReader.PhotosFolderPath;
				PhotosUpdateIntervalInSeconds = Infoscreen.ConfigReader.DoctorsPhotoUpdateIntervalInSeconds.ToString();
				DatabaseAddress = Infoscreen.ConfigReader.DataBaseAddress;
				DatabaseName = Infoscreen.ConfigReader.DataBaseName;
				DatabaseUpdateIntervalInSeconds = Infoscreen.ConfigReader.DatabaseQueryExecutionIntervalInSeconds.ToString();
				ActiveDirectoryOU = Infoscreen.ConfigReader.ActiveDirectoryOU;
				Infoscreen.ConfigReader.SystemItems.ForEach(SystemItems.Add);
			} else {
				ChairsRotateIntervalInSeconds = "15";
				TimetableRotateIntervalInSeconds = "30";
				PhotosUpdateIntervalInSeconds = "3600";
				DatabaseUpdateIntervalInSeconds = "15";
			}

			DataContext = this;
		}

		private void ButtonSelectPhotosFolder_Click(object sender, RoutedEventArgs e) {
			using (CommonOpenFileDialog openFileDialog = new CommonOpenFileDialog()) {
				openFileDialog.IsFolderPicker = true;

				if (!string.IsNullOrEmpty(PhotosFolderPath))
					openFileDialog.InitialDirectory = PhotosFolderPath;

				if (openFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
					PhotosFolderPath = openFileDialog.FileName;
			}
		}

		private void ButtonSelectActiveDirectoryOU_Click(object sender, RoutedEventArgs e) {
			WindowSelectAdOu windowSelectAdOu = new WindowSelectAdOu(ActiveDirectoryOU);
			windowSelectAdOu.Owner = Window.GetWindow(this);

			if (windowSelectAdOu.ShowDialog() == true) 
				ActiveDirectoryOU = windowSelectAdOu.SelectedOU;
		}

		private void ButtonAddSystem_Click(object sender, RoutedEventArgs e) {

		}

		private void ButtonDeleteSystem_Click(object sender, RoutedEventArgs e) {

		}

		private void ButtonEditSystem_Click(object sender, RoutedEventArgs e) {

		}

		private void ButtonSaveConfig_Click(object sender, RoutedEventArgs e) {

		}

		private void ButtonCheckDBConnect_Click(object sender, RoutedEventArgs e) {

		}

		private void DataGridItemSystem_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			ButtonDeleteSystem.IsEnabled = DataGridItemSystem.SelectedItems.Count > 0;
			ButtonEditSystem.IsEnabled = DataGridItemSystem.SelectedItems.Count == 1;
		}

		private static bool IsTextAllowed(Key key) {
			return key >= Key.D0 && key <= Key.D9 || key == Key.Delete || key == Key.Back;
		}

		private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e) {
			e.Handled = !IsTextAllowed(e.Key);
		}
	}
}
