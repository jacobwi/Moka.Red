using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moka.Red.Core.Announcer;
using Moka.Red.Core.Base;
using Moka.Red.Core.Colors;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.Navigation.Tabs;
using Moka.Red.ThemeGen.Appearance;
using Moka.Red.ThemeGen.ImportExport;
using Moka.Red.ThemeGen.Studio;

namespace Moka.Red.ThemeGen;

/// <summary>
///     A theme generator laid out as a studio: controls on the left (where to start from, seed colors, contrast and every
///     style choice), a sample app made of the library's own components in the middle, in the dark scheme, the light one
///     or both, and on the right the theme's contrast report, every token with a pin, and its C#, JSON and CSS. Bind
///     <see cref="Themes" /> for both schemes, or <see cref="Theme" /> for one.
/// </summary>
/// <remarks>
///     A slider's values are applied at most once a frame, and the color pickers report when a drag lets go, so a drag
///     never rebuilds the theme and redraws the previews on every pointer move. The previews, the style rows and the
///     inspector redraw only when what they show changes, and the contrast report and the exports are worked out only
///     while their tab shows. With <see cref="ShowChanges" /> on, after a change from the controls has drawn (a slider
///     once it has stood still), the elements in the previews whose look changed are marked for a moment, and a line
///     under the preview heading says what changed and how many elements it touched, which screen readers hear through
///     <see cref="IMokaAnnouncer" /> when one is registered. A new start or an imported theme changes nearly everything,
///     so those aren't marked. The previews' look is read in the browser just before the change and again after it, so
///     nothing runs between changes, and nothing at all while it's off. It inherits <see cref="ComponentBase" /> like the rest of ThemeGen, because the
///     library's component base already has a <c>Theme</c> that reads the cascaded theme.
/// </remarks>
public partial class MokaThemeStudio : IAsyncDisposable
{
	private const string CurrentStartId = "moka-theme-studio-current";
	private const MokaAppearanceRows StyleRows = MokaAppearanceRows.Style | MokaAppearanceRows.ComponentStyle | MokaAppearanceRows.Components | MokaAppearanceRows.ChartsGroup;
	private const double MaxNeutralChroma = 0.1;
	private const string ModulePath = "./_content/Moka.Red.ThemeGen/Studio/MokaThemeStudio.razor.js";
	private const MokaAppearanceRows ComponentStyleRows = MokaAppearanceRows.Toasts | MokaAppearanceRows.Notifications | MokaAppearanceRows.Popups
		| MokaAppearanceRows.Lists | MokaAppearanceRows.Dialogs | MokaAppearanceRows.Buttons;

	private static readonly TimeSpan Frame = TimeSpan.FromMilliseconds(16);

	// How long a slider has to stand still before its change counts as made and the previews are compared.
	private static readonly TimeSpan Settle = TimeSpan.FromMilliseconds(400);
	private static readonly IReadOnlyList<MokaAccent> NoAccents = [];
	private static readonly IReadOnlyDictionary<string, string> NoPins = new Dictionary<string, string>(StringComparer.Ordinal);

