using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Infoscreen {
	static class DataProvider {
		public static Dictionary<string, ItemChair> ChairsDict { get; private set; } = new Dictionary<string, ItemChair>();

		private static List<string> doctors = new List<string>();
		private static int previousDay = DateTime.Now.Day;
		private static FirebirdClient firebirdClient = new FirebirdClient(
				ConfigReader.DataBaseAddress,
				ConfigReader.DataBaseName,
				ConfigReader.DataBaseUserName,
				ConfigReader.DataBasePassword);

		public static event EventHandler OnUpdateCompleted = delegate { };
		public static bool IsUpdateSuccessfull { get; private set; }

		public static void UpdateData() {
			Logging.ToLog("DataProvider - Обновление данных");

			DataTable dataTable = firebirdClient.GetDataTable(ConfigReader.DataBaseQuery, 
				new Dictionary<string, object>() { { "@chairList", ConfigReader.Chairs } });
			ChairsDict.Clear();

			Logging.ToLog("DataProvider - Получено строк - " + dataTable.Rows.Count);

			if (dataTable.Rows.Count != 0) {
				IsUpdateSuccessfull = true;

				foreach (DataRow dataRow in dataTable.Rows) {
					try {
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

						if (!string.IsNullOrEmpty(dsinfo)) {
							if (dsinfo.Contains("|")) {
								string[] docInfo = dsinfo.Split('|');
								if (docInfo.Length == 4) {
									string[] docNames = docInfo[0].Split('@'); //new string[] { "Иванов Иван Иванович", "Сидоров Роман Андреевич" }; //
									string[] docPositions = docInfo[1].Split('@'); //new string[] { "Терапевт", "Хирург" }; //
									string[] docDepartments = docInfo[2].Split('@'); //new string[] { "Терапия", "Хирургия" }; //
									string workTime = docInfo[3];

									for (int i = 0; i < docNames.Length; i++) {
										Logging.ToLog("DataProvider - Сотрудник: " + docNames[i] + "|" + docPositions[i] + "|" + docDepartments[i] + "|" + workTime);
										Logging.ToLog("DataProvider - Статус: " + currentState.Status);

										ItemChair.Employee employee = new ItemChair.Employee() {
											Name = docNames[i],
											Position = docPositions[i],
											Department = docDepartments[i],
											WorkingTime = workTime
										};

										if (!doctors.Contains(employee.Name))
											doctors.Add(employee.Name);

										currentState.employees.Add(employee);
									}
								} else
									Logging.ToLog("DataProvider - Количество элементов в строке не соответствует 4, пропуск обработки");
							} else
								Logging.ToLog("DataProvider - Строка не содержит разделитель |, пропуск обработки");
						}

						itemChair.CurrentState = currentState;

						ChairsDict.Add(itemChair.ChID, itemChair);
					} catch (Exception e) {
						IsUpdateSuccessfull = false;
						Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
					}
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

		public static void UpdateDoctorsPhoto() {
			Logging.ToLog("DataProvider - Обновление фотографий сотрудников");

			string searchPath = ConfigReader.PhotosFolderPath;
			string destinationPath = Directory.GetCurrentDirectory() + "\\DoctorsPhotos\\";
			if (!Directory.Exists(destinationPath))
				Directory.CreateDirectory(destinationPath);

			if (!Directory.Exists(searchPath)) {
				Logging.ToLog("DataProvider - Директория с фотографиями недоступна: " + searchPath);
				return;
			}

			string[] photos = Directory.GetFiles(searchPath, "*.jpg", SearchOption.AllDirectories);
			List<string> missedPhotos = new List<string>();
			foreach (string doctor in doctors) {
				Logging.ToLog("DataProvider - Поиск фото для сотрудника: " + doctor);
				string photoLink = "";

				foreach (string photo in photos) {
					string fileName = Path.GetFileNameWithoutExtension(photo);
					if (!fileName.ToLower().Replace('ё', 'е').Replace("  ", " ").Contains(doctor.ToLower().Replace('ё', 'е')))
						continue;

					photoLink = photo;
					string destFileName = destinationPath + doctor + ".jpg";

					try {
						if (File.Exists(destFileName) &&
						File.GetLastWriteTime(destFileName).Equals(File.GetLastWriteTime(photo))) {
							Logging.ToLog("DataProvider - Пропуск копирования файла (скопирован ранее) " + photo);
							break;
						}

						File.Copy(photo, destFileName);
						Logging.ToLog("DataProvider - Копирование файла " + photo + " в файл " + destFileName);
						break;
					} catch (Exception e) {
						Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
						photoLink = "";
					}
				}

				if (String.IsNullOrEmpty(photoLink))
					Logging.ToLog("DataProvider - Не удалось найти фото");
			}
		}

		public static string GetImageForDoctor(string name) {
			Logging.ToLog("DataProvider - Поиск фото для сотрудника: " + name);

			try {
				string folderToSearchPhotos = Directory.GetCurrentDirectory() + "\\DoctorsPhotos\\";
				string[] files = Directory.GetFiles(folderToSearchPhotos, "*.jpg");

				string wantedFile = "";
				foreach (string file in files) {
					string fileName = Path.GetFileNameWithoutExtension(file);

					if (!fileName.Contains(name))
						continue;
					
					wantedFile = file;
					break;
				}

				if (string.IsNullOrEmpty(wantedFile)) {
					Logging.ToLog("DataProvider - Не удалось найти изображение для сотрудника: " + name);
					return string.Empty;
				}

				return wantedFile;
			} catch (Exception e) {
				Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
				return string.Empty;
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
