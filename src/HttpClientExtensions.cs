using System.Threading.Tasks;
using VadRsa.Extensions.Http;

namespace System.Net.Http
{
    public static class HttpClientExtensions
    {
        public static Task<HttpDownloadResponse> DownloadAsync(this HttpClient client, Func<HttpRequestMessage> requestFactory, HttpDownloadOptions options = null)
            => new HttpDownloader(client, requestFactory, options).DownloadAsync();

        public static Task<HttpDownloadResponse> DownloadAsync(this HttpClient client, Uri uri, HttpDownloadOptions options = null)
            => client.DownloadAsync(() => new HttpRequestMessage(HttpMethod.Get, uri), options);
        
        public static Task<HttpDownloadResponse> DownloadAsync(this HttpClient client, string url, HttpDownloadOptions options = null)
            => client.DownloadAsync(() => new HttpRequestMessage(HttpMethod.Get, CreateUri(url)), options);
        private static Uri CreateUri(string uri) =>
            string.IsNullOrEmpty(uri) ? null : new Uri(uri, UriKind.RelativeOrAbsolute);
    }
}