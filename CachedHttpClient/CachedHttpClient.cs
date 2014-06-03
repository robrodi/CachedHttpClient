namespace CachedHttpClient
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.Caching;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Uses <see cref="HttpClient"/> to make requests, and caches requests using a .net <see cref="ObjectCache"/>.
    /// </summary>
    public class CachedHttpClient
    {
        private readonly HttpClient client;

        private static readonly ObjectCache _cache;

        /// <summary>
        /// Mostly used for testing.
        /// </summary>
        private readonly ObjectCache _instanceCache;

        static CachedHttpClient()
        {
            _cache = new MemoryCache("CachedHttp");
        }

        public CachedHttpClient()
            : this(new HttpClient())
        {
        }

        public CachedHttpClient(HttpClient client, ObjectCache objectCache = null)
        {
            this.client = client;
            this._instanceCache = objectCache ?? _cache;
        }

        private ObjectCache cache
        {
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

        /// <summary>
        /// Send a GET request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// 
        /// <returns>
        /// Returns <see cref="T:System.Threading.Tasks.Task`1"/>.The task object representing the asynchronous operation.
        /// </returns>
        /// <param name="requestUri">The Uri the request is sent to.</param><exception cref="T:System.ArgumentNullException">The <paramref name="requestUri"/> was null.</exception>
        public Task<Result> GetAsync(Uri requestUri)
        {
            return this.GetAsync(requestUri, CancellationToken.None);
        }

        /// <summary>
        /// Send a GET request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// 
        /// <returns>
        /// Returns <see cref="T:System.Threading.Tasks.Task`1"/>.The task object representing the asynchronous operation.
        /// </returns>
        /// <param name="requestUri">The Uri the request is sent to.</param><exception cref="T:System.ArgumentNullException">The <paramref name="requestUri"/> was null.</exception>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        public async Task<Result> GetAsync(Uri requestUri, CancellationToken cancellationToken)
        {
            if (this.cache.Contains(requestUri.ToString()))
            {
                return new Result((string) this.cache[requestUri.ToString()], HttpStatusCode.OK, true);
            }

            var result = await this.client.GetAsync(requestUri, cancellationToken);
            if (result.StatusCode == HttpStatusCode.OK)
            {
                if (result.Headers.CacheControl != null && !result.Headers.CacheControl.Private && result.Headers.CacheControl.MaxAge.HasValue && result.Headers.CacheControl.MaxAge > TimeSpan.Zero)
                {

                    this.cache.Set(new CacheItem(requestUri.ToString(), result.Content.ToString()), new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.Add(result.Headers.CacheControl.MaxAge.Value) });
                }
            }

            return new Result(result.Content.ToString(), result.StatusCode, false);
        }

        public Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content)
        {
            return this.PostAsync(requestUri, content, CancellationToken.None);
        }

        public async Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            var result = await client.PostAsync(requestUri, content, cancellationToken);
            this.ClearCacheKeyIfSuccessful(requestUri, result);
            return result;
        }

        private void ClearCacheKeyIfSuccessful(Uri requestUri, HttpResponseMessage result)
        {
            if (result.IsSuccessStatusCode)
            {
                this.cache.Remove(requestUri.ToString());
            }
        }

        public Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content)
        {
            return this.PutAsync(requestUri, content, CancellationToken.None);
        }

        public async Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            var result = await client.PostAsync(requestUri, content, cancellationToken);
            this.ClearCacheKeyIfSuccessful(requestUri, result);
            return result;
        }

        public Task<HttpResponseMessage> DeleteAsync(Uri requestUri)
        {
            return this.DeleteAsync(requestUri, CancellationToken.None);
        }

        public async Task<HttpResponseMessage> DeleteAsync(Uri requestUri, CancellationToken cancellationToken)
        {
            var result = await client.DeleteAsync(requestUri, cancellationToken);
            this.ClearCacheKeyIfSuccessful(requestUri, result);
            return result;
        }

        public void CancelPendingRequests()
        {
            client.CancelPendingRequests();
        }
    }
}