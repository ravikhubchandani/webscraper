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
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/html", bodyType: typeof(string), Description = "The OK response")]
    public async Task<IActionResult> Run(
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

    private static string GenerateComicStripsHtmlContentForDate(DateTime date)
    {
        string[] comics = { "garfield", "adult-children", "peanuts", "9to5", "calvinandhobbes", "wtduck", "shen-comix" };
        var stripCollection = new List<(string src, string title)>();
        StripContentConentGenerator.AddDilbert(stripCollection, DateTime.Today);
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

    private static DateTime TryParseDateTimeOrDefault(string value, DateTime @default)
    {
        if(DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }
        return @default;
    }
}
