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
		public PageAdvertisement(string imagesToShow) {
			InitializeComponent();

			try {
				ImageMain.Source = new BitmapImage(new Uri(imagesToShow));
			} catch (Exception e) {
				Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
			}
		}
	}
}
