namespace CachedHttpClient.Tests
{
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using FluentAssertions;

    using Microsoft.Owin.Testing;

    using Owin;

    using Xunit;

    public class CachedHttpClient
    {
        private readonly HttpClient client;

        public CachedHttpClient()
            : this(new HttpClient())
        {
        }

        public CachedHttpClient(HttpClient client)
        {
            this.client = client;
        }

        public Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            return this.GetAsync(requestUri, CancellationToken.None);
        }
        public Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken)
        {
            return this.client.GetAsync(requestUri, cancellationToken);
        }
    }

    [Trait("Basic Http Client", "")]
    public class Tests
    {
        [Fact(DisplayName = "Connects to an external Serivce")]
        public void Connect()
        {
            using (var server = TestServer.Create(
                app => app.Run(
                    context =>
                    {
                        context.Response.Headers["cache-control"] = "max-age=12, private";
                        return context.Response.WriteAsync("Hello world using OWIN TestServer");
                    })))
            {
                var client = new CachedHttpClient(server.HttpClient);
                var result = client.GetAsync("/").Result;

                result.StatusCode.Should().Be(HttpStatusCode.OK);
                result.Headers.CacheControl.ToString().Should().Be("max-age=12, private");
            }

        }
    }
}
