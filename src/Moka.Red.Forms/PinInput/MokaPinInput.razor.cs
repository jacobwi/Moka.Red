using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Base;

namespace Moka.Red.Forms.PinInput;

/// <summary>
///     A numeric PIN, such as a card PIN or a backup code: one field over a box per digit, masked by default and marked
///     <c>autocomplete="off"</c> so the browser doesn't keep it. For a code the person received, use <c>MokaOtpInput</c>.
/// </summary>
public partial class MokaPinInput : MokaCodeInputBase
{
	/// <summary>The number of digits, one box each. Default 4. Below 1 counts as 1.</summary>
	[Parameter]
	public override int Length { get; set; } = 4;

	/// <summary>Shows a dot for each digit and makes the field a password field. Default true.</summary>
	[Parameter]
	public override bool Masked { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-pin";

	/// <inheritdoc />
	protected override string AutocompleteToken => "off";

	/// <inheritdoc />
	protected override string DefaultName => "PIN";

	private string RootCssClass => new CssBuilder(RootClass)
		.AddClass($"moka-pin--{SizeToKebab(Size)}")
		.AddClass("moka-pin--masked", Masked)
		.AddClass("moka-pin--disabled", Disabled)
		.AddClass("moka-pin--error", HasError)
		.AddClass(CssClass)
		.AddClass(Class)
		.Build();

	private string MessageCssClass => new CssBuilder("moka-pin-helper")
		.AddClass("moka-pin-helper--error", HasError)
		.Build();
}
