namespace AzureComicStripFetcherFunction;

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AngleSharp.Dom;
using WebScraper.Core;
using System.Globalization;

public class ComicStripFetcher
{
    private readonly ILogger<ComicStripFetcher> _logger;

    public ComicStripFetcher(ILogger<ComicStripFetcher> log)
    {
        _logger = log;
    }

    [FunctionName("ComicStripFetcher")]
    [OpenApiOperation(operationId: "Run", tags: new[] { "date" })]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter(name: "date", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The date parameter yyyy-MM-dd")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string dateStr = req.Query["date"];
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            var date = TryParseDateTimeOrDefault(dateStr ?? data?.name, DateTime.Now);

            string[] comics = { "garfield", "adult-children", "peanuts", "9to5", "calvinandhobbes", "wtduck", "shen-comix" };
            var stripCollection = new List<(string src, string title)>();
            AddDilbert(stripCollection, DateTime.Today);
            AddGoComicsStrip(stripCollection, date, comics);
            string responseMessage = GetHtmlNotification(stripCollection);
            return new OkObjectResult(responseMessage);
        }
        catch (Exception e)
        {
            return new BadRequestObjectResult(e.Message);
        }
    }

    private static DateTime TryParseDateTimeOrDefault(string value, DateTime @default)
    {
        if(DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }
        return @default;
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