	private readonly string _id = $"moka-theme-studio-{Guid.NewGuid():N}";
	private readonly List<Action> _queued = [];
	private IMokaAnnouncer? _announcer;
	private string? _changeLine;
	private StudioChange? _drawn;
	private IJSObjectReference? _module;
	private ITimer? _settle;
	private int _settleNumber;
	private bool? _lastShowChanges;
	private Task? _moduleLoad;
	private bool _showChanges;
	private StudioChange? _settling;
	private bool _snapshotHeld;
	private ElementReference _well;
	private double? _chromaInput;
	private IReadOnlyDictionary<string, string> _darkPins = NoPins;
	private string? _editError;
	private string? _editing;
	private string? _editValue;
	private int _editVersion;
	private MokaThemeExportFormat _format;
	private ITimer? _frame;
	private int _frameNumber;
	private bool _disposed;
	private bool _highContrast;
	private bool _inspectDark = true;
	private IReadOnlyList<MokaAppearancePalette>? _lastStarts;
	private MokaTheme? _lastTheme;
	private MokaThemePair? _lastThemes;
	private MokaThemeStudioView? _lastView;
	private IReadOnlyDictionary<string, string> _lightPins = NoPins;
	private MokaThemePair _pair = MokaThemePair.From(MokaTheme.Default);
	private (MokaTheme Theme, MokaContrastReport Report)? _report;
	private string? _search;
	private string _searchText = "";
	private MokaThemeSeed? _seed;
	private IReadOnlyList<MokaAppearancePalette> _shownStarts = MokaAppearancePalette.Presets;
	private bool _singleDark = true;
	private MokaAppearancePalette _start = MokaAppearancePalette.Red;
	private bool _started;
	private MokaAppearance _style = new() { Palette = MokaAppearancePalette.Red.Id };
	private string _tab = nameof(Tab.Contrast);
	private List<MokaTabItem> _tabs = [];
	private MokaThemeStudioText? _tabsText;
	private MokaThemeStudioView _view;

	[Inject]
	private IServiceProvider Services { get; set; } = default!;

	[Inject]
	private IJSRuntime JS { get; set; } = default!;

	/// <summary>
	///     One theme to edit. Two-way bindable. <see cref="ThemeChanged" /> carries the design in this theme's scheme
	///     whatever the previews show. Ignored while <see cref="Themes" /> is set. Neither set starts from
	///     <see cref="MokaTheme.Default" />.
	/// </summary>
	[Parameter]
	public MokaTheme? Theme { get; set; }

	/// <summary>Raised with the theme after every change, in the scheme <see cref="Theme" /> was in (dark when it wasn't set).</summary>
	[Parameter]
	public EventCallback<MokaTheme> ThemeChanged { get; set; }

	/// <summary>
	///     The design in both schemes, as <see cref="MokaThemeProvider" /> takes them for <c>DarkTheme</c> and
	///     <c>LightTheme</c>. Two-way bindable, and wins over <see cref="Theme" />. One value rather than two parameters, so
	///     each change reaches the app once and the two schemes never disagree in between.
	/// </summary>
	[Parameter]
	public MokaThemePair? Themes { get; set; }

	/// <summary>Raised with both schemes after every change.</summary>
	[Parameter]
	public EventCallback<MokaThemePair> ThemesChanged { get; set; }

	/// <summary>
	///     Whether a change from the controls is shown in the previews: the elements whose look it changed are marked for a
	///     moment, and a line under the preview heading names the change and counts them. Two-way bindable; the person
	///     turns it on with the switch at the preview heading. Default false, and while it's off nothing is loaded, read or
	///     said for it.
	/// </summary>
	[Parameter]
	public bool ShowChanges { get; set; }

	/// <summary>Raised when the person turns <see cref="ShowChanges" /> on or off.</summary>
	[Parameter]
	public EventCallback<bool> ShowChangesChanged { get; set; }

	/// <summary>Which schemes the middle shows. Two-way bindable. Default <see cref="MokaThemeStudioView.Both" />.</summary>
	[Parameter]
	public MokaThemeStudioView View { get; set; }

	/// <summary>Raised when the person picks other schemes to show.</summary>
	[Parameter]
	public EventCallback<MokaThemeStudioView> ViewChanged { get; set; }

	/// <summary>
	///     The designs to start from. Default is <see cref="MokaAppearancePalette.Presets" />: Red, Gate, Crisp, Matrix,
	///     Ember and the original design. A theme passed in that matches none of them shows first, as the current theme.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaAppearancePalette> Starts { get; set; } = MokaAppearancePalette.Presets;

	/// <summary>Every word the studio writes, its sample app, style rows and export tab included.</summary>
	[Parameter]
	public MokaThemeStudioText Text { get; set; } = new();

