using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Xml;

namespace Infoscreen {
    static class ConfigReader {
		static public int DatabaseQueryExecutionIntervalInSeconds { get; private set; }
		static public int DoctorsPhotoUpdateIntervalInSeconds { get; private set; }
		static public int ChairPagesRotateIntervalInSeconds { get; private set; }
		static public string PhotosFolderPath { get; private set; }
		static public string DataBaseCopyAddress { get; private set; }
		static public string DataBaseAddress { get; private set; }
		static public string DataBaseName { get; private set; }
		static public string DataBaseUserName { get; private set; }
		static public string DataBasePassword { get; private set; }
		static public string DataBaseQuery { get; private set; }
		static public string Chairs { get; private set; }
		static public bool LiveQueue { get; private set; }
		static public bool ConfigReadedSuccessfull { get; private set; }
		static public bool ShowAdvertisement { get; private set; }
		static public int PauseBetweenAdvertisementsInSeconds { get; private set; }
		static public List<ItemAdvertisement> Advertisements { get; private set; } = new List<ItemAdvertisement>();
		static private string ConfigFilePath { get; set; }
		
		static public void ReadConfigFile(string configFilePath) {
			Logging.ToLog("ConfigReader - Считывание настроек из файла: " + configFilePath);

			ConfigFilePath = configFilePath;

			if (!File.Exists(configFilePath)) {
				Logging.ToLog("ConfigReader - !Ошибка! Не удается найти файл");
				ConfigReadedSuccessfull = false;
				return;
			}

			XmlDocument xmlDocument = GetConfig();
			if (xmlDocument == null) {
				ConfigReadedSuccessfull = false;
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
				DataBaseAddress = GetChildInnerText(xmlDocument, "/configuration/database/address");
				DataBaseName = GetChildInnerText(xmlDocument, "/configuration/database/name");
				DataBaseUserName = GetChildInnerText(xmlDocument, "/configuration/database/username");
				DataBasePassword = GetChildInnerText(xmlDocument, "/configuration/database/password");
				DataBaseQuery = GetChildInnerText(xmlDocument, "/configuration/database/query");
			} catch (Exception e) {
				ConfigReadedSuccessfull = false;
				Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
				return;
			}

			string compName = Environment.MachineName.ToLower();
			XmlNode nodeComputer = xmlDocument.SelectSingleNode("//computer[@name='" + compName + "']");
			if (nodeComputer != null) {
				XmlNode nodeChairs = xmlDocument.SelectSingleNode("//computer[@name='" + compName + "']/chairs");
				if (nodeChairs != null)
					Chairs = nodeChairs.InnerText;

				XmlNode nodeLiveQueue = xmlDocument.SelectSingleNode("//computer[@name='" + compName + "']/liveQueue");
				if (nodeLiveQueue != null)
					LiveQueue = nodeLiveQueue.InnerText.Equals("1");
			} else
				Logging.ToLog("ConfigReader - Не удалось найти элемент для текущей системы " + compName);
			
			ConfigReadedSuccessfull = true;

			DispatcherTimer dispatcherTimerAdvertisements = new DispatcherTimer();
			dispatcherTimerAdvertisements.Interval = TimeSpan.FromMinutes(10);
			dispatcherTimerAdvertisements.Tick += DispatcherTimerAdvertisements_Tick;
			dispatcherTimerAdvertisements.Start();
			DispatcherTimerAdvertisements_Tick(null, null);
		}

		private static XmlDocument GetConfig() {
			XmlDocument xmlDocument = new XmlDocument();

			try {
				xmlDocument.Load(ConfigFilePath);
			} catch (Exception e) {
				Logging.ToLog("ConfigReader - " + e.Message + Environment.NewLine + e.StackTrace);
				ConfigReadedSuccessfull = false;
				return null;
			}

			if (!xmlDocument.HasChildNodes) {
				Logging.ToLog("ConfigReader - Отсутствуют дочерние элементы");
				ConfigReadedSuccessfull = false;
				return null;
			}

			return xmlDocument;
		}

		private static void DispatcherTimerAdvertisements_Tick(object sender, EventArgs e) {
			Logging.ToLog("ConfigReader - " + "Считывание информации о рекламных сообщениях");

			Advertisements.Clear();

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
					string stopShowing = GetChildInnerText(xmlDocument, "/configuration/advertisements/advertisement[" + (i + 1) + "]/stopShowing");
					if (!stopShowing.Equals("0")) {
						DateTime.TryParse(stopShowing, out DateTime stopShowingDate);
						if (stopShowingDate != new DateTime()) {
							if (DateTime.Now.Date >= stopShowingDate.Date) {
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

					Advertisements.Add(itemAdvertisement);
				} catch (Exception exc) {
					Logging.ToLog("ConfigReader - " + exc.Message + Environment.NewLine + exc.StackTrace);
				}
			}
		}

		static private string GetChildInnerText(XmlDocument xmlDocument, string xPath) {
			Logging.ToLog("ConfigReader - Считывание значения для элемента: " + xPath);
			XmlNode xmlNode = xmlDocument.SelectSingleNode(xPath);

			if (xmlNode == null) {
				Logging.ToLog("ConfigReader - Не удалось прочитать элемент");
				return null;
			}

			return xmlNode.InnerText;
		}

		public class ItemAdvertisement {
			public string Title { get; set; } = string.Empty;
			public string Body { get; set; } = string.Empty;
			public string PostScriptum { get; set; } = string.Empty;
		}
    }
}
