using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Infoscreen {
	public class DataProvider {
		public Dictionary<string, ItemChair> ChairsDict { get; private set; } = new Dictionary<string, ItemChair>();
		private readonly Configuration configuration;
		private FirebirdClient firebirdClient;

		private List<string> doctors = new List<string>();
		private readonly int previousDay = DateTime.Now.Day;

		public event EventHandler OnUpdateCompleted = delegate { };
		public bool IsUpdateSuccessfull { get; private set; }

		private static readonly string rootFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		private static readonly string localDoctorPhotosPath = Path.Combine(rootFolder, "DoctorsPhotos");
		private static readonly string localDepartmentPhotosPath = Path.Combine(rootFolder, "DepartmentsPhotos");



		public DataProvider(Configuration configReader) {
			this.configuration = configReader;
			firebirdClient = new FirebirdClient(
				configReader.DataBaseAddress,
				configReader.DataBaseName,
				configReader.DataBaseUserName,
				configReader.DataBasePassword);
		}




		public void UpdateData() {
			Logging.ToLog("DataProvider - Обновление данных");

			DataTable dataTable = firebirdClient.GetDataTable(configuration.DataBaseQuery,
				new Dictionary<string, object>() { { "@chairList", configuration.GetChairsIdForSystem(Environment.MachineName) } });
			ChairsDict.Clear();

			Logging.ToLog("DataProvider - Получено строк - " + dataTable.Rows.Count);

			if (dataTable.Rows.Count != 0) {
				IsUpdateSuccessfull = true;

				foreach (DataRow dataRow in dataTable.Rows)
					try {
						ItemChair itemChair = ParseItemChair(dataRow);

						if (!ChairsDict.ContainsKey(itemChair.ChID))
							ChairsDict.Add(itemChair.ChID, itemChair);
						else
							Logging.ToLog("!!! DataProvider - элемент с ключом уже добавлен: " + itemChair.ChID);
					} catch (Exception e) {
						IsUpdateSuccessfull = false;
						Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
					}
			} else {
				Logging.ToLog("DataProvider - Результат запроса - 0 строк");
				IsUpdateSuccessfull = false;
			}

			OnUpdateCompleted(null, EventArgs.Empty);

			if (previousDay == DateTime.Now.Day)
				return;

			Logging.ToLog("DataProvider - ----- Автоматическое завершение работы");
			Application.Current.Shutdown();
		}

		private ItemChair ParseItemChair(DataRow dataRow) {
			string chid = dataRow["CHID"].ToString();
			string rnum = dataRow["RNUM"].ToString();
			string status = dataRow["STATUS"].ToString();
			string acfullname = dataRow["ACFULLNAME"].ToString();
			string timeleft = dataRow["TIMELEFT"].ToString();
			string dsinfo = dataRow["DSINFO"].ToString();

			Logging.ToLog("DataProvider - Кресло: " + chid + "|" + rnum);

			ItemChair itemChair = new ItemChair(chid, rnum);
			ItemChair.StatusInfo currentState = new ItemChair.StatusInfo() {
				PatientToInviteName = acfullname,
				ReceptionTimeLeft = timeleft
			};

			switch (status) {
				case "10":
					currentState.Status = ItemChair.Status.Delayed;
					break;
				case "20":
				case "21":
					currentState.Status = ItemChair.Status.Underway;
					break;
				case "30":
				case "31":
					currentState.Status = ItemChair.Status.Invitation;
					break;
				case "40":
					currentState.Status = ItemChair.Status.Free;
					break;
				case "50":
				default:
					currentState.Status = ItemChair.Status.NotConducted;
					break;
			}
			
			itemChair.CurrentState = currentState;

			if (string.IsNullOrEmpty(dsinfo))
				return itemChair;

			if (!dsinfo.Contains("|")) {
				Logging.ToLog("DataProvider - Строка не содержит разделитель |, пропуск обработки");
				return itemChair;
			}

			string[] docInfo = dsinfo.Split('|');
			if (docInfo.Length != 4) {
				Logging.ToLog("DataProvider - Количество элементов в строке не соответствует 4, пропуск обработки");
				return itemChair;
			}

			string[] docNames = docInfo[0].Split('@'); //new string[] { "Иванов Иван Иванович", "Сидоров Роман Андреевич" }; //
			string[] docPositions = docInfo[1].Split('@'); //new string[] { "Терапевт", "Хирург" }; //
			string[] docDepartments = docInfo[2].Split('@'); //new string[] { "Терапия", "Хирургия" }; //
			string workTime = docInfo[3];

			for (int i = 0; i < docNames.Length; i++) {
				Logging.ToLog("DataProvider - Сотрудник: " + docNames[i] + "|" + docPositions[i] + "|" + docDepartments[i] + "|" + workTime);
				Logging.ToLog("DataProvider - Статус: " + currentState.Status);

				string docName = ClearDoctorName(docNames[i]);

				string docPosition = docPositions[i];
				if (docName.TrimEnd(' ').EndsWith("ич") && docPosition.ToLower().Contains("медицинская сестра"))
					docPosition = "Медицинский брат";

				ItemChair.Employee employee = new ItemChair.Employee() {
					Name = docName,
					Position = docPosition,
					Department = docDepartments[i],
					WorkingTime = workTime
				};

				//doctors list is using for photo search
				if (!doctors.Contains(employee.Name))
					doctors.Add(employee.Name);

				if (currentState.employees.Where(x => x.Name.Equals(employee.Name)).Count() == 0)
					currentState.employees.Add(employee);
				else {
					for (int x = 0; x < currentState.employees.Count; x++) {
						if (currentState.employees[x].Name.Equals(employee.Name)) {
							if (!currentState.employees[x].Department.Contains(employee.Department))
								currentState.employees[x].Department += ", " + employee.Department;

							if (!currentState.employees[x].Position.Contains(employee.Position))
								currentState.employees[x].Position += ", " + employee.Position;

							break;
						}
					}
				}
			}

			itemChair.CurrentState = currentState;
			return itemChair;
		}

		public void UpdateDoctorsPhoto() {
			Logging.ToLog("DataProvider - Обновление фотографий сотрудников");
			
			Logging.ToLog("DataProvider - Папка назначения: " + localDoctorPhotosPath);
			if (!Directory.Exists(localDoctorPhotosPath)) {
				try {
					Directory.CreateDirectory(localDoctorPhotosPath);
				} catch (Exception e) {
					Logging.ToLog("DataProvider - " + e.Message + Environment.NewLine + e.StackTrace);
					return;
				}
			}

			string searchPath = configuration.PhotosFolderPath;
			Logging.ToLog("DataProvider - Папка поиска: " + searchPath);
			if (!Directory.Exists(searchPath)) {
				Logging.ToLog("DataProvider - Директория с фотографиями недоступна: " + searchPath);
				return;
			}

			string[] photos = new string[0];
			try {
				photos = Directory.GetFiles(searchPath, "*.jpg", SearchOption.AllDirectories);
				Logging.ToLog("DataProvider - Найдено фотографий: " + photos.Length);
			} catch (Exception e) {
				Logging.ToLog("DataProvider - " + e.Message + Environment.NewLine + e.StackTrace);
				return;
			}	
			
			foreach (string doctor in doctors) {
				Logging.ToLog("DataProvider - Поиск фото для сотрудника: " + doctor);
				bool isPhotoFound = false;

				foreach (string photo in photos) {
					try {
						string fileName = Path.GetFileNameWithoutExtension(photo);
						if (!fileName.ToLower().Replace('ё', 'е').Replace("  ", " ").Contains(doctor.ToLower().Replace('ё', 'е')))
							continue;

						isPhotoFound = true;
						string destFileName = Path.Combine(localDoctorPhotosPath, doctor + ".jpg");

						if (File.Exists(destFileName) &&
							File.GetLastWriteTime(destFileName).Equals(File.GetLastWriteTime(photo))) {
							Logging.ToLog("DataProvider - Пропуск копирования файла (скопирован ранее) " + photo);
							break;
						}

						File.Copy(photo, destFileName, true);
						Logging.ToLog("DataProvider - Копирование файла " + photo + " в файл " + destFileName);
						break;
					} catch (Exception e) {
						Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
						isPhotoFound = false;
					}
				}

				if (!isPhotoFound)
					Logging.ToLog("DataProvider - Не удалось найти фото");
			}
		}

		public static BitmapImage GetImageForDoctor(string name) {
			Logging.ToLog("DataProvider - Поиск фото для сотрудника: " + name);
			string defaultImage = "pack://application:,,,/Infoscreen;component/Media/DoctorWithoutAPhoto.png";

			if (!Directory.Exists(localDoctorPhotosPath)) {
				Logging.ToLog("Не удается найти \\ получить доступ к папке: " + localDoctorPhotosPath);
				return GetBitmapFromFile(defaultImage);
			}

			try {
				string[] files = Directory.GetFiles(localDoctorPhotosPath, "*.jpg");
				
				foreach (string file in files) {
					string fileName = Path.GetFileNameWithoutExtension(file);

					if (!fileName.Contains(name))
						continue;
					
					return GetBitmapFromFile(file);
				}

				Logging.ToLog("DataProvider - Не удалось найти изображение для сотрудника: " + name);
			} catch (Exception e) {
				Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
			}

			return GetBitmapFromFile(defaultImage);
		}

		public static BitmapImage GetImageForDepartment(string depname) {
			try {
				string wantedFile = Path.Combine(localDepartmentPhotosPath, depname + ".png");

				if (File.Exists(wantedFile)) 
					return GetBitmapFromFile(wantedFile);
			} catch (Exception e) {
				Logging.ToLog("Не удалось открыть файл с изображением: " + e.Message +
					Environment.NewLine + e.StackTrace);
			}

			return GetBitmapFromFile("pack://application:,,,/Infoscreen;component/Media/UnknownDepartment.png");
		}

		private static BitmapImage GetBitmapFromFile(string file) {
			if (file.StartsWith("pack"))
				return new BitmapImage(new Uri(file));

			try {
				using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
					BitmapImage bitmapImage = new BitmapImage();
					stream.Seek(0, SeekOrigin.Begin);
					bitmapImage.BeginInit();
					bitmapImage.CreateOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;
					bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
					bitmapImage.StreamSource = stream;
					bitmapImage.EndInit();
					return bitmapImage;
				}
			} catch (Exception e) {
				Logging.ToLog("DataProvider - GetBitmapFromFile: " + file + " - " + e.Message + Environment.NewLine + e.StackTrace);
				return null;
			}
		}

		private string ClearDoctorName(string name) {
			if (name.Contains('(')) {
				name = name.Substring(0, name.IndexOf('('));
				name = name.TrimEnd(' ');
			}

			return name;
		}

		public static string ClearTimeString(string timeString) {
			string text = string.Empty;
			string[] timeValues = timeString.Split(new string[] { " - " }, StringSplitOptions.None);

			if (timeValues.Length == 2) {
				string partLeft = timeValues[0].TrimStart('0');
				if (partLeft.StartsWith(":"))
					partLeft = "0" + partLeft;

				string partRight = timeValues[1].TrimStart('0');
				if (partRight.StartsWith(":"))
					partRight = "0" + partRight;

				text = partLeft + " - " + partRight;
			} else
				text = timeString;

			return text;
		}

		public Timetable GetTimeTable() {
			Logging.ToLog("DataProvider - получение расписания");
			Timetable timetable = new Timetable();

			DataTable dataTable = firebirdClient.GetDataTable(
				configuration.DataBaseQueryTimetable, new Dictionary<string, object>());

			if (dataTable.Rows.Count == 0)
				Logging.ToLog("DataProvider - не удалось получить данные расписания");

			foreach (DataRow row in dataTable.Rows) {
				try {
					string depName = row["DEPNAME"].ToString();
					string docName = ClearDoctorName(row["FULLNAME"].ToString());
					string d0 = row["D0"].ToString();
					string d1 = row["D1"].ToString();
					string d2 = row["D2"].ToString();
					string d3 = row["D3"].ToString();
					string d4 = row["D4"].ToString();
					string d5 = row["D5"].ToString();
					string d6 = row["D6"].ToString();

					Timetable.DocInfo docInfo = new Timetable.DocInfo(docName, d0, d1, d2, d3, d4, d5, d6);

					if (!timetable.departments.ContainsKey(depName))
						timetable.departments.Add(depName, new Timetable.Department());

					if (!timetable.departments[depName].doctors.ContainsKey(docName))
						timetable.departments[depName].doctors.Add(docName, docInfo);
				} catch (Exception e) {
					Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
				}
			}

			if (previousDay == DateTime.Now.Day)
				return timetable;

			Logging.ToLog("DataProvider - ----- Автоматическое завершение работы");
			Application.Current.Shutdown();
			return timetable;
		}

		public class Timetable {
			public SortedDictionary<string, Department> departments = new SortedDictionary<string, Department>();

			public class Department {
				public SortedDictionary<string, DocInfo> doctors = new SortedDictionary<string, DocInfo>();
			}

			public class DocInfo {
				public string name;
				public SortedDictionary<string, string> timeTable = new SortedDictionary<string, string>();

				public DocInfo(string name, string d0, string d1, string d2, string d3, string d4, string d5, string d6) {
					this.name = name;
					timeTable.Add("d0", d0);
					timeTable.Add("d1", d1);
					timeTable.Add("d2", d2);
					timeTable.Add("d3", d3);
					timeTable.Add("d4", d4);
					timeTable.Add("d5", d5);
					timeTable.Add("d6", d6);
				}
			}
		}

		public class ItemChair {
			public enum Status { NotConducted, Free, Invitation, Underway, Delayed }
			public string ChID { get; private set; } = string.Empty;
			public string RNum { get; private set; } = string.Empty;
			public StatusInfo CurrentState { get; set; }

			public ItemChair(string chid, string rnum) {
				ChID = chid;
				RNum = rnum;
			}

			public class StatusInfo {
				public Status Status { get; set; } = ItemChair.Status.NotConducted;
				public List<Employee> employees = new List<Employee>();
				public string PatientToInviteName { get; set; } = string.Empty;
				public string ReceptionTimeLeft { get; set; } = string.Empty;
			}

			public class Employee {
				public string Position { get; set; } = string.Empty;
				public string Name { get; set; } = string.Empty;
				public string Department { get; set; } = string.Empty;
				public string WorkingTime { get; set; } = string.Empty;
			}
		}
	}
}
