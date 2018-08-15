using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace InfoscreenConfigManager {
	/// <summary>
	/// Логика взаимодействия для WindowSelectFilial.xaml
	/// </summary>
	public partial class WindowEditSystemChairs : Window, INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public ObservableCollection<Infoscreen.Configuration.ItemSystem.ItemChair> ChairItems { get; set; } =
			new ObservableCollection<Infoscreen.Configuration.ItemSystem.ItemChair>();

		public ObservableCollection<Infoscreen.Configuration.ItemSystem.ItemChair> SelectedChairItems { get; set; } =
			new ObservableCollection<Infoscreen.Configuration.ItemSystem.ItemChair>();

		public ICollectionView ChairItemsView {
			get { return CollectionViewSource.GetDefaultView(ChairItems); }
		}

		private string searchByRoomNumber;
		public string SearchByRoomNumber {
			get { return searchByRoomNumber; }
			set {
				searchByRoomNumber = value;
				NotifyPropertyChanged();
				ChairItemsView.Refresh();
			}
		}

		public WindowEditSystemChairs(
			List<Infoscreen.Configuration.ItemSystem.ItemChair> chairItems, 
			string systemName, 
			ObservableCollection<Infoscreen.Configuration.ItemSystem.ItemChair> selectedChairItems) {
			InitializeComponent();
			DataContext = this;
			chairItems.ForEach(ChairItems.Add);
			Title += systemName;
			DataGridItemChairs.DataContext = this;
			DataGridSelectedItemChairs.DataContext = this;
			ChairItems.OrderBy(x => x.ChairName).ThenBy(y => y.RoomName);
			ChairItemsView.Filter = new Predicate<object>(o => FilterByRoomNumber(o as Infoscreen.Configuration.ItemSystem.ItemChair));
			SelectedChairItems = selectedChairItems;
			SetButtonsState();
			TextBoxFilterByRoomNumber.Focus();
		}

		private void ButtonOk_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		private bool FilterByRoomNumber(Infoscreen.Configuration.ItemSystem.ItemChair itemChair) {
			return SearchByRoomNumber == null ||
				itemChair.RoomNumber.Contains(SearchByRoomNumber);
		}

		private void ButtonToSelectedOne_Click(object sender, RoutedEventArgs e) {
			OneChairToSelected();
		}

		private void ButtonToSelectedAll_Click(object sender, RoutedEventArgs e) {
			foreach (Infoscreen.Configuration.ItemSystem.ItemChair item in ChairItemsView)
				SelectedChairItems.Add(item);

			foreach (Infoscreen.Configuration.ItemSystem.ItemChair item in SelectedChairItems)
				ChairItems.Remove(item);

			SetButtonsState();
		}

		private void ButtonFromSelectedAll_Click(object sender, RoutedEventArgs e) {
			foreach (Infoscreen.Configuration.ItemSystem.ItemChair item in SelectedChairItems)
				ChairItems.Add(item);

			SelectedChairItems.Clear();
			SetButtonsState();
		}

		private void ButtonFromSelectedOne_Click(object sender, RoutedEventArgs e) {
			OneChairFromSelected();
		}

		private void DataGridSelectedItemChairs_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			OneChairFromSelected();
		}

		private void DataGridItemChairs_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			OneChairToSelected();
		}

		private void DataGridSelectedItemChairs_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			SetButtonsState();
		}

		private void DataGridItemChairs_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			SetButtonsState();
		}

		private void SetButtonsState() {
			ButtonToSelectedAll.IsEnabled = !ChairItemsView.IsEmpty;
			ButtonFromSelectedAll.IsEnabled = SelectedChairItems.Count > 0;
			ButtonFromSelectedOne.IsEnabled = DataGridSelectedItemChairs.SelectedItems.Count > 0;
			ButtonToSelectedOne.IsEnabled = DataGridItemChairs.SelectedItems.Count > 0;
		}

		private void OneChairToSelected() {
			if (!(DataGridItemChairs.SelectedItem is Infoscreen.Configuration.ItemSystem.ItemChair itemChair))
				return;

			ChairItems.Remove(itemChair);
			SelectedChairItems.Add(itemChair);

			SetButtonsState();
		}

		private void OneChairFromSelected() {
			if (!(DataGridSelectedItemChairs.SelectedItem is Infoscreen.Configuration.ItemSystem.ItemChair itemChair))
				return;

			ChairItems.Add(itemChair);
			SelectedChairItems.Remove(itemChair);

			SetButtonsState();
		}
	}
}
