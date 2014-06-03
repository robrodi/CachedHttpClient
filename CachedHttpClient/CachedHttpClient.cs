namespace CachedHttpClient
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.Caching;
    using System.Threading;
    using System.Threading.Tasks;

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
            if (this.cache.Contains(requestUri))
            {
                return new Result((string) this.cache[requestUri], HttpStatusCode.OK, true);
            }

            var result = await this.client.GetAsync(requestUri, cancellationToken);
            if (result.StatusCode == HttpStatusCode.OK)
            {
                if (result.Headers.CacheControl != null && !result.Headers.CacheControl.Private && result.Headers.CacheControl.MaxAge.HasValue && result.Headers.CacheControl.MaxAge > TimeSpan.Zero)
                {

                    this.cache.Set(new CacheItem(requestUri, result.Content.ToString()), new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.Add(result.Headers.CacheControl.MaxAge.Value) });
                }
            }

            return new Result(result.Content.ToString(), result.StatusCode, false);
        }
    }
}