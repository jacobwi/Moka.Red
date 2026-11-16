using Moka.Red.Core.Colors;
using Moka.Red.Core.Theming;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Tests.Services;

public class MokaDiagnosticsServiceTests
{
	private readonly MokaDiagnosticsService _service;

	public MokaDiagnosticsServiceTests()
	{
		_service = new MokaDiagnosticsService(new DiagnosticsOptions());
	}

	// ── Theme Token Tests ──────────────────────────────────────────

	// The inspector listed 53 tokens it named by hand, a fifth of what a theme writes, so the new design's tokens, the
	// label, chart and code colors and every layout and component token were missing from it.
	[Theory]
	[MemberData(nameof(Themes))]
	public void GetThemeTokens_ListsEveryTokenTheThemeWrites(string name)
	{
		MokaTheme theme = ThemeNamed(name);
		IReadOnlyDictionary<string, string> written = theme.ToTokenMap();

		IReadOnlyList<ThemeTokenGroup> groups = _service.GetThemeTokens(theme);

		ThemeToken[] tokens = groups.SelectMany(g => g.Tokens).ToArray();
		Assert.Equal(written.Count, tokens.Length);
		Assert.All(tokens, token => Assert.Equal(written[token.CssVariable], token.Value));
		Assert.Equal(
			MokaTokens.All.Where(t => written.ContainsKey(t.Name)).Select(t => t.Group.ToString()).Distinct(),
			groups.Select(g => g.Name));
	}

	public static TheoryData<string> Themes => ["Default", "Light", "Dark", "Crisp"];

	private static MokaTheme ThemeNamed(string name) => name switch
	{
		"Light" => MokaTheme.Light,
		"Dark" => MokaTheme.Dark,
		"Crisp" => MokaTheme.Crisp,
		_ => MokaTheme.Default
	};

	// A token written as var() or color-mix() has no color of its own to draw, and in the popup page no theme on the
	// page to work one out from.
	[Fact]
	public void GetThemeTokens_ResolvesColorsWrittenAsReferences()
	{
		MokaTheme theme = MokaTheme.Default;
		IReadOnlyDictionary<string, string> written = theme.ToTokenMap();
		string referenced = written.First(pair =>
			MokaTokens.TryFind(pair.Key, out MokaToken? token) && token.Kind == MokaTokenKind.Color
			&& pair.Value.StartsWith("var(", StringComparison.Ordinal)
			&& theme.TryResolveColor(pair.Value, out _)).Key;

		ThemeToken shown = _service.GetThemeTokens(theme).SelectMany(g => g.Tokens).Single(t => t.CssVariable == referenced);

		Assert.Equal(ThemeTokenKind.Color, shown.Kind);
		Assert.True(theme.TryResolveColor(written[referenced], out MokaSrgb color));
		Assert.Equal(color.ToString(), shown.ResolvedColor);
	}

	[Fact]
	public void GetThemeTokens_KindsSayHowToPreview()
	{
		ThemeToken[] tokens = _service.GetThemeTokens(MokaTheme.Light).SelectMany(g => g.Tokens).ToArray();

		Assert.Equal(ThemeTokenKind.Spacing, tokens.Single(t => t.CssVariable == "--moka-spacing-md").Kind);
		Assert.Equal(ThemeTokenKind.BorderRadius, tokens.Single(t => t.CssVariable == "--moka-radius-md").Kind);
		Assert.Equal(ThemeTokenKind.FontFamily, tokens.Single(t => t.CssVariable == "--moka-font-family").Kind);
		Assert.Equal(ThemeTokenKind.FontSize, tokens.Single(t => t.CssVariable == "--moka-font-size-base").Kind);
		Assert.Equal(ThemeTokenKind.Other, tokens.Single(t => t.CssVariable == "--moka-z-modal").Kind);
		Assert.All(tokens.Where(t => t.Kind != ThemeTokenKind.Color), t => Assert.Null(t.ResolvedColor));
	}

