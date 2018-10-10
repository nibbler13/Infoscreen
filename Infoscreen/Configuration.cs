using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Xml.Serialization;

namespace Infoscreen {
	public class Configuration : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private int databaseQueryExecutionIntervalInSeconds;
		public int DatabaseQueryExecutionIntervalInSeconds {
			get { return databaseQueryExecutionIntervalInSeconds; }
			set {
				if (value != databaseQueryExecutionIntervalInSeconds) {
					databaseQueryExecutionIntervalInSeconds = value;
					NotifyPropertyChanged();
				}
			}
		}

		private int doctorsPhotoUpdateIntervalInSeconds;
		public int DoctorsPhotoUpdateIntervalInSeconds {
			get { return doctorsPhotoUpdateIntervalInSeconds; }
			set {
				if (value != doctorsPhotoUpdateIntervalInSeconds) {
					doctorsPhotoUpdateIntervalInSeconds = value;
					NotifyPropertyChanged();
				}
			}
		}

		private int chairPagesRotateIntervalInSeconds;
		public int ChairPagesRotateIntervalInSeconds {
			get { return chairPagesRotateIntervalInSeconds; }
			set {
				if (value != chairPagesRotateIntervalInSeconds) {
					chairPagesRotateIntervalInSeconds = value;
					NotifyPropertyChanged();
				}
			}
		}

		private int timetableRotateIntervalInSeconds;
		public int TimetableRotateIntervalInSeconds {
			get { return timetableRotateIntervalInSeconds; }
			set {
				if (value != timetableRotateIntervalInSeconds) {
					timetableRotateIntervalInSeconds = value;
					NotifyPropertyChanged();
				}
			}
		}


		public string PhotosFolderPath { get; set; }
		public string DataBaseCopyAddress { get; set; }
		public string DataBaseAddress { get; set; }
		public string DataBaseName { get; set; }
		public string DataBaseUserName { get; set; }
		public string DataBasePassword { get; set; }
		public string DataBaseQuery { get; set; }
		public string DataBaseQueryTimetable { get; set; }
		public bool IsConfigReadedSuccessfull { get; set; } = false;

		private string configFilePath;
		public string ConfigFilePath {
			get { return configFilePath; }
			set {
				if (value != configFilePath) {
					configFilePath = value;
					NotifyPropertyChanged();
				}
			}
		}

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
		
		public ObservableCollection<ItemSystem> SystemItems { get; set; } =
			new ObservableCollection<ItemSystem>();
		
		public ICollectionView SystemItemsView {
			get { return CollectionViewSource.GetDefaultView(SystemItems); }
		}

		public Configuration() {
			ChairPagesRotateIntervalInSeconds = 15;
			TimetableRotateIntervalInSeconds = 30;
			DoctorsPhotoUpdateIntervalInSeconds = 3600;
			DatabaseQueryExecutionIntervalInSeconds = 15;
			DataBaseUserName = "sysdba";
			DataBasePassword = "masterkey";
			DataBaseQuery = Properties.Resources.SqlQueryMain;
			DataBaseQueryTimetable = Properties.Resources.SqlQueryTimetable;
			ConfigFilePath = Logging.ASSEMBLY_DIRECTORY + "InfoscreenConfig.xml";
		}

		public bool IsSystemHasLiveQueue() {
			try {
				return SystemItems.Where(x => x.SystemName.ToLower().Equals(Environment.MachineName.ToLower())).First().IsLiveQueue;
			} catch (Exception exc) {
				Logging.ToLog("MainWindow - " + exc.Message + Environment.NewLine + exc.StackTrace);
			}

			return false;
		}

		public bool IsSystemWorkAsTimetable() {
			try {
				return SystemItems.Where(x => x.SystemName.ToLower().Equals(Environment.MachineName.ToLower())).First().IsTimetable;
			} catch (Exception exc) {
				Logging.ToLog("MainWindow - " + exc.Message + Environment.NewLine + exc.StackTrace);
			}

			return false;
		}


		public static bool LoadConfiguration(string configFilePath, out Configuration configuration) {
			Logging.ToLog("Configuration - Считывание файла настроек: " + configFilePath);
			configuration = new Configuration();

			if (!File.Exists(configFilePath))
				return false;

			try {
				using (FileStream fileStream = new FileStream(configFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
					XmlSerializer xmlSerializer = new XmlSerializer(typeof(Configuration));
					xmlSerializer.UnknownAttribute += (s, e) => { Logging.ToLog(e.ExpectedAttributes); };
					xmlSerializer.UnknownElement += (s, e) => { Logging.ToLog(e.ExpectedElements); };
					xmlSerializer.UnknownNode += (s, e) => { Logging.ToLog(e.Name); };
					xmlSerializer.UnreferencedObject += (s, e) => { Logging.ToLog(e.UnreferencedId); };
					configuration = (Configuration)xmlSerializer.Deserialize(fileStream);
					configuration.IsConfigReadedSuccessfull = true;
					configuration.ConfigFilePath = configFilePath;

					return true;
				}
			} catch (Exception e) {
				Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
			}

			return false;
		}

		public static bool SaveConfiguration(Configuration configuration) {
			try {
				XmlSerializer xmlSerializer = new XmlSerializer(typeof(Configuration));
				TextWriter textWriter = new StreamWriter(configuration.ConfigFilePath);
				xmlSerializer.Serialize(textWriter, configuration);
				textWriter.Close();

				return true;
			} catch (Exception e) {
				Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);

				return false;
			}
		}

		public string GetChairsIdForSystem(string systemName) {
			foreach (ItemSystem item in SystemItems) {
				if (systemName.ToLower().Equals(item.SystemName.ToLower())) {
					if (item.ChairItems.Count == 0)
						return string.Empty;
					else
						return string.Join(",", item.ChairItems.Select(x => x.ChairID));
				}
			}

			return string.Empty;
		}

		public class ItemSystem : INotifyPropertyChanged {
			public event PropertyChangedEventHandler PropertyChanged;
			private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
			
			public string SystemName { get; set; } = string.Empty;
			public string SystemUnit { get; set; } = string.Empty;

			private bool isLiveQueue = false;
			public bool IsLiveQueue {
				get { return isLiveQueue; }
				set {
					if (value != isLiveQueue) {
						isLiveQueue = value;
						NotifyPropertyChanged();
					}
				}
			}

			private bool isTimetable = false;
			public bool IsTimetable {
				get { return isTimetable; }
				set {
					if (value != isTimetable) {
						isTimetable = value;
						NotifyPropertyChanged();
					}
				}
			}

			public ObservableCollection<ItemChair> ChairItems { get; set; } = 
				new ObservableCollection<ItemChair>();

			public class ItemChair {
				public string ChairID { get; set; } = string.Empty;
				public string ChairName { get; set; } = string.Empty;
				public string RoomNumber { get; set; } = string.Empty;
				public string RoomName { get; set; } = string.Empty;
			}
		}
    }
}
