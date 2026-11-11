using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;
using Moka.Red.Navigation.Tabs;

namespace Moka.Red.DevApp.Components.Shared;

public partial class DemoSection
{
	private const string ExampleTab = "example";
	private const string CodeTab = "code";
	private const string NotesTab = "notes";
	private const string PlaygroundTab = "playground";

	private static readonly MokaTabItem _exampleItem = new(ExampleTab, "Example");
	private static readonly MokaTabItem _codeItem = new(CodeTab, "Code");
	private static readonly MokaTabItem _notesItem = new(NotesTab, "Notes");
	private static readonly MokaTabItem _playgroundItem = new(PlaygroundTab, "Playground");
	private static readonly IReadOnlyList<MokaTabItem> _plainTabs = [_exampleItem, _codeItem];
	private static readonly IReadOnlyList<MokaTabItem> _notesTabs = [_exampleItem, _codeItem, _notesItem];
	private static readonly IReadOnlyList<MokaTabItem> _playgroundTabs = [_exampleItem, _codeItem, _playgroundItem];
	private static readonly IReadOnlyList<MokaTabItem> _allTabs = [_exampleItem, _codeItem, _notesItem, _playgroundItem];

	private bool _foldOpen;
	private string? _source;
	private bool _sourceRead;
	private string _tab = ExampleTab;

	[CascadingParameter]
	private DemoGroup? Group { get; set; }

	[Parameter]
	[EditorRequired]
	public string Id { get; set; } = string.Empty;

	[Parameter]
	[EditorRequired]
	public string Title { get; set; } = string.Empty;

	[Parameter]
	public string? Description { get; set; }

	[Parameter]
	public string? Component { get; set; }

	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	[Parameter]
	public string? Code { get; set; }

	[Parameter]
	public RenderFragment? Notes { get; set; }

	[Parameter]
	public string? Tip { get; set; }

	[Parameter]
	public RenderFragment? Playground { get; set; }

	private IReadOnlyList<MokaTabItem> Tabs => (Notes is not null, Playground is not null) switch
	{
		(true, true) => _allTabs,
		(true, false) => _notesTabs,
		(false, true) => _playgroundTabs,
		_ => _plainTabs
	};

	private bool ExampleShown => _tab is ExampleTab or NotesTab;

	private bool CopyShown => _tab is ExampleTab or CodeTab;

	private string CardClass => new CssBuilder("demo-section__card")
		.AddClass("demo-section__card--copy", CopyShown)
		.Build();

	private string BodyClass => $"demo-section__body demo-section__body--{_tab}";

	private string FoldLabel => LineCount == 1 ? "1 line" : $"{LineCount} lines";

	private int LineCount => SourceCode() is { Length: > 0 } source ? source.Count(c => c == '\n') + 1 : 0;

	private string SourceCode()
	{
		if (Code is not null)
		{
			return Code;
		}

		if (!_sourceRead)
		{
			_source = DemoSources.Find(DemoSources.OwnerOf(ChildContent ?? Notes ?? Playground), Id);
			_sourceRead = true;
		}

		return _source ?? string.Empty;
	}

	private void Show(string tab) => _tab = tab;

	private void ToggleFold() => _foldOpen = !_foldOpen;
}
