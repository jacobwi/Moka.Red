using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.StatusBar;

/// <summary>
///     Bottom status bar similar to VS Code's status bar. Fixed to the bottom of the viewport
///     with compact height and small font. Supports start (left) and end (right) content areas.
///     When the two sides don't fit, items that may shrink give way first, then whole items drop out,
///     lowest <see cref="MokaStatusBarItem.Priority" /> first.
/// </summary>
public partial class MokaStatusBar : MokaVisualComponentBase
{
	private const string JsModulePath = "./_content/Moka.Red.Layout/StatusBar/MokaStatusBar.razor.js";

	private DotNetObjectReference<MokaStatusBar>? _dotNetRef;
	private int _droppedCount;
	private bool _expanded;
	private bool? _reportingDrops;
	private ElementReference _root;

	[CascadingParameter]
	private MokaTheme? CascadedTheme { get; set; }

	/// <summary>Generic status bar content.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Left-aligned status items.</summary>
	[Parameter]
	public RenderFragment? StartContent { get; set; }

	/// <summary>Right-aligned status items.</summary>
	[Parameter]
	public RenderFragment? EndContent { get; set; }

	/// <summary>Whether to show a top border. Default true.</summary>
	[Parameter]
	public bool Bordered { get; set; } = true;

	/// <summary>
	///     Whether the bar is fixed to the bottom of the window, across its whole width. Default true. False lays it out
	///     where it is, as a row across its container, for a status row under a panel or a view.
	/// </summary>
	[Parameter]
	public bool Fixed { get; set; } = true;

	/// <summary>
	///     How this bar is drawn. Null takes the theme's choice (<see cref="MokaTheme.WithStatusBar" />), which is
	///     <see cref="MokaStatusBarLook.Standard" /> unless the app set one.
	/// </summary>
	[Parameter]
	public MokaStatusBarLook? Look { get; set; }

	/// <summary>
	///     Shows a button with the number of items that didn't fit. It opens the bar onto more rows, with those items in
	///     their usual places, and goes away once everything fits. Needs an interactive render.
	/// </summary>
	[Parameter]
	public bool Expandable { get; set; }

	/// <summary>Names the button in <see cref="Expandable" />, from how many items didn't fit. Default "Show 2 more items".</summary>
	[Parameter]
	public Func<int, string>? MoreLabel { get; set; }

	/// <summary>Names the button in <see cref="Expandable" /> while the bar is open.</summary>
	[Parameter]
	public string LessLabel { get; set; } = "Show fewer items";

	/// <inheritdoc />
	protected override string RootClass => "moka-statusbar";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-statusbar--bordered", Bordered)
		.AddClass("moka-statusbar--inline", !Fixed)
		.AddClass(LookClass)
		.AddClass("moka-statusbar--expanded", IsExpanded)
		.AddClass(Class)
		.Build();

	private string? LookClass => (Look ?? CascadedTheme?.StatusBar ?? MokaStatusBarLook.Standard) switch
	{
		MokaStatusBarLook.Accent => "moka-statusbar--accent",
		MokaStatusBarLook.Hairline => "moka-statusbar--hairline",
		MokaStatusBarLook.Floating => "moka-statusbar--floating",
		MokaStatusBarLook.Chips => "moka-statusbar--chips",
		MokaStatusBarLook.Segments => "moka-statusbar--segments",
		MokaStatusBarLook.Terminal => "moka-statusbar--terminal",
		_ => null
	};

	private bool IsExpanded => Expandable && _expanded;

	private string AriaExpanded => IsExpanded ? "true" : "false";

	private string CountText => _droppedCount.ToString(CultureInfo.InvariantCulture);

	private string MoreText => IsExpanded ? $"-{CountText}" : $"+{CountText}";

	private string MoreName => IsExpanded
		? LessLabel
		: MoreLabel?.Invoke(_droppedCount) ?? $"Show {CountText} more items";

	/// <summary>Called by the bar's script when the number of items that didn't fit changes.</summary>
	/// <param name="count">How many items are out of the bar.</param>
	/// <returns>A task that completes once the bar has been asked to redraw.</returns>
	[JSInvokable]
	public Task OnDroppedChanged(int count) =>
		InvokeAsync(() =>
		{
			_droppedCount = count;
			_expanded = _expanded && count > 0;
			StateHasChanged();
		});

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (_reportingDrops != Expandable)
		{
			_reportingDrops = Expandable;
			_dotNetRef ??= Expandable ? DotNetObjectReference.Create(this) : null;
			await SafeModuleInvokeVoidAsync(JsModulePath, "bindStatusBar", _root, Expandable ? _dotNetRef : null);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		await SafeModuleInvokeVoidAsync(JsModulePath, "unbindStatusBar", _root);
		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}

	private void ToggleExpanded() => _expanded = !_expanded;
}
