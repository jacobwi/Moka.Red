using Moka.Red.Primitives.DataList;
using Moka.Red.Primitives.Meter;

namespace Moka.Red.DevApp.Components.Pages;

public partial class PrimitivesPage
{
	private static readonly string[] _materialColors =
	[
		"#f44336", "#e91e63", "#9c27b0", "#673ab7", "#3f51b5", "#2196f3", "#03a9f4", "#00bcd4",
		"#009688", "#4caf50", "#8bc34a", "#cddc39", "#ffeb3b", "#ffc107", "#ff9800", "#ff5722"
	];

	// Steps demo state
	private static readonly string[] _stepsLabels = ["Cart", "Shipping", "Payment", "Confirm"];

	// Meter demo state
	private static readonly MokaMeterSegment[] _meterSegments =
	[
		new(0, 33, "var(--moka-color-success)"),
		new(33, 66, "var(--moka-color-warning)"),
		new(66, 100, "var(--moka-color-error)")
	];

	// DataList demo state
	private static readonly IReadOnlyList<MokaDataListItem> _dataListItems = new[]
	{
		new MokaDataListItem("Name", "Jane Doe"),
		new MokaDataListItem("Email", "jane@example.com"),
		new MokaDataListItem("Role", "Administrator"),
		new MokaDataListItem("Joined", "March 2024")
	};

	// CodeBlock demo state
	private readonly string _codeBlockSample =
		"public class Example\n{\n    public static void Main(string[] args)\n    {\n        Console.WriteLine(\"Hello, Moka.Red!\");\n    }\n}";

	private readonly List<string> _sortableHandleItems =
		["High priority", "Medium priority", "Low priority", "Backlog"];

	// Sortable demo
	private readonly List<string> _sortableItems =
		["Design mockups", "Write tests", "Code review", "Deploy to staging", "Update docs"];

	// TransferList demo state
	private readonly IList<string> _transferAvailable = new List<string> { "C#", "TypeScript", "Python", "Rust", "Go" };
	private readonly IList<string> _transferSelected = new List<string>();

	// Badge demo state
	private int _badgeCount = 3;
	private bool _chipBusiness;
	private bool _chipDesign;
	private bool _chipScience;

	// Chip demo state
	private bool _chipTech = true;
	private bool _chipPinned;
	private readonly List<string> _chipTags = ["Blazor", "C#", "CSS"];
	private bool _chipAudit;
	private bool _chipAuditLoose;
	private string _chipAuditNote = "Turn it on, then try to turn it off.";

	private void SetAudit(bool on)
	{
		if (on)
		{
			_chipAudit = true;
			_chipAuditNote = "On.";
		}
		else
		{
			_chipAuditNote = "Refused: the audit log stays on.";
		}
	}

	private void SetAuditLoose(bool on)
	{
		if (on)
		{
			_chipAuditLoose = true;
		}
	}

	// Controlled segmented control demo: Team takes a moment to save, Enterprise is refused.
	private string _segmentPlan = "free";
	private string _segmentPlanNote = "Free";

	private async Task SetPlan(string plan)
	{
		if (plan == "enterprise")
		{
			_segmentPlanNote = "Refused: Enterprise needs a sales call.";
			return;
		}

		if (plan == "team")
		{
			_segmentPlanNote = "Saving Team...";
			StateHasChanged();
			await Task.Delay(800);
		}

		_segmentPlan = plan;
		_segmentPlanNote = plan == "team" ? "Team" : "Free";
	}
	private int _clickableStep = 1;

	// Confetti demo state
	private bool _confettiActive;
	private bool _filterActive;

	// Attribute filter demo state
	private bool _filterAll = true;
	private bool _filterError;
	private bool _filterPending;
	private string _segmentTab = "overview";

	// Segmented control demo state
	private string _segmentValue = "daily";
	private string _segmentView = "list";

	// Color swatch demo
	private string? _selectedSwatchColor = "#d32f2f";

	private string? _selectedSwatchColor2;

	// SplitButton demo state
	private string _splitButtonStatus = "Click an action...";

	// SwipeActions demo state
	private string _swipeStatus = "Swipe the item to reveal actions";

	// Number Ticker demo state
	private double _tickerValue;

	private void HandleSortReorder((int OldIndex, int NewIndex) e) => StateHasChanged();

	private void HandleSortReorder2((int OldIndex, int NewIndex) e) => StateHasChanged();

	private void HandleSwipeDelete() => _swipeStatus = "Delete action triggered!";

	private void HandleSwipeArchive() => _swipeStatus = "Archive action triggered!";
}
