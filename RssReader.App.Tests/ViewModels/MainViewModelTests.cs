using RssReader.App.ViewModels;
using RssReader.App.Tests.Fakes;
using RssReader.Core;

namespace RssReader.App.Tests.ViewModels;

public class MainViewModelTests
{
	private static (
			MainViewModel ViewModel,
			FakeSubscriptionRepository Repository)
			CreateViewModel()
	{
		var repository = new FakeSubscriptionRepository();

		var subscriptionService =
				new SubscriptionService(repository);

		var feedReader = new FakeFeedReader();

		var feedService = new FeedService(
				subscriptionService,
				feedReader);

		var articleStateRepository =
				new FakeArticleStateRepository();

		var articleStateService =
				new ArticleStateService(articleStateRepository);

		var viewModel = new MainViewModel(
				subscriptionService,
				feedService,
				articleStateService);

		return (viewModel, repository);
	}

	private static FeedSubscription CreateSubscription()
	{
		return new FeedSubscription
		{
			Url = "https://example.com/feed.xml",
			Title = "Example Feed"
		};
	}

	[Fact]
	public async Task RemoveSubscriptionAsync_WhenSuccessful_RemovesSubscription()
	{
		var (viewModel, repository) = CreateViewModel();

		FeedSubscription subscription = CreateSubscription();

		repository.Subscriptions.Add(subscription);

		var subscriptionViewModel =
				new SubscriptionViewModel(subscription);

		viewModel.Subscriptions.Add(subscriptionViewModel);

		bool result = await viewModel.RemoveSubscriptionAsync(
				subscriptionViewModel);

		Assert.True(result);
		Assert.Empty(viewModel.Subscriptions);
		Assert.Empty(repository.Subscriptions);
		Assert.Null(viewModel.SubscriptionOperationError);
		Assert.False(viewModel.IsRemovingSubscription);
	}

	[Fact]
	public async Task RemoveSubscriptionAsync_WhenSelected_ClearsSelection()
	{
		var (viewModel, repository) = CreateViewModel();

		FeedSubscription subscription = CreateSubscription();

		repository.Subscriptions.Add(subscription);

		var subscriptionViewModel =
				new SubscriptionViewModel(subscription);

		viewModel.Subscriptions.Add(subscriptionViewModel);

		viewModel.SelectedSubscription = subscriptionViewModel;

		bool result = await viewModel.RemoveSubscriptionAsync(
				subscriptionViewModel);

		Assert.True(result);
		Assert.Null(viewModel.SelectedSubscription);
		Assert.False(viewModel.HasSelectedSubscription);
		Assert.True(viewModel.HasNoSelectedSubscription);
	}

	[Fact]
	public async Task RemoveSubscriptionAsync_WhenSubscriptionDoesNotExist_ReturnsFalse()
	{
		var (viewModel, _) = CreateViewModel();

		var subscriptionViewModel =
				new SubscriptionViewModel(CreateSubscription());

		bool result = await viewModel.RemoveSubscriptionAsync(
				subscriptionViewModel);

		Assert.False(result);

		Assert.Equal(
				"The subscription could not be found.",
				viewModel.SubscriptionOperationError);

		Assert.False(viewModel.IsRemovingSubscription);
	}

	[Fact]
	public async Task RemoveSubscriptionAsync_WhenRepositoryThrows_PreservesSubscription()
	{
		var (viewModel, repository) = CreateViewModel();

		FeedSubscription subscription = CreateSubscription();

		repository.Subscriptions.Add(subscription);

		var subscriptionViewModel =
				new SubscriptionViewModel(subscription);

		viewModel.Subscriptions.Add(subscriptionViewModel);

		repository.RemoveException =
				new InvalidOperationException("Simulated database failure");

		bool result = await viewModel.RemoveSubscriptionAsync(
				subscriptionViewModel);

		Assert.False(result);

		Assert.Single(viewModel.Subscriptions);
		Assert.Single(repository.Subscriptions);

		Assert.Equal(
				"Unable to remove the subscription. Please try again.",
				viewModel.SubscriptionOperationError);

		Assert.False(viewModel.IsRemovingSubscription);
	}

	[Fact]
	public async Task RemoveSubscriptionAsync_WhenAnotherRemovalIsInProgress_RejectsSecondAttempt()
	{
		var (viewModel, repository) = CreateViewModel();

		FeedSubscription subscription = CreateSubscription();

		repository.Subscriptions.Add(subscription);

		var subscriptionViewModel =
				new SubscriptionViewModel(subscription);

		viewModel.Subscriptions.Add(subscriptionViewModel);

		var removeStarted = new TaskCompletionSource<bool>(
				TaskCreationOptions.RunContinuationsAsynchronously);

		var allowRemove = new TaskCompletionSource<bool>(
				TaskCreationOptions.RunContinuationsAsynchronously);

		repository.RemoveStarted = removeStarted;
		repository.AllowRemove = allowRemove;

		// Start the first removal, but don't let it finish yet.
		Task<bool> firstRemoval =
				viewModel.RemoveSubscriptionAsync(subscriptionViewModel);

		// Wait until the repository confirms removal has started.
		await removeStarted.Task;

		Assert.True(viewModel.IsRemovingSubscription);

		// Attempt a second removal while the first is still running.
		bool secondResult =
				await viewModel.RemoveSubscriptionAsync(subscriptionViewModel);

		Assert.False(secondResult);
		Assert.True(viewModel.IsRemovingSubscription);
		Assert.Single(viewModel.Subscriptions);
		Assert.Single(repository.Subscriptions);

		// Let the first removal finish.
		allowRemove.SetResult(true);

		bool firstResult = await firstRemoval;

		Assert.True(firstResult);
		Assert.Empty(viewModel.Subscriptions);
		Assert.Empty(repository.Subscriptions);
		Assert.False(viewModel.IsRemovingSubscription);
	}
}