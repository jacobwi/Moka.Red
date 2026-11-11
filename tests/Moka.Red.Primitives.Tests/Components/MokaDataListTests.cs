using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Interactions;
using Moka.Red.Icons;
using Moka.Red.Primitives.DataList;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaDataListTests : BunitContext
{
	private const string DragModule = "./_content/Moka.Red.Core/moka-drag.js";

	private static readonly MokaDataListItem[] Profile =
	[
		new("Name", "Jane Doe"),
		new("Email", "jane@example.com"),
		new("Phone")
	];

	[Fact]
	public void WithoutTheNewOptions_TheMarkupIsAsItWas()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p.Add(x => x.Items, Profile));

		cut.MarkupMatches(
			"""
			<dl class="moka-data-list moka-data-list--md">
				<div class="moka-data-list-entry"><dt class="moka-data-list-term">Name</dt><dd class="moka-data-list-description">Jane Doe</dd></div>
				<div class="moka-data-list-entry"><dt class="moka-data-list-term">Email</dt><dd class="moka-data-list-description">jane@example.com</dd></div>
				<div class="moka-data-list-entry"><dt class="moka-data-list-term">Phone</dt><dd class="moka-data-list-description"></dd></div>
			</dl>
			""");
	}

	[Fact]
	public void TheModifiers_AreClassesOnTheList()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p
			.Add(x => x.Items, Profile)
			.Add(x => x.Orientation, MokaDirection.Row)
			.Add(x => x.Striped, true)
			.Add(x => x.Bordered, true)
			.Add(x => x.Dense, true)
			.Add(x => x.Size, MokaSize.Lg));

		Assert.Equal(
			"moka-data-list moka-data-list--lg moka-data-list--row moka-data-list--striped moka-data-list--bordered moka-data-list--dense",
			cut.Find("dl").ClassName);
	}

	[Fact]
	public void ChildContent_RendersInsideTheList_WhenThereAreNoItems()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p
			.AddChildContent("<div><dt>Name</dt><dd>Jane</dd></div>"));

		Assert.Equal("Jane", cut.Find("dl > div > dd").TextContent);
	}

	[Fact]
	public void AnIcon_DrawsBeforeTheTerm_HiddenFromScreenReaders()
	{
		IRenderedComponent<MokaDataList> cut = List(new MokaDataListItem("Address", "10.0.0.4") { Icon = MokaIcons.Content.Globe });

		IElement term = cut.Find("dt");
		Assert.Contains("moka-data-list-term--icon", term.ClassName, StringComparison.Ordinal);
		IElement icon = term.QuerySelector("svg.moka-data-list-icon")!;
		Assert.Equal("true", icon.GetAttribute("aria-hidden"));
		Assert.Equal("Address", term.QuerySelector(".moka-data-list-label")!.TextContent);
	}

	[Fact]
	public void Help_ShowsUnderTheTerm_AsTextAThemeCanHide()
	{
		IRenderedComponent<MokaDataList> cut = List(new MokaDataListItem("Retention", "30 days") { Help = "How long logs are kept." });

		IElement term = cut.Find("dt");
		Assert.Equal("Retention", term.QuerySelector(".moka-data-list-label")!.TextContent);
		IElement help = term.QuerySelector(".moka-data-list-help")!;
		Assert.Equal("How long logs are kept.", help.TextContent);
		Assert.Contains("moka-about", help.ClassName, StringComparison.Ordinal);
		Assert.DoesNotContain("moka-data-list-term--icon", term.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Mono_IsAClassOnTheValue()
	{
		IRenderedComponent<MokaDataList> cut = List(new MokaDataListItem("MAC", "3C:22:FB:10:4A:01") { Mono = true });

		Assert.Contains("moka-data-list-description--mono", cut.Find("dd").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void EmptyText_FillsOnlyTheItemsWithNothingToShow()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p
			.Add(x => x.Items, [.. Profile, new MokaDataListItem("Badge", ValueContent: b => b.AddContent(0, "Admin"))])
			.Add(x => x.EmptyText, "Not set"));

		IReadOnlyList<IElement> values = cut.FindAll("dd");
		Assert.Equal(["Jane Doe", "jane@example.com", "Not set", "Admin"], values.Select(v => v.TextContent));
		Assert.Equal([false, false, true, false],
			values.Select(v => v.ClassList.Contains("moka-data-list-description--empty")));
	}

	[Fact]
	public void Copyable_PutsAQuietCopyButtonAfterTheValue_NamedForTheItem()
	{
		IRenderedComponent<MokaDataList> cut = List(new MokaDataListItem("Email", "jane@example.com") { Copyable = true });

		Assert.Equal("jane@example.com", cut.Find(".moka-copyable__value").TextContent);
		IElement button = cut.Find("dd button");
		Assert.Equal("Copy Email", button.GetAttribute("aria-label"));
		Assert.Contains("moka-copy-btn--quiet", button.ClassName, StringComparison.Ordinal);
		Assert.Equal("moka-data-list-entry moka-quiet-copies", cut.Find("dl > div").ClassName);
		Assert.DoesNotContain("moka-copyable--mono", cut.Find(".moka-copyable").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public async Task CopyText_IsWhatsCopied_WhileTheValueShows()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(DragModule);
		module.Setup<bool?>("copyToClipboard", "ssh-ed25519 AAAAC3Nz full").SetResult(true);
		IRenderedComponent<MokaDataList> cut = List(
			new MokaDataListItem("Key", "ssh-ed25519 AAAA...") { Copyable = true, CopyText = "ssh-ed25519 AAAAC3Nz full" });

		await cut.Find("dd button").ClickAsync(new MouseEventArgs());

		Assert.Equal("ssh-ed25519 AAAA...", cut.Find(".moka-copyable__value").TextContent);
		Assert.Single(module.Invocations, i => i.Identifier == "copyToClipboard");
	}

	[Fact]
	public void Copyable_WithNothingToCopy_DrawsNoButton()
	{
		IRenderedComponent<MokaDataList> cut = List(new MokaDataListItem("Phone") { Copyable = true });

		Assert.Empty(cut.FindAll("button"));
		Assert.Equal("moka-data-list-entry", cut.Find("dl > div").ClassName);
	}

	[Fact]
	public void Copyable_KeepsRichContent_AndCopiesTheValue()
	{
		IRenderedComponent<MokaDataList> cut = List(
			new MokaDataListItem("Status", "online", b => b.AddMarkupContent(0, "<b>Online</b>")) { Copyable = true });

		Assert.Equal("Online", cut.Find(".moka-copyable__value b").TextContent);
		Assert.Equal("Copy Status", cut.Find("dd button").GetAttribute("aria-label"));
	}

	[Fact]
	public async Task TheCopyWords_CanBeReplaced()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(DragModule);
		module.Setup<bool?>("copyToClipboard", "jane@example.com").SetResult(true);
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p
			.Add(x => x.Items, [new MokaDataListItem("E-Mail", "jane@example.com") { Copyable = true }])
			.Add(x => x.CopyLabel, item => $"{item.Label} kopieren")
			.Add(x => x.CopiedText, "Kopiert"));

		Assert.Equal("E-Mail kopieren", cut.Find("dd button").GetAttribute("aria-label"));

		await cut.Find("dd button").ClickAsync(new MouseEventArgs());

		Assert.Equal("Kopiert", cut.Find(".moka-copy-btn__tag").TextContent);
	}

	[Fact]
	public void EndContent_SitsAfterTheValue_AtTheRowsEnd()
	{
		IRenderedComponent<MokaDataList> cut = List(new MokaDataListItem("Password", "********")
		{
			EndContent = b => b.AddMarkupContent(0, "<button type=\"button\">Reveal</button>")
		});

		IElement value = cut.Find("dd");
		Assert.Contains("moka-data-list-description--end", value.ClassName, StringComparison.Ordinal);
		Assert.Equal("********", value.QuerySelector(".moka-data-list-value")!.TextContent);
		Assert.Equal("Reveal", value.QuerySelector(".moka-data-list-end > button")!.TextContent);
	}

	[Fact]
	public void TermWidth_IsAVariableOnTheList()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p
			.Add(x => x.Items, Profile)
			.Add(x => x.Orientation, MokaDirection.Row)
			.Add(x => x.TermWidth, "10rem"));

		IElement list = cut.Find("dl");
		Assert.Contains("moka-data-list--term-width", list.ClassName, StringComparison.Ordinal);
		Assert.Equal("--_dl-term-width: 10rem", list.GetAttribute("style"));
	}

	[Theory]
	[InlineData("10rem; color: red")]
	[InlineData("calc(1px")]
	[InlineData("wide")]
	public void ATermWidthThatIsntALength_IsDropped(string width)
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p.Add(x => x.Items, Profile).Add(x => x.TermWidth, width));

		IElement list = cut.Find("dl");
		Assert.Null(list.GetAttribute("style"));
		Assert.DoesNotContain("moka-data-list--term-width", list.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Columns_SetTheCountAndTheNarrowestColumn()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p
			.Add(x => x.Items, Profile)
			.Add(x => x.Columns, 3)
			.Add(x => x.MinColumn, "12rem"));

		IElement list = cut.Find("dl");
		Assert.Contains("moka-data-list--columns", list.ClassName, StringComparison.Ordinal);
		Assert.Equal("--_dl-columns: 3; --_dl-min-column: 12rem", list.GetAttribute("style"));
	}

	[Theory]
	[InlineData(1)]
	[InlineData(0)]
	[InlineData(-2)]
	public void OneColumnOrFewer_AddsNothing(int columns)
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p
			.Add(x => x.Items, Profile)
			.Add(x => x.Columns, columns)
			.Add(x => x.MinColumn, "12rem"));

		IElement list = cut.Find("dl");
		Assert.Null(list.GetAttribute("style"));
		Assert.DoesNotContain("moka-data-list--columns", list.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void WithoutAListener_RowsHaveNoContextMenuHandler()
	{
		IRenderedComponent<MokaDataList> cut = List(Profile);

		Assert.All(cut.FindAll("dl > div"), row => Assert.False(row.HasAttribute("blazor:oncontextmenu")));
	}

	[Fact]
	public void ARightClick_RaisesTheItem()
	{
		MokaItemContextMenuArgs<MokaDataListItem>? raised = null;
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p
			.Add(x => x.Items, Profile)
			.Add(x => x.OnItemContextMenu, a => raised = a));

		IElement row = cut.FindAll("dl > div")[1];
		Assert.True(row.HasAttribute("blazor:oncontextmenu"));
		Assert.True(row.HasAttribute("blazor:oncontextmenu:preventdefault"));

		row.ContextMenu(new MouseEventArgs { ClientX = 40, ClientY = 12 });

		Assert.NotNull(raised);
		Assert.Same(Profile[1], raised.Item);
		Assert.Equal(40, raised.MouseEvent.ClientX);
	}

	[Fact]
	public void ADisabledList_ListensForNoRightClick_AndLeavesTheBrowsersMenu()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p
			.Add(x => x.Items, Profile)
			.Add(x => x.Disabled, true)
			.Add(x => x.OnItemContextMenu, _ => { }));

		Assert.All(cut.FindAll("dl > div"), row =>
		{
			Assert.False(row.HasAttribute("blazor:oncontextmenu"));
			Assert.False(row.HasAttribute("blazor:oncontextmenu:preventdefault"));
		});
	}

	private IRenderedComponent<MokaDataList> List(params MokaDataListItem[] items) =>
		Render<MokaDataList>(p => p.Add(x => x.Items, items));
}
