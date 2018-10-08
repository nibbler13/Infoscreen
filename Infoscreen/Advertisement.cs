using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
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
			IsSaved = false;
		}

		private bool disableAdDisplay;
		public bool DisableAdDisplay {
			get { return disableAdDisplay; }
			set {
				if (value != disableAdDisplay) {
					disableAdDisplay = value;
					NotifyPropertyChanged();
				}
			}
		}

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

		public List<ItemAdvertisement> AdvertisementItemsToShow = new List<ItemAdvertisement>();

		private int CurrentAdvertisementIndex { get; set; } = 0;
		private bool IsSaved = false;


		public Advertisement() {
			AdFilePath = Infoscreen.Logging.ASSEMBLY_DIRECTORY + "Advertisement.xml";
			DisableAdDisplay = false;
			PauseBetweenAdInSeconds = 15;
			AdDurationInSeconds = 25;
		}




		public static bool LoadAdvertisement(string adFilePath, out Advertisement advertisement) {
			Logging.ToLog("Advertisement - Считывание файла с информационными сообщениями: " + adFilePath);
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
				advertisement.CurrentAdvertisementIndex = 0;

				Random random = new Random();
				foreach (ItemAdvertisement item in advertisement.AdvertisementItems) {
					item.OrderNumber = random.Next(0, 1000);

					if (item.SetDateBegin && item.DateBegin.HasValue && DateTime.Now < item.DateBegin.Value)
						continue;

					if (item.SetDateEnd && item.DateEnd.HasValue && DateTime.Now > item.DateEnd.Value)
						continue;

					advertisement.AdvertisementItemsToShow.Add(item);
				}

				return true;
			} catch (Exception e) {
				Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
			}

			return false;
		}

		public void MarkAsSaved() {
			IsSaved = true;

			for (int i = 0; i < AdvertisementItems.Count; i++)
				AdvertisementItems[i].HasChanged = false;
		}

		public bool IsNeedToSave() {
			bool isAllItemsHasntChanges = true;

			foreach (ItemAdvertisement item in AdvertisementItems)
				if (item.HasChanged) {
					isAllItemsHasntChanges = false;
					break;
				}

			return !IsSaved || !isAllItemsHasntChanges;
		}

		public bool IsAdItemsCorrect(out string error) {
			error = string.Empty;
			bool result = true;

			foreach (ItemAdvertisement item in AdvertisementItems) {
				if (!item.HasChanged)
					continue;

				string head = "Сообщение от " + item.DateCreated + ": ";
				string internalError = string.Empty;

				if ((item.DisplayTitle && item.TitleSymbolsCountBackground == Brushes.Yellow) ||
					item.BodySymbolsCountBackground == Brushes.Yellow ||
					(item.DisplayPostScriptum && item.PostScriptumSymbolsCountBackground == Brushes.Yellow)) {
					internalError +=  "некорректное количество символов (выделено желтым)" + Environment.NewLine;
					result = false;
				}

				if (item.SetDateBegin && item.DateBegin == null) {
					internalError += "не выбрана дата начала отображения" + Environment.NewLine;
					result = false;
				}

				if (item.SetDateEnd && item.DateEnd == null) {
					internalError += "не выбрана дата окончания отображения" + Environment.NewLine;
					result = false;
				}

				if (item.SetDateBegin && item.SetDateEnd &&
					item.DateBegin.HasValue && item.DateEnd.HasValue && 
					(item.DateEnd.Value <= item.DateBegin.Value)) {
					internalError += "дата окончания меньше или равна дате начала отображения" + Environment.NewLine;
					result = false;
				}

				if ((item.DisplayTitle && item.Title.Equals(ItemAdvertisement.TITLE_TEMPLATE)) ||
					item.Body.Equals(ItemAdvertisement.BODY_TEMPLATE) ||
					(item.DisplayPostScriptum && item.PostScriptum.Equals(ItemAdvertisement.POSTSCRIPTUM_TEMPLATE))) {
					internalError += "текст сообщения не заполнен";
					result = false;
				}

				if (!string.IsNullOrEmpty(internalError))
					error += head + Environment.NewLine + internalError;
			}

			return result;
		}

		public bool SaveAdvertisements(out string error) {
			error = string.Empty;

			try {
				XmlSerializer xmlSerializer = new XmlSerializer(typeof(Advertisement));
				TextWriter textWriter = new StreamWriter(AdFilePath);
				xmlSerializer.Serialize(textWriter, this);
				textWriter.Close();

				IsSaved = true;
				foreach (ItemAdvertisement item in AdvertisementItems)
					item.HasChanged = false;

				return true;
			} catch (Exception e) {
				error = e.Message + Environment.NewLine + e.StackTrace;
				Logging.ToLog(error);

				return false;
			}
		}



		public ItemAdvertisement GetNextAdItem() {
			if (AdvertisementItemsToShow.Count == 0)
				return new ItemAdvertisement();

			CurrentAdvertisementIndex++;
			if (CurrentAdvertisementIndex == AdvertisementItemsToShow.Count)
				CurrentAdvertisementIndex = 0;

			return AdvertisementItemsToShow.OrderBy(x => x.OrderNumber).ToList()[CurrentAdvertisementIndex];
		}



		public class ItemAdvertisement : INotifyPropertyChanged {
			public event PropertyChangedEventHandler PropertyChanged;
			private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
				HasChanged = true;
			}

			public const string TITLE_TEMPLATE = "Заголовок";
			public const string BODY_TEMPLATE = "Основной текст";
			public const string POSTSCRIPTUM_TEMPLATE = "Постскриптум";


			private bool displayTitle = true;
			public bool DisplayTitle {
				get { return displayTitle; }
				set {
					if (value != displayTitle) {
						displayTitle = value;
						NotifyPropertyChanged();
						NotifyPropertyChanged("DisplayBodyIcon");
						MaxLenghtBody += value ? -40 : 40;
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
						MaxLenghtBody += value ? -50 : 50;
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
			
			private string title = TITLE_TEMPLATE;
			public string Title {
				get { return title; }
				set {
					if (value != title) {
						title = value;
						NotifyPropertyChanged();
						NotifyPropertyChanged("TitleSymbolsCount");
					}
				}
			}

			private string body = BODY_TEMPLATE;
			public string Body {
				get { return body; }
				set {
					if (value != body) {
						body = value;
						NotifyPropertyChanged();
						NotifyPropertyChanged("BodySymbolsCount");
					}
				}
			}

			private string postScriptum = POSTSCRIPTUM_TEMPLATE;
			public string PostScriptum {
				get { return postScriptum; }
				set {
					if (value != postScriptum) {
						postScriptum = value;
						NotifyPropertyChanged();
						NotifyPropertyChanged("PostScriptumSymbolsCount");
					}
				}
			}

			public DateTime? DateBegin { get; set; } = null;
			public DateTime? DateEnd { get; set; } = null;

			private int maxLenghtTitle = 40;
			public int MaxLenghtTitle {
				get { return maxLenghtTitle; }
				set {
					if (value != maxLenghtTitle) {
						maxLenghtTitle = value;
						NotifyPropertyChanged();
						NotifyPropertyChanged("TitleSymbolsCount");
					}
				}
			}

			private int maxLenghtBody = 120;
			public int MaxLenghtBody {
				get { return maxLenghtBody; }
				set {
					if (value != maxLenghtBody) {
						maxLenghtBody = value;
						NotifyPropertyChanged();
						NotifyPropertyChanged("BodySymbolsCount");
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
						NotifyPropertyChanged("PostScriptumSymbolsCount");
					}
				}
			}


			public string TitleSymbolsCount {
				get {
					NotifyPropertyChanged("TitleSymbolsCountBackground");
					return Title.Length + "/" + MaxLenghtTitle;
				}
			}

			public string BodySymbolsCount {
				get {
					NotifyPropertyChanged("BodySymbolsCountBackground");
					return Body.Length + "/" + MaxLenghtBody;
				}
			}

			public string PostScriptumSymbolsCount {
				get {
					NotifyPropertyChanged("PostScriptumSymbolsCountBackground");
					return PostScriptum.Length + "/" + MaxLenghtPostScriptum;
				}
			}


			public Brush TitleSymbolsCountBackground {
				get {
					if (Title.Length == 0) {
						return Brushes.Yellow;
					} else if (Title.Length < MaxLenghtTitle)
						return Brushes.White;
					else if (Title.Length == MaxLenghtTitle)
						return Brushes.LightGreen;
					else
						return Brushes.Yellow;
				}
			}

			public Brush BodySymbolsCountBackground {
				get {
					if (Body.Length == 0) {
						return Brushes.Yellow;
					} else if (Body.Length < MaxLenghtBody)
						return Brushes.White;
					else if (Body.Length == MaxLenghtBody)
						return Brushes.LightGreen;
					else
						return Brushes.Yellow;
				}
			}

			public Brush PostScriptumSymbolsCountBackground {
				get {
					if (PostScriptum.Length == 0) {
						return Brushes.Yellow;
					} else if (PostScriptum.Length < MaxLenghtPostScriptum)
						return Brushes.White;
					else if (PostScriptum.Length == MaxLenghtPostScriptum)
						return Brushes.LightGreen;
					else
						return Brushes.Yellow;
				}
			}

			public int OrderNumber { get; set; }
			public string DateCreated { get; set; }
			public bool HasChanged = false;

			public ItemAdvertisement() {
				DateCreated = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToShortTimeString();
			}
		}
	}
}
