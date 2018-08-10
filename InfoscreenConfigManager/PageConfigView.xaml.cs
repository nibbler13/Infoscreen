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
using System.DirectoryServices;
using System.Data;
using System.Xml.Serialization;
using System.IO;

namespace InfoscreenConfigManager {
	/// <summary>
	/// Логика взаимодействия для PageConfigView.xaml
	/// </summary>
	public partial class PageConfigView : Page, INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;
		private Infoscreen.FirebirdClient firebirdClient;

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

		private string databaseAddress;
		public string DatabaseAddress {
			get { return databaseAddress; }
			set {
				if (value != databaseAddress) {
					databaseAddress = value;
					UpdateFirebirdClientInstance();
				}
			}
		}

		private string databaseName;
		public string DatabaseName {
			get { return databaseName; }
			set {
				if (value != databaseName) {
					databaseName = value;
					UpdateFirebirdClientInstance();
				}
			}
		}

		private readonly string QUERY_GET_CHAIRS = 
			Properties.Settings.Default.DataBaseSqlQueryGetChairs;

		private List<Infoscreen.ConfigReader.ItemChair> chairItems =
			new List<Infoscreen.ConfigReader.ItemChair>();

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

		private Infoscreen.ConfigReader configReader;





		public PageConfigView(Infoscreen.ConfigReader configReader) {
			InitializeComponent();

			this.configReader = configReader;
			
			DataGridItemSystem.DataContext = this;

			ChairsRotateIntervalInSeconds = configReader.ChairPagesRotateIntervalInSeconds.ToString();
			TimetableRotateIntervalInSeconds = configReader.TimetableRotateIntervalInSeconds.ToString();
			PhotosFolderPath = configReader.PhotosFolderPath;
			PhotosUpdateIntervalInSeconds = configReader.DoctorsPhotoUpdateIntervalInSeconds.ToString();
			DatabaseAddress = configReader.DataBaseAddress;
			DatabaseName = configReader.DataBaseName;
			DatabaseUpdateIntervalInSeconds = configReader.DatabaseQueryExecutionIntervalInSeconds.ToString();
			ActiveDirectoryOU = configReader.ActiveDirectoryOU;

			DataContext = this;

			Loaded += PageConfigView_Loaded;
		}





		private async void UpdateFirebirdClientInstance() {
			firebirdClient = null;
			chairItems.Clear();

			if (string.IsNullOrEmpty(DatabaseAddress) ||
				string.IsNullOrEmpty(DatabaseName))
				return;

			firebirdClient = new Infoscreen.FirebirdClient(
				DatabaseAddress,
				DatabaseName,
				configReader.DataBaseUserName,
				configReader.DataBasePassword);

			if (!firebirdClient.IsDbAvailable())
				return;

			await Task.Run(() => { GetChairItems(); });
		}

		private static bool IsTextAllowed(Key key) {
			return key >= Key.D0 && key <= Key.D9 || key == Key.Delete || key == Key.Back;
		}

		private void PageConfigView_Loaded(object sender, RoutedEventArgs e) {
			UpdateSystemItems();
		}



