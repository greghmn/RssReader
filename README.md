# RSS Reader

A free and open-source (FOSS) desktop feed reader for RSS and Atom built with C#, .NET, and Avalonia UI.

RSS Reader lets you subscribe to news feeds, read articles, and track your reading progress from a desktop application. SQLite allows for local storage of subscriptions and retrieved articles and keeps track of whether an article has been read. Users can mark articles as read/unread manually.

The project is currently in its infancy at v0.0.0.1-beta.0, the only release as of right now. Versioning, for the time being, follows a Major.Major.Tweak.Debug scheme, a "tweak" referring to under-the-hood refactorings and (in theory) performance improvements as well as small cosmetic changes to the UI that don't meet the criteria for constituting a separate minor version, with suffixes like "beta" as needed to indicate life cycle stage and so on.

As an aside, the name of this project is going to change from the most generic possible to something more interesting once I find one I'm happy with. Any and all suggestions are welcome at this point, and credit will be given henceforth to anyone who comes up with a name I like enough to use, or anything else, like a logo or a motto. Bonus points if the name is related to lighthouses in some way! Proper attention will be given to anything that needs to be modified for releases under that name, especially backward compatibility concerns like retention of SQLite databases and reading histories.

## Installation

### Windows (x64)

The initial release targets Windows x64 and is distributed as a **framework-dependent application**. The .NET runtime is not included in the download and must be installed separately.

1. Download `RssReader-v0.0.0.0-beta.0-win-x64.zip` from the project's GitHub Releases page once it becomes available.
2. Install the **[.NET 10 Runtime for Windows x64](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)** if it is not already installed. Select **.NET Runtime**, not **ASP.NET Core Runtime** or **.NET Desktop Runtime**. The .NET SDK also works but is unnecessary for running the application.
3. Extract the ZIP archive to a directory of your choice.
4. Run `RssReader.App.exe` from the extracted directory.

**Important:** Keep all extracted application files together in their original directory structure. The executable depends on accompanying libraries and other files, and moving or deleting them may prevent the application from starting or functioning correctly.

RSS Reader stores its application data locally. Installing an updated version should not require deleting existing subscriptions, saved articles, or other application data. Nevertheless, backing up important data before updating is recommended, particularly while the software is in beta.

**Current release target:** Windows x64. Other platforms are planned but are not yet available as downloadable releases. See **Other platforms** and **Future plans** for additional information.

### Estimated system requirements

The following specifications are preliminary estimates, not independently verified minimum or recommended requirements. Actual performance and compatibility may vary depending on the system, the number of subscriptions, and the amount of content stored.

#### Minimum (estimated)

- **Operating system:** Windows 10 22H2, 64-bit (intended compatibility target)
- **Processor:** Dual-core x64 CPU, approximately 1.5 GHz
- **Graphics:** Dedicated GPU not expected to be necessary
- **Memory:** 2 GB RAM for the entire system
- **Storage:** Approximately 250 MB for application files, plus additional space for the separately installed .NET runtime and locally retained content
- **Display:** Minimum application window size of 600 × 300 pixels; a resolution of at least 1280 × 720 is suggested for comfortable use
- **Internet:** Connection required to retrieve feeds and access online content
- **Runtime:** .NET 10 Runtime, Windows x64

#### Recommended (estimated)

- **Operating system:** Windows 11, 64-bit
- **Processor:** Dual-core x64 CPU or better, approximately 2 GHz or faster
- **Graphics:** Dedicated GPU not expected to be necessary
- **Memory:** At least 4 GB RAM
- **Storage:** Approximately 250 MB for application files, plus additional space for the separately installed .NET runtime and locally retained content
- **Display:** At least 1366 × 768 pixels; 1920 × 1080 or higher is preferable
- **Internet:** Broadband connection
- **Runtime:** .NET 10 Runtime, Windows x64

#### Additional notes

**Runtime dependency:** This is a framework-dependent release. The .NET 10 Runtime must be installed separately, and the application requires a compatible .NET 10 runtime version. The full .NET SDK is not required.

**Operating system compatibility:** Windows 10 22H2 is an intended compatibility target, but Windows 10 reached the end of its general support lifecycle in October 2025. Compatibility with an operating system does not imply that the operating system continues to receive general security updates or support from its manufacturer. Compatibility and performance across different Windows configurations have not yet been comprehensively tested.

