using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Xml;

namespace Infoscreen {
    public class ConfigReader {
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
		public string Chairs { get; set; }
		public bool IsLiveQueue { get; set; }
		public bool IsTimetable { get; set; }
		public bool IsConfigReadedSuccessfull { get; set; }
		public bool ShowAdvertisement { get; set; }
		public int PauseBetweenAdvertisementsInSeconds { get; set; }

		public List<ItemAdvertisement> AdvertisementItems { get; set; } =
			new List<ItemAdvertisement>();

		private string ConfigFilePath { get; set; }
		public int CurrentAdvertisementIndex { get; set; }
		public string ActiveDirectoryOU { get; set; }

		public List<ItemSystem> SystemItems { get; set; } = 
			new List<ItemSystem>();




		public void InitializeConfig(string dataBaseUserName, string dataBaseUserPassword, string dataBaseSqlQuery) {
			ChairPagesRotateIntervalInSeconds = 15;
			TimetableRotateIntervalInSeconds = 30;
			DoctorsPhotoUpdateIntervalInSeconds = 3600;
			DatabaseQueryExecutionIntervalInSeconds = 15;
			DataBaseUserName = dataBaseUserName;
			DataBasePassword = dataBaseUserPassword;
			DataBaseQuery = dataBaseSqlQuery;
		}
		
		public void ReadConfigFile(string configFilePath, bool setTimerForUpdateAd = true) {
			Logging.ToLog("ConfigReader - Считывание настроек из файла: " + configFilePath);

			ConfigFilePath = configFilePath;

			if (!File.Exists(configFilePath)) {
				Logging.ToLog("ConfigReader - !Ошибка! Не удается найти файл");
				IsConfigReadedSuccessfull = false;
				return;
			}

			XmlDocument xmlDocument = LoadConfig();
			if (xmlDocument == null) {
				IsConfigReadedSuccessfull = false;
				return;
			}

			try {
				PhotosFolderPath = GetChildInnerText(xmlDocument, "/configuration/photos/folderPath");
				DatabaseQueryExecutionIntervalInSeconds = Convert.ToInt32(GetChildInnerText(xmlDocument, 
					"/configuration/database/updateIntervalInSeconds"));
				DoctorsPhotoUpdateIntervalInSeconds = Convert.ToInt32(GetChildInnerText(xmlDocument,
					"/configuration/photos/updateIntervalInSeconds"));
				ChairPagesRotateIntervalInSeconds = Convert.ToInt32(GetChildInnerText(xmlDocument,
					"/configuration/chairs/rotateIntervalInSeconds"));
				TimetableRotateIntervalInSeconds = Convert.ToInt32(GetChildInnerText(xmlDocument,
					"/configuration/timetable/rotateIntervalInSeconds"));
				DataBaseAddress = GetChildInnerText(xmlDocument, "/configuration/database/address");
				DataBaseName = GetChildInnerText(xmlDocument, "/configuration/database/name");
				DataBaseUserName = GetChildInnerText(xmlDocument, "/configuration/database/username");
				DataBasePassword = GetChildInnerText(xmlDocument, "/configuration/database/password");
				DataBaseQuery = GetChildInnerText(xmlDocument, "/configuration/database/query");
				ActiveDirectoryOU = GetChildInnerText(xmlDocument, "/configuration/computers/activeDirectoryOU");
			} catch (Exception e) {
				IsConfigReadedSuccessfull = false;
				Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
				return;
			}
			
			IsConfigReadedSuccessfull = true;

			if (!setTimerForUpdateAd) {
				XmlNodeList xmlNodeList = xmlDocument.SelectNodes("//computer");

				foreach (XmlNode xmlNode in xmlNodeList)
					SystemItems.Add(ReadSystemNode(xmlNode));

				DispatcherTimerAdvertisements_Tick(null, null);

				return;
			}

			string compName = Environment.MachineName.ToLower();
			XmlNode nodeComputer = xmlDocument.SelectSingleNode("//computer[@name='" + compName + "']");
			if (nodeComputer != null) {
				ItemSystem itemSystem = ReadSystemNode(nodeComputer);
				Chairs = string.Join(",", itemSystem.ChairItems.Select(x => x.ChairID).ToArray());
				IsLiveQueue = itemSystem.IsLiveQueue;
			} else
				Logging.ToLog("ConfigReader - Не удалось найти элемент для текущей системы " + compName);

			DispatcherTimer dispatcherTimerAdvertisements = new DispatcherTimer();
			dispatcherTimerAdvertisements.Interval = TimeSpan.FromMinutes(10);
			dispatcherTimerAdvertisements.Tick += DispatcherTimerAdvertisements_Tick;
			dispatcherTimerAdvertisements.Start();
			DispatcherTimerAdvertisements_Tick(null, null);
		}

		private static ItemSystem ReadSystemNode(XmlNode xmlNode) {
			ItemSystem itemSystem = new ItemSystem();

			try {
				itemSystem.SystemName = xmlNode.Attributes["name"].Value;

				XmlNode nodeChairs = xmlNode["chairs"];
				if (nodeChairs != null) {
					string[] chairs = nodeChairs.InnerText.Split('@');
					foreach (string chair in chairs) {
						string[] chairDetails = chair.Split('|');
						if (chairDetails.Length != 4) {
							Console.WriteLine("Длина строки с креслом не равна 4 элементам, пропуск: " + chair);
							continue;
						}

						ItemChair itemChair = new ItemChair() {
							ChairID = chairDetails[0],
							ChairName = chairDetails[1],
							RoomNumber = chairDetails[2],
							RoomName = chairDetails[3]
						};

						itemSystem.ChairItems.Add(itemChair);
					}
				}

				XmlNode nodeLiveQueue = xmlNode["liveQueue"];
				if (nodeLiveQueue != null)
					itemSystem.IsLiveQueue = nodeLiveQueue.InnerText.Equals("1");

				XmlNode nodeTimetable = xmlNode["timetable"];
				if (nodeTimetable != null)
					itemSystem.IsTimetable = nodeTimetable.InnerXml.Equals("1");
			} catch (Exception e) {
				Console.WriteLine(e.Message + Environment.NewLine + e.StackTrace);
			}

			return itemSystem;
		}

