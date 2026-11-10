using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Theming;
using Moka.Red.Feedback.Extensions;
using Moka.Red.Feedback.Notification;
using Moka.Red.Feedback.NotificationBell;
using Moka.Red.Feedback.Toast;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

// The looks of MokaNotificationCenter and MokaNotificationBell: which one shows, and the parts only some of them draw.
// The drawing itself is CSS, checked as text at the end.
public class NotificationLookTests : BunitContext
{
	private const string CenterModule = "./_content/Moka.Red.Feedback/Notification/MokaNotificationCenter.razor.js";
	private const string CenterCss = "MokaNotificationCenter.razor.css";
	private const string BellCss = "MokaNotificationBell.razor.css";

	// Ten past one in the morning, ten hours ahead of UTC, so today's local date is a day after the UTC one.
	private static readonly DateTimeOffset Now = new(2026, 11, 9, 15, 10, 0, TimeSpan.Zero);
	private static readonly DateTime JustAfterMidnight = new(2026, 11, 9, 14, 30, 0, DateTimeKind.Utc);
	private static readonly DateTime LastNight = new(2026, 11, 9, 13, 0, 0, DateTimeKind.Utc);
	private static readonly DateTime LastWeek = new(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);

	private readonly BunitJSModuleInterop _centerModule;

