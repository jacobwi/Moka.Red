using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Primitives.Media;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// TileCaption: a caption under each tile, beside the tile's button rather than in it.
public class MokaMediaGalleryCaptionTests : BunitContext
{
	private const string Stylesheet = "MokaMediaGallery.razor.css";

	private static readonly MokaMediaItem[] Items =
	[
		new() { Key = 1, Src = "/a.jpg", Alt = "Harbour at dawn" },
		new() { Key = 2, Src = "/b.jpg", Alt = "Market" },
		new() { Key = 3, Src = "/c.jpg", Alt = "Dunes" }
	];

	public MokaMediaGalleryCaptionTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	// A name, and a button that does something of its own, as a download would.
	private static RenderFragment<MokaMediaItem> Caption(Action<MokaMediaItem>? pressed = null) => item => builder =>
	{
		builder.OpenElement(0, "span");
		builder.AddAttribute(1, "class", "name");
		builder.AddContent(2, item.Alt);
		builder.CloseElement();
		builder.OpenElement(3, "button");
		builder.AddAttribute(4, "type", "button");
		builder.AddAttribute(5, "class", "save");
		builder.AddAttribute(6, "onclick",
			EventCallback.Factory.Create<MouseEventArgs>(new object(), () => pressed?.Invoke(item)));
		builder.AddContent(7, "Save");
		builder.CloseElement();
	};

	[Fact]
	public void WithoutTileCaption_TheTilesAreTheGridsCells()
	{
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p.Add(x => x.Items, Items));

		Assert.All(cut.Find(".moka-media-gallery").Children, cell => Assert.Equal("BUTTON", cell.TagName));
		Assert.Empty(cut.FindAll("figure, figcaption"));
	}

	[Fact]
	public void TileCaption_PutsACaptionUnderEachTile()
	{
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p
			.Add(x => x.Items, Items)
			.Add(x => x.TileCaption, Caption()));

		IReadOnlyList<IElement> cells = [.. cut.Find(".moka-media-gallery").Children];
		Assert.Equal(3, cells.Count);
		Assert.All(cells, cell =>
		{
			Assert.Equal("FIGURE", cell.TagName);
			Assert.Equal("moka-media-gallery-figure", cell.ClassName);
			Assert.Equal(["BUTTON", "FIGCAPTION"], cell.Children.Select(child => child.TagName));
			Assert.Equal("moka-media-gallery-item", cell.Children[0].ClassName);
			Assert.Equal("moka-media-gallery-caption", cell.Children[1].ClassName);
		});
		Assert.Equal(["Harbour at dawn", "Market", "Dunes"],
			cut.FindAll(".moka-media-gallery-caption .name").Select(name => name.TextContent));
		Assert.Empty(cut.FindAll(".moka-media-gallery-item .save"));
	}

	// The caption is outside the tile's button, so its own controls don't open the lightbox or report an open.
	[Fact]
	public async Task AControlInTheCaption_DoesItsOwnWork_AndOpensNothing()
	{
		List<MokaMediaItem> saved = [];
		List<MokaMediaItem> clicked = [];
		List<MokaMediaItemOpenArgs> opened = [];
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p
			.Add(x => x.Items, Items)
			.Add(x => x.TileCaption, Caption(saved.Add))
			.Add(x => x.OnItemClick, item => clicked.Add(item))
			.Add(x => x.OnItemOpen, args => opened.Add(args)));

		await cut.FindAll(".save")[1].ClickAsync(new MouseEventArgs());

		Assert.Equal([Items[1]], saved);
		Assert.Empty(clicked);
		Assert.Empty(opened);
		Assert.Empty(cut.FindAll(".moka-lightbox"));

		await cut.FindAll(".moka-media-gallery-item")[1].ClickAsync(new MouseEventArgs());

		Assert.Equal([Items[1]], clicked);
		Assert.Equal(1, Assert.Single(opened).Index);
		Assert.Equal("/b.jpg", cut.Find(".moka-lightbox-image").GetAttribute("src"));
	}

	[Fact]
	public void StaticTilesWithCaptions_AreFiguresToo()
	{
		MokaMediaItem[] items = [new() { Src = "/a.jpg", Alt = "Harbour" }, new() { Alt = "Waiting" }];
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p
			.Add(x => x.Items, items)
			.Add(x => x.Lightbox, false)
			.Add(x => x.TileCaption, Caption()));

		IReadOnlyList<IElement> tiles = [.. cut.FindAll("figure > .moka-media-gallery-item")];
		Assert.Equal(["DIV", "DIV"], tiles.Select(tile => tile.TagName));
		Assert.False(tiles[0].HasAttribute("role"));
		Assert.Equal("img", tiles[1].GetAttribute("role"));
		Assert.Equal("Waiting, loading", tiles[1].GetAttribute("aria-label"));
	}

	[Fact]
	public void TheCaptionFollowsItsItem_AsItemsChange()
	{
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p
			.Add(x => x.Items, Items)
			.Add(x => x.TileCaption, Caption()));

		cut.Render(p => p.Add(x => x.Items, [Items[2], Items[0]]));

		Assert.Equal(["Dunes", "Harbour at dawn"], cut.FindAll(".moka-media-gallery-caption .name").Select(name => name.TextContent));
		Assert.Equal(["Dunes", "Harbour at dawn"],
			cut.FindAll("figure > .moka-media-gallery-item img").Select(image => image.GetAttribute("alt")));
	}

	[Fact]
	public async Task FocusItemAsync_WithCaptions_StillAsksTheScriptForThatTile()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Moka.Red.Primitives/Media/MokaMediaGallery.razor.js");
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p
			.Add(x => x.Items, Items)
			.Add(x => x.TileCaption, Caption()));

		await cut.InvokeAsync(() => cut.Instance.FocusItemAsync(2));

		Assert.Equal(2, module.VerifyInvoke("focusItem").Arguments[1]);
	}

	// The figure lays the tile over its caption; the caption's first part, the name, takes the room left and is cut.
	[Fact]
	public void TheCaptionsFirstPart_TakesTheRoomLeft()
	{
		Assert.Equal("column", ScopedCss.Declarations(Stylesheet, ".moka-media-gallery-figure")["flex-direction"]);
		IReadOnlyDictionary<string, string> first = ScopedCss.Declarations(Stylesheet, ".moka-media-gallery-caption ::deep > :first-child");
		Assert.Equal("1 1 auto", first["flex"]);
		Assert.Equal("0", first["min-width"]);
		Assert.Equal("ellipsis", first["text-overflow"]);
		Assert.Equal("none", ScopedCss.Declarations(Stylesheet, ".moka-media-gallery-caption ::deep > *")["flex"]);
	}
}
