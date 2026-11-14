using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.Banner;
using Moka.Red.Feedback.Extensions;
using Moka.Red.Feedback.Toast;

namespace Moka.Red.Feedback.Tests.Components;

public class MokaBannerHostTests : BunitContext
{
	private readonly MemoryStorage _storage = new();

	public MokaBannerHostTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton<IMokaBannerStorage>(_storage);
		Services.AddMokaFeedback();
	}

	private IMokaBannerService Banners => Services.GetRequiredService<IMokaBannerService>();

	[Fact]
	public void ABanner_ShowsItsWords_InALabelledStrip()
	{
		IRenderedComponent<MokaBannerHost> cut = Render<MokaBannerHost>();
		Banners.Show(new MokaBanner
		{
			Id = "maintenance",
			Title = "Maintenance",
			Message = "Down from 22:00 to 23:00.",
			Severity = MokaToastSeverity.Warning,
			LinkText = "Details",
			Href = "/status"
		});

		IElement strip = cut.Find("section.moka-banner-host");
		Assert.Equal("Notices", strip.GetAttribute("aria-label"));
		Assert.Contains("moka-banner--warning", cut.Find(".moka-banner").ClassList);
		Assert.Equal("Maintenance", cut.Find(".moka-banner__title").TextContent);
		Assert.Equal("/status", cut.Find(".moka-banner__link").GetAttribute("href"));
	}

	[Fact]
	public void NoBanners_DrawNoStrip()
	{
		IRenderedComponent<MokaBannerHost> cut = Render<MokaBannerHost>();

		Assert.Empty(cut.FindAll("section"));
	}

	[Fact]
	public void TheSameId_ReplacesTheBannerInPlace()
	{
		IRenderedComponent<MokaBannerHost> cut = Render<MokaBannerHost>();
		Banners.Show(new MokaBanner { Id = "net", Message = "Offline." });
		Banners.Show(new MokaBanner { Id = "update", Message = "A new version is ready." });
		Banners.Show(new MokaBanner { Id = "net", Message = "Back online." });

		Assert.Equal(["Back online.", "A new version is ready."],
			cut.FindAll(".moka-banner__message").Select(m => m.TextContent));
	}

	[Fact]
	public async Task Dismissing_TakesItAway_WithoutRememberingUnlessAsked()
	{
		IRenderedComponent<MokaBannerHost> cut = Render<MokaBannerHost>();
		Banners.Show(new MokaBanner { Id = "net", Message = "Offline." });

		IElement close = cut.Find(".moka-banner__close");
		Assert.Equal("Dismiss", close.GetAttribute("aria-label"));
		await close.ClickAsync(new MouseEventArgs());

		Assert.Empty(Banners.Banners);
		Assert.Empty(_storage.Dismissed);
	}

	[Fact]
	public async Task ARememberedBanner_IsRememberedWhenDismissed()
	{
		IRenderedComponent<MokaBannerHost> cut = Render<MokaBannerHost>();
		Banners.Show(new MokaBanner { Id = "maintenance-11-20", Message = "Maintenance on Friday.", Remember = true });
		cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".moka-banner")));

		await cut.Find(".moka-banner__close").ClickAsync(new MouseEventArgs());

		Assert.Contains("maintenance-11-20", _storage.Dismissed);
	}

	[Fact]
	public void ARememberedDismissal_KeepsTheBannerAway()
	{
		_storage.Dismissed.Add("maintenance-11-20");
		IRenderedComponent<MokaBannerHost> cut = Render<MokaBannerHost>();

		Banners.Show(new MokaBanner { Id = "maintenance-11-20", Message = "Maintenance on Friday.", Remember = true });
		Banners.Show(new MokaBanner { Id = "other", Message = "Shown." });

		cut.WaitForAssertion(() =>
			Assert.Equal(["Shown."], cut.FindAll(".moka-banner__message").Select(m => m.TextContent)));
	}

	[Fact]
	public void ARememberedBanner_WaitsForTheStorage_SoItNeverFlashesUp()
	{
		TaskCompletionSource<bool> answer = new();
		_storage.Answer = answer.Task;
		IRenderedComponent<MokaBannerHost> cut = Render<MokaBannerHost>();
		Banners.Show(new MokaBanner { Id = "tip", Message = "Try the new search.", Remember = true });

		Assert.Empty(cut.FindAll(".moka-banner"));

		answer.SetResult(false);

		cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".moka-banner")));
	}

	[Fact]
	public void ABannerThatCantBeDismissed_HasNoCloseButton()
	{
		IRenderedComponent<MokaBannerHost> cut = Render<MokaBannerHost>();
		Banners.Show(new MokaBanner { Id = "net", Message = "Offline.", Dismissible = false });

		Assert.Empty(cut.FindAll(".moka-banner__close"));
	}

	[Fact]
	public void ABanner_IsHeardPolitely_Once()
	{
		IRenderedComponent<MokaBannerHost> cut = Render<MokaBannerHost>();
		Banners.Show(new MokaBanner { Id = "update", Title = "Update", Message = "Version 2 is ready." });
		Banners.Show(new MokaBanner { Id = "other", Message = "Second." });

		cut.WaitForAssertion(() => Assert.Equal(["Update. Version 2 is ready.", "Second."],
			cut.Find(".moka-announcer[role=status]").Children.Select(c => c.TextContent)));
	}

	[Fact]
	public void AScriptLink_IsDropped()
	{
		IRenderedComponent<MokaBannerHost> cut = Render<MokaBannerHost>();
		Banners.Show(new MokaBanner { Id = "x", Message = "Hi.", LinkText = "Go", Href = "javascript:alert(1)" });

		Assert.Empty(cut.FindAll(".moka-banner__link"));
	}

	[Fact]
	public async Task TheAction_RunsOnItsButton()
	{
		int reloads = 0;
		IRenderedComponent<MokaBannerHost> cut = Render<MokaBannerHost>();
		Banners.Show(new MokaBanner
		{
			Id = "update",
			Message = "A new version is ready.",
			ActionText = "Reload",
			OnAction = () =>
			{
				reloads++;
				return Task.CompletedTask;
			}
		});

		IElement action = cut.Find(".moka-banner__action");
		Assert.Equal("button", action.GetAttribute("type"));
		await action.ClickAsync(new MouseEventArgs());

		Assert.Equal(1, reloads);
	}

	[Fact]
	public void TheWords_CanBeReplaced()
	{
		IRenderedComponent<MokaBannerHost> cut = Render<MokaBannerHost>(p => p.Add(x => x.Text,
			new MokaBannerText { Dismiss = "Fermer", Region = "Avis" }));
		Banners.Show(new MokaBanner { Id = "x", Message = "Bonjour." });

		Assert.Equal("Avis", cut.Find("section").GetAttribute("aria-label"));
		Assert.Equal("Fermer", cut.Find(".moka-banner__close").GetAttribute("aria-label"));
	}

	[Fact]
	public async Task TheBrowserStorage_ReadsAndWritesItsModule()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Moka.Red.Feedback/moka-banners.js");
		module.Setup<bool>("isDismissed", "tip").SetResult(true);
		await using MokaBrowserBannerStorage storage = new(JSInterop.JSRuntime);

		Assert.True(await storage.IsDismissedAsync("tip", Xunit.TestContext.Current.CancellationToken));
		await storage.RememberDismissedAsync("tip", Xunit.TestContext.Current.CancellationToken);

		Assert.Equal("tip", Assert.Single(module.Invocations["remember"]).Arguments[0]);
	}

	private sealed class MemoryStorage : IMokaBannerStorage
	{
		public HashSet<string> Dismissed { get; } = [];

		public Task<bool>? Answer { get; set; }

		public async ValueTask<bool> IsDismissedAsync(string id, CancellationToken cancellationToken = default) =>
			Answer is null ? Dismissed.Contains(id) : await Answer;

		public ValueTask RememberDismissedAsync(string id, CancellationToken cancellationToken = default)
		{
			Dismissed.Add(id);
			return ValueTask.CompletedTask;
		}

		public ValueTask ForgetAsync(string id, CancellationToken cancellationToken = default)
		{
			Dismissed.Remove(id);
			return ValueTask.CompletedTask;
		}
	}
}