	/// <summary>A content security policy nonce for the previews' theme providers. Pass the one the app gives its own.</summary>
	[Parameter]
	public string? Nonce { get; set; }

	/// <summary>Classes added to the studio's root element.</summary>
	[Parameter]
	public string? Class { get; set; }

	/// <summary>Inline style on the studio's root element.</summary>
	[Parameter]
	public string? Style { get; set; }

	/// <summary>The id of the studio's root element.</summary>
	[Parameter]
	public string? Id { get; set; }

	/// <summary>Any other attributes, put on the studio's root element.</summary>
	[Parameter(CaptureUnmatchedValues = true)]
	public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

	private string CssClass => new CssBuilder("moka-theme-studio")
		.AddClass("moka-fill-width")
		.AddClass(Class)
		.Build();

	private string ControlsHeadingId => $"{_id}-controls";

	private string StageHeadingId => $"{_id}-preview";

	private string InspectorHeadingId => $"{_id}-inspector";

	private string ChangeLineClass => new CssBuilder("moka-theme-studio__change")
		.AddClass("moka-theme-studio__change--said", _changeLine is not null)
		.Build();

	private bool InspectDark => _view switch
	{
		MokaThemeStudioView.Dark => true,
		MokaThemeStudioView.Light => false,
		_ => _inspectDark
	};

	private MokaTheme Inspected => _pair.For(InspectDark);

	private bool TakesSeeds => _start.Dark.Look == MokaLook.Gate && _start.Light.Look == MokaLook.Gate;

	private bool SeedsAreOwn => _seed is not null || _start.Dark.Seed is not null;

	private MokaThemeSeed CurrentSeed => _seed ?? _start.Dark.Seed ?? new MokaThemeSeed
	{
		Accent = Hex(_start.Dark.Palette.Primary) ?? MokaAccent.Red.Dark.Color,
		Neutral = Hex(_start.Dark.WithHighContrast(false).Palette.Surface)
	};

	private MokaSeedContrast CurrentContrast => SeedsAreOwn ? CurrentSeed.Contrast : MokaSeedContrast.Standard;

	private double NeutralStrength => _chromaInput ?? (SeedsAreOwn ? CurrentSeed.NeutralChroma : null) ?? NeutralsOwnChroma;

	private bool NeutralStrengthIsOwn => _chromaInput is null && (!SeedsAreOwn || CurrentSeed.NeutralChroma is null);

	private double NeutralsOwnChroma =>
		MokaCssColor.TryParse(CurrentSeed.Neutral, out MokaSrgb neutral) ? Math.Min(neutral.ToOklch().C, MaxNeutralChroma) : 0;

	private MokaAppearance StyleDefaults => new() { Palette = _start.Id };

	// The start list is the panel's palette row, so its label is the studio's heading rather than a second one.
	private MokaAppearanceText StartWords => Text.Appearance with { Palette = Text.StartHeading };

	private MokaContrastReport Report
	{
		get
		{
			MokaTheme theme = Inspected;
			if (_report is not { } held || !ReferenceEquals(held.Theme, theme))
			{
				_report = (theme, theme.CheckContrast());
			}

			return _report.Value.Report;
		}
	}

	private TimeProvider Clock => Services.GetService<TimeProvider>() ?? TimeProvider.System;

