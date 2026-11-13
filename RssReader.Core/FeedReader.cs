// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.
using System.Xml.Linq;
using System.Linq;

namespace RssReader.Core;

public class FeedReader : IFeedReader
{
	private readonly HttpClient _httpClient;

	public FeedReader()
			: this(new HttpClient(
					new HttpClientHandler
					{
						AutomaticDecompression =
									System.Net.DecompressionMethods.GZip
									| System.Net.DecompressionMethods.Deflate
									| System.Net.DecompressionMethods.Brotli
					})
			{
				Timeout = TimeSpan.FromSeconds(30)
			})
	{
	}

	public FeedReader(HttpClient httpClient)
	{
		_httpClient = httpClient;
	}

	public async Task<List<FeedItem>> ReadAsync(string feedUrl, CancellationToken cancellationToken = default)
	{
		if (!Uri.TryCreate(feedUrl, UriKind.Absolute, out Uri? feedUri) || (feedUri.Scheme != Uri.UriSchemeHttp && feedUri.Scheme != Uri.UriSchemeHttps))
		{
			throw new ArgumentException("The feed URL must be an absolute HTTP or HTTPS URL.", nameof(feedUrl));
		}

		// Combine caller cancellation with the HTTP timeout.
		using var timeoutSource = CreateTimeoutSource(cancellationToken);
		CancellationToken effectiveToken = timeoutSource.Token;

		XDocument document;
		Uri finalFeedUri;

		try
		{
			// Request headers without buffering the entire response.
			using HttpResponseMessage response = await _httpClient.GetAsync(feedUri, HttpCompletionOption.ResponseHeadersRead, effectiveToken);

			response.EnsureSuccessStatusCode();

			// Enforce the 10 MiB limit while downloading and parsing.
			document = await LoadBoundedXmlAsync(response, effectiveToken);

			// Resolve relative links against the final URL after redirects.
			finalFeedUri = response.RequestMessage?.RequestUri ?? feedUri;
		}
		catch (OperationCanceledException ex) when (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
		{
			throw new TimeoutException("Feed retrieval timed out.", ex);
		}


		List<FeedItem> items = FeedParser.Parse(document, finalFeedUri);

		return items
			.Select(item => new FeedItem
			{
				Id = item.Id,
				FeedUrl = feedUrl,
				Title = item.Title,
				Link = item.Link,
				Summary = item.Summary,
				Content = item.Content,
				PublishedAt = item.PublishedAt,
				RawPublishedDate = item.RawPublishedDate
			})
			.ToList();
	}

	public async Task<string?> ReadTitleAsync(string feedUrl, CancellationToken cancellationToken = default)
	{
		if (!Uri.TryCreate(feedUrl, UriKind.Absolute, out Uri? feedUri) || (feedUri.Scheme != Uri.UriSchemeHttp && feedUri.Scheme != Uri.UriSchemeHttps))
		{
			throw new ArgumentException("The feed URL must be an absolute HTTP or HTTPS URL.", nameof(feedUrl));
		}

		// Combine caller cancellation with the HTTP timeout.
		using var timeoutSource = CreateTimeoutSource(cancellationToken);
		CancellationToken effectiveToken = timeoutSource.Token;

		try
		{
			using HttpResponseMessage response = await _httpClient.GetAsync(feedUri, HttpCompletionOption.ResponseHeadersRead, effectiveToken);

			response.EnsureSuccessStatusCode();

			XDocument document = await LoadBoundedXmlAsync(response, effectiveToken);

			return FeedParser.ParseFeedTitle(document);
		}
		catch (OperationCanceledException ex) when (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
		{
			throw new TimeoutException("Feed title retrieval timed out.", ex);
		}
	}
	//note: this method is used to load the feed XML document, but it also checks the size of the feed and throws an exception if it exceeds a reasonable limit. This is to prevent downloading very large feeds that could cause performance issues or memory exhaustion.
	private static async Task<XDocument> LoadBoundedXmlAsync(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		const long maximumFeedSize = 10L * 1024 * 1024;

		long? declaredSize =
				response.Content.Headers.ContentLength;

		if (declaredSize.HasValue &&
				declaredSize.Value > maximumFeedSize)
		{
			throw new FeedSizeLimitExceededException();
		}

		await using var stream =
				await response.Content.ReadAsStreamAsync(cancellationToken);

		using var buffer = new MemoryStream();

		byte[] chunk = new byte[81920];

		while (true)
		{
			int bytesRead = await stream.ReadAsync(
					chunk.AsMemory(),
					cancellationToken);

			if (bytesRead == 0)
			{
				break;
			}

			if (buffer.Length + bytesRead > maximumFeedSize)
			{
				throw new FeedSizeLimitExceededException();
			}

			buffer.Write(chunk, 0, bytesRead);
		}

		buffer.Position = 0;

		return await XDocument.LoadAsync(
				buffer,
				LoadOptions.None,
				cancellationToken);
	}
	private CancellationTokenSource CreateTimeoutSource(CancellationToken cancellationToken)
	{
		var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

		if (_httpClient.Timeout != Timeout.InfiniteTimeSpan)
		{
			source.CancelAfter(_httpClient.Timeout);
		}

		return source;
	}
}
