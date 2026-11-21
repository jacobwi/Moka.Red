using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Icons;
using Moka.Red.Primitives.Button;
using Moka.Red.Primitives.Chip;
using Moka.Red.Primitives.Fab;
using Moka.Red.Primitives.List;
using Moka.Red.Primitives.Media;
using Moka.Red.Primitives.SplitButton;
using Moka.Red.Primitives.Typography;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// A link that navigates keeps the link role however it looks, and one that opens another tab carries
// rel="noopener noreferrer" and says so to screen readers. Without an Href nothing changes.
public sealed class LinkTargetTests : BunitContext
{
	private const string NewTab = ", opens in a new tab";

	public LinkTargetTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	public static TheoryData<bool, bool> DisabledOrBusy => new() { { true, false }, { false, true } };

	private static string? HiddenWords(IElement element) =>
		element.QuerySelector(".moka-visually-hidden:not([role])")?.TextContent;

	private static void AssertInertLink(IElement link)
	{
		Assert.False(link.HasAttribute("href"));
		Assert.False(link.HasAttribute("target"));
		Assert.False(link.HasAttribute("rel"));
		Assert.Equal("link", link.GetAttribute("role"));
		Assert.Equal("true", link.GetAttribute("aria-disabled"));
		Assert.True(link.HasAttribute("blazor:onclick:preventDefault"));
	}

	[Fact]
	public void MokaButton_Link_KeepsTheLinkRole_AndHasNoRelOfItsOwn()
	{
		IElement link = Render<MokaButton>(p => p
			.Add(x => x.Href, "/reports/q3")
			.AddChildContent("Report")).Find("a.moka-btn");

		Assert.Equal("/reports/q3", link.GetAttribute("href"));
		Assert.False(link.HasAttribute("role"));
		Assert.False(link.HasAttribute("target"));
		Assert.False(link.HasAttribute("rel"));
		Assert.Null(HiddenWords(link));
		Assert.False(link.HasAttribute("blazor:onclick:preventDefault"));
	}

