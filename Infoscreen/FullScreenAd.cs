using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Infoscreen {
	public class FullScreenAd {
		public static List<ItemAd> GetAdItems(string path, bool onlyAvailable) {
			Dictionary<string, ItemAd> adItems = new Dictionary<string, ItemAd>();

			if (!Directory.Exists(path))
				return adItems.Values.ToList();

			Dictionary<string, ItemAd> availableAd = new Dictionary<string, ItemAd>();
			List<string> advertisementsInFolder =
				Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).
				Where(f => new List<string> { ".jpg", ".png" }.IndexOf(Path.GetExtension(f)) >= 0).ToList();

			if (advertisementsInFolder.Count == 0)
				return adItems.Values.ToList();

			foreach (string item in advertisementsInFolder) {
				try {
					string itemName = Path.GetFileNameWithoutExtension(item);
					string[] itemNameSplitted = itemName.Split('@');

					if (itemNameSplitted.Length != 3)
						continue;

					string dateToStop = itemNameSplitted[0];
					if (DateTime.TryParse(dateToStop, out DateTime stopDate)) {
						if (onlyAvailable && DateTime.Now.Date >= stopDate)
							continue;
					} else
						continue;

					DateTime createDate = File.GetCreationTime(item);

					itemName = itemNameSplitted[1];
					if (!adItems.ContainsKey(itemName))
						adItems.Add(itemName, new ItemAd(itemName, createDate, stopDate));

					adItems[itemName].Images.Add(new ItemImage(item));			
				} catch (Exception e) {
					Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace); 
				}
			}

			return adItems.Values.ToList();
		}

		public class ItemAd : INotifyPropertyChanged {
			public event PropertyChangedEventHandler PropertyChanged;
			private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}

			public Brush StopDateBackground {
				get {
					if (IsEnding && StopDate.Date <= DateTime.Now.Date)
						return new SolidColorBrush(Colors.LightYellow);
					else
						return new SolidColorBrush(Colors.Transparent);
				}
            }
			public string Name { get; set; }
			public DateTime CreateDate { get; private set; }
			private DateTime stopDate;
			public DateTime StopDate { 
				get { return stopDate; }
				set {
					if (value != stopDate) {
						stopDate = value;
						NotifyPropertyChanged();
						NotifyPropertyChanged("StopDateBackground");
                    }
                }
			}

			private bool isEnding;
			public bool IsEnding { 
				get { return isEnding; }
				set {
					if (value != isEnding) {
						isEnding = value;
						NotifyPropertyChanged();
						NotifyPropertyChanged("StopDateBackground");
                    }
                }
			}

			public string OptimalImage { get {
					return GetOptimalImage();
                } 
			}

			public ObservableCollection<ItemImage> Images { get; private set; } = new ObservableCollection<ItemImage>();

			public ItemAd(string name, DateTime createDate, DateTime stopDate) {
				Name = name;
				CreateDate = createDate;
				StopDate = stopDate;
				IsEnding = stopDate.Year < 2100;
            }

			private string GetOptimalImage() {
				double windowWidth = SystemParameters.PrimaryScreenWidth;
				double windowHeight = SystemParameters.PrimaryScreenHeight;
				double windowRatio = windowHeight / windowWidth;

				ItemImage itemImage = null;
                foreach (ItemImage image in Images) {
					if (image.Width == windowWidth && image.Height == windowHeight) {
						itemImage = image;
						break;
                    }

					if (Math.Abs(image.Ratio - windowRatio) <= 0.05) {
						if (itemImage == null) {
							itemImage = image;
							continue;
						}

						if (image.SizeInPixels > itemImage.SizeInPixels) {
							itemImage = image;
							continue;
                        }
					}
				}

				if (itemImage == null) {
                    foreach (ItemImage image in Images) {
						if (itemImage == null) {
							itemImage = image;
							continue;
                        }

						if (image.SizeInPixels > itemImage.SizeInPixels)
							itemImage = image;
                    }
                }

				return itemImage != null ? itemImage.FullPath : string.Empty;
			}
		}

		public class ItemImage {
			public string FullPath { get; private set; }
			public double Width { get; private set; }
			public double Height { get; private set; }

			public string Dimensions { 
				get {
					return Width + "x" + Height;
                } 
			}

			public double SizeInPixels { 
				get {
					return Width * Height;
                } 
			}

			public double Ratio {
				get {
					return Width != 0 ? Height / Width : 0;
				}
			}
			
			public ItemImage(string fullPath) {
				FullPath = fullPath;

                try {
					using (System.Drawing.Image image = System.Drawing.Image.FromFile(FullPath)) {
						Width = image.Width;
						Height = image.Height;
					}
				} catch (Exception e) {
					Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
                }
			}
		}
	}
}
