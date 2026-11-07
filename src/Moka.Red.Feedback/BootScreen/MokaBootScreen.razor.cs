using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.BootScreen;

/// <summary>
///     A screen for an app starting up, a reconnect or a part of the page loading. It draws one of several
///     <see cref="BootVariant" />s, shows how far along it is, and says what went wrong when startup fails. In a small
///     box it draws as a compact card row. For the splash shown before Blazor starts, link
///     <c>_content/Moka.Red.Core/moka-boot.css</c> and put its static markup in the page.
/// </summary>
public partial class MokaBootScreen : MokaVisualComponentBase
{
	private const string Module = "./_content/Moka.Red.Feedback/BootScreen/MokaBootScreen.razor.js";
	private const string DefaultStatusText = "initializing";
	private const int AsciiCells = 20;
	private const int TerminalTagWidth = 6;

	private readonly string _holdKey = Guid.NewGuid().ToString("N");
	private ElementReference _root;
	private bool _holding;
	private bool _liveReady;

	// Which design the page is on decides the variant when none is named, so this computes from the theme.
	[CascadingParameter]
	private MokaTheme? CascadedTheme { get; set; }

	/// <summary>Extra content under the rest.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Whether the boot screen is rendered. Defaults to true.</summary>
	[Parameter]
	public bool Visible { get; set; } = true;

	/// <summary>
	///     How it draws. Null, the default, is <see cref="MokaBootVariant.Mark" /> on the new design and
	///     <see cref="MokaBootVariant.Classic" />, exactly as before, on <c>MokaTheme.Dark</c> and <c>Light</c>.
	/// </summary>
	[Parameter]
	public MokaBootVariant? BootVariant { get; set; }

	/// <summary>The brand. Defaults to "MOKA".</summary>
	[Parameter]
	public string Brand { get; set; } = "MOKA";

	/// <summary>Optional second part of the brand, drawn after a dot and muted on the new design.</summary>
	[Parameter]
	public string? BrandSecondary { get; set; }

	/// <summary>
	///     Something to draw beside the brand, such as an app icon. Without it the new design draws the brand's first
	///     letter on an accent tile.
	/// </summary>
	[Parameter]
	public RenderFragment? Logo { get; set; }

	/// <summary>
	///     The status line. Defaults to "initializing". With <see cref="Steps" />, the running step's label stands in
	///     for it while it is empty or left at its default. Null or empty hides the line.
	/// </summary>
	[Parameter]
	public string? StatusText { get; set; } = DefaultStatusText;

	/// <summary>
	///     Facts such as the version and engine: <c>&lt;span&gt;engine&lt;span class="moka-boot-screen-v"&gt;WASM&lt;/span&gt;&lt;/span&gt;</c>
	///     per chip. The classic variants draw them as chips with the value in the accent, the others as one quiet
	///     mono line.
	/// </summary>
	[Parameter]
	public RenderFragment? ChipsContent { get; set; }

	/// <summary>The faint accent grid behind <see cref="MokaBootVariant.Classic" />. Defaults to true.</summary>
	[Parameter]
	public bool ShowGrid { get; set; } = true;

	/// <summary>The line sweeping down <see cref="MokaBootVariant.Classic" />. Defaults to true.</summary>
	[Parameter]
	public bool ShowScanline { get; set; } = true;

	/// <summary>Whether to draw the progress bar. Defaults to true.</summary>
	[Parameter]
	public bool ShowBar { get; set; } = true;

	/// <summary>
	///     When true (default) the screen covers the window and the page under it is inert until it goes. When false
	///     it fills its nearest positioned ancestor, and in a box under about 360 by 240 px it draws as a card row.
	/// </summary>
	[Parameter]
	public bool FullScreen { get; set; } = true;

	/// <summary>
	///     The steps of the startup. <see cref="MokaBootVariant.Log" />, <see cref="MokaBootVariant.Steps" /> and
	///     <see cref="MokaBootVariant.Terminal" /> list them, and the card counts them.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaBootStep>? Steps { get; set; }

	/// <summary>Whether a log shows each line's time and the step list each step's duration. Defaults to true.</summary>
	[Parameter]
	public bool ShowTimes { get; set; } = true;

	/// <summary>How many lines a log keeps. Older lines scroll off the top. Defaults to 8.</summary>
	[Parameter]
	public int MaxLines { get; set; } = 8;

	/// <summary>The label over the step list. Defaults to "Starting up".</summary>
	[Parameter]
	public string Title { get; set; } = "Starting up";

	/// <summary>A line under the bar, such as "Usually about 5 seconds".</summary>
	[Parameter]
	public string? Hint { get; set; }

	/// <summary>Content under the brand on the left of <see cref="MokaBootVariant.Steps" />.</summary>
	[Parameter]
	public RenderFragment? Aside { get; set; }

