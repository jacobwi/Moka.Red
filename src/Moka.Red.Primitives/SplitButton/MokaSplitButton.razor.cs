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
	private const string PopupModule = "./_content/Moka.Red.Core/moka-popup.js";

	private readonly string _menuId = $"moka-split-btn-{Guid.NewGuid():N}";
	private bool _announcing;
	private bool _bound;
	private bool _isOpen;
	private ElementReference _menu;
	private bool _menuShown;
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

	/// <summary>
	///     Marks the main action busy, as <c>MokaButton.Loading</c> does: a spinner over its label, so the split button
	///     keeps its width, <c>aria-busy</c>, and <see cref="LoadingText" /> announced. Both halves stay focusable with
	///     <c>aria-disabled</c>, so a keyboard user keeps their place, and neither acts: <see cref="OnClick" /> isn't
	///     raised, the menu doesn't open and an open one closes. The menu holds other forms of the same action, such as
	///     Save as or Save a draft, which shouldn't start while the first one runs.
	/// </summary>
	[Parameter]
	public bool Loading { get; set; }

	/// <summary>What a screen reader hears when the main action becomes busy. Defaults to "Loading".</summary>
	[Parameter]
	public string LoadingText { get; set; } = "Loading";

	/// <inheritdoc />
	protected override string RootClass => "moka-split-btn";

	private MokaColor ResolvedColor => Color ?? MokaColor.Primary;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-split-btn--{VariantToKebab(Variant)}")
		.AddClass($"moka-split-btn--{ColorToKebab(ResolvedColor)}")
		.AddClass($"moka-split-btn--{SizeToKebab(Size)}")
		.AddClass("moka-split-btn--disabled", Disabled || Loading)
		.AddClass("moka-split-btn--loading", Loading)
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

	private bool Busy => Loading && !Disabled;

	private string ToggleId => $"{_menuId}-toggle";

	/// <summary>Has internal open/closed state.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		if (Loading)
		{
			_isOpen = false;
		}
		else
		{
			_announcing = false;
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// The status region is drawn empty first and filled on the next render, since a region that arrives with its
		// text already in it isn't announced by every screen reader.
		if (Loading && !_announcing)
		{
			_announcing = true;
			StateHasChanged();
		}

		// Loaded only for a split button that has a menu to drive.
		if (!_bound && DropdownContent is not null)
		{
			_bound = true;
			await SafeModuleInvokeVoidAsync(ModulePath, "bindSplitButton", _root);
		}

		// The menu goes to the top layer, out of reach of an ancestor's overflow and of later stacking contexts.
		bool menuOpen = _isOpen && DropdownContent is not null;
		if (_menuShown != menuOpen)
		{
			_menuShown = menuOpen;
			if (menuOpen)
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "open", _menuId, _menu, _root, null,
					new Dictionary<string, object?> { ["placement"] = "bottom-end", ["width"] = "min" });
			}
			else
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "close", _menuId);
			}
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_menuShown)
		{
			_menuShown = false;
			await SafeModuleInvokeVoidAsync(PopupModule, "close", _menuId);
		}

		await base.DisposeAsyncCore();
	}

	private async Task HandlePrimaryClick(MouseEventArgs args)
	{
		if (!Disabled && !Loading)
		{
			await OnClick.InvokeAsync(args);
		}
	}

	private void ToggleDropdown()
	{
		if (!Disabled && !Loading)
		{
			_isOpen = !_isOpen;
		}
	}

	private void CloseDropdown() => _isOpen = false;
}
