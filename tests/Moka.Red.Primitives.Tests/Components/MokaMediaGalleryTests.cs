using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Primitives.Media;

namespace Moka.Red.Primitives.Tests.Components;

// Clickable thumbnails are native buttons, so the browser gives them focus, Enter and Space. The
// lightbox's focus handling lives in MokaMediaGallery.razor.js; these tests cover the markup, the
// .NET keys and when the script is called.
public class MokaMediaGalleryTests : BunitContext
{
	private const string GalleryModule = "./_content/Moka.Red.Primitives/Media/MokaMediaGallery.razor.js";

	private static readonly MokaMediaItem[] Items =
	[
		new() { Src = "/a.jpg", Alt = "Harbour at dawn" },
		new() { Src = "/b.jpg", Caption = "Market" },
		new() { Src = "/c.jpg" }
	];

	public MokaMediaGalleryTests() => JSInterop.Mode = JSRuntimeMode.Loose;

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
	}

	[Fact]
	public async Task TheLightbox_IsAModalDialogThatTakesFocus()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(GalleryModule);
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p.Add(x => x.Items, Items));
		string? rootId = cut.Find(".moka-media-gallery").GetAttribute("blazor:elementReference");

		await cut.FindAll(".moka-media-gallery-item")[1].ClickAsync(new MouseEventArgs());

		IElement lightbox = cut.Find(".moka-lightbox");
		Assert.Equal("dialog", lightbox.GetAttribute("role"));
		Assert.Equal("true", lightbox.GetAttribute("aria-modal"));
		Assert.Equal("Image viewer", lightbox.GetAttribute("aria-label"));
		Assert.Equal("-1", lightbox.GetAttribute("tabindex"));
		Assert.Equal("polite", cut.Find(".moka-lightbox-counter").GetAttribute("aria-live"));

		JSRuntimeInvocation open = module.VerifyInvoke("openLightbox");
		Assert.Equal(rootId, Assert.IsType<ElementReference>(open.Arguments[0]).Id);
		Assert.Equal(lightbox.GetAttribute("blazor:elementReference"), Assert.IsType<ElementReference>(open.Arguments[1]).Id);
	}

	[Fact]
	public async Task ArrowsMove_AndEscapeClosesAndHandsFocusBackToTheImageOnShow()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(GalleryModule);
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p.Add(x => x.Items, Items));
		string? rootId = cut.Find(".moka-media-gallery").GetAttribute("blazor:elementReference");

		await cut.FindAll(".moka-media-gallery-item")[0].ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-lightbox").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });
		await cut.Find(".moka-lightbox").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });
		await cut.Find(".moka-lightbox").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowLeft" });

		Assert.Equal("/b.jpg", cut.Find(".moka-lightbox-image").GetAttribute("src"));
		Assert.Equal("2 / 3", cut.Find(".moka-lightbox-counter").TextContent);
		Assert.DoesNotContain(module.Invocations, i => i.Identifier == "closeLightbox");

		await cut.Find(".moka-lightbox").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Empty(cut.FindAll(".moka-lightbox"));
		JSRuntimeInvocation close = module.VerifyInvoke("closeLightbox");
		Assert.Equal(rootId, Assert.IsType<ElementReference>(close.Arguments[0]).Id);
		Assert.Equal(1, Assert.IsType<int>(close.Arguments[1]));
		module.VerifyInvoke("openLightbox");
	}

	// Alt+Left is the browser's back. It used to change the image as well.
	[Fact]
	public async Task ArrowsWithAModifier_LeaveTheImageAlone()
	{
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p.Add(x => x.Items, Items));

		await cut.FindAll(".moka-media-gallery-item")[1].ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-lightbox").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowLeft", AltKey = true });

		Assert.Equal("2 / 3", cut.Find(".moka-lightbox-counter").TextContent);

		await cut.Find(".moka-lightbox").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight", CtrlKey = true });

		Assert.Equal("2 / 3", cut.Find(".moka-lightbox-counter").TextContent);
	}

	// The script takes the page's scroll lock in openLightbox and gives it back in closeLightbox.
	// The lock is counted, so each open has to be followed by exactly one close.
	[Fact]
	public async Task EachOpenAndClose_ReachesTheScriptOnce()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(GalleryModule);
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p.Add(x => x.Items, Items));

		await cut.FindAll(".moka-media-gallery-item")[0].ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-lightbox").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });
		await cut.Find(".moka-lightbox-close").ClickAsync(new MouseEventArgs());

		module.VerifyInvoke("openLightbox", 1);
		module.VerifyInvoke("closeLightbox", 1);

		await cut.FindAll(".moka-media-gallery-item")[2].ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-lightbox").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		module.VerifyInvoke("openLightbox", 2);
		module.VerifyInvoke("closeLightbox", 2);
	}

	// A gallery removed with its lightbox open never rendered it closed, so the page stayed locked.
	[Fact]
	public async Task DisposedWithTheLightboxOpen_GivesBackTheScrollLock()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(GalleryModule);
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p.Add(x => x.Items, Items));
		string? rootId = cut.Find(".moka-media-gallery").GetAttribute("blazor:elementReference");
		await cut.FindAll(".moka-media-gallery-item")[1].ClickAsync(new MouseEventArgs());

		await DisposeComponentsAsync();

		JSRuntimeInvocation dispose = module.VerifyInvoke("disposeLightbox");
		Assert.Equal(rootId, Assert.IsType<ElementReference>(dispose.Arguments[0]).Id);
		module.VerifyNotInvoke("closeLightbox");
	}

	[Fact]
	public async Task DisposedWithTheLightboxClosed_LeavesTheScrollLockAlone()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(GalleryModule);
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p.Add(x => x.Items, Items));
		await cut.FindAll(".moka-media-gallery-item")[1].ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-lightbox-close").ClickAsync(new MouseEventArgs());

		await DisposeComponentsAsync();

		module.VerifyInvoke("closeLightbox");
		module.VerifyNotInvoke("disposeLightbox");
	}

	// The Escape that closed the lightbox bubbled on and closed a dialog or drawer around the
	// gallery as well. The host's handler plays that dialog.
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
		await host.Find(".moka-lightbox").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });
		await host.Find(".moka-lightbox").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Empty(host.FindAll(".moka-lightbox"));
		Assert.Empty(host.Instance.Keys);
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
			builder.AddContent(2, ChildContent);
			builder.CloseElement();
		}
	}
}
