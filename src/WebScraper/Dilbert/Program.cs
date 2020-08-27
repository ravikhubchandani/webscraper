using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WebScraper.Core;

namespace WebScraper.DailyStrips
{
    class Program
    {
        static void Main(string[] args)
        {
            var stripCollection = new List<(string src, string title)>();
            AddDilbert(stripCollection, DateTime.Today);
            AddGoComicsStrip(stripCollection, DateTime.Today, "garfield", "adult-children", "peanuts", "9to5", "calvinandhobbes", "wtduck", "shen-comix");
            string msg = GetHtmlNotification(stripCollection);
            SendEmailNotification(msg);
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
            string body = webSource.GetResponse(route).Result;
            HtmlContentParser parser = new HtmlContentParser(body);
            var dom = parser.GetFirstChild(selector);
            return dom.FirstOrDefault();
        }

        private static (string, string) GetStripData(IElement domElement, string imgAttr, string titleAttr)
        {            
            string title = domElement.Attributes[titleAttr]?.Value ?? string.Empty;
            string img = domElement.Attributes[imgAttr]?.Value?.Trim('/') ?? string.Empty;
            return (img, title);
        }

        private static string GetHtmlNotification(List<(string src, string title)> stripData)
        {
            return $"<html><body>{string.Join("<hr />", stripData.Select(x => GetHtmlForStrip(x.src, x.title)))}</body></html>";
        }

        private static string GetHtmlForStrip(string imgSrc, string imgTitle)
        {
            return $"<p><i>{imgTitle}</i></p><img src='{imgSrc}' />";
        }
    }
}
