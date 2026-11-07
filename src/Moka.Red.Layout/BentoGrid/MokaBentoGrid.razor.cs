using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.BentoGrid;

/// <summary>
///     An asymmetric bento-box grid layout (like Apple marketing pages).
///     Children are placed using CSS grid with configurable column/row spans.
///     Use <see cref="MokaBentoItem" /> for individual cells with span control.
///     With <see cref="Board" /> set it is a board the person arranges: cards they reorder, resize and hide.
/// </summary>
public partial class MokaBentoGrid : MokaComponentBase
{
	private const string JsModulePath = "./_content/Moka.Red.Layout/BentoGrid/MokaBentoGrid.razor.js";
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	private static readonly Dictionary<string, object?>[] GripKeys =
	[
		new()
		{
			["selector"] = "[data-tool=\"grip\"]",
			["keys"] = new[] { "ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown" },
			["unlessModified"] = true
		}
	];

	private IReadOnlyList<MokaBoardCard>? _board;
	private IReadOnlyList<MokaBoardCard>? _lastBoardParameter;
	private bool _customizing;
	private bool _lastCustomizingParameter;
	private ElementReference _root;
	private string? _boundRoot;
	private DotNetObjectReference<MokaBentoGrid>? _dotNetRef;
	private (string? Card, string Part)? _focus;
	private string? _trayLabelId;

	/// <summary>Grid items (use <see cref="MokaBentoItem" /> children). Not drawn on a board.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Number of columns. Default 4. A board always has 4, 2 below 1180 px and 1 on a phone.</summary>
	[Parameter]
	public int Columns { get; set; } = 4;

	/// <summary>Gap between cells. Default Md. Not used on a board.</summary>
	[Parameter]
	public MokaSpacingScale Gap { get; set; } = MokaSpacingScale.Md;

	/// <summary>Custom gap override. Not used on a board.</summary>
	[Parameter]
	public string? GapValue { get; set; }

	/// <summary>Auto row height. Default "auto". Not used on a board.</summary>
	[Parameter]
	public string RowHeight { get; set; } = "auto";

	/// <summary>Minimum row height. Default "120px". Not used on a board.</summary>
	[Parameter]
	public string MinRowHeight { get; set; } = "120px";

	/// <summary>
	///     Makes the grid a board: the cards it shows, in this order, with their sizes, and the hidden ones the tray
	///     holds. Each card is drawn by <see cref="CardTemplate" />. A change the person makes draws at once and is
	///     raised through <see cref="BoardChanged" />. A new list whose cards differ from the last one passed replaces
	///     the board, so an app that refuses a change puts it back by passing the layout it kept.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaBoardCard>? Board { get; set; }

	/// <summary>Raised with the whole new layout, hidden cards included, after every change. Storing it is the app's job.</summary>
	[Parameter]
	public EventCallback<IReadOnlyList<MokaBoardCard>> BoardChanged { get; set; }

	/// <summary>Draws one card of the board. Put a <see cref="MokaBentoItem" /> with <c>Card</c> set in it.</summary>
	[Parameter]
	public RenderFragment<MokaBoardCard>? CardTemplate { get; set; }

	/// <summary>The layout "Back to the default" puts back. Without it the button isn't drawn.</summary>
	[Parameter]
	public IReadOnlyList<MokaBoardCard>? DefaultBoard { get; set; }

	/// <summary>
	///     Customize mode: a row of tools on each card, a dashed ring around it, the tray of hidden cards, and
	///     "Back to the default" and "Done" above them.
	/// </summary>
	[Parameter]
	public bool Customizing { get; set; }

	/// <summary>Raised when "Done" ends customize mode.</summary>
	[Parameter]
	public EventCallback<bool> CustomizingChanged { get; set; }

	/// <summary>Names a card by its id, for the tray and for a card with no <c>Title</c> of its own. Default: the id.</summary>
	[Parameter]
	public Func<string, string>? CardTitle { get; set; }

	/// <summary>Content at the start of the customize bar, such as whether the last change was saved.</summary>
	[Parameter]
	public RenderFragment? CustomizeContent { get; set; }

	/// <summary>Text of the button that ends customize mode. Default "Done".</summary>
	[Parameter]
	public string DoneText { get; set; } = "Done";

