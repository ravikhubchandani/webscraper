using System.Collections.Generic;
using System.Net;
using System.Net.Mail;

namespace WebScraper.Core
{
    public class GmailNotifier : INotifier
    {
        public string EmailSubject { get; set; }
        private readonly List<string> _recievers;
        private readonly string _emailAddressFrom;
        private readonly string _emailAddressFromPwd;

        public GmailNotifier(string emailAddressFrom, string emailAddressFromPwd)
        {
            _recievers = new List<string>();
            _emailAddressFrom = emailAddressFrom;
            _emailAddressFromPwd = emailAddressFromPwd;
        }

        public void AddReciever(params string[] email)
        {
            _recievers.AddRange(email);
        }

        public void Push(string content)
        {
            var smtp = new SmtpClient
            {
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_emailAddressFrom, _emailAddressFromPwd),
                Host = "smtp.gmail.com",
                Port = 587
            };
            using (var message = new MailMessage(_emailAddressFrom, string.Join(',', _recievers))
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