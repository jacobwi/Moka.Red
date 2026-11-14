using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moka.Red.Feedback.Extensions;
using Moka.Red.Feedback.SystemNotification;

namespace Moka.Red.Feedback.Tests.Services;

public class MokaSystemNotifierTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Feedback/moka-system-notifier.js";

	private readonly BunitJSModuleInterop _module;

	public MokaSystemNotifierTests()
	{
		_module = JSInterop.SetupModule(Module);
		Services.AddMokaSystemNotifier();
	}

	private static CancellationToken Token => Xunit.TestContext.Current.CancellationToken;

	private IMokaSystemNotifier Notifier => Services.GetRequiredService<IMokaSystemNotifier>();

	[Theory]
	[InlineData("granted", MokaSystemNotificationConsent.Granted)]
	[InlineData("denied", MokaSystemNotificationConsent.Denied)]
	[InlineData("default", MokaSystemNotificationConsent.NotAsked)]
	[InlineData("unsupported", MokaSystemNotificationConsent.Unsupported)]
	public async Task ThePermission_IsReadWithoutAsking(string answer, MokaSystemNotificationConsent expected)
	{
		_module.Setup<string>("permission").SetResult(answer);

		Assert.Equal(expected, await Notifier.GetPermissionAsync(Token));
		Assert.Empty(_module.Invocations["requestPermission"]);
	}

	[Fact]
	public async Task Asking_HappensOnlyWhenTheAppAsks()
	{
		_module.Setup<string>("requestPermission").SetResult("granted");

		Assert.Equal(MokaSystemNotificationConsent.Granted, await Notifier.RequestPermissionAsync(Token));
		Assert.Single(_module.Invocations["requestPermission"]);
	}

	[Theory]
	[InlineData("shown", MokaSystemNotificationResult.Shown)]
	[InlineData("visible", MokaSystemNotificationResult.PageVisible)]
	[InlineData("not-allowed", MokaSystemNotificationResult.NotAllowed)]
	[InlineData("unsupported", MokaSystemNotificationResult.Unsupported)]
	public async Task Notify_SaysWhatBecameOfIt(string answer, MokaSystemNotificationResult expected)
	{
		_module.Setup<string>("notify", _ => true).SetResult(answer);

		MokaSystemNotificationResult result = await Notifier.NotifyAsync(
			new MokaSystemNotification { Title = "Build finished" }, Token);

		Assert.Equal(expected, result);
	}

	[Fact]
	public async Task Notify_PassesItsOptions_AndAReferenceOnlyForAClick()
	{
		_module.Setup<string>("notify", _ => true).SetResult("shown");

		await Notifier.NotifyAsync(new MokaSystemNotification
		{
			Title = "Build finished",
			Body = "All green.",
			Tag = "build",
			OnlyWhenHidden = true
		}, Token);
		await Notifier.NotifyAsync(new MokaSystemNotification
		{
			Title = "Message",
			OnClick = () => Task.CompletedTask
		}, Token);

		IReadOnlyList<JSRuntimeInvocation> calls = _module.Invocations["notify"];
		Dictionary<string, object?> options = Assert.IsType<Dictionary<string, object?>>(calls[0].Arguments[0]);
		Assert.Equal("Build finished", options["title"]);
		Assert.Equal("All green.", options["body"]);
		Assert.Equal("build", options["tag"]);
		Assert.Equal(true, options["onlyWhenHidden"]);
		Assert.Null(calls[0].Arguments[1]);
		Assert.NotNull(calls[1].Arguments[1]);
	}

	[Fact]
	public async Task AClick_RunsTheAppsHandler()
	{
		int clicks = 0;
		_module.Setup<string>("notify", _ => true).SetResult("shown");

		await Notifier.NotifyAsync(new MokaSystemNotification
		{
			Title = "Message",
			OnClick = () =>
			{
				clicks++;
				return Task.CompletedTask;
			}
		}, Token);

		DotNetObjectReference<MokaSystemNotificationClick> reference =
			Assert.IsType<DotNetObjectReference<MokaSystemNotificationClick>>(_module.Invocations["notify"][0].Arguments[1]);
		await reference.Value.Clicked();

		Assert.Equal(1, clicks);
	}
}
