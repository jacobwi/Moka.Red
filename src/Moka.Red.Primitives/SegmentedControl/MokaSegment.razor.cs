using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.SegmentedControl;

/// <summary>
///     A single segment within a <see cref="MokaSegmentedControl" />.
/// </summary>
public partial class MokaSegment
{
	/// <summary>The value identifying this segment. Required.</summary>
	[Parameter]
	public string Value { get; set; } = string.Empty;

	/// <summary>Display text for the segment.</summary>
	[Parameter]
	public string? Text { get; set; }

	/// <summary>Optional icon for the segment.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>Whether this segment is disabled.</summary>
	[Parameter]
	public bool Disabled { get; set; }

	/// <summary>Parent segmented control.</summary>
	[CascadingParameter]
	public MokaSegmentedControl? Parent { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-segment";

	private bool _isActive => Parent?.IsSelected(Value) == true;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-segment--active", _isActive)
		.AddClass("moka-segment--disabled", Disabled)
		.AddClass(Class)
		.Build();

	/// <summary>Active state depends on parent — always re-render.</summary>
	protected override bool ShouldRender() => true;

	private async Task HandleClick()
	{
		if (!Disabled && Parent is not null)
		{
			await Parent.SelectAsync(Value);
		}
	}
}
