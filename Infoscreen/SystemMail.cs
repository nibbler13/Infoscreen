using System.Net.Mail;
using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Net.Mime;

namespace Infoscreen {
	public static partial class SystemMail {
		public static void SendMail (string body) {
			string subject = "Ошибки в работе Infoscreen " + Environment.MachineName;
			Logging.ToLog("Отправка сообщения, тема: " + subject + ", текст: " + body);
			Logging.ToLog("Получатели: " + MAIL_RECEIVER);

			try {
				string appName = Assembly.GetExecutingAssembly().GetName().Name;
				MailAddress from = new MailAddress(USER_NAME + "@" + USER_DOMAIN, appName);

				body += Environment.NewLine + Environment.NewLine + 
					"___________________________________________" + Environment.NewLine +
					"Это автоматически сгенерированное сообщение" + Environment.NewLine + 
					"Просьба не отвечать на него" + Environment.NewLine +
 					"Имя системы: " + Environment.MachineName;

				MailMessage message = new MailMessage {
					From = from,
					Subject = subject,
					Body = body
				};

				message.To.Add(new MailAddress(MAIL_RECEIVER));

				SmtpClient client = new SmtpClient(MAIL_SMTP_SERVER, 25) {
					UseDefaultCredentials = false,
					Credentials = new System.Net.NetworkCredential(USER_NAME, USER_PASSWORD, USER_DOMAIN)
				};

				client.SendCompleted += (s, e) => {
					client.Dispose();
					message.Dispose();
					Logging.ToLog("Письмо отправлено успешно");
				};
				
				client.Send(message);
			} catch (Exception e) {
				Logging.ToLog("SendMail exception: " + e.Message + Environment.NewLine + e.StackTrace);
			}
		}
	}
}
