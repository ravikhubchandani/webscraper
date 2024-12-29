using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WebScraper.Core;

namespace WebScraper.DailyStrips
{
    public static class Program
    {
        private static AppSettings _appSettings;

        static void Main(string[] args)
        {
            const bool PUSH_TO_GMAIL = false;
            const bool PUSH_TO_TEAMS = false;
            const bool PUSH_TO_MAILCHIMP = true;
            _appSettings = GetAppSettings();

            var stripCollection = new List<(string src, string title)>();
            StripContentConentGenerator.AddDilbert(stripCollection, DateTime.Today);
            StripContentConentGenerator.AddGoComicsStrip(stripCollection, DateTime.Today, _appSettings.GoComicsStrips);

            if (!stripCollection.Any())
                return;

            if (PUSH_TO_GMAIL)
            {
                string msg = StripContentConentGenerator.GetComicStripHtmlContent(stripCollection);
                SendGmailNotification(msg);
            }

            if (PUSH_TO_TEAMS)
            {
                string msg = GetJsonNotification(stripCollection);
                SendTeamsNotification(msg);
            }

            if (PUSH_TO_MAILCHIMP)
            {
                string msg = StripContentConentGenerator.GetComicStripHtmlContent(stripCollection);
                SendMailChimpNotification(msg);
            }
        }

        private static AppSettings GetAppSettings()
        {
            var settings = new AppSettings();
            IConfiguration config = new ConfigurationBuilder()
                   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                   .Build();
            config.Bind(settings);
            return settings;
        }

        private static void SendMailChimpNotification(string msg)
        {
            // var notifier = new MailChimpNotifier(_appSettings.MailChimpApiKey, _appSettings.MailChimpListId);
            // notifier.Push(msg);
        }

        private static void SendTeamsNotification(string msg)
        {
            var notifier = new TeamsNotifier(_appSettings.TeamsWebhookUri);
            notifier.Push(msg);
        }

        private static void SendGmailNotification(string msg)
        {
            var notifier = new GmailNotifier(_appSettings.EmailAddressFrom, _appSettings.EmailAddressFromPassword)
            {
                EmailSubject = "Daily strips - " + DateTime.Today.ToString("dddd dd", CultureInfo.GetCultureInfo("es-ES"))
            };
            notifier.AddReciever(_appSettings.EmailAddressTo);
            notifier.Push(msg);
        }

        private static string GetJsonNotification(List<(string src, string title)> stripData)
        {
            return string.Format(@"{{
              ""@context"": ""https://schema.org/extensions"",
              ""@type"": ""MessageCard"",
              ""themeColor"": ""6F2DA8"",
              ""title"": ""{0}"",
              ""text"":""{1}""
            }}",
            string.Empty, //$"Daily strips - {DateTime.Today.ToString("dddd dd", CultureInfo.GetCultureInfo("es-ES"))}",
            StripContentConentGenerator.GetComicStripHtmlBodyContent(stripData));
        }
    }
}