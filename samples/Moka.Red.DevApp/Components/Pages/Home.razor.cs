using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Hotkeys;
using Moka.Red.Data.Chart;
using Moka.Red.DevApp.Components.Library;
using Moka.Red.Feedback.Toast;

namespace Moka.Red.DevApp.Components.Pages;

public sealed partial class Home : IAsyncDisposable
{
	private const string FrontDoorLayout = "front-door";
	private const string LauncherLayout = "launcher";
	private const string ShowcaseLayout = "showcase";
	private const int HitsShown = 8;
	private const int ReleasesShown = 6;
	private const int TrafficPoints = 24;

	private static readonly TimeSpan TrafficStep = TimeSpan.FromMilliseconds(1600);

	private static readonly IReadOnlyList<string> TrafficLabels =
		[.. Enumerable.Range(0, TrafficPoints).Select(i => i == TrafficPoints - 1 ? "now" : $"-{(TrafficPoints - 1 - i) * 25}s")];

	private static readonly IReadOnlyList<Device> Devices =
	[
		new("nas-01", "Online", MokaColor.Success, "2 s ago"),
		new("printer", "Idle", MokaColor.Warning, "4 min ago"),
		new("tv-lounge", "Offline", MokaColor.Error, "2 h ago"),
		new("phone-anna", "Online", MokaColor.Success, "8 s ago")
	];

	private static readonly IReadOnlyList<string> ShowcaseParts = ["Card", "Stat", "LineChart", "Table", "StatusDot", "Toast"];

	private static readonly IReadOnlyList<Release> Releases = ReleaseNotes.Minor("0.3.");

	private static readonly int ComponentCount = LibraryCatalog.Components.Count;

	private static readonly string PresetNames =
		$"{LibraryCatalog.Presets.Count(p => p.Theme.IsDark)} dark, {LibraryCatalog.Presets.Count(p => !p.Theme.IsDark)} light";

	private readonly double[] _trafficValues = new double[TrafficPoints];
	private readonly Random _random = new(11);

	private string _layout = FrontDoorLayout;
	private string _query = string.Empty;
	private IReadOnlyList<LibraryComponent> _hits = [];
	private bool _allHits;
	private bool _allReleases;
	private ElementReference _finder;
	private string? _boundFinder;
	private IJSObjectReference? _module;
	private IDisposable? _hotkey;
	private CancellationTokenSource? _trafficStop;
	private Task? _trafficLoop;
	private IReadOnlyList<MokaLineSeries> _traffic = [];

	[Inject]
	private NavigationManager Navigation { get; set; } = default!;

	[Inject]
	private IJSRuntime JS { get; set; } = default!;

	[Inject]
	private IMokaHotkeyService Hotkeys { get; set; } = default!;

	[Inject]
	private IMokaToastService Toast { get; set; } = default!;

	[SupplyParameterFromQuery(Name = "layout")]
	private string? LayoutQuery { get; set; }

	private bool Searching => _query.Trim().Length > 0;

	private IReadOnlyList<LibraryComponent> Hits => _hits;

	private IEnumerable<LibraryComponent> ShownHits => _allHits ? _hits : _hits.Take(HitsShown);

	private IEnumerable<Release> ShownReleases => _allReleases ? Releases : Releases.Take(ReleasesShown);

	private string CurrentRate => _trafficValues[^1].ToString("0", CultureInfo.InvariantCulture);

	protected override void OnInitialized()
	{
		SeedTraffic();
		_hotkey = Hotkeys.Register(new MokaHotkey
		{
			Keys = "/",
			Description = "Find a component",
			Group = "Overview",
			OnInvoke = FocusFinderAsync
		});
	}