	[Fact]
	public void GetThemeTokens_PaletteGroup_ContainsAllColors()
	{
		IReadOnlyList<ThemeTokenGroup> groups = _service.GetThemeTokens(MokaTheme.Light);
		ThemeTokenGroup palette = groups.First(g => g.Name == "Color");

		Assert.Contains(palette.Tokens, t => t.CssVariable == "--moka-color-primary");
		Assert.Contains(palette.Tokens, t => t.CssVariable == "--moka-color-secondary");
		Assert.Contains(palette.Tokens, t => t.CssVariable == "--moka-color-surface");
		Assert.Contains(palette.Tokens, t => t.CssVariable == "--moka-color-error");
		Assert.Contains(palette.Tokens, t => t.CssVariable == "--moka-color-warning");
		Assert.Contains(palette.Tokens, t => t.CssVariable == "--moka-color-success");
		Assert.Contains(palette.Tokens, t => t.CssVariable == "--moka-color-info");
	}

	[Fact]
	public void GetThemeTokens_LightTheme_HasCorrectPrimaryColor()
	{
		IReadOnlyList<ThemeTokenGroup> groups = _service.GetThemeTokens(MokaTheme.Light);
		ThemeTokenGroup palette = groups.First(g => g.Name == "Color");
		ThemeToken primary = palette.Tokens.First(t => t.CssVariable == "--moka-color-primary");

		Assert.Equal("#d32f2f", primary.Value);
	}

	[Fact]
	public void GetThemeTokens_DarkTheme_HasDifferentColors()
	{
		IReadOnlyList<ThemeTokenGroup> lightGroups = _service.GetThemeTokens(MokaTheme.Light);
		IReadOnlyList<ThemeTokenGroup> darkGroups = _service.GetThemeTokens(MokaTheme.Dark);

		ThemeToken lightPrimary = lightGroups.First(g => g.Name == "Color").Tokens
			.First(t => t.CssVariable == "--moka-color-primary");
		ThemeToken darkPrimary = darkGroups.First(g => g.Name == "Color").Tokens
			.First(t => t.CssVariable == "--moka-color-primary");

		Assert.NotEqual(lightPrimary.Value, darkPrimary.Value);
	}

	[Fact]
	public void GetThemeTokens_CustomTheme_ReflectsOverrides()
	{
		var customTheme = new MokaTheme
		{
			Palette = new MokaPalette
			{
				Primary = "#00ff00",
				PrimaryLight = "#66ff66",
				PrimaryDark = "#009900",
				OnPrimary = "#000000",
				Secondary = "#455a64",
				SecondaryLight = "#718792",
				SecondaryDark = "#1c313a",
				OnSecondary = "#ffffff",
				Surface = "#ffffff",
				SurfaceVariant = "#f5f5f5",
				OnSurface = "#1c1b1f",
				Background = "#fafafa",
				OnBackground = "#1c1b1f",
				Error = "#b00020",
				OnError = "#ffffff",
				Warning = "#f57c00",
				OnWarning = "#ffffff",
				Success = "#2e7d32",
				OnSuccess = "#ffffff",
				Info = "#0288d1",
				OnInfo = "#ffffff",
				Outline = "#c4c4c4",
				OutlineVariant = "#e0e0e0"
			}
		};

		IReadOnlyList<ThemeTokenGroup> groups = _service.GetThemeTokens(customTheme);
		ThemeTokenGroup palette = groups.First(g => g.Name == "Color");
		ThemeToken primary = palette.Tokens.First(t => t.CssVariable == "--moka-color-primary");

		Assert.Equal("#00ff00", primary.Value);
	}

	[Theory]
	[InlineData("Color")]
	[InlineData("Typography")]
	[InlineData("Spacing")]
	[InlineData("Radius")]
	[InlineData("Layout")]
	[InlineData("Chart")]
	public void GetThemeTokens_HasExpectedGroup(string groupName)
	{
		IReadOnlyList<ThemeTokenGroup> groups = _service.GetThemeTokens(MokaTheme.Light);

		Assert.Contains(groups, g => g.Name == groupName);
	}

	// ── Render Tracking Tests ──────────────────────────────────────

	[Fact]
	public void RecordRender_CreatesNewEntry()
	{
		_service.RecordRender("MokaButton", "btn-1", TimeSpan.FromMilliseconds(5));

		IReadOnlyList<ComponentRenderEntry> entries = _service.GetRenderEntries();
		Assert.Single(entries);
		Assert.Equal(1, entries[0].RenderCount);
		Assert.Equal("MokaButton", entries[0].ComponentType);
		Assert.Equal("btn-1", entries[0].ComponentId);
	}