	/// <summary>Stops the frame and settle timers, so a slider value still waiting is dropped, and lets go of the script.</summary>
	/// <returns>A task that completes when the timers and the script are disposed.</returns>
	public async ValueTask DisposeAsync()
	{
		_disposed = true;
		if (_frame is not null)
		{
			await _frame.DisposeAsync();
			_frame = null;
		}

		if (_settle is not null)
		{
			await _settle.DisposeAsync();
			_settle = null;
		}

		await _module.SafeDisposeAsync();
		_module = null;
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		base.OnInitialized();
		_announcer = Services.GetService<IMokaAnnouncer>();
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (_showChanges)
		{
			await LoadModuleAsync();
		}

		if (_drawn is { } change)
		{
			_drawn = null;
			await CompareAsync(change);
		}
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (!ReferenceEquals(_lastStarts, Starts))
		{
			_lastStarts = Starts;
			_shownStarts = _start.Id == CurrentStartId ? [_start, .. Starts] : Starts;
		}

		if (Themes is { } pair)
		{
			if (!pair.Equals(_lastThemes))
			{
				_lastThemes = pair;
				Open(pair);
			}
		}
		else if (Theme is { } theme)
		{
			if (!theme.Equals(_lastTheme))
			{
				_lastTheme = theme;
				_singleDark = theme.IsDark;
				_inspectDark = theme.IsDark;
				Open(MokaThemePair.From(theme));
			}
		}
		else if (!_started)
		{
			Open(_pair);
		}

		if (_lastView != View)
		{
			_lastView = View;
			_view = View;
		}

		if (_lastShowChanges != ShowChanges)
		{
			_lastShowChanges = ShowChanges;
			ShowingChanges(ShowChanges);
		}

		if (!ReferenceEquals(_tabsText, Text))
		{
			_tabsText = Text;
			_tabs =
			[
				new MokaTabItem(nameof(Tab.Contrast), Text.ContrastTab),
				new MokaTabItem(nameof(Tab.Tokens), Text.TokensTab),
				new MokaTabItem(nameof(Tab.Export), Text.ExportTab)
			];
		}
	}

	// A theme from outside replaces everything: its own colors become the start, its pins stay pinned, and the style rows
	// show what it has.
	private void Open(MokaThemePair pair)
	{
		_started = true;
		_pair = pair;
		_darkPins = Copy(pair.Dark.Pins);
		_lightPins = Copy(pair.Light.Pins);
		_highContrast = pair.Dark.HighContrast;

		MokaTheme dark = Unpinned(pair.Dark).WithHighContrast(false);
		MokaTheme light = Unpinned(pair.Light).WithHighContrast(false);
		MokaAppearancePalette? match = Starts.FirstOrDefault(start => start.Dark.Equals(dark) && start.Light.Equals(light));
		_start = match ?? new MokaAppearancePalette(CurrentStartId, Text.CurrentStart, dark, light) { Hint = Text.CurrentStartHint };
		_shownStarts = match is null ? [_start, .. Starts] : Starts;
		_seed = null;
		_chromaInput = null;
		_style = new MokaAppearance { Palette = _start.Id };
		_queued.Clear();
		StopFrame();
	}

	private MokaThemePair Build()
	{
		IReadOnlyList<MokaAppearancePalette> palettes = [new MokaAppearancePalette(_start.Id, _start.Name, Seeded(_start.Dark), Seeded(_start.Light))];
		return new MokaThemePair(
			Finish(_style.ToTheme(true, palettes, NoAccents), _darkPins),
			Finish(_style.ToTheme(false, palettes, NoAccents), _lightPins));
	}

	private MokaTheme Seeded(MokaTheme theme) => _seed is { } seed && theme.Look == MokaLook.Gate ? theme.WithSeed(seed) : theme;

	private MokaTheme Finish(MokaTheme theme, IReadOnlyDictionary<string, string> pins)
	{
		MokaTheme finished = theme.WithHighContrast(_highContrast);
		foreach ((string name, string value) in pins)
		{
			finished = finished.WithToken(name, value);
		}

		return finished;
	}

	// Changes made by clicks, picks and a color picker letting go: the previews' look is read, whatever a drag queued is
	// applied, then the change, then the theme is built once. The render that draws it compares the previews. A change
	// with no name replaces the whole theme, so it isn't compared, and what a drag waited to compare goes with it.
	private async Task ApplyAsync(StudioChange? name, Action change)
	{
		StopFrame();
		StopSettle();
		if (name is null)
		{
			_snapshotHeld = false;
			_changeLine = null;
		}
		else
		{
			await SnapshotAsync();
		}

		RunQueued();
		change();
		_drawn = _showChanges ? name : null;
		await RaiseAsync(Build());
	}

