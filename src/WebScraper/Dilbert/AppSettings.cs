namespace WebScraper.DailyStrips
{
    public class AppSettings
    {
        public string[] GoComicsStrips { get; set; }
        public string TeamsWebhookUri { get; set; }
        public string[] EmailAddressTo { get; set; }
        public string AzureAcsConnectionString { get; set; }
        public string AzureAcsSenderEmail { get; set; }
    }
}
