using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.SplitButton;

/// <summary>
///     A split button with a primary action and a dropdown arrow for additional actions.
///     The left side triggers the primary <see cref="OnClick" /> action, while the right
///     arrow toggles a dropdown menu rendered from <see cref="DropdownContent" />.
/// </summary>
public partial class MokaSplitButton
{
	private const string ModulePath = "./_content/Moka.Red.Primitives/SplitButton/MokaSplitButton.razor.js";

	private readonly string _menuId = $"moka-split-btn-{Guid.NewGuid():N}";
	private bool _bound;
	private bool _isOpen;
	private ElementReference _root;

	/// <summary>Primary button label content.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Primary button click event.</summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	/// <summary>
	///     Content rendered inside the dropdown panel, usually <c>MokaDropdownItem</c>s. The panel is a
	///     menu: the arrow keys move between its <c>role="menuitem"</c> elements.
	/// </summary>
	[Parameter]
	public RenderFragment? DropdownContent { get; set; }

	/// <summary>Accessible name of the arrow button, which shows only an icon. Defaults to "More options".</summary>
	[Parameter]
	public string ToggleLabel { get; set; } = "More options";

	/// <inheritdoc />
	protected override string RootClass => "moka-split-btn";

	private MokaColor ResolvedColor => Color ?? MokaColor.Primary;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-split-btn--{VariantToKebab(Variant)}")
		.AddClass($"moka-split-btn--{ColorToKebab(ResolvedColor)}")
		.AddClass($"moka-split-btn--{SizeToKebab(Size)}")
		.AddClass("moka-split-btn--disabled", Disabled)
		.AddClass("moka-split-btn--open", _isOpen)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle(Style)
		.Build();

	/// <summary>Maps button size to a slightly smaller icon size.</summary>
	private MokaSize IconSize => Size switch
	{
		MokaSize.Xs => MokaSize.Xs,
		MokaSize.Sm => MokaSize.Xs,
		MokaSize.Md => MokaSize.Sm,
		MokaSize.Lg => MokaSize.Md,
		_ => MokaSize.Sm
	};

	private string MenuId => _menuId;

	private string ToggleId => $"{_menuId}-toggle";

	/// <summary>Has internal open/closed state.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// Loaded only for a split button that has a menu to drive.
		if (!_bound && DropdownContent is not null)
		{
			_bound = true;
			await SafeModuleInvokeVoidAsync(ModulePath, "bindSplitButton", _root);
		}
	}

	private async Task HandlePrimaryClick(MouseEventArgs args)
	{
		if (!Disabled)
		{
			await OnClick.InvokeAsync(args);
		}
	}

	private void ToggleDropdown()
	{
		if (!Disabled)
		{
			_isOpen = !_isOpen;
		}
	}

	private void CloseDropdown() => _isOpen = false;
}
