using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.BentoGrid;

/// <summary>
///     A single cell in a <see cref="MokaBentoGrid" />.
///     Configure column/row span to create the asymmetric bento layout.
///     With <see cref="Card" /> set it is a card on a board, with a head and, while the board is customized, its tools.
/// </summary>
public partial class MokaBentoItem : MokaVisualComponentBase
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	private ElementReference _element;
	private bool _keysBound;

	/// <summary>Cell content.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Number of columns this cell spans. Default 1. Not used on a card, whose size decides.</summary>
	[Parameter]
	public int ColSpan { get; set; } = 1;

	/// <summary>Number of rows this cell spans. Default 1. Not used on a card.</summary>
	[Parameter]
	public int RowSpan { get; set; } = 1;

	/// <summary>Explicit column start position (1-based). Default auto.</summary>
	[Parameter]
	public int? ColStart { get; set; }

	/// <summary>Explicit row start position (1-based). Default auto.</summary>
	[Parameter]
	public int? RowStart { get; set; }

	/// <summary>Simple text title for the cell header. On a card it is the card's name.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>Description text below the title.</summary>
	[Parameter]
	public string? Description { get; set; }

	/// <summary>When true, the cell is interactive (hover effects). Default false.</summary>
	[Parameter]
	public bool Clickable { get; set; }

	/// <summary>Click event handler.</summary>
	[Parameter]
	public EventCallback OnClick { get; set; }

	/// <summary>
	///     The board card this item draws, as the board's <c>CardTemplate</c> hands it over. It sets the item's size
	///     (Small one column, Wide two, Full a row) and, while the board is customized, adds the card's tools.
	/// </summary>
	[Parameter]
	public MokaBoardCard? Card { get; set; }

	/// <summary>An icon in the head, before the title.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>A small tag after the title saying where the card comes from, such as "Plugin".</summary>
	[Parameter]
	public string? Source { get; set; }

	/// <summary>Where the head's Open link goes. Without it there is no link.</summary>
	[Parameter]
	public string? Href { get; set; }

	/// <summary>Text of the head's Open link. Default "Open".</summary>
	[Parameter]
	public string OpenText { get; set; } = "Open";

	[CascadingParameter] private MokaBentoGrid? ParentGrid { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-bento-item";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-bento-item--clickable", Clickable)
		.AddClass("moka-bento-item--tile", IsTile)
		.AddClass(SizeClass)
		.AddClass("moka-bento-item--customizing", IsCustomizing)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("grid-column", $"span {ColSpan}", Card is null && ColSpan > 1 && ColStart is null)
		.AddStyle("grid-row", $"span {RowSpan}", Card is null && RowSpan > 1 && RowStart is null)
		.AddStyle("grid-column", $"{ColStart} / span {ColSpan}", Card is null && ColStart.HasValue)
		.AddStyle("grid-row", $"{RowStart} / span {RowSpan}", Card is null && RowStart.HasValue)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private int? TabIndex => Clickable ? 0 : null;

	private bool IsTile => Card is not null || Icon is not null || Source is not null || Href is not null;

	// The board's own copy, so a card drawn through a component of the app's that held on to an older one is current.
	private MokaBoardCard? CurrentCard => Card is null ? null : ParentGrid?.Find(Card.Id) ?? Card;

	private bool IsCustomizing => Card is not null && ParentGrid is { IsCustomizing: true };

	private bool IsFirst => CurrentCard is { } card && ParentGrid?.IsFirstShown(card.Id) == true;

	private bool IsLast => CurrentCard is { } card && ParentGrid?.IsLastShown(card.Id) == true;

	private string? ResolvedTitle => Title ?? (Card is null ? null : ParentGrid?.TitleOf(Card.Id) ?? Card.Id);

	private string SizeName => (CurrentCard?.Size ?? MokaBoardSize.Wide).ToString();

	private string? SizeClass => CurrentCard?.Size switch
	{
		MokaBoardSize.Small => "moka-bento-item--small",
		MokaBoardSize.Wide => "moka-bento-item--wide",
		MokaBoardSize.Full => "moka-bento-item--full",
		_ => null
	};

	private string? TileRole => Clickable ? "button" : Card is not null ? "article" : null;

	private string? TileLabel => Card is not null ? ResolvedTitle : null;

	private string? OpenHref => UrlValues.SafeHref(Href);

	private EventCallback<MouseEventArgs> TileClick => Clickable
		? EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick)
		: default;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// Enter and Space are handled in the browser, for the card itself only. A .NET key
		// handler also fired for keys pressed in a button or input inside the card, and Space
		// scrolled the page as well.
		if (Clickable && !_keysBound)
		{
			_keysBound = true;
			await SafeModuleInvokeVoidAsync(KeysModule, "bindActivation", _element);
		}
	}

	private async Task HandleClick()
	{
		if (Clickable && OnClick.HasDelegate)
		{
			await OnClick.InvokeAsync();
		}
	}

	private Task HandleGripKey(KeyboardEventArgs e)
	{
		int by = e.Key switch
		{
			"ArrowLeft" or "ArrowUp" => -1,
			"ArrowRight" or "ArrowDown" => 1,
			_ => 0
		};

		return by == 0 || e.CtrlKey || e.AltKey || e.MetaKey ? Task.CompletedTask : StepAsync(by, "grip");
	}

	private Task MoveEarlierAsync() => StepAsync(-1, "earlier");

	private Task MoveLaterAsync() => StepAsync(1, "later");

	private Task StepAsync(int by, string focusPart) =>
		ParentGrid is not null && Card is not null ? ParentGrid.StepAsync(Card.Id, by, focusPart) : Task.CompletedTask;

	private Task ResizeAsync() =>
		ParentGrid is not null && Card is not null ? ParentGrid.ResizeAsync(Card.Id) : Task.CompletedTask;

	private Task HideAsync() =>
		ParentGrid is not null && Card is not null ? ParentGrid.HideAsync(Card.Id) : Task.CompletedTask;
}
