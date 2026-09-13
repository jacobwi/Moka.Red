using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Icons;

namespace Moka.Red.Forms.Rating;

/// <summary>
///     A star rating input component. Renders stars via MokaIcon.
///     Supports hover preview, custom icons, and clearing.
/// </summary>
public partial class MokaRating : MokaVisualComponentBase
{
	private int _hoverValue;

	/// <summary>Current rating value (0 to <see cref="MaxValue" />). Two-way bindable.</summary>
	[Parameter]
	public int Value { get; set; }

	/// <summary>Callback invoked when <see cref="Value" /> changes.</summary>
	[Parameter]
	public EventCallback<int> ValueChanged { get; set; }

	/// <summary>Number of stars. Default 5.</summary>
	[Parameter]
	public int MaxValue { get; set; } = 5;

	/// <summary>Label text displayed above the rating.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Whether the rating is read-only. Default false.</summary>
	[Parameter]
	public bool ReadOnly { get; set; }

	/// <summary>Whether clicking the current value clears the rating to 0. Default true.</summary>
	[Parameter]
	public bool AllowClear { get; set; } = true;

	/// <summary>Custom outline icon definition.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>Custom filled icon definition.</summary>
	[Parameter]
	public MokaIconDefinition? FilledIcon { get; set; }

	/// <summary>Whether to highlight stars on hover. Default true.</summary>
	[Parameter]
	public bool HoverPreview { get; set; } = true;

	/// <summary>Whether to show the numeric value next to the stars. Default false.</summary>
	[Parameter]
	public bool ShowValue { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-rating";

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-rating--readonly", ReadOnly)
		.AddClass("moka-rating--disabled", Disabled)
		.AddClass($"moka-rating--{SizeToKebab(Size)}")
		.AddClass(Class)
		.Build();

	private bool IsInteractive => !ReadOnly && !Disabled;

	/// <summary>The effective display value (hover value or actual value).</summary>
	private int DisplayValue => _hoverValue > 0 ? _hoverValue : Value;

	/// <summary><see cref="FilledIcon" /> when set, otherwise the built-in solid star.</summary>
	private MokaIconDefinition ResolvedFilledIcon => FilledIcon ?? MokaIcons.Toggle.Star;

	/// <summary><see cref="Icon" /> when set, otherwise the built-in outline star.</summary>
	private MokaIconDefinition ResolvedOutlineIcon => Icon ?? MokaIcons.Toggle.StarOutline;

	/// <summary>Rating has hover state that changes independently of parameters.</summary>
	protected override bool ShouldRender() => true;

	private async Task HandleClick(int star)
	{
		if (!IsInteractive)
		{
			return;
		}

		if (AllowClear && star == Value)
		{
			Value = 0;
		}
		else
		{
			Value = star;
		}

		await ValueChanged.InvokeAsync(Value);
	}

	private void HandleMouseEnter(int star)
	{
		if (IsInteractive && HoverPreview)
		{
			_hoverValue = star;
		}
	}

	private void HandleMouseLeave() => _hoverValue = 0;
}
