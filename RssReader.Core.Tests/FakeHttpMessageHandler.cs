// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.

using System.Net.Http;

namespace RssReader.Core.Tests;

public class FakeHttpMessageHandler : HttpMessageHandler
{
	private readonly Func<
			HttpRequestMessage,
			CancellationToken,
			Task<HttpResponseMessage>> _handler;

	public FakeHttpMessageHandler(
			Func<HttpRequestMessage,
					 CancellationToken,
					 Task<HttpResponseMessage>> handler)
	{
		_handler = handler;
	}

	protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
	{
		return _handler(request, cancellationToken);
	}
}
