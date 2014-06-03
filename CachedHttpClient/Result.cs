namespace CachedHttpClient
{
    using System.Net;

    /// <summary>
    /// The result of a request.
    /// </summary>
    public class Result
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Result"/> class.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="status">The status.</param>
        /// <param name="wasCached">if set to <c>true</c> [was cached].</param>
        public Result(string body, HttpStatusCode status, bool wasCached)
        {
            this.Body = body;
            this.StatusCode = status;
            this.WasCached = wasCached;
        }

        /// <summary>
        /// Gets the body of the message.
        /// </summary>
        /// <value>
        /// The body.
        /// </value>
        public string Body { get; private set; }

        /// <summary>
        /// Gets the HTTP status code.
        /// </summary>
        /// <value>
        /// The status code.
        /// </value>
        public HttpStatusCode StatusCode { get; private set; }
        
        /// <summary>
        /// Gets a value indicating whether the request was served from cache.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [was cached]; otherwise, <c>false</c>.
        /// </value>
        public bool WasCached { get; private set; }
    }
}