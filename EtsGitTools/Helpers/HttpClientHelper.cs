using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EtsGitTools
{
    public class HttpClientHelper
    {
        private const string GITHUB_API_BASE_URL = "https://api.github.com/";
        private const string SONARCLOUD_API_BASE_URL = "https://sonarcloud.io/api/";

        public static HttpClient CreateGitHubHttpClient(string token)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(GITHUB_API_BASE_URL, UriKind.Absolute);
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.110 Safari/537.36");
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Add("Authorization", $"token {token}");
            return client;
        }

        public static HttpClient CreateSonarCloudHttpClient()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(SONARCLOUD_API_BASE_URL, UriKind.Absolute);
            return client;
        }
    }
}
