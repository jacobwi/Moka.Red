using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Icons;
using Moka.Red.Primitives.SegmentedControl;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// The control used tablist and tab roles with no arrow keys and no tab panels. It is now a radio
// group of native radios: the browser owns the tab stop, the arrow keys and the checked state as
// long as the radios share a name. These tests cover the parts Blazor owns.
public class MokaSegmentedControlTests : BunitContext
{
	[Fact]
	public void IsARadioGroup_WithANativeRadioPerSegment()
	{
		IRenderedComponent<MokaSegmentedControl> cut = RenderControl("a", _ => { });

		Assert.Equal("radiogroup", cut.Find(".moka-segmented").GetAttribute("role"));
		Assert.Empty(cut.FindAll("[role=tablist], [role=tab]"));
		Assert.Equal(3, cut.FindAll(".moka-segment input[type=radio]").Count);
		Assert.All(cut.FindAll(".moka-segment"), segment => Assert.Equal("LABEL", segment.TagName));
	}

	[Fact]
	public void Radios_ShareOneName_ThatAnotherControlDoesNotUse()
	{
		IRenderedComponent<MokaSegmentedControl> first = RenderControl("a", _ => { });
		IRenderedComponent<MokaSegmentedControl> second = RenderControl("a", _ => { });

		IReadOnlyList<IElement> radios = first.FindAll("input[type=radio]");
		string? name = radios[0].GetAttribute("name");
		Assert.False(string.IsNullOrEmpty(name));
		Assert.All(radios, radio => Assert.Equal(name, radio.GetAttribute("name")));
		Assert.NotEqual(name, second.Find("input[type=radio]").GetAttribute("name"));
	}

	[Fact]
	public void SelectedSegment_HasTheCheckedRadio()
	{
		IRenderedComponent<MokaSegmentedControl> cut = RenderControl("b", _ => { });

		Assert.Equal([false, true, false], cut.FindAll("input[type=radio]").Select(r => r.HasAttribute("checked")));
		Assert.Contains("moka-segment--active", cut.FindAll(".moka-segment")[1].ClassList);
	}

	// An arrow key checks the next radio, and the browser raises change on it, just as a click does.
	[Fact]
	public async Task Change_SelectsTheSegment_AndRaisesValueChanged()
	{
		string? chosen = null;
		IRenderedComponent<MokaSegmentedControl> cut = RenderControl("a", value => chosen = value);

		await cut.FindAll("input[type=radio]")[2].ChangeAsync(new ChangeEventArgs { Value = "c" });

		Assert.Equal("c", chosen);
		Assert.Equal([false, false, true], cut.FindAll("input[type=radio]").Select(r => r.HasAttribute("checked")));
		Assert.Equal([false, false, true], cut.FindAll(".moka-segment").Select(s => s.ClassList.Contains("moka-segment--active")));
	}

