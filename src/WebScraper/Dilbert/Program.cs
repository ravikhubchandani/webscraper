using System;
using System.Globalization;
using System.Linq;
using WebScraper.Core;

namespace WebScraper.Dilbert
{
    class Program
    {
        static void Main(string[] args)
        {
            WebSource source = new WebSource("https://dilbert.com/");
            DateTime when = DateTime.Today;
            string body = source.GetResponse("strip", when.ToString("yyyy-MM-dd")).Result;
            HtmlContentParser parser = new HtmlContentParser(body);
            var dom = parser.GetFirstChild("div.img-comic-container img");
            var element = dom.FirstOrDefault();
            if (element != null)
            {
                var notifier = new EmailNotifier { EmailSubject = "Dilbert " + when.ToString("dddd dd", CultureInfo.GetCultureInfo("es-ES"))  };
                notifier.AddReciever("ravirajkhubchandani@gmail.com");
                string alt = element.Attributes["alt"].Value;
                string src = element.Attributes["src"].Value.Trim('/');
                string msg = GetHtmlNotification(src, alt);
                notifier.Push(msg);
            }
        }

        private static string GetHtmlNotification(string imgSrc, string imgTitle)
        {
            return $"<html><body><img src='{imgSrc}'><p>{imgTitle}</p></body></html>";
        }
    }
}
