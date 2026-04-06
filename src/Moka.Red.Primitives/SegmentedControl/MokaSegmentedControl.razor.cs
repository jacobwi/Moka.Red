using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.SegmentedControl;

/// <summary>
///     A horizontal toggle between options (segmented control / pill toggle).
///     Children are <see cref="MokaSegment" /> components.
/// </summary>
public partial class MokaSegmentedControl
{
	/// <summary>Child content containing <see cref="MokaSegment" /> elements.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Currently selected segment value. Two-way bindable.</summary>
	[Parameter]
	public string? Value { get; set; }

	/// <summary>Callback for when <see cref="Value" /> changes.</summary>
	[Parameter]
	public EventCallback<string> ValueChanged { get; set; }

	/// <summary>Whether the control stretches to fill its container width. Default false.</summary>
	[Parameter]
	public bool FullWidth { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-segmented";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-segmented--{SizeToKebab(Size)}")
		.AddClass("moka-segmented--full-width", FullWidth)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	/// <summary>Has internal selection state.</summary>
	protected override bool ShouldRender() => true;

	/// <summary>Sets the selected segment value.</summary>
	internal async Task SelectAsync(string value)
	{
		if (Value != value)
		{
			Value = value;
			if (ValueChanged.HasDelegate)
			{
				await ValueChanged.InvokeAsync(value);
			}
		}
	}

	/// <summary>Checks whether a given segment value is currently selected.</summary>
	internal bool IsSelected(string value) => Value == value;
}
