// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.
using RssReader.Core;

namespace RssReader.Core.Tests;

public class FakeArticleRepository : IArticleRepository
{
	private readonly List<FeedItem> _articles = new();

	public Exception? SaveException { get; set; }

	public Exception? GetException { get; set; }

	public int GetCount { get; private set; }

	public bool FailFirstGet { get; set; }

	public Task<List<FeedItem>> GetByFeedAsync(string feedUrl)
	{
		GetCount++;

		if (FailFirstGet && GetCount == 1)
		{
			throw new InvalidOperationException(
					"Simulated initial archive read failure");
		}

		if (GetException is not null)
		{
			throw GetException;
		}

		return Task.FromResult(
				_articles
						.Where(article => article.FeedUrl == feedUrl)
						.ToList());
	}

	public Task SaveAsync(string feedUrl, IEnumerable<FeedItem> articles)
	{
		if (SaveException is not null)
		{
			throw SaveException;
		}
		foreach (FeedItem article in articles)
		{
			string? identityKey = ArticleIdentity.GetId(article);

			if (identityKey is not null)
			{
				_articles.RemoveAll(existing =>
						existing.FeedUrl == feedUrl
						&& ArticleIdentity.GetId(existing) == identityKey);
			}

			_articles.Add(new FeedItem
			{
				Id = article.Id,
				FeedUrl = feedUrl,
				Link = article.Link,
				Title = article.Title,
				Summary = article.Summary,
				Content = article.Content,
				PublishedAt = article.PublishedAt,
				RawPublishedDate = article.RawPublishedDate
			});
		}

		return Task.CompletedTask;
	}
}
