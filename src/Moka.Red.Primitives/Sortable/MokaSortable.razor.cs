using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Sortable;

/// <summary>
///     Drag-to-reorder list. Supports vertical/horizontal orientation,
///     drag handles, disabled items, and cross-list grouping.
/// </summary>
/// <typeparam name="TItem">The item type.</typeparam>
public partial class MokaSortable<TItem>
{
	private ElementReference _containerRef;
	private DotNetObjectReference<MokaSortable<TItem>>? _dotNetRef;
	private bool _jsAttached;
	private IJSObjectReference? _jsModule;

	/// <summary>The list of items to render and reorder. Must be a mutable list.</summary>
	[Parameter]
	[EditorRequired]
	public IList<TItem> Items { get; set; } = default!;

	/// <summary>Template for rendering each item.</summary>
	[Parameter]
	public RenderFragment<TItem>? ItemTemplate { get; set; }

	/// <summary>Called after a drag-reorder with old and new indices.</summary>
	[Parameter]
	public EventCallback<(int OldIndex, int NewIndex)> OnReorder { get; set; }

	/// <summary>When true, only the grip handle initiates drag. Default false (whole item).</summary>
	[Parameter]
	public bool DragHandle { get; set; }

	/// <summary>Horizontal layout. Default false (vertical).</summary>
	[Parameter]
	public bool Horizontal { get; set; }

	/// <summary>Group name for cross-list drag support.</summary>
	[Parameter]
	public string? Group { get; set; }

	/// <summary>Per-item disabled predicate.</summary>
	[Parameter]
	public Func<TItem, bool>? IsItemDisabled { get; set; }

	/// <summary>Key selector for stable rendering.</summary>
	[Parameter]
	public Func<TItem, object>? ItemKey { get; set; }

	[Inject] private IJSRuntime JsRuntime { get; set; } = default!;

	/// <inheritdoc />
	protected override string RootClass => "moka-sortable";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-sortable--horizontal", Horizontal)
		.AddClass("moka-sortable--has-handle", DragHandle)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <summary>Called from JS when a sort completes.</summary>
	[JSInvokable]
	public async Task OnSortEnd(int oldIndex, int newIndex)
	{
		if (oldIndex == newIndex || oldIndex < 0 || newIndex < 0)
		{
			return;
		}

		if (oldIndex >= Items.Count || newIndex > Items.Count)
		{
			return;
		}

		TItem item = Items[oldIndex];
		Items.RemoveAt(oldIndex);
		Items.Insert(Math.Min(newIndex, Items.Count), item);

		await OnReorder.InvokeAsync((oldIndex, newIndex));
		StateHasChanged();
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!_jsAttached && HasRendered)
		{
			_jsModule ??= await JsRuntime.InvokeAsync<IJSObjectReference>(
				"import", "./_content/Moka.Red.Core/moka-drag.js");
			_dotNetRef ??= DotNetObjectReference.Create(this);

			await SafeJsInvokeAsync("initSortable", _dotNetRef, _containerRef,
				new { horizontal = Horizontal, dragHandle = DragHandle, group = Group });
			_jsAttached = true;
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_jsModule is not null && _jsAttached)
		{
			await SafeJsInvokeAsync("removeSortable", _containerRef);
		}

		_dotNetRef?.Dispose();

		if (_jsModule is not null)
		{
			try
			{
				await _jsModule.DisposeAsync();
			}
			catch (JSDisconnectedException)
			{
			}

			_jsModule = null;
		}

		await base.DisposeAsyncCore();
	}

	private async ValueTask SafeJsInvokeAsync(string method, params object?[] args)
	{
		if (_jsModule is null)
		{
			return;
		}

		try
		{
			await _jsModule.InvokeVoidAsync(method, args);
		}
		catch (JSDisconnectedException)
		{
		}
		catch (ObjectDisposedException)
		{
		}
	}
}
