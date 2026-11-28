using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.Data.Chart;
using Moka.Red.Feedback.NotificationBell;
using Moka.Red.Feedback.Toast;
using Moka.Red.Icons;
using Moka.Red.Navigation.Tabs;

namespace Moka.Red.ThemeGen.Preview;

/// <summary>
///     A small app made of the library's own components, drawn in <see cref="PreviewTheme" /> through a real
///     <see cref="MokaThemeProvider" />: a page header, tabs, buttons in every variant, fields, a select, a switch and a
///     checkbox, alerts, cards, a table with a selected row, a chart, tags and labels, a raised card holding a side menu
///     with the current project, a settings section, a notification list drawn in place, a small dock layout, a dialog and
///     toasts that open inside it, and a status bar whose sync item has a tooltip. Every style choice a theme holds shows
///     somewhere in it: those for dialogs, toasts, lists, popups and tooltips once they open, and chart print only in
///     print. Nested in an app's own provider, it writes only the tokens that differ from the app's theme, and its own
///     buttons take the previewed button looks, which only an app's outermost provider writes.
/// </summary>
/// <remarks>
///     The dialog and toasts stay inside the preview: its overlay area contains layout, so their fixed positions are taken
///     from it rather than from the window. Toasts need <c>AddMokaFeedback()</c>, and the preview's toast host shows every
///     toast the app raises while it is on the page, since the toast service is one per app.
/// </remarks>
public partial class MokaThemePreview
{
	private static readonly MokaLabelColor[] LabelCycle =
	[
		MokaLabelColor.Violet, MokaLabelColor.Teal, MokaLabelColor.Amber, MokaLabelColor.Sky, MokaLabelColor.Pink, MokaLabelColor.Lime
	];

	private bool _blocking = true;
	private bool _chip = true;
	private bool _digest = true;
	private bool _dialogOpen;
	private string? _file;
	private MokaThemePreviewText? _lastText;
	private string? _name;
	private List<MokaNotificationBellItem> _notices = [];
	private bool _grid = true;
	private bool _notify = true;
	private bool _pinned = true;
	private string? _owner;
	private string? _project;
	private List<TaskRow> _rows = [];
	private HashSet<TaskRow> _selected = [];
	private List<MokaBarSeries> _series = [];
	private string? _tab;
	private List<MokaTabItem> _tabs = [];
	private IMokaToastService? _toasts;

	[Inject]
	private IServiceProvider Services { get; set; } = default!;

	/// <summary>The theme to draw the sample in. Named apart from the inherited <c>Theme</c>, which reads the cascaded theme.</summary>
	[Parameter]
	public MokaTheme PreviewTheme { get; set; } = MokaTheme.Default;

	/// <summary>Every word the sample writes.</summary>
	[Parameter]
	public MokaThemePreviewText Text { get; set; } = new();

	/// <summary>A content security policy nonce for the preview's <see cref="MokaThemeProvider" />. Pass the app's own.</summary>
	[Parameter]
	public string? Nonce { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-theme-preview";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		base.OnInitialized();
		_toasts = Services.GetService<IMokaToastService>();
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		if (ReferenceEquals(_lastText, Text))
		{
			return;
		}

		_lastText = Text;
		_name = Text.NameValue;
		_owner = Text.People.Count > 0 ? Text.People[0] : null;
		_tab = Text.Overview;
		_project = Text.Projects.Count > 0 ? Text.Projects[0] : null;
		_file = Text.WorkspaceFiles.Count > 0 ? Text.WorkspaceFiles[0] : null;

		// Two from today and two from before, the first two unread, so the grouped and unread looks have something to show.
		DateTime now = (Services.GetService<TimeProvider>() ?? TimeProvider.System).GetUtcNow().UtcDateTime;
		TimeSpan[] ages = [TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(40), TimeSpan.FromDays(1), TimeSpan.FromDays(2)];
		MokaIconDefinition[] icons = [MokaIcons.Status.CheckCircle, MokaIcons.Content.Comment, MokaIcons.Action.Upload, MokaIcons.Status.Bell];
		_notices = [.. Text.Notices.Select((notice, i) => new MokaNotificationBellItem(Guid.NewGuid(), notice.Title, notice.Message,
			now - ages[i % ages.Length], Read: i >= 2, Icon: icons[i % icons.Length]))];
		_tabs =
		[
			new MokaTabItem(Text.Overview, Text.Overview),
			new MokaTabItem(Text.Tasks, Text.Tasks, 12),
			new MokaTabItem(Text.Files, Text.Files),
			new MokaTabItem(Text.Archive, Text.Archive, Disabled: true)
		];

		(string Status, MokaColor Tone)[] statuses =
		[
			(Text.StatusDone, MokaColor.Success), (Text.StatusDoing, MokaColor.Info), (Text.StatusReview, MokaColor.Warning),
			(Text.StatusBlocked, MokaColor.Error)
		];
		_rows = [.. Text.RowTasks.Select((task, i) => new TaskRow(task, statuses[i % statuses.Length].Status, statuses[i % statuses.Length].Tone,
			Text.People.Count > 0 ? Text.People[i % Text.People.Count] : "", 3 + (i * 2 % 7)))];
		_selected = _rows.Count > 1 ? [_rows[1]] : [];

		_series =
		[
			new MokaBarSeries { Name = Text.ThisRelease, Values = [.. Text.ChartLabels.Select((_, i) => (double)(4 + (i * 3 % 7)))] },
			new MokaBarSeries { Name = Text.LastRelease, Values = [.. Text.ChartLabels.Select((_, i) => (double)(3 + (i * 5 % 6)))] }
		];
	}

	// A nested provider leaves button looks to the app's outermost one, whose rules reach every button under it, so the
	// sample's own buttons carry the previewed theme's looks themselves, as MokaButton.Look is there for.
	private MokaButtonLook ButtonLook => PreviewTheme.Buttons;

	private MokaDisabledButtonLook DisabledButtonLook => PreviewTheme.DisabledButtons;

	private MokaPressedButtonLook PressedButtonLook => PreviewTheme.PressedButtons;

	private MokaPressedButtonFill PressedButtonFill => PreviewTheme.PressedButtonFill;

	private int UnreadNotices => _notices.Count(notice => !notice.Read);

	private static MokaLabelColor LabelFor(int index) => LabelCycle[index % LabelCycle.Length];

	private void MarkNoticesRead() => _notices = [.. _notices.Select(notice => notice with { Read = true })];

	private void OpenDialog() => _dialogOpen = true;

	private void CloseDialog() => _dialogOpen = false;

	private void ShowToast() => _toasts?.ShowSuccess(Text.ToastMessage);

	private sealed record TaskRow(string Task, string Status, MokaColor Tone, string Owner, int Points);
}
