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
        public DateTimeOffset? PublishedAt { get; init; }
        //for identifying articles 
        public string Id { get; init; } = "";
        public string FeedUrl { get; init; } = "";
    }
}
