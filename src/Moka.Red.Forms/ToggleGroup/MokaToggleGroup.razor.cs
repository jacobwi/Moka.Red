using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.ToggleGroup;

/// <summary>
///     A group of connected toggle buttons where one or more can be selected.
///     In single-select mode, use <see cref="Value" />/<see cref="ValueChanged" />.
///     In multi-select mode, set <see cref="Multiple" /> and use <see cref="Values" />/<see cref="ValuesChanged" />.
///     Children are <see cref="MokaToggleGroupItem" /> components.
/// </summary>
public partial class MokaToggleGroup : MokaVisualComponentBase
{
	/// <summary>Child content containing <see cref="MokaToggleGroupItem" /> elements.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Currently selected value in single-select mode. Two-way bindable.</summary>
	[Parameter]
	public string? Value { get; set; }

	/// <summary>Callback for when <see cref="Value" /> changes (single-select mode).</summary>
	[Parameter]
	public EventCallback<string?> ValueChanged { get; set; }

	/// <summary>Currently selected values in multi-select mode. Two-way bindable.</summary>
	[Parameter]
	public IReadOnlyList<string>? Values { get; set; }

	/// <summary>Callback for when <see cref="Values" /> changes (multi-select mode).</summary>
	[Parameter]
	public EventCallback<IReadOnlyList<string>?> ValuesChanged { get; set; }

	/// <summary>Whether multiple items can be selected simultaneously. Default is false.</summary>
	[Parameter]
	public bool Multiple { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-toggle-group";

	private MokaColor ResolvedColor => Color ?? MokaColor.Primary;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-toggle-group--{SizeToKebab(Size)}")
		.AddClass($"moka-toggle-group--{ColorToKebab(ResolvedColor)}")
		.AddClass("moka-toggle-group--disabled", Disabled)
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

	/// <summary>Checks whether a given item value is currently selected.</summary>
	internal bool IsSelected(string value)
	{
		if (Multiple)
		{
			return Values?.Contains(value) == true;
		}

		return Value == value;
	}

	/// <summary>Toggles the selection of an item.</summary>
#pragma warning disable CA1868 // Remove/Contains pattern — false positive: Remove return value is used for toggle logic
	internal async Task ToggleAsync(string value)
	{
		if (Multiple)
		{
			List<string> current = Values?.ToList() ?? [];
			if (!current.Remove(value))
			{
				current.Add(value);
			}

			Values = current.AsReadOnly();
			await ValuesChanged.InvokeAsync(Values);
		}
		else
		{
			// Single select: toggle off if same value clicked
			Value = Value == value ? null : value;
			await ValueChanged.InvokeAsync(Value);
		}
	}
#pragma warning restore CA1868
}
