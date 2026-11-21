using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Feedback.Dropdown;

namespace Moka.Red.Feedback.Tests.Components;

// A link item with a target for another tab had no rel, and nothing told a screen reader or the eye where it went.
public class MokaDropdownItemTargetTests : BunitContext
{
	private const string Address = "https://example.com/reports/q3";

	public MokaDropdownItemTargetTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void ALinkForANewTab_SaysSo()
	{
		IElement link = Item(p => p.Add(x => x.Href, Address).Add(x => x.Target, "_blank")).Find("a[role=menuitem]");

		Assert.Equal("_blank", link.GetAttribute("target"));
		Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
		Assert.Equal(", opens in a new tab", link.QuerySelector(".moka-visually-hidden")!.TextContent);
		IElement mark = link.QuerySelector(".moka-dropdown-item__external")!;
		Assert.Equal("true", mark.GetAttribute("aria-hidden"));
		Assert.NotNull(mark.QuerySelector("svg"));
		Assert.Equal(["moka-dropdown-item-text", "moka-visually-hidden", "moka-dropdown-item__external"],
			link.Children.Select(child => child.ClassList[0]));
	}

	[Fact]
	public void ALinkWithItsOwnContent_StillSaysSo()
	{
		IElement link = Render<MokaDropdownItem>(p => p
			.Add(x => x.Href, Address)
			.Add(x => x.Target, "_blank")
			.AddChildContent("<strong>Q3 report</strong>")).Find("a[role=menuitem]");

		Assert.Equal("Q3 report, opens in a new tab", link.TextContent.Trim());
	}

	[Fact]
	public void NewTabText_ReplacesTheWords() =>
		Assert.Equal(", öppnas i en ny flik", Item(p => p
			.Add(x => x.Href, Address)
			.Add(x => x.Target, "_blank")
			.Add(x => x.NewTabText, "öppnas i en ny flik")).Find(".moka-visually-hidden").TextContent);

	[Theory]
	[InlineData(null)]
	[InlineData("_self")]
	[InlineData("_parent")]
	public void ALinkForThisTab_AddsNothing(string? target)
	{
		IElement link = Item(p => p.Add(x => x.Href, "/reports/q3").Add(x => x.Target, target)).Find("a[role=menuitem]");

		Assert.False(link.HasAttribute("rel"));
		Assert.Empty(link.QuerySelectorAll(".moka-visually-hidden, .moka-dropdown-item__external"));
	}

	[Fact]
	public void ADisabledLinkForANewTab_SaysNothingAboutIt()
	{
		IElement link = Item(p => p
			.Add(x => x.Href, Address)
			.Add(x => x.Target, "_blank")
			.Add(x => x.Disabled, true)).Find("a[role=menuitem]");

		Assert.False(link.HasAttribute("href"));
		Assert.False(link.HasAttribute("target"));
		Assert.False(link.HasAttribute("rel"));
		Assert.Equal("true", link.GetAttribute("aria-disabled"));
		Assert.Empty(link.QuerySelectorAll(".moka-visually-hidden, .moka-dropdown-item__external"));
	}

	[Fact]
	public void TheAppsOwnRel_StaysOnALinkForThisTab() =>
		Assert.Equal("nofollow", Item(p => p
			.Add(x => x.Href, "/reports/q3")
			.AddUnmatched("rel", "nofollow")).Find("a[role=menuitem]").GetAttribute("rel"));

	[Fact]
	public void ALinkForANewTab_AlwaysGetsNoOpener() =>
		Assert.Equal("noopener noreferrer", Item(p => p
			.Add(x => x.Href, Address)
			.Add(x => x.Target, "_blank")
			.AddUnmatched("rel", "nofollow")).Find("a[role=menuitem]").GetAttribute("rel"));

	[Fact]
	public async Task AClick_ClosesTheMenuAndRaisesOnClick()
	{
		List<string> log = [];
		IRenderedComponent<MokaDropdown> cut = Render<MokaDropdown>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.OpenChanged, open => log.Add(open ? "opened" : "closed"))
			.Add(x => x.ChildContent, "<button type=\"button\">Export</button>")
			.Add(x => x.Items, (RenderFragment)(builder =>
			{
				builder.OpenComponent<MokaDropdownItem>(0);
				builder.AddAttribute(1, nameof(MokaDropdownItem.Text), "Q3 report");
				builder.AddAttribute(2, nameof(MokaDropdownItem.Href), Address);
				builder.AddAttribute(3, nameof(MokaDropdownItem.Target), "_blank");
				builder.AddAttribute(4, nameof(MokaDropdownItem.OnClick),
					EventCallback.Factory.Create<MouseEventArgs>(this, () => log.Add("clicked")));
				builder.CloseComponent();
			})));

		await cut.Find("a[role=menuitem]").ClickAsync(new MouseEventArgs());

		Assert.Equal(["closed", "clicked"], log);
	}

	private IRenderedComponent<MokaDropdownItem> Item(Action<ComponentParameterCollectionBuilder<MokaDropdownItem>> parameters) =>
		Render<MokaDropdownItem>(p =>
		{
			p.Add(x => x.Text, "Report");
			parameters(p);
		});
}
