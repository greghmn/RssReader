using System.IO;
using System.Xml.Linq;
using System.Text;

namespace RssReader.Core;

public static class OpmlExporter
{
	public static string Export(
			IEnumerable<FeedSubscription> subscriptions)
	{
		var document = new XDocument(
				new XDeclaration("1.0", "utf-8", null),
				new XElement("opml",
						new XAttribute("version", "2.0"),
						new XElement("head",
								new XElement("title", "RSS Reader Subscriptions")),
						new XElement("body",
								subscriptions.Select(subscription =>
										new XElement("outline",
												new XAttribute("type", "rss"),
												new XAttribute("text", subscription.Title),
												new XAttribute("title", subscription.Title),
												new XAttribute("xmlUrl", subscription.Url))))));

		using var writer = new Utf8StringWriter();

		document.Save(writer);

		return writer.ToString();
	}

	private sealed class Utf8StringWriter : StringWriter
	{
		public override Encoding Encoding => Encoding.UTF8;
	}


}
