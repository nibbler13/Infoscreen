using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace Infoscreen {
	public class Configuration : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public int DatabaseQueryExecutionIntervalInSeconds { get; set; }
		public int DoctorsPhotoUpdateIntervalInSeconds { get; set; }
		public int ChairPagesRotateIntervalInSeconds { get; set; }
		public int TimetableRotateIntervalInSeconds { get; set; }
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




		public static bool LoadConfiguration(string configFilePath, out Configuration configuration) {
			if (!File.Exists(configFilePath)) {
				configuration = new Configuration();
				return false;
			}

			try {
				FileStream fileStream = new FileStream(configFilePath, FileMode.Open);
				XmlSerializer xmlSerializer = new XmlSerializer(typeof(Configuration));
				xmlSerializer.UnknownAttribute += (s, e) => { Logging.ToLog(e.ExpectedAttributes); };
				xmlSerializer.UnknownElement += (s, e) => { Logging.ToLog(e.ExpectedElements); };
				xmlSerializer.UnknownNode += (s, e) => { Logging.ToLog(e.Name); };
				xmlSerializer.UnreferencedObject += (s, e) => { Logging.ToLog(e.UnreferencedId); };
				configuration = (Configuration)xmlSerializer.Deserialize(fileStream);
				configuration.IsConfigReadedSuccessfull = true;
				configuration.ConfigFilePath = configFilePath;

				return true;
			} catch (Exception e) {
				Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
				configuration = new Configuration();

				return false;
			}
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

		public string GetChairsId() {
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
