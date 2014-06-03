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

        /// <summary>
        /// Initializes the <see cref="CachedHttpClient"/> class.
        /// </summary>
        static CachedHttpClient()
        {
            _cache = new MemoryCache("CachedHttp");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedHttpClient"/> class.
        /// </summary>
        public CachedHttpClient()
            : this(new HttpClient())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedHttpClient"/> class.
        /// Note: this is primaraliy used for testing.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="objectCache">The object cache.</param>
        internal CachedHttpClient(HttpClient client, ObjectCache objectCache = null)
        {
            this.client = client;
            this._instanceCache = objectCache ?? _cache;
        }

        /// <summary>
        /// Gets the cache. First uses the instance cache, if available, then falls over to static.
        /// </summary>
        /// <value>
        /// The cache.
        /// </value>
        private ObjectCache cache
        {
            get
            {
                return this._instanceCache ?? Cache;
            }
        }

        /// <summary>
        /// Gets the cache.
        /// </summary>
        /// <value>
        /// The cache.
        /// </value>
        internal static ObjectCache Cache
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

        /// <summary>
        /// Send a POST request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// 
        /// <returns>
        /// Returns <see cref="T:System.Threading.Tasks.Task`1"/>.The task object representing the asynchronous operation.
        /// </returns>
        /// <param name="requestUri">The Uri the request is sent to.</param><param name="content">The HTTP request content sent to the server.</param><exception cref="T:System.ArgumentNullException">The <paramref name="requestUri"/> was null.</exception>
        public Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content)
        {
            return this.PostAsync(requestUri, content, CancellationToken.None);
        }

        /// <summary>
        /// Send a POST request with a cancellation token as an asynchronous operation.
        /// </summary>
        /// 
        /// <returns>
        /// Returns <see cref="T:System.Threading.Tasks.Task`1"/>.The task object representing the asynchronous operation.
        /// </returns>
        /// <param name="requestUri">The Uri the request is sent to.</param><param name="content">The HTTP request content sent to the server.</param><param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param><exception cref="T:System.ArgumentNullException">The <paramref name="requestUri"/> was null.</exception>
        public async Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            var result = await client.PostAsync(requestUri, content, cancellationToken);
            ClearCacheKeyIfSuccessful(requestUri, result, this.cache);
            return result;
        }

        /// <summary>
        /// Send a PUT request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// 
        /// <returns>
        /// Returns <see cref="T:System.Threading.Tasks.Task`1"/>.The task object representing the asynchronous operation.
        /// </returns>
        /// <param name="requestUri">The Uri the request is sent to.</param><param name="content">The HTTP request content sent to the server.</param><exception cref="T:System.ArgumentNullException">The <paramref name="requestUri"/> was null.</exception>
       public Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content)
        {
            return this.PutAsync(requestUri, content, CancellationToken.None);
        }

       /// <summary>
       /// Send a PUT request with a cancellation token as an asynchronous operation.
       /// </summary>
       /// 
       /// <returns>
       /// Returns <see cref="T:System.Threading.Tasks.Task`1"/>.The task object representing the asynchronous operation.
       /// </returns>
       /// <param name="requestUri">The Uri the request is sent to.</param><param name="content">The HTTP request content sent to the server.</param><param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param><exception cref="T:System.ArgumentNullException">The <paramref name="requestUri"/> was null.</exception>
        public async Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            var result = await client.PostAsync(requestUri, content, cancellationToken);
            ClearCacheKeyIfSuccessful(requestUri, result, this.cache);
            return result;
        }

        /// <summary>
        /// Send a DELETE request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// 
        /// <returns>
        /// Returns <see cref="T:System.Threading.Tasks.Task`1"/>.The task object representing the asynchronous operation.
        /// </returns>
        /// <param name="requestUri">The Uri the request is sent to.</param><exception cref="T:System.ArgumentNullException">The <paramref name="requestUri"/> was null.</exception><exception cref="T:System.InvalidOperationException">The request message was already sent by the <see cref="T:System.Net.Http.HttpClient"/> instance.</exception>
        public Task<HttpResponseMessage> DeleteAsync(Uri requestUri)
        {
            return this.DeleteAsync(requestUri, CancellationToken.None);
        }

        /// <summary>
        /// Send a DELETE request to the specified Uri with a cancellation token as an asynchronous operation.
        /// </summary>
        /// 
        /// <returns>
        /// Returns <see cref="T:System.Threading.Tasks.Task`1"/>.The task object representing the asynchronous operation.
        /// </returns>
        /// <param name="requestUri">The Uri the request is sent to.</param><param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param><exception cref="T:System.ArgumentNullException">The <paramref name="requestUri"/> was null.</exception><exception cref="T:System.InvalidOperationException">The request message was already sent by the <see cref="T:System.Net.Http.HttpClient"/> instance.</exception>
        public async Task<HttpResponseMessage> DeleteAsync(Uri requestUri, CancellationToken cancellationToken)
        {
            var result = await client.DeleteAsync(requestUri, cancellationToken);
            ClearCacheKeyIfSuccessful(requestUri, result, this.cache);
            return result;
        }

        /// <summary>
        /// Cancel all pending requests on this instance.
        /// </summary>
        public void CancelPendingRequests()
        {
            client.CancelPendingRequests();
        }

        private static void ClearCacheKeyIfSuccessful(Uri requestUri, HttpResponseMessage result, ObjectCache cache)
        {
            if (result.IsSuccessStatusCode)
            {
                cache.Remove(requestUri.ToString());
            }
        }
    }
}