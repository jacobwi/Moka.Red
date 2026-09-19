using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.RadioGroup;

/// <summary>
///     A radio button group component. Contains <see cref="MokaRadioItem{TValue}" /> children
///     that register via CascadingParameter. Supports vertical and horizontal layouts.
/// </summary>
/// <typeparam name="TValue">The type of the selected value.</typeparam>
public partial class MokaRadioGroup<TValue> : MokaVisualInputBase<TValue>
{
	/// <summary>The radio item children.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Label text displayed above the radio group.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text displayed below the radio group.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Layout direction. Default Column (vertical).</summary>
	[Parameter]
	public MokaDirection Orientation { get; set; } = MokaDirection.Column;

	/// <inheritdoc />
	protected override string RootClass => "moka-radiogroup";

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-radiogroup--row", Orientation == MokaDirection.Row)
		.AddClass("moka-radiogroup--disabled", Disabled)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
	{
		result = default!;
		validationErrorMessage = $"Cannot convert '{value}' to {typeof(TValue).Name}: the selected value comes from a child MokaRadioItem, not from a string.";
		return false;
	}

	/// <summary>
	///     The <c>name</c> every radio in the group shares. The browser needs it to treat them as one
	///     group: one checked at a time, one tab stop, and the arrow keys moving between them.
	/// </summary>
	internal string GroupName { get; } = $"moka-radiogroup-{Guid.NewGuid():N}";

	/// <summary>Selects a value from a radio item click.</summary>
	internal void SelectValue(TValue value)
	{
		CurrentValue = value;
		StateHasChanged();
	}

	/// <summary>Checks if a value is currently selected.</summary>
	internal bool IsSelected(TValue value) => EqualityComparer<TValue>.Default.Equals(value, CurrentValue);
}
