using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace Infoscreen {
	public class Configuration {
		public int DatabaseQueryExecutionIntervalInSeconds { get; set; }
		public int DoctorsPhotoUpdateIntervalInSeconds { get; set; }
		public int ChairPagesRotateIntervalInSeconds { get; set; }
		public int TimetableRotateIntervalInSeconds { get; set; }
		public string PhotosFolderPath { get; set; }
		public string DataBaseCopyAddress { get; set; }
		public string DataBaseAddress { get; set; }
		public string DataBaseName { get; set; }
		public string DataBaseUserName { get; set; }
		public string DataBasePassword { get; set; }
		public string DataBaseQuery { get; set; }
		public bool IsConfigReadedSuccessfull { get; set; } = false;

		public string ConfigFilePath { get; set; }
		public string ActiveDirectoryOU { get; set; }

		public List<ItemSystem> SystemItems { get; set; } = 
			new List<ItemSystem>();

		private readonly string SQL_QUERY =
			"/*changelog: " + Environment.NewLine +
			"ver 0.03 " + Environment.NewLine +
			"1)	Исправлена ошибка при которой в некоторых ситуациях не отображалась инфо о докторе " + Environment.NewLine +
			"2)	Должность врача в настоящей версии берется из справочника " + Environment.NewLine +
			"3)	Удалён рейтинг врача " + Environment.NewLine +
			"ver 0.02 " + Environment.NewLine +
			"1)	Приглашение на приём - возвращается только имя + отчество " + Environment.NewLine +
			"2)	Добавлен рейтинг врача - поле DSINFO 4 колонка " + Environment.NewLine +
			"3)	Исправлена ошибка, когда при статусе «Идёт приём» в поле timeleft отображалось 0 мин.  " + Environment.NewLine +
			"*/ " + Environment.NewLine +
			"--ver 0.03 " + Environment.NewLine +
			"with a as " + Environment.NewLine +
			"(select chid " + Environment.NewLine +
			",x.rnum " + Environment.NewLine +
			",CASE WHEN x.starttreat=1 and x.isdelay=1 THEN 10 --'Приём задерживается' " + Environment.NewLine +
			"WHEN x.starttreat=1 and x.isdelay=0 THEN 20 --'Идёт приём' " + Environment.NewLine +
			"WHEN x.starttreat=0 and x.actual=1 and x.sortactual=1 THEN 30 --'Пройдите на приём' " + Environment.NewLine +
			"WHEN x.starttreat=0 and x.actual=1 and x.sortactual=0 THEN 31 --'Пройдите на приём' " + Environment.NewLine +
			"WHEN x.DSINFO is not null THEN 40 --'Кабинет свободен' " + Environment.NewLine +
			"ELSE 50 --'Нет приёма' " + Environment.NewLine +
			"END status " + Environment.NewLine +
			",iif(x.starttreat=0 and x.actual=1,(select firstname||' '||midname from clients where pcode=x.pcode),null) acfullname /*при 'Пройдите на приём'*/ " + Environment.NewLine +
			",iif(x.starttreat=1 and x.isdelay=0 and et-ct<et-st,et-ct-1,null) timeleft /*при 'Идёт приём'*/ " + Environment.NewLine +
			",x.DSINFO " + Environment.NewLine +
			"from (select c.chid " + Environment.NewLine +
			",c.ct " + Environment.NewLine +
			",s.st " + Environment.NewLine +
			",s.et " + Environment.NewLine +
			",s.pcode " + Environment.NewLine +
			",c.rnum " + Environment.NewLine +
			",iif(c.ct-st&gt;=0 and c.ct-et<0,1,0) SORTACTUAL " + Environment.NewLine +
			",iif(c.ct-st&gt;=-5 and c.ct-et<0,1,0) ACTUAL " + Environment.NewLine +
			"/*ACTUAL (c.ct-st>=_ приглашать за _ мин до начала приема, c.ct-et<_ мин после конца приёма) !изменяются синхронно! */ " + Environment.NewLine +
			",iif(s.starttreat is null,0,1) starttreat " + Environment.NewLine +
			",iif(s.et<=c.ct+1,1,0) isdelay " + Environment.NewLine +
			",(select list(d.fullname,'@') ||'|'|| " + Environment.NewLine +
			"list(profname,'@') ||'|'|| " + Environment.NewLine +
			"list(depname,'@') ||'|'|| " + Environment.NewLine +
			"lpad(cast(min(beghour||':'||begmin) as time),5)||' - '||lpad(cast(min(endhour||':'||endmin) as time),5) INFO " + Environment.NewLine +
			"from doctshedule ds " + Environment.NewLine +
			"join doctor d on d.dcode=ds.dcode " + Environment.NewLine +
			"join departments dp on dp.depnum=d.depnum " + Environment.NewLine +
			"join profession p on p.profid=d.profid " + Environment.NewLine +
			"where ds.chair=c.chid " + Environment.NewLine +
			"and ds.wdate=current_date " + Environment.NewLine +
			"and (beghour*60+begmin)-5<=c.ct /*(beghour*60+begmin)-5 (приглашать за 5 мин до начала приема)*/ " + Environment.NewLine +
			"and (endhour*60+endmin)>c.ct " + Environment.NewLine +
			") DSINFO " + Environment.NewLine +
			"from (select id chid " + Environment.NewLine +
			",(select r.rnum from chairs ch join (select rid,rnum from rooms) r on r.rid=ch.rid where ch.chid=id) rnum " + Environment.NewLine +
			",extract(hour from current_time)*60+extract(minute from current_time) ct " + Environment.NewLine +
			"from mrgetlist(@chairList) " + Environment.NewLine +
			") c " + Environment.NewLine +
			"left join (select bhour*60+bmin ST " + Environment.NewLine +
			", fhour*60+fmin ET " + Environment.NewLine +
			", s1.* " + Environment.NewLine +
			"from schedule s1) s on s.chid=c.chid " + Environment.NewLine +
			"and s.workdate=current_date " + Environment.NewLine +
			"and s.pcode<>-1 " + Environment.NewLine +
			"and s.finishtreat is null " + Environment.NewLine +
			"and s.clvisit=1 " + Environment.NewLine +
			") x " + Environment.NewLine +
			") " + Environment.NewLine +
			"/*-------------------END WITH-------------------*/ " + Environment.NewLine +
			"select a.chid,a.rnum,a.status,a.acfullname,a.timeleft,a.dsinfo " + Environment.NewLine +
			"from a " + Environment.NewLine +
			"join (select chid " + Environment.NewLine +
			", min(status) status " + Environment.NewLine +
			"from a " + Environment.NewLine +
			"group by 1) srt on srt.chid=a.chid " + Environment.NewLine +
			"and srt.status=a.status " + Environment.NewLine +
			"group by 1,2,3,4,5,6";




		public Configuration() {
			ChairPagesRotateIntervalInSeconds = 15;
			TimetableRotateIntervalInSeconds = 30;
			DoctorsPhotoUpdateIntervalInSeconds = 3600;
			DatabaseQueryExecutionIntervalInSeconds = 15;
			DataBaseUserName = "sysdba";
			DataBasePassword = "masterkey";
			DataBaseQuery = SQL_QUERY;
			ConfigFilePath = Logging.ASSEMBLY_DIRECTORY + "InfoscreenConfig.xml";
		}




		public static bool GetConfiguration(string configFilePath, out Configuration configuration) {
			try {
				FileStream fileStream = new FileStream(configFilePath, FileMode.Open);
				XmlSerializer xmlSerializer = new XmlSerializer(typeof(Configuration));
				xmlSerializer.UnknownAttribute += (s, e) => { Logging.ToLog(e.ExpectedAttributes); };
				xmlSerializer.UnknownElement += (s, e) => { Logging.ToLog(e.ExpectedElements); };
				xmlSerializer.UnknownNode += (s, e) => { Logging.ToLog(e.Name); };
				xmlSerializer.UnreferencedObject += (s, e) => { Logging.ToLog(e.UnreferencedId); };
				configuration = (Configuration)xmlSerializer.Deserialize(fileStream);
				configuration.IsConfigReadedSuccessfull = true;
				configuration.ConfigFilePath = configFilePath;

				return true;
			} catch (Exception e) {
				Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
				configuration = new Configuration();

				return false;
			}
		}

		public static bool SaveConfiguration(Configuration configuration) {
			try {
				XmlSerializer xmlSerializer = new XmlSerializer(typeof(Configuration));
				TextWriter textWriter = new StreamWriter(configuration.ConfigFilePath);
				xmlSerializer.Serialize(textWriter, configuration);
				textWriter.Close();

				return true;
			} catch (Exception e) {
				Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);

				return false;
			}
		}

		public string GetChairsId() {
			return string.Empty;
		}

		public class ItemSystem : INotifyPropertyChanged {
			public event PropertyChangedEventHandler PropertyChanged;
			private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
			
			public string SystemName { get; set; } = string.Empty;
			public string SystemUnit { get; set; } = string.Empty;

			private bool isLiveQueue = false;
			public bool IsLiveQueue {
				get { return isLiveQueue; }
				set {
					if (value != isLiveQueue) {
						isLiveQueue = value;
						NotifyPropertyChanged();
					}
				}
			}

			private bool isTimetable = false;
			public bool IsTimetable {
				get { return isTimetable; }
				set {
					if (value != isTimetable) {
						isTimetable = value;
						NotifyPropertyChanged();
					}
				}
			}

			public ObservableCollection<ItemChair> ChairItems { get; set; } = 
				new ObservableCollection<ItemChair>();

			public class ItemChair {
				public string ChairID { get; set; } = string.Empty;
				public string ChairName { get; set; } = string.Empty;
				public string RoomNumber { get; set; } = string.Empty;
				public string RoomName { get; set; } = string.Empty;
			}
		}
    }
}
