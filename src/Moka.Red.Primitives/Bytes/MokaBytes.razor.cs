using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.JobProgress;
using Moka.Red.Feedback.Tooltip;

namespace Moka.Red.Primitives.Bytes;

/// <summary>
///     A byte count as text, such as "1.5 GB", or a rate, such as "12 MB/s". Digits are tabular, so a value that keeps
///     changing doesn't jitter. It takes the size and color of the text around it. In a string, use
///     <see cref="MokaByteFormat.Format" />.
/// </summary>
public partial class MokaBytes : MokaVisualComponentBase
{
	/// <summary>The byte count. Null shows <see cref="EmptyText" />, and a negative count keeps its sign.</summary>
	[Parameter]
	public long? Value { get; set; }

	/// <summary>Reads the value as a rate, adding "/s". Also added after what <see cref="Formatter" /> writes.</summary>
	[Parameter]
	public bool PerSecond { get; set; }

	/// <summary>
	///     How the count is split into units: <see cref="MokaByteUnits.Decimal" /> (the default) in powers of 1000 with
	///     KB, MB, GB and TB, or <see cref="MokaByteUnits.Binary" /> in powers of 1024 with KiB, MiB, GiB and TiB.
	/// </summary>
	[Parameter]
	public MokaByteUnits Units { get; set; }

	/// <summary>
	///     How many digits follow the point, always that many, which holds the width steady. Null, the default, shows one
	///     under 100 and none from there up, dropping a zero: "1.5 GB", "2 GB", "148 MB". Plain bytes never have any.
	/// </summary>
	[Parameter]
	public int? Decimals { get; set; }

	/// <summary>Writes the count, unit included. Wins over <see cref="Units" /> and <see cref="Decimals" />.</summary>
	[Parameter]
	public Func<long, string>? Formatter { get; set; }

	/// <summary>Shown when <see cref="Value" /> is null. Default "-".</summary>
	[Parameter]
	public string EmptyText { get; set; } = "-";

	/// <summary>Shows the exact count in a tooltip on hover, such as "1,536,000 bytes". Default false.</summary>
	[Parameter]
	public bool ShowTooltip { get; set; }

	/// <summary>Writes the tooltip's text, in place of "1,536,000 bytes", and it's up to it to say it's a rate.</summary>
	[Parameter]
	public Func<long, string>? ExactText { get; set; }

	/// <summary>Where the tooltip opens. Default Top.</summary>
	[Parameter]
	public MokaTooltipPosition TooltipPosition { get; set; } = MokaTooltipPosition.Top;

	/// <inheritdoc />
	protected override string RootClass => "moka-bytes";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("font-size", FontSize)
		.AddStyle("color", TextColor)
		.AddStyle(Style)
		.Build();

	// Md, the base class's default, is what no choice looks like, so it leaves the size to the text around it.
	private string? FontSize => SizeValue is not null
		? CssValues.WithPixels(SizeValue)
		: Size == MokaSize.Md ? null : MokaEnumHelpers.ToFontSize(Size);

	// Surface means the text color on a surface. The surface color itself would be invisible on one.
	private string? TextColor => Color switch
	{
		null => null,
		MokaColor.Surface => "var(--moka-color-on-surface)",
		MokaColor color => $"var(--moka-color-{ColorToKebab(color)})"
	};

	private string Shown
	{
		get
		{
			if (Value is not { } value)
			{
				return EmptyText;
			}

			if (Formatter is { } write)
			{
				return PerSecond ? $"{write(value)}/s" : write(value);
			}

			return MokaByteFormat.Format(value, Units, Decimals, PerSecond);
		}
	}

	private string? Exact
	{
		get
		{
			if (!ShowTooltip || Value is not { } value)
			{
				return null;
			}

			if (ExactText is { } write)
			{
				return write(value);
			}

			string count = value.ToString("N0", CultureInfo.CurrentCulture);
			string noun = value is 1 or -1 ? "byte" : "bytes";
			return PerSecond ? $"{count} {noun}/s" : $"{count} {noun}";
		}
	}
}
