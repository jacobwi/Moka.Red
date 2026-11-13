using Bunit;
using Moka.Red.Primitives.SegmentedControl;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// Segments that don't fit move to a second row, which reads as broken in a narrow dialog. Wrap="false" keeps one row
// and cuts the labels; wrapping stays the default, since hiding options was worse.
public class MokaSegmentedControlOneRowTests : BunitContext
{
	private const string Css = "MokaSegmentedControl.razor.css";

	private IRenderedComponent<MokaSegmentedControl> Control(bool? wrap) =>
		Render<MokaSegmentedControl>(p =>
		{
			p.Add(x => x.Value, "a");
			if (wrap is { } value)
			{
				p.Add(x => x.Wrap, value);
			}

			p.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "a").Add(x => x.Text, "Everything"));
			p.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "b").Add(x => x.Text, "Unread"));
			p.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "c").Add(x => x.Icon, Moka.Red.Icons.MokaIcons.Action.Search)
				.AddUnmatched("aria-label", "Search"));
		});

	[Fact]
	public void TheDefault_WrapsAndKeepsTodaysMarkup()
	{
		IRenderedComponent<MokaSegmentedControl> cut = Control(null);

		Assert.Equal("moka-segmented moka-segmented--md", cut.Find(".moka-segmented").ClassName);
		Assert.All(cut.FindAll(".moka-segment__input"), input => Assert.Null(input.GetAttribute("title")));
	}

	[Fact]
	public void WrapFalse_IsAClassOnTheTrack() =>
		Assert.Equal("moka-segmented moka-segmented--md moka-segmented--one-row", Control(false).Find(".moka-segmented").ClassName);

	[Fact]
	public void WrapFalse_TitlesEachLabelThatCanBeCut()
	{
		IRenderedComponent<MokaSegmentedControl> cut = Control(false);
		var inputs = cut.FindAll(".moka-segment__input");

		Assert.Equal("Everything", inputs[0].GetAttribute("title"));
		Assert.Equal("Unread", inputs[1].GetAttribute("title"));
		Assert.Null(inputs[2].GetAttribute("title"));
	}

	[Fact]
	public void AnAppsOwnTitle_Wins()
	{
		IRenderedComponent<MokaSegmentedControl> cut = Render<MokaSegmentedControl>(p => p
			.Add(x => x.Wrap, false)
			.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "a").Add(x => x.Text, "Day").AddUnmatched("title", "Today only")));

		Assert.Equal("Today only", cut.Find(".moka-segment__input").GetAttribute("title"));
	}

	[Fact]
	public void TurningWrapBackOn_DropsTheTitles()
	{
		IRenderedComponent<MokaSegmentedControl> cut = Control(false);

		cut.Render(p => p.Add(x => x.Wrap, true));

		Assert.Equal("moka-segmented moka-segmented--md", cut.Find(".moka-segmented").ClassName);
		Assert.All(cut.FindAll(".moka-segment__input"), input => Assert.Null(input.GetAttribute("title")));
	}

	[Fact]
	public void OneRow_IsAGridThatStaysInsideItsContainer()
	{
		var track = ScopedCss.Declarations(Css, ".moka-segmented--one-row");

		Assert.Equal("inline-grid", track["display"]);
		Assert.Equal("column", track["grid-auto-flow"]);
		Assert.Equal("100%", track["max-width"]);
		Assert.Equal("grid", ScopedCss.Declarations(Css, ".moka-segmented--one-row.moka-segmented--full-width")["display"]);
	}

	// minmax(0, auto) columns grow equally up to their content, so a short label isn't cut while a long one can give,
	// and auto columns stretch to fill a full-width track.
	[Fact]
	public void OneRow_ColumnsShareTheRoom_AndTheTextGivesWay()
	{
		Assert.Equal("minmax(0, auto)", ScopedCss.Declarations(Css, ".moka-segmented--one-row")["grid-auto-columns"]);
		Assert.Equal("0", ScopedCss.Declarations(Css, ".moka-segmented--one-row ::deep .moka-segment")["min-width"]);
		Assert.Equal("0", ScopedCss.Declarations(Css, ".moka-segmented--one-row ::deep .moka-segment__text")["min-width"]);
		Assert.Equal("ellipsis", ScopedCss.Declarations(Css, ".moka-segmented ::deep .moka-segment__text")["text-overflow"]);
	}
}
