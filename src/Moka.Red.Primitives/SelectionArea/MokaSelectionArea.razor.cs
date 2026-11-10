using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.SelectionArea;

/// <summary>
///     Wraps content whose items can be selected with a box, as in a file manager: a mouse press on empty space and a
///     drag draw a rectangle, and every item it touches is reported, live while dragging and once on release. The app
///     keeps the selection; this only says what the box covers and how to combine it. On by default, since drawing a box is
///     what the area is for. Turned off it draws nothing extra and listens to nothing.
/// </summary>
public partial class MokaSelectionArea : MokaVisualComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Core/moka-box-select.js";
	private const string DefaultKeyAttribute = "data-moka-select-key";

	private bool _bound;
	private BoundOptions? _boundOptions;
	private bool _disposed;
	private DotNetObjectReference<MokaSelectionArea>? _dotNetRef;
	private ElementReference _root;

	/// <summary>The content, with the items somewhere inside it.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Lets a mouse press on empty space draw a box. Default on; off renders the content in a plain <c>div</c> and loads
	///     no script. A press on an item, a control, a link or editable text keeps its own behaviour, and touch and pen keep
	///     scrolling.
	/// </summary>
	[Parameter]
	public bool BoxSelect { get; set; } = true;

	/// <summary>The attribute an item carries its key in. Default <c>data-moka-select-key</c>.</summary>
	[Parameter]
	public string KeyAttribute { get; set; } = DefaultKeyAttribute;

	/// <summary>A CSS selector for the items. Null is every element carrying <see cref="KeyAttribute" />.</summary>
	[Parameter]
	public string? ItemSelector { get; set; }

	/// <summary>
	///     A CSS selector for the element that scrolls, looked for among the area's ancestors first and then in the page.
	///     Null is the nearest scrolling ancestor, the area itself included, or the window.
	/// </summary>
	[Parameter]
	public string? ScrollContainerSelector { get; set; }

	/// <summary>
	///     Describes items on lines of equal height, so the box also covers the ones that aren't drawn and reports them
	///     as a <see cref="MokaBoxSelectArgs.Range" />. For a virtualized list.
	/// </summary>
	[Parameter]
	public MokaBoxSelectGeometry? Geometry { get; set; }

	/// <summary>
	///     Raised when a box starts, each time what it covers changes (at most once a frame, and never while the last
	///     report is still on its way), and when it finishes or is cancelled.
	/// </summary>
	[Parameter]
	public EventCallback<MokaBoxSelectArgs> OnBoxSelect { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-selection-area";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	private bool IsActive => BoxSelect && !Disabled;

	/// <summary>Called by the box selection script with what the box covers.</summary>
	/// <param name="phase">A <see cref="MokaBoxSelectPhase" /> value.</param>
	/// <param name="mode">A <see cref="MokaBoxSelectMode" /> value.</param>
	/// <param name="keys">The keys of the drawn items the box touches.</param>
	/// <param name="range">The first and last line and column, the columns and the count, or null.</param>
	/// <returns>A task that completes once <see cref="OnBoxSelect" /> has run.</returns>
	[JSInvokable]
	public Task HandleBoxSelect(int phase, int mode, string[]? keys, int[]? range)
	{
		if (!Enum.IsDefined((MokaBoxSelectPhase)phase) || !Enum.IsDefined((MokaBoxSelectMode)mode))
		{
			return Task.CompletedTask;
		}

		MokaBoxSelectRange? covered = range is { Length: 6 }
			? new MokaBoxSelectRange(range[0], range[1], range[2], range[3], range[4], range[5])
			: null;
		MokaBoxSelectArgs args = new((MokaBoxSelectPhase)phase, (MokaBoxSelectMode)mode, keys ?? [], covered);
		return InvokeAsync(() => RaiseAsync(args));
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		if (!IsActive)
		{
			if (_bound)
			{
				_bound = false;
				_boundOptions = null;
				await SafeModuleInvokeVoidAsync(ModulePath, "unbindBoxSelect", _root);
			}

			return;
		}

		BoundOptions options = new(KeyAttribute, ItemSelector, ScrollContainerSelector, Geometry);
		if (_bound && options == _boundOptions)
		{
			return;
		}

		_bound = true;
		_boundOptions = options;
		_dotNetRef ??= DotNetObjectReference.Create(this);
		await SafeModuleInvokeVoidAsync(ModulePath, "bindBoxSelect", _root, _dotNetRef, ScriptOptions(options));
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		_disposed = true;
		if (_bound)
		{
			_bound = false;
			await SafeModuleInvokeVoidAsync(ModulePath, "unbindBoxSelect", _root);
		}

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}

	private static Dictionary<string, object?> ScriptOptions(BoundOptions options) => new()
	{
		["box"] = ":scope > [data-moka-box]",
		["items"] = options.ItemSelector,
		["key"] = string.IsNullOrWhiteSpace(options.KeyAttribute) ? DefaultKeyAttribute : options.KeyAttribute,
		["scroller"] = options.ScrollContainer,
		["geometry"] = options.Geometry is { } geometry
			? new Dictionary<string, object?>
			{
				["count"] = geometry.Count,
				["columns"] = geometry.Columns,
				["lineHeight"] = geometry.LineHeight,
				["index"] = geometry.IndexAttribute,
				["base"] = geometry.IndexBase
			}
			: null,
		["method"] = nameof(HandleBoxSelect)
	};

	// JS swallows what a call from it throws, so a failing handler is handed to the error boundary from here.
	private async Task RaiseAsync(MokaBoxSelectArgs args)
	{
		try
		{
			await OnBoxSelect.InvokeAsync(args);
		}
		catch (Exception ex) when (!_disposed)
		{
			await DispatchExceptionAsync(ex);
		}
	}

	private sealed record BoundOptions(string KeyAttribute, string? ItemSelector, string? ScrollContainer, MokaBoxSelectGeometry? Geometry);
}
