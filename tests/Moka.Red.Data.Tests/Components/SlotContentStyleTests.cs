using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Data.Chart;
using Moka.Red.Data.Tests.Charts;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Components;

/// <summary>
///     Rules meant for what an app passes in. The app's markup carries the app's scope attribute, so a scoped rule never
///     matched it: a link in a donut's center couldn't be clicked, and a table's FooterContent cells had no padding.
/// </summary>
public sealed class SlotContentStyleTests : BunitContext
{
	private static readonly MokaDonutSegment[] Segments =
	[
		new() { Name = "Web", Value = 60 },
		new() { Name = "Mail", Value = 40 }
	];

	[Fact]
	public void WhatTheDonutsCenterHolds_TakesThePointerAgain()
	{
		Assert.Equal("none", ScopedCss.Declarations("MokaDonutChart.razor.css", ".moka-donutchart-center")["pointer-events"]);
		Assert.Equal("auto", ScopedCss.Declarations("MokaDonutChart.razor.css", ".moka-donutchart-center ::deep > *")["pointer-events"]);
		Assert.Empty(ScopedCss.Declarations("MokaDonutChart.razor.css", ".moka-donutchart-center > *"));
	}

	[Fact]
	public void TheDonutsCenter_HoldsTheAppsLinkDirectly()
	{
		this.Static();
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Segments, Segments)
			.AddChildContent("<a href=\"/traffic\">1.2 TB</a>"));

		IElement link = Assert.Single(cut.Find(".moka-donutchart-center").Children);
		Assert.Equal("A", link.TagName);
	}

	// A fragment that reads only its parent's fields is an equal delegate every render, and the chart took that to mean
	// nothing changed, so the center kept its first text.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void TheDonutsCenter_DrawsAgainWhenTheParentDoes(bool modern)
	{
		this.Static();
		Center center = new();
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Segments, Segments)
			.Add(x => x.Title, modern ? "Traffic" : null)
			.Add(x => x.ChildContent, (RenderFragment)center.Draw));
		Assert.Equal("Clicked 0", cut.Find(".moka-donutchart-center").TextContent.Trim());

		center.Clicks = 1;
		cut.Render(p => p
			.Add(x => x.Segments, Segments)
			.Add(x => x.Title, modern ? "Traffic" : null)
			.Add(x => x.ChildContent, (RenderFragment)center.Draw));

		Assert.Equal("Clicked 1", cut.Find(".moka-donutchart-center").TextContent.Trim());
	}

	[Fact]
	public void AnUnchangedDonutWithoutCenter_StillSkipsTheRender()
	{
		this.Static();
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p.Add(x => x.Segments, Segments));
		int renders = cut.RenderCount;

		cut.Render(p => p.Add(x => x.Segments, Segments));

		Assert.Equal(renders, cut.RenderCount);
	}

	[Fact]
	public void TheTablesFooterCells_AreStyledWhoeverWritesThem()
	{
		Assert.Equal("var(--moka-spacing-sm) var(--moka-spacing-md)",
			ScopedCss.Declarations("MokaTable.razor.css", ".moka-table-footer ::deep td")["padding"]);
		Assert.Equal("var(--moka-cell-padding)",
			ScopedCss.Declarations("MokaTable.razor.css", ".moka-gate .moka-table-footer ::deep td")["padding"]);
		Assert.Empty(ScopedCss.Declarations("MokaTable.razor.css", ".moka-table-footer td"));
	}

	private sealed class Center
	{
		public int Clicks { get; set; }

		public void Draw(RenderTreeBuilder builder) =>
			builder.AddContent(0, $"Clicked {Clicks.ToString(CultureInfo.InvariantCulture)}");
	}
}