	// A slider's values arrive on every step of a drag. They are queued without a render and applied together once a frame.
	private void Queue(Action change)
	{
		_queued.Add(change);
		if (_frame is not null)
		{
			return;
		}

		int number = ++_frameNumber;
		_frame = Clock.CreateTimer(_ => _ = FromTimerAsync(() => FlushAsync(number)), null, Frame, Timeout.InfiniteTimeSpan);
	}

	// A timer fires on a pool thread. Starting inside this method keeps anything thrown in the discarded task, from where
	// it reaches the error boundary rather than the thread pool.
	private async Task FromTimerAsync(Func<Task> work)
	{
		try
		{
			await InvokeAsync(work);
		}
		catch (Exception ex) when (!_disposed)
		{
			await DispatchExceptionAsync(ex);
		}
	}

	// Values that arrive while the look is being read start a frame of their own, so a frame clears itself even when the
	// values it was started for have already gone out.
	private async Task FlushAsync(int number)
	{
		if (_disposed || number != _frameNumber)
		{
			return;
		}

		StopFrame();
		if (_queued.Count == 0)
		{
			return;
		}

		await SnapshotAsync();
		RunQueued();
		await RaiseAsync(Build());
		StateHasChanged();
		if (_showChanges)
		{
			StartSettle(new StudioChange(Text.NeutralStrength, null));
		}
	}

	private void StartSettle(StudioChange change)
	{
		StopSettle();
		_settling = change;
		int number = ++_settleNumber;
		_settle = Clock.CreateTimer(_ => _ = FromTimerAsync(() => SettledAsync(number)), null, Settle, Timeout.InfiniteTimeSpan);
	}

	private Task SettledAsync(int number)
	{
		if (_disposed || number != _settleNumber || _settling is not { } change)
		{
			return Task.CompletedTask;
		}

		StopSettle();
		return CompareAsync(change);
	}

	private void StopSettle()
	{
		_settleNumber++;
		_settling = null;
		_settle?.Dispose();
		_settle = null;
	}

	// Loaded the first time changes are shown, once. A renderer with no script behind it, such as a static preview, answers
	// the import with null, and then no change is compared.
	private Task LoadModuleAsync() => _moduleLoad ??= ImportModuleAsync();

	private async Task ImportModuleAsync()
	{
		try
		{
			_module = await JS.InvokeAsync<IJSObjectReference?>("import", ModulePath);
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
			return;
		}

		if (_disposed)
		{
			await _module.SafeDisposeAsync();
			_module = null;
		}
	}

	// Once per change: a second change before the first has drawn is measured against the same look, so it counts both.
	private async Task SnapshotAsync()
	{
		if (!_showChanges || _snapshotHeld || _module is null)
		{
			return;
		}

		_snapshotHeld = await _module.SafeInvokeVoidAsync("snapshot", _well);
	}

	private async Task CompareAsync(StudioChange change)
	{
		if (!_showChanges || !_snapshotHeld || _module is null)
		{
			return;
		}

		_snapshotHeld = false;
		int changed;
		try
		{
			changed = await _module.InvokeAsync<int>("compare", _well);
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
			return;
		}

		if (changed < 0 || _disposed || !_showChanges)
		{
			return;
		}

		_changeLine = changed == 0 && change.Row is { } row && Text.ShownLater(change.What, row) is { } later
			? later
			: Text.ChangeSummary(change.What, changed);
		_announcer?.Announce(_changeLine);
		StateHasChanged();
	}

