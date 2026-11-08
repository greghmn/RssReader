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
			: this(new HttpClient
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
			throw new ArgumentException(
					"The feed URL must be an absolute HTTP or HTTPS URL.",
					nameof(feedUrl));
		}
		using HttpResponseMessage response =
				await _httpClient.GetAsync(
						feedUri,
						HttpCompletionOption.ResponseHeadersRead,
						cancellationToken);

		response.EnsureSuccessStatusCode();

		await using var stream =
				await response.Content.ReadAsStreamAsync(cancellationToken);

		XDocument document = await XDocument.LoadAsync(
				stream,
				LoadOptions.None,
				cancellationToken);

		Uri finalFeedUri = response.RequestMessage?.RequestUri ?? feedUri;

		List<FeedItem> items = FeedParser.Parse(document, finalFeedUri);

		//note: FeedItem's properties have init, not set; the immutability removes any potential shared state issue, but it might be computationally costly to copy like so. We'll see/figure out a workaround if necessary.

		return items
.Select(item => new FeedItem
{
	Id = item.Id,
	FeedUrl = feedUrl,
	Title = item.Title,
	Link = item.Link,
	Summary = item.Summary,
	Content = item.Content,
	PublishedAt = item.PublishedAt
})
.ToList();
	}


	public async Task<string?> ReadTitleAsync(
					string feedUrl,
					CancellationToken cancellationToken = default)
	{
		if (!Uri.TryCreate(
						feedUrl,
						UriKind.Absolute,
						out Uri? feedUri)
						|| (feedUri.Scheme != Uri.UriSchemeHttp
								&& feedUri.Scheme != Uri.UriSchemeHttps))
		{
			throw new ArgumentException(
							"The feed URL must be an absolute HTTP or HTTPS URL.",
							nameof(feedUrl));
		}

		using HttpResponseMessage response =
						await _httpClient.GetAsync(
										feedUri,
										HttpCompletionOption.ResponseHeadersRead,
										cancellationToken);

		response.EnsureSuccessStatusCode();

		await using var stream =
						await response.Content.ReadAsStreamAsync(cancellationToken);

		XDocument document = await XDocument.LoadAsync(
						stream,
						LoadOptions.None,
						cancellationToken);

		return FeedParser.ParseFeedTitle(document);
	}

}
