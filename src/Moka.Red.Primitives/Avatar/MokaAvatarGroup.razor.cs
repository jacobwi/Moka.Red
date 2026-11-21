using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Avatar;

/// <summary>
///     Overlapping avatar stack showing multiple users with an optional "+N" overflow indicator. The group's
///     <c>Size</c> and <c>SizeValue</c>, when set, size every avatar in it that sets neither of its own, and the "+N"
///     avatar with them.
/// </summary>
public partial class MokaAvatarGroup
{
	private readonly MokaChildList<MokaAvatar> _avatars;
	private MokaAvatarGroupSizing _sizing = MokaAvatarGroupSizing.None;
	private bool _sizeGiven;

	/// <summary>Creates the group.</summary>
	public MokaAvatarGroup()
	{
		_avatars = new MokaChildList<MokaAvatar>(StateHasChanged);
	}

	/// <summary>Child content containing <see cref="MokaAvatar" /> elements.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     How many avatars show, in the order they stand. The rest are left out and a "+N" avatar counts them. Default 5;
	///     0 or less shows only the count.
	/// </summary>
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
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("--moka-avatar-group-spacing", Spacing)
		.AddStyle(Style)
		.Build();

	private int Shown => Math.Max(Max, 0);

	private int OverflowCount => Math.Max(_avatars.Count - Shown, 0);

	/// <summary>Has internal child counting state.</summary>
	protected override bool ShouldRender() => true;

	/// <summary>Notes whether the page set <c>Size</c>, which has a default and so can't say so itself.</summary>
	/// <param name="parameters">The parameters the page passed.</param>
	/// <returns>The task of the base class.</returns>
	public override Task SetParametersAsync(ParameterView parameters)
	{
		_sizeGiven = parameters.TryGetValue<MokaSize>(nameof(Size), out _);
		return base.SetParametersAsync(parameters);
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		MokaSize? size = _sizeGiven ? Size : null;
		if (_sizing.Size != size || _sizing.SizeValue != SizeValue)
		{
			_sizing = size is null && SizeValue is null ? MokaAvatarGroupSizing.None : new MokaAvatarGroupSizing(size, SizeValue);
		}
	}

	internal void Join(MokaAvatar avatar) => _avatars.Add(avatar);

	internal void Saw(MokaAvatar avatar) => _avatars.Saw(avatar);

	internal void Leave(MokaAvatar avatar) => _avatars.Remove(avatar);

	internal bool Hides(MokaAvatar avatar) => _avatars.IndexOf(avatar) >= Shown;
}
