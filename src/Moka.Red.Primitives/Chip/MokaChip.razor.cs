using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Chip;

/// <summary>
///     A compact interactive element for filtering, selection, or tags.
///     Features Material-style selected state with check icon and filled background.
/// </summary>
public partial class MokaChip
{
	/// <summary>Custom child content for the chip label.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Chip label text.</summary>
	[Parameter]
	public string? Text { get; set; }

	/// <summary>Leading icon.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>Small avatar URL displayed as a leading image.</summary>
	[Parameter]
	public string? Avatar { get; set; }

	/// <summary>Shows a close/remove button. Default false.</summary>
	[Parameter]
	public bool Closable { get; set; }

	/// <summary>Fires when the close button is clicked.</summary>
	[Parameter]
	public EventCallback OnClose { get; set; }

	/// <summary>Whether the chip is currently selected. Two-way bindable.</summary>
	[Parameter]
	public bool Selected { get; set; }

	/// <summary>Callback for when <see cref="Selected" /> changes.</summary>
	[Parameter]
	public EventCallback<bool> SelectedChanged { get; set; }

	/// <summary>Click event callback.</summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-chip";

	private MokaColor ResolvedColor => Color ?? MokaColor.Surface;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-chip--{SizeToKebab(Size)}")
		.AddClass($"moka-chip--{ColorToKebab(ResolvedColor)}")
		.AddClass("moka-chip--selected", Selected)
		.AddClass("moka-chip--closable", Closable)
		.AddClass("moka-chip--disabled", Disabled)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private MokaSize IconSize => Size switch
	{
		MokaSize.Xs => MokaSize.Xs,
		MokaSize.Sm => MokaSize.Xs,
		MokaSize.Md => MokaSize.Sm,
		MokaSize.Lg => MokaSize.Sm,
		_ => MokaSize.Xs
	};

	/// <summary>Chip has selectable toggle state that changes independently of parameters.</summary>
	protected override bool ShouldRender() => true;

	private async Task HandleClick(MouseEventArgs args)
	{
		if (Disabled)
		{
			return;
		}

		if (SelectedChanged.HasDelegate)
		{
			Selected = !Selected;
			await SelectedChanged.InvokeAsync(Selected);
		}

		if (OnClick.HasDelegate)
		{
			await OnClick.InvokeAsync(args);
		}
	}

	private async Task HandleClose()
	{
		if (!Disabled && OnClose.HasDelegate)
		{
			await OnClose.InvokeAsync();
		}
	}
}