	// One row by its name. Several of the six the component style row sets at once are that row. Anything else that moves
	// several rows at once is the panel's way back to the defaults.
	private StudioChange StyleChange(MokaAppearance before, MokaAppearance after)
	{
		MokaAppearanceRows rows = after.StyleRowsChangedFrom(before);
		if (rows == MokaAppearanceRows.None)
		{
			return new StudioChange(Text.StyleHeading, null);
		}

		if (((long)rows & ((long)rows - 1)) == 0)
		{
			return new StudioChange(Text.Appearance.LabelOf(rows), rows);
		}

		return (rows & ~ComponentStyleRows) == 0
			? new StudioChange(Text.Appearance.ComponentStyle, MokaAppearanceRows.ComponentStyle)
			: new StudioChange(Text.Appearance.Reset, null);
	}

	private void RunQueued()
	{
		foreach (Action change in _queued)
		{
			change();
		}

		_queued.Clear();
	}

	private void StopFrame()
	{
		_frameNumber++;
		_frame?.Dispose();
		_frame = null;
	}

	private async Task RaiseAsync(MokaThemePair pair)
	{
		_pair = pair;
		_lastThemes = pair;
		_lastTheme = pair.For(_singleDark);
		await ThemesChanged.InvokeAsync(pair);
		await ThemeChanged.InvokeAsync(_lastTheme);
	}

	private Task PickStartAsync(MokaAppearance value)
	{
		MokaAppearancePalette? start = _shownStarts.FirstOrDefault(candidate => candidate.Id == value.Palette);
		if (start is null || start.Id == _start.Id)
		{
			return Task.CompletedTask;
		}

		return ApplyAsync(null, () =>
		{
			_start = start;
			_seed = null;
			_chromaInput = null;
			_style = _style with { Palette = start.Id };
		});
	}

	private Task SetStyleAsync(MokaAppearance value)
	{
		MokaAppearance style = value with { Palette = _start.Id, Mode = MokaColorMode.Dark };
		return ApplyAsync(StyleChange(_style, style), () => _style = style);
	}

	private Task SetSeedAsync(SeedSlot slot, string? value)
	{
		if (!MokaCssColor.TryParse(value, out MokaSrgb color) || !color.IsOpaque)
		{
			return Task.CompletedTask;
		}

		string hex = color.ToHex(uppercase: true);
		return ApplyAsync(new StudioChange(SeedLabel(slot), null), () =>
		{
			_seed = WithSlot(CurrentSeed, slot, hex);
			_chromaInput = null;
		});
	}

	private Task ResetSeedAsync(SeedSlot slot) => ApplyAsync(new StudioChange(SeedLabel(slot), null), () => _seed = WithSlot(CurrentSeed, slot, null));

	private Task SetContrastAsync(MokaSeedContrast contrast) =>
		ApplyAsync(new StudioChange(Text.Contrast, null), () => _seed = CurrentSeed with { Contrast = contrast });

	private Task SetHighContrastAsync(bool value) => ApplyAsync(new StudioChange(Text.HighContrast, null), () => _highContrast = value);

	private void QueueNeutralStrength(double value)
	{
		_chromaInput = value;
		Queue(() => _seed = CurrentSeed with { NeutralChroma = Math.Clamp(value, 0, MaxNeutralChroma) });
	}

	private Task UseNeutralsOwnStrengthAsync() => ApplyAsync(new StudioChange(Text.NeutralStrength, null), () =>
	{
		_seed = CurrentSeed with { NeutralChroma = null };
		_chromaInput = null;
	});

	private async Task SetShowChangesAsync(bool value)
	{
		ShowingChanges(value);
		if (value)
		{
			await LoadModuleAsync();
		}

		await ShowChangesChanged.InvokeAsync(value);
	}

	// Turned off, whatever was waiting to be compared is dropped and the line goes, so nothing more runs for it.
	private void ShowingChanges(bool on)
	{
		_showChanges = on;
		if (on)
		{
			return;
		}

		StopSettle();
		_drawn = null;
		_snapshotHeld = false;
		_changeLine = null;
	}