**Graphics:** Avalonia supports software rendering, so a dedicated graphics processor should not be necessary. However, compatibility with every graphics configuration, driver, or rendering environment is not guaranteed.

**Memory and processing:** The CPU and RAM specifications are estimates. Resource usage may vary depending on the number and size of feeds, the amount of retained content, and other applications running simultaneously.

**Storage:** The estimated installation size does not include the separately installed .NET runtime or accumulated application data. Storage requirements may increase substantially as articles and other supported content are retained locally. Future features, including potential podcast downloading and other media-related capabilities, may introduce additional storage requirements.

**Internet connectivity:** An internet connection is necessary to retrieve or refresh RSS/Atom feeds and access external resources. Previously saved articles may remain accessible without an active connection, depending on the content available in local storage. The application is not guaranteed to preserve every externally hosted resource referenced by an article.

### Other platforms

The initial downloadable release targets **Windows x64 only**.

Support for additional operating systems and architectures is planned, but no release dates or guarantees of availability are currently provided. See **Future plans** for more information about the project's intended direction.

## Features

### RSS and Atom Feed Support

- Subscribe to RSS and Atom feeds with HTTP and HTTPS URLs.
- Automatically discover the titles of feeds you subscribe to.
- Read article titles, publication dates, summaries, and available content.
- Support common feed extensions and XML namespaces.
- Open original articles in your default web browser.
- Handle malformed and unavailable feeds without crashing the application.

### Subscription Management

- Add, remove, and rename subscriptions. Note: upon adding a subscription, you can opt to choose a name; the name will be the default (the title discovered by the application) if you omit one.
- Refresh individual feeds to retrieve new articles.
- Refresh feeds using the context menu (right-click, etc.) without changing the subscription you've selected currently.
- Import and export subscriptions using OPML files.
- Display the last time a feed was retrieved successfully.
- Show unread article counts for each subscription.

### Reading and Archiving

- Read retrieved article content directly within the application.
- Mark articles as read or unread.
- Preserve read/unread state after restarting the application.
- Cache retrieved articles locally to maintain access to them offline.
- Retain articles retrieved previously after refreshing the feed.
- Preserve articles and their read/unread state after unsubscribing so that you can restore them should you choose to resubscribe to that feed's URL.

### Reliability and Privacy

- Confine subscriptions, articles, and reading history to local storage using SQLite.
- Cached articles continue to display even if feed retrival fails.
- Monitor visible progress indicators and error messages for feed operations.
- Keep previously stored articles upon recovery from temporary network failures.
- **_Make use of all of this without compromising your privacy: no account registration or personal information or anything else!!!_** (Well, to the extent that I can maximize privacy; some tools used might restrict )

## Usage

### Adding a Subscription

1. Click **Add subscription** in the subscriptions pane.
2. Enter the URL of an RSS or Atom feed.
3. Optionally, enter a custom title. If you leave the title blank, RSS Reader will attempt to retrieve the feed's title automatically.
4. Confirm to add the subscription.

The new subscription will appear in the subscriptions list.

If the feed is temporarily unavailable, the subscription can still be added. When automatic title discovery fails, the feed URL is used as the fallback title.

### Reading Articles

1. Select a subscription from the subscriptions list.
2. Choose an article from the article list.
3. Read its available content in the reading pane.

Use **Open original article** to visit the article's webpage in your default browser, if a valid article URL is available.

RSS Reader displays the content supplied by the feed publisher. Some feeds provide full articles, while others provide only summaries or excerpts.

### Refreshing Feeds

To retrieve the latest articles, select a subscription and click **Refresh** above the article list.

Alternatively, right-click a subscription and choose **Refresh** from its context menu. This allows you to refresh a different subscription without changing the feed you're currently reading.

The application displays refresh progress and reports retrieval errors. Previously cached articles remain available if a refresh fails.

The **Last Retrieved** timestamp indicates when the subscription was last successfully refreshed.

### Tracking Read and Unread Articles

Select an article and use **Mark as read** or **Mark as unread** to change its reading status.

Unread articles are indicated in the article list, and subscriptions display their unread article counts.

Reading states are saved automatically and preserved when you close and reopen the application.

### Managing Subscriptions

Right-click a subscription to access its context menu:

- **Refresh:** Retrieve updated articles.
- **Open feed URL:** Open the subscription's feed URL.
- **Rename:** Change the subscription's displayed title.
- **Remove subscription:** Unsubscribe from the feed.

