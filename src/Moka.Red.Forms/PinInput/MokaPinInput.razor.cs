using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Base;

namespace Moka.Red.Forms.PinInput;

/// <summary>
///     A numeric PIN entry component with individual masked digit boxes.
///     Designed for ATM PINs, backup codes, and similar numeric secrets.
///     Unlike <c>MokaOtpInput</c>, this is specifically for numeric PINs with masked display by default.
/// </summary>
public partial class MokaPinInput : MokaSegmentedInputBase
{
	/// <summary>Number of PIN digit inputs. Defaults to 4.</summary>
	[Parameter]
	public int Length { get; set; } = 4;

	/// <summary>
	///     Whether to mask digits with dots instead of showing the actual numbers.
	///     Defaults to true for security.
	/// </summary>
	[Parameter]
	public bool Masked { get; set; } = true;

	/// <summary>Fires when all digits have been entered.</summary>
	[Parameter]
	public EventCallback<string> OnComplete { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-pin";

	/// <inheritdoc />
	protected override int SegmentCount => Length;

	/// <inheritdoc />
	protected override int MaxSegmentLength => 1;

	/// <inheritdoc />
	protected override string Separator => "";

	/// <inheritdoc />
	protected override string InputMode => "numeric";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-pin--{SizeToKebab(Size)}")
		.AddClass("moka-pin--masked", Masked)
		.AddClass("moka-pin--disabled", Disabled)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private string InputType => Masked ? "password" : "text";

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
