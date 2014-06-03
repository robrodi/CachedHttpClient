namespace CachedHttpClient.Tests
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.Caching;
    using System.Threading;
    using System.Threading.Tasks;

    using FluentAssertions;

    using Microsoft.Owin;
    using Microsoft.Owin.Testing;

    using Moq;

    using Owin;

    using Xunit;

    public class Result
    {
        public Result(string body, HttpStatusCode status, bool wasCached)
        {
            this.Body = body;
            this.StatusCode = status;
            this.WasCached = wasCached;
        }

        public string Body { get; private set; }
        public HttpStatusCode StatusCode { get; private set; }
        public bool WasCached { get; private set; }
    }
    public class CachedHttpClient
    {
        private static readonly ObjectCache _cache;
        private readonly ObjectCache _instanceCache;

        static CachedHttpClient()
        {
            _cache = new MemoryCache("CachedHttp");
        }

        private readonly HttpClient client;

        public CachedHttpClient()
            : this(new HttpClient())
        {
        }

        public CachedHttpClient(HttpClient client, ObjectCache objectCache = null)
        {
            this.client = client;
            this._instanceCache = objectCache ?? _cache;
        }

        private ObjectCache cache {
            get
            {
                return this._instanceCache ?? Cache;
            }
        }

        public static ObjectCache Cache
        {
            get
            {
                return _cache;
            }
        }

        public Task<Result> GetAsync(string requestUri)
        {
            return this.GetAsync(requestUri, CancellationToken.None);
        }
        public async Task<Result> GetAsync(string requestUri, CancellationToken cancellationToken)
        {
            if (cache.Contains(requestUri))
            {
                return new Result((string) cache[requestUri], HttpStatusCode.OK, true);
            }

            var result = await this.client.GetAsync(requestUri, cancellationToken);
            if (result.StatusCode == HttpStatusCode.OK)
            {
                if (result.Headers.CacheControl != null && !result.Headers.CacheControl.Private && result.Headers.CacheControl.MaxAge.HasValue && result.Headers.CacheControl.MaxAge > TimeSpan.Zero)
                {

                    cache.Set(new CacheItem(requestUri, result.Content.ToString()), new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.Add(result.Headers.CacheControl.MaxAge.Value) });
                }
            }

            return new Result(result.Content.ToString(), result.StatusCode, false);
        }
    }

    [Trait("Basic Http Client", "")]
    public class Tests : IDisposable
    {
        private TestServer server;

        public Tests()
        {
            this.server = SingleApiServer(12);
        }

        [Fact(DisplayName = "Connects to an Owin Serivce")]
        public void Connect()
        {
            var client = new CachedHttpClient(this.server.HttpClient);
            var result = client.GetAsync("/").Result;
            result.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact(DisplayName = "Caches subsequent Requests")]
        public void CachedConnect()
        {
            CachedHttpClient.Cache.Remove("/");

            var client = new CachedHttpClient(this.server.HttpClient);
            var result = client.GetAsync("/").Result;
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.WasCached.Should().BeFalse("should not yet be cached.");

            CachedHttpClient.Cache.GetCount().Should().Be(1);
            CachedHttpClient.Cache["/"].Should().NotBeNull("Should contain entry");

            var result2 = client.GetAsync("/").Result;
            result2.StatusCode.Should().Be(HttpStatusCode.OK, "Should be ok");
            result2.WasCached.Should().BeTrue("should be cached");
        }

        [Fact(DisplayName = "Does not cache Max-Age of Zero")]
        public void DoNotCacheMaxAgeOfZero()
        {
            using (var testServer = SingleApiServer(0))
            {
                var mockCache = new Mock<ObjectCache>(MockBehavior.Strict);
                mockCache.Setup(m => m.Contains("/", null)).Returns(false);
                var client = new CachedHttpClient(testServer.HttpClient, mockCache.Object);
                var result = client.GetAsync("/").Result;
                result.StatusCode.Should().Be(HttpStatusCode.OK);
                result.WasCached.Should().BeFalse("should not be cached if time = 0.");
            }
        }

        [Fact(DisplayName = "Caches subsequent Requests for the duration of cache control")]
        public void CachedConnectRespectsCacheControl()
        {
            using (var testServer = SingleApiServer(1))
            {

                var mockCache = new Mock<ObjectCache>(MockBehavior.Strict);
                //cache.Set(new CacheItem(requestUri, result.Content.ToString()), new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.Add(result.Headers.CacheControl.MaxAge.Value) });
                mockCache.Setup(mc => mc.Contains("/", null)).Returns(false);
                mockCache.Setup(m => m.Set(It.IsAny<CacheItem>(), It.Is<CacheItemPolicy>(cip => cip.AbsoluteExpiration < DateTimeOffset.UtcNow.AddMilliseconds(1001)&& cip.AbsoluteExpiration > DateTimeOffset.UtcNow.AddMilliseconds(900)))).Verifiable("Didn't add to cache");
                
                var client = new CachedHttpClient(testServer.HttpClient, mockCache.Object);
                var result = client.GetAsync("/").Result;
                result.StatusCode.Should().Be(HttpStatusCode.OK);
                result.WasCached.Should().BeFalse("should not yet be cached.");
                mockCache.Verify();
                
                mockCache.Setup(mc => mc.Contains("/", null)).Returns(true);
                mockCache.Setup(mc => mc["/"]).Returns(null);
                
                var result2 = client.GetAsync("/").Result;
                result.StatusCode.Should().Be(HttpStatusCode.OK);
                result.WasCached.Should().BeFalse("should not yet be cached.");
            }
        }

        private static TestServer SingleApiServer(int maxAge)
        {
            return TestServer.Create(app => app.Run(GetHandler(maxAge)));
        }

        private static Func<IOwinContext, Task> GetHandler(int maxAge)
        {
            return context =>
            {
                context.Response.Headers["cache-control"] = "max-age=" + maxAge;
                return context.Response.WriteAsync("Hello world using OWIN TestServer");
            };
        }

        public void Dispose()
        {
            if (this.server != null) this.server.Dispose();
        }
    }
}
