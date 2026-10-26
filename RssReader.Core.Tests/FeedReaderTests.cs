
using RssReader.Core;
using Xunit;

namespace RssReader.Core.Tests;

public class FeedReaderTests
{
    [Fact]
    public async Task ReadAsync_WhenResponseHasFinalRequestUri_ResolvesRelativeLinksAgainstIt()
    {
        // Arrange
        const string xml = """
            <feed xmlns="http://www.w3.org/2005/Atom">
                <title>Test Feed</title>
                <entry>
                    <title>Test Article</title>
                    <link href="articles/1" />
                </entry>
            </feed>
            """;

        var handler = new RedirectingHttpMessageHandler(xml);

        using var httpClient = new HttpClient(handler);
        var feedReader = new FeedReader(httpClient);

        // Act
        List<FeedItem> items = await feedReader.ReadAsync(
            "https://example.com/old/feed");

        // Assert
        Assert.Single(items);
        Assert.Equal(
            "https://example.com/new/articles/1",
            items[0].Link);
    }

    private sealed class RedirectingHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _xml;

        public RedirectingHttpMessageHandler(string xml)
        {
            _xml = xml;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(
                System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(
                    _xml,
                    System.Text.Encoding.UTF8,
                    "application/atom+xml"),
                RequestMessage = new HttpRequestMessage(
                    HttpMethod.Get,
                    "https://example.com/new/feed")
            };

            return Task.FromResult(response);
        }
    }
}
