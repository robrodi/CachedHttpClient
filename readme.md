# Cached Http Client
Decorator(mostly) on top of HttpClient that simply adds an ObjectCache to cache successful requests based on the the max-age of the response.
Presently limited to Text, since I'm lazy.

Tested w/ xUnit and a niftly little Owin server.

## Example
````csharp
var client = new CachedHttpClient();
var result = client.GetAsync(new Uri("http://github.com"));
````