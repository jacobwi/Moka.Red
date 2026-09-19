using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.AutoComplete;
using Moka.Red.Forms.DateRangePicker;
using Moka.Red.Forms.SelectField;
using Moka.Red.Forms.TreeSelect;

namespace Moka.Red.Forms.Tests.Components;

// These components wrote the user's pick into their own parameters. A parent re-render passes the
// old value back, so the pick was undone (gotcha #9). A new value from the parent must still win.
public class ParentRerenderTests : BunitContext
{
	private static readonly List<MokaTreeSelectItem<string>> TreeItems = [new("a", "Alpha"), new("b", "Beta")];

	private static readonly string[] Words = ["Alpha", "Beta"];

	public ParentRerenderTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task TreeSelect_KeepsAPick_WhenTheParentPassesTheOldValueAgain()
	{
		IRenderedComponent<MokaTreeSelect<string>> cut = Render<MokaTreeSelect<string>>(p => p
			.Add(x => x.Items, TreeItems)
			.Add(x => x.Value, "a"));

		await cut.Find(".moka-tree-select-trigger").ClickAsync(new MouseEventArgs());
		IElement beta = cut.FindAll(".moka-tree-select-item").Single(i => i.TextContent.Contains("Beta", StringComparison.Ordinal));
		await beta.ClickAsync(new MouseEventArgs());

		cut.Render(p => p.Add(x => x.Items, TreeItems).Add(x => x.Value, "a"));
		Assert.Equal("Beta", cut.Find(".moka-tree-select-display").TextContent.Trim());

		cut.Render(p => p.Add(x => x.Items, TreeItems).Add(x => x.Value, "b"));
		cut.Render(p => p.Add(x => x.Items, TreeItems).Add(x => x.Value, "a"));
		Assert.Equal("Alpha", cut.Find(".moka-tree-select-display").TextContent.Trim());
	}

	[Fact]
	public async Task DateRangePicker_KeepsARange_WhenTheParentPassesTheOldDatesAgain()
	{
		IRenderedComponent<MokaDateRangePicker> cut = Render<MokaDateRangePicker>();

		await cut.Find("input").ClickAsync(new MouseEventArgs());
		IReadOnlyList<IElement> days = cut.FindAll(".moka-daterange__day:not(.moka-daterange__day--other-month)");
		await days[2].ClickAsync(new MouseEventArgs());
		await cut.FindAll(".moka-daterange__day:not(.moka-daterange__day--other-month)")[5].ClickAsync(new MouseEventArgs());

		string picked = cut.Find("input").GetAttribute("value")!;
		Assert.False(string.IsNullOrEmpty(picked));

		cut.Render(p => p.Add(x => x.StartDate, null).Add(x => x.EndDate, null));

		Assert.Equal(picked, cut.Find("input").GetAttribute("value"));
	}

	[Fact]
	public async Task AutoComplete_KeepsAClearedValue_AndShowsANewValueFromTheParent()
	{
		IRenderedComponent<MokaAutoComplete<string>> cut = Render<MokaAutoComplete<string>>(p => p
			.Add(x => x.SearchFunc, _ => Task.FromResult<IEnumerable<string>>(Words))
			.Add(x => x.Clearable, true)
			.Add(x => x.Value, "Alpha"));

		Assert.Equal("Alpha", cut.Find("input").GetAttribute("value"));

		await cut.Find(".moka-autocomplete-clear").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Value, "Alpha"));
		Assert.Equal(string.Empty, cut.Find("input").GetAttribute("value") ?? string.Empty);

		// A value the parent had not passed before replaces the text, even with text in the box.
		cut.Render(p => p.Add(x => x.Value, "Beta"));
		Assert.Equal("Beta", cut.Find("input").GetAttribute("value"));
	}

	// IsOpenChanged was a parameter but IsOpen was not, so the list could not be opened from markup.
	[Fact]
	public async Task Select_OpensFromIsOpen_AndAReRenderWithTheOldValueKeepsItClosed()
	{
		bool? reported = null;
		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p => p
			.Add(x => x.Items, Words)
			.Add(x => x.IsOpen, true)
			.Add(x => x.IsOpenChanged, open => reported = open));

		Assert.Single(cut.FindAll("[role=listbox]"));

		await cut.Find(".moka-select-trigger").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
		Assert.False(reported);
		Assert.Empty(cut.FindAll("[role=listbox]"));

		cut.Render(p => p.Add(x => x.Items, Words).Add(x => x.IsOpen, true));
		Assert.Empty(cut.FindAll("[role=listbox]"));
	}
}
