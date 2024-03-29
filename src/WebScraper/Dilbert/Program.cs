﻿using AngleSharp.Dom;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using WebScraper.Core;

namespace WebScraper.DailyStrips
{
    public static class Program
    {
        private static AppSettings _appSettings;

        static void Main(string[] args)
        {
            const bool PUSH_TO_EMAIL = false;
            const bool PUSH_TO_TEAMS = true;
            _appSettings = GetAppSettings();

            var stripCollection = new List<(string src, string title)>();
            AddDilbert(stripCollection, DateTime.Today);
            AddGoComicsStrip(stripCollection, DateTime.Today, _appSettings.GoComicsStrips);

            if (!stripCollection.Any())
                return;

            if (PUSH_TO_EMAIL)
            {
                string msg = GetHtmlNotification(stripCollection);
                SendEmailNotification(msg);
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

        private static void SendEmailNotification(string msg)
        {
            var notifier = new EmailNotifier(_appSettings.EmailAddressFrom, _appSettings.EmailAddressFromPassword)
            {
                EmailSubject = "Daily strips - " + DateTime.Today.ToString("dddd dd", CultureInfo.GetCultureInfo("es-ES"))
            };
            notifier.AddReciever(_appSettings.EmailAddressTo);
            notifier.Push(msg);
        }

        private static void AddDilbert(List<(string src, string title)> stripCollection, DateTime when)
        {
            Console.WriteLine("Fetching Dilbert");
            string source = "https://dilbert.com/";
            string selector = "div.img-comic-container img";
            string[] route = { "strip", when.ToString("yyyy-MM-dd") };
            GetStripFromSourceAndAddToCollection(stripCollection, source, selector, route, "src", "alt");
        }

        private static void AddGoComicsStrip(List<(string src, string title)> stripCollection, DateTime when, params string[] codes)
        {
            string source = "https://gocomics.com/";
            string selector = "div.comic__image img.img-fluid";
            foreach (string code in codes)
            {
                Console.WriteLine("Fetching " + code);
                string[] route = { code, when.ToString("yyyy"), when.ToString("MM"), when.ToString("dd") };
                GetStripFromSourceAndAddToCollection(stripCollection, source, selector, route, "src", "alt");
            }
        }

        private static void GetStripFromSourceAndAddToCollection(List<(string src, string title)> stripCollection, string source, string selector, string[] route, string imgAttr, string titleAttr)
        {
            for (int tries = 0; tries < 5; tries++)
            {
                try
                {
                    IElement domElement = GetDomForStrip(source, selector, route);
                    if (domElement != null)
                    {
                        (string, string) stripData = GetStripData(domElement, imgAttr, titleAttr);
                        stripCollection.Add(stripData);
                    }
                    break;
                }
                catch (Exception e)
                {
                    Thread.Sleep(5000);
                }
            }
        }

        private static IElement GetDomForStrip(string source, string selector, string[] route)
        {
            WebSource webSource = new WebSource(source);
            string body = webSource.GetResponseAsync(route).Result;
            HtmlContentParser parser = new HtmlContentParser(body);
            var dom = parser.GetFirstChild(selector);
            return dom.FirstOrDefault();
        }

        private static (string, string) GetStripData(IElement domElement, string imgAttr, string titleAttr)
        {
            string title = domElement.Attributes[titleAttr]?.Value ?? string.Empty;
            string img = ConvertToUrl(domElement.Attributes[imgAttr]?.Value) ?? string.Empty;
            return (img, title);
        }

        private static string ConvertToUrl(string resource)
        {
            if (string.IsNullOrWhiteSpace(resource))
                return resource;

            string url = resource.Trim('/');
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                url = "https://" + url;
            return url;
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
            GetHtmlNotificationBodyContent(stripData));
        }

        private static string GetHtmlNotification(List<(string src, string title)> stripData)
        {
            return $"<html><body>{GetHtmlNotificationBodyContent(stripData)}</body></html>";
        }

        private static string GetHtmlNotificationBodyContent(List<(string src, string title)> stripData)
        {
            return string.Join("<br />", stripData.Select(x => GetHtmlForStrip(x.src, x.title)));
        }

        private static string GetHtmlForStrip(string imgSrc, string imgTitle)
        {
            return $"<p><strong><i>{imgTitle}</i></strong><hr /><img src='{imgSrc}' /></p>";
        }
    }
}