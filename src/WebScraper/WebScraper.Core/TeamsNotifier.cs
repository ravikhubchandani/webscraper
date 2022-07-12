using System.Net.Http;
using System.Text;

namespace WebScraper.Core
{
    public class TeamsNotifier : INotifier
    {
        private readonly string _hookUrl;

        public TeamsNotifier(string hookUrl)
        {
            _hookUrl = hookUrl;
        }

        public void Push(string content)
        {
            var body = new StringContent(content, Encoding.UTF8, "application/json");
            HttpClient client = new HttpClient();
            _ = client.PostAsync(_hookUrl, body).Result;
        }
    }
}