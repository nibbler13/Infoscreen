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
	public partial class PageConfigView : Page {
		private Infoscreen.FirebirdClient firebirdClient;
		private bool isAuthorized = false;

		private List<Infoscreen.Configuration.ItemSystem.ItemChair> chairItemsCache =
			new List<Infoscreen.Configuration.ItemSystem.ItemChair>();

		private Infoscreen.Configuration configuration;



		public PageConfigView(Infoscreen.Configuration configuration) {
			InitializeComponent();
			KeepAlive = true;
			this.configuration = configuration;
			DataContext = this;
			DataGridItemSystem.DataContext = configuration;
			Loaded += PageConfigView_Loaded;
			configuration.SystemItemsView.SortDescriptions.Add(new SortDescription("SystemName", ListSortDirection.Ascending));
		}



		private void PageConfigView_Loaded(object sender, RoutedEventArgs e) {
			UpdateChairItems();
			UpdateSystemItems();
			configuration.SystemItemsView.Refresh();
		}
		
		private async void UpdateChairItems() {
			firebirdClient = null;
			chairItemsCache.Clear();

			if (string.IsNullOrEmpty(configuration.DataBaseAddress) ||
				string.IsNullOrEmpty(configuration.DataBaseName)) {
				MessageBox.Show(Application.Current.MainWindow,
					"Не заданы параметры подключения к БД в разделе 'Внутренние настройки'", 
					"Ошибка конфигурации", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			firebirdClient = new Infoscreen.FirebirdClient(
				configuration.DataBaseAddress,
				configuration.DataBaseName,
				configuration.DataBaseUserName,
				configuration.DataBasePassword);

			if (!firebirdClient.IsDbAvailable())
				return;

			await Task.Run(() => { GetChairItems(); });
		}

		private void GetChairItems() {
			DataTable dataTable = firebirdClient.GetDataTable(
				Properties.Settings.Default.DataBaseSqlQueryGetChairs,
				new Dictionary<string, object>());

			if (dataTable.Rows.Count == 0) {
					MessageBox.Show(Window.GetWindow(this),
						"Не удалось получить список кресел. Проверьте подключение к БД в разделе 'Внутренние настройки'.",
						"Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			foreach (DataRow row in dataTable.Rows) {
				try {
					string chid = row["CHID"].ToString();
					string chname = row["CHNAME"].ToString();
					string rnum = row["RNUM"].ToString();
					string rname = row["RNAME"].ToString();

					Infoscreen.Configuration.ItemSystem.ItemChair chair = new Infoscreen.Configuration.ItemSystem.ItemChair() {
						ChairID = chid,
						ChairName = chname,
						RoomNumber = rnum,
						RoomName = rname
					};

					Application.Current.Dispatcher.BeginInvoke((Action)(() => { chairItemsCache.Add(chair); }));
				} catch (Exception exc) {
					MessageBox.Show(Application.Current.MainWindow, 
						exc.Message + Environment.NewLine + exc.StackTrace, "", 
						MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private async void UpdateSystemItems() {
			if (string.IsNullOrEmpty(configuration.ActiveDirectoryOU)) {
				MessageBox.Show(Application.Current.MainWindow, 
					"Не выбрано подразделение ActiveDirectory в разделе 'Внутренние настройки'", "Ошибка конфигурации",
					MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			List<Infoscreen.Configuration.ItemSystem> itemSystems = new List<Infoscreen.Configuration.ItemSystem>();
			await Task.Run(() => {
				string searchPath = "LDAP://" + configuration.ActiveDirectoryOU;

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

									Infoscreen.Configuration.ItemSystem itemSystem = new Infoscreen.Configuration.ItemSystem() {
										SystemName = name,
										SystemUnit = dn.Replace(configuration.ActiveDirectoryOU, "").
											Replace("CN=" + name + ",", "").
											TrimEnd(',').
											TrimStart(new char[] { 'O', 'U', '=' }).
											Replace(",OU=", ", ")
									};

									try {
										IEnumerable<Infoscreen.Configuration.ItemSystem> systemsInConfig =
											configuration.SystemItems.Where(
												x => x.SystemName.ToUpper().Equals(itemSystem.SystemName.ToUpper()));

										if (systemsInConfig.Count() == 1)
											itemSystem = systemsInConfig.First();

									} catch (Exception excInner) {
										Console.WriteLine(excInner.Message + Environment.NewLine + excInner.StackTrace);
									}
									
									itemSystems.Add(itemSystem);
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

			configuration.SystemItems.Clear();
			itemSystems.ForEach(configuration.SystemItems.Add);
		}

		private void ButtonEditChairs_Click(object sender, RoutedEventArgs e) {
			if (firebirdClient == null)
				firebirdClient = new Infoscreen.FirebirdClient(
					configuration.DataBaseAddress,
					configuration.DataBaseName,
					configuration.DataBaseUserName, 
					configuration.DataBasePassword);

			if (chairItemsCache.Count == 0)
				GetChairItems();

			if (chairItemsCache.Count == 0)
				return;

			Infoscreen.Configuration.ItemSystem itemSystem =
				(sender as Button).DataContext as Infoscreen.Configuration.ItemSystem;
			string systemName = itemSystem.SystemName;

			WindowEditSystemChairs windowAddOrEditSystem =
				new WindowEditSystemChairs(chairItemsCache, systemName, itemSystem.ChairItems);
			windowAddOrEditSystem.Owner = Window.GetWindow(this);
			windowAddOrEditSystem.ShowDialog();
		}

		private void ButtonSaveConfig_Click(object sender, RoutedEventArgs e) {
			string errorMsg = string.Empty;

			if (!string.IsNullOrEmpty(errorMsg)) {
				errorMsg = "Невозможно продолжить сохранение по следующим причинам:" + Environment.NewLine + errorMsg;
				MessageBox.Show(Window.GetWindow(this), errorMsg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			try {
				if (Infoscreen.Configuration.SaveConfiguration(configuration)) {
					MessageBox.Show(Window.GetWindow(this), "Конфигурация успешно сохранена в файл: " + configuration.ConfigFilePath,
						"", MessageBoxButton.OK, MessageBoxImage.Information);
					Infoscreen.Logging.ToLog("Сохранение настроек");
				}
				else
					MessageBox.Show(Window.GetWindow(this), "Возникла ошибка при сохранении конфигурации в файл: " + configuration.ConfigFilePath +
						". Подробности в журнале работы.", "", MessageBoxButton.OK, MessageBoxImage.Error);
			} catch (Exception exc) {
				Infoscreen.Logging.ToLog(exc.Message + Environment.NewLine + exc.StackTrace);
				MessageBox.Show(Window.GetWindow(this), "Возникла ошибка при сохранении конфигурации в файл: " + configuration.ConfigFilePath +
					". " + exc.Message + Environment.NewLine + exc.StackTrace, "", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void CheckBoxIsLiveQueue_Checked(object sender, RoutedEventArgs e) {
			if (!((sender as CheckBox).DataContext is Infoscreen.Configuration.ItemSystem itemSystem))
				return;

			itemSystem.IsTimetable = false;
		}

		private void CheckBoxIsTimetable_Checked(object sender, RoutedEventArgs e) {
			if (!((sender as CheckBox).DataContext is Infoscreen.Configuration.ItemSystem itemSystem))
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

		private void ButtonInternalSettings_Click(object sender, RoutedEventArgs e) {
			if (!isAuthorized) {
				WindowEnterPassword windowEnterPassword = new WindowEnterPassword();
				windowEnterPassword.Owner = Application.Current.MainWindow;
				bool? result = windowEnterPassword.ShowDialog();
				if ((result.HasValue && result.Value) == true)
					isAuthorized = true;
				else return;
			}

			PageInternalSettings pageInternalSettings = new PageInternalSettings(configuration);
			NavigationService.Navigate(pageInternalSettings);
		}
	}
}