	[Fact]
	public void AriaLabel_NamesTheGroup()
	{
		IRenderedComponent<MokaSegmentedControl> cut = Render<MokaSegmentedControl>(p => p
			.Add(x => x.AriaLabel, "Time range")
			.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "day").Add(x => x.Text, "Day")));

		Assert.Equal("Time range", cut.Find("[role=radiogroup]").GetAttribute("aria-label"));
	}

	[Fact]
	public async Task DisabledControl_DisablesEveryRadio_AndIgnoresChange()
	{
		string? chosen = null;
		IRenderedComponent<MokaSegmentedControl> cut = RenderControl("a", value => chosen = value, disabled: true);

		await cut.FindAll("input[type=radio]")[1].ChangeAsync(new ChangeEventArgs { Value = "b" });

		Assert.Null(chosen);
		Assert.All(cut.FindAll("input[type=radio]"), radio => Assert.True(radio.HasAttribute("disabled")));
		Assert.All(cut.FindAll(".moka-segment"), segment => Assert.Contains("moka-segment--disabled", segment.ClassList));
		Assert.Equal("true", cut.Find("[role=radiogroup]").GetAttribute("aria-disabled"));
	}

	[Fact]
	public async Task DisabledSegment_IsADisabledRadio_AndIgnoresChange()
	{
		string? chosen = null;
		IRenderedComponent<MokaSegmentedControl> cut = Render<MokaSegmentedControl>(p => p
			.Add(x => x.Value, "a")
			.Add(x => x.ValueChanged, value => chosen = value)
			.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "a").Add(x => x.Text, "A"))
			.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "b").Add(x => x.Text, "B").Add(x => x.Disabled, true)));

		IElement disabled = cut.FindAll("input[type=radio]")[1];
		await disabled.ChangeAsync(new ChangeEventArgs { Value = "b" });

		Assert.True(disabled.HasAttribute("disabled"));
		Assert.False(cut.FindAll("input[type=radio]")[0].HasAttribute("disabled"));
		Assert.Null(chosen);
	}

	// An icon-only segment has no text, so its name has to reach the radio, not the label around it.
	[Fact]
	public void IconOnlySegment_PassesAriaLabelAndTitleToTheRadio()
	{
		IRenderedComponent<MokaSegmentedControl> cut = Render<MokaSegmentedControl>(p => p
			.Add(x => x.Value, "dense")
			.AddChildContent<MokaSegment>(s => s
				.Add(x => x.Value, "dense")
				.Add(x => x.Icon, MokaIcons.Content.RowsDense)
				.AddUnmatched("aria-label", "Dense rows")
				.AddUnmatched("title", "Dense rows")));

		IElement radio = cut.Find("input[type=radio]");
		Assert.Equal("Dense rows", radio.GetAttribute("aria-label"));
		Assert.Equal("Dense rows", radio.GetAttribute("title"));
		Assert.Null(cut.Find(".moka-segment").GetAttribute("aria-label"));
	}

	[Fact]
	public void WithoutCount_NoCountIsDrawn()
	{
		IRenderedComponent<MokaSegmentedControl> cut = RenderControl("a", _ => { });

		Assert.Empty(cut.FindAll(".moka-segment__count"));
	}

	// The label around the radio names it, so a count inside the label is read with the text: "On 3".
	[Fact]
	public void Count_FollowsTheText_InsideTheLabelThatNamesTheRadio()
	{
		IRenderedComponent<MokaSegmentedControl> cut = Render<MokaSegmentedControl>(p => p
			.Add(x => x.Value, "on")
			.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "on").Add(x => x.Text, "On").Add(x => x.Count, 3))
			.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "off").Add(x => x.Text, "Off").Add(x => x.Count, 12)));

		IReadOnlyList<IElement> segments = cut.FindAll("label.moka-segment");
		Assert.Equal(["moka-segment__input", "moka-segment__text", "moka-segment__count"],
			segments[0].Children.Select(c => c.ClassName));
		Assert.Equal("On3", string.Concat(segments[0].TextContent.Where(c => !char.IsWhiteSpace(c))));
		Assert.Equal("12", segments[1].QuerySelector(".moka-segment__count")!.TextContent);
		Assert.All(cut.FindAll(".moka-segment__count"), count => Assert.False(count.HasAttribute("aria-hidden")));
	}

	[Fact]
	public void ACountOfZero_IsDrawn()
	{
		IRenderedComponent<MokaSegmentedControl> cut = Render<MokaSegmentedControl>(p => p
			.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "on").Add(x => x.Text, "On").Add(x => x.Count, 0)));

		Assert.Equal("0", cut.Find(".moka-segment__count").TextContent);
	}

	// One rule for both states and both looks: no selected or hover rule sets the count's color, so it stays muted
	// beside the selected segment's text.
	[Fact]
	public void Count_IsMutedMonoWithTabularFigures()
	{
		const string css = "MokaSegmentedControl.razor.css";
		IReadOnlyDictionary<string, string> count = ScopedCss.Declarations(css, ".moka-segmented ::deep .moka-segment__count");

		Assert.Equal("var(--moka-color-on-surface-variant)", count["color"]);
		Assert.Equal("var(--moka-font-family-mono)", count["font-family"]);
		Assert.Equal("tabular-nums", count["font-variant-numeric"]);
		Assert.Equal("normal", count["letter-spacing"]);
		Assert.Equal("0", count["flex-shrink"]);
		Assert.Empty(ScopedCss.Declarations(css, ".moka-gate .moka-segmented ::deep .moka-segment__count"));
	}

	private IRenderedComponent<MokaSegmentedControl> RenderControl(string value, Action<string> changed, bool disabled = false) =>
		Render<MokaSegmentedControl>(p => p
			.Add(x => x.Value, value)
			.Add(x => x.ValueChanged, changed)
			.Add(x => x.Disabled, disabled)
			.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "a").Add(x => x.Text, "A"))
			.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "b").Add(x => x.Text, "B"))
			.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "c").Add(x => x.Text, "C")));
}