You can also use the **Remove subscription** button for the currently selected subscription.

**Removing a subscription does not delete its archived articles or their read/unread states.** If you subscribe to the same feed URL again, previously cached articles and their reading states remain available.

### Importing and Exporting Subscriptions

RSS Reader supports OPML, a common format for transferring feed subscriptions between RSS readers.

**Importing subscriptions:**

1. Click **Import OPML...**.
2. Select an OPML file.
3. Confirm the import and review the displayed result.

Existing subscriptions are not duplicated.

**Exporting subscriptions:**

1. Click **Export OPML...**.
2. Choose a destination for the OPML file.
3. Save the exported subscription list.

OPML import and export transfer subscription information, not cached articles or read/unread history.

### Offline Reading

Previously retrieved articles are cached locally and can be viewed without an internet connection.

To make articles available offline, retrieve them while connected to the internet.

Offline reading is limited to content previously supplied by the feeds. Opening original webpages normally requires an internet connection.

## Data Storage and Privacy

RSS Reader is designed around local data storage and minimal collection of personal information.

The application does not require an account, registration, or the submission of personal information to use its features.

### Local Data Storage

RSS Reader uses SQLite to store application data locally, including:

- Feed subscriptions and their titles.
- Previously retrieved articles and available feed content.
- Article read/unread states.
- Subscription metadata, including retrieval timestamps.

On Windows, application data is stored in the user's local application-data directory:

```text
%LOCALAPPDATA%\RssReader\
```

You can open this location by entering the path above into the Windows Run dialog (`Win + R`) or File Explorer's address bar.

Application data is stored separately from the executable and its supporting libraries. Moving, replacing, or updating the application files should not delete existing subscriptions or reading history.

**Important:** Uninstalling or deleting the application folder does not necessarily delete locally stored application data.

### Network Connections

RSS Reader connects to external servers when retrieving RSS and Atom feeds.

When you subscribe to or refresh a feed, the destination server may receive information normally associated with an HTTP request, including your IP address and request headers.

The application does not provide network anonymity. The privacy practices of external feed publishers are outside the project's control.

Opening an original article launches its URL through your operating system's configured application, typically a web browser. Any subsequent communication with that website is subject to the browser's configuration and the website's privacy practices.

### Data Retention

RSS Reader uses a non-destructive approach to archived content.

Removing a subscription does not automatically erase its previously cached articles or their read/unread states.

If you subscribe to the same feed URL again, previously stored articles and their reading states remain available.

This behavior is intentional, but it also means removing a subscription should not be interpreted as deleting all associated local data.

### Backups and Data Portability

RSS Reader supports importing and exporting subscriptions in OPML format.

An OPML export contains subscription information, but **does not constitute a complete backup** of the application's cached articles or reading history.

For a more complete local backup, close RSS Reader and copy its SQLite database files from the application-data directory to a safe location.

Restore procedures and more advanced data-management features may be documented or introduced in future releases.

### Privacy Scope

RSS Reader is designed to operate without user accounts or a centralized synchronization service.

Its local-first approach reduces the need to disclose information to the application developer or a separate application backend.

However, local storage does not itself provide encryption, anonymity, or protection against other software or users with access to the same computer.

Users should take appropriate precautions to protect their device and local data.

## Known Limitations

RSS Reader v0.0.0.0-beta.0 is an initial release. While its core reading and subscription-management features are functional, several capabilities and refinements are not yet available.

### Platform Support

- The initial binary distribution targets **Windows x64**.
- Although Avalonia UI and .NET support multiple operating systems, Linux, macOS, and mobile distributions have not yet been officially tested or packaged.
- Compatibility with Wine or other Windows compatibility layers is not guaranteed.

### Feed Compatibility and Content

- Some RSS and Atom feeds may contain nonstandard XML, unsupported extensions, or unusual content that the application cannot interpret correctly.
- Article content is limited to the information provided by the feed publisher. Full webpage extraction is not currently supported.
- Embedded media, interactive content, and complex HTML layouts are not rendered as they would appear in a web browser.
- Feed retrieval requires network access. Previously cached articles remain available offline.

### Refreshing and Background Operations

- Feeds must be refreshed individually. Automatic scheduled refreshes and bulk refresh are not currently supported.
- Refresh requests may take time to complete when a feed server is slow or unresponsive.
- Background refreshes provide per-subscription status messages, but there is no dedicated background-task management interface.

