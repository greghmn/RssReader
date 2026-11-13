using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using RssReader.Core;
using Xunit;

namespace RssReader.Core.Tests;

public class FeedReaderCompressionTests
{
	[Fact]
	public async Task ReadAsync_WhenGzipExpandsBeyondLimit_RejectsFeed()
	{
		string xml =
				"<rss version=\"2.0\"><channel><description>"
				+ new string('x', 11 * 1024 * 1024)
				+ "</description></channel></rss>";

		byte[] uncompressed = Encoding.UTF8.GetBytes(xml);

		byte[] compressed;

		using (var buffer = new MemoryStream())
		{
			using (var gzip = new GZipStream(
					buffer,
					CompressionLevel.Optimal,
					leaveOpen: true))
			{
				gzip.Write(uncompressed);
			}

			compressed = buffer.ToArray();
		}

		Assert.True(compressed.Length < 10 * 1024 * 1024);

		using var listener = new TcpListener(
				IPAddress.Loopback,
				0);

		listener.Start();

		int port = ((IPEndPoint)listener.LocalEndpoint).Port;

		Task serverTask = Task.Run(async () =>
		{
			using TcpClient client =
					await listener.AcceptTcpClientAsync();

			await using NetworkStream stream = client.GetStream();

			using var reader = new StreamReader(
					stream,
					Encoding.ASCII,
					leaveOpen: true);

			while (!string.IsNullOrEmpty(
					await reader.ReadLineAsync()))
			{
			}

			string headers =
					"HTTP/1.1 200 OK\r\n"
					+ "Content-Type: application/rss+xml\r\n"
					+ "Content-Encoding: gzip\r\n"
					+ $"Content-Length: {compressed.Length}\r\n"
					+ "Connection: close\r\n"
					+ "\r\n";

			byte[] headerBytes = Encoding.ASCII.GetBytes(headers);

			await stream.WriteAsync(headerBytes);
			await stream.WriteAsync(compressed);
			await stream.FlushAsync();
		});

		var feedReader = new FeedReader();

		try
		{
			await Assert.ThrowsAsync<FeedSizeLimitExceededException>(
					() => feedReader.ReadAsync(
							$"http://127.0.0.1:{port}/feed.xml"));

			await serverTask;
		}
		finally
		{
			listener.Stop();
		}
	}
}
