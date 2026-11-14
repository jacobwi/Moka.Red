using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;

namespace Moka.Red.Data.Table;

/// <summary>
///     A column of numbers: right aligned, in tabular figures so the digits line up, written in the current culture.
///     It sorts and filters by the number and exports it in the invariant culture, so a spreadsheet reads it as one.
/// </summary>
/// <typeparam name="TItem">The row data type.</typeparam>
public class MokaNumberColumn<TItem> : MokaColumn<TItem>
{
	/// <summary>The row's number, or null for none. An <c>int</c> or <c>long</c> converts by itself; cast a <c>decimal</c>.</summary>
	[Parameter]
	[EditorRequired]
	public Func<TItem, double?>? Value { get; set; }

	/// <summary>
	///     Writes a number into a row and returns the row, for editing: <c>(p, v) =&gt; p with { Age = (int)(v ?? 0) }</c>.
	///     See <see cref="MokaColumn{TItem}.SetField" />.
	/// </summary>
	[Parameter]
	public Func<TItem, double?, TItem>? SetValue { get; set; }

	/// <inheritdoc />
	/// <remarks>Numbers default to <see cref="MokaTextAlign.Right" />, so their digits line up.</remarks>
	public override MokaTextAlign Align { get; set; } = MokaTextAlign.Right;

	/// <inheritdoc />
	protected internal override bool HasValue => Value is not null;

	/// <inheritdoc />
	protected internal override string? KindCellClass => "moka-table-cell--figures";

	/// <inheritdoc />
	protected internal override object? ValueOf(TItem item) => Value?.Invoke(item);

	/// <inheritdoc />
	protected internal override Type EditValueType(TItem item) => typeof(double?);

	/// <inheritdoc />
	protected internal override MokaColumnEditor AutoEditor(TItem item) =>
		EditOptions is null && FilterOptions is null ? MokaColumnEditor.Number : MokaColumnEditor.Select;

	/// <inheritdoc />
	protected internal override bool CanWrite => SetValue is not null || base.CanWrite;

	/// <inheritdoc />
	protected internal override TItem WriteValue(TItem item, object? value) =>
		SetValue is null ? base.WriteValue(item, value) : SetValue(item, TableValues.ToDouble(value));

	/// <inheritdoc />
	/// <remarks>Written with <see cref="MokaColumn{TItem}.Format" />, by default with group separators and up to two decimals.</remarks>
	protected internal override string TextOf(TItem item) =>
		Value?.Invoke(item) is { } number ? number.ToString(Format ?? "#,0.##", CultureInfo.CurrentCulture) : string.Empty;

	/// <inheritdoc />
	protected internal override string ExportTextOf(TItem item) =>
		Value?.Invoke(item) is { } number ? number.ToString(CultureInfo.InvariantCulture) : string.Empty;
}
