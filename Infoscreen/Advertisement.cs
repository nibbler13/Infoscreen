using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Infoscreen {
	public class Advertisement : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public bool DisableAdDisplay { get; set; }

		private int pauseBetweenAdInSeconds;
		public int PauseBetweenAdInSeconds {
			get { return pauseBetweenAdInSeconds; }
			set {
				if (value != pauseBetweenAdInSeconds) {
					pauseBetweenAdInSeconds = value;
					NotifyPropertyChanged();
				}
			}
		}

		private int adDurationInSeconds;
		public int AdDurationInSeconds {
			get { return adDurationInSeconds; }
			set {
				if (value != adDurationInSeconds) {
					adDurationInSeconds = value;
					NotifyPropertyChanged();
				}
			}
		}

		public string AdFilePath { get; set; }
		public bool IsReadedSuccessfully { get; set; } = false;

		public ObservableCollection<ItemAdvertisement> AdvertisementItems { get; set; } =
			new ObservableCollection<ItemAdvertisement>();

		private int CurrentAdvertisementIndex { get; set; } = 0;



		public Advertisement() {
			AdFilePath = Infoscreen.Logging.ASSEMBLY_DIRECTORY + "Advertisement.xml";
			DisableAdDisplay = false;
			PauseBetweenAdInSeconds = 30;
			AdDurationInSeconds = 20;
		}




		public static bool GetAdvertisement(string adFilePath, out Advertisement advertisement) {
			advertisement = new Advertisement();

			if (!File.Exists(adFilePath))
				return false;

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
			}

			return false;
		}

		public bool SaveAdvertisements(out string error) {
			error = string.Empty;

			try {
				XmlSerializer xmlSerializer = new XmlSerializer(typeof(Advertisement));
				TextWriter textWriter = new StreamWriter(AdFilePath);
				xmlSerializer.Serialize(textWriter, this);
				textWriter.Close();

				foreach (ItemAdvertisement item in AdvertisementItems)
					item.IsItemChanged = false;

				return true;
			} catch (Exception e) {
				error = e.Message + Environment.NewLine + e.StackTrace;
				Logging.ToLog(error);

				return false;
			}
		}



		public ItemAdvertisement GetNextAdItem() {
			//configuration.CurrentAdvertisementIndex++;
			//if (configuration.CurrentAdvertisementIndex == configuration.AdvertisementItems.Count)
			//	configuration.CurrentAdvertisementIndex = 0;

			return new ItemAdvertisement();
		}



		public class ItemAdvertisement : INotifyPropertyChanged {
			public event PropertyChangedEventHandler PropertyChanged;
			private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
				IsItemChanged = true;
			}

			private bool displayTitle = true;
			public bool DisplayTitle {
				get { return displayTitle; }
				set {
					if (value != displayTitle) {
						displayTitle = value;
						NotifyPropertyChanged();
						NotifyPropertyChanged("DisplayBodyIcon");
						MaxLenghtBody += value ? -50 : 50;
					}
				}
			}

			public bool DisplayBodyIcon {
				get { return !DisplayTitle; }
			}

			private bool displayPostScriptum = true;
			public bool DisplayPostScriptum {
				get { return displayPostScriptum; }
				set {
					if (value != displayPostScriptum) {
						displayPostScriptum = value;
						NotifyPropertyChanged();
						MaxLenghtBody += value ? -60 : 60;
					}
				}
			}

			private bool setDateBegin = false;
			public bool SetDateBegin {
				get { return setDateBegin; }
				set {
					if (value != setDateBegin) {
						setDateBegin = value;
						NotifyPropertyChanged();
					}
				}
			}

			private bool setDateEnd = false;
			public bool SetDateEnd {
				get { return setDateEnd; }
				set {
					if (value != setDateEnd) {
						setDateEnd = value;
						NotifyPropertyChanged();
					}
				}
			}

			public string Title { get; set; } = "Заголовок";
			public string Body { get; set; } = "Основной текст";
			public string PostScriptum { get; set; } = "Постскриптум";

			public DateTime? DateBegin { get; set; } = null;
			public DateTime? DateEnd { get; set; } = null;

			private int maxLenghtTitle = 40;
			public int MaxLenghtTitle {
				get { return maxLenghtTitle; }
				set {
					if (value != maxLenghtTitle) {
						maxLenghtTitle = value;
						NotifyPropertyChanged();
					}
				}
			}

			private int maxLenghtBody = 100;
			public int MaxLenghtBody {
				get { return maxLenghtBody; }
				set {
					if (value != maxLenghtBody) {
						maxLenghtBody = value;
						NotifyPropertyChanged();
					}
				}
			}

			private int maxLenghtPostScriptum = 70;
			public int MaxLenghtPostScriptum {
				get { return maxLenghtPostScriptum; }
				set {
					if (value != maxLenghtPostScriptum) {
						maxLenghtPostScriptum = value;
						NotifyPropertyChanged();
					}
				}
			}

			public int OrderNumber { get; set; }
			public string DateCreated { get; set; }
			public bool IsItemChanged = false;

			public ItemAdvertisement() {
				Random random = new Random();
				OrderNumber = random.Next(0, 1000);
				DateCreated = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToShortTimeString();
			}
		}
	}
}