		private async void UpdateSystemItems() {
			SystemItems.Clear();

			if (string.IsNullOrEmpty(ActiveDirectoryOU))
				return;

			await Task.Run(() => {
				string searchPath = "LDAP://" + ActiveDirectoryOU;

				try {
					using (DirectoryEntry entry = new DirectoryEntry(searchPath)) {
						using (DirectorySearcher mySearcher = new DirectorySearcher(entry)) {
							mySearcher.SearchScope = SearchScope.Subtree;
							mySearcher.Filter = ("(objectClass=Computer)");
							mySearcher.SizeLimit = int.MaxValue;
							mySearcher.PageSize = int.MaxValue;

							foreach (SearchResult resEnt in mySearcher.FindAll()) {
								try {
									string name = resEnt.Properties["name"][0].ToString();
									string dn = resEnt.Properties["distinguishedName"][0].ToString();

									Infoscreen.ConfigReader.ItemSystem itemSystem = new Infoscreen.ConfigReader.ItemSystem() {
										SystemName = name,
										SystemUnit = dn.Replace(ActiveDirectoryOU, "").
													Replace("CN=" + name + ",", "").
													TrimEnd(',').
													TrimStart(new char[] { 'O', 'U', '=' }).
													Replace(",OU=", ", ")
									};

									try {
										IEnumerable<Infoscreen.ConfigReader.ItemSystem> systemsInConfig =
											configReader.SystemItems.Where(
												x => x.SystemName.ToUpper().Equals(itemSystem.SystemName.ToUpper()));

										if (systemsInConfig.Count() == 1) {
											Infoscreen.ConfigReader.ItemSystem itemSystemCR = systemsInConfig.First();
											itemSystem.ChairItems = itemSystemCR.ChairItems;
											itemSystem.IsLiveQueue = itemSystemCR.IsLiveQueue;
											itemSystem.IsTimetable = itemSystemCR.IsTimetable;
										}

									} catch (Exception excInner) {
										Console.WriteLine(excInner.Message + Environment.NewLine + excInner.StackTrace);
									}

									Application.Current.Dispatcher.BeginInvoke((Action)(() => { SystemItems.Add(itemSystem); }));
								} catch (Exception exc) {
									Console.WriteLine(exc.Message + Environment.NewLine + exc.StackTrace);
								}
							}
						}
					}
				} catch (Exception exceptionLdap) {
					Console.WriteLine(exceptionLdap.Message + Environment.NewLine + exceptionLdap.StackTrace);
				}
			});
		}

