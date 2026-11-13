using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Keys;
using Moka.Red.Primitives.Media;

namespace Moka.Red.Primitives.Tests.Components;

// Clickable thumbnails are native buttons, so the browser gives them focus, Enter and Space. The
// lightbox is a MokaLightbox (MokaLightboxTests); these tests cover the tiles, how the gallery drives
// its lightbox and when the scripts are called.
public class MokaMediaGalleryTests : BunitContext
{
	private const string GalleryModule = "./_content/Moka.Red.Primitives/Media/MokaMediaGallery.razor.js";
	private const string LightboxModule = "./_content/Moka.Red.Primitives/Media/MokaLightbox.razor.js";

	private static readonly MokaMediaItem[] Items =
	[
		new() { Src = "/a.jpg", Alt = "Harbour at dawn" },
		new() { Src = "/b.jpg", Caption = "Market" },
		new() { Src = "/c.jpg" }
	];

	public MokaMediaGalleryTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private static Task PressAsync<TComponent>(IRenderedComponent<TComponent> cut, string key)
		where TComponent : IComponent =>
		cut.Find(".moka-lightbox").TriggerEventAsync("onmokakeydown",
			new MokaKeyEventArgs { Key = key, Owner = MokaKeyOwner.Modal });

	[Fact]
	public void Thumbnails_AreNamedButtons()
	{
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p.Add(x => x.Items, Items));

		IReadOnlyList<IElement> thumbnails = cut.FindAll(".moka-media-gallery-item");
		Assert.All(thumbnails, thumbnail =>
		{
			Assert.Equal("BUTTON", thumbnail.TagName);
			Assert.Equal("button", thumbnail.GetAttribute("type"));
			Assert.Equal("dialog", thumbnail.GetAttribute("aria-haspopup"));
		});

