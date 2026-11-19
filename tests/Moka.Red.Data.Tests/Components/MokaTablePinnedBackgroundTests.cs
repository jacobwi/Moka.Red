using System.Text.RegularExpressions;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Components;

// A pinned cell painted the surface with the row's tint over it and nothing under them, so where an app's surface let the
// page through, the cells scrolling sideways showed under the pinned column. The surface and the tint now lie on an
// opaque base, --moka-table-pin-background, which an opaque surface covers, so such a table looks as it did.
public sealed partial class MokaTablePinnedBackgroundTests
{
	private const string Css = "MokaTable.razor.css";
	private const string Base = "var(--moka-table-pin-background, var(--moka-color-background))";
	private const string Tint = "linear-gradient(var(--_row-tint, transparent), var(--_row-tint, transparent))";
	private const string Surface = "linear-gradient(var(--_pin-surface), var(--_pin-surface))";

	[Theory]
	[InlineData(".moka-table > * > tr > .moka-table-cell--pin-start")]
	[InlineData(".moka-table > * > tr > .moka-table-cell--pin-end")]
	public void APinnedCell_LaysTheTintAndTheSurfaceOnAnOpaqueBase(string selector)
	{
		IReadOnlyDictionary<string, string> cell = ScopedCss.Declarations(Css, selector);

		Assert.Equal(Base, cell["background-color"]);
		Assert.Equal($"{Tint}, {Surface}", OneLine(cell["background-image"]));
		Assert.Equal("var(--moka-color-surface)", cell["--_pin-surface"]);
	}

	// The headings, on both designs, change only the surface, so the base stays under them.
	[Theory]
	[InlineData(".moka-table > thead > tr > th.moka-table-cell--pin-start", "var(--moka-color-surface-variant)")]
	[InlineData(".moka-table > thead > tr > th.moka-table-cell--pin-end", "var(--moka-color-surface-variant)")]
	[InlineData(".moka-gate .moka-table > thead > tr > th.moka-table-cell--pin-start", "var(--moka-color-surface)")]
	[InlineData(".moka-gate .moka-table > thead > tr > th.moka-table-cell--pin-end", "var(--moka-color-surface)")]
	public void APinnedHeading_ChangesOnlyTheSurface(string selector, string surface)
	{
		IReadOnlyDictionary<string, string> heading = ScopedCss.Declarations(Css, selector);

		Assert.Equal(surface, heading["--_pin-surface"]);
		Assert.DoesNotContain("background-color", heading.Keys);
		Assert.DoesNotContain("background", heading.Keys);
	}

	// Striped, hovered and selected rows tint the cells through --_row-tint, which a pinned cell draws over its surface.
	[Theory]
	[InlineData(".moka-table--striped > tbody > tr:nth-child(even)")]
	[InlineData(".moka-table--hoverable > tbody > tr:hover")]
	[InlineData(".moka-table > tbody > tr.moka-table-row--selected")]
	[InlineData(".moka-gate .moka-table--hoverable > tbody > tr:hover")]
	[InlineData(".moka-gate .moka-table > tbody > tr.moka-table-row--selected")]
	public void ARowsState_ReachesAPinnedCellAsATintOnly(string selector)
	{
		IReadOnlyDictionary<string, string> row = ScopedCss.Declarations(Css, selector);

		Assert.Equal(["--_row-tint"], row.Keys);
	}

	// A row held at the top or the bottom has its own surface on the new design; in a pinned column that is the surface
	// the base carries, not a background that would take the base's place.
	[Fact]
	public void APinnedRowsCell_InAPinnedColumn_KeepsTheBase()
	{
		IReadOnlyDictionary<string, string> pinned = ScopedCss.Declarations(Css,
			".moka-gate .moka-table-row--pinned > :is(.moka-table-cell--pin-start, .moka-table-cell--pin-end)");
		IReadOnlyDictionary<string, string> others = ScopedCss.Declarations(Css,
			".moka-gate .moka-table-row--pinned > td:not(.moka-table-cell--pin-start, .moka-table-cell--pin-end)");

		Assert.Equal(["--_pin-surface"], pinned.Keys);
		Assert.Equal("var(--moka-color-surface-2)", pinned["--_pin-surface"]);
		Assert.Equal("var(--moka-color-surface-2)", others["background-color"]);
		Assert.Empty(ScopedCss.Declarations(Css, ".moka-gate .moka-table-row--pinned > td"));
	}

	private static string OneLine(string value) => Whitespace().Replace(value, " ").Trim();

	[GeneratedRegex(@"\s+")]
	private static partial Regex Whitespace();
}
