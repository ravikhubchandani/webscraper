using System;
using System.Collections.Generic;
using System.Linq;
using Azure;
using Azure.Communication.Email;

namespace WebScraper.Core
{
    public class EmailNotifier : INotifier
    {
        public string EmailSubject { get; set; }
        private readonly List<EmailAddress> _recievers;
        private readonly string _azureAcsConnectionString;
        private readonly string _azrueAcsSenderEmail;

        public EmailNotifier(string azureAcsConnectionString, string azureAcsSenderEmail)
        {
            _recievers = new List<EmailAddress>();
            _azureAcsConnectionString = azureAcsConnectionString;
            _azrueAcsSenderEmail = azureAcsSenderEmail;
        }

        public void AddReciever(params string[] email)
        {
            _recievers.AddRange(email.Select(e => new EmailAddress(e)));
        }

        public void Push(string content)
        {
            var emailClient = new EmailClient(_azureAcsConnectionString);

            var emailMessage = new EmailMessage(
                senderAddress: _azrueAcsSenderEmail,
                content: new EmailContent($"Daily comic strip for {DateTime.Today.ToShortDateString()}")
                {
                    Html = content
                },
                recipients: new EmailRecipients(_recievers));                

            _ = emailClient.Send(WaitUntil.Completed, emailMessage);
        }
    }
}