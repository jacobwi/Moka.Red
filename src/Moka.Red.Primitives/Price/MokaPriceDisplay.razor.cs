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

	/// <summary>Number of decimal places. Default 2.</summary>
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

	private string FormattedPrice =>
		$"{CurrencySymbol}{Price.ToString($"F{DecimalPlaces}", CultureInfo.InvariantCulture)}";

	private string? FormattedOriginalPrice => OriginalPrice.HasValue
		? $"{CurrencySymbol}{OriginalPrice.Value.ToString($"F{DecimalPlaces}", CultureInfo.InvariantCulture)}"
		: null;

	private int DiscountPercent
	{
		get
		{
			if (!OriginalPrice.HasValue || OriginalPrice.Value == 0)
			{
				return 0;
			}

			return (int)Math.Round((1 - Price / OriginalPrice.Value) * 100);
		}
	}

	private bool ShowDiscountBadge => ShowDiscount && OriginalPrice.HasValue && DiscountPercent > 0;
}
