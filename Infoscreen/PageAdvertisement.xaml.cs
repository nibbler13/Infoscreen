using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Infoscreen {
	/// <summary>
	/// Interaction logic for PageAdvertisement.xaml
	/// </summary>
	public partial class PageAdvertisement : Page {
		private List<string> imagesToShow;
        private int currentImage = 0;

		public PageAdvertisement(List<string> imagesToShow) {
			InitializeComponent();
			this.imagesToShow = imagesToShow;

			IsVisibleChanged += (s, e) => {
                if (!(bool)e.NewValue)
                    return;

				try {
                    if (currentImage == imagesToShow.Count)
                        currentImage = 0;

					string filePath = imagesToShow[currentImage++];
					Logging.ToLog("Считывание файла: " + filePath);
					ImageMain.Source = new BitmapImage(new Uri(filePath));
				} catch (Exception exc) {
					Logging.ToLog(exc.Message + Environment.NewLine + exc.StackTrace);
				}
			};
		}
	}
}