	/// <summary>
	///     How far along it is, 0 to 100. Null fills the bar from <see cref="Steps" /> when there are any, and
	///     otherwise slides a piece along it.
	/// </summary>
	[Parameter]
	public double? Progress { get; set; }

	/// <summary>What went wrong. When set, the failed state shows. A failed step shows it too.</summary>
	[Parameter]
	public string? Error { get; set; }

	/// <summary>Called by the Try again button of the failed state. Without it there is no button.</summary>
	[Parameter]
	public EventCallback OnRetry { get; set; }

	/// <summary>Text of the Try again button. Defaults to "Try again".</summary>
	[Parameter]
	public string RetryText { get; set; } = "Try again";

	/// <summary>
	///     Writes the card row's count from the current step's number and the number of steps. Defaults to
	///     "Step 3 of 5".
	/// </summary>
	[Parameter]
	public Func<int, int, string>? StepCountText { get; set; }

	/// <summary>
	///     Writes a log's footer count from the finished steps and the number of steps. Defaults to "3 of 5 steps".
	/// </summary>
	[Parameter]
	public Func<int, int, string>? StepsDoneText { get; set; }

	/// <summary>
	///     Writes the line under a step list that stopped, from the failed step's number and the number of steps.
	///     Defaults to "Stopped at step 3 of 5". <see cref="Error" /> takes its place when set.
	/// </summary>
	[Parameter]
	public Func<int, int, string>? StoppedText { get; set; }

	/// <summary>
	///     Names a step's state for screen readers, read before each step of the list. Defaults to "Waiting",
	///     "Running", "Done", "Warning", "Failed" and "Skipped".
	/// </summary>
	[Parameter]
	public Func<MokaBootStepState, string>? StepStateText { get; set; }

	/// <summary>
	///     The state word a log prints on each line, which Terminal draws in capitals inside its brackets. Defaults
	///     to "ok", "warn", "fail", "skip" and "run", and Terminal's own "OK", "WARN", "FAIL", "SKIP" and "..".
	/// </summary>
	[Parameter]
	public Func<MokaBootStepState, string>? LogWordText { get; set; }

	/// <summary>Writes how long a step or the whole startup took. Defaults to "120 ms" under a second, else "1.39 s".</summary>
	[Parameter]
	public Func<TimeSpan, string>? DurationText { get; set; }

	/// <summary>More for the failed state, beside Try again: details, a link or a second button.</summary>
	[Parameter]
	public RenderFragment? ErrorContent { get; set; }

	/// <summary>An icon the card row shows in place of its spinner.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-boot-screen";

	/// <inheritdoc />
	protected override string CssClass => IsLegacy
		? new CssBuilder(RootClass)
			.AddClass("moka-boot-screen--grid", ShowGrid)
			.AddClass("moka-boot-screen--scanline", ShowScanline)
			.AddClass("moka-boot-screen--embedded", !FullScreen)
			.AddClass("moka-boot-screen--failed", IsFailed)
			.AddClass(Class)
			.Build()
		: new CssBuilder(RootClass)
			.AddClass(VariantClass)
			.AddClass(IsPlain ? "moka-boot-screen--plain" : "moka-boot-screen--lit")
			.AddClass("moka-boot-screen--grid", ShowGrid && ResolvedVariant == MokaBootVariant.Classic)
			.AddClass("moka-boot-screen--scanline", ShowScanline && ResolvedVariant == MokaBootVariant.Classic)
			.AddClass("moka-boot-screen--embedded", !FullScreen)
			.AddClass("moka-boot-screen--shrinks", !FullScreen)
			.AddClass("moka-boot-screen--failed", IsFailed)
			.AddClass(Class)
			.Build();

	private bool IsNewDesign => CascadedTheme?.Look == MokaLook.Gate;

	private MokaBootVariant ResolvedVariant =>
		BootVariant ?? (IsNewDesign ? MokaBootVariant.Mark : MokaBootVariant.Classic);

	// No variant named on the original design: exactly the screen it always drew, small boxes included.
	private bool IsLegacy => BootVariant is null && !IsNewDesign;

	private bool IsPlain => ResolvedVariant is MokaBootVariant.Mark or MokaBootVariant.Log or MokaBootVariant.Steps;

	private bool Shrinks => !FullScreen && !IsLegacy;

	private string VariantClass => ResolvedVariant switch
	{
		MokaBootVariant.Mark => "moka-boot-screen--mark",
		MokaBootVariant.Log => "moka-boot-screen--log",
		MokaBootVariant.Steps => "moka-boot-screen--steps",
		MokaBootVariant.Classic => "moka-boot-screen--classic",
		MokaBootVariant.Terminal => "moka-boot-screen--terminal",
		MokaBootVariant.Pulse => "moka-boot-screen--pulse",
		_ => "moka-boot-screen--horizon"
	};

