using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Keys;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Primitives.Media;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// The lightbox's focus trap, scroll lock and focus return live in MokaLightbox.razor.js; these tests cover the markup,
// the keys, the reads of full images and when the script is called.
public class MokaLightboxTests : BunitContext
{
	private const string LightboxModule = "./_content/Moka.Red.Primitives/Media/MokaLightbox.razor.js";

	private static readonly MokaMediaItem[] Items =
	[
		new() { Src = "/a.jpg", ThumbnailSrc = "/a-small.jpg", Alt = "Harbour at dawn" },
		new() { Src = "/b.jpg", Caption = "Market" },
		new() { Src = "/c.jpg" },
		new() { Src = "/d.jpg" }
	];

	public MokaLightboxTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private static Task PressAsync(IRenderedComponent<MokaLightbox> cut, string key,
		MokaKeyOwner owner = MokaKeyOwner.Modal, bool alt = false) =>
		cut.Find(".moka-lightbox").TriggerEventAsync("onmokakeydown",
			new MokaKeyEventArgs { Key = key, Owner = owner, AltKey = alt });

	private IRenderedComponent<MokaLightbox> RenderOpen(int index = 0,
		Action<ComponentParameterCollectionBuilder<MokaLightbox>>? more = null) =>
		Render<MokaLightbox>(p =>
		{
			p.Add(x => x.Items, Items).Add(x => x.Open, true).Add(x => x.Index, index);
			more?.Invoke(p);
		});

	[Fact]
	public void Closed_RendersNothing()
	{
		IRenderedComponent<MokaLightbox> cut = Render<MokaLightbox>(p => p.Add(x => x.Items, Items));

		Assert.Empty(cut.Nodes);
	}

	[Fact]
	public void Open_IsANamedModalDialog_ThatSaysWhereItIs()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(LightboxModule);
		IRenderedComponent<MokaLightbox> cut = RenderOpen(1);

		IElement dialog = cut.Find(".moka-lightbox");
		Assert.Equal("dialog", dialog.GetAttribute("role"));
		Assert.Equal("true", dialog.GetAttribute("aria-modal"));
		Assert.Equal("Image viewer", dialog.GetAttribute("aria-label"));
		Assert.Equal("-1", dialog.GetAttribute("tabindex"));
		Assert.Equal("/b.jpg", cut.Find(".moka-lightbox-image").GetAttribute("src"));
		Assert.Equal("Market", cut.Find(".moka-lightbox-caption").TextContent);

		IElement counter = cut.Find(".moka-lightbox-counter");
		Assert.Equal("polite", counter.GetAttribute("aria-live"));
		Assert.Equal("2 / 4", counter.QuerySelector("[aria-hidden='true']")?.TextContent);
		Assert.Equal(["Image 2 of 4", "Market"],
			counter.QuerySelectorAll(".moka-visually-hidden").Select(span => span.TextContent));

