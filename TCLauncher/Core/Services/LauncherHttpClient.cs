using System;
using System.Net;
using System.Net.Http;

namespace TCLauncher.Core.Services
{
    public static class LauncherHttpClient
    {
        private static readonly Lazy<HttpClient> LazyClient = new Lazy<HttpClient>(Create);
        public static HttpClient Instance => LazyClient.Value;

        private static HttpClient Create()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TCLauncher-Windows/1.0");
            return client;
        }
    }
}