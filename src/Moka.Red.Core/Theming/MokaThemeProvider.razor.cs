using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
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
	private readonly RenderFragment _pageRules;
	private readonly RenderFragment _schemeRules;
	private readonly RenderFragment _scopedRules;
	private string _rootClass = "moka-root";
	private string? _pageStyle;
	private bool _disposed;
	private DotNetObjectReference<MokaThemeProvider>? _dotNetRef;
	private IJSObjectReference? _module;
	private bool? _prefersDark;
	private AppliedThemes? _applied;
	private string? _rootStyle;
	private string? _schemeStyle;
	private string? _themeStyle;
	private int _watchHandle;
	private bool _watchStarted;

	/// <summary>Creates a provider.</summary>
	public MokaThemeProvider()
	{
		_pageRules = WritePageRules;
		_schemeRules = WriteSchemeRules;
		_scopedRules = WriteScopedRules;
	}

	[Inject] private IJSRuntime JsRuntime { get; set; } = default!;

	/// <summary>
	///     The theme to apply. Defaults to <see cref="MokaTheme.Default" />. Not used while
	///     <see cref="AutoDetectColorScheme" /> is on.
	/// </summary>
	[Parameter]
	public MokaTheme Theme { get; set; } = MokaTheme.Default;

	/// <summary>Content to render inside the themed root.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     When true, the page follows the OS color scheme: <see cref="DarkTheme" /> when it asks for dark,
	///     <see cref="LightTheme" /> otherwise, and it switches when the setting changes. Both themes' tokens are written
	///     under <c>prefers-color-scheme</c> media queries, so the first paint of a prerendered or static page already
	///     shows the right one, and a static page follows the OS too. Once the browser has answered, the cascaded theme
	///     and the root's <c>moka-dark</c> class follow it; until then components receive <see cref="DarkTheme" />. It can
	///     be turned on or off at any time. Defaults to false.
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
	private MokaTheme ActiveTheme => AutoDetectColorScheme
		? _prefersDark ?? true ? DarkTheme : LightTheme
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

	// The theme this provider's root carries the tokens of while the OS asks for the given scheme. A provider that
	// doesn't follow the OS carries its Theme in both. Nested providers read their parent's to work out what to declare.
	private MokaTheme SchemeTheme(bool dark) => AutoDetectColorScheme
		? dark ? DarkTheme : LightTheme
		: Theme;

	// Themes compare by value, so a theme built in markup, a new but equal instance on every render, costs nothing.
	private void ApplyTheme()
	{
		var applied = new AppliedThemes(ActiveTheme, SchemeTheme(false), SchemeTheme(true),
			ParentProvider?.SchemeTheme(false), ParentProvider?.SchemeTheme(true), _prefersDark is not null);

		if (applied == _applied)
		{
			return;
		}

		AppliedThemes? previous = _applied;
		_applied = applied;
		_themeSource.Theme = applied.Active;

		if (applied.FollowsScheme)
		{
			ApplySchemeThemes(applied, previous is not { FollowsScheme: true } before || !applied.WritesSameTokens(before));
		}
		else
		{
			ApplyOneTheme(applied);
		}
	}

	private void ApplyOneTheme(AppliedThemes applied)
	{
		MokaTheme theme = applied.Active;

		// The outermost provider declares every token twice. On :root, for content outside its root such as the page canvas
		// and the reconnect dialog. On its root, because a page can hold several outermost providers side by side (live
		// previews, or panels drawn outside the app's provider), each writing :root, where the last would win for all of
		// them. A nested root declares only what differs from the provider around it and what reads that, since every
		// other token arrives inherited (see MokaThemeTokens.RootDeclarations). Inside a provider that follows the OS it
		// declares what differs from either of that provider's themes, which holds in both schemes.
		string declarations;
		if (applied.ParentLight is not { } parentLight || applied.ParentDark is not { } parentDark)
		{
			declarations = theme.ToCssVariables();
		}
		else if (Equals(parentLight, parentDark))
		{
			declarations = MokaThemeTokens.RootDeclarations(theme, parentLight);
		}
		else
		{
			declarations = MokaThemeTokens.RootDeclarations(theme, parentLight, parentDark);
		}

		_rootStyle = IsOutermost ? theme.ToCssVariables() : null;
		_themeStyle = declarations.Length == 0 ? null : declarations;
		_schemeStyle = null;
		_rootClass = RootClass(theme, theme.IsDark, false);

		// The root element fills the window, so this shows only where a page overscrolls, and the scheme is
		// what the window's own scrollbars follow. The Matrix look leaves both to the app, as it always has.
		_pageStyle = PageStyle(theme);
	}

	// Both themes go out under prefers-color-scheme, so the first paint shows the scheme the OS asks for before any
	// script runs: in prerendering, on a static page, and in the first interactive render, which then matches the
	// prerendered one. The browser's answer changes only the class and the cascaded theme, never the tokens.
	private void ApplySchemeThemes(AppliedThemes applied, bool tokensChanged)
	{
		_rootStyle = null;
		_themeStyle = null;
		_pageStyle = null;
		if (tokensChanged)
		{
			string light = SchemeBlock("light", applied.Light, applied.ParentLight);
			string dark = SchemeBlock("dark", applied.Dark, applied.ParentDark);

			// A nested root whose themes match the provider around it in both schemes declares nothing, as it would for one.
			_schemeStyle = light.Length + dark.Length == 0 ? null : string.Join(" ", light, dark).Trim();
		}

		// The class reads moka-dark only once the browser has said so. Until then moka-scheme-auto stands in for it under
		// the dark media query: every stylesheet rule on .moka-dark has a twin there.
		MokaTheme theme = applied.Active;
		_rootClass = RootClass(theme, applied.SchemeKnown && theme.IsDark, true);
	}

	private string SchemeBlock(string scheme, MokaTheme theme, MokaTheme? parent)
	{
		string root = $".moka-root[data-moka-theme=\"{_scopeId}\"]";
		if (parent is not null)
		{
			string declarations = MokaThemeTokens.RootDeclarations(theme, parent);
			return declarations.Length == 0 ? "" : $"@media (prefers-color-scheme: {scheme}) {{ {root} {{ {declarations}}} }}";
		}

		string page = PageStyle(theme) is { } pageStyle ? $":root {{ {pageStyle} }} " : "";
		return $"@media (prefers-color-scheme: {scheme}) {{ {page}:root, {root} {{ {theme.ToCssVariables()}}} }}";
	}

	// The outermost provider's page-wide rules: :root's tokens for one theme, or both themes' rules when it follows the OS.
	private void WritePageRules(RenderTreeBuilder builder)
	{
		if (_schemeStyle is not null)
		{
			AddRules(builder, 5, _schemeStyle);
			return;
		}

		AddRules(builder, 0, ":root { ");
		AddRules(builder, 1, _rootStyle);
		AddRules(builder, 2, " ");
		AddRules(builder, 3, _pageStyle);
		AddRules(builder, 4, " }");
	}

	private void WriteSchemeRules(RenderTreeBuilder builder) => AddRules(builder, 0, _schemeStyle);

	// A nested root's tokens, or with a nonce any root's, in a style element scoped to it.
	private void WriteScopedRules(RenderTreeBuilder builder)
	{
		AddRules(builder, 0, ".moka-root[data-moka-theme=\"");
		AddRules(builder, 1, _scopeId);
		AddRules(builder, 2, "\"] { ");
		AddRules(builder, 3, _themeStyle);
		AddRules(builder, 4, " }");
	}

	// The static renderer HTML-encodes text, quotes and plus signs included, and a style element never decodes it, so
	// there the rules go out as markup. An interactive render sets text, which reaches the page as written, while
	// markup would go through innerHTML, which a Trusted Types policy refuses. A renderer is one or the other for good,
	// so both share a sequence number.
	private void AddRules(RenderTreeBuilder builder, int sequence, string? css)
	{
		if (RendererInfo.IsInteractive)
		{
			builder.AddContent(sequence, css);
		}
		else
		{
			builder.AddMarkupContent(sequence, css);
		}
	}

	private static string? PageStyle(MokaTheme theme) => theme.Look == MokaLook.Gate
		? $"background: var(--moka-color-background); color-scheme: {(theme.IsDark ? "dark" : "light")};"
		: null;

	private string RootClass(MokaTheme theme, bool dark, bool followsScheme)
	{
		bool gate = theme.Look == MokaLook.Gate;
		return new CssBuilder("moka-root")
			.AddClass("moka-root--nested", !IsOutermost)
			.AddClass("moka-dark", dark)
			.AddClass("moka-scheme-auto", followsScheme)
			.AddClass("moka-gate", gate)
			.AddClass(PopupsClass(theme), gate)
			.AddClass(ButtonsClass(theme), gate && IsOutermost)
			.AddClass(DisabledButtonsClass(theme),
				gate && IsOutermost && theme.DisabledButtons != MokaDisabledButtonLook.Faded)
			.AddClass(PressedButtonsClass(theme), gate && IsOutermost && theme.PressedButtons != MokaPressedButtonLook.Tint)
			.AddClass(PressedButtonFillClass(theme),
				gate && IsOutermost && theme.PressedButtonFill != MokaPressedButtonFill.Tinted)
			.AddClass("moka-control-heights-matched", gate && theme.ControlHeights == MokaControlHeights.Matched)
			.AddClass(TooltipsClass(theme), gate && theme.Tooltips != MokaTooltipLook.Standard)
			.AddClass("moka-tooltips-arrow", gate && theme.TooltipArrows)
			.AddClass(TooltipMotionClass(theme), gate && theme.TooltipMotion != MokaTooltipMotion.Fade)
			.AddClass(TooltipSizeClass(theme), gate && theme.TooltipSize != MokaTooltipSize.Standard)
			.Build();
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

	// Pressed looks are rules per variant and color too, so the same: left off at their defaults, outermost only.
	private static string PressedButtonsClass(MokaTheme theme) =>
		$"moka-pressed-buttons-{MokaEnumHelpers.ToCssClass(theme.PressedButtons)}";

	private static string PressedButtonFillClass(MokaTheme theme) =>
		$"moka-pressed-button-fill-{MokaEnumHelpers.ToCssClass(theme.PressedButtonFill)}";

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

	// What the root's style and class were last worked out from. The provider follows the OS while its two themes differ.
	private readonly record struct AppliedThemes(
		MokaTheme Active,
		MokaTheme Light,
		MokaTheme Dark,
		MokaTheme? ParentLight,
		MokaTheme? ParentDark,
		bool SchemeKnown)
	{
		public bool FollowsScheme => !Equals(Light, Dark);

		public bool WritesSameTokens(AppliedThemes other) => Equals(Light, other.Light) && Equals(Dark, other.Dark)
		                                                     && Equals(ParentLight, other.ParentLight)
		                                                     && Equals(ParentDark, other.ParentDark);
	}
}
