using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Icons;
using Moka.Red.Primitives.Avatar;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// Enter and Space are handled by Core's moka-keys.js in the browser, so these tests cover the
// wiring: which avatars are buttons, what they are called, and when the key listener is bound.
public class MokaAvatarTests : BunitContext
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";
	private const string AvatarCss = "MokaAvatar.razor.css";

	[Fact]
	public void ATile_IsASoftSquare_NotACircle()
	{
		IRenderedComponent<MokaAvatar> cut = Render<MokaAvatar>(p => p
			.Add(x => x.Icon, MokaIcons.Navigation.Home)
			.Add(x => x.Tile, true));

		IElement root = cut.Find(".moka-avatar");
		Assert.Contains("moka-avatar--tile", root.ClassName, StringComparison.Ordinal);
		Assert.DoesNotContain("moka-avatar--rounded", root.ClassName, StringComparison.Ordinal);
		Assert.DoesNotContain("border-radius", root.GetAttribute("style") ?? string.Empty, StringComparison.Ordinal);
		Assert.Contains("width: 2.75rem", root.GetAttribute("style") ?? string.Empty, StringComparison.Ordinal);
	}

	[Fact]
	public void OnlyAnAvatarWithOnClick_IsAButtonBoundToTheKeys()
	{
		BunitJSModuleInterop keys = SetupKeysModule();

		IRenderedComponent<MokaAvatar> clickable = Render<MokaAvatar>(p => p
			.Add(x => x.Initials, "JD")
			.Add(x => x.OnClick, _ => { }));
		IRenderedComponent<MokaAvatar> staticAvatar = Render<MokaAvatar>(p => p.Add(x => x.Initials, "AB"));

		IElement button = clickable.Find(".moka-avatar");
		Assert.Equal("button", button.GetAttribute("role"));
		Assert.Equal("0", button.GetAttribute("tabindex"));

		JSRuntimeInvocation bind = keys.VerifyInvoke("bindActivation");
		ElementReference bound = Assert.IsType<ElementReference>(bind.Arguments[0]);
		Assert.Equal(button.GetAttribute("blazor:elementReference"), bound.Id);
		Assert.Single(bind.Arguments);

		IElement plain = staticAvatar.Find(".moka-avatar");
		Assert.False(plain.HasAttribute("role"));
		Assert.False(plain.HasAttribute("tabindex"));
	}

	[Fact]
	public async Task ClickableAvatar_IsNamedByAlt_AndClicks()
	{
		SetupKeysModule();
		int clicks = 0;

		IRenderedComponent<MokaAvatar> cut = Render<MokaAvatar>(p => p
			.Add(x => x.Initials, "JD")
			.Add(x => x.Alt, "Jane Doe")
			.Add(x => x.OnClick, _ => clicks++));

		// The key listener activates the avatar with click(), so this is the path Enter and Space take.
		await cut.Find("[role=button]").ClickAsync(new MouseEventArgs());

		Assert.Equal("Jane Doe", cut.Find("[role=button]").GetAttribute("aria-label"));
		Assert.Equal(1, clicks);
	}

	// A consumer's own aria-label goes on last and wins.
	[Fact]
	public void TheConsumersAriaLabel_WinsOverAlt()
	{
		SetupKeysModule();

		IRenderedComponent<MokaAvatar> cut = Render<MokaAvatar>(p => p
			.Add(x => x.Src, "/me.png")
			.Add(x => x.Alt, "Jane Doe")
			.Add(x => x.OnClick, _ => { })
			.AddUnmatched("aria-label", "Open account menu"));

		IElement button = cut.Find("[role=button]");
		Assert.Equal("Open account menu", button.GetAttribute("aria-label"));
		Assert.Equal("Jane Doe", button.QuerySelector("img")?.GetAttribute("alt"));
	}

	// Pinned, so the hash can't drift between processes, platforms or the two sides of a prerender: string.GetHashCode
	// gave a new color at every restart and could throw on int.MinValue.
	[Theory]
	[InlineData("JD", "cyan")]
	[InlineData("jd", "cyan")]
	[InlineData("AB", "pink")]
	[InlineData("BC", "cyan")]
	[InlineData("KL", "indigo")]
	[InlineData("MK", "pink")]
	[InlineData("A", "violet")]
	public void Initials_SitOnALabelColorPickedTheSameWayEveryTime(string initials, string hue)
	{
		IRenderedComponent<MokaAvatar> cut = Render<MokaAvatar>(p => p.Add(x => x.Initials, initials));

		Assert.Equal(
			$"width: 40px; height: 40px; background-color: var(--moka-label-{hue}-bg); color: var(--moka-label-{hue}-ink); border-radius: var(--moka-radius-full)",
			cut.Find(".moka-avatar").GetAttribute("style"));
	}

	// The label hues come with an ink held at 4.5:1 on every preset, and none of them is a status color.
	[Fact]
	public void EveryInitialsColor_IsALabelTintWithItsOwnInk_NeverAStatus()
	{
		HashSet<string> hues = [];
		for (char first = 'A'; first <= 'Z'; first++)
		{
			for (char second = 'A'; second <= 'Z'; second++)
			{
				IRenderedComponent<MokaAvatar> cut = Render<MokaAvatar>(p => p.Add(x => x.Initials, $"{first}{second}"));
				string style = cut.Find(".moka-avatar").GetAttribute("style")!;

				Match pair = Regex.Match(style,
					@"background-color: var\(--moka-label-(?<hue>[a-z]+)-bg\); color: var\(--moka-label-(?<ink>[a-z]+)-ink\)");
				Assert.True(pair.Success, style);
				Assert.Equal(pair.Groups["hue"].Value, pair.Groups["ink"].Value);
				hues.Add(pair.Groups["hue"].Value);
			}
		}

		Assert.Equal(["cyan", "indigo", "pink", "purple", "teal", "violet"], hues.Order());
	}

	// Black or white ink by the fill's lightness, as for ColorValue: the theme's on-colors miss 4.5:1 on the original
	// palettes (white on Dark's red, white on Light's orange).
	[Theory]
	[InlineData(MokaColor.Primary, "primary")]
	[InlineData(MokaColor.Success, "success")]
	[InlineData(MokaColor.Warning, "warning")]
	public void Color_FillsTheAvatar_AndTheStylesheetPicksTheInk(MokaColor color, string name)
	{
		IRenderedComponent<MokaAvatar> cut = Render<MokaAvatar>(p => p
			.Add(x => x.Initials, "JD")
			.Add(x => x.Color, color));

		IElement root = cut.Find(".moka-avatar");
		Assert.Contains("moka-avatar--own", root.ClassList);
		Assert.Equal($"width: 40px; height: 40px; --_avatar-color: var(--moka-color-{name}); border-radius: var(--moka-radius-full)",
			root.GetAttribute("style"));
	}

	[Fact]
	public void SurfaceColor_KeepsTheNeutralFill()
	{
		IRenderedComponent<MokaAvatar> cut = Render<MokaAvatar>(p => p
			.Add(x => x.Initials, "JD")
			.Add(x => x.Color, MokaColor.Surface));

		Assert.Equal("width: 40px; height: 40px; border-radius: var(--moka-radius-full)", cut.Find(".moka-avatar").GetAttribute("style"));
	}

	[Fact]
	public void Color_FillsAnIconAvatarToo()
	{
		IRenderedComponent<MokaAvatar> cut = Render<MokaAvatar>(p => p
			.Add(x => x.Icon, MokaIcons.Navigation.Home)
			.Add(x => x.Color, MokaColor.Info));

		Assert.Contains("--_avatar-color: var(--moka-color-info)", cut.Find(".moka-avatar").GetAttribute("style"), StringComparison.Ordinal);
		Assert.Contains("moka-avatar--own", cut.Find(".moka-avatar").ClassList);
		Assert.Contains("moka-avatar__icon", cut.Find("svg").ClassList);
	}

	[Theory]
	[InlineData("#2563eb", "#2563eb")]
	[InlineData("var(--moka-label-teal)", "var(--moka-label-teal)")]
	[InlineData(" oklch(62% 0.2 250) ", "oklch(62% 0.2 250)")]
	public void ColorValue_FillsTheAvatar_AndTheStylesheetPicksTheInk(string value, string written)
	{
		IRenderedComponent<MokaAvatar> cut = Render<MokaAvatar>(p => p
			.Add(x => x.Initials, "JD")
			.Add(x => x.Color, MokaColor.Primary)
			.Add(x => x.ColorValue, value));

		IElement root = cut.Find(".moka-avatar");
		Assert.Contains("moka-avatar--own", root.ClassList);
		Assert.Equal($"width: 40px; height: 40px; --_avatar-color: {written}; border-radius: var(--moka-radius-full)", root.GetAttribute("style"));
	}

	[Theory]
	[InlineData("red; background: url(x)")]
	[InlineData("not a color")]
	public void AColorValueThatIsNoColor_IsIgnored(string value)
	{
		IRenderedComponent<MokaAvatar> cut = Render<MokaAvatar>(p => p
			.Add(x => x.Initials, "JD")
			.Add(x => x.ColorValue, value));

		IElement root = cut.Find(".moka-avatar");
		Assert.DoesNotContain("moka-avatar--own", root.ClassList);
		Assert.Contains("var(--moka-label-cyan-bg)", root.GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void APicture_TakesNoFill()
	{
		IRenderedComponent<MokaAvatar> cut = Render<MokaAvatar>(p => p
			.Add(x => x.Src, "/me.png")
			.Add(x => x.Initials, "JD")
			.Add(x => x.Color, MokaColor.Primary)
			.Add(x => x.ColorValue, "#2563eb"));

		IElement root = cut.Find(".moka-avatar");
		Assert.DoesNotContain("moka-avatar--own", root.ClassList);
		Assert.Equal("width: 40px; height: 40px; border-radius: var(--moka-radius-full)", root.GetAttribute("style"));
	}

	// The initials used to carry on-primary themselves, so no color on the avatar reached them: white on a light
	// green, and a Style that set the avatar's color changed nothing.
	[Fact]
	public void TheInitials_TakeTheAvatarsColor_AndColorValueGetsBlackOrWhite()
	{
		const string css = "MokaAvatar.razor.css";

		Assert.False(ScopedCss.Declarations(css, ".moka-avatar__initials").ContainsKey("color"));
		Assert.Equal("var(--_avatar-color)", ScopedCss.Declarations(css, ".moka-avatar--own")["background-color"]);
		Assert.Equal(
			["white", "lab(from var(--_avatar-color) clamp(0, (49.4 - l) * 1000, 100) 0 0)"],
			ScopedCss.Values(css, ".moka-avatar--own", "color"));
	}

	// Centring the line box centred the letters only for a font whose ascent and descent balance around its capitals.
	// Trimmed to cap height and the baseline, the box is the capitals, so centring it centres them in any font.
	[Fact]
	public void Initials_AreCentredOnTheirCapitals()
	{
		Assert.Equal("trim-both cap alphabetic",
			ScopedCss.Declarations("MokaAvatar.razor.css", ".moka-avatar__initials")["text-box"]);
	}

	// SizeValue used to size only the box: a 56 px avatar drew the 14 px initials and the 16 px icon of a 40 px one.
	[Fact]
	public void SizeValue_GivesTheInitialsTheirStepsShareOfTheBox()
	{
		IRenderedComponent<MokaAvatar> cut = Render<MokaAvatar>(p => p
			.Add(x => x.Initials, "JD")
			.Add(x => x.SizeValue, "3.5rem"));

		IElement root = cut.Find(".moka-avatar");
		Assert.Contains("moka-avatar--sized", root.ClassList);
		Assert.StartsWith("width: 3.5rem; height: 3.5rem; --_avatar-size: 3.5rem;", root.GetAttribute("style"), StringComparison.Ordinal);
		Assert.Equal("calc(var(--_avatar-size) * var(--_avatar-text) / var(--_avatar-box) * var(--moka-font-scale))",
			ScopedCss.Declarations(AvatarCss, ".moka-avatar--sized")["font-size"]);
	}

	[Fact]
	public void SizeValue_GivesTheIconItsStepsShareOfTheBox()
	{
		IRenderedComponent<MokaAvatar> cut = Render<MokaAvatar>(p => p
			.Add(x => x.Icon, MokaIcons.Navigation.Home)
			.Add(x => x.SizeValue, "3.5rem"));

		Assert.Contains("width: var(--_avatar-icon-size); height: var(--_avatar-icon-size)",
			cut.Find(".moka-avatar__icon").GetAttribute("style"), StringComparison.Ordinal);
		Assert.Equal("calc(var(--_avatar-size) * var(--_avatar-icon) / var(--_avatar-box))",
			ScopedCss.Declarations(AvatarCss, ".moka-avatar--sized")["--_avatar-icon-size"]);
	}

	// A tile's icon was 20 px whatever the tile's size.
	[Fact]
	public void ATileWithSizeValue_ScalesItsIconWithTheTile()
	{
		IRenderedComponent<MokaAvatar> cut = Render<MokaAvatar>(p => p
			.Add(x => x.Icon, MokaIcons.Navigation.Home)
			.Add(x => x.Tile, true)
			.Add(x => x.SizeValue, "2rem"));

		IElement root = cut.Find(".moka-avatar");
		Assert.Contains("moka-avatar--sized", root.ClassList);
		Assert.Contains("--_avatar-size: 2rem", root.GetAttribute("style"), StringComparison.Ordinal);
		Assert.Contains("width: var(--_avatar-icon-size)", cut.Find(".moka-avatar__icon").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void WithoutSizeValue_EveryStepDrawsAsBefore(bool tile)
	{
		IRenderedComponent<MokaAvatar> cut = Render<MokaAvatar>(p => p
			.Add(x => x.Icon, MokaIcons.Navigation.Home)
			.Add(x => x.Tile, tile));

		IElement root = cut.Find(".moka-avatar");
		Assert.DoesNotContain("moka-avatar--sized", root.ClassList);
		Assert.DoesNotContain("--_avatar-size", root.GetAttribute("style") ?? string.Empty, StringComparison.Ordinal);
		Assert.Contains(tile ? "width: 20px; height: 20px" : "width: 16px; height: 16px",
			cut.Find(".moka-avatar__icon").GetAttribute("style"), StringComparison.Ordinal);
	}

	// The stylesheet's numbers are each step's box, text and icon at the default text size. A SizeValue equal to a
	// step's own box then draws exactly that step.
	[Theory]
	[InlineData(MokaSize.Xs, "xs", false)]
	[InlineData(MokaSize.Sm, "sm", false)]
	[InlineData(MokaSize.Md, "md", false)]
	[InlineData(MokaSize.Lg, "lg", false)]
	[InlineData(MokaSize.Md, "md", true)]
	public void TheShares_AreTheStepsOwnSizes(MokaSize size, string step, bool tile)
	{
		IReadOnlyDictionary<string, string> shares =
			ScopedCss.Declarations(AvatarCss, $".moka-avatar--sized.moka-avatar--{step}");
		IReadOnlyDictionary<string, string> tileShares = ScopedCss.Declarations(AvatarCss, ".moka-avatar--sized.moka-avatar--tile");

		IRenderedComponent<MokaAvatar> icon = Render<MokaAvatar>(p => p
			.Add(x => x.Icon, MokaIcons.Navigation.Home)
			.Add(x => x.Size, size)
			.Add(x => x.Tile, tile));
		string box = tile ? tileShares["--_avatar-box"] : shares["--_avatar-box"];
		string iconSize = tile ? tileShares["--_avatar-icon"] : shares["--_avatar-icon"];

		Match width = Regex.Match(icon.Find(".moka-avatar").GetAttribute("style") ?? string.Empty, "^width: (?<size>[^;]+);");
		Assert.Equal(Number(box), Pixels(width.Groups["size"].Value));
		Assert.Contains($"width: {iconSize}px; height: {iconSize}px", icon.Find(".moka-avatar__icon").GetAttribute("style"),
			StringComparison.Ordinal);

		string textToken = ScopedCss.Declarations(AvatarCss, $".moka-avatar--{step}")["font-size"];
		string text = ScopedCss.Declarations("moka.css", ":root")[textToken["var(".Length..^1]];
		Assert.Equal(Number(shares["--_avatar-text"]), Pixels(text));
	}

	[Theory]
	[InlineData("56", "56px")]
	[InlineData(" 3.5rem ", "3.5rem")]
	[InlineData("var(--app-avatar)", "var(--app-avatar)")]
	[InlineData("clamp(2rem, 8vw, 4rem)", "clamp(2rem, 8vw, 4rem)")]
	public void SizeValue_ThatIsALength_IsShared(string value, string written)
	{
		IElement root = Render<MokaAvatar>(p => p
			.Add(x => x.Initials, "JD")
			.Add(x => x.SizeValue, value)).Find(".moka-avatar");

		Assert.StartsWith($"width: {written}; height: {written}; --_avatar-size: {written};", root.GetAttribute("style"),
			StringComparison.Ordinal);
	}

	// A value the content can't take a share of still sizes the box, and the content keeps its step's sizes. A
	// percentage is of the avatar's container, and em, ex and ch of the avatar's own text, which the share would set.
	[Theory]
	[InlineData("fit-content")]
	[InlineData("80%")]
	[InlineData("calc(100% - 1rem)")]
	[InlineData("3em")]
	[InlineData("4ch")]
	public void SizeValue_TheContentCantShare_SizesOnlyTheBox(string value)
	{
		IRenderedComponent<MokaAvatar> cut = Render<MokaAvatar>(p => p
			.Add(x => x.Icon, MokaIcons.Navigation.Home)
			.Add(x => x.SizeValue, value));

		IElement root = cut.Find(".moka-avatar");
		Assert.DoesNotContain("moka-avatar--sized", root.ClassList);
		Assert.StartsWith($"width: {value}; height: {value};", root.GetAttribute("style"), StringComparison.Ordinal);
		Assert.Contains("width: 16px", cut.Find(".moka-avatar__icon").GetAttribute("style"), StringComparison.Ordinal);
	}

	// Large initials were a literal 20 px, so they ignored the theme's text size and font scale.
	[Fact]
	public void LargeInitials_ReadATextSizeToken()
	{
		Assert.Equal("var(--moka-font-size-xl)", ScopedCss.Declarations(AvatarCss, ".moka-avatar--lg")["font-size"]);
	}

	private static double Number(string value) => double.Parse(value, CultureInfo.InvariantCulture);

	// At the browser's default 16 px root, which is what the stylesheet's numbers assume.
	private static double Pixels(string length) => length.EndsWith("rem", StringComparison.Ordinal)
		? Number(length[..^"rem".Length]) * 16
		: Number(length[..^"px".Length]);

	private BunitJSModuleInterop SetupKeysModule()
	{
		BunitJSModuleInterop keys = JSInterop.SetupModule(KeysModule);
		keys.SetupVoid("bindActivation", _ => true).SetVoidResult();
		return keys;
	}
}
