using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Tooltip;

namespace Moka.Red.Primitives.Utility;

/// <summary>
///     A value with its copy button, for addresses, keys and IDs. In a table row or an element with the
///     <c>moka-quiet-copies</c> class the button stays faint until the row is hovered.
/// </summary>
public partial class MokaCopyable : MokaVisualComponentBase
{
	private Func<string>? _readText;

	/// <summary>What gets copied, and what shows unless <see cref="ChildContent" /> is given.</summary>
	[Parameter]
	[EditorRequired]
	public string Text { get; set; } = string.Empty;

	/// <summary>What shows instead of <see cref="Text" />, for a value shown shorter than what's copied.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     What it copies in words, for the button's name and tooltip: "VPN key". Defaults to <see cref="Text" />,
	///     so a list of them isn't a row of buttons all called "Copy".
	/// </summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Draws the value in the mono font. Default true, since this is mostly addresses and keys.</summary>
	[Parameter]
	public bool Mono { get; set; } = true;

	/// <summary>A long value breaks onto the next line instead of being cut short.</summary>
	[Parameter]
	public bool Wrap { get; set; }

	/// <summary>The button's name and tooltip. Defaults to "Copy " and <see cref="Label" />.</summary>
	[Parameter]
	public string? CopyLabel { get; set; }

	/// <summary>Shown above the button after a copy. Default "Copied".</summary>
	[Parameter]
	public string CopiedText { get; set; } = "Copied";

	/// <summary>Shown above the button when the browser refused the copy. Default "Couldn't copy".</summary>
	[Parameter]
	public string FailedText { get; set; } = MokaCopyButton.DefaultFailedText;

	/// <summary>
	///     Keeps <see cref="Text" /> out of the page: the button asks for it when clicked instead of carrying it, and
	///     isn't named after it. For a key or token shown masked through <see cref="ChildContent" />. Default false,
	///     since a copy that waits for its text can be refused on Blazor Server over plain http in Safari and Firefox.
	/// </summary>
	[Parameter]
	public bool Secret { get; set; }

	/// <summary>Where the button's tooltip opens.</summary>
	[Parameter]
	public MokaTooltipPosition TooltipPosition { get; set; } = MokaTooltipPosition.Bottom;

	private string? Named => Label ?? (Secret ? null : Text);

	private string CopyName =>
		CopyLabel ?? (Named is null ? MokaCopyButton.DefaultTooltipText : $"{MokaCopyButton.DefaultTooltipText} {Named}");

	private Func<string>? SecretSource => Secret ? _readText ??= () => Text : null;

	/// <inheritdoc />
	protected override string RootClass => "moka-copyable";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-copyable--mono", Mono)
		.AddClass("moka-copyable--wrap", Wrap)
		.AddClass(Class)
		.Build();
}