	private IReadOnlyList<MokaBootStep> StepList => Steps ?? [];

	private MokaBootStep? RunningStep => StepList.FirstOrDefault(s => s.State == MokaBootStepState.Running);

	private MokaBootStep? FailedStep => StepList.FirstOrDefault(s => s.State == MokaBootStepState.Failed);

	private bool IsFailed => !string.IsNullOrWhiteSpace(Error) || FailedStep is not null;

	private bool HasActions => OnRetry.HasDelegate || ErrorContent is not null;

	private bool HasSecondary => !string.IsNullOrEmpty(BrandSecondary);

	private string? Status
	{
		get
		{
			bool own = !string.IsNullOrEmpty(StatusText) &&
			           !string.Equals(StatusText, DefaultStatusText, StringComparison.Ordinal);
			return own || RunningStep is null ? StatusText : RunningStep.Label;
		}
	}

	private string FailureTitle => !string.IsNullOrWhiteSpace(Error) ? Error : FailedStep?.Label ?? string.Empty;

	private string? FailureDetail =>
		FailedStep?.Error is { Length: > 0 } detail && !string.Equals(detail, FailureTitle, StringComparison.Ordinal)
			? detail
			: null;

	private string? LiveText
	{
		get
		{
			if (!_liveReady || !Visible)
			{
				return null;
			}

			if (!IsFailed)
			{
				return Status;
			}

			return FailureDetail is null ? FailureTitle : $"{FailureTitle}. {FailureDetail}";
		}
	}

	private string? Initial => string.IsNullOrWhiteSpace(Brand)
		? null
		: char.ToUpper(Brand.TrimStart()[0], CultureInfo.CurrentCulture).ToString();

	private RenderFragment? LogoTile => Logo ?? (Initial is null ? null : DefaultLogo);

	private int FinishedCount => StepList.Count(s => s.State is MokaBootStepState.Done or MokaBootStepState.Warning
		or MokaBootStepState.Skipped);

	private double? ShownProgress
	{
		get
		{
			if (Progress is double progress && !double.IsNaN(progress))
			{
				return Math.Clamp(progress, 0, 100);
			}

			return StepList.Count == 0 ? null : FinishedCount * 100d / StepList.Count;
		}
	}

	private string LineClass => new CssBuilder("moka-boot-screen__line")
		.AddClass("moka-boot-screen__line--determinate", ShownProgress is not null)
		.Build();

	private string BarClass => new CssBuilder("moka-boot-screen-bar")
		.AddClass("moka-boot-screen-bar--determinate", ShownProgress is not null)
		.Build();

	private string? FillStyle => ShownProgress is double progress
		? new StyleBuilder()
			.AddStyle("width", $"{progress.ToString("0.##", CultureInfo.InvariantCulture)}%")
			.Build()
		: null;

	private TimeSpan TotalElapsed => StepList.Aggregate(TimeSpan.Zero, (sum, s) => sum + (s.Elapsed ?? TimeSpan.Zero));

	private int CurrentStepNumber
	{
		get
		{
			int index = -1;
			for (int i = 0; i < StepList.Count; i++)
			{
				if (StepList[i].State is MokaBootStepState.Running or MokaBootStepState.Failed)
				{
					index = i;
					break;
				}
			}

			return index >= 0 ? index + 1 : Math.Min(FinishedCount + 1, StepList.Count);
		}
	}

	private string StepsDoneLine => StepsDoneText?.Invoke(FinishedCount, StepList.Count)
	                                ?? string.Create(CultureInfo.CurrentCulture, $"{FinishedCount} of {StepList.Count} steps");

	private string StoppedLine => !string.IsNullOrWhiteSpace(Error)
		? Error
		: StoppedText?.Invoke(CurrentStepNumber, StepList.Count)
		  ?? string.Create(CultureInfo.CurrentCulture, $"Stopped at step {CurrentStepNumber} of {StepList.Count}");

	private string? CardTitle => IsFailed ? FailureTitle : Status;

	private string? CardSubline
	{
		get
		{
			if (IsFailed)
			{
				return FailureDetail;
			}

			if (StepList.Count == 0)
			{
				return Hint;
			}

			string count = StepCountText?.Invoke(CurrentStepNumber, StepList.Count)
			               ?? string.Create(CultureInfo.CurrentCulture, $"Step {CurrentStepNumber} of {StepList.Count}");
			return ShowTimes && TotalElapsed > TimeSpan.Zero ? $"{count} · {Duration(TotalElapsed)}" : count;
		}
	}

	private string CardTileClass => new CssBuilder("moka-boot-screen__card-tile")
		.AddClass("moka-boot-screen__card-tile--failed", IsFailed)
		.Build();

	// Steps whose time has come, in order, with when each started counted from the first. Waiting steps don't print.
	private List<(int Index, MokaBootStep Step, TimeSpan Start)> PrintedSteps
	{
		get
		{
			List<(int, MokaBootStep, TimeSpan)> printed = [];
			TimeSpan start = TimeSpan.Zero;
			for (int i = 0; i < StepList.Count; i++)
			{
				MokaBootStep step = StepList[i];
				if (step.State != MokaBootStepState.Waiting)
				{
					printed.Add((i, step, start));
				}

				start += step.Elapsed ?? TimeSpan.Zero;
			}

			int keep = Math.Max(1, MaxLines);
			return printed.Count > keep ? printed.GetRange(printed.Count - keep, keep) : printed;
		}
	}

	// An Error with no failed step to carry it gets a line of its own in a log.
	private bool PrintsErrorLine => !string.IsNullOrWhiteSpace(Error) && FailedStep is null;

	private string LinesClass => new CssBuilder("moka-boot-screen__lines")
		.AddClass("moka-boot-screen__lines--untimed", !ShowTimes)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			// A live region has to be on the page before its text changes, or a screen reader never reads the
			// first status. It renders empty once, and gets its text now.
			_liveReady = true;
			StateHasChanged();
		}

