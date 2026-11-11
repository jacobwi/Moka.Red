using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Primitives.Emoji;

namespace Moka.Red.Primitives.Reactions;

/// <summary>
///     Emoji reactions on a message: a bar of quick reactions over it on hover or focus, a smile that opens every emoji,
///     and the counts under it, where clicking your own takes it back. The app owns the counts: it passes
///     <see cref="Reactions" /> in and changes them in <see cref="OnToggle" />.
/// </summary>
public partial class MokaReactions : MokaVisualComponentBase
{
	private static readonly IReadOnlyList<string> DefaultQuick =
		["\U0001F44D", "\u2764\uFE0F", "\U0001F602", "\U0001F389", "\U0001F62E", "\U0001F64F"];

	private ElementReference _barButton;
	private ElementReference _chipButton;
	private Opener _openFrom;

	/// <summary>The reactions so far. Reactions at 0 aren't shown.</summary>
	[Parameter]
	public IReadOnlyList<MokaReaction>? Reactions { get; set; }

	/// <summary>
	///     Raised with an emoji when the viewer reacts with it or takes their reaction back: from the bar, from a count,
	///     or a pick in the panel of an emoji they haven't reacted with yet. The app flips <see cref="MokaReaction.Mine" />
	///     and the count.
	/// </summary>
	[Parameter]
	public EventCallback<string> OnToggle { get; set; }

	/// <summary>The bar's quick reactions. Null is 👍 ❤️ 😂 🎉 😮 🙏.</summary>
	[Parameter]
	public IReadOnlyList<string>? Quick { get; set; }

	/// <summary>Whether the quick bar shows over <see cref="ChildContent" /> on hover or focus. Default true.</summary>
	[Parameter]
	public bool ShowBar { get; set; } = true;

	/// <summary>Whether a smile opens the full emoji panel, in the bar and after the counts. Default true.</summary>
	[Parameter]
	public bool ShowAdd { get; set; } = true;

	/// <summary>
	///     The panel's options. Its shape is always a popup: <see cref="MokaEmojiShape.List" /> stays a list and every
	///     other shape opens the popover grid.
	/// </summary>
	[Parameter]
	public MokaEmojiOptions? Options { get; set; }

	/// <summary>Raised when the panel changes the skin tone or the frequently used list, for the app to store.</summary>
	[Parameter]
	public EventCallback<MokaEmojiOptions> OptionsChanged { get; set; }

	/// <summary>The words. Null is English.</summary>
	[Parameter]
	public MokaReactionsText? Text { get; set; }

	/// <summary>The message the reactions belong to. With it the quick bar shows over it on hover or focus.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-reactions";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-reactions--message", ChildContent is not null)
		.AddClass("moka-reactions--picking", _openFrom == Opener.Bar)
		.AddClass("moka-reactions--disabled", Disabled)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle().AddStyle(Style).Build();

	private MokaReactionsText Words => Text ?? MokaReactionsText.Default;

	private IReadOnlyList<string> QuickList => Quick ?? DefaultQuick;

	private List<MokaReaction> Shown => [.. (Reactions ?? []).Where(static r => r.Count > 0 && !string.IsNullOrEmpty(r.Emoji))
		.DistinctBy(static r => MokaEmojiRecent.Bare(r.Emoji))];

	private ElementReference PanelAnchor => _openFrom == Opener.Bar ? _barButton : _chipButton;

	private MokaEmojiOptions PanelOptions
	{
		get
		{
			MokaEmojiOptions options = Options ?? MokaEmojiOptions.Default;
			return options.Shape is MokaEmojiShape.Popover or MokaEmojiShape.List ? options : options with { Shape = MokaEmojiShape.Popover };
		}
	}

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	private bool IsMine(string emoji)
	{
		string key = MokaEmojiRecent.Bare(emoji);
		return (Reactions ?? []).Any(r => r.Mine && r.Count > 0 && MokaEmojiRecent.Bare(r.Emoji) == key);
	}

	private async Task ToggleAsync(string emoji)
	{
		if (OnToggle.HasDelegate && !Disabled)
		{
			await OnToggle.InvokeAsync(emoji);
		}
	}

	private void OpenPanel(Opener from) => _openFrom = _openFrom == from ? Opener.None : from;

	private void PanelOpenChanged(bool open)
	{
		if (!open)
		{
			_openFrom = Opener.None;
		}
	}

	private async Task PickAsync(MokaEmojiPick pick)
	{
		if (!IsMine(pick.Emoji))
		{
			await ToggleAsync(pick.Emoji);
		}
	}

	private enum Opener
	{
		None,
		Bar,
		Chips
	}
}
