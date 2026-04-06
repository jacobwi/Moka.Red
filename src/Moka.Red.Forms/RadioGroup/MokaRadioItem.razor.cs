using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.RadioGroup;

/// <summary>
///     A single radio button item within a <see cref="MokaRadioGroup{TValue}" />.
///     Registers with the parent group via CascadingParameter.
/// </summary>
/// <typeparam name="TValue">The type of the value this radio item represents.</typeparam>
public partial class MokaRadioItem<TValue> : MokaComponentBase
{
	private readonly string _inputId = $"moka-radio-{Guid.NewGuid():N}";

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

	/// <inheritdoc />
	protected override void OnInitialized() => ParentGroup?.AddItem(this);

	private void HandleClick()
	{
		if (IsDisabled)
		{
			return;
		}

		ParentGroup?.SelectValue(Value);
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		ParentGroup?.RemoveItem(this);
		await base.DisposeAsyncCore();
	}
}
