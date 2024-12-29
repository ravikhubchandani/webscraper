using System.Collections.Generic;
using System;
using AngleSharp.Dom;
using WebScraper.Core;
using System.Linq;
using System.Threading;

public static class StripContentConentGenerator
{
    public static void AddDilbert(List<(string src, string title)> stripCollection, DateTime when)
    {
        string source = "https://dilbert.com/";
        string selector = "div.img-comic-container img";
        string[] route = { "strip", when.ToString("yyyy-MM-dd") };
        GetStripFromSourceAndAddToCollection(stripCollection, source, selector, route, "src", "alt");
    }

    public static void AddGoComicsStrip(List<(string src, string title)> stripCollection, DateTime when, params string[] codes)
    {
        string source = "https://gocomics.com/";
        string selector = "div.comic__image img.img-fluid";
        foreach (string code in codes)
        {
            string[] route = { code, when.ToString("yyyy"), when.ToString("MM"), when.ToString("dd") };
            GetStripFromSourceAndAddToCollection(stripCollection, source, selector, route, "src", "alt");
        }
    }

    public static string GetComicStripHtmlContent(List<(string src, string title)> stripData)
    {
        return $"<html><body>{GetComicStripHtmlBodyContent(stripData)}</body></html>";
    }

    public static string GetComicStripHtmlBodyContent(List<(string src, string title)> stripData)
    {
        return string.Join("<br />", stripData.Select(x => GetHtmlForStrip(x.src, x.title)));
    }

    private static string GetHtmlForStrip(string imgSrc, string imgTitle)
    {
        return $"<p><strong><i>{imgTitle}</i></strong><hr /><img src='{imgSrc}' /></p>";
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
            catch
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
}