	protected override void OnParametersSet()
	{
		_layout = LayoutQuery switch
		{
			LauncherLayout => LauncherLayout,
			ShowcaseLayout => ShowcaseLayout,
			_ => FrontDoorLayout
		};

		if (_layout == ShowcaseLayout && RendererInfo.IsInteractive)
		{
			StartTraffic();
		}
		else
		{
			StopTraffic();
		}
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			_module = await JS.InvokeAsync<IJSObjectReference>("import", "./Components/Pages/Home.razor.js");
		}

		if (_module is not null && _layout != ShowcaseLayout && _finder.Id is { } id && id != _boundFinder)
		{
			_boundFinder = id;
			await _module.SafeInvokeVoidAsync("bindFinder", _finder);
		}
	}

	public async ValueTask DisposeAsync()
	{
		_hotkey?.Dispose();
		StopTraffic();
		if (_trafficLoop is not null)
		{
			await _trafficLoop;
		}

		await _module.SafeDisposeAsync();
	}

	private void PickLayout(string layout) =>
		Navigation.NavigateTo(
			Navigation.GetUriWithQueryParameter("layout", layout == FrontDoorLayout ? null : layout),
			replace: true);

	private void SetQuery(string query)
	{
		_query = query;
		_hits = LibraryCatalog.Find(query);
		_allHits = false;
	}

	private void ToggleHits() => _allHits = !_allHits;

	private void ToggleReleases() => _allReleases = !_allReleases;

	private static string HitWhere(LibraryComponent hit) =>
		string.Equals(hit.Where, hit.Package, StringComparison.Ordinal) ? $"{hit.TypeName}, on the {hit.Package} page" : $"{hit.TypeName}, shown in {hit.Where}";

	private static string Sample(LibraryPackage area)
	{
		string[] names = [.. area.Components.Take(4).Select(component => component.Name)];
		int rest = area.Components.Count - names.Length;
		return rest > 0 ? $"{string.Join(", ", names)} and {rest} more" : string.Join(", ", names);
	}

	private async Task FocusFinderAsync()
	{
		if (_layout == ShowcaseLayout || _module is null || _finder.Id is null)
		{
			return;
		}

		await _module.SafeInvokeVoidAsync("focusFinder", _finder);
	}

	private void RaiseToast() =>
		Toast.ShowSuccess("48 GB in 6 min, nothing skipped.", options => options.Title = "Backup finished");

	private static string Rate(double value) => $"{value.ToString("0", CultureInfo.InvariantCulture)} Mb/s";

	private void SeedTraffic()
	{
		double value = 42;
		for (int i = 0; i < TrafficPoints; i++)
		{
			value = NextRate(value);
			_trafficValues[i] = value;
		}

		PublishTraffic();
	}

	private double NextRate(double last) => Math.Clamp(last + ((_random.NextDouble() - 0.46) * 14), 12, 92);

	private void PublishTraffic() =>
		_traffic = [new MokaLineSeries { Name = "Traffic", Values = [.. _trafficValues], Color = MokaColor.Primary }];

	private void StartTraffic()
	{
		if (_trafficStop is not null)
		{
			return;
		}

		_trafficStop = new CancellationTokenSource();
		_trafficLoop = RunTrafficAsync(_trafficStop.Token);
	}

	private void StopTraffic()
	{
		if (_trafficStop is null)
		{
			return;
		}

		_trafficStop.Cancel();
		_trafficStop.Dispose();
		_trafficStop = null;
	}

	// The loop resumes on the renderer's context, so it can render without InvokeAsync.
	private async Task RunTrafficAsync(CancellationToken token)
	{
		using var timer = new PeriodicTimer(TrafficStep);
		try
		{
			while (await timer.WaitForNextTickAsync(token))
			{
				Array.Copy(_trafficValues, 1, _trafficValues, 0, TrafficPoints - 1);
				_trafficValues[^1] = NextRate(_trafficValues[^2]);
				PublishTraffic();
				StateHasChanged();
			}
		}
		catch (OperationCanceledException)
		{
		}
	}

	private sealed record Device(string Name, string Status, MokaColor Tone, string Seen);
}