		private XmlDocument LoadConfig() {
			XmlDocument xmlDocument = new XmlDocument();

			try {
				xmlDocument.Load(ConfigFilePath);
			} catch (Exception e) {
				Logging.ToLog("ConfigReader - " + e.Message + Environment.NewLine + e.StackTrace);
				IsConfigReadedSuccessfull = false;
				return null;
			}

			if (!xmlDocument.HasChildNodes) {
				Logging.ToLog("ConfigReader - Отсутствуют дочерние элементы");
				IsConfigReadedSuccessfull = false;
				return null;
			}

			return xmlDocument;
		}

		private void DispatcherTimerAdvertisements_Tick(object sender, EventArgs e) {
			Logging.ToLog("ConfigReader - " + "Считывание информации о рекламных сообщениях");

			AdvertisementItems.Clear();

			XmlDocument xmlDocument = LoadConfig();
			if (xmlDocument == null)
				return;

			try {
				ShowAdvertisement = GetChildInnerText(xmlDocument, "/configuration/advertisements/main/showAdvertisements").Equals("1");
				PauseBetweenAdvertisementsInSeconds = Convert.ToInt32(GetChildInnerText(xmlDocument,
					"/configuration/advertisements/main/pauseBetweenAdvertisementsInSeconds"));
			} catch (Exception exc) {
				Logging.ToLog("ConfigReader - " + exc.Message + Environment.NewLine + exc.StackTrace);
				return;
			}

			XmlNodeList nodesAdvertisement = xmlDocument.SelectNodes("/configuration/advertisements/advertisement");
			if (nodesAdvertisement == null) {
				Logging.ToLog("ConfigReader - " + "В конфигурацицонном файле отсутствует информация об акциях");
				return;
			}

			for (int i = 0; i < nodesAdvertisement.Count; i++) {
				try {
					string start = GetChildInnerText(xmlDocument, "/configuration/advertisements/advertisement[" + (i + 1) + "]/start");

					if (!start.Equals("0")) {
						DateTime.TryParse(start, out DateTime stopShowingDate);
						if (stopShowingDate != new DateTime()) {
							if (DateTime.Now.Date < stopShowingDate.Date) {
								Logging.ToLog("Пропуск добавления, т.к. срок начала отображения еще не наступил");
								continue;
							}
						}
					}

					string stop = GetChildInnerText(xmlDocument, "/configuration/advertisements/advertisement[" + (i + 1) + "]/stop");

					if (!stop.Equals("0")) {
						DateTime.TryParse(stop, out DateTime stopShowingDate);
						if (stopShowingDate != new DateTime()) {
							if (DateTime.Now.Date > stopShowingDate.Date) {
								Logging.ToLog("Пропуск добавления, т.к. вышел срок отображения сообщения");
								continue;
							}
						}
					}

					string title = GetChildInnerText(xmlDocument, "/configuration/advertisements/advertisement[" + (i + 1) + "]/title");
					string body = GetChildInnerText(xmlDocument, "/configuration/advertisements/advertisement[" + (i + 1) + "]/body");
					string postScriptum = GetChildInnerText(xmlDocument, "/configuration/advertisements/advertisement[" + (i + 1) + "]/postScriptum");

					ItemAdvertisement itemAdvertisement = new ItemAdvertisement() {
						Title = title,
						Body = body,
						PostScriptum = postScriptum
					};

					AdvertisementItems.Add(itemAdvertisement);
				} catch (Exception exc) {
					Logging.ToLog("ConfigReader - " + exc.Message + Environment.NewLine + exc.StackTrace);
				}
			}

			if (AdvertisementItems.Count == 0)
				return;

			AdvertisementItems = AdvertisementItems.OrderBy(t => t.OrderNumber).ToList();
			CurrentAdvertisementIndex = 0;
		}

		private string GetChildInnerText(XmlDocument xmlDocument, string xPath) {
			Logging.ToLog("ConfigReader - Считывание значения для элемента: " + xPath);
			XmlNode xmlNode = xmlDocument.SelectSingleNode(xPath);

			if (xmlNode == null) {
				Logging.ToLog("ConfigReader - Не удалось прочитать элемент");
				throw new KeyNotFoundException();
			}

			return xmlNode.InnerText;
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
		}

		public class ItemChair {
			public string ChairID { get; set; } = string.Empty;
			public string ChairName { get; set; } = string.Empty;
			public string RoomNumber { get; set; } = string.Empty;
			public string RoomName { get; set; } = string.Empty;
		}

		public class ItemAdvertisement {
			public string Title { get; set; } = string.Empty;
			public string Body { get; set; } = string.Empty;
			public string PostScriptum { get; set; } = string.Empty;
			public int OrderNumber { get; set; }

			public ItemAdvertisement() {
				Random random = new Random();
				OrderNumber = random.Next(0, 1000);
			}
		}
    }
}
