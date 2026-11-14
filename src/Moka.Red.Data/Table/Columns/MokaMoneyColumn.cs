using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;

namespace Moka.Red.Data.Table;

/// <summary>
///     A column of amounts of money: right aligned in tabular figures, written as currency in the current culture, in
///     the culture's own currency or the one <see cref="Currency" /> names. It sorts and filters by the amount and exports
///     the plain number in the invariant culture.
/// </summary>
/// <typeparam name="TItem">The row data type.</typeparam>
public class MokaMoneyColumn<TItem> : MokaColumn<TItem>
{
	private static readonly ConcurrentDictionary<string, string> Symbols = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>The row's amount, or null for none.</summary>
	[Parameter]
	[EditorRequired]
	public Func<TItem, decimal?>? Value { get; set; }

	/// <summary>
	///     The ISO 4217 code of the currency ("EUR", "USD"), written with its usual symbol in the current culture's
	///     layout. Null, the default, is the current culture's currency.
	/// </summary>
	[Parameter]
	public string? Currency { get; set; }

	/// <summary>How many decimals to write. Null, the default, is the currency's usual number in the current culture.</summary>
	[Parameter]
	public int? Decimals { get; set; }

	/// <inheritdoc />
	/// <remarks>Amounts default to <see cref="MokaTextAlign.Right" />, so their digits line up.</remarks>
	public override MokaTextAlign Align { get; set; } = MokaTextAlign.Right;

	/// <inheritdoc />
	protected internal override bool HasValue => Value is not null;

	/// <summary>Writes an amount into a row and returns the row, for editing. See <see cref="MokaColumn{TItem}.SetField" />.</summary>
	[Parameter]
	public Func<TItem, decimal?, TItem>? SetValue { get; set; }

	/// <inheritdoc />
	protected internal override Type EditValueType(TItem item) => typeof(decimal?);

	/// <inheritdoc />
	protected internal override MokaColumnEditor AutoEditor(TItem item) =>
		EditOptions is null && FilterOptions is null ? MokaColumnEditor.Number : MokaColumnEditor.Select;

	/// <inheritdoc />
	protected internal override bool CanWrite => SetValue is not null || base.CanWrite;

	/// <inheritdoc />
	protected internal override TItem WriteValue(TItem item, object? value) => SetValue is null
		? base.WriteValue(item, value)
		: SetValue(item, value switch { null => null, decimal d => d, _ => (decimal?)TableValues.ToDouble(value) });

	/// <inheritdoc />
	protected internal override string? KindCellClass => "moka-table-cell--figures";

	/// <inheritdoc />
	protected override object? KindSettings => (Currency, Decimals);

	/// <inheritdoc />
	protected internal override object? ValueOf(TItem item) => Value?.Invoke(item);

	/// <inheritdoc />
	protected internal override string TextOf(TItem item)
	{
		if (Value?.Invoke(item) is not { } amount)
		{
			return string.Empty;
		}

		NumberFormatInfo format = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
		if (Currency is { Length: > 0 } code)
		{
			format.CurrencySymbol = SymbolOf(code);
		}

		if (Decimals is { } decimals)
		{
			format.CurrencyDecimalDigits = Math.Clamp(decimals, 0, 28);
		}

		return amount.ToString("C", format);
	}

	/// <inheritdoc />
	protected internal override string ExportTextOf(TItem item) =>
		Value?.Invoke(item) is { } amount ? amount.ToString(CultureInfo.InvariantCulture) : string.Empty;

	// The symbol people write for a currency, from a culture of a region that uses it; the code itself when none does.
	private static string SymbolOf(string code) => Symbols.GetOrAdd(code, static wanted =>
	{
		foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
		{
			if (culture.LCID == CultureInfo.InvariantCulture.LCID || string.IsNullOrEmpty(culture.Name))
			{
				continue;
			}

			RegionInfo region = new(culture.Name);
			if (string.Equals(region.ISOCurrencySymbol, wanted, StringComparison.OrdinalIgnoreCase))
			{
				return region.CurrencySymbol;
			}
		}

		return wanted.ToUpperInvariant();
	});
}
