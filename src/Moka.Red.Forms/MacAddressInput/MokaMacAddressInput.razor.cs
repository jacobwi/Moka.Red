using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Base;

namespace Moka.Red.Forms.MacAddressInput;

/// <summary>
///     MAC address input with 6 individual hex pair boxes (e.g., "AA:BB:CC:DD:EE:FF").
///     Supports colon or hyphen separators, auto-tabs on 2 characters, and validates hex input.
/// </summary>
public partial class MokaMacAddressInput : MokaSegmentedInputBase
{
	private string _separatorChar = ":";

	/// <summary>
	///     The separator character between MAC octets. Default is ":" (e.g., "AA:BB:CC:DD:EE:FF").
	///     Use "-" for Windows-style formatting (e.g., "AA-BB-CC-DD-EE-FF").
	/// </summary>
	[Parameter]
	public string MacSeparator { get; set; } = ":";

	/// <summary>
	///     Whether to automatically uppercase hex characters as they are typed. Default true.
	/// </summary>
	[Parameter]
	public bool Uppercase { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-mac";

	/// <inheritdoc />
	protected override int SegmentCount => 6;

	/// <inheritdoc />
	protected override int MaxSegmentLength => 2;

	/// <inheritdoc />
	protected override string Separator => _separatorChar;

	/// <inheritdoc />
	protected override string InputMode => "text";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-mac--{SizeToKebab(Size)}")
		.AddClass("moka-mac--disabled", Disabled)
		.AddClass("moka-mac--error", HasError)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		// Sync separator before base processes segments
		_separatorChar = MacSeparator is ":" or "-" ? MacSeparator : ":";
		base.OnParametersSet();
	}

	/// <inheritdoc />
	protected override bool IsValidChar(char c) => IsHexChar(c);

	/// <inheritdoc />
	protected override string ClampSegment(int index, string value)
	{
		ArgumentNullException.ThrowIfNull(value);
		return string.IsNullOrEmpty(value) ? value : Uppercase ? value.ToUpperInvariant() : value;
	}

	/// <inheritdoc />
	protected override string? CombineSegments()
	{
		bool allEmpty = Array.TrueForAll(Segments, string.IsNullOrEmpty);
		if (allEmpty)
		{
			return null;
		}

		string[] segments = Uppercase
			? Segments.Select(s => s.ToUpperInvariant()).ToArray()
			: Segments;

		return string.Join(Separator, segments);
	}
}
