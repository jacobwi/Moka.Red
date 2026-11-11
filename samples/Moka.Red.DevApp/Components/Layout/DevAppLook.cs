using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.ThemeGen.Appearance;

namespace Moka.Red.DevApp.Components.Layout;

public enum DevAppMenu
{
	ByArea,
	TwoLevel,
	PatternsApart
}

public enum DevAppLookControl
{
	Drawer,
	Popover,
	Dock
}

public sealed record DevAppSettings
{
	public MokaAppearance Appearance { get; init; } = new();

	public DevAppMenu Menu { get; init; } = DevAppMenu.ByArea;

	public DevAppLookControl LookControl { get; init; } = DevAppLookControl.Drawer;
}

// The dev app's look and menu for one circuit, and the one place that keeps them. They live in a cookie rather than
// localStorage: App.razor reads it while prerendering, so a reload paints the chosen look at once instead of the
// default first.
internal sealed class DevAppLook(IJSRuntime js) : IAsyncDisposable
{
	public const string CookieName = "moka-devapp-look";

	private const string ModulePath = "./devapp-look.js";

	private static readonly JsonSerializerOptions StoredForm = new()
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Converters = { new JsonStringEnumConverter() }
	};

	private Task<IJSObjectReference>? _module;

	public static IReadOnlyList<MokaAppearancePalette> Palettes { get; } =
	[
		MokaAppearancePalette.Red,
		MokaAppearancePalette.Gate,
		MokaAppearancePalette.Crisp,
		MokaAppearancePalette.Matrix,
		MokaAppearancePalette.Original
	];

	public DevAppSettings Settings { get; private set; } = new();

	public MokaAppearanceThemes Themes { get; private set; } = new MokaAppearance().ToThemes(Palettes);

	public bool IsDefault => Settings == new DevAppSettings();

	public event Action? Changed;

	public void Restore(string? stored)
	{
		if (string.IsNullOrWhiteSpace(stored))
		{
			return;
		}

		try
		{
			if (JsonSerializer.Deserialize<DevAppSettings>(stored, StoredForm) is { Appearance: not null } settings)
			{
				Settings = settings;
				Themes = settings.Appearance.ToThemes(Palettes);
			}
		}
		catch (JsonException)
		{
		}
	}

	public Task SetAppearanceAsync(MokaAppearance appearance) => SetAsync(Settings with { Appearance = appearance });

	public Task SetMenuAsync(DevAppMenu menu) => SetAsync(Settings with { Menu = menu });

	public Task SetLookControlAsync(DevAppLookControl control) => SetAsync(Settings with { LookControl = control });

	public Task ResetAsync() => SetAsync(new DevAppSettings());

	public async Task SetAsync(DevAppSettings settings)
	{
		if (settings == Settings)
		{
			return;
		}

		if (settings.Appearance != Settings.Appearance)
		{
			Themes = settings.Appearance.ToThemes(Palettes);
		}

		Settings = settings;
		Changed?.Invoke();

		string stored = JsonSerializer.Serialize(settings, StoredForm);
		await InvokeAsync("keep", CookieName, stored);
	}

	public ValueTask FocusFirstAsync(ElementReference within) => InvokeAsync("focusFirst", within);

	public ValueTask FocusAsync(string id) => InvokeAsync("focusId", id);

	public async ValueTask DisposeAsync()
	{
		if (_module is { IsCompletedSuccessfully: true } loaded)
		{
			IJSObjectReference module = await loaded;
			await module.SafeDisposeAsync();
		}
	}

	private async ValueTask InvokeAsync(string identifier, params object?[] args)
	{
		try
		{
			_module ??= js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
			IJSObjectReference module = await _module;
			await module.SafeInvokeVoidAsync(identifier, args);
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
		}
	}
}
