// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.
using System.IO;
using RssReader.Core;
using System.Net;
using System.Text;
using Xunit;
using System.Net.Http;

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

	//helper for the test below

	private sealed class StatusCodeHandler : HttpMessageHandler
	{
		private readonly HttpStatusCode _statusCode;

		public StatusCodeHandler(HttpStatusCode statusCode)
		{
			_statusCode = statusCode;
		}

		protected override Task<HttpResponseMessage> SendAsync(
				HttpRequestMessage request,
				CancellationToken cancellationToken)
		{
			return Task.FromResult(
					new HttpResponseMessage(_statusCode));
		}
	}

	[Theory]
	[InlineData(HttpStatusCode.Forbidden)]
	[InlineData(HttpStatusCode.NotFound)]
	[InlineData(HttpStatusCode.TooManyRequests)]
	[InlineData(HttpStatusCode.InternalServerError)]
	[InlineData(HttpStatusCode.ServiceUnavailable)]
	public async Task ReadAsync_WhenHttpResponseIsUnsuccessful_ThrowsHttpRequestException(
		HttpStatusCode statusCode)
	{
		using var httpClient = new HttpClient(
				new StatusCodeHandler(statusCode));

		var reader = new FeedReader(httpClient);

		HttpRequestException exception =
				await Assert.ThrowsAsync<HttpRequestException>(
						() => reader.ReadAsync(
								"https://example.com/feed.xml"));

		Assert.Equal(
				statusCode,
				exception.StatusCode);
	}

	//helper class

	private sealed class ConnectionFailureHandler : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(
				HttpRequestMessage request,
				CancellationToken cancellationToken)
		{
			throw new HttpRequestException(
					"Simulated network connection failure.");
		}
	}

	[Fact]
	public async Task ReadAsync_WhenConnectionFails_PropagatesHttpRequestException()
	{
		using var httpClient = new HttpClient(
				new ConnectionFailureHandler());

		var reader = new FeedReader(httpClient);

		HttpRequestException exception =
				await Assert.ThrowsAsync<HttpRequestException>(
						() => reader.ReadAsync(
								"https://example.com/feed.xml"));

		Assert.Null(exception.StatusCode);
	}

	private sealed class CancellationHandler : HttpMessageHandler
	{
		private readonly TaskCompletionSource<bool> _started;
		private readonly TaskCompletionSource<bool> _canceled;

		public CancellationHandler(
				TaskCompletionSource<bool> started,
				TaskCompletionSource<bool> canceled)
		{
			_started = started;
			_canceled = canceled;
		}

		protected override async Task<HttpResponseMessage> SendAsync(
				HttpRequestMessage request,
				CancellationToken cancellationToken)
		{
			_started.TrySetResult(true);

			try
			{
				await Task.Delay(
						Timeout.InfiniteTimeSpan,
						cancellationToken);
			}
			catch (OperationCanceledException)
			{
				_canceled.TrySetResult(true);
				throw;
			}

			throw new InvalidOperationException(
					"The simulated request should have been canceled.");
		}
	}

	[Fact]
	public async Task ReadAsync_WhenCanceled_StopsHttpRequest()
	{
		var requestStarted = new TaskCompletionSource<bool>(
				TaskCreationOptions.RunContinuationsAsynchronously);

		var requestCanceled = new TaskCompletionSource<bool>(
				TaskCreationOptions.RunContinuationsAsynchronously);

		using var httpClient = new HttpClient(
				new CancellationHandler(
						requestStarted,
						requestCanceled));

		var reader = new FeedReader(httpClient);

		using var cancellationSource = new CancellationTokenSource();

		Task<List<FeedItem>> readTask = reader.ReadAsync(
				"https://example.com/feed.xml",
				cancellationSource.Token);

		await requestStarted.Task.WaitAsync(
				TimeSpan.FromSeconds(5));

		cancellationSource.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(
				() => readTask);

		await requestCanceled.Task.WaitAsync(
				TimeSpan.FromSeconds(5));
	}

	private sealed class NeverCompletingHandler : HttpMessageHandler
	{
		protected override async Task<HttpResponseMessage> SendAsync(
				HttpRequestMessage request,
				CancellationToken cancellationToken)
		{
			await Task.Delay(
					Timeout.InfiniteTimeSpan,
					cancellationToken);

			throw new InvalidOperationException(
					"The simulated request should have timed out.");
		}
	}

	[Fact]
	public async Task ReadAsync_WhenHttpRequestTimesOut_ThrowsOperationCanceledException()
	{
		using var httpClient = new HttpClient(
				new NeverCompletingHandler())
		{
			Timeout = TimeSpan.FromMilliseconds(100)
		};

		var reader = new FeedReader(httpClient);

		await Assert.ThrowsAnyAsync<OperationCanceledException>(
				() => reader.ReadAsync(
						"https://example.com/feed.xml"));
	}

	[Fact]
	public async Task ReadAsync_WhenResponseContainsMalformedXml_ThrowsXmlException()
	{
		const string malformedXml = """
        <rss version="2.0">
          <channel>
            <title>Broken Feed</title>
            <item>
              <title>Unclosed Article
            </item>
          </channel>
        </rss>
        """;

		using var httpClient = new HttpClient(
				new XmlResponseHandler(malformedXml));

		var reader = new FeedReader(httpClient);

		await Assert.ThrowsAsync<System.Xml.XmlException>(
				() => reader.ReadAsync(
						"https://example.com/feed.xml"));
	}

	private sealed class XmlResponseHandler : HttpMessageHandler
	{
		private readonly string _xml;

		public XmlResponseHandler(string xml)
		{
			_xml = xml;
		}

		protected override Task<HttpResponseMessage> SendAsync(
				HttpRequestMessage request,
				CancellationToken cancellationToken)
		{
			return Task.FromResult(
					new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent(
									_xml,
									Encoding.UTF8,
									"application/xml")
					});
		}
	}

	[Fact]
	public async Task ReadAsync_WhenXmlIsNotRssOrAtom_RejectsDocument()
	{
		const string xml = """
        <catalog>
          <book>
            <title>Example Book</title>
          </book>
        </catalog>
        """;

		using var httpClient = new HttpClient(
				new XmlResponseHandler(xml));

		var reader = new FeedReader(httpClient);

		InvalidDataException exception =
				await Assert.ThrowsAsync<InvalidDataException>(
						() => reader.ReadAsync(
								"https://example.com/feed.xml"));

		Assert.Contains(
				"Unsupported feed format",
				exception.Message);
	}


	[Fact]
	public async Task ReadTitleAsync_Rss_ReturnsFeedTitle()
	{
		const string xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <rss version="2.0">
            <channel>
                <title>Example News</title>
                <description>A sample feed.</description>
            </channel>
        </rss>
        """;

		using var httpClient = new HttpClient(
				new FakeHttpMessageHandler((request, token) =>
						Task.FromResult(new HttpResponseMessage(
								HttpStatusCode.OK)
						{
							Content = new StringContent(xml)
						})));

		var reader = new FeedReader(httpClient);

		string? title = await reader.ReadTitleAsync(
				"https://example.com/feed.xml");

		Assert.Equal("Example News", title);
	}


	[Fact]
	public async Task ReadTitleAsync_Atom_ReturnsFeedTitle()
	{
		const string xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <feed xmlns="http://www.w3.org/2005/Atom">
            <title>Science Weekly</title>
            <id>https://example.com/feed</id>
            <updated>2026-11-06T12:00:00Z</updated>
            <entry>
                <title>New Discovery</title>
                <id>https://example.com/article-1</id>
                <updated>2026-11-06T12:00:00Z</updated>
            </entry>
        </feed>
        """;

		using var httpClient = new HttpClient(
				new FakeHttpMessageHandler((request, token) =>
						Task.FromResult(new HttpResponseMessage(
								HttpStatusCode.OK)
						{
							Content = new StringContent(xml)
						})));

		var reader = new FeedReader(httpClient);

		string? title = await reader.ReadTitleAsync(
				"https://example.com/atom.xml");

		Assert.Equal("Science Weekly", title);
	}


	[Fact]
	public async Task ReadTitleAsync_HttpError_ThrowsHttpRequestException()
	{
		using var httpClient = new HttpClient(
				new FakeHttpMessageHandler((request, token) =>
						Task.FromResult(new HttpResponseMessage(
								HttpStatusCode.ServiceUnavailable))));

		var reader = new FeedReader(httpClient);

		await Assert.ThrowsAsync<HttpRequestException>(
				() => reader.ReadTitleAsync(
						"https://example.com/feed.xml"));
	}


	[Fact]
	public async Task ReadAsync_PreservesRawPublishedDateFromParsedFeed()
	{
		const string feedUrl = "https://example.com/feed.xml";
		const string rawDate = "Sometime after the purple moon";

		const string xml = """
        <rss version="2.0">
            <channel>
                <title>Publication Date Test</title>
                <description>Tests raw publication dates.</description>
                <link>https://example.com</link>
                <item>
                    <guid>article-123</guid>
                    <title>Unparseable Date</title>
                    <link>https://example.com/article-123</link>
                    <pubDate>Sometime after the purple moon</pubDate>
                </item>
            </channel>
        </rss>
        """;

		using var httpClient = new HttpClient(
						new XmlResponseHandler(xml));

		var reader = new FeedReader(httpClient);

		List<FeedItem> items = await reader.ReadAsync(feedUrl);

		FeedItem article = Assert.Single(items);

		Assert.Null(article.PublishedAt);
		Assert.Equal(rawDate, article.RawPublishedDate);
		Assert.Equal(feedUrl, article.FeedUrl);
	}


}