		private void GetChairItems(bool showWarning = false) {
			DataTable dataTable = firebirdClient.GetDataTable(QUERY_GET_CHAIRS, new Dictionary<string, object>());

			if (dataTable.Rows.Count == 0) {
				if (showWarning)
					MessageBox.Show(Window.GetWindow(this), "Не удалось получить список кресел. Проверьте подключение к БД.", "Ошибка БД",
						MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			foreach (DataRow row in dataTable.Rows) {
				try {
					string chid = row["CHID"].ToString();
					string chname = row["CHNAME"].ToString();
					string rnum = row["RNUM"].ToString();
					string rname = row["RNAME"].ToString();

					Infoscreen.ConfigReader.ItemChair chair = new Infoscreen.ConfigReader.ItemChair() {
						ChairID = chid,
						ChairName = chname,
						RoomNumber = rnum,
						RoomName = rname
					};

					Application.Current.Dispatcher.BeginInvoke((Action)(() => { chairItems.Add(chair); }));
				} catch (Exception exc) {
					Console.WriteLine(exc.Message + Environment.NewLine + exc.StackTrace);
				}
			}
		}



		private void ButtonCheckDBConnect_Click(object sender, RoutedEventArgs e) {
			if (firebirdClient == null)
				firebirdClient = new Infoscreen.FirebirdClient(
					DatabaseAddress,
					DatabaseName,
					configReader.DataBaseUserName,
					configReader.DataBasePassword);

			if (firebirdClient.IsDbAvailable())
				MessageBox.Show(Window.GetWindow(this), "Соединение выполнено успешно", "",
					MessageBoxButton.OK, MessageBoxImage.Information);
			else
				MessageBox.Show(Window.GetWindow(this), "Не удалось выполнить тестовый запрос. " +
					"Подробности можно узнать в журнале работы, расположенном в папке с программой.", "",
					MessageBoxButton.OK, MessageBoxImage.Error);
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
			if (!string.IsNullOrEmpty(ActiveDirectoryOU)) {
				if (MessageBox.Show(Window.GetWindow(this), "При смене подразделения ActiveDirectory будет заменен весь текущий " +
					"список соответствия кресел. Продолжить?", "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
					return;
			}

			WindowSelectAdOu windowSelectAdOu = new WindowSelectAdOu(ActiveDirectoryOU);
			windowSelectAdOu.Owner = Window.GetWindow(this);

			if (windowSelectAdOu.ShowDialog() == true) {
				ActiveDirectoryOU = windowSelectAdOu.SelectedOU;
				UpdateSystemItems();
			}
		}

		private void ButtonEditChairs_Click(object sender, RoutedEventArgs e) {
			if (firebirdClient == null)
				firebirdClient = new Infoscreen.FirebirdClient(DatabaseAddress, DatabaseName,
					configReader.DataBaseUserName, configReader.DataBasePassword);

			if (chairItems.Count == 0)
				GetChairItems(true);

			if (chairItems.Count == 0)
				return;

			Infoscreen.ConfigReader.ItemSystem itemSystem =
				(sender as Button).DataContext as Infoscreen.ConfigReader.ItemSystem;
			string systemName = itemSystem.SystemName;

			WindowEditSystemChairs windowAddOrEditSystem =
				new WindowEditSystemChairs(chairItems, systemName, itemSystem.ChairItems);
			windowAddOrEditSystem.Owner = Window.GetWindow(this);
			windowAddOrEditSystem.ShowDialog();
		}

		private void ButtonSaveConfig_Click(object sender, RoutedEventArgs e) {
			string errorMsg = string.Empty;

			if (string.IsNullOrEmpty(ChairsRotateIntervalInSeconds) ||
				string.IsNullOrWhiteSpace(ChairsRotateIntervalInSeconds))
				errorMsg += Environment.NewLine + "Поле 'таймеры - переключение страниц кресел' не может быть пустым";

			if (string.IsNullOrEmpty(TimetableRotateIntervalInSeconds) ||
				string.IsNullOrWhiteSpace(TimetableRotateIntervalInSeconds))
				errorMsg += Environment.NewLine + "Поле 'таймеры - переключение страниц расписания' не может быть пустым";


			if (string.IsNullOrEmpty(DatabaseUpdateIntervalInSeconds) ||
				string.IsNullOrWhiteSpace(DatabaseUpdateIntervalInSeconds))
				errorMsg += Environment.NewLine + "Поле 'таймеры - обновление данных' не может быть пустым";


			if (string.IsNullOrEmpty(PhotosUpdateIntervalInSeconds) ||
				string.IsNullOrWhiteSpace(PhotosUpdateIntervalInSeconds))
				errorMsg += Environment.NewLine + "Поле 'таймеры - обновление фотографий' не может быть пустым";


			if (string.IsNullOrEmpty(DatabaseAddress) ||
				string.IsNullOrWhiteSpace(DatabaseAddress))
				errorMsg += Environment.NewLine + "Поле 'подключение к БД МИС Инфоклиника - адрес' не может быть пустым";


			if (string.IsNullOrEmpty(DatabaseName) ||
				string.IsNullOrWhiteSpace(DatabaseName))
				errorMsg += Environment.NewLine + "Поле 'подключение к БД МИС Инфоклиника - имя базы' не может быть пустым";


			if (firebirdClient != null && !firebirdClient.IsDbAvailable())
				errorMsg += Environment.NewLine + "Не удается подключиться к БД, используя текущие параметры";


			if (string.IsNullOrEmpty(ActiveDirectoryOU) ||
				string.IsNullOrWhiteSpace(ActiveDirectoryOU))
				errorMsg += Environment.NewLine + "Поле 'соответствие кресел - подразделение в ActiveDirectory' не может быть пустым";

			if (!string.IsNullOrEmpty(errorMsg)) {
				errorMsg = "Невозможно продолжить сохранение по следующим причинам:" + Environment.NewLine + errorMsg;
				MessageBox.Show(Window.GetWindow(this), errorMsg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			XmlSerializer xmlSerializer = new XmlSerializer(typeof(Infoscreen.ConfigReader));
			TextWriter textWriter = new StreamWriter(Infoscreen.Logging.ASSEMBLY_DIRECTORY + "test.xml");
			xmlSerializer.Serialize(textWriter, configReader);
			textWriter.Close();
		}



		private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e) {
			e.Handled = !IsTextAllowed(e.Key);
		}



		private void CheckBoxIsLiveQueue_Checked(object sender, RoutedEventArgs e) {
			if (!((sender as CheckBox).DataContext is Infoscreen.ConfigReader.ItemSystem itemSystem))
				return;

			itemSystem.IsTimetable = false;
		}

		private void CheckBoxIsTimetable_Checked(object sender, RoutedEventArgs e) {
			if (!((sender as CheckBox).DataContext is Infoscreen.ConfigReader.ItemSystem itemSystem))
				return;

			if (itemSystem.ChairItems.Count == 0) {
				itemSystem.IsLiveQueue = false;
				return;
			}

			if (MessageBox.Show(Window.GetWindow(this), "Установка данной опции приведет к обнулению списка выбранных кресел. Продолжить?", "",
				MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) {
				itemSystem.IsTimetable = false;
				return;
			}

			itemSystem.ChairItems.Clear();
			itemSystem.IsLiveQueue = false;
		}
	}
}
