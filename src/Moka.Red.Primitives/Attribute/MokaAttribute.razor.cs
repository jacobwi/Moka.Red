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
///     Renders as &lt;button&gt; when interactive, &lt;span&gt; when static.
/// </summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix",
	Justification = "MokaAttribute is a UI component name, not a .NET attribute.")]
public partial class MokaAttribute
{
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

	/// <summary>Shows a dismiss (X) button at the end. Fires <see cref="OnDismiss" /> when clicked.</summary>
	[Parameter]
	public bool Dismissible { get; set; }

	/// <summary>Fires when the dismiss button is clicked.</summary>
	[Parameter]
	public EventCallback OnDismiss { get; set; }

	/// <summary>Makes the whole attribute clickable with pointer cursor.</summary>
	[Parameter]
	public bool Clickable { get; set; }

	/// <summary>Fires when clicked (only when <see cref="Clickable" /> is true).</summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	/// <summary>Enables toggle selection behavior.</summary>
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

	private bool IsInteractive => Clickable || Selectable || Dismissible;

	private MokaColor ResolvedColor => Color ?? MokaColor.Surface;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-attr--{VariantToKebab(Variant)}")
		.AddClass($"moka-attr--{ColorToKebab(ResolvedColor)}")
		.AddClass($"moka-attr--{SizeToKebab(Size)}")
		.AddClass("moka-attr--pill", Pill)
		.AddClass("moka-attr--clickable", Clickable || Selectable)
		.AddClass("moka-attr--selected", Selected)
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
		if (Pill && Rounded is null)
		{
			Rounded = MokaRounding.Full;
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
			Selected = !Selected;
			await SelectedChanged.InvokeAsync(Selected);
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