	public NotificationLookTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_centerModule = JSInterop.SetupModule(CenterModule);
		Services.AddMokaFeedback();
		Services.AddSingleton<TimeProvider>(new FixedClock(Now,
			TimeZoneInfo.CreateCustomTimeZone("Ten ahead", TimeSpan.FromHours(10), "Ten ahead", "Ten ahead")));
	}

	private IMokaNotificationService Notifications => Services.GetRequiredService<IMokaNotificationService>();

	public static TheoryData<MokaNotificationLook, string> Looks => new()
	{
		{ MokaNotificationLook.Inbox, "inbox" },
		{ MokaNotificationLook.Cards, "cards" },
		{ MokaNotificationLook.Compact, "compact" },
		{ MokaNotificationLook.Grouped, "grouped" },
		{ MokaNotificationLook.Timeline, "timeline" },
		{ MokaNotificationLook.Terminal, "terminal" }
	};

	[Theory]
	[MemberData(nameof(Looks))]
	public void ACentersLook_PutsItsClassOnTheRoot(MokaNotificationLook look, string name)
	{
		IRenderedComponent<MokaNotificationCenter> cut = Render<MokaNotificationCenter>(p => p.Add(x => x.Look, look));

		Assert.Contains($"moka-notification-center--{name}", cut.Find(".moka-notification-center").ClassList);
	}

	[Theory]
	[MemberData(nameof(Looks))]
	public void ABellsLook_PutsItsClassOnTheRoot(MokaNotificationLook look, string name)
	{
		IRenderedComponent<MokaNotificationBell> cut = Render<MokaNotificationBell>(p => p.Add(x => x.Look, look));

		Assert.Contains($"moka-notification-bell--{name}", cut.Find("div.moka-notification-bell").ClassList);
	}

	[Fact]
	public void Standard_AddsNothing()
	{
		IRenderedComponent<MokaNotificationCenter> center =
			Render<MokaNotificationCenter>(p => p.Add(x => x.Look, MokaNotificationLook.Standard));
		IRenderedComponent<MokaNotificationBell> bell =
			Render<MokaNotificationBell>(p => p.Add(x => x.Look, MokaNotificationLook.Standard));

		Assert.Equal("moka-notification-center", center.Find(".moka-notification-center").ClassName);
		Assert.Equal("moka-notification-bell moka-notification-bell--md", bell.Find("div.moka-notification-bell").ClassName);
	}

	[Fact]
	public void WithNoLookOfItsOwn_AListTakesTheThemes()
	{
		MokaTheme theme = MokaTheme.Red.WithNotifications(MokaNotificationLook.Timeline);

		IRenderedComponent<CascadingValue<MokaTheme>> center = Render<CascadingValue<MokaTheme>>(p => p
			.Add(x => x.Value, theme)
			.AddChildContent<MokaNotificationCenter>());
		IRenderedComponent<CascadingValue<MokaTheme>> bell = Render<CascadingValue<MokaTheme>>(p => p
			.Add(x => x.Value, theme)
			.AddChildContent<MokaNotificationBell>());

		Assert.Contains("moka-notification-center--timeline", center.Find(".moka-notification-center").ClassList);
		Assert.Contains("moka-notification-bell--timeline", bell.Find("div.moka-notification-bell").ClassList);
	}

	[Fact]
	public void AListsOwnLook_BeatsTheThemes()
	{
		MokaTheme theme = MokaTheme.Red.WithNotifications(MokaNotificationLook.Terminal);

		IRenderedComponent<CascadingValue<MokaTheme>> cards = Render<CascadingValue<MokaTheme>>(p => p
			.Add(x => x.Value, theme)
			.AddChildContent<MokaNotificationCenter>(c => c.Add(x => x.Look, MokaNotificationLook.Cards)));
		IRenderedComponent<CascadingValue<MokaTheme>> standard = Render<CascadingValue<MokaTheme>>(p => p
			.Add(x => x.Value, theme)
			.AddChildContent<MokaNotificationBell>(c => c.Add(x => x.Look, MokaNotificationLook.Standard)));

		Assert.Equal("moka-notification-center moka-notification-center--cards",
			cards.Find(".moka-notification-center").ClassName);
		Assert.Equal("moka-notification-bell moka-notification-bell--md", standard.Find("div.moka-notification-bell").ClassName);
	}

	// The original design keeps its one look, even when a list asks for another, so its markup can't change either.
	[Fact]
	public async Task TheOriginalDesign_GetsNoLook()
	{
		Notifications.Push("Disk almost full", "Only 3.1 GB left.", MokaToastSeverity.Warning);
		Assert.Equal(MokaNotificationLook.Standard, MokaTheme.Dark.WithNotifications(MokaNotificationLook.Terminal).Notifications);

		IRenderedComponent<CascadingValue<MokaTheme>> cut = Render<CascadingValue<MokaTheme>>(p => p
			.Add(x => x.Value, MokaTheme.Dark)
			.AddChildContent<MokaNotificationCenter>(c => c.Add(x => x.Look, MokaNotificationLook.Terminal)));
		await cut.Find("button.moka-notification-bell").ClickAsync(new MouseEventArgs());

		Assert.Equal("moka-notification-center moka-notification-center--open", cut.Find(".moka-notification-center").ClassName);
		Assert.Empty(cut.FindAll(".moka-notification-item__tag"));
		Assert.Empty(cut.FindAll(".moka-notification-panel__count"));
		Assert.Single(cut.FindAll(".moka-notification-item__icon"));
	}

	[Fact]
	public async Task Standard_DrawsNoneOfTheLooksParts()
	{
		Notifications.Push(new MokaNotification { Title = "Backup finished", Message = "1.2 GB copied.", Timestamp = LastWeek });
		IRenderedComponent<MokaNotificationCenter> cut = await OpenCenterAsync(MokaNotificationLook.Standard);

		Assert.Empty(cut.FindAll(".moka-notification-panel__count"));
		Assert.Empty(cut.FindAll(".moka-notification-panel__group"));
		Assert.Empty(cut.FindAll(".moka-notification-item__tag"));
		IElement list = Assert.Single(cut.FindAll("[role=list]"));
		Assert.False(list.HasAttribute("aria-labelledby"));
		Assert.Equal("moka-notification-item__message", cut.Find(".moka-notification-item__message").ClassName);
		Assert.Contains("width: 16px", cut.Find(".moka-notification-item__icon svg").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public async Task Terminal_TagsEachNotificationBySeverity_InPlaceOfItsIcon()
	{
		Notifications.Push(new MokaNotification { Title = "Info", Message = "i", Timestamp = LastWeek.AddMinutes(4) });
		Notifications.Push(new MokaNotification
			{ Title = "Ok", Message = "o", Severity = MokaToastSeverity.Success, Timestamp = LastWeek.AddMinutes(3) });
		Notifications.Push(new MokaNotification
			{ Title = "Warn", Message = "w", Severity = MokaToastSeverity.Warning, Timestamp = LastWeek.AddMinutes(2) });
		Notifications.Push(new MokaNotification
			{ Title = "Fail", Message = "f", Severity = MokaToastSeverity.Error, Timestamp = LastWeek.AddMinutes(1) });
		IRenderedComponent<MokaNotificationCenter> cut = await OpenCenterAsync(MokaNotificationLook.Terminal);

		IReadOnlyList<IElement> tags = cut.FindAll(".moka-notification-item__tag");
		Assert.Equal(["[INFO]", "[ OK ]", "[WARN]", "[FAIL]"], tags.Select(t => t.TextContent));
		Assert.All(tags, tag => Assert.Equal("true", tag.GetAttribute("aria-hidden")));
		Assert.Empty(cut.FindAll(".moka-notification-item__icon"));
	}

	// The terminal look marks unread rows with a glow, so the word a screen reader hears has to stay.
	[Fact]
	public async Task UnderEveryLook_AnUnreadNotificationIsStillDescribedAsUnread()
	{
		Notifications.Push(new MokaNotification { Title = "Backup finished", Message = "Done.", Timestamp = JustAfterMidnight });
		Notifications.Push(new MokaNotification { Title = "Old", Message = "Read.", Timestamp = LastWeek, Read = true });

		foreach (MokaNotificationLook look in Enum.GetValues<MokaNotificationLook>())
		{
			IRenderedComponent<MokaNotificationCenter> cut = await OpenCenterAsync(look);

			IReadOnlyList<IElement> buttons = cut.FindAll("button.moka-notification-item__main");
			Assert.Equal("Unread", cut.Find($"[id='{buttons[0].GetAttribute("aria-describedby")}']").TextContent);
			Assert.False(buttons[1].HasAttribute("aria-describedby"));
		}
	}

	[Theory]
	[InlineData(MokaNotificationLook.Inbox, true)]
	[InlineData(MokaNotificationLook.Grouped, true)]
	[InlineData(MokaNotificationLook.Terminal, true)]
	[InlineData(MokaNotificationLook.Cards, false)]
	[InlineData(MokaNotificationLook.Compact, false)]
	[InlineData(MokaNotificationLook.Timeline, false)]
	[InlineData(MokaNotificationLook.Standard, false)]
	public async Task SomeLooks_CountTheNewOnesInTheHeader(MokaNotificationLook look, bool counts)
	{
		Notifications.Push("One", "1");
		Notifications.Push("Two", "2");
		IRenderedComponent<MokaNotificationCenter> cut = await OpenCenterAsync(look);

		if (counts)
		{
			Assert.Equal("2 new", cut.Find(".moka-notification-panel__header .moka-notification-panel__count").TextContent);
		}
		else
		{
			Assert.Empty(cut.FindAll(".moka-notification-panel__count"));
		}
	}

	[Fact]
	public async Task TheCount_HidesWhenNothingIsNew_AndItsWordsCanBeReplaced()
	{
		Notifications.Push("One", "1");
		IRenderedComponent<MokaNotificationCenter> cut = await OpenCenterAsync(MokaNotificationLook.Inbox,
			p => p.Add(x => x.NewCountText, n => $"{n} nouvelle"));

		Assert.Equal("1 nouvelle", cut.Find(".moka-notification-panel__count").TextContent);

		await cut.InvokeAsync(Notifications.MarkAllAsRead);

		Assert.Empty(cut.FindAll(".moka-notification-panel__count"));
	}

	// Today is the local date of the registered clock, ten hours ahead here, so half past two UTC on the 9th is
	// already the 10th and counts as today, and one in the afternoon UTC on the 9th does not.
	[Fact]
	public async Task Grouped_PutsTodaysOnesUnderTheirOwnHeading_ByTheClocksLocalDate()
	{
		Notifications.Push(new MokaNotification { Title = "Backup finished", Message = "Done.", Timestamp = JustAfterMidnight });
		Notifications.Push(new MokaNotification { Title = "Disk almost full", Message = "3 GB.", Timestamp = LastNight });
		Notifications.Push(new MokaNotification { Title = "Sync failed", Message = "No server.", Timestamp = LastWeek });
		IRenderedComponent<MokaNotificationCenter> cut = await OpenCenterAsync(MokaNotificationLook.Grouped);

		IReadOnlyList<IElement> headings = cut.FindAll(".moka-notification-panel__group");
		Assert.Equal(["Today", "Earlier"], headings.Select(h => h.TextContent));

		IReadOnlyList<IElement> lists = cut.FindAll("[role=list]");
		Assert.Equal(2, lists.Count);
		Assert.Equal(headings[0].Id, lists[0].GetAttribute("aria-labelledby"));
		Assert.Equal(headings[1].Id, lists[1].GetAttribute("aria-labelledby"));
		Assert.Equal(["Backup finished"], Titles(lists[0]));
		Assert.Equal(["Disk almost full", "Sync failed"], Titles(lists[1]));

		// Headings name the lists from outside them: a list holds only its items.
		Assert.All(lists, list => Assert.All(list.Children, child => Assert.Equal("listitem", child.GetAttribute("role"))));
		Assert.All(headings, heading => Assert.False(heading.HasAttribute("tabindex")));
	}

	[Fact]
	public async Task Grouped_ShowsOnlyTheHeadingsItHasRowsFor_InTheWordsItWasGiven()
	{
		Notifications.Push(new MokaNotification { Title = "Backup finished", Message = "Done.", Timestamp = JustAfterMidnight });
		IRenderedComponent<MokaNotificationCenter> cut = await OpenCenterAsync(MokaNotificationLook.Grouped,
			p => p.Add(x => x.TodayText, "Aujourd'hui").Add(x => x.EarlierText, "Plus tôt"));

		Assert.Equal("Aujourd'hui", Assert.Single(cut.FindAll(".moka-notification-panel__group")).TextContent);
	}

	// Focus moves to the row that takes the dismissed one's place on screen, which in Grouped is counted across both
	// lists.
	[Fact]
	public async Task Grouped_DismissingMovesFocusByTheOrderOnScreen()
	{
		Notifications.Push(new MokaNotification { Title = "Backup finished", Message = "Done.", Timestamp = JustAfterMidnight });
		Notifications.Push(new MokaNotification { Title = "Disk almost full", Message = "3 GB.", Timestamp = LastNight });
		Notifications.Push(new MokaNotification { Title = "Sync failed", Message = "No server.", Timestamp = LastWeek });
		IRenderedComponent<MokaNotificationCenter> cut = await OpenCenterAsync(MokaNotificationLook.Grouped);

		await cut.FindAll(".moka-notification-item__dismiss")[1].ClickAsync(new MouseEventArgs());

		Assert.Equal(1, _centerModule.VerifyInvoke("focusNth").Arguments[2]);
		Assert.Equal(["Backup finished", "Sync failed"], cut.FindAll(".moka-notification-item__title").Select(t => t.TextContent));
	}

	[Fact]
	public async Task Compact_KeepsTheMessageForScreenReaders()
	{
		Notifications.Push("Backup finished", "1.2 GB copied.");
		IRenderedComponent<MokaNotificationCenter> cut = await OpenCenterAsync(MokaNotificationLook.Compact);

		IElement message = cut.Find(".moka-notification-item__message");
		Assert.Contains("moka-visually-hidden", message.ClassList);
		Assert.Equal("1.2 GB copied.", message.TextContent);
	}

	[Fact]
	public async Task Timeline_DrawsTheIconSmaller_ToSitInItsRing()
	{
		Notifications.Push("Backup finished", "1.2 GB copied.");
		IRenderedComponent<MokaNotificationCenter> cut = await OpenCenterAsync(MokaNotificationLook.Timeline);

		Assert.Contains("width: 12px", cut.Find(".moka-notification-item__icon svg").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public async Task ABellsTerminalLook_TagsWhatIsNew_SinceItsItemsHaveNoSeverity()
	{
		IRenderedComponent<MokaNotificationBell> cut = await OpenBellAsync(MokaNotificationLook.Terminal,
			[new(Guid.NewGuid(), "Build succeeded", "Pipeline #42.", LastWeek), new(Guid.NewGuid(), "New comment", "Alice.", LastWeek, Read: true)]);

		IReadOnlyList<IElement> tags = cut.FindAll(".moka-notification-bell__item-tag");
		Assert.Equal(["[NEW]", "[---]"], tags.Select(t => t.TextContent));
		Assert.All(tags, tag => Assert.Equal("true", tag.GetAttribute("aria-hidden")));
		Assert.Empty(cut.FindAll(".moka-notification-bell__item-icon"));
		Assert.Equal("1 new", cut.Find(".moka-notification-bell__count").TextContent);
		Assert.Equal("Unread", Description(cut, cut.FindAll("[role=listitem]")[0]));
	}

	[Fact]
	public async Task ABellGrouped_UsesTheClockToo_AndKeepsItsItemsInTheirLists()
	{
		IRenderedComponent<MokaNotificationBell> cut = await OpenBellAsync(MokaNotificationLook.Grouped,
		[
			new(Guid.NewGuid(), "Sync failed", "No server.", LastWeek),
			new(Guid.NewGuid(), "Backup finished", "Done.", JustAfterMidnight, Read: true)
		]);

		IReadOnlyList<IElement> headings = cut.FindAll(".moka-notification-bell__group");
		Assert.Equal(["Today", "Earlier"], headings.Select(h => h.TextContent));
		IReadOnlyList<IElement> lists = cut.FindAll("[role=list]");
		Assert.Equal(headings[0].Id, lists[0].GetAttribute("aria-labelledby"));
		Assert.Equal(["Backup finished"], lists[0].QuerySelectorAll(".moka-notification-bell__item-title").Select(t => t.TextContent));
		Assert.Equal(["Sync failed"], lists[1].QuerySelectorAll(".moka-notification-bell__item-title").Select(t => t.TextContent));
		Assert.Equal("Unread", Description(cut, lists[1].QuerySelector("[role=listitem]")!));
	}

	[Fact]
	public async Task ABellStandard_DrawsNoneOfTheLooksParts()
	{
		IRenderedComponent<MokaNotificationBell> cut = await OpenBellAsync(MokaNotificationLook.Standard,
			[new(Guid.NewGuid(), "Build succeeded", "Pipeline #42.", LastWeek)]);

		Assert.Empty(cut.FindAll(".moka-notification-bell__count"));
		Assert.Empty(cut.FindAll(".moka-notification-bell__group"));
		Assert.Empty(cut.FindAll(".moka-notification-bell__item-tag"));
		Assert.False(cut.Find("[role=list]").HasAttribute("aria-labelledby"));
		Assert.Equal("moka-notification-bell__item-message", cut.Find(".moka-notification-bell__item-message").ClassName);
	}

	// Under the standard look the panel takes the theme's popup look, each value falling back to the one it had.
	[Theory]
	[InlineData(CenterCss, ".moka-gate .moka-notification-panel")]
	[InlineData(BellCss, ".moka-gate .moka-notification-bell__dropdown")]
	public void ThePanel_ReadsThePopupLook_WithItsOldValuesAsFallbacks(string file, string panel)
	{
		IReadOnlyDictionary<string, string> box = ScopedCss.Declarations(file, panel);

		Assert.Equal("var(--moka-popup-bg, var(--moka-color-surface))", box["background-color"]);
		Assert.Equal("var(--moka-popup-border, var(--moka-color-outline-strong))", box["border-color"]);
		Assert.Equal("var(--moka-popup-radius, var(--moka-radius-popup))", box["border-radius"]);
		Assert.Equal("var(--moka-popup-shadow, var(--moka-shadow-popup))", box["box-shadow"]);
		Assert.Equal("var(--moka-popup-blur, none)", box["backdrop-filter"]);
		Assert.Equal("var(--moka-popup-color, var(--moka-color-on-surface))", box["--_nl-text"]);
		Assert.Equal("var(--moka-popup-font, var(--moka-font-family))", box["--_nl-font"]);
	}

	// Cards and Terminal draw a box of their own, so the theme's popup look doesn't reach them.
	[Theory]
	[InlineData(CenterCss, ".moka-gate .moka-notification-center--cards .moka-notification-panel", "var(--moka-color-surface-variant)")]
	[InlineData(CenterCss, ".moka-gate .moka-notification-center--terminal .moka-notification-panel", "var(--moka-color-background)")]
	[InlineData(BellCss, ".moka-gate .moka-notification-bell--cards .moka-notification-bell__dropdown", "var(--moka-color-surface-variant)")]
	[InlineData(BellCss, ".moka-gate .moka-notification-bell--terminal .moka-notification-bell__dropdown", "var(--moka-color-background)")]
	public void CardsAndTerminal_SetTheirOwnBox(string file, string panel, string background)
	{
		IReadOnlyDictionary<string, string> box = ScopedCss.Declarations(file, panel);

		Assert.Equal(background, box["background-color"]);
		Assert.Equal("none", box["backdrop-filter"]);
		Assert.DoesNotContain("--moka-popup", string.Concat(box.Values), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(CenterCss, ".moka-gate .moka-notification-center--terminal .moka-notification-panel")]
	[InlineData(BellCss, ".moka-gate .moka-notification-bell--terminal .moka-notification-bell__dropdown")]
	public void Terminal_IsMonospace(string file, string panel) =>
		Assert.Equal("var(--moka-font-family-mono)", ScopedCss.Declarations(file, panel)["--_nl-font"]);

	// The original design's rules come before the Gate block and never name a look.
	[Theory]
	[InlineData(CenterCss)]
	[InlineData(BellCss)]
	public void TheLooks_LiveOnlyInTheGateBlock(string file)
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", file));
		string matrix = css[..css.IndexOf("/* Gate look.", StringComparison.Ordinal)];

		foreach (string name in new[] { "inbox", "cards", "compact", "grouped", "timeline", "terminal" })
		{
			Assert.DoesNotContain($"--{name}", matrix, StringComparison.Ordinal);
		}
	}

	private static string?[] Titles(IElement list) =>
		[.. list.QuerySelectorAll(".moka-notification-item__title").Select(t => t.TextContent)];

	private static string? Description(IRenderedComponent<MokaNotificationBell> cut, IElement element)
	{
		string? id = element.GetAttribute("aria-describedby");
		return id is null ? null : cut.Find($"[id='{id}']").TextContent;
	}

	private async Task<IRenderedComponent<MokaNotificationCenter>> OpenCenterAsync(MokaNotificationLook look,
		Action<ComponentParameterCollectionBuilder<MokaNotificationCenter>>? configure = null)
	{
		IRenderedComponent<MokaNotificationCenter> cut = Render<MokaNotificationCenter>(p =>
		{
			p.Add(x => x.Look, look);
			configure?.Invoke(p);
		});
		await cut.Find("button.moka-notification-bell").ClickAsync(new MouseEventArgs());
		return cut;
	}

	private async Task<IRenderedComponent<MokaNotificationBell>> OpenBellAsync(MokaNotificationLook look,
		IReadOnlyList<MokaNotificationBellItem> items)
	{
		IRenderedComponent<MokaNotificationBell> cut = Render<MokaNotificationBell>(p => p
			.Add(x => x.Look, look)
			.Add(x => x.Notifications, items)
			.Add(x => x.UnreadCount, items.Count(item => !item.Read)));
		await cut.Find(".moka-notification-bell__trigger").ClickAsync(new MouseEventArgs());
		return cut;
	}

	private sealed class FixedClock(DateTimeOffset now, TimeZoneInfo zone) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => now;

		public override TimeZoneInfo LocalTimeZone => zone;
	}
}
