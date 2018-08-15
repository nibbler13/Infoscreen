using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Infoscreen {
	public class Advertisement {
		public bool ShowAd { get; set; }
		public int PauseBetweenAdInSeconds { get; set; }
		public int AdDurationInSeconds { get; set; }
		public string AdFilePath { get; set; }
		public bool IsReadedSuccessfully { get; set; } = false;

		public List<ItemAdvertisement> AdvertisementItems { get; set; } =
			new List<ItemAdvertisement>();

		private int CurrentAdvertisementIndex { get; set; } = 0;



		public Advertisement() {
			ShowAd = true;
			PauseBetweenAdInSeconds = 30;
			AdDurationInSeconds = 20;
		}




		public static bool GetAdvertisement(string adFilePath, out Advertisement advertisement) {
			try {
				FileStream fileStream = new FileStream(adFilePath, FileMode.Open);
				XmlSerializer xmlSerializer = new XmlSerializer(typeof(Advertisement));
				xmlSerializer.UnknownAttribute += (s, e) => { Logging.ToLog(e.ExpectedAttributes); };
				xmlSerializer.UnknownElement += (s, e) => { Logging.ToLog(e.ExpectedElements); };
				xmlSerializer.UnknownNode += (s, e) => { Logging.ToLog(e.Name); };
				xmlSerializer.UnreferencedObject += (s, e) => { Logging.ToLog(e.UnreferencedId); };
				advertisement = (Advertisement)xmlSerializer.Deserialize(fileStream);
				advertisement.AdFilePath = adFilePath;
				advertisement.IsReadedSuccessfully = true;

				return true;
			} catch (Exception e) {
				Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
				advertisement = new Advertisement();

				return false;
			}
		}

		public static bool SaveAdvertisements(Advertisement advertisement) {
			try {
				XmlSerializer xmlSerializer = new XmlSerializer(typeof(Advertisement));
				TextWriter textWriter = new StreamWriter(advertisement.AdFilePath);
				xmlSerializer.Serialize(textWriter, advertisement);
				textWriter.Close();

				return true;
			} catch (Exception e) {
				Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);

				return false;
			}
		}



		public ItemAdvertisement GetNextAdItem() {
			//configuration.CurrentAdvertisementIndex++;
			//if (configuration.CurrentAdvertisementIndex == configuration.AdvertisementItems.Count)
			//	configuration.CurrentAdvertisementIndex = 0;

			return new ItemAdvertisement();
		}



		public class ItemAdvertisement {
			public string Title { get; set; } = "Заголовок";
			public string Body { get; set; } = "Основной текст";
			public string PostScriptum { get; set; } = "Постскриптум";
			public DateTime? DateBegin { get; set; } = null;
			public DateTime? DateEnd { get; set; } = null;
			public int OrderNumber { get; set; }

			public ItemAdvertisement() {
				Random random = new Random();
				OrderNumber = random.Next(0, 1000);
			}
		}
	}
}
