using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Avatar;

/// <summary>
///     User avatar component displaying an image, initials, or icon fallback. Initials sit on a label color picked from
///     the initials, the same one in every process and on every platform, unless <c>Color</c> or
///     <see cref="ColorValue" /> names one. <c>Color</c> fills the avatar with that theme color, black or white ink on it
///     by its lightness; <c>MokaColor.Surface</c> is the neutral fill.
/// </summary>
public partial class MokaAvatar
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	// Label hues, so each tint comes with the ink LabelContrastTests holds at 4.5:1 on every preset. The hues the
	// status colors use (red, rose, orange, amber, yellow, green, emerald, sky, blue) and slate, which reads as
	// switched off, are left out, so a person's initials never look like an error, a warning, a success or a note.
	private static readonly MokaLabelColor[] InitialsHues =
	[
		MokaLabelColor.Teal,
		MokaLabelColor.Cyan,
		MokaLabelColor.Indigo,
		MokaLabelColor.Violet,
		MokaLabelColor.Purple,
		MokaLabelColor.Pink
	];

	private ElementReference _element;
	private bool _keysBound;

	/// <summary>Image URL for the avatar.</summary>
	[Parameter]
	public string? Src { get; set; }

	/// <summary>
	///     Alt text for the avatar image. A clickable avatar also uses it as its accessible name,
	///     whether it shows the image, initials, an icon or an identicon.
	/// </summary>
	[Parameter]
	public string? Alt { get; set; }

	/// <summary>Fallback initials text when no image is set (e.g., "JD").</summary>
	[Parameter]
	public string? Initials { get; set; }

	/// <summary>Fallback icon when no image or initials are set.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>
	///     A string to generate a deterministic identicon from (e.g., user ID, email).
	///     Used as fallback when no Src, Initials, or Icon is set.
	///     Set to null or empty to disable identicon fallback.
	/// </summary>
	[Parameter]
	public string? IdenticonValue { get; set; }

	/// <summary>
	///     Whether to show an identicon as the final fallback. Default true.
	///     When false, shows nothing instead.
	/// </summary>
	[Parameter]
	public bool ShowIdenticon { get; set; } = true;

	/// <summary>White border for overlapping avatars.</summary>
	[Parameter]
	public bool Bordered { get; set; }

	/// <summary>
	///     Click handler for the avatar. With a handler the avatar is a button: it joins the tab
	///     order, and Enter or Space click it.
	/// </summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	/// <summary>A soft square for an icon rather than a person: a device, a plugin or a section of an app.</summary>
	[Parameter]
	public bool Tile { get; set; }

	/// <summary>
	///     A CSS color or token to fill the avatar with when it shows no picture, such as <c>#2563eb</c> or
	///     <c>var(--moka-label-teal)</c>. The initials or icon on it are black or white, whichever reads better, as they
	///     are on a <c>Color</c>. Wins over <c>Color</c>; anything that isn't a color is ignored.
	/// </summary>
	[Parameter]
	public string? ColorValue { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-avatar";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-avatar--{SizeToKebab(Size)}")
		.AddClass("moka-avatar--tile", Tile)
		.AddClass("moka-avatar--rounded", Rounded == MokaRounding.Full)
		.AddClass("moka-avatar--square", Rounded == MokaRounding.None)
		.AddClass("moka-avatar--bordered", Bordered)
		.AddClass("moka-avatar--clickable", IsClickable)
		.AddClass("moka-avatar--own", OwnColor is not null)
		.AddClass(Class)
		.Build();

	private bool IsClickable => OnClick.HasDelegate;

	private int? TabIndex => IsClickable ? 0 : null;

	private string? ButtonLabel => IsClickable && !string.IsNullOrEmpty(Alt) ? Alt : null;

	private bool ShowsPicture => !string.IsNullOrEmpty(Src);

	// The stylesheet inks these black or white by their lightness. A status color's own on-color misses 4.5:1 on the
	// original palettes: white on the dark scheme's red is 3.5:1 and on the light scheme's orange 2.7:1.
	private string? OwnColor
	{
		get
		{
			if (ShowsPicture)
			{
				return null;
			}

			if (CssValues.IsColor(ColorValue))
			{
				return ColorValue.Trim();
			}

			return Color is { } color && color != MokaColor.Surface ? $"var(--moka-color-{ColorToKebab(color)})" : null;
		}
	}

	// A Surface avatar keeps the stylesheet's neutral fill.
	private MokaLabelColor? InitialsHue =>
		ShowsPicture || Color is not null || OwnColor is not null || string.IsNullOrEmpty(Initials) ? null : HueFor(Initials);

	/// <inheritdoc />
	protected override string? CssStyle
	{
		get
		{
			MokaLabelColor? hue = InitialsHue;
			return new StyleBuilder()
				.AddStyle("width", AvatarSize)
				.AddStyle("height", AvatarSize)
				.AddStyle("--_avatar-color", OwnColor)
				.AddStyle("background-color", hue is { } tint ? MokaLabelColors.Background(tint) : null)
				.AddStyle("color", hue is { } ink ? MokaLabelColors.Ink(ink) : null)
				.AddStyle("border-radius", ResolvedRounding)
				.AddStyle("margin", ResolvedMargin)
				.AddStyle("padding", ResolvedPadding)
				.AddStyle(Style)
				.Build();
		}
	}

	private string AvatarSize => SizeValue ?? (Tile ? "2.75rem" : null) ?? Size switch
	{
		MokaSize.Xs => "24px",
		MokaSize.Sm => "32px",
		MokaSize.Md => "40px",
		MokaSize.Lg => "56px",
		_ => "40px"
	};

	private MokaSize IconSize => Size switch
	{
		MokaSize.Xs => MokaSize.Xs,
		MokaSize.Sm => MokaSize.Xs,
		MokaSize.Md => MokaSize.Sm,
		MokaSize.Lg => MokaSize.Md,
		_ => MokaSize.Sm
	};

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Circular by default for avatars. A tile's corners come from the theme, through its stylesheet.
		if (!Tile)
		{
			Rounded ??= MokaRounding.Full;
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// Enter and Space are handled in the browser, for the avatar itself only, and Space does
		// not scroll the page. Static avatars never load the script.
		if (IsClickable && !_keysBound)
		{
			_keysBound = true;
			await SafeModuleInvokeVoidAsync(KeysModule, "bindActivation", _element);
		}
	}

	// FNV-1a over the upper-cased characters, so "jd" and "JD" match and the answer is the same in every process, on
	// every platform and on both sides of a prerender. string.GetHashCode is seeded per process. The hue comes from the
	// high half: over two characters the low bits barely mix, and the lowest is just their parity.
	private static MokaLabelColor HueFor(string initials)
	{
		uint hash = 2166136261;
		foreach (char c in initials)
		{
			hash = unchecked((hash ^ char.ToUpperInvariant(c)) * 16777619);
		}

		return InitialsHues[(hash >> 16) % (uint)InitialsHues.Length];
	}

	private async Task HandleClick(MouseEventArgs args)
	{
		if (OnClick.HasDelegate)
		{
			await OnClick.InvokeAsync(args);
		}
	}
}
