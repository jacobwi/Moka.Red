using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Layout.Card;

namespace Moka.Red.Layout.GlassCard;

/// <summary>
///     A glassmorphism card with backdrop-filter blur, translucent background,
///     and optional animated border glow. Designed for layering over
///     grid backgrounds, images, or gradient surfaces.
/// </summary>
public partial class MokaGlassCard : MokaVisualComponentBase
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	private readonly string _regionId = $"moka-glass-card-{Guid.NewGuid():N}";
	private ElementReference _element;
	private bool _keysBound;

	/// <summary>Card content.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Header content slot.</summary>
	[Parameter]
	public RenderFragment? Header { get; set; }

	/// <summary>Footer content slot.</summary>
	[Parameter]
	public RenderFragment? Footer { get; set; }

	/// <summary>Simple text title for the header.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>Subtitle below the title.</summary>
	[Parameter]
	public string? Subtitle { get; set; }

	/// <summary>Backdrop blur amount in pixels. Default 12.</summary>
	[Parameter]
	public int Blur { get; set; } = 12;

	/// <summary>Background opacity (0-100%). Default 8 (very translucent).</summary>
	[Parameter]
	public int BackgroundOpacity { get; set; } = 8;

	/// <summary>Background tint color. Defaults to surface color.</summary>
	[Parameter]
	public string? Tint { get; set; }

	/// <summary>Border color. Defaults to outline-variant.</summary>
	[Parameter]
	public string? BorderColor { get; set; }

	/// <summary>When true, shows a subtle animated glow on the border. Default false.</summary>
	[Parameter]
	public bool Glow { get; set; }

	/// <summary>Glow color. Defaults to primary.</summary>
	[Parameter]
	public string? GlowColor { get; set; }

	/// <summary>
	///     When true, the card is a button: hover effects, a tab stop, and a click, Enter or Space raise
	///     <see cref="OnClick" />. Default false. A card with an <see cref="Href" /> is a link instead.
	/// </summary>
	[Parameter]
	public bool Clickable { get; set; }

	/// <summary>Raised by a click on a <see cref="Clickable" /> card, or on the link of a card with an <see cref="Href" />.</summary>
	[Parameter]
	public EventCallback OnClick { get; set; }

	/// <summary>
	///     Optional href. A click anywhere on the card follows it, and the card's one tab stop is a link: the title when
	///     the card shows one, else a visually hidden link named by <see cref="ActionLabel" />. Links, buttons and inputs
	///     inside the card keep their own clicks. A <c>rel</c> or <c>download</c> attribute goes on that link, and a
	///     <c>rel</c> wins over the one <see cref="Target" /> gives. A <c>javascript:</c>, <c>vbscript:</c> or
	///     <c>data:</c> URL is not rendered: the card stays as it is without one.
	/// </summary>
	[Parameter]
	public string? Href { get; set; }

	/// <summary>
	///     Where the card's link opens, such as <c>_blank</c>. Only used with <see cref="Href" />. One that opens another
	///     tab gets <c>rel="noopener noreferrer"</c>, and its name ends in <see cref="NewTabText" /> for screen readers.
	/// </summary>
	[Parameter]
	public string? Target { get; set; }

	/// <summary>
	///     What screen readers hear after the name of a card whose <see cref="Target" /> opens another tab, after a comma.
	///     Default "opens in a new tab".
	/// </summary>
	[Parameter]
	public string NewTabText { get; set; } = "opens in a new tab";

	/// <summary>
	///     Names the card's link when the title isn't the link: a card with an <see cref="Href" /> that shows a custom
	///     <see cref="Header" /> or no <see cref="Title" />. Defaults to <see cref="Title" />. With neither, the link is
	///     named by the card's header and body.
	/// </summary>
	[Parameter]
	public string? ActionLabel { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-glass-card";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-glass-card--clickable", Clickable || LinkHref is not null)
		.AddClass("moka-glass-card--link", LinkHref is not null)
		.AddClass("moka-glass-card--glow", Glow)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle
	{
		get
		{
			var tint = Tint ?? "var(--moka-color-surface)";
			var border = BorderColor ?? "var(--moka-color-outline-variant)";
			var glow = GlowColor ?? "var(--moka-color-primary)";

			return new StyleBuilder()
				.AddStyle("--glass-blur", $"{Blur}px")
				.AddStyle("--glass-tint", tint)
				.AddStyle("--glass-opacity", (BackgroundOpacity / 100.0).ToString("F2", CultureInfo.InvariantCulture))
				.AddStyle("--glass-border", border)
				.AddStyle("--glass-glow", glow)
				.AddStyle("border-radius", ResolvedRounding)
				.AddStyle("margin", ResolvedMargin)
				.AddStyle("padding", ResolvedPadding)
				.AddStyle(Style)
				.Build();
		}
	}

	private bool HasHeader => Header is not null || Title is not null;

	private string? LinkHref => UrlValues.SafeHref(Href);

	private string? LinkRel => UrlValues.RelFor(Target);

	private bool OpensNewTab => LinkHref is not null && UrlValues.OpensNewTab(Target);

	// With a link the card is a plain element and the link is its action, so it is a button only without one.
	private bool IsButton => Clickable && LinkHref is null;

	private int? TabIndex => IsButton ? 0 : null;

	// No listener unless a click has somewhere to go, since every listener costs a round trip.
	private EventCallback ButtonClick => IsButton ? OnClick : default;

	private bool TitleIsAction => LinkHref is not null && Header is null && Title is not null;

	private string? HiddenActionName => ActionLabel ?? Title;

	private bool ActionNamedByContent =>
		LinkHref is not null && !TitleIsAction && HiddenActionName is null &&
		!CardActionAttributes.HasAriaLabel(AdditionalAttributes);

	private string? HeaderId => ActionNamedByContent && HasHeader ? $"{_regionId}-header" : null;

	private string? BodyId => ActionNamedByContent && ChildContent is not null ? $"{_regionId}-body" : null;

	private string? NewTabWordsId => ActionNamedByContent ? $"{_regionId}-new-tab" : null;

	private IReadOnlyDictionary<string, object>? RootAttributes =>
		CardActionAttributes.ForRoot(AdditionalAttributes, LinkHref is not null, LinkHref is not null);

	private IReadOnlyDictionary<string, object>? ActionAttributes =>
		CardActionAttributes.ForAction(AdditionalAttributes, true, OpensNewTab ? NewTabText : null);

	private static string ActionCssClass(bool hidden) => new CssBuilder("moka-glass-card__action")
		.AddClass("moka-glass-card__action--hidden", hidden)
		.Build();

	// With nothing else to name it, the hidden link reads the card's header and body.
	private string? ActionLabelledBy(bool hidden)
	{
		if (!hidden || !ActionNamedByContent)
		{
			return null;
		}

		string labelledBy = string.Join(' ', new[] { HeaderId, BodyId }.Where(id => id is not null));
		if (labelledBy.Length == 0)
		{
			return null;
		}

		return OpensNewTab ? $"{labelledBy} {NewTabWordsId}" : labelledBy;
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// Enter and Space are handled in the browser, for the card itself only. A .NET key
		// handler also fired for keys pressed in a button or input inside the card, and Space
		// scrolled the page as well.
		if (IsButton && !_keysBound)
		{
			_keysBound = true;
			await SafeModuleInvokeVoidAsync(KeysModule, "bindActivation", _element);
		}
	}
}
