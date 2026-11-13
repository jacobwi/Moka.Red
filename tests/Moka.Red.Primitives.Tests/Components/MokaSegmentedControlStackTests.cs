using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Primitives.SegmentedControl;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// On a phone, three long labels on one row end in ellipses, and the full text sat only in a title a touch screen never
// shows. StackBelow puts each segment on a row of its own under a width the app picks, so every label shows whole.
public class MokaSegmentedControlStackTests : BunitContext
{
	private const string Css = "MokaSegmentedControl.razor.css";

	private IRenderedComponent<MokaSegmentedControl> Control(Action<ComponentParameterCollectionBuilder<MokaSegmentedControl>> extra) =>
		Render<MokaSegmentedControl>(p =>
		{
			p.Add(x => x.Value, "add");
			extra(p);
			p.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "add").Add(x => x.Text, "Add this device"));
			p.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "invite").Add(x => x.Text, "I have an invite"));
			p.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "restore").Add(x => x.Text, "Restore a backup"));
		});

	[Fact]
	public void AFullWidthControl_CarriesTheWidthAndTheClass()
	{
		var track = Control(p => p.Add(x => x.FullWidth, true).Add(x => x.StackBelow, "30rem")).Find(".moka-segmented");

		Assert.Contains("moka-segmented--stack", track.ClassList);
		Assert.Equal("--_stack-below: 30rem", track.GetAttribute("style"));
	}

	[Fact]
	public void ItTakesALengthExpression()
	{
		var track = Control(p => p.Add(x => x.FullWidth, true).Add(x => x.StackBelow, "min(30rem, 80vw)")).Find(".moka-segmented");

		Assert.Equal("--_stack-below: min(30rem, 80vw)", track.GetAttribute("style"));
	}

	[Fact]
	public void TheAppsOwnStyle_ComesAfterIt()
	{
		var track = Control(p => p.Add(x => x.FullWidth, true).Add(x => x.StackBelow, "30rem").Add(x => x.Style, "max-width: 40rem"))
			.Find(".moka-segmented");

		Assert.Equal("--_stack-below: 30rem; max-width: 40rem", track.GetAttribute("style"));
	}

	// A control as wide as its segments has no width of its own to compare, so nothing changes.
	[Fact]
	public void WithoutFullWidth_NothingChanges()
	{
		var track = Control(p => p.Add(x => x.StackBelow, "30rem")).Find(".moka-segmented");

		Assert.Equal("moka-segmented moka-segmented--md", track.ClassName);
		Assert.Null(track.GetAttribute("style"));
	}

	[Theory]
	[InlineData("red")]
	[InlineData("30rem; color: red")]
	[InlineData("")]
	public void AValueThatIsntALength_IsIgnored(string value)
	{
		var track = Control(p => p.Add(x => x.FullWidth, true).Add(x => x.StackBelow, value)).Find(".moka-segmented");

		Assert.DoesNotContain("moka-segmented--stack", track.ClassList);
		Assert.Null(track.GetAttribute("style"));
	}

	[Fact]
	public void TheDefault_KeepsTodaysMarkup()
	{
		var track = Control(p => p.Add(x => x.FullWidth, true)).Find(".moka-segmented");

		Assert.Equal("moka-segmented moka-segmented--md moka-segmented--full-width moka-fill-width", track.ClassName);
		Assert.Null(track.GetAttribute("style"));
	}

	[Fact]
	public void WithOneRow_BothClassesStay()
	{
		var track = Control(p => p.Add(x => x.FullWidth, true).Add(x => x.Wrap, false).Add(x => x.StackBelow, "30rem")).Find(".moka-segmented");

		Assert.Contains("moka-segmented--one-row", track.ClassList);
		Assert.Contains("moka-segmented--stack", track.ClassList);
	}

	// A wrapping track is flex: each segment's basis is a whole row under the width and clamps to zero over it.
	[Fact]
	public void AWrappingTrack_StacksThroughTheSegmentsBasis() =>
		Assert.Equal("calc((var(--_stack-below) - 100%) * 999)",
			ScopedCss.Declarations(Css, ".moka-segmented--stack ::deep .moka-segment")["flex-basis"]);

	// A one-row track is a grid: under the width one column holds the whole track, over it columns clamp to zero and
	// share the row as they did.
	[Fact]
	public void AOneRowTrack_StacksThroughItsColumns()
	{
		var track = ScopedCss.Declarations(Css, ".moka-segmented--one-row.moka-segmented--stack");

		Assert.Equal("row", track["grid-auto-flow"]);
		Assert.Equal("repeat(auto-fit, minmax(min(100%, calc((var(--_stack-below) - 100%) * 999)), auto))", track["grid-template-columns"]);
	}
}
