using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Core.Theming;

public partial class MokaThemeProvider : ComponentBase, IAsyncDisposable
{
	private const string ThemeModule = "./_content/Moka.Red.Core/moka-theme.js";
	private const string ProviderCascadeName = "Moka.Red.Core.Theming.MokaThemeProvider";

	private readonly string _scopeId = $"moka-theme-{Guid.NewGuid():N}";
	private readonly MokaThemeSource _themeSource = new();
	private string _rootClass = "moka-root";
	private string? _pageStyle;
	private bool _disposed;
	private DotNetObjectReference<MokaThemeProvider>? _dotNetRef;
	private IJSObjectReference? _module;
	private bool? _prefersDark;
	private MokaTheme? _previousTheme;
	private string? _themeStyle;
	private int _watchHandle;

	[Inject] private IJSRuntime JsRuntime { get; set; } = default!;

	/// <summary>
	///     The theme to apply. Defaults to <see cref="MokaTheme.Default" />.
	/// </summary>
	[Parameter]
	public MokaTheme Theme { get; set; } = MokaTheme.Default;

	/// <summary>Content to render inside the themed root.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     When true, automatically detects the OS color scheme preference
	///     and applies <see cref="DarkTheme" /> or <see cref="LightTheme" /> accordingly.
	///     Listens for changes in real time (e.g., when the user switches OS dark mode).
	///     Defaults to false.
	/// </summary>
	[Parameter]
	public bool AutoDetectColorScheme { get; set; }

	/// <summary>
	///     The dark theme to use when <see cref="AutoDetectColorScheme" /> detects dark mode.
	///     Defaults to <see cref="MokaTheme.Default" />.
	/// </summary>
	[Parameter]
	public MokaTheme DarkTheme { get; set; } = MokaTheme.Default;

	/// <summary>
	///     The light theme to use when <see cref="AutoDetectColorScheme" /> detects light mode.
	///     Defaults to <see cref="MokaTheme.DefaultLight" />.
	/// </summary>
	[Parameter]
	public MokaTheme LightTheme { get; set; } = MokaTheme.DefaultLight;

	/// <summary>
	///     A content security policy nonce for the style elements the provider writes. With it set, the
	///     tokens for the provider's own subtree also move from an inline <c>style</c> attribute into a
	///     style element, so a policy that does not allow <c>'unsafe-inline'</c> styles accepts the
	///     provider. Pass the same nonce your host puts in its Content-Security-Policy header.
	/// </summary>
	[Parameter]
	public string? Nonce { get; set; }

	/// <summary>
	///     Whether the outermost provider links <c>moka-fonts.css</c>, which holds Inter, Space Grotesk and
	///     JetBrains Mono. Defaults to true. The browser fetches a font file only when text on the page uses
	///     that font. Turn it off when the app brings its own copies or other fonts.
	/// </summary>
	[Parameter]
	public bool LoadFonts { get; set; } = true;

	// Set when this provider sits inside another one.
	[CascadingParameter(Name = ProviderCascadeName)]
	private MokaThemeProvider? ParentProvider { get; set; }

	private bool IsOutermost => ParentProvider is null;

	// The detected scheme is kept here rather than written into Theme: a value written into the
	// parameter was undone the next time the parent rendered.
	private MokaTheme ActiveTheme => AutoDetectColorScheme && _prefersDark is { } dark
		? dark ? DarkTheme : LightTheme
		: Theme;

	public async ValueTask DisposeAsync()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		if (_watchHandle != 0)
		{
			await _module.SafeInvokeVoidAsync("unwatchColorScheme", _watchHandle);
		}

		await _module.SafeDisposeAsync();
		_module = null;

		_dotNetRef?.Dispose();
		GC.SuppressFinalize(this);
	}

	/// <summary>Called from JS when the OS colour scheme changes.</summary>
	[JSInvokable]
	public Task OnColorSchemeChanged(bool dark) => InvokeAsync(() =>
	{
		_prefersDark = dark;
		ApplyTheme();
		StateHasChanged();
	});

	protected override void OnParametersSet() => ApplyTheme();

	private void ApplyTheme()
	{
		MokaTheme activeTheme = ActiveTheme;

		if (ReferenceEquals(activeTheme, _previousTheme))
		{
			return;
		}

		_previousTheme = activeTheme;
		_themeSource.Theme = activeTheme;
		_themeStyle = activeTheme.ToCssVariables();

		bool gate = activeTheme.Look == MokaLook.Gate;
		_rootClass = new CssBuilder("moka-root")
			.AddClass("moka-root--nested", !IsOutermost)
			.AddClass("moka-dark", activeTheme.IsDark)
			.AddClass("moka-gate", gate)
			.Build();

		// The root element fills the window, so this shows only where a page overscrolls, and the scheme is
		// what the window's own scrollbars follow. The Matrix look leaves both to the app, as it always has.
		_pageStyle = gate
			? $"background: var(--moka-color-background); color-scheme: {(activeTheme.IsDark ? "dark" : "light")};"
			: null;
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && AutoDetectColorScheme)
		{
			await DetectColorSchemeAsync();
		}
	}

	// A module, not eval: a content security policy without 'unsafe-eval' blocks eval. It also
	// watches the OS setting, which the one-off eval never did.
	private async Task DetectColorSchemeAsync()
	{
		try
		{
			_module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", ThemeModule);
			_prefersDark = await _module.InvokeAsync<bool>("prefersDarkColorScheme");
			_dotNetRef = DotNetObjectReference.Create(this);
			_watchHandle = await _module.InvokeAsync<int>("watchColorScheme", _dotNetRef);

			ApplyTheme();
			StateHasChanged();
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
		}
		catch (InvalidOperationException)
		{
			// JS interop not available
		}
	}
}
