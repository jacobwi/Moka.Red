using Moka.Red.Feedback.NotificationBell;
using Moka.Red.Layout.DockLayout;

namespace Moka.Red.ThemeGen.Preview;

/// <summary>Every word <see cref="MokaThemePreview" /> writes in its sample app. Change any with <c>new MokaThemePreviewText { ... }</c>.</summary>
public sealed record MokaThemePreviewText
{
	/// <summary>The accessible name of the sample, which is a region of its own.</summary>
	public string Region { get; init; } = "Theme preview";

	/// <summary>The page header's eyebrow.</summary>
	public string Eyebrow { get; init; } = "Projects";

	/// <summary>The page header's title.</summary>
	public string Title { get; init; } = "Spring release";

	/// <summary>The line under the title saying what is so now.</summary>
	public string Said { get; init; } = "12 tasks open, 3 due this week";

	/// <summary>The line under the title saying what the page is for.</summary>
	public string Lead { get; init; } = "Plan the work, track who has what, and ship on time.";

	/// <summary>The header's secondary action.</summary>
	public string Share { get; init; } = "Share";

	/// <summary>The header's main action.</summary>
	public string NewTask { get; init; } = "New task";

	/// <summary>The tabs' accessible name.</summary>
	public string TabsLabel { get; init; } = "Project sections";

	/// <summary>The first tab.</summary>
	public string Overview { get; init; } = "Overview";

	/// <summary>The second tab.</summary>
	public string Tasks { get; init; } = "Tasks";

	/// <summary>The third tab.</summary>
	public string Files { get; init; } = "Files";

	/// <summary>The fourth tab, shown disabled.</summary>
	public string Archive { get; init; } = "Archive";

	/// <summary>The buttons section's heading.</summary>
	public string ButtonsHeading { get; init; } = "Buttons";

	/// <summary>The filled button.</summary>
	public string Filled { get; init; } = "Filled";

	/// <summary>The outlined button.</summary>
	public string Outlined { get; init; } = "Outlined";

	/// <summary>The soft button.</summary>
	public string Soft { get; init; } = "Soft";

	/// <summary>The text button.</summary>
	public string TextButton { get; init; } = "Text";

	/// <summary>The second-accent button.</summary>
	public string Secondary { get; init; } = "Secondary";

	/// <summary>The destructive button.</summary>
	public string Delete { get; init; } = "Delete";

	/// <summary>The disabled button.</summary>
	public string Disabled { get; init; } = "Disabled";

	/// <summary>The busy button.</summary>
	public string Saving { get; init; } = "Saving";

	/// <summary>The form card's title.</summary>
	public string FormHeading { get; init; } = "Task details";

	/// <summary>The name field's label.</summary>
	public string NameLabel { get; init; } = "Name";

	/// <summary>The name field's starting value.</summary>
	public string NameValue { get; init; } = "Write release notes";

	/// <summary>The field shown in its error state.</summary>
	public string DueLabel { get; init; } = "Due date";

	/// <summary>The error under that field.</summary>
	public string DueError { get; init; } = "Pick a date after today";

	/// <summary>The select's label.</summary>
	public string OwnerLabel { get; init; } = "Owner";

	/// <summary>The people the select offers, who also own the table's tasks in turn.</summary>
	public IReadOnlyList<string> People { get; init; } = ["Ada Park", "Lena Ruiz", "Omar Haddad", "Mia Chen"];

	/// <summary>The switch's label.</summary>
	public string NotifyLabel { get; init; } = "Notify the team";

	/// <summary>The checkbox's label.</summary>
	public string BlockingLabel { get; init; } = "Blocks the release";

	/// <summary>The cards section's heading.</summary>
	public string CardsHeading { get; init; } = "This week";

	/// <summary>The first card's title.</summary>
	public string DoneTitle { get; init; } = "Done";

	/// <summary>The first card's value.</summary>
	public string DoneValue { get; init; } = "28";

	/// <summary>The line under the first card's value.</summary>
	public string DoneNote { get; init; } = "Up 6 from last week";

	/// <summary>The second card's title.</summary>
	public string OpenTitle { get; init; } = "Open";

	/// <summary>The second card's value.</summary>
	public string OpenValue { get; init; } = "12";

	/// <summary>The line under the second card's value.</summary>
	public string OpenNote { get; init; } = "3 due this week";

	/// <summary>The third card's title.</summary>
	public string LateTitle { get; init; } = "Late";

	/// <summary>The third card's value.</summary>
	public string LateValue { get; init; } = "2";

	/// <summary>The line under the third card's value.</summary>
	public string LateNote { get; init; } = "Both waiting on review";

	/// <summary>The table's accessible name.</summary>
	public string TableLabel { get; init; } = "Tasks";

	/// <summary>The table's first column, the task.</summary>
	public string TaskColumn { get; init; } = "Task";

	/// <summary>The table's second column, the status.</summary>
	public string StatusColumn { get; init; } = "Status";

	/// <summary>The table's third column, the owner.</summary>
	public string OwnerColumn { get; init; } = "Owner";

	/// <summary>The table's fourth column, the points.</summary>
	public string PointsColumn { get; init; } = "Points";

	/// <summary>The table's tasks, one row each. Their owners come from <see cref="People" /> in turn.</summary>
	public IReadOnlyList<string> RowTasks { get; init; } = ["Draft the changelog", "Update screenshots", "Review the API", "Fix the sign-in bug"];

	/// <summary>The status of a finished task.</summary>
	public string StatusDone { get; init; } = "Done";

	/// <summary>The status of a task under way.</summary>
	public string StatusDoing { get; init; } = "In progress";

	/// <summary>The status of a task waiting on review.</summary>
	public string StatusReview { get; init; } = "Review";

