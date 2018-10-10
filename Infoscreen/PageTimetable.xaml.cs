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
	/// Логика взаимодействия для PageTimetable.xaml
	/// </summary>
	public partial class PageTimetable : Page {
		private DataProvider.Timetable timetable;
		private enum TextBlockStyle { Department, DoctorName, WorkTime, Days }

		public PageTimetable(DataProvider.Timetable timetable) {
			InitializeComponent();

			this.timetable = timetable;

			CreateHeader();

			int row = 1;
			foreach (KeyValuePair<string, DataProvider.Timetable.Department> department  in timetable.departments) {
				CreateTextBlock(department.Key, row, 0, TextBlockStyle.Department);
				row++;

				foreach (KeyValuePair<string, DataProvider.Timetable.DocInfo> docInfo in department.Value.doctors) {
					CreateTextBlock(docInfo.Key, row, 0, TextBlockStyle.DoctorName);

					int column = 1;
					foreach (KeyValuePair<string, string> schedule in docInfo.Value.timeTable) {
						string text = string.Empty;
						string[] timeValues = schedule.Value.Split(new string[] { " - " }, StringSplitOptions.None);

						if (timeValues.Length == 2)
							text = timeValues[0].TrimStart('0') + " - " + timeValues[1].TrimStart('0');
						else
							text = schedule.Value;

						CreateTextBlock(text, row, column, TextBlockStyle.WorkTime);
						column++;
					}

					row++;
				}
			}
		}

		private void CreateHeader() {
			for (int i = 0; i < 7; i++) {
				string dayOfWeek = string.Empty;
				DateTime dateToShow = DateTime.Now.AddDays(i);
				switch (dateToShow.DayOfWeek) {
					case DayOfWeek.Sunday:
						dayOfWeek = "Вс";
						break;
					case DayOfWeek.Monday:
						dayOfWeek = "Пн";
						break;
					case DayOfWeek.Tuesday:
						dayOfWeek = "Вт";
						break;
					case DayOfWeek.Wednesday:
						dayOfWeek = "Ср";
						break;
					case DayOfWeek.Thursday:
						dayOfWeek = "Чт";
						break;
					case DayOfWeek.Friday:
						dayOfWeek = "Пт";
						break;
					case DayOfWeek.Saturday:
						dayOfWeek = "Сб";
						break;
					default:
						break;
				}

				string month = string.Empty;
				switch (dateToShow.Month) {
					case 1:
						month = "янв.";
						break;
					case 2:
						month = "фев.";
						break;
					case 3:
						month = "мар.";
						break;
					case 4:
						month = "апр.";
						break;
					case 5:
						month = "мая";
						break;
					case 6:
						month = "июн.";
						break;
					case 7:
						month = "июл.";
						break;
					case 8:
						month = "авг.";
						break;
					case 9:
						month = "сен.";
						break;
					case 10:
						month = "окт.";
						break;
					case 11:
						month = "ноя.";
						break;
					case 12:
						month = "дек.";
						break;
					default:
						break;
				}

				dayOfWeek += " " + dateToShow.Day + " " + month;
				CreateTextBlock(dayOfWeek, 0, i + 1, TextBlockStyle.Days);
			}
		}

		private void CreateTextBlock(string text, int row, int column, TextBlockStyle textBlockStyle) {
			int columnSpan = 1;
			HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center;
			double fontSize = Application.Current.MainWindow.ActualHeight / 35;
			Brush foreground = new SolidColorBrush(Color.FromRgb(45, 61, 63));
			FontFamily fontFamily = new FontFamily("Franklin Gothic Book");
			FontWeight fontWeight = FontWeights.Light;
			Thickness margin = new Thickness(0);

			switch (textBlockStyle) {
				case TextBlockStyle.Department:
					fontSize *= 1.3;
					columnSpan = 8;
					foreground = Brushes.White;
					fontWeight = FontWeights.DemiBold;
					break;
				case TextBlockStyle.DoctorName:
					fontSize *= 1.15;
					horizontalAlignment = HorizontalAlignment.Left;
					fontWeight = FontWeights.Normal;
					margin = new Thickness(10, 0, 10, 0);
					break;
				case TextBlockStyle.WorkTime:
					fontSize *= 0.9;
					fontWeight = FontWeights.ExtraLight;
					margin = new Thickness(3, 0, 3, 0);
					break;
				case TextBlockStyle.Days:
					fontFamily = new FontFamily("Franklin Gothic");
					fontWeight = FontWeights.DemiBold;
					break;
				default:
					break;
			}

			Border border = new Border();
			if (textBlockStyle == TextBlockStyle.Department) {
				border.Background = new SolidColorBrush(Color.FromRgb(0, 169, 220));
				border.HorizontalAlignment = HorizontalAlignment.Stretch;
			} else {
				border.BorderThickness = new Thickness(1, 1, 0, 0);
				border.BorderBrush = Brushes.LightGray;
			}

			Grid.SetRow(border, row);
			Grid.SetColumn(border, column);
			Grid.SetColumnSpan(border, columnSpan);
			GridMain.Children.Add(border);

			TextBlock textBlock = new TextBlock();
			textBlock.Text = text;
			textBlock.HorizontalAlignment = horizontalAlignment;
			textBlock.FontSize = fontSize;
			textBlock.Foreground = foreground;
			textBlock.VerticalAlignment = VerticalAlignment.Center;
			textBlock.FontFamily = fontFamily;
			textBlock.FontWeight = fontWeight;
			textBlock.TextWrapping = TextWrapping.Wrap;
			textBlock.Margin = margin;

			Grid.SetRow(textBlock, row);
			Grid.SetColumn(textBlock, column);
			Grid.SetColumnSpan(textBlock, columnSpan);

			if (textBlockStyle == TextBlockStyle.DoctorName) {
				textBlock.Text = string.Empty;
				textBlock.Inlines.Add(new Bold(new Run(text.Substring(0, 1)) { FontSize = fontSize * 1.3 }));
				textBlock.Inlines.Add(text.Substring(1, text.Length - 1));
			}

			if (textBlockStyle == TextBlockStyle.Department) {
				Image image = new Image();
				image.Source = DataProvider.GetImageForDepartment(text);
				image.Margin = new Thickness(0,4,20,4);
				image.VerticalAlignment = VerticalAlignment.Center;
				RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);

				StackPanel stackPanel = new StackPanel();
				stackPanel.Orientation = Orientation.Horizontal;
				stackPanel.HorizontalAlignment = HorizontalAlignment.Center;
				stackPanel.VerticalAlignment = VerticalAlignment.Center;
				stackPanel.Children.Add(image);
				stackPanel.Children.Add(textBlock);

				Grid.SetRow(stackPanel, row);
				Grid.SetColumn(stackPanel, column);
				Grid.SetColumnSpan(stackPanel, columnSpan);

				GridMain.Children.Add(stackPanel);
			} else
				GridMain.Children.Add(textBlock);
		}
	}
}
