using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Avatar;

/// <summary>
///     User avatar component displaying an image, initials, or icon fallback.
///     Background color is auto-generated from initials hash when no image is set.
/// </summary>
public partial class MokaAvatar
{
	private static readonly string[] _initialsColors =
	[
		"var(--moka-color-primary)",
		"var(--moka-color-secondary)",
		"var(--moka-color-error)",
		"var(--moka-color-warning)",
		"var(--moka-color-success)",
		"var(--moka-color-info)"
	];

	/// <summary>Image URL for the avatar.</summary>
	[Parameter]
	public string? Src { get; set; }

	/// <summary>Alt text for the avatar image.</summary>
	[Parameter]
	public string? Alt { get; set; }

	/// <summary>Fallback initials text when no image is set (e.g., "JD").</summary>
	[Parameter]
	public string? Initials { get; set; }

	/// <summary>Fallback icon when no image or initials are set.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>
	///     A string to generate a deterministic identicon from (e.g., user ID, email).
	///     Used as fallback when no Src, Initials, or Icon is set.
	///     Set to null or empty to disable identicon fallback.
	/// </summary>
	[Parameter]
	public string? IdenticonValue { get; set; }

	/// <summary>
	///     Whether to show an identicon as the final fallback. Default true.
	///     When false, shows nothing instead.
	/// </summary>
	[Parameter]
	public bool ShowIdenticon { get; set; } = true;

	/// <summary>White border for overlapping avatars.</summary>
	[Parameter]
	public bool Bordered { get; set; }

	/// <summary>Click handler for the avatar.</summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-avatar";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-avatar--{SizeToKebab(Size)}")
		.AddClass("moka-avatar--rounded", Rounded == MokaRounding.Full)
		.AddClass("moka-avatar--square", Rounded == MokaRounding.None)
		.AddClass("moka-avatar--bordered", Bordered)
		.AddClass("moka-avatar--clickable", OnClick.HasDelegate)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("width", AvatarSize)
		.AddStyle("height", AvatarSize)
		.AddStyle("background-color", GetInitialsColor(), !string.IsNullOrEmpty(Initials) && string.IsNullOrEmpty(Src))
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private string AvatarSize => SizeValue ?? Size switch
	{
		MokaSize.Xs => "24px",
		MokaSize.Sm => "32px",
		MokaSize.Md => "40px",
		MokaSize.Lg => "56px",
		_ => "40px"
	};

	private MokaSize IconSize => Size switch
	{
		MokaSize.Xs => MokaSize.Xs,
		MokaSize.Sm => MokaSize.Xs,
		MokaSize.Md => MokaSize.Sm,
		MokaSize.Lg => MokaSize.Md,
		_ => MokaSize.Sm
	};

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		Rounded ??= MokaRounding.Full; // Circular by default for avatars
	}

	private string? GetInitialsColor()
	{
		if (string.IsNullOrEmpty(Initials))
		{
			return null;
		}

		int hash = Math.Abs(Initials.GetHashCode(StringComparison.OrdinalIgnoreCase));
		return _initialsColors[hash % _initialsColors.Length];
	}

	private async Task HandleClick(MouseEventArgs args)
	{
		if (OnClick.HasDelegate)
		{
			await OnClick.InvokeAsync(args);
		}
	}
}
