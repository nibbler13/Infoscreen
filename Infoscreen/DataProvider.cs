using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infoscreen {
	static class DataProvider {
		private static List<string> doctors = new List<string>();
		private static int previousDay = DateTime.Now.Day;
		public static Dictionary<string, ItemChair> ChairsDict { get; private set; } = new Dictionary<string, ItemChair>();
		private static string sqlQuery = Properties.Settings.Default.MisDbSelectData;
		private static string chairsList = Properties.Settings.Default.ChairsIDList;
		private static FirebirdClient firebirdClient = new FirebirdClient(
				Properties.Settings.Default.MisDbAddress,
				Properties.Settings.Default.MisDbName,
				Properties.Settings.Default.MisDbUser,
				Properties.Settings.Default.MisDbPassword);

		public static event EventHandler OnUpdateCompleted = delegate { };

		public static void UpdateData() {
			Console.WriteLine("UpdateData");
			DataTable dataTable = firebirdClient.GetDataTable(sqlQuery, new Dictionary<string, object>() {
				{ "@chairsList", chairsList} });
			ChairsDict.Clear();
			if (dataTable.Rows.Count != 0) {

				foreach (DataRow dataRow in dataTable.Rows) {
					try {
						string chid = dataRow["CHID"].ToString();
						string rnum = dataRow["RNUM"].ToString();
						string status = dataRow["STATUS"].ToString();
						string acfullname = dataRow["ACFULLNAME"].ToString();
						string timeleft = dataRow["TIMELEFT"].ToString();
						string dsinfo = dataRow["DSINFO"].ToString();

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

						if (dsinfo.Contains("|")) {
							string[] docInfo = dsinfo.Split('|');
							if (docInfo.Length == 4) {
								ItemChair.Employee employee = new ItemChair.Employee() {
									Name = docInfo[0],
									Position = docInfo[1],
									Department = docInfo[2],
									WorkingTime = docInfo[3]
								};

								if (!doctors.Contains(employee.Name))
									doctors.Add(employee.Name);

								currentState.employees.Add(employee);
							}
						}

						itemChair.CurrentState = currentState;

						ChairsDict.Add(itemChair.ChID, itemChair);
					} catch (Exception e) {
						Console.WriteLine(e.Message + Environment.NewLine + e.StackTrace);
					}
				}
			}

			OnUpdateCompleted(null, EventArgs.Empty);

			if (previousDay == DateTime.Now.Day)
				return;

			doctors.Clear();
			previousDay = DateTime.Now.Day;
		}

		public static void UpdateDoctorsPhoto() {
			string searchPath = @Properties.Settings.Default.PathDoctorsPhotoSource;
			string destinationPath = Directory.GetCurrentDirectory() + "\\DoctorsPhotos\\";
			if (!Directory.Exists(destinationPath))
				Directory.CreateDirectory(destinationPath);

			if (!Directory.Exists(searchPath)) {
				//SystemNotification.DoctorsPhotoPathError();
				return;
			}

			string[] photos = Directory.GetFiles(searchPath, "*.jpg", SearchOption.AllDirectories);
			List<string> missedPhotos = new List<string>();
				foreach (string doctor in doctors) {
					string photoLink = "";

					foreach (string photo in photos) {
						string fileName = Path.GetFileNameWithoutExtension(photo);
						if (!fileName.ToLower().Replace('ё', 'е').Replace("  ", " ").Contains(doctor.ToLower().Replace('ё', 'е')))
							continue;

						photoLink = photo;
						string destFileName = destinationPath + doctor + ".jpg";

						try {
							if (File.Exists(destFileName) && 
							File.GetLastWriteTime(destFileName).Equals(File.GetLastWriteTime(photo))){
								//SystemLogging.LogMessageToFile("Пропуск копирования файла (скопирован ранее) " + photo);
								break;
							}

							File.Copy(photo, destFileName);
							//SystemLogging.LogMessageToFile("Копирование файла " + photo + " в файл " + destFileName);
							break;
						} catch (Exception e) {
							//SystemLogging.LogMessageToFile("UpdateDoctorsPhoto exception: " + e.Message +
								//Environment.NewLine + e.StackTrace);
							photoLink = "";
						}
					}

					//if (String.IsNullOrEmpty(photoLink))
					//	missedPhotos.Add(doctor.Name + " | " + doctor.Code + " | " + doctor.Department);
				}

			//if (missedPhotos.Count == 0)
			//	return;

			//missedPhotos.Sort();
			//SystemNotification.DoctorsPhotoMissed(missedPhotos);
		}

		public static string GetImageForDoctor(string name) {
			try {
				string folderToSearchPhotos = Directory.GetCurrentDirectory() + "\\DoctorsPhotos\\";
				string[] files = Directory.GetFiles(folderToSearchPhotos, "*.jpg");

				string wantedFile = "";
				foreach (string file in files) {
					string fileName = Path.GetFileNameWithoutExtension(file);

					if (!fileName.Contains(name))
						continue;

					//string fileDcode = fileName.Split('@')[1];

					//if (fileDcode.Equals(dcode)) {
						wantedFile = file;
						break;
					//}
				}

				if (string.IsNullOrEmpty(wantedFile)) {
					//SystemLogging.LogMessageToFile("Не удалось найти изображение для доктора с кодом: " + dcode);
					return string.Empty;
				}

				return wantedFile;
			} catch (Exception e) {
				//SystemLogging.LogMessageToFile("Не удалось открыть файл с изображением: " + e.Message +
					//Environment.NewLine + e.StackTrace);
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
