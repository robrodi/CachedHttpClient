﻿namespace CachedHttpClient.Tests
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.Caching;
    using System.Threading.Tasks;

    using FluentAssertions;

    using Microsoft.Owin;
    using Microsoft.Owin.Testing;

    using Moq;

    using Owin;

    using Xunit;

    [Trait("Basic Http Client", "")]
    public class Tests : IDisposable
    {
        private readonly TestServer server;

        private const string IndexPath = "/";
        private static readonly Uri IndexUri = new Uri(IndexPath, UriKind.Relative);

        public Tests()
        {
            this.server = SingleApiServer(12);
        }

        [Fact(DisplayName = "Connects to an Owin Serivce")]
        public void Connect()
        {
            var client = new CachedHttpClient(this.server.HttpClient);
            var result = client.GetAsync(IndexUri).Result;
            result.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact(DisplayName = "Caches subsequent Requests")]
        public void CachedConnect()
        {
            CachedHttpClient.Cache.Remove(IndexPath);

            var client = new CachedHttpClient(this.server.HttpClient);
            var result = client.GetAsync(IndexUri).Result;
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.WasCached.Should().BeFalse("should not yet be cached.");

            CachedHttpClient.Cache.GetCount().Should().Be(1);
            CachedHttpClient.Cache[IndexPath].Should().NotBeNull("Should contain entry");

            var result2 = client.GetAsync(IndexUri).Result;
            result2.StatusCode.Should().Be(HttpStatusCode.OK, "Should be ok");
            result2.WasCached.Should().BeTrue("should be cached");
        }

        [Fact(DisplayName = "Caches Separate Uris Separately")]
        public void CachedConnectMultipleUris()
        {
            CachedHttpClient.Cache.Remove(IndexPath);

            var mockCache = new Mock<ObjectCache>();
            //cache.Set(new CacheItem(requestUri, result.Content.ToString()), new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.Add(result.Headers.CacheControl.MaxAge.Value) });
            mockCache.Setup(mc => mc.Contains(IndexPath, null)).Returns(false);
            mockCache.Setup(mc => mc.Contains("/HelloWorld", null)).Returns(false);
            
            mockCache.Setup(m => m.Set(It.IsAny<CacheItem>(), It.Is<CacheItemPolicy>(cip => cip.AbsoluteExpiration < DateTimeOffset.UtcNow.AddMilliseconds(1001) && cip.AbsoluteExpiration > DateTimeOffset.UtcNow.AddMilliseconds(900)))).Verifiable("Didn't add to cache");
            It.Is<CacheItemPolicy>(cip => cip.AbsoluteExpiration < DateTimeOffset.UtcNow.AddMilliseconds(1001) && cip.AbsoluteExpiration > DateTimeOffset.UtcNow.AddMilliseconds(900));
            var client = new CachedHttpClient(this.server.HttpClient);
            var result = client.GetAsync(IndexUri).Result;
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.WasCached.Should().BeFalse("should not yet be cached.");

            var result2 = client.GetAsync(new Uri("/HelloWorld", UriKind.Relative)).Result;
            result2.StatusCode.Should().Be(HttpStatusCode.OK, "Should be ok");
            result2.WasCached.Should().BeFalse("should not be cached");

            CachedHttpClient.Cache.GetCount().Should().Be(2);
            CachedHttpClient.Cache[IndexPath].Should().NotBeNull("Should contain entry");
            CachedHttpClient.Cache["/HelloWorld"].Should().NotBeNull("Should contain entry");
        }

        [Fact(DisplayName = "Does not cache Max-Age of Zero")]
        public void DoNotCacheMaxAgeOfZero()
        {
            using (var testServer = SingleApiServer(0))
            {
                var mockCache = new Mock<ObjectCache>(MockBehavior.Strict);
                mockCache.Setup(m => m.Contains(IndexPath, null)).Returns(false);
                var client = new CachedHttpClient(testServer.HttpClient, mockCache.Object);
                var result = client.GetAsync(IndexUri).Result;
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
                mockCache.Setup(mc => mc.Contains(IndexPath, null)).Returns(false);
                mockCache.Setup(m => m.Set(It.IsAny<CacheItem>(), It.Is<CacheItemPolicy>(cip => cip.AbsoluteExpiration < DateTimeOffset.UtcNow.AddMilliseconds(1001)&& cip.AbsoluteExpiration > DateTimeOffset.UtcNow.AddMilliseconds(900)))).Verifiable("Didn't add to cache");
                
                var client = new CachedHttpClient(testServer.HttpClient, mockCache.Object);
                var result = client.GetAsync(IndexUri).Result;
                result.StatusCode.Should().Be(HttpStatusCode.OK, "Should have succeeded on first request");
                result.WasCached.Should().BeFalse("should not yet be cached.");
                mockCache.Verify();

                mockCache.Setup(mc => mc.Contains(IndexPath, null)).Returns(true);
                mockCache.Setup(mc => mc[IndexPath]).Returns(null);
                
                client.GetAsync(IndexUri).Wait();
                result.StatusCode.Should().Be(HttpStatusCode.OK, "Should have succeeded on second request");
                result.WasCached.Should().BeFalse("should not yet be cached.");
            }
        }

        [Fact(DisplayName = "Deletes purges cache")]
        public void DeleteClearsCache()
        {
            var mockCache = new Mock<ObjectCache>(MockBehavior.Strict);
            //cache.Set(new CacheItem(requestUri, result.Content.ToString()), new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.Add(result.Headers.CacheControl.MaxAge.Value) });
            mockCache.Setup(mc => mc.Contains(IndexPath, null)).Returns(false);
            mockCache.Setup(m => m.Set(It.IsAny<CacheItem>(), It.Is<CacheItemPolicy>(cip => cip.AbsoluteExpiration < DateTimeOffset.UtcNow.AddMilliseconds(1001) && cip.AbsoluteExpiration > DateTimeOffset.UtcNow.AddMilliseconds(900)))).Verifiable("Didn't add to cache");
            mockCache.Setup(m => m.Remove(IndexPath, null)).Returns(null).Verifiable("didn't delete from cache");

            using (var testServer = SingleApiServer(1))
            {
                var client = new CachedHttpClient(testServer.HttpClient, mockCache.Object);
                var result = client.GetAsync(IndexUri).Result;
                result.StatusCode.Should().Be(HttpStatusCode.OK);
                result.WasCached.Should().BeFalse("should not yet be cached.");

                var result2 = client.DeleteAsync(IndexUri).Result;
                mockCache.Verify();
            }
        }

        [Fact(DisplayName = "Post purges cache")]
        public void PostClearsCache()
        {
            var mockCache = new Mock<ObjectCache>(MockBehavior.Strict);
            //cache.Set(new CacheItem(requestUri, result.Content.ToString()), new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.Add(result.Headers.CacheControl.MaxAge.Value) });
            mockCache.Setup(mc => mc.Contains(IndexPath, null)).Returns(false);
            mockCache.Setup(m => m.Set(It.IsAny<CacheItem>(), It.Is<CacheItemPolicy>(cip => cip.AbsoluteExpiration < DateTimeOffset.UtcNow.AddMilliseconds(1001) && cip.AbsoluteExpiration > DateTimeOffset.UtcNow.AddMilliseconds(900)))).Verifiable("Didn't add to cache");
            mockCache.Setup(m => m.Remove(IndexPath, null)).Returns(null).Verifiable("didn't delete from cache");

            using (var testServer = SingleApiServer(1))
            {
                var client = new CachedHttpClient(testServer.HttpClient, mockCache.Object);
                var result = client.GetAsync(IndexUri).Result;
                result.StatusCode.Should().Be(HttpStatusCode.OK);
                result.WasCached.Should().BeFalse("should not yet be cached.");
                var result2 = client.PostAsync(IndexUri, new StringContent("HI")).Result;
                mockCache.Verify();
            }
        }

        [Fact(DisplayName = "Put purges cache")]
        public void PutClearsCache()
        {
            var mockCache = new Mock<ObjectCache>(MockBehavior.Strict);
            //cache.Set(new CacheItem(requestUri, result.Content.ToString()), new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.Add(result.Headers.CacheControl.MaxAge.Value) });
            mockCache.Setup(mc => mc.Contains(IndexPath, null)).Returns(false);
            mockCache.Setup(m => m.Set(It.IsAny<CacheItem>(), It.Is<CacheItemPolicy>(cip => cip.AbsoluteExpiration < DateTimeOffset.UtcNow.AddMilliseconds(1001) && cip.AbsoluteExpiration > DateTimeOffset.UtcNow.AddMilliseconds(900)))).Verifiable("Didn't add to cache");
            mockCache.Setup(m => m.Remove(IndexPath, null)).Returns(null).Verifiable("didn't delete from cache");

            using (var testServer = SingleApiServer(1))
            {
                var client = new CachedHttpClient(testServer.HttpClient, mockCache.Object);
                var result = client.GetAsync(IndexUri).Result;
                result.StatusCode.Should().Be(HttpStatusCode.OK);
                result.WasCached.Should().BeFalse("should not yet be cached.");
                var result2 = client.PutAsync(IndexUri, new StringContent("HI")).Result;
                mockCache.Verify();
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
