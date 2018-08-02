using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Xml;

namespace Infoscreen {
    public static class ConfigReader {
		static public int DatabaseQueryExecutionIntervalInSeconds { get; private set; }
		static public int DoctorsPhotoUpdateIntervalInSeconds { get; private set; }
		static public int ChairPagesRotateIntervalInSeconds { get; private set; }
		static public int TimetableRotateIntervalInSeconds { get; private set; }
		static public string PhotosFolderPath { get; private set; }
		static public string DataBaseCopyAddress { get; private set; }
		static public string DataBaseAddress { get; private set; }
		static public string DataBaseName { get; private set; }
		static public string DataBaseUserName { get; private set; }
		static public string DataBasePassword { get; private set; }
		static public string DataBaseQuery { get; private set; }
		static public string Chairs { get; private set; }
		static public bool IsLiveQueue { get; private set; }
		static public bool IsTimetable { get; private set; }
		static public bool IsConfigReadedSuccessfull { get; private set; }
		static public bool ShowAdvertisement { get; private set; }
		static public int PauseBetweenAdvertisementsInSeconds { get; private set; }
		static public List<ItemAdvertisement> AdvertisementItems { get; private set; } = new List<ItemAdvertisement>();
		static private string ConfigFilePath { get; set; }
		static public int CurrentAdvertisementIndex { get; set; }
		static public string ActiveDirectoryOU { get; private set; }
		static public List<ItemSystem> SystemItems { get; private set; } = new List<ItemSystem>();
		
		static public void ReadConfigFile(string configFilePath, bool setTimerForUpdateAd = true) {
			Logging.ToLog("ConfigReader - Считывание настроек из файла: " + configFilePath);

			ConfigFilePath = configFilePath;

			if (!File.Exists(configFilePath)) {
				Logging.ToLog("ConfigReader - !Ошибка! Не удается найти файл");
				IsConfigReadedSuccessfull = false;
				return;
			}

			XmlDocument xmlDocument = GetConfig();
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

				return;
			}

			string compName = Environment.MachineName.ToLower();
			XmlNode nodeComputer = xmlDocument.SelectSingleNode("//computer[@name='" + compName + "']");
			if (nodeComputer != null) {
				ItemSystem itemSystem = ReadSystemNode(nodeComputer);
				Chairs = itemSystem.Chairs;
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
				if (nodeChairs != null)
					itemSystem.Chairs = nodeChairs.InnerText;

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

		private static XmlDocument GetConfig() {
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

		private static void DispatcherTimerAdvertisements_Tick(object sender, EventArgs e) {
			Logging.ToLog("ConfigReader - " + "Считывание информации о рекламных сообщениях");

			AdvertisementItems.Clear();

			XmlDocument xmlDocument = GetConfig();
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

		static private string GetChildInnerText(XmlDocument xmlDocument, string xPath) {
			Logging.ToLog("ConfigReader - Считывание значения для элемента: " + xPath);
			XmlNode xmlNode = xmlDocument.SelectSingleNode(xPath);

			if (xmlNode == null) {
				Logging.ToLog("ConfigReader - Не удалось прочитать элемент");
				throw new KeyNotFoundException();
			}

			return xmlNode.InnerText;
		}

		public class ItemSystem {
			public string SystemName { get; set; } = string.Empty;
			public string Chairs { get; set; } = string.Empty;
			public bool IsLiveQueue { get; set; } = false;
			public bool IsTimetable { get; set; } = false;
		}

		public class ItemAdvertisement {
			public string Title { get; set; } = string.Empty;
			public string Body { get; set; } = string.Empty;
			public string PostScriptum { get; set; } = string.Empty;
			public int OrderNumber { get; private set; }

			public ItemAdvertisement() {
				Random random = new Random();
				OrderNumber = random.Next(0, 1000);
			}
		}
    }
}
