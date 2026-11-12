using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Attribute;

/// <summary>
///     A compact chip/tag/badge component for displaying attributes, tags, filters, selections, or statuses.
///     Can be static (display only) or interactive (clickable, selectable, dismissible).
/// </summary>
/// <remarks>
///     A <see cref="Clickable" /> or <see cref="Selectable" /> attribute is a button, and a selectable one carries
///     <c>aria-pressed</c>. An attribute with neither is a plain <c>span</c>. A <see cref="Dismissible" /> attribute is a
///     <c>span</c> holding its own button, when it has one, beside the remove button, since a button can't hold another.
///     <c>Class</c>, <c>Style</c>, <c>Id</c> and other attributes go on the outermost element.
/// </remarks>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix",
	Justification = "MokaAttribute is a UI component name, not a .NET attribute.")]
public partial class MokaAttribute
{
	private bool? _lastSelected;
	private bool _selected;

	/// <summary>Main content/text of the attribute.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Optional prefix label for key-value style display. Rendered in a dimmer style before the content.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Icon displayed before the content.</summary>
	[Parameter]
	public MokaIconDefinition? StartIcon { get; set; }

	/// <summary>Icon displayed after the content.</summary>
	[Parameter]
	public MokaIconDefinition? EndIcon { get; set; }

	/// <summary>URL for a small circular avatar image prefix.</summary>
	[Parameter]
	public string? Avatar { get; set; }

	/// <summary>
	///     Shows a remove button at the end, named by <see cref="DismissLabel" /> through <c>aria-label</c> and a tooltip.
	///     It is a tab stop of its own, disabled with the attribute, and fires <see cref="OnDismiss" />.
	/// </summary>
	[Parameter]
	public bool Dismissible { get; set; }

	/// <summary>Fires when the remove button is clicked, or pressed with Enter or Space.</summary>
	[Parameter]
	public EventCallback OnDismiss { get; set; }

	/// <summary>
	///     The remove button's name, for its <c>aria-label</c> and tooltip, such as "Remove React". Unset, it is "Remove"
	///     and the <see cref="Label" /> ("Remove Status"), or "Remove" when there is no label, since the content is markup
	///     the attribute can't read.
	/// </summary>
	[Parameter]
	public string? DismissLabel { get; set; }

	/// <summary>Makes the whole attribute a button with a pointer cursor.</summary>
	[Parameter]
	public bool Clickable { get; set; }

	/// <summary>Fires when clicked (only when <see cref="Clickable" /> is true).</summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	/// <summary>Makes the attribute a toggle button that says whether it is selected through <c>aria-pressed</c>.</summary>
	[Parameter]
	public bool Selectable { get; set; }

	/// <summary>Whether the attribute is currently selected. Two-way bindable.</summary>
	[Parameter]
	public bool Selected { get; set; }

	/// <summary>Callback for when <see cref="Selected" /> changes.</summary>
	[Parameter]
	public EventCallback<bool> SelectedChanged { get; set; }

	/// <summary>Full border-radius (pill shape). When false, uses standard border-radius. Default true.</summary>
	[Parameter]
	public bool Pill { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-attr";

	private bool HasAction => Clickable || Selectable;

	private string? AriaPressed => !Selectable ? null : _selected ? "true" : "false";

	private string DismissName => !string.IsNullOrWhiteSpace(DismissLabel) ? DismissLabel
		: string.IsNullOrWhiteSpace(Label) ? "Remove"
		: $"Remove {Label}";

	private MokaColor ResolvedColor => Color ?? MokaColor.Surface;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-attr--{VariantToKebab(Variant)}")
		.AddClass($"moka-attr--{ColorToKebab(ResolvedColor)}")
		.AddClass($"moka-attr--{SizeToKebab(Size)}")
		.AddClass("moka-attr--pill", Pill)
		.AddClass("moka-attr--clickable", HasAction)
		.AddClass("moka-attr--dismissible", Dismissible)
		.AddClass("moka-attr--selected", _selected)
		.AddClass("moka-attr--disabled", Disabled)
		.AddClass("moka-attr--has-label", Label is not null)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	/// <summary>Maps attribute size to a slightly smaller icon size.</summary>
	private MokaSize IconSize => Size switch
	{
		MokaSize.Xs => MokaSize.Xs,
		MokaSize.Sm => MokaSize.Xs,
		MokaSize.Md => MokaSize.Sm,
		MokaSize.Lg => MokaSize.Sm,
		_ => MokaSize.Xs
	};

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Selected only seeds the state when the parent passes a new value, so a parent render that
		// passes the old one again cannot undo a click.
		if (_lastSelected != Selected)
		{
			_lastSelected = Selected;
			_selected = Selected;
		}
	}

	/// <summary>Attribute has selectable toggle state that changes independently of parameters.</summary>
	protected override bool ShouldRender() => true;

	private async Task HandleClick(MouseEventArgs args)
	{
		if (Disabled)
		{
			return;
		}

		if (Selectable)
		{
			_selected = !_selected;
			await SelectedChanged.InvokeAsync(_selected);
		}

		if (Clickable)
		{
			await OnClick.InvokeAsync(args);
		}
	}

	private async Task HandleDismiss()
	{
		if (!Disabled)
		{
			await OnDismiss.InvokeAsync();
		}
	}
}