	[Fact]
	public void RecordRender_IncrementsExistingEntry()
	{
		_service.RecordRender("MokaButton", "btn-1", TimeSpan.FromMilliseconds(5));
		_service.RecordRender("MokaButton", "btn-1", TimeSpan.FromMilliseconds(3));
		_service.RecordRender("MokaButton", "btn-1", TimeSpan.FromMilliseconds(4));

		IReadOnlyList<ComponentRenderEntry> entries = _service.GetRenderEntries();
		Assert.Single(entries);
		Assert.Equal(3, entries[0].RenderCount);
	}

	[Fact]
	public void RecordRender_TracksDuration()
	{
		var duration = TimeSpan.FromMilliseconds(42);
		_service.RecordRender("MokaButton", "btn-1", duration);

		IReadOnlyList<ComponentRenderEntry> entries = _service.GetRenderEntries();
		Assert.Equal(duration, entries[0].LastRenderDuration);
	}

	[Fact]
	public void RecordRender_UpdatesLastRenderTime()
	{
		DateTime before = DateTime.UtcNow;
		_service.RecordRender("MokaButton", "btn-1", TimeSpan.FromMilliseconds(5));
		DateTime after = DateTime.UtcNow;

		IReadOnlyList<ComponentRenderEntry> entries = _service.GetRenderEntries();
		Assert.True(entries[0].LastRenderTime >= before.AddSeconds(-1));
		Assert.True(entries[0].LastRenderTime <= after.AddSeconds(1));
	}

	[Fact]
	public void RecordShouldRenderSkip_IncrementsSkipCount()
	{
		_service.RecordShouldRenderSkip("MokaButton", "btn-1");
		_service.RecordShouldRenderSkip("MokaButton", "btn-1");

		IReadOnlyList<ComponentRenderEntry> entries = _service.GetRenderEntries();
		Assert.Single(entries);
		Assert.Equal(2, entries[0].ShouldRenderSkipCount);
	}

	[Fact]
	public void GetRenderEntries_ReturnsSortedByRenderCountDescending()
	{
		_service.RecordRender("A", "a-1", TimeSpan.Zero);

		_service.RecordRender("B", "b-1", TimeSpan.Zero);
		_service.RecordRender("B", "b-1", TimeSpan.Zero);
		_service.RecordRender("B", "b-1", TimeSpan.Zero);

		_service.RecordRender("C", "c-1", TimeSpan.Zero);
		_service.RecordRender("C", "c-1", TimeSpan.Zero);

		IReadOnlyList<ComponentRenderEntry> entries = _service.GetRenderEntries();
		Assert.Equal(3, entries.Count);
		Assert.Equal("B", entries[0].ComponentType);
		Assert.Equal("C", entries[1].ComponentType);
		Assert.Equal("A", entries[2].ComponentType);
	}

	[Fact]
	public void GetRenderEntries_ReturnsEmptyWhenNoData()
	{
		IReadOnlyList<ComponentRenderEntry> entries = _service.GetRenderEntries();

		Assert.Empty(entries);
	}

	[Fact]
	public void ClearRenderData_RemovesAllEntries()
	{
		_service.RecordRender("MokaButton", "btn-1", TimeSpan.Zero);
		_service.RecordRender("MokaCard", "card-1", TimeSpan.Zero);

		_service.ClearRenderData();

		Assert.Empty(_service.GetRenderEntries());
	}

	[Fact]
	public void RecordRender_FiresOnRenderDataChanged()
	{
		bool fired = false;
		_service.OnRenderDataChanged += (_, _) => fired = true;

		_service.RecordRender("MokaButton", "btn-1", TimeSpan.Zero);

		Assert.True(fired);
	}

	[Fact]
	public void RecordShouldRenderSkip_FiresOnRenderDataChanged()
	{
		bool fired = false;
		_service.OnRenderDataChanged += (_, _) => fired = true;

		_service.RecordShouldRenderSkip("MokaButton", "btn-1");

		Assert.True(fired);
	}

	// ── JS Interop Tracking Tests ──────────────────────────────────

	[Fact]
	public void RecordJsInteropCall_CreatesNewEntry()
	{
		_service.RecordJsInteropCall("eval", TimeSpan.FromMilliseconds(10));

		IReadOnlyList<JsInteropEntry> entries = _service.GetJsInteropEntries();
		Assert.Single(entries);
		Assert.Equal("eval", entries[0].Identifier);
		Assert.Equal(1, entries[0].CallCount);
	}

