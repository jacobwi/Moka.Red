using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Diff;

/// <summary>
///     A monospace line-diff viewer. Renders added, removed, and unchanged lines with
///     +/- markers, optional per-side line number gutters, and optional line wrapping.
///     Feed it precomputed <see cref="Lines" />, or set <see cref="OldText" /> and
///     <see cref="NewText" /> to have the diff computed internally via an LCS line diff.
/// </summary>
public partial class MokaDiffViewer : MokaVisualComponentBase
{
	private IReadOnlyList<MokaDiffLine>? _computedLines;
	private bool _hasComputed;
	private string? _lastNewText;
	private string? _lastOldText;

	/// <summary>The original text. Diffed against <see cref="NewText" /> when <see cref="Lines" /> is null.</summary>
	[Parameter]
	public string? OldText { get; set; }

	/// <summary>The revised text. Diffed against <see cref="OldText" /> when <see cref="Lines" /> is null.</summary>
	[Parameter]
	public string? NewText { get; set; }

	/// <summary>
	///     Precomputed diff lines. When set, takes precedence over
	///     <see cref="OldText" /> and <see cref="NewText" />.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaDiffLine>? Lines { get; set; }

	/// <summary>
	///     Whether to show old/new line number gutters. Numbers advance per side:
	///     removed lines advance the old side only, added lines the new side only,
	///     unchanged lines both. Defaults to false.
	/// </summary>
	[Parameter]
	public bool ShowLineNumbers { get; set; }

	/// <summary>Whether to show the +/- marker gutter. Defaults to true.</summary>
	[Parameter]
	public bool ShowMarkers { get; set; } = true;

	/// <summary>
	///     Maximum height of the line container. Accepts any CSS length.
	///     When set, the container scrolls vertically with the thin themed scrollbar.
	/// </summary>
	[Parameter]
	public string? MaxHeight { get; set; }

	/// <summary>
	///     Whether long lines wrap onto multiple rows. When false (default),
	///     lines stay on one row and the container scrolls horizontally.
	/// </summary>
	[Parameter]
	public bool Wrap { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-diff-viewer";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-diff-viewer--wrap", Wrap)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle(Style)
		.Build();

	private bool HasMaxHeight => !string.IsNullOrEmpty(MaxHeight);

	private string BodyClass => new CssBuilder("moka-diff-viewer-body")
		.AddClass("moka-diff-viewer-body--scroll", HasMaxHeight)
		.AddClass("moka-thin-scrollbar", HasMaxHeight)
		.Build();

	private string? BodyStyle => new StyleBuilder()
		.AddStyle("max-height", MaxHeight, HasMaxHeight)
		.Build();

	private IReadOnlyList<MokaDiffLine> EffectiveLines =>
		Lines ?? _computedLines ?? Array.Empty<MokaDiffLine>();

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (Lines is not null)
		{
			return;
		}

		// Recompute only when the inputs actually changed.
		if (_hasComputed
			&& string.Equals(_lastOldText, OldText, StringComparison.Ordinal)
			&& string.Equals(_lastNewText, NewText, StringComparison.Ordinal))
		{
			return;
		}

		_lastOldText = OldText;
		_lastNewText = NewText;
		_hasComputed = true;
		_computedLines = OldText is null && NewText is null
			? Array.Empty<MokaDiffLine>()
			: LineDiffEngine.Compute(OldText, NewText);
	}

	private IEnumerable<DiffRow> GetRows()
	{
		var oldNumber = 0;
		var newNumber = 0;

		foreach (var line in EffectiveLines)
		{
			string? oldLabel = null;
			string? newLabel = null;

			switch (line.Kind)
			{
				case MokaDiffLineKind.Added:
					newNumber++;
					newLabel = newNumber.ToString(CultureInfo.InvariantCulture);
					break;
				case MokaDiffLineKind.Removed:
					oldNumber++;
					oldLabel = oldNumber.ToString(CultureInfo.InvariantCulture);
					break;
				default:
					oldNumber++;
					newNumber++;
					oldLabel = oldNumber.ToString(CultureInfo.InvariantCulture);
					newLabel = newNumber.ToString(CultureInfo.InvariantCulture);
					break;
			}

			// Non-breaking space keeps empty lines from collapsing to zero height.
			var text = string.IsNullOrEmpty(line.Text) ? "\u00A0" : line.Text;

			yield return new DiffRow(line.Kind, text, oldLabel, newLabel);
		}
	}

	private static string LineClass(MokaDiffLineKind kind) => new CssBuilder("moka-diff-viewer-line")
		.AddClass("moka-diff-viewer-line--added", kind == MokaDiffLineKind.Added)
		.AddClass("moka-diff-viewer-line--removed", kind == MokaDiffLineKind.Removed)
		.Build();

	private static string MarkerFor(MokaDiffLineKind kind) => kind switch
	{
		MokaDiffLineKind.Added => "+",
		MokaDiffLineKind.Removed => "-",
		_ => "\u00A0"
	};

	private readonly record struct DiffRow(MokaDiffLineKind Kind, string Text, string? OldNumber, string? NewNumber);
}
