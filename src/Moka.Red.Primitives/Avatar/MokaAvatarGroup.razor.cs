using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Avatar;

/// <summary>
///     Overlapping avatar stack showing multiple users with an optional "+N" overflow indicator.
/// </summary>
public partial class MokaAvatarGroup
{
	private int _childCount;
	private int _overflowCount;

	/// <summary>Child content containing <see cref="MokaAvatar" /> elements.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Maximum number of avatars to display before showing "+N". Default 5.</summary>
	[Parameter]
	public int Max { get; set; } = 5;

	/// <summary>Overlap amount as a CSS margin value (negative). Default "-8px".</summary>
	[Parameter]
	public string Spacing { get; set; } = "-8px";

	/// <inheritdoc />
	protected override string RootClass => "moka-avatar-group";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-avatar-group--{SizeToKebab(Size)}")
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("--moka-avatar-group-spacing", Spacing)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private string OverflowStyle => new StyleBuilder()
		.AddStyle("width", AvatarSize)
		.AddStyle("height", AvatarSize)
		.Build() ?? string.Empty;

	private string AvatarSize => SizeValue ?? Size switch
	{
		MokaSize.Xs => "24px",
		MokaSize.Sm => "32px",
		MokaSize.Md => "40px",
		MokaSize.Lg => "56px",
		_ => "40px"
	};

	/// <summary>Has internal child counting state.</summary>
	protected override bool ShouldRender() => true;

	/// <summary>Registers a child avatar and returns its index. Returns -1 if it should be hidden.</summary>
	internal int RegisterChild()
	{
		int index = _childCount++;
		if (index >= Max)
		{
			_overflowCount = _childCount - Max;
		}

		return index;
	}

	/// <summary>Resets child tracking before each render.</summary>
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		_childCount = 0;
		_overflowCount = 0;
	}
}
