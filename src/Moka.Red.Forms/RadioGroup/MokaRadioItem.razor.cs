using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.RadioGroup;

/// <summary>
///     A single radio button item within a <see cref="MokaRadioGroup{TValue}" />.
///     Reads the selection from the parent group, which it gets as a cascading parameter.
/// </summary>
/// <typeparam name="TValue">The type of the value this radio item represents.</typeparam>
public partial class MokaRadioItem<TValue> : MokaComponentBase
{
	[CascadingParameter] private MokaRadioGroup<TValue>? ParentGroup { get; set; }

	/// <summary>The value this radio item represents. Required.</summary>
	[Parameter]
	[EditorRequired]
	public TValue Value { get; set; } = default!;

	/// <summary>Label text for this radio item.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Custom label content. Overrides <see cref="Label" />.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Whether this radio item is disabled.</summary>
	[Parameter]
	public bool Disabled { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-radio-item";

	private bool IsSelected => ParentGroup?.IsSelected(Value) ?? false;

	private bool IsDisabled => Disabled || (ParentGroup?.Disabled ?? false);

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-radio-item--selected", IsSelected)
		.AddClass("moka-radio-item--disabled", IsDisabled)
		.AddClass(Class)
		.Build();

	/// <summary>Radio item checks selection state each render.</summary>
	protected override bool ShouldRender() => true;

	// A radio only raises change when it becomes checked, by a click on the row or an arrow key.
	private void HandleChange(ChangeEventArgs e)
	{
		if (!IsDisabled)
		{
			ParentGroup?.SelectValue(Value);
		}
	}
}