	[Fact]
	public void MokaButton_LinkToANewTab_HasRel_AndSaysWhereItOpens()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Href, "https://example.com/docs")
			.Add(x => x.Target, "_blank")
			.AddChildContent("Docs"));
		IElement link = cut.Find("a.moka-btn");

		Assert.Equal("_blank", link.GetAttribute("target"));
		Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
		Assert.Equal(NewTab, HiddenWords(link));
		Assert.Equal("Docs" + NewTab, link.TextContent.Trim());

		cut.Render(p => p.Add(x => x.NewTabText, "öppnas i en ny flik"));

		Assert.Equal(", öppnas i en ny flik", HiddenWords(cut.Find("a.moka-btn")));
	}

	[Fact]
	public void MokaButton_LinkWithASameTabTarget_KeepsItAndAddsNothing()
	{
		IElement link = Render<MokaButton>(p => p
			.Add(x => x.Href, "/docs")
			.Add(x => x.Target, "_self")
			.AddChildContent("Docs")).Find("a.moka-btn");

		Assert.Equal("_self", link.GetAttribute("target"));
		Assert.False(link.HasAttribute("rel"));
		Assert.Null(HiddenWords(link));
	}

	[Theory]
	[MemberData(nameof(DisabledOrBusy))]
	public void MokaButton_DisabledOrBusyLink_IsADisabledLink(bool disabled, bool loading)
	{
		IElement link = Render<MokaButton>(p => p
			.Add(x => x.Href, "/docs")
			.Add(x => x.Target, "_blank")
			.Add(x => x.Disabled, disabled)
			.Add(x => x.Loading, loading)
			.AddChildContent("Docs")).Find("a.moka-btn");

		AssertInertLink(link);
		Assert.Equal("-1", link.GetAttribute("tabindex"));
	}

	[Fact]
	public async Task MokaButton_Link_StillRaisesOnClick()
	{
		int clicks = 0;
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Href, "/docs")
			.Add(x => x.Target, "_blank")
			.Add(x => x.OnClick, _ => clicks++)
			.AddChildContent("Docs"));

		await cut.Find("a").ClickAsync(new MouseEventArgs());

		Assert.Equal(1, clicks);
	}

	[Fact]
	public void MokaButton_TargetWithoutHref_LeavesAPlainButton()
	{
		IElement button = Render<MokaButton>(p => p
			.Add(x => x.Target, "_blank")
			.AddChildContent("Save")).Find("button");

		Assert.False(button.HasAttribute("target"));
		Assert.False(button.HasAttribute("rel"));
		Assert.False(button.HasAttribute("role"));
		Assert.Null(HiddenWords(button));
	}

	[Fact]
	public void MokaSplitButton_WithHref_TheMainPartIsALink_AndTheArrowStaysAButton()
	{
		IRenderedComponent<MokaSplitButton> cut = Render<MokaSplitButton>(p => p
			.Add(x => x.Href, "/reports/new")
			.Add(x => x.DropdownContent, (RenderFragment)(b => b.AddContent(0, "Item")))
			.AddChildContent("New report"));
		IElement link = cut.Find("a.moka-split-btn__primary");

		Assert.Equal("/reports/new", link.GetAttribute("href"));
		Assert.False(link.HasAttribute("type"));
		Assert.False(link.HasAttribute("role"));
		Assert.False(link.HasAttribute("rel"));
		Assert.False(link.HasAttribute("tabindex"));
		Assert.Equal("New report", link.QuerySelector(".moka-split-btn__content")!.TextContent);
		Assert.Null(HiddenWords(link));
		Assert.Equal("BUTTON", cut.Find(".moka-split-btn__toggle").TagName);
		Assert.Empty(cut.FindAll("button.moka-split-btn__primary"));
	}

	[Fact]
	public void MokaSplitButton_LinkToANewTab_HasRel_AndSaysWhereItOpens()
	{
		IElement link = Render<MokaSplitButton>(p => p
			.Add(x => x.Href, "https://example.com/report")
			.Add(x => x.Target, "_blank")
			.AddChildContent("Report")).Find("a.moka-split-btn__primary");

		Assert.Equal("_blank", link.GetAttribute("target"));
		Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
		Assert.Equal(NewTab, HiddenWords(link));
	}

	[Fact]
	public async Task MokaSplitButton_Link_RaisesOnClick_AndListensOnlyWhenSomethingListens()
	{
		int clicks = 0;
		IRenderedComponent<MokaSplitButton> idle = Render<MokaSplitButton>(p => p
			.Add(x => x.Href, "/report")
			.AddChildContent("Report"));
		IRenderedComponent<MokaSplitButton> cut = Render<MokaSplitButton>(p => p
			.Add(x => x.Href, "/report")
			.Add(x => x.OnClick, _ => clicks++)
			.AddChildContent("Report"));
		IElement link = cut.Find("a.moka-split-btn__primary");

		Assert.False(idle.Find("a").HasAttribute("blazor:onclick"));
		Assert.False(link.HasAttribute("blazor:onclick:preventDefault"));
		await link.ClickAsync(new MouseEventArgs());
		Assert.Equal(1, clicks);
	}

	[Theory]
	[MemberData(nameof(DisabledOrBusy))]
	public async Task MokaSplitButton_DisabledOrBusyLink_IsADisabledLink_ThatDoesNothing(bool disabled, bool loading)
	{
		int clicks = 0;
		IRenderedComponent<MokaSplitButton> cut = Render<MokaSplitButton>(p => p
			.Add(x => x.Href, "/report")
			.Add(x => x.Target, "_blank")
			.Add(x => x.Disabled, disabled)
			.Add(x => x.Loading, loading)
			.Add(x => x.OnClick, _ => clicks++)
			.AddChildContent("Report"));
		IElement link = cut.Find("a.moka-split-btn__primary");

		AssertInertLink(link);
		Assert.Equal("-1", link.GetAttribute("tabindex"));
		Assert.Equal(loading ? "true" : null, link.GetAttribute("aria-busy"));
		await link.ClickAsync(new MouseEventArgs());
		Assert.Equal(0, clicks);
	}

	[Fact]
	public void MokaSplitButton_WithAScriptUrl_KeepsTheButton()
	{
		IRenderedComponent<MokaSplitButton> cut = Render<MokaSplitButton>(p => p
			.Add(x => x.Href, "javascript:alert(1)")
			.AddChildContent("Report"));

		Assert.Empty(cut.FindAll("a"));
		Assert.Equal("button", cut.Find(".moka-split-btn__primary").GetAttribute("type"));
	}

	// A button's letter spacing, case and decoration come from the browser's own style for buttons, which a link doesn't
	// get, so the stylesheet sets them on both.
	[Fact]
	public void MokaSplitButton_TheLinkIsDrawnAsTheButton()
	{
		IReadOnlyDictionary<string, string> halves =
			ScopedCss.Declarations("MokaSplitButton.razor.css", ".moka-split-btn__primary");

		Assert.Equal("none", halves["text-decoration"]);
		Assert.Equal("inherit", halves["color"]);
		Assert.Equal("normal", halves["letter-spacing"]);
		Assert.Equal("none", halves["text-transform"]);
	}

	[Fact]
	public void MokaFloatingActionButton_WithHref_IsALinkInTheSameTooltip()
	{
		IRenderedComponent<MokaFloatingActionButton> cut = Render<MokaFloatingActionButton>(p => p
			.Add(x => x.Icon, MokaIcons.Action.Add)
			.Add(x => x.Label, "New report")
			.Add(x => x.Href, "/reports/new"));
		IElement link = cut.Find(".moka-fab > .moka-tooltip-trigger > a.moka-fab__button");

		Assert.Equal("/reports/new", link.GetAttribute("href"));
		Assert.Contains("moka-fab--primary", link.ClassList);
		Assert.False(link.HasAttribute("role"));
		Assert.False(link.HasAttribute("rel"));
		Assert.False(link.HasAttribute("type"));
		Assert.Equal("New report", link.GetAttribute("aria-label"));
		Assert.Equal("New report", cut.Find(".moka-tooltip-popup").TextContent.Trim());
		Assert.Empty(cut.FindAll("button"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void MokaFloatingActionButton_LinkToANewTab_SaysSoInItsName_NotItsTooltip(bool extended)
	{
		IRenderedComponent<MokaFloatingActionButton> cut = Render<MokaFloatingActionButton>(p => p
			.Add(x => x.Icon, MokaIcons.Action.Add)
			.Add(x => x.Label, "Help")
			.Add(x => x.Extended, extended)
			.Add(x => x.Href, "https://example.com/help")
			.Add(x => x.Target, "_blank"));
		IElement link = cut.Find("a.moka-fab__button");

		Assert.Equal("_blank", link.GetAttribute("target"));
		Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
		Assert.Equal("Help" + NewTab, link.GetAttribute("aria-label"));
		if (!extended)
		{
			Assert.Equal("Help", cut.Find(".moka-tooltip-popup").TextContent.Trim());
		}
	}

	[Theory]
	[MemberData(nameof(DisabledOrBusy))]
	public async Task MokaFloatingActionButton_DisabledOrBusyLink_IsADisabledLink_ThatDoesNothing(bool disabled, bool loading)
	{
		int clicks = 0;
		IRenderedComponent<MokaFloatingActionButton> cut = Render<MokaFloatingActionButton>(p => p
			.Add(x => x.Icon, MokaIcons.Action.Add)
			.Add(x => x.Label, "Help")
			.Add(x => x.Href, "/help")
			.Add(x => x.Target, "_blank")
			.Add(x => x.Disabled, disabled)
			.Add(x => x.Loading, loading)
			.Add(x => x.OnClick, _ => clicks++));
		IElement link = cut.Find("a.moka-fab__button");

		AssertInertLink(link);
		Assert.Equal("-1", link.GetAttribute("tabindex"));
		Assert.Equal(loading ? "true" : null, link.GetAttribute("aria-busy"));
		await link.ClickAsync(new MouseEventArgs());
		Assert.Equal(0, clicks);
	}

	[Fact]
	public async Task MokaFloatingActionButton_Link_RaisesOnClick_AndListensOnlyWhenSomethingListens()
	{
		int clicks = 0;
		IRenderedComponent<MokaFloatingActionButton> idle = Render<MokaFloatingActionButton>(p => p
			.Add(x => x.Icon, MokaIcons.Action.Add)
			.Add(x => x.Href, "/help"));
		IRenderedComponent<MokaFloatingActionButton> cut = Render<MokaFloatingActionButton>(p => p
			.Add(x => x.Icon, MokaIcons.Action.Add)
			.Add(x => x.Href, "/help")
			.Add(x => x.OnClick, _ => clicks++));

		Assert.False(idle.Find("a").HasAttribute("blazor:onclick"));
		await cut.Find("a").ClickAsync(new MouseEventArgs());
		Assert.Equal(1, clicks);
	}

	[Fact]
	public void MokaFloatingActionButton_TheLinkIsDrawnAsTheButton()
	{
		IReadOnlyDictionary<string, string> button =
			ScopedCss.Declarations("MokaFloatingActionButton.razor.css", ".moka-fab__button");

		Assert.Equal("none", button["text-decoration"]);
		Assert.Equal("normal", button["letter-spacing"]);
		Assert.Equal("none", button["text-transform"]);
	}

	[Fact]
	public async Task MokaChip_WithHref_IsALink_ThatSaysNothingAboutBeingPressed()
	{
		bool? selected = null;
		int clicks = 0;
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Blazor")
			.Add(x => x.Href, "/tags/blazor")
			.Add(x => x.Selected, false)
			.Add(x => x.SelectedChanged, value => selected = value)
			.Add(x => x.OnClick, _ => clicks++));
		IElement chip = cut.Find("a.moka-chip");

		Assert.Equal("/tags/blazor", chip.GetAttribute("href"));
		Assert.False(chip.HasAttribute("aria-pressed"));
		Assert.False(chip.HasAttribute("role"));
		Assert.DoesNotContain("moka-chip--static", chip.ClassList);
		Assert.False(chip.HasAttribute("blazor:onclick:preventDefault"));

		await chip.ClickAsync(new MouseEventArgs());

		Assert.True(selected);
		Assert.Equal(1, clicks);
	}

	[Fact]
	public void MokaChip_LinkAlone_HasNoClickListener_AndKeepsItsHover()
	{
		IElement chip = Render<MokaChip>(p => p
			.Add(x => x.Text, "Blazor")
			.Add(x => x.Href, "/tags/blazor")).Find("a.moka-chip");

		Assert.False(chip.HasAttribute("blazor:onclick"));
		Assert.DoesNotContain("moka-chip--static", chip.ClassList);
	}

	[Fact]
	public void MokaChip_ClosableLink_IsASpanHoldingTheLink_BesideTheRemoveButton()
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Blazor")
			.Add(x => x.Closable, true)
			.Add(x => x.Href, "https://example.com/blazor")
			.Add(x => x.Target, "_blank"));
		IElement chip = cut.Find(".moka-chip");
		IElement link = cut.Find("a.moka-chip__action");

		Assert.Equal("SPAN", chip.TagName);
		Assert.Contains("moka-chip", link.ParentElement!.ClassList);
		Assert.Equal("https://example.com/blazor", link.GetAttribute("href"));
		Assert.Equal("_blank", link.GetAttribute("target"));
		Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
		Assert.Equal(NewTab, HiddenWords(link));
		Assert.Equal("Remove Blazor", cut.Find("button.moka-chip__close").GetAttribute("aria-label"));
		Assert.Empty(cut.FindAll("button.moka-chip__action"));
	}

	[Fact]
	public void MokaChip_LinkToANewTab_HasRel_AndSaysWhereItOpens()
	{
		IElement chip = Render<MokaChip>(p => p
			.Add(x => x.Text, "Docs")
			.Add(x => x.Href, "https://example.com/docs")
			.Add(x => x.Target, "_blank")).Find("a.moka-chip");

		Assert.Equal("noopener noreferrer", chip.GetAttribute("rel"));
		Assert.Equal(NewTab, HiddenWords(chip));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void MokaChip_DisabledLink_IsADisabledLink(bool closable)
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Docs")
			.Add(x => x.Closable, closable)
			.Add(x => x.Href, "/docs")
			.Add(x => x.Target, "_blank")
			.Add(x => x.Disabled, true));

		IElement link = cut.Find(closable ? "a.moka-chip__action" : "a.moka-chip");
		AssertInertLink(link);
		Assert.Equal("-1", link.GetAttribute("tabindex"));
	}

	[Fact]
	public void MokaChip_TheLinkHasNoUnderline()
	{
		Assert.Equal("none", ScopedCss.Declarations("MokaChip.razor.css", ".moka-chip")["text-decoration"]);
		Assert.Equal("none", ScopedCss.Declarations("MokaChip.razor.css", ".moka-chip__action")["text-decoration"]);
	}

	[Fact]
	public void MokaListItem_LinkToANewTab_HasRel_AMark_AndSaysWhereItOpens()
	{
		IRenderedComponent<MokaList> cut = Render<MokaList>(p => p
			.AddChildContent<MokaListItem>(item => item
				.Add(x => x.Text, "Status page")
				.Add(x => x.Href, "https://status.example.com")
				.Add(x => x.Target, "_blank")
				.Add(x => x.EndContent, (RenderFragment)(b => b.AddContent(0, "Live")))));
		IElement link = cut.Find("a.moka-list-item");

		Assert.Equal("_blank", link.GetAttribute("target"));
		Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
		Assert.Equal(NewTab, HiddenWords(link));
		IElement mark = cut.Find(".moka-list-item__external");
		Assert.Equal("true", mark.GetAttribute("aria-hidden"));
		Assert.Equal(MokaIcons.Content.ExternalLink.SvgPath, mark.QuerySelector("path")!.GetAttribute("d"));
		Assert.Contains("moka-list-item__content", mark.PreviousElementSibling!.PreviousElementSibling!.ClassList);
		Assert.Contains("moka-list-item__end", mark.NextElementSibling!.ClassList);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("_self")]
	public void MokaListItem_LinkInTheSameTab_HasNoMarkOrRel(string? target)
	{
		IElement link = Render<MokaListItem>(p => p
			.Add(x => x.Text, "Invoices")
			.Add(x => x.Href, "/invoices")
			.Add(x => x.Target, target)).Find("a.moka-list-item");

		Assert.Equal(target, link.GetAttribute("target"));
		Assert.False(link.HasAttribute("rel"));
		Assert.Null(HiddenWords(link));
		Assert.Null(link.QuerySelector(".moka-list-item__external"));
	}

	[Fact]
	public void MokaListItem_DisabledLink_DropsTargetAndRel_AndKeepsItsMark()
	{
		IElement link = Render<MokaListItem>(p => p
			.Add(x => x.Text, "Status page")
			.Add(x => x.Href, "https://status.example.com")
			.Add(x => x.Target, "_blank")
			.Add(x => x.Disabled, true)).Find("a.moka-list-item");

		AssertInertLink(link);
		Assert.NotNull(link.QuerySelector(".moka-list-item__external"));
	}

	// The list's script turns Enter and Space into a click only on rows with the interactive class, so a link row keeps
	// the browser's own Enter and isn't followed twice.
	[Fact]
	public void MokaListItem_LinkRow_IsLeftToTheBrowsersEnter()
	{
		IRenderedComponent<MokaList> cut = Render<MokaList>(p => p
			.AddChildContent<MokaListItem>(item => item
				.Add(x => x.Text, "Status page")
				.Add(x => x.Href, "https://status.example.com")
				.Add(x => x.Target, "_blank")
				.Add(x => x.OnClick, _ => { })));
		IElement link = cut.Find("a.moka-list-item");

		Assert.DoesNotContain("moka-list-item--interactive", link.ClassList);
		Assert.False(link.HasAttribute("tabindex"));
		Assert.Empty(JSInterop.Invocations);
	}

	[Fact]
	public void MokaListItem_TheMarkIsQuiet()
	{
		IReadOnlyDictionary<string, string> mark =
			ScopedCss.Declarations("MokaList.razor.css", ".moka-list ::deep .moka-list-item__external");

		Assert.Equal("inline-flex", mark["display"]);
		Assert.Equal("0", mark["flex-shrink"]);
		Assert.Equal("var(--moka-color-on-surface-tertiary)", mark["color"]);
	}

	[Fact]
	public void MokaLightboxAction_LinkToANewTab_SaysSoInItsName_NotItsTooltip()
	{
		IRenderedComponent<MokaLightboxAction> cut = Render<MokaLightboxAction>(p => p
			.Add(x => x.Icon, MokaIcons.Action.Download)
			.Add(x => x.Text, "Open original")
			.Add(x => x.Href, "/full/a.jpg")
			.Add(x => x.Target, "_blank"));
		IElement link = cut.Find("a.moka-lightbox-action");

		Assert.Equal("_blank", link.GetAttribute("target"));
		Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
		Assert.Equal("Open original" + NewTab, link.GetAttribute("aria-label"));
		Assert.Equal("Open original", cut.Find(".moka-tooltip-popup").TextContent.Trim());
	}

	[Fact]
	public void MokaLightboxAction_LinkInTheSameTab_HasNoRel()
	{
		IElement link = Render<MokaLightboxAction>(p => p
			.Add(x => x.Icon, MokaIcons.Action.Download)
			.Add(x => x.Text, "Download")
			.Add(x => x.Href, "/full/a.jpg")).Find("a.moka-lightbox-action");

		Assert.False(link.HasAttribute("target"));
		Assert.False(link.HasAttribute("rel"));
		Assert.Equal("Download", link.GetAttribute("aria-label"));
	}

	[Fact]
	public void MokaLink_External_SaysItOpensANewTab()
	{
		IRenderedComponent<MokaLink> cut = Render<MokaLink>(p => p
			.Add(x => x.Href, "https://example.com")
			.Add(x => x.External, true)
			.AddChildContent("Example"));
		IElement link = cut.Find("a.moka-link");

		Assert.Equal("_blank", link.GetAttribute("target"));
		Assert.Equal(NewTab, HiddenWords(link));
		Assert.Equal("Example" + NewTab, link.TextContent.Trim());

		cut.Render(p => p.Add(x => x.NewTabText, "opens in a new window"));

		Assert.Equal(", opens in a new window", HiddenWords(cut.Find("a.moka-link")));
	}

	[Fact]
	public void MokaLink_NotExternal_HasNoHiddenWords()
	{
		IElement link = Render<MokaLink>(p => p
			.Add(x => x.Href, "/docs")
			.AddChildContent("Docs")).Find("a.moka-link");

		Assert.Null(HiddenWords(link));
	}
}
