using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Base;

namespace Moka.Red.Forms.OtpInput;

/// <summary>
///     One-time password / verification code input with individual digit boxes.
/// </summary>
public partial class MokaOtpInput : MokaSegmentedInputBase
{
	/// <summary>Number of digit inputs. Default 6.</summary>
	[Parameter]
	public int Length { get; set; } = 6;

	/// <summary>Shows dots instead of digits when true. Default false.</summary>
	[Parameter]
	public bool Masked { get; set; }

	/// <summary>Whether to auto-focus the first input. Default true.</summary>
	[Parameter]
	public bool AutoFocus { get; set; } = true;

	/// <summary>Fires when all digits have been entered.</summary>
	[Parameter]
	public EventCallback<string> OnComplete { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-otp";

	/// <inheritdoc />
	protected override int SegmentCount => Length;

	/// <inheritdoc />
	protected override int MaxSegmentLength => 1;

	/// <inheritdoc />
	protected override string Separator => "";

	/// <inheritdoc />
	protected override string? AcceptPattern => "[0-9]";

	/// <inheritdoc />
	protected override string InputMode => "numeric";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-otp--error", HasError)
		.AddClass("moka-otp--masked", Masked)
		.AddClass(ValidationCssClass)
		.AddClass(Class)
		.Build();

	private string MessageCssClass => new CssBuilder("moka-otp-helper")
		.AddClass("moka-otp-helper--error", HasError)
		.Build();

	private string GetInputType() => Masked ? "password" : "text";

	/// <inheritdoc />
	protected override bool IsValidChar(char c) => char.IsDigit(c);

	/// <inheritdoc />
	protected override async Task OnValueUpdated(string? newValue)
	{
		string val = newValue ?? "";
		if (val.Length == Length && Segments.All(d => !string.IsNullOrEmpty(d)) && OnComplete.HasDelegate)
		{
			await OnComplete.InvokeAsync(val);
		}
	}
}