		// The image's alt names the first; the others fall back to the caption, then their place.
		Assert.False(thumbnails[0].HasAttribute("aria-label"));
		Assert.Equal("Harbour at dawn", thumbnails[0].QuerySelector("img")?.GetAttribute("alt"));
		Assert.Equal("Market", thumbnails[1].GetAttribute("aria-label"));
		Assert.Equal("Image 3 of 3", thumbnails[2].GetAttribute("aria-label"));
	}

	// Without the lightbox or OnItemClick a click does nothing, so the thumbnail is not a control.
	[Fact]
	public async Task OnItemClickWithoutLightbox_IsStillAButton()
	{
		MokaMediaItem? clicked = null;
		IRenderedComponent<MokaMediaGallery> inert = Render<MokaMediaGallery>(p => p
			.Add(x => x.Items, Items)
			.Add(x => x.Lightbox, false));
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p
			.Add(x => x.Items, Items)
			.Add(x => x.Lightbox, false)
			.Add(x => x.OnItemClick, item => clicked = item));

		Assert.All(inert.FindAll(".moka-media-gallery-item"), item => Assert.Equal("DIV", item.TagName));
		IElement thumbnail = cut.FindAll(".moka-media-gallery-item")[1];
		Assert.Equal("BUTTON", thumbnail.TagName);
		Assert.False(thumbnail.HasAttribute("aria-haspopup"));

		await thumbnail.ClickAsync(new MouseEventArgs());

		Assert.Same(Items[1], clicked);
		Assert.Empty(cut.FindAll(".moka-lightbox"));
		Assert.Empty(cut.FindComponents<MokaLightbox>());
	}

	[Fact]
	public async Task TheLightbox_IsAMokaLightboxOverTheGallerysItems()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(LightboxModule);
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p.Add(x => x.Items, Items));

		Assert.Empty(cut.FindAll(".moka-lightbox"));

		await cut.FindAll(".moka-media-gallery-item")[1].ClickAsync(new MouseEventArgs());

		IRenderedComponent<MokaLightbox> lightbox = cut.FindComponent<MokaLightbox>();
		Assert.Same(Items, lightbox.Instance.Items);
		IElement dialog = cut.Find(".moka-lightbox");
		Assert.Equal("dialog", dialog.GetAttribute("role"));
		Assert.Equal("Image viewer", dialog.GetAttribute("aria-label"));
		Assert.Equal("/b.jpg", cut.Find(".moka-lightbox-image").GetAttribute("src"));
		module.VerifyInvoke("openLightbox");
	}

	[Fact]
	public async Task ArrowsMove_AndEscapeClosesAndHandsFocusBackToTheImageOnShow()
	{
		BunitJSModuleInterop lightbox = JSInterop.SetupModule(LightboxModule);
		BunitJSModuleInterop gallery = JSInterop.SetupModule(GalleryModule);
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p.Add(x => x.Items, Items));
		string? rootId = cut.Find(".moka-media-gallery").GetAttribute("blazor:elementReference");

		await cut.FindAll(".moka-media-gallery-item")[0].ClickAsync(new MouseEventArgs());
		await PressAsync(cut, "ArrowRight");
		await PressAsync(cut, "ArrowRight");
		await PressAsync(cut, "ArrowLeft");

		Assert.Equal("/b.jpg", cut.Find(".moka-lightbox-image").GetAttribute("src"));
		lightbox.VerifyNotInvoke("closeLightbox");
		gallery.VerifyNotInvoke("focusItem");

		await PressAsync(cut, "Escape");

		Assert.Empty(cut.FindAll(".moka-lightbox"));
		lightbox.VerifyInvoke("closeLightbox");
		JSRuntimeInvocation focus = gallery.VerifyInvoke("focusItem");
		Assert.Equal(rootId, Assert.IsType<ElementReference>(focus.Arguments[0]).Id);
		Assert.Equal(1, Assert.IsType<int>(focus.Arguments[1]));
	}

	// Stepping only notes where the lightbox is, so a long grid isn't diffed on every step.
	[Fact]
	public async Task SteppingInTheLightbox_LeavesTheGridAlone()
	{
		// Only the grid asks for a waiting tile's name, so the count says how often it drew.
		int gridRenders = 0;
		MokaMediaText text = new() { LoadingLabel = name => $"{name} {++gridRenders}" };
		MokaMediaItem[] items = [.. Items, new MokaMediaItem { ThumbnailSrc = "", Src = "", Alt = "Waiting" }];
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p
			.Add(x => x.Items, items)
			.Add(x => x.Text, text));
		await cut.FindAll(".moka-media-gallery-item")[0].ClickAsync(new MouseEventArgs());
		int renders = gridRenders;

		await PressAsync(cut, "ArrowRight");
		await PressAsync(cut, "ArrowRight");

		Assert.Equal(renders, gridRenders);
		Assert.Equal("/c.jpg", cut.Find(".moka-lightbox-image").GetAttribute("src"));
	}

	// The gallery's own lightbox opens where the tile was, whichever image it showed last time.
	[Fact]
	public async Task EachOpen_StartsAtTheTilePressed()
	{
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p.Add(x => x.Items, Items));

		await cut.FindAll(".moka-media-gallery-item")[0].ClickAsync(new MouseEventArgs());
		await PressAsync(cut, "End");
		await PressAsync(cut, "Escape");
		await cut.FindAll(".moka-media-gallery-item")[0].ClickAsync(new MouseEventArgs());

		Assert.Equal("/a.jpg", cut.Find(".moka-lightbox-image").GetAttribute("src"));
	}

	// The script takes the page's scroll lock when the lightbox shows and gives it back when it goes.
	// The lock is counted, so each open has to be followed by exactly one close.
	[Fact]
	public async Task EachOpenAndClose_ReachesTheScriptOnce()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(LightboxModule);
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p.Add(x => x.Items, Items));

		await cut.FindAll(".moka-media-gallery-item")[0].ClickAsync(new MouseEventArgs());
		await PressAsync(cut, "ArrowRight");
		await cut.Find(".moka-lightbox-close").ClickAsync(new MouseEventArgs());

		module.VerifyInvoke("openLightbox", 1);
		module.VerifyInvoke("closeLightbox", 1);

		await cut.FindAll(".moka-media-gallery-item")[2].ClickAsync(new MouseEventArgs());
		await PressAsync(cut, "Escape");

		module.VerifyInvoke("openLightbox", 2);
		module.VerifyInvoke("closeLightbox", 2);
	}

	// A gallery removed with its lightbox open never rendered it closed, so the page stayed locked.
	[Fact]
	public async Task DisposedWithTheLightboxOpen_GivesBackTheScrollLock()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(LightboxModule);
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p.Add(x => x.Items, Items));
		await cut.FindAll(".moka-media-gallery-item")[1].ClickAsync(new MouseEventArgs());

		await DisposeComponentsAsync();

		module.VerifyInvoke("closeLightbox", 1);
	}

	[Fact]
	public async Task DisposedWithTheLightboxClosed_LeavesTheScrollLockAlone()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(LightboxModule);
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p.Add(x => x.Items, Items));
		await cut.FindAll(".moka-media-gallery-item")[1].ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-lightbox-close").ClickAsync(new MouseEventArgs());

		await DisposeComponentsAsync();

		module.VerifyInvoke("closeLightbox", 1);
	}

	// The Escape that closed the lightbox bubbled on and closed a dialog or drawer around the
	// gallery as well. The host's handlers play that dialog.
	[Fact]
	public async Task LightboxKeys_StayInTheLightbox()
	{
		IRenderedComponent<KeyHost> host = Render<KeyHost>(p => p.Add(x => x.ChildContent, b =>
		{
			b.OpenComponent<MokaMediaGallery>(0);
			b.AddAttribute(1, nameof(MokaMediaGallery.Items), Items);
			b.CloseComponent();
		}));

		await host.FindAll(".moka-media-gallery-item")[0].ClickAsync(new MouseEventArgs());
		Assert.True(host.Find(".moka-lightbox").HasAttribute("blazor:onkeydown:stopPropagation"));
		await PressAsync(host, "ArrowRight");
		await PressAsync(host, "Escape");

		Assert.Empty(host.FindAll(".moka-lightbox"));
		Assert.Empty(host.Instance.Keys);
	}

	[Fact]
	public async Task TheGallerysLightbox_TakesItsSourcesFactsAndWords()
	{
		MokaMediaText text = new() { Viewer = "Fotos" };
		Func<MokaMediaItem, CancellationToken, Task<string?>> load = (_, _) => Task.FromResult<string?>("blob:x");
		Func<MokaMediaItem, string, Task> release = (_, _) => Task.CompletedTask;
		RenderFragment<MokaMediaItem> facts = item => builder => builder.AddContent(0, item.Src);
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p
			.Add(x => x.Items, Items)
			.Add(x => x.LoadSource, load)
			.Add(x => x.ReleaseSource, release)
			.Add(x => x.LightboxFacts, facts)
			.Add(x => x.Text, text));

		await cut.FindAll(".moka-media-gallery-item")[2].ClickAsync(new MouseEventArgs());

		MokaLightbox lightbox = cut.FindComponent<MokaLightbox>().Instance;
		Assert.Same(load, lightbox.LoadSource);
		Assert.Same(release, lightbox.ReleaseSource);
		Assert.Same(text, lightbox.Text);
		Assert.Equal("Fotos", cut.Find(".moka-lightbox").GetAttribute("aria-label"));
		Assert.Equal("/c.jpg", cut.Find(".moka-lightbox-facts").TextContent);
		cut.WaitForAssertion(() => Assert.Equal("blob:x", cut.Find(".moka-lightbox-image").GetAttribute("src")));
	}

	// Today's tiles: an image and the overlay, nothing more.
	[Fact]
	public void ATileWithAThumbnail_IsUnchanged()
	{
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p.Add(x => x.Items, Items));

		IElement tile = cut.FindAll(".moka-media-gallery-item")[0];
		Assert.Equal("moka-media-gallery-item", tile.GetAttribute("class"));
		Assert.Equal(["IMG", "DIV"], tile.Children.Select(child => child.TagName));
		Assert.Equal("lazy", tile.Children[0].GetAttribute("loading"));
		Assert.Empty(cut.FindAll(".moka-skeleton, .moka-media-gallery-failed"));
	}

	[Fact]
	public void ATileWaitingForItsThumbnail_DrawsASkeleton()
	{
		MokaMediaItem[] items =
		[
			new() { Alt = "Pier" },
			new() { Src = "/b.jpg", Caption = "Market", Loading = true }
		];
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p.Add(x => x.Items, items));

		IReadOnlyList<IElement> tiles = cut.FindAll(".moka-media-gallery-item");
		Assert.All(tiles, tile =>
		{
			Assert.Contains("moka-media-gallery-item--loading", tile.ClassList);
			Assert.Empty(tile.QuerySelectorAll("img"));
			Assert.NotNull(tile.QuerySelector(".moka-skeleton.moka-media-gallery-skeleton"));
		});
		Assert.Equal("Pier, loading", tiles[0].GetAttribute("aria-label"));
		Assert.Equal("Market, loading", tiles[1].GetAttribute("aria-label"));
	}

	[Fact]
	public void AFailedTile_SaysSo()
	{
		MokaMediaItem[] items = [new() { Src = "/a.jpg", Alt = "Pier", Failed = true, Loading = true }];
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p.Add(x => x.Items, items));

		IElement tile = cut.Find(".moka-media-gallery-item");
		Assert.Contains("moka-media-gallery-item--failed", tile.ClassList);
		Assert.Equal("Pier, can't be shown", tile.GetAttribute("aria-label"));
		Assert.Empty(tile.QuerySelectorAll("img, .moka-skeleton, .moka-media-gallery-overlay"));
		IElement failed = cut.Find(".moka-media-gallery-failed");
		Assert.Equal("true", failed.GetAttribute("aria-hidden"));
		Assert.Equal("Can't be shown", cut.Find(".moka-media-gallery-failed__text").TextContent);
		Assert.NotNull(failed.QuerySelector("svg"));
	}

	[Fact]
	public async Task AThumbnailThatFailsToLoad_TurnsTheTileFailed_UntilItGetsANewOne()
	{
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p.Add(x => x.Items, Items));

		await cut.FindAll(".moka-media-gallery-item img")[1]
			.TriggerEventAsync("onerror", new Microsoft.AspNetCore.Components.Web.ErrorEventArgs());

		Assert.Contains("moka-media-gallery-item--failed", cut.FindAll(".moka-media-gallery-item")[1].ClassList);
		Assert.Equal("Market, can't be shown", cut.FindAll(".moka-media-gallery-item")[1].GetAttribute("aria-label"));

		MokaMediaItem[] fixedItems = [Items[0], Items[1] with { ThumbnailSrc = "/b-small.jpg" }, Items[2]];
		cut.Render(p => p.Add(x => x.Items, fixedItems));

		Assert.Equal("/b-small.jpg", cut.FindAll(".moka-media-gallery-item img")[1].GetAttribute("src"));
	}

	// A tile that isn't a button still says what it is when it has no picture to show.
	[Fact]
	public void AStaticTileWithoutAPicture_IsANamedImage()
	{
		MokaMediaItem[] items = [new() { Caption = "Pier" }, new() { Src = "/b.jpg", Failed = true }];
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p
			.Add(x => x.Items, items)
			.Add(x => x.Lightbox, false));

		IReadOnlyList<IElement> tiles = cut.FindAll(".moka-media-gallery-item");
		Assert.All(tiles, tile => Assert.Equal("img", tile.GetAttribute("role")));
		Assert.Equal("Pier, loading", tiles[0].GetAttribute("aria-label"));
		Assert.Equal("Image 2 of 2, can't be shown", tiles[1].GetAttribute("aria-label"));
	}

	[Theory]
	[InlineData("9rem", "repeat(auto-fill, minmax(min(100%, 9rem), 1fr))")]
	[InlineData("120", "repeat(auto-fill, minmax(min(100%, 120px), 1fr))")]
	[InlineData("var(--tile)", "repeat(auto-fill, minmax(min(100%, var(--tile)), 1fr))")]
	[InlineData("9rem; color: red", "repeat(4, 1fr)")]
	public void MinColumn_FillsEachRow_AndWinsOverColumns(string minColumn, string template)
	{
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p
			.Add(x => x.Items, Items)
			.Add(x => x.Columns, 4)
			.Add(x => x.MinColumn, minColumn));

		Assert.Contains($"grid-template-columns: {template};", cut.Find(".moka-media-gallery").GetAttribute("style"),
			StringComparison.Ordinal);
	}

	// Equal items used to be told apart only by reference. The index says which tile it was.
	[Fact]
	public async Task OnItemOpen_CarriesTheIndex_OfEqualItems()
	{
		MokaMediaItem first = new() { Src = "/same.jpg" };
		MokaMediaItem second = new() { Src = "/same.jpg" };
		MokaMediaItemOpenArgs? opened = null;
		MokaMediaItem? clicked = null;
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p
			.Add(x => x.Items, new[] { first, second })
			.Add(x => x.Lightbox, false)
			.Add(x => x.OnItemClick, item => clicked = item)
			.Add(x => x.OnItemOpen, args => opened = args));

		Assert.Equal(first, second);
		await cut.FindAll(".moka-media-gallery-item")[1].ClickAsync(new MouseEventArgs());

		Assert.NotNull(opened);
		Assert.Equal(1, opened.Index);
		Assert.Same(second, opened.Item);
		Assert.Same(second, clicked);
	}

	[Fact]
	public void OnItemOpenAlone_MakesTheTilesButtons()
	{
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p
			.Add(x => x.Items, Items)
			.Add(x => x.Lightbox, false)
			.Add(x => x.OnItemOpen, _ => { }));

		Assert.All(cut.FindAll(".moka-media-gallery-item"), tile => Assert.Equal("BUTTON", tile.TagName));
	}

	[Fact]
	public void AKey_TellsEqualItemsApart()
	{
		Assert.NotEqual(new MokaMediaItem { Src = "/a.jpg", Key = 1 }, new MokaMediaItem { Src = "/a.jpg", Key = 2 });
		Assert.Equal(new MokaMediaItem { Src = "/a.jpg", Key = 1 }, new MokaMediaItem { Src = "/a.jpg", Key = 1 });
	}

	// Blazor throws on a repeated key among siblings, but only on the render after. A shared key is left off.
	[Fact]
	public void KeysTheItemsShare_AreLeftOff()
	{
		MokaMediaItem[] items =
		[
			new() { Src = "/a.jpg", Key = "a" },
			new() { Src = "/b.jpg", Key = "b" },
			new() { Src = "/c.jpg", Key = "b" },
			new() { Src = "/d.jpg", Key = "b" }
		];
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p.Add(x => x.Items, items));

		cut.Render(p => p.Add(x => x.Items, items.Reverse().ToArray()));
		cut.Render(p => p.Add(x => x.Items, items));

		Assert.Equal(["/a.jpg", "/b.jpg", "/c.jpg", "/d.jpg"],
			cut.FindAll(".moka-media-gallery-item img").Select(img => img.GetAttribute("src")));
	}

	// A keyed tile turns from a button into a plain image and back when the lightbox is switched off and on.
	[Fact]
	public void KeyedTiles_SwitchBetweenButtonsAndImages()
	{
		MokaMediaItem[] items = [new() { Src = "/a.jpg", Key = "a" }, new() { Src = "/b.jpg", Key = "b" }];
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p.Add(x => x.Items, items));

		cut.Render(p => p.Add(x => x.Lightbox, false));
		Assert.All(cut.FindAll(".moka-media-gallery-item"), tile => Assert.Equal("DIV", tile.TagName));

		cut.Render(p => p.Add(x => x.Lightbox, true));
		Assert.All(cut.FindAll(".moka-media-gallery-item"), tile => Assert.Equal("BUTTON", tile.TagName));
	}

	[Fact]
	public async Task FocusItemAsync_AsksTheScriptForThatTile()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(GalleryModule);
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p.Add(x => x.Items, Items));
		string? rootId = cut.Find(".moka-media-gallery").GetAttribute("blazor:elementReference");

		await cut.InvokeAsync(() => cut.Instance.FocusItemAsync(2));
		await cut.InvokeAsync(() => cut.Instance.FocusItemAsync(3));
		await cut.InvokeAsync(() => cut.Instance.FocusItemAsync(-1));

		JSRuntimeInvocation focus = module.VerifyInvoke("focusItem");
		Assert.Equal(rootId, Assert.IsType<ElementReference>(focus.Arguments[0]).Id);
		Assert.Equal(2, Assert.IsType<int>(focus.Arguments[1]));
	}

	[Fact]
	public async Task FocusItemAsync_DoesNothing_WhenTheTilesAreNotButtons()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(GalleryModule);
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p
			.Add(x => x.Items, Items)
			.Add(x => x.Lightbox, false));

		await cut.InvokeAsync(() => cut.Instance.FocusItemAsync(0));

		module.VerifyNotInvoke("focusItem");
	}

	/// <summary>Records the keys that reach it, as a surrounding MokaDialog's handler would see them.</summary>
	public sealed class KeyHost : ComponentBase
	{
		private readonly List<string> _keys = [];

		[Parameter] public RenderFragment? ChildContent { get; set; }

		public IReadOnlyList<string> Keys => _keys;

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			builder.OpenElement(0, "div");
			builder.AddAttribute(1, "onkeydown",
				EventCallback.Factory.Create<KeyboardEventArgs>(this, e => _keys.Add(e.Key)));
			builder.AddAttribute(2, "onmokakeydown",
				EventCallback.Factory.Create<MokaKeyEventArgs>(this, e => _keys.Add(e.Key)));
			builder.AddContent(3, ChildContent);
			builder.CloseElement();
		}
	}
}
