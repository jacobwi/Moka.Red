using System.Globalization;
using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Icons;
using static Moka.Red.ContextMenu.Tests.RadialDom;

namespace Moka.Red.ContextMenu.Tests.Components;

public class MokaRadialMenuChoiceStyleTests : BunitContext
{
	private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

	private readonly BunitJSModuleInterop _module;
	private readonly List<string> _picked = [];
	private int _closes;

	public MokaRadialMenuChoiceStyleTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
	}

	[Fact]
	public void TheList_IsTheDefault_AndChoicesCarryNoStyle()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		Assert.DoesNotContain(Ring(cut).ClassList, c => c.StartsWith("moka-radial-menu--choices", StringComparison.Ordinal));
		Assert.Equal("moka-radial-menu__choices moka-radial-menu__choices--right", cut.Find(".moka-radial-menu__choices").ClassName);
		Assert.All(cut.FindAll(".moka-radial-menu__choice"), c => Assert.False(c.HasAttribute("style")));
	}

	[Fact]
	public void AChoicesIcon_ShowsBeforeItsLabel()
	{
		MokaRadialMenuSpec spec = new("File",
		[
			new MokaRadialItem("share", "Share", MokaIcons.Action.Share, Choices:
			[
				new MokaRadialChoice("email", "Email") { Icon = MokaIcons.Action.Send },
				new MokaRadialChoice("link", "Link")
			])
		]);
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(spec);

		IReadOnlyList<IElement> choices = cut.FindAll(".moka-radial-menu__choice");
		Assert.NotNull(choices[0].QuerySelector(".moka-radial-menu__choice-icon"));
		Assert.Equal("moka-radial-menu__choice-text", choices[0].Children[1].ClassName);
		Assert.Null(choices[1].QuerySelector(".moka-radial-menu__choice-icon"));
	}

	[Fact]
	public void TheRing_PutsTheChoicesOnAnArcAroundTheButtonsAngle()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(style: MokaRadialChoiceStyle.Ring);

		Assert.Contains("moka-radial-menu--choices-ring", Ring(cut).ClassList);
		IElement list = cut.Find(".moka-radial-menu__choices");
		Assert.Equal("moka-radial-menu__choices moka-radial-menu__choices--arc", list.ClassName);
		Assert.Equal("menu", list.GetAttribute("role"));

		IElement band = list.Children[0];
		Assert.Equal("moka-radial-menu__arc", band.ClassName);
		Assert.Equal("true", band.GetAttribute("aria-hidden"));
		Assert.StartsWith("--moka-radial-arc-from: ", band.GetAttribute("style"), StringComparison.Ordinal);
		Assert.Contains("; --moka-radial-arc-span: ", band.GetAttribute("style"), StringComparison.Ordinal);

		double[] angles = [.. cut.FindAll(".moka-radial-menu__choice").Select(ChoiceAngle)];
		Assert.Equal(3, angles.Length);
		Assert.Equal(-30, angles[1]);
		Assert.Equal(angles[1] - angles[0], angles[2] - angles[1], 2);
		Assert.True(angles[1] - angles[0] >= 22);
		Assert.EndsWith("--moka-radial-choice-delay: 30ms", cut.FindAll(".moka-radial-menu__choice")[1].GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void TheRing_SpreadsLongLabelsFurther()
	{
		static MokaRadialMenuSpec Spec(string label) => new("Thing",
		[
			new MokaRadialItem("a", "A", MokaIcons.Action.Add, Choices: [new MokaRadialChoice("1", label), new MokaRadialChoice("2", label)])
		]);

		IRenderedComponent<MokaRadialMenu> shortCut = RenderRing(Spec("Hi"), MokaRadialChoiceStyle.Ring);
		double shortGap = Gap(shortCut);
		IRenderedComponent<MokaRadialMenu> longCut = RenderRing(Spec("A much longer choice"), MokaRadialChoiceStyle.Ring);

		Assert.True(Gap(longCut) > shortGap);
		return;

		static double Gap(IRenderedComponent<MokaRadialMenu> cut)
		{
			IReadOnlyList<IElement> choices = cut.FindAll(".moka-radial-menu__choice");
			return ChoiceAngle(choices[1]) - ChoiceAngle(choices[0]);
		}
	}

	[Fact]
	public void TheRing_TellsTheModuleToFollowTheArc()
	{
		RenderRing(style: MokaRadialChoiceStyle.Ring);

		JSRuntimeInvocation bind = _module.VerifyInvoke("bind");
		using JsonDocument options = JsonDocument.Parse(JsonSerializer.Serialize(bind.Arguments[3]));
		Assert.True(options.RootElement.GetProperty("arc").GetBoolean());
	}

	[Fact]
	public void TheFlyout_TiesItsListToTheButtonWithAStem()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(style: MokaRadialChoiceStyle.Flyout);

		Assert.Contains("moka-radial-menu--choices-flyout", Ring(cut).ClassList);
		Assert.Equal("moka-radial-menu__choices moka-radial-menu__choices--flyout", cut.Find(".moka-radial-menu__choices").ClassName);
		IElement limit = Slots(cut)[1];
		Assert.Equal("moka-radial-menu__stem", limit.Children[0].ClassName);
		Assert.Equal("true", limit.Children[0].GetAttribute("aria-hidden"));
		Assert.Single(cut.FindAll(".moka-radial-menu__stem"));
	}

	[Fact]
	public async Task TheFlyout_KeepsTheListsKeys()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(style: MokaRadialChoiceStyle.Flyout);

		await Press(cut, "2");
		Assert.Equal("50 Mbit/s", Lit(cut));
		await Press(cut, "ArrowDown");
		await Press(cut, "Enter");

		Assert.Equal(["limit:100"], _picked);
	}

	[Fact]
	public void Replace_ListsNoChoicesBesideTheButton()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(style: MokaRadialChoiceStyle.Replace);

		Assert.Contains("moka-radial-menu--choices-replace", Ring(cut).ClassList);
		Assert.Empty(cut.FindAll(".moka-radial-menu__choices"));
		Assert.NotNull(Button(cut, "Limit").QuerySelector(".moka-radial-menu__more"));
		Assert.Equal("menu", Button(cut, "Limit").GetAttribute("aria-haspopup"));
	}

	[Fact]
	public async Task Replace_SwapsTheRingForTheButtonsChoices_AndTheHubBecomesBack()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(style: MokaRadialChoiceStyle.Replace);
		await Press(cut, "ArrowRight");
		await Press(cut, "ArrowRight");
		Assert.Equal("false", Button(cut, "Limit").GetAttribute("aria-expanded"));

		await Press(cut, "Enter");

		IElement ring = Ring(cut);
		Assert.Equal("Limit", ring.GetAttribute("aria-label"));
		Assert.Contains("moka-radial-menu--back", ring.ClassList);
		IReadOnlyList<IElement> buttons = cut.FindAll(".moka-radial-menu__button");
		Assert.Equal(["10 Mbit/s", "50 Mbit/s", "100 Mbit/s"], buttons.Select(b => b.GetAttribute("aria-label")));
		Assert.All(buttons, b => Assert.Equal("menuitemradio", b.GetAttribute("role")));
		Assert.Equal(["false", "true", "false"], buttons.Select(b => b.GetAttribute("aria-checked")));
		Assert.Equal(["1", "2", "3"], buttons.Select(b => b.GetAttribute("aria-keyshortcuts")));
		Assert.Equal(["10 Mbit/s", "50 Mbit/s", "100 Mbit/s"], cut.FindAll(".moka-radial-menu__button > .moka-radial-menu__text").Select(t => t.TextContent));
		Assert.NotNull(buttons[1].QuerySelector(".moka-radial-menu__checked"));
		Assert.Null(buttons[0].QuerySelector(".moka-radial-menu__checked"));
		Assert.Equal(["-90deg", "30deg", "150deg"], Slots(cut).Select(s => s.GetAttribute("style")!.Split(';')[0][21..]));

		Assert.Equal("50 Mbit/s", Lit(cut));
		IElement hub = cut.Find(".moka-radial-menu__hub");
		Assert.Equal("Limit", hub.QuerySelector(".moka-radial-menu__title")!.TextContent);
		Assert.Equal("50 Mbit/s", HubWord(cut));
		Assert.Equal("Back", hub.GetAttribute("data-moka-sub"));
		Assert.True(hub.HasAttribute("blazor:onclick"));
		cut.WaitForAssertion(() => _module.VerifyInvoke("reset"), Wait);
	}

	[Fact]
	public async Task Replace_EscapeGoesBackToTheButton_ThenCloses()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(style: MokaRadialChoiceStyle.Replace);
		await Press(cut, "2");

		await Press(cut, "Escape");

		Assert.Equal("Limit", Lit(cut));
		Assert.Equal("Office desktop", Ring(cut).GetAttribute("aria-label"));
		Assert.DoesNotContain("moka-radial-menu--back", Ring(cut).ClassList);
		Assert.False(cut.Find(".moka-radial-menu__hub").HasAttribute("blazor:onclick"));
		Assert.Equal(0, _closes);

		await Press(cut, "Escape");

		Assert.Equal(1, _closes);
		Assert.Empty(cut.FindAll(".moka-radial-menu"));
	}

	[Fact]
	public async Task Replace_AClickOnTheHubGoesBack()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(style: MokaRadialChoiceStyle.Replace);
		await Button(cut, "Limit").ClickAsync(new MouseEventArgs());
		Assert.Contains("moka-radial-menu--back", Ring(cut).ClassList);

		await cut.Find(".moka-radial-menu__hub").ClickAsync(new MouseEventArgs());

		Assert.Equal("Limit", Lit(cut));
		Assert.DoesNotContain("moka-radial-menu--back", Ring(cut).ClassList);
	}

	[Theory]
	[InlineData("3", "limit:100")]
	[InlineData("Enter", "limit:50")]
	public async Task Replace_DigitsAndEnterPickAChoice(string key, string answer)
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(style: MokaRadialChoiceStyle.Replace);
		await Press(cut, "2");

		await Press(cut, key);

		Assert.Equal([answer], _picked);
		Assert.Empty(cut.FindAll(".moka-radial-menu"));
	}

	[Fact]
	public async Task Replace_ArrowsAndTabWalkTheChoices()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(style: MokaRadialChoiceStyle.Replace);
		await Press(cut, "2");

		await Press(cut, "ArrowRight");
		Assert.Equal("100 Mbit/s", Lit(cut));
		await Press(cut, "Tab");
		Assert.Equal("10 Mbit/s", Lit(cut));
		await Press(cut, "ArrowLeft");
		Assert.Equal("100 Mbit/s", Lit(cut));
	}

	[Fact]
	public async Task Replace_ClickingAChoicePicksIt()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(style: MokaRadialChoiceStyle.Replace);
		await Press(cut, "2");

		await Button(cut, "10 Mbit/s").ClickAsync(new MouseEventArgs());

		Assert.Equal(["limit:10"], _picked);
	}

	[Fact]
	public async Task Replace_ThePointerAndAFlick_CountInTheChoices()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(style: MokaRadialChoiceStyle.Replace);
		await cut.InvokeAsync(() => cut.Instance.OnFlicked(1, -1));
		cut.WaitForAssertion(() => Assert.Contains("moka-radial-menu--back", Ring(cut).ClassList), Wait);

		await cut.InvokeAsync(() => cut.Instance.OnPointed(0, -1));
		await Press(cut, "Enter");
		Assert.Equal(["limit:10"], _picked);
	}

	[Fact]
	public async Task Replace_AFlickOntoAChoicePicksIt()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(style: MokaRadialChoiceStyle.Replace);
		await Press(cut, "2");

		await cut.InvokeAsync(() => cut.Instance.OnFlicked(2, -1));

		cut.WaitForAssertion(() => Assert.Equal(["limit:100"], _picked), Wait);
	}

	[Fact]
	public async Task Replace_AChoicesIcon_IsItsButtonsFace()
	{
		MokaRadialMenuSpec spec = new("File",
		[
			new MokaRadialItem("share", "Share", MokaIcons.Action.Share, Choices:
			[
				new MokaRadialChoice("email", "Email") { Icon = MokaIcons.Action.Send },
				new MokaRadialChoice("team", "Team")
			])
		]);
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(spec, MokaRadialChoiceStyle.Replace);
		await Press(cut, "1");

		Assert.Null(Button(cut, "Email").QuerySelector(".moka-radial-menu__text"));
		Assert.NotNull(Button(cut, "Email").QuerySelector("svg"));
		Assert.Equal("Team", Button(cut, "Team").QuerySelector(".moka-radial-menu__text")!.TextContent);
	}

	[Fact]
	public async Task Replace_BackText_ComesFromTheSpec_AndTheParameterWins()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(Device() with { BackText = "Zurück" }, MokaRadialChoiceStyle.Replace);
		await Press(cut, "2");
		Assert.Equal("Zurück", cut.Find(".moka-radial-menu__hub").GetAttribute("data-moka-sub"));

		cut.Render(p => p.Add(x => x.BackText, "Retour"));

		Assert.Equal("Retour", cut.Find(".moka-radial-menu__hub").GetAttribute("data-moka-sub"));
	}

	[Fact]
	public async Task Replace_WithNothingHighlighted_TheHubSaysBack()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(style: MokaRadialChoiceStyle.Replace);
		await Press(cut, "2");

		await cut.InvokeAsync(() => cut.Instance.OnPointed(-1, -1));
		cut.Render();

		Assert.Equal("Back", HubWord(cut));
		Assert.Null(Lit(cut));
	}

	[Fact]
	public void TheSpecsChoiceStyle_IsUsed_AndTheParameterWins()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(Device() with { ChoiceStyle = MokaRadialChoiceStyle.Flyout });
		Assert.Contains("moka-radial-menu--choices-flyout", Ring(cut).ClassList);

		cut.Render(p => p.Add(x => x.ChoiceStyle, MokaRadialChoiceStyle.Ring));

		Assert.Contains("moka-radial-menu--choices-ring", Ring(cut).ClassList);
	}

	private static double ChoiceAngle(IElement choice)
	{
		string style = choice.GetAttribute("style")!;
		string angle = style.Split(';')[0].Replace("--moka-radial-choice-angle: ", "", StringComparison.Ordinal).Replace("deg", "", StringComparison.Ordinal);
		return double.Parse(angle, CultureInfo.InvariantCulture);
	}

	private IRenderedComponent<MokaRadialMenu> RenderRing(MokaRadialMenuSpec? spec = null, MokaRadialChoiceStyle? style = null) =>
		Render<MokaRadialMenu>(p => p
			.Add(m => m.Spec, spec ?? Device())
			.Add(m => m.Visible, true)
			.Add(m => m.X, 300)
			.Add(m => m.Y, 300)
			.Add(m => m.ChoiceStyle, style)
			.Add(m => m.OnPick, pick => _picked.Add(pick.Answer))
			.Add(m => m.OnClose, () => _closes++));

	private static Task Press(IRenderedComponent<MokaRadialMenu> cut, string key) =>
		Ring(cut).KeyDownAsync(new KeyboardEventArgs { Key = key });
}
