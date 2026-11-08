using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.JobProgress;
using Moka.Red.Icons;

namespace Moka.Red.Feedback.JobList;

/// <summary>
///     Several jobs at once, such as uploads, downloads or exports: a status dot, the name and its actions, then where
///     it's going with the numbers and a thick bar. A failed job says why on its own line and a screen reader hears about
///     it, without hearing every percent. The list only shows what it's given: an action raises a callback, and the app
///     changes <see cref="Jobs" />. For one job told in words, use <c>MokaJobProgress</c>.
/// </summary>
public partial class MokaJobList : MokaVisualComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Feedback/JobList/MokaJobList.razor.js";

	private readonly RenderFragment _rows;
	private IReadOnlyList<string> _announcements = [];
	private int _announcementNumber;
	private MokaJobCounts _counts;
	private Dictionary<string, MokaJobState>? _knownStates;
	private PressedJob? _pressed;
	private HashSet<string>? _repeatedIds;
	private ElementReference _root;
	private RowSettings _rowSettings;
	private int _rowSettingsVersion;
	private bool _showAll;

	/// <summary>Creates the list.</summary>
	public MokaJobList() => _rows = WriteRows;

	private enum JobAction
	{
		Cancel,
		Retry,
		Remove
	}

	/// <summary>The jobs, in the order to show them. Null is the same as none.</summary>
	[Parameter]
	public IReadOnlyList<MokaJob>? Jobs { get; set; }

	/// <summary>Raised by a job's cancel button, shown while it's queued or running. Leave it unset to hide the button.</summary>
	[Parameter]
	public EventCallback<MokaJob> OnCancel { get; set; }

	/// <summary>Raised by a job's retry button, shown when it failed or was canceled. Leave it unset to hide the button.</summary>
	[Parameter]
	public EventCallback<MokaJob> OnRetry { get; set; }

	/// <summary>Raised by a job's remove button, shown unless it's running. Leave it unset to hide the button.</summary>
	[Parameter]
	public EventCallback<MokaJob> OnRemove { get; set; }

	/// <summary>Raised by Clear finished in the header. Leave it unset to hide the button. The app decides what goes.</summary>
	[Parameter]
	public EventCallback OnClearFinished { get; set; }

	/// <summary>Whether the summary and Clear finished show above the list. Defaults to true.</summary>
	[Parameter]
	public bool ShowHeader { get; set; } = true;

	/// <summary>How many jobs show before a button offers the rest, from the top of the list. Null or 0 shows them all.</summary>
	[Parameter]
	public int? MaxVisible { get; set; }

	/// <summary>
	///     Something before each job that takes the height of its row, such as a thumbnail. A row with a slot redraws
	///     whenever the list does, since a slot can read anything.
	/// </summary>
	[Parameter]
	public RenderFragment<MokaJob>? StartContent { get; set; }

	/// <summary>
	///     The app's own badges and actions for a job, between its name and the buttons. A row with a slot redraws whenever
	///     the list does, since a slot can read anything.
	/// </summary>
	[Parameter]
	public RenderFragment<MokaJob>? EndContent { get; set; }

	/// <summary>Shown in place of the list when there are no jobs.</summary>
	[Parameter]
	public RenderFragment? EmptyContent { get; set; }

	/// <summary>Every word the list writes.</summary>
	[Parameter]
	public MokaJobListText Text { get; set; } = new();

	/// <summary>
	///     How sizes are counted: <see cref="MokaByteUnits.Decimal" /> (the default) in powers of 1000, or
	///     <see cref="MokaByteUnits.Binary" /> in powers of 1024.
	/// </summary>
	[Parameter]
	public MokaByteUnits ByteUnits { get; set; }

	/// <summary>Writes a size in bytes, unit included. When set it wins over <see cref="ByteUnits" />, and a speed adds "/s".</summary>
	[Parameter]
	public Func<double, string>? ByteFormatter { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-job-list";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	private IReadOnlyList<MokaJob> AllJobs => Jobs ?? [];

	private bool CutsOff => MaxVisible is > 0 && AllJobs.Count > MaxVisible;

	private int ShownCount => CutsOff && !_showAll ? MaxVisible!.Value : AllJobs.Count;

	private int HiddenCount => CutsOff ? AllJobs.Count - MaxVisible!.Value : 0;

	private bool HasSlots => StartContent is not null || EndContent is not null;

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		ReadJobs();

		RowSettings settings = new(Text, ByteUnits, ByteFormatter, Disabled, OnCancel.HasDelegate, OnRetry.HasDelegate, OnRemove.HasDelegate, HasSlots);
		if (settings != _rowSettings)
		{
			_rowSettings = settings;
			_rowSettingsVersion++;
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		if (_pressed is not { } pressed)
		{
			return;
		}

		int index = IndexOf(pressed.Id);
		if (index >= 0 && AllJobs[index].State == pressed.State)
		{
			return;
		}

		_pressed = null;
		await SafeModuleInvokeVoidAsync(ModulePath, "restoreFocus", _root, index >= 0 ? index : pressed.Index);
	}

	private void ReadJobs()
	{
		int queued = 0, running = 0, done = 0, failed = 0, canceled = 0;
		Dictionary<string, MokaJobState> states = new(AllJobs.Count);
		HashSet<string>? repeated = null;
		List<string>? failures = null;

		foreach (MokaJob job in AllJobs)
		{
			switch (job.State)
			{
				case MokaJobState.Running:
					running++;
					break;
				case MokaJobState.Done:
					done++;
					break;
				case MokaJobState.Failed:
					failed++;
					if (HasJustFailed(job))
					{
						(failures ??= []).Add(Text.FailureAnnouncement(job));
					}

					break;
				case MokaJobState.Canceled:
					canceled++;
					break;
				default:
					queued++;
					break;
			}

			if (!states.TryAdd(job.Id, job.State))
			{
				(repeated ??= []).Add(job.Id);
			}
		}

		_counts = new MokaJobCounts(queued, running, done, failed, canceled);
		_repeatedIds = repeated;
		_knownStates = states;

		if (failures is not null)
		{
			_announcements = failures;
			_announcementNumber++;
		}
	}

	// Jobs already failed when the list first appears are old news.
	private bool HasJustFailed(MokaJob job) =>
		_knownStates is not null &&
		(!_knownStates.TryGetValue(job.Id, out MokaJobState before) || before != MokaJobState.Failed);

	// Written straight into one builder so @key can match rows across a reorder. A fragment per row would give each
	// row a region of its own, and keys only match within one. Blazor throws on a key siblings share, so a repeated
	// id goes without.
	private void WriteRows(RenderTreeBuilder builder)
	{
		int shown = ShownCount;
		for (int i = 0; i < shown; i++)
		{
			MokaJob job = AllJobs[i];
			builder.OpenComponent<MokaJobListRow>(0);
			builder.SetKey(_repeatedIds?.Contains(job.Id) == true ? null : job.Id);
			builder.AddComponentParameter(1, nameof(MokaJobListRow.Job), job);
			builder.AddComponentParameter(2, nameof(MokaJobListRow.Settings), _rowSettingsVersion);
			builder.AddComponentParameter(3, nameof(MokaJobListRow.AlwaysDraw), HasSlots);
			builder.AddComponentParameter(4, nameof(MokaJobListRow.ChildContent), Row(job));
			builder.CloseComponent();
		}
	}

	private int IndexOf(string id)
	{
		for (int i = 0; i < AllJobs.Count; i++)
		{
			if (AllJobs[i].Id == id)
			{
				return i;
			}
		}

		return -1;
	}

	private void ToggleShowAll() => _showAll = !_showAll;

	private Task ClearFinishedAsync() => OnClearFinished.InvokeAsync();

	// The button under the pointer becomes a different action as soon as the job's state changes, so the second click
	// of a double click would cancel what the first one retried.
	private async Task PressAsync(MouseEventArgs args, JobAction action, MokaJob job)
	{
		if (args.Detail > 1 || Disabled)
		{
			return;
		}

		_pressed = new PressedJob(job.Id, job.State, IndexOf(job.Id));
		EventCallback<MokaJob> callback = action switch
		{
			JobAction.Cancel => OnCancel,
			JobAction.Retry => OnRetry,
			_ => OnRemove
		};
		await callback.InvokeAsync(job);
	}

	private JobAction? MainAction(MokaJob job)
	{
		if (OnCancel.HasDelegate && job.State is MokaJobState.Queued or MokaJobState.Running)
		{
			return JobAction.Cancel;
		}

		return OnRetry.HasDelegate && job.State is MokaJobState.Failed or MokaJobState.Canceled ? JobAction.Retry : null;
	}

	private bool ShowsRemove(MokaJob job) => OnRemove.HasDelegate && job.State != MokaJobState.Running;

	private string ActionLabel(JobAction action, MokaJob job) => action switch
	{
		JobAction.Cancel => Text.Cancel(job.Name),
		JobAction.Retry => Text.Retry(job.Name),
		_ => Text.Remove(job.Name)
	};

	private static MokaIconDefinition ActionIcon(JobAction action) => action switch
	{
		JobAction.Cancel => MokaIcons.Navigation.Close,
		JobAction.Retry => MokaIcons.Action.Retry,
		_ => MokaIcons.Action.Remove
	};

	private static string RowClass(MokaJob job) => new CssBuilder("moka-job-list__item")
		.AddClass(job.State switch
		{
			MokaJobState.Running => "moka-job-list__item--running",
			MokaJobState.Done => "moka-job-list__item--done",
			MokaJobState.Failed => "moka-job-list__item--failed",
			MokaJobState.Canceled => "moka-job-list__item--canceled",
			_ => "moka-job-list__item--queued"
		})
		.Build();

	private static string TrackClass(double? percent) => new CssBuilder("moka-job-list__track")
		.AddClass("moka-job-list__track--waiting", percent is null)
		.Build();

	private static MokaColor StateColor(MokaJobState state) => state switch
	{
		MokaJobState.Running => MokaColor.Info,
		MokaJobState.Done => MokaColor.Success,
		MokaJobState.Failed => MokaColor.Error,
		_ => MokaColor.Surface
	};

	private string StateWord(MokaJobState state) => state switch
	{
		MokaJobState.Running => Text.Running,
		MokaJobState.Done => Text.Done,
		MokaJobState.Failed => Text.Failed,
		MokaJobState.Canceled => Text.Canceled,
		_ => Text.Queued
	};

	private string FailureText(MokaJob job) => string.IsNullOrWhiteSpace(job.Error) ? Text.NoReason : job.Error;

	private static double? PercentOf(MokaJob job)
	{
		if (job.Percent is { } percent && double.IsFinite(percent))
		{
			return Math.Clamp(percent, 0, 100);
		}

		return job is { Bytes: >= 0, TotalBytes: > 0 } ? Math.Clamp(100d * job.Bytes.Value / job.TotalBytes.Value, 0, 100) : null;
	}

	// Rounded down, so the words never say 100% before the job is done.
	private static int? WholePercent(double? percent) => percent is { } value ? (int)Math.Floor(value) : null;

	private static string? ValueNow(double? percent) => WholePercent(percent)?.ToString(CultureInfo.InvariantCulture);

	private static string FormatPercent(int percent) => percent.ToString(CultureInfo.CurrentCulture) + "%";

	// A transform moves on the compositor, so a tick costs no layout.
	private static string? FillStyle(double? percent) => percent is { } value
		? new StyleBuilder().AddStyle("transform", $"scaleX({(value / 100).ToString("0.####", CultureInfo.InvariantCulture)})").Build()
		: null;

	private string? ValueText(MokaJob job, double? percent)
	{
		string? amount = AmountText(job);
		if (WholePercent(percent) is not { } whole)
		{
			return amount;
		}

		return amount is null ? FormatPercent(whole) : $"{FormatPercent(whole)}, {amount}";
	}

	private string? MetaText(MokaJob job)
	{
		List<string> facts = new(5);
		AddFact(facts, job.Detail);

		switch (job.State)
		{
			case MokaJobState.Running:
				AddRunningFacts(facts, job);
				break;
			case MokaJobState.Canceled:
				AddFact(facts, AmountText(job));
				facts.Add(StateWord(job.State));
				break;
			default:
				AddFact(facts, WholeText(job));
				facts.Add(StateWord(job.State));
				break;
		}

		return facts.Count == 0 ? null : string.Join(Text.Separator, facts);
	}

	private void AddRunningFacts(List<string> facts, MokaJob job)
	{
		AddFact(facts, AmountText(job));
		if (job.TotalBytes is null or <= 0 && WholePercent(PercentOf(job)) is { } whole)
		{
			facts.Add(FormatPercent(whole));
		}

		if (job.BytesPerSecond is { } rate && double.IsFinite(rate) && rate > 0)
		{
			facts.Add($"{SizeText(rate)}/s");
		}

		if (job.TimeRemaining is { } left && left > TimeSpan.Zero)
		{
			facts.Add(Text.TimeLeft(left));
		}
	}

	private static void AddFact(List<string> facts, string? fact)
	{
		if (!string.IsNullOrWhiteSpace(fact))
		{
			facts.Add(fact);
		}
	}

	// Says a unit the two sides share once, as MokaJobProgress does: "148 of 240 MB", but "900 KB of 1.2 GB".
	private string? AmountText(MokaJob job)
	{
		if (job.Bytes is not { } done || done < 0)
		{
			return job.TotalBytes is > 0 ? SizeText(job.TotalBytes.Value) : null;
		}

		if (job.TotalBytes is not { } total || total <= 0)
		{
			return done == 0 ? null : SizeText(done);
		}

		if (ByteFormatter is { } format)
		{
			return Text.Amount(format(done), format(total));
		}

		(string doneText, string totalText) = MokaByteFormat.Pair(done, total, ByteUnits);
		return Text.Amount(doneText, totalText);
	}

	private string? WholeText(MokaJob job)
	{
		if (job.TotalBytes is > 0)
		{
			return SizeText(job.TotalBytes.Value);
		}

		return job.Bytes is > 0 ? SizeText(job.Bytes.Value) : null;
	}

	private string SizeText(double bytes) => ByteFormatter is { } format
		? format(bytes)
		: MokaByteFormat.Format(bytes, ByteUnits);

	private readonly record struct PressedJob(string Id, MokaJobState State, int Index);

	private readonly record struct RowSettings(
		MokaJobListText? Text,
		MokaByteUnits Units,
		Func<double, string>? Formatter,
		bool Disabled,
		bool CanCancel,
		bool CanRetry,
		bool CanRemove,
		bool HasSlots);
}
