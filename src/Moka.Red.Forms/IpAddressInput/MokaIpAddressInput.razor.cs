using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Base;

namespace Moka.Red.Forms.IpAddressInput;

/// <summary>
///     Segmented IP address input with individual boxes per octet (IPv4) or group (IPv6).
///     Auto-tabs on separator keys, validates ranges, and supports paste distribution.
/// </summary>
public partial class MokaIpAddressInput : MokaSegmentedInputBase
{
	/// <summary>Whether to allow IPv6 addresses with colon-separated hex groups. Default false.</summary>
	[Parameter]
	public bool AllowIPv6 { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-ip";

	/// <inheritdoc />
	protected override int SegmentCount => AllowIPv6 ? 8 : 4;

	/// <inheritdoc />
	protected override int MaxSegmentLength => AllowIPv6 ? 4 : 3;

	/// <inheritdoc />
	protected override string Separator => AllowIPv6 ? ":" : ".";

	/// <inheritdoc />
	protected override string InputMode => AllowIPv6 ? "text" : "numeric";

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-ip--disabled", Disabled)
		.AddClass("moka-ip--error", HasError)
		.AddClass("moka-ip--ipv6", AllowIPv6)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override bool IsValidChar(char c) =>
		AllowIPv6 ? IsHexChar(c) : char.IsDigit(c);

	/// <inheritdoc />
	protected override string ClampSegment(int index, string value)
	{
		if (AllowIPv6 || string.IsNullOrEmpty(value))
		{
			return value ?? "";
		}

		// IPv4: clamp to 0-255
		if (int.TryParse(value, out int parsed) && parsed > 255)
		{
			return "255";
		}

		return value;
	}
}
