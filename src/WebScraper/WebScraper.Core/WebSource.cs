using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace WebScraper.Core
{
    public class WebSource
    {
        private string _baseUrl;
        public WebSource(string baseUrl)
        {
            _baseUrl = baseUrl?.Trim('/') ?? "+";
        }

        public async Task<string> GetResponse(params string[] address)
        {
            var uri = GetAddress(address);
            var httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            string body = await response.Content.ReadAsStringAsync();
            return body;
        }

        private Uri GetAddress(params string[] uri)
        {
            var relativeComponent = string.Join("/", uri.Select(x => x.Trim('/')));
            return new Uri($"{_baseUrl}/{relativeComponent}");
        }
    }
}
