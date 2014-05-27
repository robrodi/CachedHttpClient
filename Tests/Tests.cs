namespace CachedHttpClient.Tests
{
    using System.Net.Http;
    using System.Runtime.Remoting.Channels;

    using Xunit;

    public class CachedHttpClient
    {
        private readonly HttpClient client = new HttpClient();
    }

    [Trait("Basic Http Client", "")]
    public class Tests
    {
        [Fact(DisplayName = "Connects to an external Serivce")]
        public void Connect()
        {
            var client = new CachedHttpClient();

            
        }
    }
}
