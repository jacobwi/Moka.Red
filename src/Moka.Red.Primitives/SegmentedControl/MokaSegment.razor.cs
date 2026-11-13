using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.SegmentedControl;

/// <summary>
///     A single segment within a <see cref="MokaSegmentedControl" />: a label around a native radio
///     button. <c>Id</c> and unmatched attributes such as <c>aria-label</c> and <c>title</c> go on the
///     radio; <c>Class</c> and <c>Style</c> go on the label that draws the segment.
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

	/// <summary>
	///     A number after the text, as in "On 3", drawn muted in the mono font with tabular figures. It sits inside the
	///     segment's label, so it is part of the radio's accessible name unless an <c>aria-label</c> replaces that.
	/// </summary>
	[Parameter]
	public int? Count { get; set; }

	/// <summary>Whether this segment is disabled. A disabled control disables every segment.</summary>
	[Parameter]
	public bool Disabled { get; set; }

	/// <summary>Parent segmented control.</summary>
	[CascadingParameter]
	public MokaSegmentedControl? Parent { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-segment";

	private bool IsActive => Parent?.IsSelected(Value) == true;

	private bool IsChecked => Parent?.IsChecked(Value) == true;

	private bool IsDisabled => Disabled || Parent?.Disabled == true;

	private string? CutTextTitle => Parent is { Wrap: false } && !string.IsNullOrEmpty(Text) ? Text : null;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-segment--active", IsActive)
		.AddClass("moka-segment--disabled", IsDisabled)
		.AddClass(Class)
		.Build();

	/// <summary>Active state depends on parent, so always re-render.</summary>
	protected override bool ShouldRender() => true;

	// A radio raises change only when it becomes checked: a click on the segment or an arrow key.
	private async Task HandleChange()
	{
		if (!IsDisabled && Parent is not null)
		{
			await Parent.SelectAsync(Value);
		}
	}
}