		JSRuntimeInvocation open = module.VerifyInvoke("openLightbox");
		Assert.StartsWith("moka-lightbox-", Assert.IsType<string>(open.Arguments[0]), StringComparison.Ordinal);
		Assert.Equal(dialog.GetAttribute("blazor:elementReference"), Assert.IsType<ElementReference>(open.Arguments[1]).Id);
	}

	[Fact]
	public void ItsButtons_AreNamedInTooltips()
	{
		IRenderedComponent<MokaLightbox> cut = RenderOpen(1);

		IconTooltip.AssertNamed(cut.Find(".moka-lightbox-nav--prev"), "Previous image", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(cut.Find(".moka-lightbox-nav--next"), "Next image", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(cut.Find(".moka-lightbox-close"), "Close", MokaTooltipPosition.BottomEnd);
	}

	[Fact]
	public void Text_ReplacesEveryWord()
	{
		MokaMediaText text = new()
		{
			Viewer = "Visionneuse",
			Previous = "Précédente",
			Next = "Suivante",
			Close = "Fermer",
			Position = (position, count) => $"Photo {position} sur {count}"
		};
		IRenderedComponent<MokaLightbox> cut = RenderOpen(1, p => p.Add(x => x.Text, text));

		Assert.Equal("Visionneuse", cut.Find(".moka-lightbox").GetAttribute("aria-label"));
		IconTooltip.AssertNamed(cut.Find(".moka-lightbox-nav--prev"), "Précédente", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(cut.Find(".moka-lightbox-nav--next"), "Suivante", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(cut.Find(".moka-lightbox-close"), "Fermer", MokaTooltipPosition.BottomEnd);
		Assert.Equal("Photo 2 sur 4", cut.Find(".moka-lightbox-counter .moka-visually-hidden").TextContent);
	}

	[Fact]
	public async Task Keys_StepThroughTheWholeList_AndReportTheIndex()
	{
		List<int> reported = [];
		IRenderedComponent<MokaLightbox> cut = RenderOpen(1, p => p.Add(x => x.IndexChanged, i => reported.Add(i)));

		await PressAsync(cut, "ArrowRight");
		await PressAsync(cut, "End");
		await PressAsync(cut, "ArrowRight");
		await PressAsync(cut, "Home");
		await PressAsync(cut, "ArrowLeft");

		Assert.Equal([2, 3, 0], reported);
		Assert.Equal("/a.jpg", cut.Find(".moka-lightbox-image").GetAttribute("src"));
		Assert.Empty(cut.FindAll(".moka-lightbox-nav--prev"));
		Assert.Single(cut.FindAll(".moka-lightbox-nav--next"));
	}

	// Alt+Left is the browser's back, and a field or slider in the facts keeps the arrows it uses.
	[Theory]
	[InlineData(MokaKeyOwner.Field, false)]
	[InlineData(MokaKeyOwner.Control, false)]
	[InlineData(MokaKeyOwner.Modal, true)]
	public async Task Arrows_ThatSomethingElseOwns_LeaveTheImageAlone(MokaKeyOwner owner, bool alt)
	{
		IRenderedComponent<MokaLightbox> cut = RenderOpen(1);

		await PressAsync(cut, "ArrowRight", owner, alt);

		Assert.Equal("/b.jpg", cut.Find(".moka-lightbox-image").GetAttribute("src"));
	}

	[Fact]
	public async Task ThePreviousAndNextButtons_Step()
	{
		List<int> reported = [];
		IRenderedComponent<MokaLightbox> cut = RenderOpen(1, p => p.Add(x => x.IndexChanged, i => reported.Add(i)));

		await cut.Find(".moka-lightbox-nav--next").ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-lightbox-nav--prev").ClickAsync(new MouseEventArgs());

		Assert.Equal([2, 1], reported);
	}

	[Fact]
	public async Task Escape_Closes_AndTellsTheScriptOnce()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(LightboxModule);
		List<bool> reported = [];
		IRenderedComponent<MokaLightbox> cut = RenderOpen(0, p => p.Add(x => x.OpenChanged, open => reported.Add(open)));
		string key = Assert.IsType<string>(module.VerifyInvoke("openLightbox").Arguments[0]);

		await PressAsync(cut, "Escape");

		Assert.Equal([false], reported);
		Assert.Empty(cut.Nodes);
		JSRuntimeInvocation close = module.VerifyInvoke("closeLightbox");
		Assert.Equal(key, close.Arguments[0]);

		await DisposeComponentsAsync();
		module.VerifyInvoke("closeLightbox", 1);
	}

	// A click beside the image reaches the root, and the close button's click bubbles there too: one close.
	[Fact]
	public async Task TheCloseButtonAndTheBackdrop_CloseOnce()
	{
		List<bool> reported = [];
		IRenderedComponent<MokaLightbox> cut = RenderOpen(0, p => p.Add(x => x.OpenChanged, open => reported.Add(open)));

		await cut.Find(".moka-lightbox-close").ClickAsync(new MouseEventArgs());

		Assert.Equal([false], reported);
		Assert.Empty(cut.Nodes);
	}

	[Fact]
	public void AClickOnThePictureOrTheFacts_LeavesItOpen()
	{
		IRenderedComponent<MokaLightbox> cut = RenderOpen(0, p => p.Add(x => x.Facts,
			item => builder => builder.AddContent(0, item.Src)));

		Assert.True(cut.Find(".moka-lightbox-content").HasAttribute("blazor:onclick:stopPropagation"));
		Assert.True(cut.Find(".moka-lightbox-facts").HasAttribute("blazor:onclick:stopPropagation"));
	}

	[Fact]
	public void TheKeys_StopAtTheLightbox()
	{
		IRenderedComponent<MokaLightbox> cut = RenderOpen(0);

		IElement dialog = cut.Find(".moka-lightbox");
		Assert.True(dialog.HasAttribute("blazor:onkeydown:stopPropagation"));
		Assert.True(dialog.HasAttribute("blazor:onmokakeydown:stopPropagation"));
	}

	// Opening shows the image the parent names, even when that is the index it passed last time.
	[Fact]
	public async Task Opening_StartsAtIndex()
	{
		IRenderedComponent<MokaLightbox> cut = RenderOpen(1);
		await PressAsync(cut, "End");
		await PressAsync(cut, "Escape");

		cut.Render(p => p.Add(x => x.Open, false));
		cut.Render(p => p.Add(x => x.Open, true));

		Assert.Equal("/b.jpg", cut.Find(".moka-lightbox-image").GetAttribute("src"));
	}

	[Fact]
	public void AnIndexOutsideTheList_IsKeptInside()
	{
		IRenderedComponent<MokaLightbox> cut = RenderOpen(9);

		Assert.Equal("/d.jpg", cut.Find(".moka-lightbox-image").GetAttribute("src"));
	}

	[Fact]
	public void WithoutAFullSource_ItShowsTheThumbnail_AndWithNeitherItWaits()
	{
		MokaMediaItem[] items = [new() { ThumbnailSrc = "/t.jpg" }, new() { Alt = "Lost" }, new() { Alt = "Gone", Failed = true }];
		IRenderedComponent<MokaLightbox> cut = Render<MokaLightbox>(p => p
			.Add(x => x.Items, items).Add(x => x.Open, true));

		Assert.Equal("/t.jpg", cut.Find(".moka-lightbox-image").GetAttribute("src"));

		cut.Render(p => p.Add(x => x.Index, 1));

		Assert.Empty(cut.FindAll(".moka-lightbox-image"));
		Assert.Single(cut.FindAll(".moka-lightbox-wait"));

		cut.Render(p => p.Add(x => x.Index, 2));

		IElement failed = cut.Find(".moka-lightbox-failed");
		Assert.Equal("Can't be shown", failed.TextContent.Trim());
		Assert.Equal(["Image 3 of 3", "Gone", "Can't be shown"],
			cut.FindAll(".moka-lightbox-counter .moka-visually-hidden").Select(span => span.TextContent));
	}

	[Fact]
	public async Task AnImageThatFailsToLoad_IsShownAsFailed()
	{
		IRenderedComponent<MokaLightbox> cut = RenderOpen(2);

		await cut.Find(".moka-lightbox-image").TriggerEventAsync("onerror", new Microsoft.AspNetCore.Components.Web.ErrorEventArgs());

		Assert.Single(cut.FindAll(".moka-lightbox-failed"));
	}

	[Fact]
	public void AFailedItem_IsShownAsFailed_AndNeverRead()
	{
		FakeSources sources = new();
		MokaMediaItem[] items = [new() { Src = "/a.jpg", Failed = true }];
		IRenderedComponent<MokaLightbox> cut = Render<MokaLightbox>(p => p
			.Add(x => x.Items, items).Add(x => x.Open, true).Add(x => x.LoadSource, sources.Load));

		Assert.Single(cut.FindAll(".moka-lightbox-failed"));
		Assert.Empty(sources.Requested);
	}

	[Fact]
	public void Facts_ShowTheItemOnShow()
	{
		IRenderedComponent<MokaLightbox> cut = RenderOpen(1, p => p.Add(x => x.Facts,
			item => builder => builder.AddContent(0, item.Src)));

		Assert.Equal("/b.jpg", cut.Find(".moka-lightbox-facts").TextContent);
	}

	[Fact]
	public void Rounded_ShapesThePicture()
	{
		IRenderedComponent<MokaLightbox> cut = RenderOpen(0, p => p.Add(x => x.RoundedValue, "3px"));

		Assert.Equal("border-radius: 3px", cut.Find(".moka-lightbox-image").GetAttribute("style"));
		Assert.Null(cut.Find(".moka-lightbox").GetAttribute("style"));
	}

	// The image on show is read first, then its neighbours; the thumbnail stands in until the full one arrives.
	[Fact]
	public void OnDemand_ReadsTheImageOnShowAndItsNeighbours_ShowingTheThumbnailMeanwhile()
	{
		FakeSources sources = new();
		IRenderedComponent<MokaLightbox> cut = RenderOpen(0, p => p.Add(x => x.LoadSource, sources.Load));

		Assert.Equal(["/a.jpg", "/b.jpg"], sources.Requested);
		IElement preview = cut.Find(".moka-lightbox-image");
		Assert.Equal("/a-small.jpg", preview.GetAttribute("src"));
		Assert.Contains("moka-lightbox-image--preview", preview.ClassList);
		Assert.Equal("true", cut.Find(".moka-lightbox-content").GetAttribute("aria-busy"));

		cut.InvokeAsync(() => sources.Finish("/a.jpg", "blob:a"));

		cut.WaitForAssertion(() =>
		{
			IElement image = cut.Find(".moka-lightbox-image");
			Assert.Equal("blob:a", image.GetAttribute("src"));
			Assert.DoesNotContain("moka-lightbox-image--preview", image.ClassList);
		});
		Assert.Null(cut.Find(".moka-lightbox-content").GetAttribute("aria-busy"));
	}

	[Fact]
	public void OnDemand_WithNoThumbnail_ShowsASkeleton()
	{
		FakeSources sources = new();
		IRenderedComponent<MokaLightbox> cut = RenderOpen(2, p => p.Add(x => x.LoadSource, sources.Load));

		Assert.Equal(["/c.jpg", "/d.jpg", "/b.jpg"], sources.Requested);
		Assert.Equal("/c.jpg", cut.Find(".moka-lightbox-image--preview").GetAttribute("src"));

		MokaMediaItem[] bare = [new() { Key = 1 }];
		cut.Render(p => p.Add(x => x.Items, bare).Add(x => x.Index, 0));

		Assert.Single(cut.FindAll(".moka-lightbox-wait"));
		Assert.Empty(cut.FindAll(".moka-lightbox-image"));
	}

	[Fact]
	public async Task OnDemand_GivesBackWhatDropsAway_AndCancelsWhatIsStillComing()
	{
		FakeSources sources = new();
		IRenderedComponent<MokaLightbox> cut = RenderOpen(0, p => p
			.Add(x => x.LoadSource, sources.Load)
			.Add(x => x.ReleaseSource, sources.Release));
		await cut.InvokeAsync(() => sources.Finish("/a.jpg", "blob:a"));
		await cut.InvokeAsync(() => sources.Finish("/b.jpg", "blob:b"));
		cut.WaitForAssertion(() => Assert.Equal("blob:a", cut.Find(".moka-lightbox-image").GetAttribute("src")));

		await PressAsync(cut, "End");

		Assert.Equal(["/a.jpg", "/b.jpg", "/d.jpg", "/c.jpg"], sources.Requested);
		cut.WaitForAssertion(() => Assert.Equal(["/a.jpg blob:a", "/b.jpg blob:b"], sources.Released.Order()));

		CancellationToken d = sources.Token("/d.jpg");
		CancellationToken c = sources.Token("/c.jpg");
		await PressAsync(cut, "Home");

		Assert.True(d.IsCancellationRequested);
		Assert.True(c.IsCancellationRequested);

		// A read that ignores its token and answers anyway is given back at once.
		await cut.InvokeAsync(() => sources.Finish("/d.jpg", "blob:d"));
		cut.WaitForAssertion(() => Assert.Contains("/d.jpg blob:d", sources.Released));
	}

	[Fact]
	public async Task OnDemand_ClosingAndGoingAway_GiveBackEverything()
	{
		FakeSources sources = new();
		IRenderedComponent<MokaLightbox> cut = RenderOpen(1, p => p
			.Add(x => x.LoadSource, sources.Load)
			.Add(x => x.ReleaseSource, sources.Release));
		await cut.InvokeAsync(() => sources.Finish("/b.jpg", "blob:b"));
		await cut.InvokeAsync(() => sources.Finish("/c.jpg", "blob:c"));

		await PressAsync(cut, "Escape");

		cut.WaitForAssertion(() => Assert.Equal(["/b.jpg blob:b", "/c.jpg blob:c"], sources.Released.Order()));
		Assert.True(sources.Token("/a.jpg").IsCancellationRequested);

		cut.Render(p => p.Add(x => x.Open, false));
		cut.Render(p => p.Add(x => x.Open, true).Add(x => x.Index, 3));
		await cut.InvokeAsync(() => sources.Finish("/d.jpg", "blob:d"));
		cut.WaitForAssertion(() => Assert.Equal("blob:d", cut.Find(".moka-lightbox-image").GetAttribute("src")));
		CancellationToken c = sources.Token("/c.jpg");

		await DisposeComponentsAsync();

		Assert.Contains("/d.jpg blob:d", sources.Released);
		Assert.True(c.IsCancellationRequested);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task OnDemand_AReadThatFailsOrFindsNothing_IsShownAsFailed(bool throws)
	{
		FakeSources sources = new();
		IRenderedComponent<MokaLightbox> cut = RenderOpen(0, p => p.Add(x => x.LoadSource, sources.Load));

		await cut.InvokeAsync(() =>
		{
			if (throws)
			{
				sources.Fail("/a.jpg");
			}
			else
			{
				sources.Finish("/a.jpg", null);
			}
		});

		cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".moka-lightbox-failed")));
	}

	// A thumbnail arriving makes a new item. With a Key it is still the same picture, so its read isn't started again.
	[Fact]
	public void OnDemand_AnItemWithAKey_IsReadOnce_WhateverElseChanges()
	{
		FakeSources sources = new();
		MokaMediaItem[] before = [new() { Key = "a", Src = "/a.jpg" }];
		IRenderedComponent<MokaLightbox> cut = Render<MokaLightbox>(p => p
			.Add(x => x.Items, before).Add(x => x.Open, true).Add(x => x.LoadSource, sources.Load));

		MokaMediaItem[] after = [before[0] with { ThumbnailSrc = "/a-small.jpg" }];
		cut.Render(p => p.Add(x => x.Items, after));

		Assert.Equal(["/a.jpg"], sources.Requested);
		Assert.False(sources.Token("/a.jpg").IsCancellationRequested);
		Assert.Equal("/a-small.jpg", cut.Find(".moka-lightbox-image--preview").GetAttribute("src"));
	}

	[Fact]
	public void OnDemand_Preload_SetsHowFarItReadsAhead()
	{
		FakeSources sources = new();
		RenderOpen(1, p => p.Add(x => x.LoadSource, sources.Load).Add(x => x.Preload, 0));

		Assert.Equal(["/b.jpg"], sources.Requested);
	}

	/// <summary>An app's reads of full images, each finished by the test.</summary>
	private sealed class FakeSources
	{
		private readonly Dictionary<string, (TaskCompletionSource<string?> Read, CancellationToken Token)> _reads = [];

		public List<string> Requested { get; } = [];

		public List<string> Released { get; } = [];

		public Task<string?> Load(MokaMediaItem item, CancellationToken token)
		{
			TaskCompletionSource<string?> read = new();
			_reads[item.Src] = (read, token);
			Requested.Add(item.Src);
			return read.Task;
		}

		public Task Release(MokaMediaItem item, string url)
		{
			Released.Add($"{item.Src} {url}");
			return Task.CompletedTask;
		}

		public CancellationToken Token(string src) => _reads[src].Token;

		public void Finish(string src, string? url) => _reads[src].Read.SetResult(url);

		public void Fail(string src) => _reads[src].Read.SetException(new InvalidOperationException("Unreadable"));
	}
}