	private Task SetViewAsync(MokaThemeStudioView view)
	{
		_view = view;
		return ViewChanged.InvokeAsync(view);
	}

	private void SetInspected(MokaThemeStudioView view) => _inspectDark = view == MokaThemeStudioView.Dark;

	private void SetSearchText(string value) => _searchText = value;

	private void SetSearch(string value) => _search = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

	private Task ImportAsync(MokaTheme theme) => ApplyAsync(null, () =>
	{
		Open(MokaThemePair.From(theme));
		_inspectDark = theme.IsDark;
	});

	private void StartPin(MokaToken token)
	{
		_editing = token.Name;
		_editValue = Inspected.ToTokenMap().GetValueOrDefault(token.Name);
		_editError = null;
		_editVersion++;
	}

	private void CancelPin()
	{
		_editing = null;
		_editError = null;
		_editVersion++;
	}

	private void SetEditValue(string? value)
	{
		_editValue = value;
		_editVersion++;
	}

	private Task ApplyPinAsync(MokaToken token)
	{
		string value = _editValue?.Trim() ?? "";
		if (!token.Accepts(value))
		{
			_editError = Text.PinInvalid;
			_editVersion++;
			return Task.CompletedTask;
		}

		_editing = null;
		_editError = null;
		_editVersion++;
		return ApplyAsync(new StudioChange(token.Name, null), () => ChangePins(token, pins => pins[token.Name] = value));
	}

	private Task UnpinAsync(MokaToken token) => ApplyAsync(new StudioChange(token.Name, null), () => ChangePins(token, pins => pins.Remove(token.Name)));

	private Task UnpinAllAsync() => ApplyAsync(new StudioChange(Text.UnpinAll, null), () =>
	{
		_darkPins = NoPins;
		_lightPins = NoPins;
	});

	// A color reads differently on dark and light surfaces, so a color pin holds in the scheme being inspected. Sizes,
	// fonts and timings read the same in both, so their pins hold in both.
	private void ChangePins(MokaToken token, Action<Dictionary<string, string>> change)
	{
		bool colorOnly = token.Kind == MokaTokenKind.Color;
		if (!colorOnly || InspectDark)
		{
			Dictionary<string, string> dark = new(_darkPins, StringComparer.Ordinal);
			change(dark);
			_darkPins = dark;
		}

		if (!colorOnly || !InspectDark)
		{
			Dictionary<string, string> light = new(_lightPins, StringComparer.Ordinal);
			change(light);
			_lightPins = light;
		}
	}

	private List<IGrouping<MokaTokenGroup, MokaToken>> ShownTokenGroups => [.. MokaTokens.All
		.Where(token => _search is null || token.Name.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
			token.Description.Contains(_search, StringComparison.OrdinalIgnoreCase))
		.GroupBy(token => token.Group)];

	private string? TokenValue(MokaToken token) => Inspected.ToTokenMap().GetValueOrDefault(token.Name);

	private static string PinEditorKey(MokaToken token) => $"{token.Name} pin";

	private int FailingCount => Report.Checks.Count(check => !check.Passes);

	private static string CheckClass(MokaContrastCheck check) => new CssBuilder("moka-theme-studio__check")
		.AddClass("moka-theme-studio__check--fail", !check.Passes)
		.Build();

	private string Verdict(MokaContrastCheck check) => !check.Passes
		? Text.Fails
		: check.Requirement == MokaContrastRequirement.Informational ? Text.Informational : Text.Passes;

	private static MokaColor VerdictColor(MokaContrastCheck check) => !check.Passes
		? MokaColor.Error
		: check.Requirement == MokaContrastRequirement.Informational ? MokaColor.Surface : MokaColor.Success;

