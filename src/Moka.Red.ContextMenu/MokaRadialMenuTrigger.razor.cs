using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;

namespace Moka.Red.ContextMenu;

/// <summary>
///     Opens a <see cref="MokaRadialMenu" /> around its content on a right click, Shift+F10, the context-menu key or
///     a long press. Uses <c>display: contents</c>, so it takes no room. With a <see cref="MokaContextMenuHost" />
///     mounted it opens the shared menu through <see cref="IMokaContextMenuService" />, so only one menu is ever
///     open; without one it draws its own.
/// </summary>
public partial class MokaRadialMenuTrigger : ComponentBase, IAsyncDisposable
{
	private const string ModulePath = "./_content/Moka.Red.ContextMenu/MokaRadialMenuTrigger.razor.js";

	private bool _disposed;
	private bool _flick;
	private bool _fromKeyboard;
	private IJSObjectReference? _module;
	private bool _open;
	private MokaRadialMenuSpec? _openSpec;
	private IMokaContextMenuService? _service;
	private ElementReference _wrapper;
	private double _x;
	private double _y;

	[Inject]
	private IServiceProvider Services { get; set; } = default!;

	[Inject]
	private IJSRuntime JsRuntime { get; set; } = default!;

	/// <summary>What the menu acts on.</summary>
	[Parameter]
	[EditorRequired]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>What the menu holds. Read when the menu opens. Without it a right click shows the browser's own menu.</summary>
	[Parameter]
	[EditorRequired]
	public MokaRadialMenuSpec? Spec { get; set; }

	/// <summary>Whether the trigger opens nothing, leaving the browser's own menu.</summary>
	[Parameter]
	public bool Disabled { get; set; }

	/// <summary>How the buttons sit around the target. Wins over <see cref="MokaRadialMenuSpec.Shape" /> when set.</summary>
	[Parameter]
	public MokaRadialShape? Shape { get; set; }

	/// <summary>How the menu is drawn. Wins over <see cref="MokaRadialMenuSpec.Look" /> when set.</summary>
	[Parameter]
	public MokaRadialLook? Look { get; set; }

	/// <summary>How a button's choices show. Wins over <see cref="MokaRadialMenuSpec.ChoiceStyle" /> when set.</summary>
	[Parameter]
	public MokaRadialChoiceStyle? ChoiceStyle { get; set; }

	/// <summary>
	///     Whether a ring turns into a half ring or a corner fan near the window's edges. Wins over
	///     <see cref="MokaRadialMenuSpec.AdaptToEdges" /> when set.
	/// </summary>
	[Parameter]
	public bool? AdaptToEdges { get; set; }

	private bool CanOpen => !Disabled && Spec is not null;

	// The trigger's choices travel in the spec, so the shared host draws them too.
	private MokaRadialMenuSpec OpeningSpec(MokaRadialMenuSpec spec) =>
		Shape is null && Look is null && ChoiceStyle is null && AdaptToEdges is null
			? spec
			: spec with
			{
				Shape = Shape ?? spec.Shape,
				Look = Look ?? spec.Look,
				ChoiceStyle = ChoiceStyle ?? spec.ChoiceStyle,
				AdaptToEdges = AdaptToEdges ?? spec.AdaptToEdges
			};

	private bool UsesSharedMenu => _service is { HasHost: true };

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		await _module.SafeDisposeAsync();
		_module = null;

		GC.SuppressFinalize(this);
	}

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnInitialized() => _service = Services.GetService<IMokaContextMenuService>();

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender || _disposed)
		{
			return;
		}

		// A touch screen's long press: the module sends it on as a contextmenu event, so it arrives below like a
		// right click, with the finger still down.
		try
		{
			_module ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
			await _module.InvokeVoidAsync("bindLongPress", _wrapper);
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
		}
		catch (InvalidOperationException)
		{
			// JS interop attempted during prerendering.
		}
	}

	private void HandleContextMenu(MouseEventArgs e)
	{
		if (!CanOpen)
		{
			return;
		}

		if (UsesSharedMenu)
		{
			_service!.ShowRadial(e, OpeningSpec(Spec!));
			return;
		}

		bool fromKeyboard = MokaContextMenuService.IsFromKeyboard(e);
		_openSpec = OpeningSpec(Spec!);
		_x = e.ClientX;
		_y = e.ClientY;
		_fromKeyboard = fromKeyboard;
		_flick = !fromKeyboard && e.Buttons != 0;
		_open = true;
	}

	private void Close() => _open = false;
}
