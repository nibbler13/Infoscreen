using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Infoscreen {
	class FullScreenAd {
		public static List<string> GetAvailableAd(string path) {
			if (!Directory.Exists(path))
				return new List<string>();

			Dictionary<string, ItemAd> availableAd = new Dictionary<string, ItemAd>();
			List<string> advertisementsInFolder =
				Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).
				Where(f => new List<string> { ".jpg", ".png" }.IndexOf(Path.GetExtension(f)) >= 0).ToList();

			if (advertisementsInFolder.Count == 0)
				return advertisementsInFolder;

			double windowWidth = Application.Current.MainWindow.ActualWidth;
			double windowHeight = Application.Current.MainWindow.ActualHeight;
			double windowRatio = windowWidth / windowHeight;

			foreach (string item in advertisementsInFolder) {
				try {
					string itemName = Path.GetFileNameWithoutExtension(item);
					string[] itemNameSplitted = itemName.Split('@');

					if (itemNameSplitted.Length != 3)
						continue;

					string dateToStop = itemNameSplitted[0];
					if (DateTime.TryParse(dateToStop, out DateTime dt) &&
						DateTime.Now.Date >= dt)
						continue;

					itemName = itemNameSplitted[1];
					ItemAd itemAd = new ItemAd(item, itemName);
					if (!availableAd.Keys.Contains(itemName)) {
						availableAd.Add(itemName, itemAd);
						continue;
					}

					//Existed and new items had same ratio
					if (Math.Abs(availableAd[itemName].Ratio - itemAd.Ratio) < 0.05) {
						if (availableAd[itemName].SizeInPixels >= itemAd.SizeInPixels) //Existed item had bigger resolution, skip
							continue;
						else {
							availableAd[itemName] = itemAd; //New item had bigger resolution, change existed item to new
							continue;
						}
					}

					//Existed and new items had different ratio

					//New item ratio equals window ratio
					if (Math.Abs(itemAd.Ratio - windowRatio) < 0.05) {
						availableAd[itemName] = itemAd;
						continue;
					}

					//Existed item ratio equals window ratio
					if (Math.Abs(availableAd[itemName].Ratio - windowRatio) < 0.05)
						continue;

					//Existed item size had bigger resolution
					if (availableAd[itemName].SizeInPixels >= itemAd.SizeInPixels)
						continue;

					availableAd[itemName] = itemAd;					
				} catch (Exception) { }
			}

			return availableAd.Values.Select(x => x.FullPath).ToList();
		}

		private class ItemAd {
			public string FullPath { get; private set; }
			public double SizeInPixels { get; private set; }
			public double Ratio { get; private set; }
			public string AdName { get; private set; }

			public ItemAd(string fullPath, string adName) {
				FullPath = fullPath;
				AdName = adName;

				using (Image image = Image.FromFile(FullPath)) {
					SizeInPixels = image.Width * image.Height;
					Ratio = (double)image.Width / (double)image.Height;
				}
			}
		}
	}
}