	private IEnumerable<MokaContrastCheck> OrderedChecks() => Report.Checks
		.OrderBy(check => check.Passes)
		.ThenBy(check => check.Requirement == MokaContrastRequirement.Informational)
		.ThenBy(check => check.Ratio);

	private string? SeedValue(SeedSlot slot)
	{
		MokaThemeSeed seed = CurrentSeed;
		MokaPalette palette = Inspected.Palette;
		string? own = slot switch
		{
			SeedSlot.Accent => seed.Accent,
			SeedSlot.Neutral => seed.Neutral,
			SeedSlot.Success => seed.Success,
			SeedSlot.Warning => seed.Warning,
			SeedSlot.Error => seed.Error,
			_ => seed.Info
		};

		if (own is not null && (SeedsAreOwn || slot is SeedSlot.Accent or SeedSlot.Neutral))
		{
			return Hex(own);
		}

		return Hex(slot switch
		{
			SeedSlot.Accent => palette.Primary,
			SeedSlot.Neutral => palette.Surface,
			SeedSlot.Success => palette.Success,
			SeedSlot.Warning => palette.Warning,
			SeedSlot.Error => palette.Error,
			_ => palette.Info
		});
	}

	private bool SeedIsOwn(SeedSlot slot) => SeedsAreOwn && slot switch
	{
		SeedSlot.Accent or SeedSlot.Neutral => true,
		SeedSlot.Success => CurrentSeed.Success is not null,
		SeedSlot.Warning => CurrentSeed.Warning is not null,
		SeedSlot.Error => CurrentSeed.Error is not null,
		_ => CurrentSeed.Info is not null
	};

	private string SeedLabel(SeedSlot slot) => slot switch
	{
		SeedSlot.Accent => Text.Accent,
		SeedSlot.Neutral => Text.Neutral,
		SeedSlot.Success => Text.Success,
		SeedSlot.Warning => Text.Warning,
		SeedSlot.Error => Text.Error,
		_ => Text.Info
	};

	private static MokaThemeSeed WithSlot(MokaThemeSeed seed, SeedSlot slot, string? color) => slot switch
	{
		SeedSlot.Accent => seed with { Accent = color ?? seed.Accent },
		SeedSlot.Neutral => seed with { Neutral = color },
		SeedSlot.Success => seed with { Success = color },
		SeedSlot.Warning => seed with { Warning = color },
		SeedSlot.Error => seed with { Error = color },
		_ => seed with { Info = color }
	};

	private static string? Hex(string? color) =>
		MokaCssColor.TryParse(color, out MokaSrgb parsed) ? parsed.Opaque.ToHex(uppercase: true) : null;

	private static MokaTheme Unpinned(MokaTheme theme) => theme.Pins.Keys.ToList().Aggregate(theme, (unpinned, name) => unpinned.WithoutToken(name));

	private static Dictionary<string, string> Copy(IReadOnlyDictionary<string, string> pins) => new(pins, StringComparer.Ordinal);

	private static string? SwatchStyle(MokaSrgb color) => new StyleBuilder()
		.AddStyle("background-color", color.ToHex(withAlpha: true))
		.Build();

	private static string? SampleStyle(MokaContrastCheck check) => new StyleBuilder()
		.AddStyle("color", check.ForegroundColor.ToHex(withAlpha: true))
		.AddStyle("background-color", check.BackgroundColor.ToHex())
		.Build();

	private string? TokenSwatchStyle(MokaToken token, string? value) =>
		token.Kind == MokaTokenKind.Color && value is not null && Inspected.TryResolveColor(value, out MokaSrgb color) ? SwatchStyle(color) : null;

	// What a change is called in the line under the preview heading, and the style row it came from, if any.
	private readonly record struct StudioChange(string What, MokaAppearanceRows? Row);

	private enum Tab
	{
		Contrast,
		Tokens,
		Export
	}

	private enum SeedSlot
	{
		Accent,
		Neutral,
		Success,
		Warning,
		Error,
		Info
	}
}
