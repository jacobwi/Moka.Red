using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Tooltip;

namespace Moka.Red.Layout.StatusBar;

/// <summary>
///     Individual item within a <see cref="MokaStatusBar" />.
///     Supports text, icon, click handling, and tooltip.
/// </summary>
public partial class MokaStatusBarItem : MokaComponentBase
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	private ElementReference _element;

	// Whether the keys were bound to an item drawn inside a tooltip. A tooltip that comes or goes moves the item
	// into or out of the tooltip's element, which makes a new item element, and that one needs binding again.
	private bool? _keysBoundInTooltip;

	/// <summary>Custom content. Overrides <see cref="Text" /> and <see cref="Icon" />.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Text to display.</summary>
	[Parameter]
	public string? Text { get; set; }

	/// <summary>Icon to display before the text.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>
	///     Click handler. When set, the item is a button: it joins the tab order, and Enter or Space
	///     click it.
	/// </summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	/// <summary>
	///     Tooltip text, shown above the item in a <see cref="MokaTooltip" /> on hover and on keyboard focus, and
	///     wrapped when it is long. An item that shows only an icon takes its accessible name from it; an item that
	///     shows text carries it as its accessible description.
	/// </summary>
	[Parameter]
	public string? Tooltip { get; set; }

	/// <summary>
	///     Classes for the tooltip element that wraps the item when there is a <see cref="Tooltip" />. That element,
	///     not the item, is what the status bar's flex layout sees, so anything about how the item is placed among
	///     its neighbours (<c>min-width</c>, <c>flex-shrink</c>, <c>order</c>) belongs here; <see cref="MokaComponentBase.Class" />
	///     stays on the item and styles the item itself. Ignored without a tooltip, where the item is the flex item
	///     and <see cref="MokaComponentBase.Class" /> already reaches it.
	/// </summary>
	[Parameter]
	public string? TooltipClass { get; set; }

	/// <summary>
	///     Inline styles for the tooltip element that wraps the item. See <see cref="TooltipClass" /> for which of
	///     the two carries what.
	/// </summary>
	[Parameter]
	public string? TooltipStyle { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-statusbar-item";

	// The wrapper is the status bar's flex item, so a consumer needs a way to reach it. display: contents on it
	// would have let Class keep doing that job, but the trigger carries the tooltip's anchor-name and an element
	// with no box cannot be an anchor: the popup lost its anchor and moved about 90px away from the item.
	private string TooltipWrapperClass => new CssBuilder("moka-statusbar-item-tooltip")
		.AddClass(TooltipClass)
		.Build();

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-statusbar-item--clickable", IsClickable)
		.AddClass(Class)
		.Build();

	private bool IsClickable => OnClick.HasDelegate;

	private int? TabIndex => IsClickable ? 0 : null;

	private bool HasTooltip => !string.IsNullOrEmpty(Tooltip);

	private bool ShowsText => ChildContent is not null || !string.IsNullOrEmpty(Text);

	// An icon alone is named by the tooltip. Where the item shows text, that text stays its name and the tooltip
	// describes it, as the title it replaced did.
	private string? AccessibleName => HasTooltip && !ShowsText ? Tooltip : null;

	private string? AccessibleDescription => HasTooltip && ShowsText ? Tooltip : null;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// Enter and Space are handled in the browser, for the item itself only, and Space does not
		// scroll the page. Static items never load the script.
		if (IsClickable && _keysBoundInTooltip != HasTooltip)
		{
			_keysBoundInTooltip = HasTooltip;
			await SafeModuleInvokeVoidAsync(KeysModule, "bindActivation", _element);
		}
	}

	private async Task HandleClick(MouseEventArgs args)
	{
		if (IsClickable)
		{
			await OnClick.InvokeAsync(args);
		}
	}
}
