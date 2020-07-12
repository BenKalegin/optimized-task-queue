using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OptimizedTaskQueue;

namespace TestHeavyWait
{
    public readonly struct ApiResponse
    {
        public ApiResponse(HttpStatusCode httpResponseCode, string content)
        {
            HttpResponseCode = httpResponseCode;
            Content = content;
        }

        public HttpStatusCode HttpResponseCode { get; }

        public string Content { get; }
    }

    public interface ISomeApiService
    {
        Task<ApiResponse> Invoke(string payload, CancellationToken abortSignal);
    }

    public class SomeApiService : ISomeApiService
    {
        private readonly Uri uri;
        private readonly ILog log;

        private static HttpClient httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = delegate { return true; }
        });

        public SomeApiService(Uri uri, ILog log)
        {
            this.uri = uri;
            this.log = log;
            ServicePointManager.DefaultConnectionLimit = 10000;
            httpClient = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = delegate { return true; }
            })
            {
                BaseAddress = uri
            };
        }

        public async Task<ApiResponse> Invoke(string payload, CancellationToken abortSignal)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            var response = await httpClient.SendAsync(request, abortSignal);

            string content;
            try
            {
                // TODO log invocation times and consider retries on some codes.
                content = await response.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                // Handler logic is based on the http status code.
                // we don't want to lost this result because of some network issue
                // Ignore exception and continue
                log.Warning("Error downloading message content. Ignored.");
                content = e.Message;
            }

            return new ApiResponse(response.StatusCode, content);
        }
    }
}