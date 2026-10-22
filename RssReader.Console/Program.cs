
using RssReader.Core;

// Set up persistent storage.
string appDataDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "RssReader");

Directory.CreateDirectory(appDataDirectory);

string databasePath = Path.Combine(
    appDataDirectory,
    "subscriptions.db");

var repository = new SqliteSubscriptionRepository(databasePath);
await repository.InitializeAsync();

var subscriptionService = new SubscriptionService(repository);

// Run the console menu until the user quits.
while (true)
{
    Console.WriteLine("=== RSS Reader ===");
    Console.WriteLine("1. List subscriptions");
    Console.WriteLine("2. Add subscription");
    Console.WriteLine("3. Remove subscription");
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

                break;
            }

        case "3":
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
