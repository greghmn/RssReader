
using RssReader.Core;
using System.Net;
using System.Text;
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

    //for handling redirects 
    [Fact]
    public async Task ReadAsync_WhenFeedRedirects_PreservesOriginalFeedUrlAndResolvesRelativeArticleLink()
    {
        const string subscriptionUrl =
            "https://example.com/subscribe";

        const string finalFeedUrl =
            "https://cdn.example.com/feeds/current.xml";

        const string feedXml = """
            <feed xmlns="http://www.w3.org/2005/Atom">
                <title>Example Feed</title>
                <entry>
                    <id>article-123</id>
                    <title>Example Article</title>
                    <link href="articles/123" />
                </entry>
            </feed>
            """;

        using HttpClient httpClient = new(
            new RedirectingHandler(
                new Uri(finalFeedUrl),
                feedXml));

        FeedReader reader = new(httpClient);

        List<FeedItem> items =
            await reader.ReadAsync(subscriptionUrl);

        FeedItem item = Assert.Single(items);

        Assert.Equal(subscriptionUrl, item.FeedUrl);

        Assert.Equal(
            "https://cdn.example.com/feeds/articles/123",
            item.Link);
    }

    //helper for the previous test

    private sealed class RedirectingHandler : HttpMessageHandler
    {
        private readonly Uri _finalResponseUri;
        private readonly string _content;

        public RedirectingHandler(
            Uri finalResponseUri,
            string content)
        {
            _finalResponseUri = finalResponseUri;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = new(
                HttpStatusCode.OK)
            {
                Content = new StringContent(
                    _content,
                    Encoding.UTF8,
                    "application/atom+xml"),

                // Simulate the URL after an HTTP redirect.
                RequestMessage = new HttpRequestMessage(
                    HttpMethod.Get,
                    _finalResponseUri)
            };

            return Task.FromResult(response);
        }
    }
}
