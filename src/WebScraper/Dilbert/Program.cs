using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WebScraper.Core;

namespace WebScraper.DailyStrips
{
    public class Program
    {
        static void Main(string[] args)
        {
            const bool PUSH_TO_EMAIL = false;
            const bool PUSH_TO_TEAMS = true;

            var stripCollection = new List<(string src, string title)>();
            AddDilbert(stripCollection, DateTime.Today);
            AddGoComicsStrip(stripCollection, DateTime.Today, "garfield", "adult-children", "peanuts", "9to5", "calvinandhobbes", "wtduck", "shen-comix");

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

        private static void SendTeamsNotification(string msg)
        {
            // This would need an actual id string.
            // TO DO Move this string id to configuration file
            var notifier = new TeamsNotifier("https://outlook.office.com/webhook/WEBHOOK-ID");
            notifier.Push(msg);
        }

        private static void SendEmailNotification(string msg)
        {
            var notifier = new EmailNotifier { EmailSubject = "Daily strips - " + DateTime.Today.ToString("dddd dd", CultureInfo.GetCultureInfo("es-ES")) };
            notifier.AddReciever("ravirajkhubchandani@gmail.com");
            notifier.Push(msg);
        }

        private static void AddDilbert(List<(string src, string title)> stripCollection, DateTime when)
        {
            string source = "https://dilbert.com/";
            string selector = "div.img-comic-container img";
            string[] route = { "strip", when.ToString("yyyy-MM-dd") };
            IElement domElement = GetDomForStrip(source, selector, route);
            (string, string) stripData = GetStripData(domElement, "src", "alt");
            stripCollection.Add(stripData);
        }

        private static void AddGoComicsStrip(List<(string src, string title)> stripCollection, DateTime when, params string[] codes)
        {
            string source = "https://gocomics.com/";
            string selector = "div.comic__image img.img-fluid";
            foreach (string code in codes)
            {
                string[] route = { code, when.ToString("yyyy"), when.ToString("MM"), when.ToString("dd") };
                IElement domElement = GetDomForStrip(source, selector, route);
                if (domElement != null)
                {
                    (string, string) stripData = GetStripData(domElement, "src", "alt");
                    stripCollection.Add(stripData);
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