	/// <summary>Text of the button that puts <see cref="DefaultBoard" /> back. Default "Back to the default".</summary>
	[Parameter]
	public string ResetText { get; set; } = "Back to the default";

	/// <summary>The label over the tray of hidden cards. Default "Hidden".</summary>
	[Parameter]
	public string TrayLabel { get; set; } = "Hidden";

	/// <summary>What the tray says when no card is hidden. Default "Nothing hidden".</summary>
	[Parameter]
	public string TrayEmptyText { get; set; } = "Nothing hidden";

	/// <inheritdoc />
	protected override string RootClass => "moka-bento-grid";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-bento-grid--board", _board is not null)
		.AddClass("moka-bento-grid--customizing", IsCustomizing)
		.AddClass(Class)
		.Build();

	private string ResolvedGap => GapValue ?? MokaEnumHelpers.ToCssValue(Gap);

	/// <inheritdoc />
	protected override string? CssStyle => _board is not null
		? new StyleBuilder().AddStyle(Style).Build()
		: new StyleBuilder()
			.AddStyle("grid-template-columns", $"repeat({Columns}, 1fr)")
			.AddStyle("grid-auto-rows", RowHeight == "auto" ? $"minmax({MinRowHeight}, auto)" : RowHeight)
			.AddStyle("gap", ResolvedGap)
			.AddStyle(Style)
			.Build();

	internal bool IsCustomizing => _board is not null && _customizing;

	private IEnumerable<MokaBoardCard> ShownCards => _board?.Where(c => !c.Hidden) ?? [];

	private IEnumerable<MokaBoardCard> HiddenCards => _board?.Where(c => c.Hidden) ?? [];

	private bool HasHidden => _board?.Any(c => c.Hidden) ?? false;

	private bool IsDefault => DefaultBoard is not null && _board is not null && _board.SequenceEqual(Normalize(DefaultBoard));

	private string TrayLabelId => _trayLabelId ??= $"moka-board-{Guid.NewGuid():N}-tray";

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (!SameCards(Board, _lastBoardParameter))
		{
			_board = Board is null ? null : Normalize(Board);
		}

		_lastBoardParameter = Board;

		if (Customizing != _lastCustomizingParameter)
		{
			_lastCustomizingParameter = Customizing;
			_customizing = Customizing;
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (_board is null)
		{
			return;
		}

		if (_customizing && _boundRoot != _root.Id)
		{
			_boundRoot = _root.Id;
			_dotNetRef ??= DotNetObjectReference.Create(this);
			await SafeModuleInvokeVoidAsync(KeysModule, "preventKeys", _root, GripKeys);
			await SafeModuleInvokeVoidAsync(JsModulePath, "bindBoard", _root, _dotNetRef);
		}

		if (_focus is { } focus)
		{
			_focus = null;
			await SafeModuleInvokeVoidAsync(JsModulePath, "focusBoard", _root, focus.Card, focus.Part);
		}
	}

	/// <summary>
	///     Called from the browser when a card dragged by its grip is dropped: <paramref name="id" /> goes just before
	///     or just after <paramref name="target" />.
	/// </summary>
	/// <param name="id">The card that was dragged.</param>
	/// <param name="target">The card it was dropped on.</param>
	/// <param name="before">Whether it lands before the target rather than after it.</param>
	[JSInvokable]
	public Task Dropped(string id, string target, bool before)
	{
		if (IsCustomizing && Move(_board!, id, target, before) is { } next)
		{
			RunDetached(() => ApplyAsync(next, null));
		}

		return Task.CompletedTask;
	}

	internal MokaBoardCard? Find(string id) => _board?.FirstOrDefault(c => c.Id == id);

	internal string TitleOf(string id) => CardTitle?.Invoke(id) ?? id;

	internal bool IsFirstShown(string id) => ShownCards.FirstOrDefault()?.Id == id;

	internal bool IsLastShown(string id) => ShownCards.LastOrDefault()?.Id == id;

	internal Task StepAsync(string id, int by, string focusPart) =>
		IsCustomizing && Step(_board!, id, by) is { } next ? ApplyAsync(next, (id, focusPart)) : Task.CompletedTask;

