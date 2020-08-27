using System.Collections.Generic;
using System.Net;
using System.Net.Mail;

namespace WebScraper.Core
{
    public class EmailNotifier : INotifier
    {
        public string EmailSubject { get; set; }
        private List<string> _recievers;

        public EmailNotifier()
        {
            _recievers = new List<string>();
        }

        public void AddReciever(string email)
        {
            _recievers.Add(email);
        }

        public void Push(string content)
        {
            // TO DO Move configuration data to constructor or to appsettings.json file
            const string fromAddress = "ravi.notificaciones@gmail.com";
            const string fromPassword = "";

            var smtp = new SmtpClient
            {
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress, fromPassword),
                Host = "smtp.gmail.com",
                Port = 587
            };
            using (var message = new MailMessage(fromAddress, string.Join(',', _recievers))
            {
                Subject = EmailSubject,
                Body = content,
                IsBodyHtml = true
            })
            {
                smtp.Send(message);
            }
        }
    }
}