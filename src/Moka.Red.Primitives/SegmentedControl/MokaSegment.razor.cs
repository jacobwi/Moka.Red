using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.SegmentedControl;

/// <summary>
///     A single segment within a <see cref="MokaSegmentedControl{TValue}" />: a label around a native radio
///     button. <c>Id</c> and unmatched attributes such as <c>aria-label</c> and <c>title</c> go on the
///     radio; <c>Class</c> and <c>Style</c> go on the label that draws the segment.
/// </summary>
/// <typeparam name="TValue">
///     The type of <see cref="Value" />, the same as the control's. Written inside the control's markup, a segment takes it
///     from the control.
/// </typeparam>
public partial class MokaSegment<TValue>
{
	/// <summary>
	///     The value identifying this segment, of the control's type. A string literal needs the C# form,
	///     <c>Value="@("week")"</c>, since Razor reads a value of a generic type as C#.
	/// </summary>
	[Parameter]
	[EditorRequired]
	public TValue Value { get; set; } = default!;

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

	/// <summary>
	///     Parent segmented control, of the same <typeparamref name="TValue" />. A segment without one throws when it
	///     renders, as Blazor's <c>InputRadio</c> does without its group.
	/// </summary>
	[CascadingParameter]
	public MokaSegmentedControl<TValue>? Parent { get; set; }

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

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// A segment infers its type from its own Value before the control's, so Value="Period.Week" in a control of
		// Period? is a MokaSegment<Period> that no control cascades to. Without this it would draw a radio that does nothing.
		if (Parent is null)
		{
			string type = TypeName(typeof(TValue));
			throw new InvalidOperationException(
				$"MokaSegment<{type}> must be inside a MokaSegmentedControl<{type}>. A segment takes its type from its own " +
				"Value first, so in a control of a nullable type, such as MokaSegmentedControl<Period?>, set the segment's " +
				"type too: <MokaSegment TValue=\"Period?\" Value=\"Period.Week\" />.");
		}
	}

	private static string TypeName(Type type) =>
		Nullable.GetUnderlyingType(type) is { } underlying ? $"{underlying.Name}?" : type.Name;

	// A radio raises change only when it becomes checked: a click on the segment or an arrow key.
	private async Task HandleChange()
	{
		if (!IsDisabled && Parent is not null)
		{
			await Parent.SelectAsync(Value);
		}
	}
}
