namespace CachedHttpClient
{
    using System.Net;

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
}