namespace AzureFunctions;

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;
using WebScraper.Core;

public class ComicStripFetcher
{
    private readonly ILogger<ComicStripFetcher> _logger;
    private const string _azureAcsConnectionStringKeyVaultEntry = "AzureAcsConnectionString";
    private const string _azureAcsSenderEmailKeyVaultEntry = "AzureAcsSenderEmail";

    public ComicStripFetcher(ILogger<ComicStripFetcher> log)
    {
        _logger = log;
    }

    [FunctionName("ComicStripWeb")]
    public async Task<IActionResult> RunWeb(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var date = await GetDateFromQueryOrDefaultAsync(req, DateTime.Now);
            string responseMessage = GenerateComicStripsHtmlContentForDate(date);

            return new ContentResult
            {
                ContentType = "text/html",
                Content = responseMessage
            };
        }
        catch (Exception e)
        {
            return new BadRequestObjectResult(e.Message);
        }
    }

    [FunctionName("ComicStripMailer")]
    public async Task<IActionResult> RunMailer(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var receiveremail = await GetReceiversEmailsFromQueryOrDefaultAsync(req, string.Empty);
            string resultMessage = string.Empty;

            if(receiveremail == null || receiveremail.Length == 0)
            {
                resultMessage = "Email not sent. Please add email address (or multiple emails separated by ; ) as receiveremail parameter in query string";
            }
            else
            {
                var date = await GetDateFromQueryOrDefaultAsync(req, DateTime.Now);
                string responseMessage = GenerateComicStripsHtmlContentForDate(date);

                var notifier = new EmailNotifier(
                    azureAcsConnectionString: Environment.GetEnvironmentVariable(_azureAcsConnectionStringKeyVaultEntry, EnvironmentVariableTarget.Process),
                    azureAcsSenderEmail: Environment.GetEnvironmentVariable(_azureAcsSenderEmailKeyVaultEntry, EnvironmentVariableTarget.Process))
                {
                    EmailSubject = "Daily strips - " + DateTime.Today.ToString("dddd dd", CultureInfo.GetCultureInfo("es-ES"))
                };

                notifier.AddReciever(receiveremail);
                notifier.Push(responseMessage);
                resultMessage = $"Email sent to {receiveremail}";
            }

            return new ContentResult
            {
                ContentType = "text/html",
                Content = $"<html><body>{resultMessage}</body></html>"
            };
        }
        catch (Exception e)
        {
            return new BadRequestObjectResult(e.Message);
        }
    }

    private static string GenerateComicStripsHtmlContentForDate(DateTime date)
    {
        string[] comics = { "garfield", "adult-children", "peanuts", "9to5", "calvinandhobbes", "bc", "ziggy" };
        var stripCollection = new List<(string src, string title)>();
        
        // Apparently Dilbert is not available anymore
        // StripContentConentGenerator.AddDilbert(stripCollection, DateTime.Today);
        StripContentConentGenerator.AddGoComicsStrip(stripCollection, date, comics);
        return StripContentConentGenerator.GetComicStripHtmlContent(stripCollection);
    }

    private async static Task<DateTime> GetDateFromQueryOrDefaultAsync(HttpRequest req, DateTime @default)
    {
        string dateStr = req.Query["date"];
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);
        return TryParseDateTimeOrDefault(dateStr ?? data?.name, @default);
    }

    private async static Task<string[]> GetReceiversEmailsFromQueryOrDefaultAsync(HttpRequest req, string @default)
    {
        string email = req.Query["receiveremail"];
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);
        string emailsCsv = email ?? data?.email ?? @default;
        return emailsCsv == null ? Array.Empty<string>() : emailsCsv.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static DateTime TryParseDateTimeOrDefault(string value, DateTime @default)
    {
        if(DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }
        return @default;
    }
}
