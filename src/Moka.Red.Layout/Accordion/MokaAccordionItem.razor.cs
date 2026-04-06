using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.Accordion;

/// <summary>
///     A single collapsible section within a <see cref="MokaAccordion" />.
///     Registers with the parent to support single-expand behavior.
/// </summary>
public partial class MokaAccordionItem : MokaComponentBase
{
	[CascadingParameter] private MokaAccordion? ParentAccordion { get; set; }

	/// <summary>Content shown when the item is expanded.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Custom header content. Overrides <see cref="Title" /> and <see cref="Subtitle" />.</summary>
	[Parameter]
	public RenderFragment? Header { get; set; }

	/// <summary>Simple text title for the header.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>Secondary text below the title.</summary>
	[Parameter]
	public string? Subtitle { get; set; }

	/// <summary>Whether this item starts expanded. Default false.</summary>
	[Parameter]
	public bool DefaultExpanded { get; set; }

	/// <summary>Whether this item is disabled and cannot be toggled.</summary>
	[Parameter]
	public bool Disabled { get; set; }

	/// <summary>Custom expand icon definition. Uses CSS chevron by default.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>Whether the item is currently expanded.</summary>
	internal bool IsExpanded { get; private set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-accordion-item";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-accordion-item--expanded", IsExpanded)
		.AddClass("moka-accordion-item--disabled", Disabled)
		.AddClass(Class)
		.Build();

	private string? BodyStyle => IsExpanded
		? "max-height: 500px"
		: "max-height: 0";

	/// <summary>
	///     Accordion items have internal expand/collapse state that changes independently of parameters.
	/// </summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		IsExpanded = DefaultExpanded;
		ParentAccordion?.AddItem(this);
	}

	/// <summary>Collapses this item without user interaction.</summary>
	internal void Collapse()
	{
		if (IsExpanded)
		{
			IsExpanded = false;
			InvokeAsync(StateHasChanged);
		}
	}

	private void Toggle()
	{
		if (Disabled)
		{
			return;
		}

		IsExpanded = !IsExpanded;

		if (IsExpanded)
		{
			ParentAccordion?.NotifyItemExpanding(this);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		ParentAccordion?.RemoveItem(this);
		await base.DisposeAsyncCore();
	}
}
