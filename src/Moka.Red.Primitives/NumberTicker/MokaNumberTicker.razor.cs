using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.NumberTicker;

/// <summary>
///     An animated number counter that rolls/slides digits when the value changes,
///     similar to an odometer or slot machine display. Each digit position animates independently.
/// </summary>
public partial class MokaNumberTicker : MokaVisualComponentBase
{
	private string _currentFormatted = "";

	/// <summary>The numeric value to display.</summary>
	[Parameter]
	public double Value { get; set; }

	/// <summary>
	///     .NET number format string. Defaults to "N0" (integer with thousands separators).
	/// </summary>
	[Parameter]
	public string Format { get; set; } = "N0";

	/// <summary>Animation duration in milliseconds. Defaults to 1000.</summary>
	[Parameter]
	public int Duration { get; set; } = 1000;

	/// <inheritdoc />
	protected override string RootClass => "moka-number-ticker";

	private MokaColor ResolvedColor => Color ?? MokaColor.Primary;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-number-ticker--{SizeToKebab(Size)}")
		.AddClass($"moka-number-ticker--{ColorToKebab(ResolvedColor)}")
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("--moka-ticker-duration", $"{Duration}ms")
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		_currentFormatted = Value.ToString(Format, CultureInfo.CurrentCulture);
	}

	/// <summary>Determines if a character is a digit that should animate.</summary>
	private static bool IsAnimatableDigit(char c) => char.IsDigit(c);

	/// <summary>Gets the vertical offset for a digit column (0-9 maps to 0%-90%).</summary>
	private static string GetDigitOffset(char c)
	{
		if (!char.IsDigit(c))
		{
			return "0%";
		}

		int digit = c - '0';
		return $"{digit * -10}%";
	}
}