	[Fact]
	public void RecordJsInteropCall_IncrementsCallCount()
	{
		_service.RecordJsInteropCall("eval", TimeSpan.FromMilliseconds(10));
		_service.RecordJsInteropCall("eval", TimeSpan.FromMilliseconds(20));

		IReadOnlyList<JsInteropEntry> entries = _service.GetJsInteropEntries();
		Assert.Single(entries);
		Assert.Equal(2, entries[0].CallCount);
	}

	[Fact]
	public void RecordJsInteropCall_TracksMaxDuration()
	{
		_service.RecordJsInteropCall("eval", TimeSpan.FromMilliseconds(10));
		_service.RecordJsInteropCall("eval", TimeSpan.FromMilliseconds(50));
		_service.RecordJsInteropCall("eval", TimeSpan.FromMilliseconds(30));

		IReadOnlyList<JsInteropEntry> entries = _service.GetJsInteropEntries();
		Assert.Equal(TimeSpan.FromMilliseconds(50), entries[0].MaxDuration);
	}

	[Fact]
	public void RecordJsInteropCall_CalculatesAverageDuration()
	{
		_service.RecordJsInteropCall("eval", TimeSpan.FromMilliseconds(10));
		_service.RecordJsInteropCall("eval", TimeSpan.FromMilliseconds(20));
		_service.RecordJsInteropCall("eval", TimeSpan.FromMilliseconds(30));

		IReadOnlyList<JsInteropEntry> entries = _service.GetJsInteropEntries();
		Assert.Equal(TimeSpan.FromMilliseconds(20), entries[0].AverageDuration);
	}

	[Fact]
	public void GetJsInteropEntries_ReturnsSortedByCallCountDescending()
	{
		_service.RecordJsInteropCall("a", TimeSpan.Zero);

		_service.RecordJsInteropCall("b", TimeSpan.Zero);
		_service.RecordJsInteropCall("b", TimeSpan.Zero);
		_service.RecordJsInteropCall("b", TimeSpan.Zero);

		_service.RecordJsInteropCall("c", TimeSpan.Zero);
		_service.RecordJsInteropCall("c", TimeSpan.Zero);

		IReadOnlyList<JsInteropEntry> entries = _service.GetJsInteropEntries();
		Assert.Equal(3, entries.Count);
		Assert.Equal("b", entries[0].Identifier);
		Assert.Equal("c", entries[1].Identifier);
		Assert.Equal("a", entries[2].Identifier);
	}

	[Fact]
	public void RecordJsInteropCall_FiresOnPerformanceDataChanged()
	{
		bool fired = false;
		_service.OnPerformanceDataChanged += (_, _) => fired = true;

		_service.RecordJsInteropCall("eval", TimeSpan.Zero);

		Assert.True(fired);
	}

	// ── Disposal Tracking Tests ────────────────────────────────────

	[Fact]
	public void RecordDisposal_IncrementsDisposedCount()
	{
		_service.RecordRender("MokaButton", "btn-1", TimeSpan.Zero);
		_service.RecordDisposal("MokaButton", "btn-1");

		Assert.Equal(1, _service.DisposedComponentCount);
	}

	[Fact]
	public void ActiveComponentCount_ReflectsLiveComponents()
	{
		_service.RecordRender("A", "a-1", TimeSpan.Zero);
		_service.RecordRender("B", "b-1", TimeSpan.Zero);
		_service.RecordRender("C", "c-1", TimeSpan.Zero);

		Assert.Equal(3, _service.ActiveComponentCount);

		_service.RecordDisposal("A", "a-1");

		Assert.Equal(2, _service.ActiveComponentCount);
	}

	[Fact]
	public void ClearPerformanceData_ResetsCounters()
	{
		_service.RecordJsInteropCall("eval", TimeSpan.Zero);
		_service.RecordRender("MokaButton", "btn-1", TimeSpan.Zero);
		_service.RecordDisposal("MokaButton", "btn-1");

		_service.ClearPerformanceData();

		Assert.Equal(0, _service.DisposedComponentCount);
		Assert.Empty(_service.GetJsInteropEntries());
	}

	// Clearing the performance data marked every disposed component live again, so Active jumped by every component
	// ever disposed and the Perf tab's trend read it as growth.
	[Fact]
	public void ClearPerformanceData_KeepsDisposedComponentsOutOfTheActiveCount()
	{
		_service.RecordRender("A", "a-1", TimeSpan.Zero);
		_service.RecordRender("B", "b-1", TimeSpan.Zero);
		_service.RecordDisposal("A", "a-1");

		_service.ClearPerformanceData();

		Assert.Equal(1, _service.ActiveComponentCount);
		Assert.True(_service.GetRenderEntries().Single(e => e.ComponentId == "a-1").IsDisposed);
	}

