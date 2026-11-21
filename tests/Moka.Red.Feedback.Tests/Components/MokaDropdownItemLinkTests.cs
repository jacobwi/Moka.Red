using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Feedback.Dropdown;
using Moka.Red.Icons;

namespace Moka.Red.Feedback.Tests.Components;

// An item that went somewhere was a div that navigated from OnClick, so a middle click, opening it in a new tab and a
// download link didn't work. With Href it's an a element with role="menuitem", as in WAI-ARIA's menu button with links.
public partial class MokaDropdownItemLinkTests : BunitContext
{
	private static readonly RenderFragment TriggerButton = builder =>
	{
		builder.OpenElement(0, "button");
		builder.AddAttribute(1, "type", "button");
		builder.AddContent(2, "Export");
		builder.CloseElement();
	};

	public MokaDropdownItemLinkTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IRenderedComponent<MokaDropdownItem> Item(Action<ComponentParameterCollectionBuilder<MokaDropdownItem>> parameters) =>
		Render<MokaDropdownItem>(p =>
		{
			p.Add(x => x.Text, "Report");
			parameters(p);
		});

	[Fact]
	public void AnItemWithoutHref_KeepsItsMarkup() =>
		Assert.Equal(
			"""<div class="moka-dropdown-item" role="menuitem" tabindex="-1" blazor:onclick="1">""" +
			"""<span class="moka-dropdown-item-text">Report</span></div>""",
			Markup(Item(_ => { })));

	[Fact]
	public void ADisabledItemWithoutHref_KeepsItsMarkup() =>
		Assert.Equal(
			"""<div class="moka-dropdown-item moka-dropdown-item--disabled" role="menuitem" aria-disabled="true" """ +
			"""blazor:onclick="1" blazor:onclick:stopPropagation><span class="moka-dropdown-item-text">Report</span></div>""",
			Markup(Item(p => p.Add(x => x.Disabled, true))));

	[Fact]
	public void Href_MakesTheItemALink()
	{
		IRenderedComponent<MokaDropdownItem> cut = Item(p => p
			.Add(x => x.Href, "/reports/q3.pdf")
			.Add(x => x.Target, "_self"));

		Assert.Equal(
			"""<a class="moka-dropdown-item" role="menuitem" href="/reports/q3.pdf" target="_self" tabindex="-1" blazor:onclick="1">""" +
			"""<span class="moka-dropdown-item-text">Report</span></a>""",
			Markup(cut));
	}

	[Fact]
	public void TheLinksAttributes_ReachIt()
	{
		IElement link = Item(p => p
			.Add(x => x.Href, "/reports/q3.pdf")
			.AddUnmatched("download", "q3.pdf")).Find("a");

		Assert.Equal("q3.pdf", link.GetAttribute("download"));
	}

	[Theory]
	[InlineData("javascript:alert(1)")]
	[InlineData(" JavaScript:alert(1)")]
	[InlineData("vbscript:msgbox(1)")]
	[InlineData("data:text/html,<script>alert(1)</script>")]
	[InlineData("")]
	public void AnUnsafeOrEmptyHref_LeavesAPlainItem(string href)
	{
		IRenderedComponent<MokaDropdownItem> cut = Item(p => p
			.Add(x => x.Href, href)
			.Add(x => x.Target, "_blank"));

		Assert.Empty(cut.FindAll("a"));
		IElement item = cut.Find("div[role=menuitem]");
		Assert.False(item.HasAttribute("href"));
		Assert.False(item.HasAttribute("target"));
	}

	[Fact]
	public void TargetWithoutHref_IsIgnored() =>
		Assert.False(Item(p => p.Add(x => x.Target, "_blank")).Find("[role=menuitem]").HasAttribute("target"));

	[Fact]
	public void ADisabledLink_GoesNowhere()
	{
		IElement link = Item(p => p
			.Add(x => x.Href, "/reports/q3.pdf")
			.Add(x => x.Target, "_blank")
			.Add(x => x.Disabled, true)).Find("a[role=menuitem]");

		Assert.False(link.HasAttribute("href"));
		Assert.False(link.HasAttribute("target"));
		Assert.False(link.HasAttribute("tabindex"));
		Assert.Equal("true", link.GetAttribute("aria-disabled"));
		Assert.Contains("moka-dropdown-item--disabled", link.ClassList);
	}

	[Fact]
	public async Task ALink_ClosesTheMenuAndRaisesOnClick()
	{
		var log = new List<string>();
		IRenderedComponent<MokaDropdown> cut = RenderDropdown(log, disabled: false);

		await cut.Find("a[role=menuitem]").ClickAsync(new MouseEventArgs());

		Assert.Equal(["closed", "clicked"], log);
		Assert.Empty(cut.FindAll("[role=menu]"));
	}

	[Fact]
	public async Task ADisabledLink_LeavesTheMenuOpen()
	{
		var log = new List<string>();
		IRenderedComponent<MokaDropdown> cut = RenderDropdown(log, disabled: true);

		await cut.Find("a[role=menuitem]").ClickAsync(new MouseEventArgs());

		Assert.Empty(log);
		Assert.NotEmpty(cut.FindAll("[role=menu]"));
	}

	[Fact]
	public void ALinkWithAnIcon_DrawsItBeforeTheText()
	{
		IElement link = Item(p => p
			.Add(x => x.Href, "/reports")
			.Add(x => x.Icon, MokaIcons.Content.ExternalLink)).Find("a");

		Assert.Equal(["svg", "span"], link.Children.Select(child => child.LocalName));
		Assert.Contains("moka-dropdown-item-icon", link.Children[0].ClassList);
	}

	private IRenderedComponent<MokaDropdown> RenderDropdown(List<string> log, bool disabled) =>
		Render<MokaDropdown>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.OpenChanged, open => log.Add(open ? "opened" : "closed"))
			.Add(x => x.ChildContent, TriggerButton)
			.Add(x => x.Items, LinkItem(log, disabled)));

	private RenderFragment LinkItem(List<string> log, bool disabled) => builder =>
	{
		builder.OpenComponent<MokaDropdownItem>(0);
		builder.AddAttribute(1, nameof(MokaDropdownItem.Text), "Q3 report");
		builder.AddAttribute(2, nameof(MokaDropdownItem.Href), "/reports/q3");
		builder.AddAttribute(3, nameof(MokaDropdownItem.Disabled), disabled);
		builder.AddAttribute(4, nameof(MokaDropdownItem.OnClick),
			EventCallback.Factory.Create<MouseEventArgs>(this, () => log.Add("clicked")));
		builder.CloseComponent();
	};

	private static string Markup(IRenderedComponent<MokaDropdownItem> cut) => ScopeAttribute().Replace(cut.Markup, "");

	[GeneratedRegex(@" b-\w+(="""")?")]
	private static partial Regex ScopeAttribute();
}
