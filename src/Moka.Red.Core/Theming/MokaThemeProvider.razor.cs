using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Core.Theming;

/// <summary>
///     Applies a <see cref="MokaTheme" /> to everything inside it: writes the theme's CSS custom properties on a
///     <c>.moka-root</c> element and cascades the theme to the components. The outermost provider also links the
///     library's stylesheet and fonts, so an app adds no CSS links of its own. A provider nested inside another themes
///     its own part of the page.
/// </summary>
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
	private MokaTheme? _previousParentTheme;
	private string? _rootStyle;
	private string? _themeStyle;
	private int _watchHandle;
	private bool _watchStarted;

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
	///     It can be turned on or off at any time. Defaults to false.
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
	///     that font. Turn it off when the app brings its own copies or other fonts. The flag font is apart from it
	///     (<see cref="LoadFlagFont" />).
	/// </summary>
	[Parameter]
	public bool LoadFonts { get; set; } = true;

	/// <summary>
	///     Whether the outermost provider links <c>moka-flags.css</c>, the Twemoji Country Flags font that draws
	///     <c>MokaFlag</c> and the country select's flags, whatever <see cref="LoadFonts" /> says. Defaults to true. Its
	///     <c>unicode-range</c> covers only flag characters, so the browser fetches the font the first time a flag is on
	///     screen and never for ordinary text. Turn it off when the app declares that font itself or wants the system's
	///     emoji flags (which Windows draws as two letters).
	/// </summary>
	[Parameter]
	public bool LoadFlagFont { get; set; } = true;

	// Set when this provider sits inside another one.
	[CascadingParameter(Name = ProviderCascadeName)]
	private MokaThemeProvider? ParentProvider { get; set; }

	private bool IsOutermost => ParentProvider is null;

	// The detected scheme is kept here rather than written into Theme: a value written into the
	// parameter was undone the next time the parent rendered.
	private MokaTheme ActiveTheme => AutoDetectColorScheme && _prefersDark is { } dark
		? dark ? DarkTheme : LightTheme
		: Theme;

	/// <summary>Stops watching the OS color scheme and releases the provider's JavaScript module.</summary>
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

	/// <inheritdoc />
	protected override void OnParametersSet() => ApplyTheme();

	// Themes compare by value, so a theme built in markup, a new but equal instance on every render, costs nothing.
	private void ApplyTheme()
	{
		MokaTheme activeTheme = ActiveTheme;
		MokaTheme? parentTheme = ParentProvider?.ActiveTheme;

		if (Equals(activeTheme, _previousTheme) && Equals(parentTheme, _previousParentTheme))
		{
			return;
		}

		_previousTheme = activeTheme;
		_previousParentTheme = parentTheme;
		_themeSource.Theme = activeTheme;

		// The outermost provider declares every token twice. On :root, for content outside its root such as the page canvas
		// and the reconnect dialog. On its root, because a page can hold several outermost providers side by side (live
		// previews, or panels drawn outside the app's provider), each writing :root, where the last would win for all of
		// them. A nested root declares only what differs from the provider around it and what reads that, since every
		// other token arrives inherited (see MokaThemeTokens.RootDeclarations).
		_rootStyle = IsOutermost ? activeTheme.ToCssVariables() : null;
		string declarations = parentTheme is null ? activeTheme.ToCssVariables() : MokaThemeTokens.RootDeclarations(activeTheme, parentTheme);
		_themeStyle = declarations.Length == 0 ? null : declarations;

		bool gate = activeTheme.Look == MokaLook.Gate;
		_rootClass = new CssBuilder("moka-root")
			.AddClass("moka-root--nested", !IsOutermost)
			.AddClass("moka-dark", activeTheme.IsDark)
			.AddClass("moka-gate", gate)
			.AddClass(PopupsClass(activeTheme), gate)
			.AddClass(ButtonsClass(activeTheme), gate && IsOutermost)
			.AddClass(DisabledButtonsClass(activeTheme),
				gate && IsOutermost && activeTheme.DisabledButtons != MokaDisabledButtonLook.Faded)
			.AddClass("moka-control-heights-matched", gate && activeTheme.ControlHeights == MokaControlHeights.Matched)
			.AddClass(TooltipsClass(activeTheme), gate && activeTheme.Tooltips != MokaTooltipLook.Standard)
			.AddClass("moka-tooltips-arrow", gate && activeTheme.TooltipArrows)
			.AddClass(TooltipMotionClass(activeTheme), gate && activeTheme.TooltipMotion != MokaTooltipMotion.Fade)
			.AddClass(TooltipSizeClass(activeTheme), gate && activeTheme.TooltipSize != MokaTooltipSize.Standard)
			.Build();

		// The root element fills the window, so this shows only where a page overscrolls, and the scheme is
		// what the window's own scrollbars follow. The Matrix look leaves both to the app, as it always has.
		_pageStyle = gate
			? $"background: var(--moka-color-background); color-scheme: {(activeTheme.IsDark ? "dark" : "light")};"
			: null;
	}

	// Classes rather than a cascaded value, because hundreds of buttons and every popup read them, and each cascading
	// parameter that can change costs a subscription (blazor/performance/rendering.md). The popup class only sets CSS
	// variables, which nest, so every provider writes it and the nearest wins. Button looks change rules per color,
	// which variables set on the root can't do, so their rules hang off the class and only the outermost provider
	// writes it: two providers' classes on one button would both match.
	private static string PopupsClass(MokaTheme theme) => $"moka-popups-{MokaEnumHelpers.ToCssClass(theme.Popups)}";

	private static string ButtonsClass(MokaTheme theme) => $"moka-buttons-{MokaEnumHelpers.ToCssClass(theme.Buttons)}";

	// Left off at Faded, so a root looks as it always has. Outermost only, for the button looks' reason.
	private static string DisabledButtonsClass(MokaTheme theme) =>
		$"moka-disabled-buttons-{MokaEnumHelpers.ToCssClass(theme.DisabledButtons)}";

	// The matched control heights class also only sets variables, so every provider writes it and the nearest wins.

	// Tooltip classes also only set variables, and a page holds hundreds of tooltips, so the same reasoning as popups.
	// They're left off at their defaults, so a root looks as it always has; moka.css resets the variables on every
	// new-design root, which is how an inner provider at the defaults undoes an outer one's choices.
	private static string TooltipsClass(MokaTheme theme) => $"moka-tooltips-{MokaEnumHelpers.ToCssClass(theme.Tooltips)}";

	private static string TooltipMotionClass(MokaTheme theme) => $"moka-tooltips-motion-{MokaEnumHelpers.ToCssClass(theme.TooltipMotion)}";

	private static string TooltipSizeClass(MokaTheme theme) => $"moka-tooltips-size-{MokaEnumHelpers.ToCssClass(theme.TooltipSize)}";

	/// <inheritdoc />
	/// <remarks>
	///     Starts watching the OS color scheme whenever <see cref="AutoDetectColorScheme" /> is on and nothing watches yet,
	///     not only after the first render, so turning it on later works; turning it off stops the watch.
	/// </remarks>
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (AutoDetectColorScheme && !_watchStarted)
		{
			_watchStarted = true;
			await DetectColorSchemeAsync();
		}
		else if (!AutoDetectColorScheme && _watchStarted)
		{
			_watchStarted = false;
			await StopWatchingAsync();
		}
	}

	// A module, not eval: a content security policy without 'unsafe-eval' blocks eval. It also
	// watches the OS setting, which the one-off eval never did.
	private async Task DetectColorSchemeAsync()
	{
		try
		{
			_module ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", ThemeModule);
			_prefersDark = await _module.InvokeAsync<bool>("prefersDarkColorScheme");
			_dotNetRef ??= DotNetObjectReference.Create(this);
			_watchHandle = await _module.InvokeAsync<int>("watchColorScheme", _dotNetRef);

			// Turned off while the module answered: the watch it just started goes again.
			if (!AutoDetectColorScheme)
			{
				_watchStarted = false;
				await StopWatchingAsync();
				return;
			}

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

	private async Task StopWatchingAsync()
	{
		if (_watchHandle != 0)
		{
			await _module.SafeInvokeVoidAsync("unwatchColorScheme", _watchHandle);
			_watchHandle = 0;
		}

		_prefersDark = null;
	}
}
