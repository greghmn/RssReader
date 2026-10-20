using System;
using System.Collections.Generic;
using System.Text;

namespace RssReader.Core
{
    public class FeedItem
    {
        public string Title { get; init; } = "";
        public string Link { get; init; } = "";
        public string Description { get; init; } = "";
    }
}