### Data Management

- Removing a subscription does not delete previously cached articles or their reading states.
- There is currently no dedicated interface for permanently deleting archived articles or clearing individual subscriptions' stored histories.
- OPML import and export transfer subscriptions, not cached articles or reading progress.
- There is no built-in synchronization between devices or automatic cloud backup.

### Interface and Accessibility

- The interface is designed to remain usable in relatively small windows, but some information may be truncated at minimum pane widths.
- Keyboard navigation is supported for primary controls and subscription context menus, although focus behavior may vary after certain actions.
- Advanced customization, configurable keyboard shortcuts, and extensive accessibility testing remain areas for future improvement.

### Other Limitations

- The application does not currently provide notifications for newly retrieved articles.
- A command-line interface is not included in v0.0.0.0.
- Application settings and configurations options are limited.
- You will need to install the .NET 10 Runtime to run this. A fully functional build with a self-contained .NET Runtime is complete, but release is pending since I'm not a lawyer and may need to do a bit more research regarding licensing and further modification of the source code to make sure as much as possible falls under AGPL-3.0-or-later.

## Building from Source

RSS Reader is developed using C# and .NET 10, with Avalonia UI for its graphical interface and SQLite for local data storage.

### Prerequisites

Building RSS Reader from source requires the .NET 10 SDK.

Download it from the [official .NET download page](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

The SDK includes the .NET Runtime and the tools needed to restore dependencies, build the application, and run its automated tests.

The separately distributed Windows executable requires only the compatible .NET 10 Runtime, not the full SDK.

### Obtaining the Source Code

Clone the project's Git repository:

```bash
git clone <repository-url>
cd RssReader
```

Replace `<repository-url>` with the project's actual GitHub repository URL.

Alternatively, download the source archive from GitHub and extract it.

### Restoring Dependencies

From the solution's root directory, run:

```bash
dotnet restore RssReader.slnx
```

This restores the NuGet packages required by the solution.

An internet connection may be necessary when restoring dependencies for the first time.

### Building the Application

To build the solution:

```bash
dotnet build RssReader.slnx
```

For an optimized Release build:

```bash
dotnet build RssReader.slnx --configuration Release
```

### Running the Application

From the repository's root directory:

```bash
dotnet run --project RssReader.App/RssReader.App.csproj
```

This starts the Avalonia desktop application.

To run in Release configuration:

```bash
dotnet run --project RssReader.App/RssReader.App.csproj --configuration Release
```

### Running the Tests

RSS Reader includes automated tests using xUnit.

Run the solution's tests with:

```bash
dotnet test RssReader.slnx
```

The tests cover functionality including feed parsing, subscription management, persistence, HTTP error handling, and application ViewModel behavior.

### Publishing for Windows

To create a framework-dependent Windows x64 distribution:

```bash
dotnet publish RssReader.App/RssReader.App.csproj --configuration Release --runtime win-x64 --self-contained false -p:PublishSingleFile=false -p:PublishTrimmed=false
```

This produces a Windows x64 build that does not bundle the .NET Runtime.

Users must install a compatible .NET 10 Runtime separately before running the application.

The publishing command generates the application files but does not necessarily include all repository documentation and licensing files. These must be incorporated into the distribution package separately.

A self-contained Windows build has also been tested successfully. However, it is not currently offered as an official distribution pending completion of additional licensing review.

### Project Structure

The solution is organized into several projects:

| Project                | Purpose                                              |
| ---------------------- | ---------------------------------------------------- |
| `RssReader.Core`       | Feed parsing, services, data models, and persistence |
| `RssReader.App`        | Avalonia desktop user interface                      |
| `RssReader.Console`    | Console project used during development              |
| `RssReader.Core.Tests` | Core functionality and persistence tests             |
| `RssReader.App.Tests`  | Application and ViewModel tests                      |

Contributors should run the existing tests before submitting changes to help identify regressions.

## License

RSS Reader is intended to be released under the **GNU Affero General Public License, version 3 or later (AGPL-3.0-or-later)**.

This license permits users to run, study, modify, and redistribute the software, subject to its terms and conditions.

The repository's `LICENSE` file will contain the full license text. Copyright notices, attribution requirements, and any applicable additional terms will be finalized before the first public release.

Third-party components remain subject to their respective licenses.
