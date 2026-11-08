// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.

using RssReader.Core;


// Set up persistent storage.
string appDataDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "RssReader");

Directory.CreateDirectory(appDataDirectory);

string databasePath = Path.Combine(
    appDataDirectory,
    "subscriptions.db");

// Initialize subscription storage.
var repository = new SqliteSubscriptionRepository(databasePath);
await repository.InitializeAsync();

var subscriptionService = new SubscriptionService(repository);

// Initialize article read-state storage.
var articleStateRepository =
    new SqliteArticleStateRepository(databasePath);

await articleStateRepository.InitializeAsync();

var articleStateService =
    new ArticleStateService(articleStateRepository);

// Initialize article content storage.
var articleRepository =
		new SqliteArticleRepository(databasePath);

await articleRepository.InitializeAsync();

// Set up feed reading.
IFeedReader feedReader = new FeedReader();

var feedService = new FeedService(
		subscriptionService,
		feedReader,
		articleRepository);


// Run the console menu until the user quits.
while (true)
{
    Console.WriteLine("=== RSS Reader ===");
    Console.WriteLine("1. List subscriptions");
    Console.WriteLine("2. Read a subscription");
    Console.WriteLine("3. Add subscription");
    Console.WriteLine("4. Remove subscription");
    Console.WriteLine("Q. Quit");
    Console.Write("Choose an option: ");

    string? choice = Console.ReadLine();

    Console.WriteLine();

    switch (choice?.Trim().ToLowerInvariant())
    {
        case "1":
            {
                List<FeedSubscription> subscriptions =
                    await subscriptionService.GetAllAsync();

                if (subscriptions.Count == 0)
                {
                    Console.WriteLine("No subscriptions saved.");
                }
                else
                {
                    foreach (FeedSubscription subscription in subscriptions)
                    {
                        Console.WriteLine(
                            $"{subscription.Title}: {subscription.Url}");
                    }
                }

                break;
            }
        case "2":
            {
                List<FeedSubscription> subscriptions =
                    await subscriptionService.GetAllAsync();

                if (subscriptions.Count == 0)
                {
                    Console.WriteLine("No subscriptions saved.");
                    break;
                }

                for (int i = 0; i < subscriptions.Count; i++)
                {
                    Console.WriteLine(
                        $"{i + 1}. {subscriptions[i].Title} " +
                        $"({subscriptions[i].Url})");
                }

                Console.Write("Choose a subscription number: ");
                string? input = Console.ReadLine();

                if (!int.TryParse(input, out int selection) ||
                    selection < 1 ||
                    selection > subscriptions.Count)
                {
                    Console.WriteLine("Invalid selection.");
                    break;
                }

                FeedSubscription selectedSubscription =
                    subscriptions[selection - 1];


                List<FeedItem> items;

                try
                {
                    items = await feedService.GetItemsAsync(
                        selectedSubscription.Url);
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine(
                        $"Could not retrieve the feed. {ex.Message}");
                    break;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine(
                        "The feed request timed out or was cancelled.");
                    break;
                }
                catch (System.Xml.XmlException)
                {
                    Console.WriteLine(
                        "The server response contains invalid XML.");
                    break;
                }
                catch (InvalidDataException)
                {
                    Console.WriteLine(
                        "The response is not a supported RSS or Atom feed.");
                    break;
                }
                catch (ArgumentException)
                {
                    Console.WriteLine(
                        "Please enter a valid absolute HTTP or HTTPS feed URL, such as https://example.com/feed.xml.");
                    break;
                }

                if (items.Count == 0)
                {
                    Console.WriteLine("No articles found.");
                    break;
                }

                Console.WriteLine($"\nArticles from {selectedSubscription.Title}:\n");




                for (int i = 0; i < items.Count; i++)
                {
                    FeedItem item = items[i];

                    bool isRead = await articleStateService.IsReadAsync(
                        selectedSubscription.Url, item);

                    string status = isRead ? "[Read]" : "[Unread]";

                    Console.WriteLine($"{i + 1}. {status} {item.Title}");
                    Console.WriteLine(item.Link);

                    if (item.PublishedAt.HasValue)
                    {
                        Console.WriteLine(
                            $"Published: {item.PublishedAt.Value.ToLocalTime():g}");
                    }

                    Console.WriteLine();
                }

                Console.Write("Enter an article number to change its read state, or press Enter to return: ");
                string? articleInput = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(articleInput))
                {
                    break;
                }

                if (!int.TryParse(articleInput, out int articleSelection) ||
                    articleSelection < 1 ||
                    articleSelection > items.Count)
                {
                    Console.WriteLine("Invalid article selection.");
                    break;
                }

                FeedItem selectedItem = items[articleSelection - 1];

                bool selectedItemIsRead = await articleStateService.IsReadAsync(
                    selectedSubscription.Url, selectedItem);

                Console.WriteLine(
                    selectedItemIsRead
                        ? "1. Mark as unread"
                        : "1. Mark as read");
                Console.WriteLine("2. Cancel");
                Console.Write("Choose an option: ");

                string? stateChoice = Console.ReadLine();

                if (stateChoice == "1")
                {
                    if (selectedItemIsRead)
                    {
                        await articleStateService.MarkAsUnreadAsync(
                            selectedSubscription.Url, selectedItem);

                        Console.WriteLine("Article marked as unread.");
                    }
                    else
                    {
                        await articleStateService.MarkAsReadAsync(
                            selectedSubscription.Url, selectedItem);

                        Console.WriteLine("Article marked as read.");
                    }
                }
                else if (stateChoice != "2")
                {
                    Console.WriteLine("Unrecognized option.");
                }



                break;
            }
        case "3":
            {
                Console.Write("Feed URL: ");
                string? url = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(url))
                {
                    Console.WriteLine("URL cannot be empty.");
                    break;
                }

                Console.Write("Feed title: ");
                string? title = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(title))
                {
                    Console.WriteLine("Title cannot be empty.");
                    break;
                }

                try
                {
                    bool added = await subscriptionService.AddAsync(
                        new FeedSubscription
                        {
                            Url = url.Trim(),
                            Title = title.Trim()
                        });

                    Console.WriteLine(
                        added
                            ? "Subscription added."
                            : "That subscription already exists.");
                }
                catch (ArgumentException)
                {
                    Console.WriteLine(
                        "Please enter a valid absolute HTTP or HTTPS URL, " +
                        "such as https://example.com/feed.xml.");
                }

                break;
            }

        case "4":
            {
                Console.Write("URL of subscription to remove: ");
                string? url = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(url))
                {
                    Console.WriteLine("URL cannot be empty.");
                    break;
                }

                bool removed = await subscriptionService.RemoveAsync(url.Trim());

                Console.WriteLine(
                    removed
                        ? "Subscription removed."
                        : "Subscription not found.");

                break;
            }

        case "q":
            return;

        default:
            Console.WriteLine("Unrecognized option. Try again.");
            break;
    }

    Console.WriteLine();
}
