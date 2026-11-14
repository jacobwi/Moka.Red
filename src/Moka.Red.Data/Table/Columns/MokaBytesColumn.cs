using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.JobProgress;

namespace Moka.Red.Data.Table;

/// <summary>
///     A column of byte counts written in the largest unit that fits ("1.5 GB"), right aligned in tabular figures, through
///     the same formatter as <c>MokaBytes</c>. It sorts and filters by the count and exports the plain count.
/// </summary>
/// <typeparam name="TItem">The row data type.</typeparam>
public class MokaBytesColumn<TItem> : MokaColumn<TItem>
{
	/// <summary>The row's byte count, or null for none.</summary>
	[Parameter]
	[EditorRequired]
	public Func<TItem, long?>? Value { get; set; }

	/// <summary>Whether to count in powers of 1000 (KB, MB) or 1024 (KiB, MiB). Default 1000.</summary>
	[Parameter]
	public MokaByteUnits Units { get; set; }

	/// <summary>How many decimals to write. Null, the default, writes one under 100 and none from there up.</summary>
	[Parameter]
	public int? Decimals { get; set; }

	/// <summary>Writes the count as a rate, "12 MB/s". Default false.</summary>
	[Parameter]
	public bool PerSecond { get; set; }

	/// <inheritdoc />
	/// <remarks>Sizes default to <see cref="MokaTextAlign.Right" />, so their digits line up.</remarks>
	public override MokaTextAlign Align { get; set; } = MokaTextAlign.Right;

	/// <inheritdoc />
	protected internal override bool HasValue => Value is not null;

	/// <summary>Writes a byte count into a row and returns the row, for editing. See <see cref="MokaColumn{TItem}.SetField" />.</summary>
	[Parameter]
	public Func<TItem, long?, TItem>? SetValue { get; set; }

	/// <inheritdoc />
	protected internal override Type EditValueType(TItem item) => typeof(long?);

	/// <inheritdoc />
	protected internal override bool CanWrite => SetValue is not null || base.CanWrite;

	/// <inheritdoc />
	protected internal override TItem WriteValue(TItem item, object? value) => SetValue is null
		? base.WriteValue(item, value)
		: SetValue(item, value is null ? null : Convert.ToInt64(value, CultureInfo.InvariantCulture));

	/// <inheritdoc />
	protected internal override string? KindCellClass => "moka-table-cell--figures";

	/// <inheritdoc />
	protected override object? KindSettings => (Units, Decimals, PerSecond);

	/// <inheritdoc />
	protected internal override object? ValueOf(TItem item) => Value?.Invoke(item);

	/// <inheritdoc />
	protected internal override string TextOf(TItem item) =>
		Value?.Invoke(item) is { } bytes ? MokaByteFormat.Format(bytes, Units, Decimals, PerSecond) : string.Empty;

	/// <inheritdoc />
	protected internal override string ExportTextOf(TItem item) =>
		Value?.Invoke(item) is { } bytes ? bytes.ToString(CultureInfo.InvariantCulture) : string.Empty;
}
