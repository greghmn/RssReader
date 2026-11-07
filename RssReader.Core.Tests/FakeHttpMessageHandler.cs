
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
