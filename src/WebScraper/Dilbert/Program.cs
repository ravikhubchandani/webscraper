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
            const bool PUSH_TO_EMAIL = true;
            const bool PUSH_TO_TEAMS = false;
            _appSettings = GetAppSettings();

            var stripCollection = new List<(string src, string title)>();

            // Apparently Dilbert is not available anymore
            // StripContentConentGenerator.AddDilbert(stripCollection, DateTime.Today);
            StripContentConentGenerator.AddGoComicsStrip(stripCollection, DateTime.Today, _appSettings.GoComicsStrips);

            if (!stripCollection.Any())
                return;

            if (PUSH_TO_EMAIL)
            {
                string msg = StripContentConentGenerator.GetComicStripHtmlContent(stripCollection);
                SendGmailNotification(msg);
            }

            if (PUSH_TO_TEAMS)
            {
                string msg = GetJsonNotification(stripCollection);
                SendTeamsNotification(msg);
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

        private static void SendTeamsNotification(string msg)
        {
            var notifier = new TeamsNotifier(_appSettings.TeamsWebhookUri);
            notifier.Push(msg);
        }

        private static void SendGmailNotification(string msg)
        {
            var notifier = new EmailNotifier(_appSettings.AzureAcsConnectionString, _appSettings.AzureAcsSenderEmail)
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