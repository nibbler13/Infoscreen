using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace InfoscreenAdvertisementManager {
	public partial class MainWindow : Window, INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private Infoscreen.Advertisement advertisement;
		private List<Infoscreen.FullScreenAd.ItemAd> fullscreenAdInfoscreen;

		private int selectedIndex = 0;

		public ObservableCollection<ItemFilial> Filials { get; set; } = new ObservableCollection<ItemFilial>();

		private ItemFilial selectedFilial;
		public ItemFilial SelectedFilial { 
			get { return selectedFilial; }
			set {
				if (value != selectedFilial) {
					if (advertisement != null && advertisement.IsNeedToSave() && !Debugger.IsAttached) {
						MessageBoxResult result = 
							MessageBox.Show(this, "Сохранить изменения?", "В текущем филиале имеются изменения", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

						if (result == MessageBoxResult.Cancel)
							return;

						if (result == MessageBoxResult.Yes) {
							if (advertisement.SaveAdvertisements(out string error))
								MessageBox.Show(this, "Изменения сохранены", string.Empty, MessageBoxButton.OK, MessageBoxImage.Information);
							else
								MessageBox.Show(this, "Не удалось сохранить изменения: " + Environment.NewLine + error, string.Empty, MessageBoxButton.OK, MessageBoxImage.Error);

							if (advertisement.IsNeedToSave())
								return;
						}
                    }

					selectedFilial = value;
					NotifyPropertyChanged();

					if (IsLoaded)
						MainWindow_Loaded(this, null);
                }
            }
		}

		public class ItemFilial {
			public string Name { get; set; }
			public string InfoscreenSettingsFolder { get; set; }
			public string LoyaltyViewerAdvertisementsFolder { get; set; }

			public string Prefix { get; set; }

			public ItemFilial(string name) {
				Name = name;
            }

			override public string  ToString() {
				return Name;
            }
        }


		public MainWindow() {
			InitializeComponent();

			Filials.Add(new ItemFilial("Сретенка") {
				InfoscreenSettingsFolder = @"\\mspo-fs-02\Infoscreen",
				LoyaltyViewerAdvertisementsFolder = @"\\mspo-infomon-2\Advertisements",
				Prefix = "mspo"
			});

			Filials.Add(new ItemFilial("Сущевка") {
				InfoscreenSettingsFolder = @"\\mssu-fs-01\Infoscreen",
				LoyaltyViewerAdvertisementsFolder = @"\\mssu-plaz-r1\Advertisements",
				Prefix = "mssu"
			});

			Filials.Add(new ItemFilial("Сущевка детство") {
				InfoscreenSettingsFolder = @"\\mssu-fs-01\Infoscreen",
				LoyaltyViewerAdvertisementsFolder = @"\\mssu-plaz-r2\Advertisements",
				Prefix = "mssu"
			});

			Filials.Add(new ItemFilial("Фрунзенская") {
				InfoscreenSettingsFolder = @"\\mskm-fs-01\Infoscreen",
				LoyaltyViewerAdvertisementsFolder = @"\\mskm-infomon-2\Advertisements",
				Prefix = "mskm"
			});

			Filials.Add(new ItemFilial("Краснодар") {
				InfoscreenSettingsFolder = @"\\kdtt-fs-01\Infoscreen",
				LoyaltyViewerAdvertisementsFolder = @"\\kdtt-infomon-02\Advertisements",
				Prefix = "kdtt"
			});

			Filials.Add(new ItemFilial("Казань") {
				InfoscreenSettingsFolder = @"\\kzkk-fs-01\Infoscreen",
				LoyaltyViewerAdvertisementsFolder = @"\\kzkk-infomon-2\Advertisements",
				Prefix = "kzkk"
			});

			Filials.Add(new ItemFilial("Каменск-Уральский") {
				InfoscreenSettingsFolder = @"\\yekuk-fs-01\Infoscreen",
				Prefix = "yekuk"
			});

			Filials.Add(new ItemFilial("Санкт-Петербург") {
				InfoscreenSettingsFolder = @"\\splp-fs-01\Infoscreen",
				LoyaltyViewerAdvertisementsFolder = @"\\splp-infomon-2\Advertisements",
				Prefix = "splp"
			});

			Filials.Add(new ItemFilial("Сочи") {
				InfoscreenSettingsFolder = @"\\sctrk-fs-01\Infoscreen",
				LoyaltyViewerAdvertisementsFolder = @"\\sctrk-loyal-mon\Advertisements",
				Prefix = "sctrk"
			});

			Filials.Add(new ItemFilial("Уфа") {
				InfoscreenSettingsFolder = @"\\ufkk-fs-01\Infoscreen",
				LoyaltyViewerAdvertisementsFolder = @"\\ufkk-infomon-2\Advertisements",
				Prefix = "ufkk"
			});

			foreach (ItemFilial filial in Filials) {
				if (Environment.MachineName.ToLower().Contains(filial.Prefix.ToLower())) {
					SelectedFilial = filial;
					break;
				}
			}

			if (SelectedFilial == null)
				SelectedFilial = Filials[selectedIndex++];

			DataContext = this;

			Infoscreen.Logging.ToLog(new string('-', 40));
			Infoscreen.Logging.ToLog("Запуск приложения Advertisement Manager, пользователь: " + Environment.UserName);

			Loaded += MainWindow_Loaded;
			Closing += MainWindow_Closing;

			if (Debugger.IsAttached) {
				DispatcherTimer timer = new DispatcherTimer();
				timer.Interval = new TimeSpan(0, 0, 6);
				timer.Tick += (s, a) => {
					Console.WriteLine(DateTime.Now.ToLongTimeString() + ": " + selectedIndex);
					if (selectedIndex == Filials.Count)
						selectedIndex = 0;

					SelectedFilial = Filials[selectedIndex++];
				};
				timer.Start();
			}
		}

		private async void MainWindow_Loaded(object sender, RoutedEventArgs e) {
			if (SelectedFilial == null)
				return;

			string adFilePath = Path.Combine(SelectedFilial.InfoscreenSettingsFolder, "Advertisement.xml");
			TextBlockTextAdInfoscreen.Text = "Считывание файла с информацией о рекламе: " + adFilePath;

			string fullscreenAdInfoscreenPath =
				Path.Combine(SelectedFilial.InfoscreenSettingsFolder, "FullScreenAdvertisements");
			TextBlockFullscreenAdInfoscreen.Text = "Считывание информации о слайдах Infoscreen " +
				fullscreenAdInfoscreenPath;

			TextBlockTextAdInfoscreen.Visibility = Visibility.Visible;
			FrameTextAdInfoscreen.Visibility = Visibility.Hidden;

			TextBlockFullscreenAdInfoscreen.Visibility = Visibility.Visible;
			FrameFullscreenAdInfoscreen.Visibility = Visibility.Hidden;

			await Task.Run(() => {
				Infoscreen.Advertisement.LoadAdvertisement(adFilePath, out advertisement); 

				if (Directory.Exists(fullscreenAdInfoscreenPath))
					fullscreenAdInfoscreen = Infoscreen.FullScreenAd.GetAdItems(fullscreenAdInfoscreenPath, false);

				Application.Current.Dispatcher.BeginInvoke(new Action(() => {
					if (advertisement.IsReadedSuccessfully) {
						PageAdvertisementFileView pageAdvertisementFileView =
							new PageAdvertisementFileView(advertisement);
						FrameTextAdInfoscreen.Navigate(pageAdvertisementFileView);
						advertisement.MarkAsSaved();
					} else {
						PageAdvertisementFileNotFound pageAdvertisementFileNotFound =
							new PageAdvertisementFileNotFound();
						FrameTextAdInfoscreen.Navigate(pageAdvertisementFileNotFound);
					}

					if (Directory.Exists(fullscreenAdInfoscreenPath)) {
						PageFullscreenAdInfoscreen pageFullscreenAdInfoscreen = new PageFullscreenAdInfoscreen(fullscreenAdInfoscreen);
						FrameFullscreenAdInfoscreen.Navigate(pageFullscreenAdInfoscreen);
					} else {
						TextBlockFullscreenAdInfoscreen.Text = "Не удается получить доступ (папка не существует): " +
							fullscreenAdInfoscreenPath;
					}
				}));
			});

			TextBlockTextAdInfoscreen.Visibility = Visibility.Hidden;
			FrameTextAdInfoscreen.Visibility = Visibility.Visible;

			TextBlockFullscreenAdInfoscreen.Visibility = Visibility.Hidden;
			FrameFullscreenAdInfoscreen.Visibility = Visibility;
		}

		private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			if (advertisement != null && advertisement.IsNeedToSave()) {
				MessageBoxResult result = MessageBox.Show(Application.Current.MainWindow,
					"Сохранить изменения?", "Имеются несохраненные изменения",
					MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

				if (result == MessageBoxResult.No)
					return;


				if (result == MessageBoxResult.Cancel) {
					e.Cancel = true;
					return;
				}

				if (advertisement.SaveAdvertisements(out string error))
					MessageBox.Show(this, "Изменения сохранены", string.Empty, MessageBoxButton.OK, MessageBoxImage.Information);
				else
					MessageBox.Show(this, "Не удалось сохранить изменения: " + Environment.NewLine + error, string.Empty, MessageBoxButton.OK, MessageBoxImage.Error);

				if (advertisement.IsNeedToSave())
					e.Cancel = true;
			}
		}

        private void Frame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e) {
            Console.WriteLine("Frame_Navigated, selectedIndex: " + selectedIndex);
			Frame frame = sender as Frame;

			if (frame.NavigationService.CanGoBack)
				frame.NavigationService.RemoveBackEntry();
        }
    }
}
