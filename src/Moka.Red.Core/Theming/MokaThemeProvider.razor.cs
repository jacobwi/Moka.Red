using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Moka.Red.Core.Theming;

public partial class MokaThemeProvider : ComponentBase, IAsyncDisposable
{
	private string? _darkClass;
	private bool _disposed;
	private MokaTheme? _previousTheme;
	private string? _themeStyle;

	[Inject] private IJSRuntime JsRuntime { get; set; } = default!;

	/// <summary>
	///     The theme to apply. Defaults to <see cref="MokaTheme.Light" />.
	/// </summary>
	[Parameter]
	public MokaTheme Theme { get; set; } = MokaTheme.Light;

	/// <summary>Content to render inside the themed root.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     When true, automatically detects the OS color scheme preference
	///     and applies <see cref="MokaTheme.Dark" /> or <see cref="MokaTheme.Light" /> accordingly.
	///     Listens for changes in real time (e.g., when the user switches OS dark mode).
	///     Defaults to false.
	/// </summary>
	[Parameter]
	public bool AutoDetectColorScheme { get; set; }

	/// <summary>
	///     The dark theme to use when <see cref="AutoDetectColorScheme" /> detects dark mode.
	///     Defaults to <see cref="MokaTheme.Dark" />.
	/// </summary>
	[Parameter]
	public MokaTheme DarkTheme { get; set; } = MokaTheme.Dark;

	/// <summary>
	///     The light theme to use when <see cref="AutoDetectColorScheme" /> detects light mode.
	///     Defaults to <see cref="MokaTheme.Light" />.
	/// </summary>
	[Parameter]
	public MokaTheme LightTheme { get; set; } = MokaTheme.Light;

	public async ValueTask DisposeAsync()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		GC.SuppressFinalize(this);
		await ValueTask.CompletedTask;
	}

	protected override void OnParametersSet()
	{
		MokaTheme activeTheme = Theme;

		if (ReferenceEquals(activeTheme, _previousTheme))
		{
			return;
		}

		_previousTheme = activeTheme;
		_themeStyle = activeTheme.ToCssVariables();
		_darkClass = activeTheme.IsDark ? "moka-dark" : null;
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && AutoDetectColorScheme)
		{
			await DetectColorSchemeAsync();
		}
	}

	private async Task DetectColorSchemeAsync()
	{
		try
		{
			bool prefersDark = await JsRuntime.InvokeAsync<bool>(
				"eval", "window.matchMedia('(prefers-color-scheme: dark)').matches");

			Theme = prefersDark ? DarkTheme : LightTheme;
			_previousTheme = Theme;
			_themeStyle = Theme.ToCssVariables();
			_darkClass = Theme.IsDark ? "moka-dark" : null;
			StateHasChanged();
		}
		catch (JSDisconnectedException)
		{
			// Circuit disconnected
		}
		catch (InvalidOperationException)
		{
			// JS interop not available
		}
	}
}