	internal Task ResizeAsync(string id)
	{
		if (!IsCustomizing || Find(id) is not { } card)
		{
			return Task.CompletedTask;
		}

		MokaBoardSize size = card.Size switch
		{
			MokaBoardSize.Small => MokaBoardSize.Wide,
			MokaBoardSize.Wide => MokaBoardSize.Full,
			_ => MokaBoardSize.Small
		};
		return ApplyAsync(Replace(_board!, card with { Size = size }), null);
	}

	internal Task HideAsync(string id) =>
		IsCustomizing && Find(id) is { Hidden: false } card
			? ApplyAsync(Replace(_board!, card with { Hidden = true }), (id, "tray"))
			: Task.CompletedTask;

	private Task ShowAsync(string id)
	{
		if (!IsCustomizing || Find(id) is not { Hidden: true } card)
		{
			return Task.CompletedTask;
		}

		List<MokaBoardCard> next = [.. _board!.Where(c => c.Id != id)];
		next.Insert(next.FindLastIndex(c => !c.Hidden) + 1, card with { Hidden = false });
		return ApplyAsync(next, (id, "grip"));
	}

	private Task ResetAsync() =>
		IsCustomizing && DefaultBoard is not null && !IsDefault
			? ApplyAsync([.. Normalize(DefaultBoard)], (null, "done"))
			: Task.CompletedTask;

	private async Task DoneAsync()
	{
		_customizing = false;
		StateHasChanged();
		await CustomizingChanged.InvokeAsync(false);
	}

	private async Task ApplyAsync(List<MokaBoardCard> next, (string? Card, string Part)? focus)
	{
		_board = next.AsReadOnly();
		_focus = focus;
		StateHasChanged();
		await BoardChanged.InvokeAsync(_board);
	}

	private static List<MokaBoardCard> Replace(IReadOnlyList<MokaBoardCard> cards, MokaBoardCard card) =>
		[.. cards.Select(c => c.Id == card.Id ? card : c)];

	// Swaps the card with its neighbour on the board. Hidden cards are not neighbours: they keep their place.
	private static List<MokaBoardCard>? Step(IReadOnlyList<MokaBoardCard> cards, string id, int by)
	{
		List<MokaBoardCard> list = [.. cards];
		List<int> shown = [.. Enumerable.Range(0, list.Count).Where(i => !list[i].Hidden)];
		int at = shown.FindIndex(i => list[i].Id == id);
		int to = at + Math.Sign(by);
		if (at < 0 || to < 0 || to >= shown.Count)
		{
			return null;
		}

		(list[shown[at]], list[shown[to]]) = (list[shown[to]], list[shown[at]]);
		return list;
	}

	private static List<MokaBoardCard>? Move(IReadOnlyList<MokaBoardCard> cards, string id, string target, bool before)
	{
		List<MokaBoardCard> list = [.. cards];
		int from = list.FindIndex(c => c.Id == id);
		if (id == target || from < 0 || !list.Exists(c => c.Id == target))
		{
			return null;
		}

		MokaBoardCard card = list[from];
		list.RemoveAt(from);
		int at = list.FindIndex(c => c.Id == target);
		list.Insert(before ? at : at + 1, card);
		return list.SequenceEqual(cards) ? null : list;
	}

	// Blazor throws on a repeated @key, and a card without an id can't be found again.
	private static ReadOnlyCollection<MokaBoardCard> Normalize(IReadOnlyList<MokaBoardCard> cards) =>
		cards.Where(c => c is not null && !string.IsNullOrWhiteSpace(c.Id))
			.DistinctBy(c => c.Id, StringComparer.Ordinal)
			.ToList()
			.AsReadOnly();

	private static bool SameCards(IReadOnlyList<MokaBoardCard>? a, IReadOnlyList<MokaBoardCard>? b) =>
		ReferenceEquals(a, b) || (a is not null && b is not null && a.SequenceEqual(b));

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_boundRoot is not null)
		{
			await SafeModuleInvokeVoidAsync(JsModulePath, "unbindBoard", _root);
		}

		_dotNetRef?.Dispose();
		_dotNetRef = null;

		await base.DisposeAsyncCore();
	}
}