	// A component disposed while tracking was paused stayed active for good.
	[Fact]
	public void RecordDisposal_WhilePaused_StillMarksTheComponentGone_ButRecordsNothing()
	{
		_service.RecordRender("A", "a-1", TimeSpan.Zero);
		int events = _service.GetRecentEvents(int.MaxValue).Count;
		_service.IsPaused = true;

		_service.RecordDisposal("A", "a-1");

		Assert.Equal(0, _service.ActiveComponentCount);
		Assert.Equal(0, _service.DisposedComponentCount);
		Assert.Equal(events, _service.GetRecentEvents(int.MaxValue).Count);
	}

	// Every disposed component stayed listed for as long as the circuit lived.
	[Fact]
	public void DisposedComponents_AreKeptUpToTheLimit_OldestDroppedFirst()
	{
		int made = MokaDiagnosticsService.MaxDisposedEntries + 25;
		_service.RecordRender("Live", "live", TimeSpan.Zero);
		for (int i = 0; i < made; i++)
		{
			_service.RecordRender("Row", $"row-{i}", TimeSpan.Zero);
			_service.RecordDisposal("Row", $"row-{i}");
		}

		IReadOnlyList<ComponentRenderEntry> entries = _service.GetRenderEntries();
		Assert.Equal(MokaDiagnosticsService.MaxDisposedEntries, entries.Count(e => e.IsDisposed));
		Assert.Contains(entries, e => e.ComponentId == "live" && !e.IsDisposed);
		Assert.DoesNotContain(entries, e => e.ComponentId == "row-0");
		Assert.Contains(entries, e => e.ComponentId == $"row-{made - 1}");
		Assert.Equal(1, _service.ActiveComponentCount);
		Assert.Equal(made, _service.DisposedComponentCount);
	}

	[Fact]
	public void RecordDisposal_FiresOnPerformanceDataChanged()
	{
		bool fired = false;
		_service.OnPerformanceDataChanged += (_, _) => fired = true;

		_service.RecordRender("MokaButton", "btn-1", TimeSpan.Zero);
		_service.RecordDisposal("MokaButton", "btn-1");

		Assert.True(fired);
	}

	// ── Overlay Visibility Tests ───────────────────────────────────

	[Fact]
	public void IsOverlayVisible_DefaultFalse() => Assert.False(_service.IsOverlayVisible);

	[Fact]
	public void IsOverlayVisible_SetTrue_FiresEvent()
	{
		bool fired = false;
		_service.OnOverlayVisibilityChanged += (_, _) => fired = true;

		_service.IsOverlayVisible = true;

		Assert.True(fired);
		Assert.True(_service.IsOverlayVisible);
	}

	[Fact]
	public void IsOverlayVisible_SetSameValue_DoesNotFireEvent()
	{
		_service.IsOverlayVisible = false; // already false by default

		bool fired = false;
		_service.OnOverlayVisibilityChanged += (_, _) => fired = true;

		_service.IsOverlayVisible = false;

		Assert.False(fired);
	}

	// ── Thread Safety Tests ────────────────────────────────────────

	[Fact]
	public void ConcurrentRecordRender_DoesNotThrow()
	{
		Exception? exception = Record.Exception(() =>
		{
			Parallel.For(0, 100, i =>
			{
				string componentId = $"comp-{i % 10}";
				_service.RecordRender("MokaButton", componentId, TimeSpan.FromMilliseconds(i));
			});
		});

		Assert.Null(exception);
	}

	[Fact]
	public void ConcurrentRecordAndRead_DoesNotThrow()
	{
		Exception? exception = Record.Exception(() =>
		{
			var tasks = new Task[2];

			tasks[0] = Task.Run(() =>
			{
				for (int i = 0; i < 100; i++)
				{
					_service.RecordRender("MokaButton", $"comp-{i % 5}", TimeSpan.FromMilliseconds(i));
				}
			});

			tasks[1] = Task.Run(() =>
			{
				for (int i = 0; i < 100; i++)
				{
					_ = _service.GetRenderEntries();
				}
			});

			Task.WaitAll(tasks);
		});

		Assert.Null(exception);
	}
}
