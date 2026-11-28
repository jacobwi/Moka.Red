using AngleSharp.Dom;
using Bunit;
using Moka.Red.Feedback.Dropdown;
using Moka.Red.Icons;

namespace Moka.Red.Feedback.Tests.Components;

// An item that switched something on or off had no checked state, so it couldn't show one and screen readers heard none.
public class MokaDropdownItemCheckedTests : BunitContext
{
	public MokaDropdownItemCheckedTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void WithoutChecked_TheItemIsAsItWas()
	{
		IElement item = Item(p => p.Add(x => x.Text, "Rename")).Find(".moka-dropdown-item");

		Assert.Equal("menuitem", item.GetAttribute("role"));
		Assert.False(item.HasAttribute("aria-checked"));
		Assert.Empty(item.QuerySelectorAll(".moka-dropdown-item-check"));
		Assert.DoesNotContain("moka-dropdown-item--checked", item.ClassList);
	}

	[Fact]
	public void AnItemThatIsOn_IsACheckedMenuItemCheckbox_WithACheck()
	{
		IElement item = Item(p => p.Add(x => x.Text, "Show grid").Add(x => x.Checked, true)).Find(".moka-dropdown-item");

		Assert.Equal("menuitemcheckbox", item.GetAttribute("role"));
		Assert.Equal("true", item.GetAttribute("aria-checked"));
		Assert.Contains("moka-dropdown-item--checked", item.ClassList);
		IElement check = item.QuerySelector(".moka-dropdown-item-check")!;
		Assert.DoesNotContain("moka-dropdown-item-check--off", check.ClassList);
		Assert.Equal("true", check.GetAttribute("aria-hidden"));
	}

	[Fact]
	public void AnItemThatIsOff_KeepsTheChecksWidth_SoNamesLineUp()
	{
		IElement item = Item(p => p.Add(x => x.Text, "Show rulers").Add(x => x.Checked, false)).Find(".moka-dropdown-item");

		Assert.Equal("menuitemcheckbox", item.GetAttribute("role"));
		Assert.Equal("false", item.GetAttribute("aria-checked"));
		Assert.Contains("moka-dropdown-item-check--off", item.QuerySelector(".moka-dropdown-item-check")!.ClassList);
	}

	[Fact]
	public void TheCheck_TakesTheIconsPlace_OnlyWhileOn()
	{
		IRenderedComponent<MokaDropdownItem> cut = Item(p => p
			.Add(x => x.Text, "Pin")
			.Add(x => x.Icon, MokaIcons.Toggle.Pin)
			.Add(x => x.Checked, true));
		Assert.Single(cut.FindAll(".moka-dropdown-item-icon"));
		Assert.Single(cut.FindAll(".moka-dropdown-item-check"));

		cut.Render(p => p.Add(x => x.Checked, false));

		Assert.Single(cut.FindAll(".moka-dropdown-item-icon"));
		Assert.Empty(cut.FindAll(".moka-dropdown-item-check"));
		Assert.Equal("false", cut.Find(".moka-dropdown-item").GetAttribute("aria-checked"));
	}

	[Fact]
	public void ALink_IsNeverCheckable()
	{
		IElement link = Item(p => p.Add(x => x.Href, "/reports").Add(x => x.Checked, true)).Find("a.moka-dropdown-item");

		Assert.Equal("menuitem", link.GetAttribute("role"));
		Assert.False(link.HasAttribute("aria-checked"));
		Assert.Empty(link.QuerySelectorAll(".moka-dropdown-item-check"));
	}

	[Theory]
	[InlineData(true, "true")]
	[InlineData(false, "false")]
	public void ARadioItem_IsAMenuItemRadio_WithTheSameCheck(bool on, string state)
	{
		IElement item = Item(p => p.Add(x => x.Text, "Original").Add(x => x.Checked, on).Add(x => x.Radio, true))
			.Find(".moka-dropdown-item");

		Assert.Equal("menuitemradio", item.GetAttribute("role"));
		Assert.Equal(state, item.GetAttribute("aria-checked"));
		Assert.Equal(!on, item.QuerySelector(".moka-dropdown-item-check")!.ClassList.Contains("moka-dropdown-item-check--off"));
	}

	[Fact]
	public void Radio_WithoutChecked_IsAPlainItem()
	{
		IElement item = Item(p => p.Add(x => x.Text, "Rename").Add(x => x.Radio, true)).Find(".moka-dropdown-item");

		Assert.Equal("menuitem", item.GetAttribute("role"));
		Assert.False(item.HasAttribute("aria-checked"));
	}

	[Fact]
	public void ADisabledItem_StillSaysWhetherItIsOn()
	{
		IElement item = Item(p => p.Add(x => x.Text, "Wrap").Add(x => x.Checked, true).Add(x => x.Disabled, true))
			.Find(".moka-dropdown-item");

		Assert.Equal("true", item.GetAttribute("aria-checked"));
		Assert.Equal("true", item.GetAttribute("aria-disabled"));
	}

	private IRenderedComponent<MokaDropdownItem> Item(Action<ComponentParameterCollectionBuilder<MokaDropdownItem>> parameters) =>
		Render(parameters);
}
