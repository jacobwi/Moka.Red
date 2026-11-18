using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Base;

namespace Moka.Red.Forms.OtpInput;

/// <summary>
///     A one-time password or verification code: one field over a box per digit, marked
///     <c>autocomplete="one-time-code"</c> so the browser can offer a code it received.
/// </summary>
public partial class MokaOtpInput : MokaCodeInputBase
{
	/// <summary>Whether the field takes focus when the page loads. Default true.</summary>
	[Parameter]
	public bool AutoFocus { get; set; } = true;

	/// <summary>
	///     Asks the browser for the code from a text message through the WebOTP API, where it has one (Chrome on Android,
	///     over HTTPS), and fills the field with it as if typed, after the person allows it. The message's last line
	///     names the site and the code, as in <c>@example.com #123456</c>. Typing a whole code stops the request.
	///     Default false.
	/// </summary>
	[Parameter]
	public bool FillFromSms { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-otp";

	/// <inheritdoc />
	protected override string AutocompleteToken => "one-time-code";

	/// <inheritdoc />
	protected override string DefaultName => "One-time code";

	/// <inheritdoc />
	protected override bool ReadsSmsCode => FillFromSms && !Disabled;

	private string RootCssClass => new CssBuilder(RootClass)
		.AddClass("moka-otp--error", HasError)
		.AddClass("moka-otp--masked", Masked)
		.AddClass("moka-otp--disabled", Disabled)
		.AddClass(CssClass)
		.AddClass(Class)
		.Build();

	private string MessageCssClass => new CssBuilder("moka-otp-helper")
		.AddClass("moka-otp-helper--error", HasError)
		.Build();
}