	/// <summary>The status of a blocked task.</summary>
	public string StatusBlocked { get; init; } = "Blocked";

	/// <summary>The alerts section's heading.</summary>
	public string AlertsHeading { get; init; } = "Alerts";

	/// <summary>The info alert.</summary>
	public string InfoAlert { get; init; } = "The release branch opens on Monday.";

	/// <summary>The success alert.</summary>
	public string SuccessAlert { get; init; } = "All checks passed.";

	/// <summary>The warning alert.</summary>
	public string WarningAlert { get; init; } = "Two tasks have no owner.";

	/// <summary>The error alert.</summary>
	public string ErrorAlert { get; init; } = "The nightly build failed.";

	/// <summary>The chart's title and accessible name.</summary>
	public string ChartTitle { get; init; } = "Tasks closed";

	/// <summary>The chart's first series.</summary>
	public string ThisRelease { get; init; } = "This release";

	/// <summary>The chart's second series.</summary>
	public string LastRelease { get; init; } = "Last release";

	/// <summary>The chart's labels along its axis.</summary>
	public IReadOnlyList<string> ChartLabels { get; init; } = ["Mon", "Tue", "Wed", "Thu", "Fri"];

	/// <summary>The tags section's heading.</summary>
	public string TagsHeading { get; init; } = "Tags and labels";

	/// <summary>The words on the label tags, one per label color in turn.</summary>
	public IReadOnlyList<string> Tags { get; init; } = ["Design", "Backend", "Docs", "Research", "Later", "Ops"];

	/// <summary>The chip.</summary>
	public string Chip { get; init; } = "Assigned to me";

	/// <summary>The tag drawn as new.</summary>
	public string NewTag { get; init; } = "New";

	/// <summary>The title of the card holding the side menu, which also names the menu.</summary>
	public string ProjectsTitle { get; init; } = "Projects";

	/// <summary>The side menu's entries. The first is the current one until another is picked.</summary>
	public IReadOnlyList<string> Projects { get; init; } = ["Spring release", "Website refresh", "Mobile app", "Design system"];

	/// <summary>The settings section's title.</summary>
	public string SettingsTitle { get; init; } = "Notifications";

	/// <summary>The settings section's line saying how things are set now.</summary>
	public string SettingsNow { get; init; } = "Email and in-app, with a summary at 9:00";

	/// <summary>The word screen readers hear before that line.</summary>
	public string NowLabel { get; init; } = "Now:";

	/// <summary>The settings section's switch.</summary>
	public string DigestLabel { get; init; } = "Send a daily summary";

	/// <summary>The notifications in the list drawn in place, newest first. The first two are unread and from today.</summary>
	public IReadOnlyList<MokaThemePreviewNotice> Notices { get; init; } =
	[
		new("Build passed", "The release build finished in 4 minutes."),
		new("New comment", "Lena replied on Update screenshots."),
		new("File uploaded", "design-notes.pdf was added to Files."),
		new("Reminder", "The review is on Thursday at 10:00.")
	];

	/// <summary>Every word the notification list writes around the notifications.</summary>
	public MokaNotificationBellText Bell { get; init; } = new();

	/// <summary>The heading over the dock layout.</summary>
	public string WorkspaceHeading { get; init; } = "Workspace";

	/// <summary>The dock layout's files panel, which lists <see cref="WorkspaceFiles" />.</summary>
	public string FilesPanel { get; init; } = "Files";

	/// <summary>The files in the files panel. The first one shows in the main area until another is picked.</summary>
	public IReadOnlyList<string> WorkspaceFiles { get; init; } = ["release-notes.md", "roadmap.md", "budget.csv"];

	/// <summary>The dock layout's second tab beside the files.</summary>
	public string OutlinePanel { get; init; } = "Outline";

	/// <summary>The outline panel's text.</summary>
	public string OutlineBody { get; init; } = "Highlights, fixes, and what comes next.";

	/// <summary>The text of the file showing in the main area.</summary>
	public string DocumentBody { get; init; } = "Drag a tab to another group, or undock it into a window of its own.";

	/// <summary>The dock layout's panel along the bottom.</summary>
	public string OutputPanel { get; init; } = "Output";

	/// <summary>The output panel's line.</summary>
	public string OutputLine { get; init; } = "Build succeeded, 0 warnings";

	/// <summary>Every word the dock layout writes, such as its header buttons and menu.</summary>
	public MokaDockText Dock { get; init; } = MokaDockText.Default;

	/// <summary>The status bar's accessible name.</summary>
	public string StatusBarLabel { get; init; } = "Project status";

	/// <summary>The status bar's sync item.</summary>
	public string Synced { get; init; } = "Synced";

	/// <summary>The sync item's tooltip.</summary>
	public string SyncedTooltip { get; init; } = "Saved to the server a minute ago";

	/// <summary>The status bar's item at the end.</summary>
	public string Version { get; init; } = "v2.3";

	/// <summary>The overlays section's heading.</summary>
	public string OverlaysHeading { get; init; } = "Dialog and toast";

	/// <summary>The button that opens the dialog.</summary>
	public string OpenDialog { get; init; } = "Open dialog";

	/// <summary>The button that shows a toast.</summary>
	public string ShowToast { get; init; } = "Show toast";

	/// <summary>The toast's message.</summary>
	public string ToastMessage { get; init; } = "Task saved.";

	/// <summary>The dialog's title.</summary>
	public string DialogTitle { get; init; } = "Archive this task?";

	/// <summary>The dialog's text.</summary>
	public string DialogBody { get; init; } = "It leaves the board, and you can bring it back from Archive.";

	/// <summary>The dialog's cancel button.</summary>
	public string Cancel { get; init; } = "Cancel";

	/// <summary>The dialog's confirm button.</summary>
	public string Confirm { get; init; } = "Archive";
}