		bool hold = Visible && FullScreen;
		if (hold && !_holding)
		{
			_holding = true;
			await SafeModuleInvokeVoidAsync(Module, "hold", _holdKey, _root);
		}
		else if (!hold && _holding)
		{
			_holding = false;
			await SafeModuleInvokeVoidAsync(Module, "release", _holdKey);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_holding)
		{
			_holding = false;
			await SafeModuleInvokeVoidAsync(Module, "release", _holdKey);
		}

		await base.DisposeAsyncCore();
	}

	private Task RetryAsync() => OnRetry.InvokeAsync();

	private string Duration(TimeSpan elapsed) => DurationText?.Invoke(elapsed) ?? (elapsed.TotalSeconds < 1
		? string.Format(CultureInfo.CurrentCulture, "{0:0} ms", elapsed.TotalMilliseconds)
		: string.Format(CultureInfo.CurrentCulture, "{0:0.##} s", elapsed.TotalSeconds));

	private static string FormatClock(TimeSpan start) => start.TotalSeconds.ToString("00.000", CultureInfo.CurrentCulture);

	private static string FormatUptime(TimeSpan start) =>
		start.TotalSeconds.ToString("0.000", CultureInfo.CurrentCulture).PadLeft(8);

	private string LogWord(MokaBootStepState state) => LogWordText?.Invoke(state) ?? state switch
	{
		MokaBootStepState.Done => "ok",
		MokaBootStepState.Warning => "warn",
		MokaBootStepState.Failed => "fail",
		MokaBootStepState.Skipped => "skip",
		_ => "run"
	};

	// Six columns between the brackets, so the labels line up whatever the word.
	private string TerminalTag(MokaBootStepState state)
	{
		if (LogWordText is null)
		{
			return state switch
			{
				MokaBootStepState.Done => "  OK  ",
				MokaBootStepState.Warning => " WARN ",
				MokaBootStepState.Failed => " FAIL ",
				MokaBootStepState.Skipped => " SKIP ",
				_ => "  ..  "
			};
		}

		string word = LogWordText(state).ToUpper(CultureInfo.CurrentCulture);
		int left = Math.Max(0, (TerminalTagWidth - word.Length) / 2);
		return word.PadLeft(word.Length + left).PadRight(TerminalTagWidth);
	}

	private string StateWord(MokaBootStepState state) => StepStateText?.Invoke(state) ?? state switch
	{
		MokaBootStepState.Running => "Running",
		MokaBootStepState.Done => "Done",
		MokaBootStepState.Warning => "Warning",
		MokaBootStepState.Failed => "Failed",
		MokaBootStepState.Skipped => "Skipped",
		_ => "Waiting"
	};

	private static string StateClass(string block, MokaBootStepState state) => new CssBuilder(block)
		.AddClass(state switch
		{
			MokaBootStepState.Running => $"{block}--running",
			MokaBootStepState.Done => $"{block}--done",
			MokaBootStepState.Warning => $"{block}--warning",
			MokaBootStepState.Failed => $"{block}--failed",
			MokaBootStepState.Skipped => $"{block}--skipped",
			_ => $"{block}--waiting"
		})
		.Build();

	private static string? LineDetail(MokaBootStep step) =>
		step.State == MokaBootStepState.Failed && !string.IsNullOrEmpty(step.Error) ? step.Error : step.Detail;

	private string AsciiFill => new('█', AsciiCells * (int)Math.Round(ShownProgress ?? 0) / 100);

	private string AsciiRest => new('░', AsciiCells - AsciiFill.Length);

	private string AsciiPercent => string.Format(CultureInfo.CurrentCulture, "{0:0}%", ShownProgress ?? 0);
}
