using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Price;

/// <summary>
///     Display component for prices with original/sale price support and discount badge.
/// </summary>
public partial class MokaPriceDisplay : MokaVisualComponentBase
{
	/// <summary>Current or sale price.</summary>
	[Parameter]
	[EditorRequired]
	public decimal Price { get; set; }

	/// <summary>Crossed-out original price (for sales). Null if no sale.</summary>
	[Parameter]
	public decimal? OriginalPrice { get; set; }

	/// <summary>Currency symbol displayed before the price. Default "$".</summary>
	[Parameter]
	public string CurrencySymbol { get; set; } = "$";

	/// <summary>ISO currency code shown after the price.</summary>
	[Parameter]
	public string? CurrencyCode { get; set; }

	/// <summary>Number of decimal places, from 0 to 28. Values outside that range are clamped. Default 2.</summary>
	[Parameter]
	public int DecimalPlaces { get; set; } = 2;

	/// <summary>Whether to show a discount percentage badge when OriginalPrice is set. Default true.</summary>
	[Parameter]
	public bool ShowDiscount { get; set; } = true;

	/// <summary>Emphasizes the price (larger, primary color). Default false.</summary>
	[Parameter]
	public bool Highlight { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-price";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-price--highlight", Highlight)
		.AddClass("moka-price--sale", OriginalPrice.HasValue)
		.AddClass(Class)
		.Build();

	// A negative count made the format string "F-1", which .NET prints as literal text. 28 is the
	// largest scale a decimal has.
	private string AmountFormat =>
		string.Create(CultureInfo.InvariantCulture, $"F{Math.Clamp(DecimalPlaces, 0, 28)}");

	private string FormattedPrice =>
		$"{CurrencySymbol}{Price.ToString(AmountFormat, CultureInfo.InvariantCulture)}";

	private string? FormattedOriginalPrice => OriginalPrice.HasValue
		? $"{CurrencySymbol}{OriginalPrice.Value.ToString(AmountFormat, CultureInfo.InvariantCulture)}"
		: null;

	private int DiscountPercent
	{
		get
		{
			// Only a real discount gets a percent, which also keeps the division and the cast in range.
			if (OriginalPrice is not { } original || original <= 0 || Price < 0 || Price >= original)
			{
				return 0;
			}

			// A shop rounds a half percent up: 12.5% off reads as 13%, not the banker's 12%.
			return (int)Math.Round((1 - Price / original) * 100, MidpointRounding.AwayFromZero);
		}
	}

	private bool ShowDiscountBadge => ShowDiscount && OriginalPrice.HasValue && DiscountPercent > 0;
}
