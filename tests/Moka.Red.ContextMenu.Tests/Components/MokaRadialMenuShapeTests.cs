using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using static Moka.Red.ContextMenu.Tests.RadialDom;

namespace Moka.Red.ContextMenu.Tests.Components;

public class MokaRadialMenuShapeTests : BunitContext
{
	private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

	private readonly BunitJSModuleInterop _module;
	private readonly List<string> _picked = [];

	public MokaRadialMenuShapeTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
	}

	[Fact]
	public void TheDefaultRing_DrawsNoneOfTheNewParts()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		Assert.Equal("moka-radial-menu moka-radial-menu--success", Ring(cut).ClassName);
		Assert.Single(cut.FindAll(".moka-radial-menu__track"));
		Assert.Single(cut.FindAll(".moka-radial-menu__hub"));
		Assert.Equal(6, cut.FindAll(".moka-radial-menu__key").Count);
		foreach (string part in new[]
		         {
			         "__disc", "__sep", "__text", "__label", "__tag", "__line", "__dot", "__stem", "__arc", "__checked",
			         "__choice-icon"
		         })
		{
			Assert.Empty(cut.FindAll($".moka-radial-menu{part}"));
		}

		Assert.False(cut.Find(".moka-radial-menu__hub").HasAttribute("blazor:onclick"));
		Assert.All(Slots(cut), s => Assert.StartsWith("moka-radial-menu__slot", s.ClassName, StringComparison.Ordinal));
		Assert.DoesNotContain(_module.Invocations, i => i.Identifier == "measureViewport");
	}

	[Theory]
	[InlineData(MokaRadialShape.Pie, "moka-radial-menu--pie")]
	[InlineData(MokaRadialShape.HalfRing, "moka-radial-menu--half-ring")]
	[InlineData(MokaRadialShape.Compact, "moka-radial-menu--compact")]
	[InlineData(MokaRadialShape.Labeled, "moka-radial-menu--labeled")]
	[InlineData(MokaRadialShape.Marking, "moka-radial-menu--marking")]
	public void AShape_IsAClassOnTheRing(MokaRadialShape shape, string css)
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(shape: shape);

		Assert.Contains(css, Ring(cut).ClassList);
	}

	[Fact]
	public void ThePie_DrawsADiscWithItsSector_ASeparatorBetweenWedges_AndAWordOnEachWedge()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(shape: MokaRadialShape.Pie);

		Assert.Empty(cut.FindAll(".moka-radial-menu__track"));
		IElement disc = cut.Find(".moka-radial-menu__disc");
		Assert.Equal("true", disc.GetAttribute("aria-hidden"));
		Assert.Equal("--moka-radial-step: 60deg", disc.GetAttribute("style"));
		IElement lit = Assert.Single(disc.Children);
		Assert.Equal("moka-radial-menu__disc-lit", lit.ClassName);
		Assert.False(lit.HasAttribute("style"));

		IReadOnlyList<IElement> separators = cut.FindAll(".moka-radial-menu__sep");
		Assert.Equal(["-60deg", "0deg", "60deg", "120deg", "180deg", "240deg"],
			separators.Select(s => s.GetAttribute("style")!.Replace("--moka-radial-angle: ", "", StringComparison.Ordinal)));
		Assert.All(separators, s => Assert.Equal("true", s.GetAttribute("aria-hidden")));

		IReadOnlyList<IElement> words = cut.FindAll(".moka-radial-menu__button > .moka-radial-menu__text");
		Assert.Equal(["Open", "Limit", "Rename", "Allow", "Block", "Forget"], words.Select(t => t.TextContent));
		Assert.All(words, w => Assert.Equal("true", w.GetAttribute("aria-hidden")));
		Assert.Equal("Limit", Button(cut, "Limit").GetAttribute("aria-label"));
		Assert.Equal(6, cut.FindAll(".moka-radial-menu__button > .moka-radial-menu__key").Count);
		Assert.Single(cut.FindAll(".moka-radial-menu__hub"));
	}

	[Fact]
	public void TheHalfRing_SpreadsTheButtonsOverTheTopHalf()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(shape: MokaRadialShape.HalfRing);

		Assert.Equal(["-165deg", "-135deg", "-105deg", "-75deg", "-45deg", "-15deg"], Angles(cut));
		Assert.Single(cut.FindAll(".moka-radial-menu__track"));
	}

	[Theory]
	[InlineData(1000, 700, true, true, "-168.75deg", "-101.25deg")]
	[InlineData(200, 700, false, true, "-78.75deg", "-11.25deg")]
	[InlineData(200, 100, false, false, "11.25deg", "78.75deg")]
	[InlineData(1000, 100, true, false, "101.25deg", "168.75deg")]
	public void TheCornerFan_MeasuresTheWindow_AndOpensTowardItsMiddle(double x, double y, bool left, bool up, string first, string last)
	{
		_module.Setup<MokaRadialMenu.ViewportSize?>("measureViewport")
			.SetResult(new MokaRadialMenu.ViewportSize { Width = 1200, Height = 800 });

		IRenderedComponent<MokaRadialMenu> cut = RenderRing(x: x, y: y, shape: MokaRadialShape.CornerFan);

		cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".moka-radial-menu[role='menu']")), Wait);
		IElement ring = Ring(cut);
		Assert.Contains("moka-radial-menu--corner-fan", ring.ClassList);
		Assert.Contains(left ? "moka-radial-menu--opens-left" : "moka-radial-menu--opens-right", ring.ClassList);
		Assert.Contains(up ? "moka-radial-menu--opens-up" : "moka-radial-menu--opens-down", ring.ClassList);
		Assert.Equal(first, Angles(cut)[0]);
		Assert.Equal(last, Angles(cut)[^1]);
	}

	[Fact]
	public void TheCornerFan_IsMeasuredBeforeItIsDrawn()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(shape: MokaRadialShape.CornerFan);

		cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".moka-radial-menu[role='menu']")), Wait);
		string[] calls = [.. _module.Invocations.Select(i => i.Identifier).Where(i => i is "measureViewport" or "bind")];
		Assert.Equal(["measureViewport", "bind"], calls);
	}

	[Fact]
	public void ACornerFan_WithoutAWindowSize_OpensUpAndLeft()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(shape: MokaRadialShape.CornerFan);

		cut.WaitForAssertion(() => Assert.Contains("moka-radial-menu--opens-left", Ring(cut).ClassList), Wait);
		Assert.Contains("moka-radial-menu--opens-up", Ring(cut).ClassList);
	}

	[Fact]
	public void Compact_HasADotAndATagPerButton_AndNoHubTrackOrDigits()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(shape: MokaRadialShape.Compact);

		Assert.Empty(cut.FindAll(".moka-radial-menu__hub"));
		Assert.Empty(cut.FindAll(".moka-radial-menu__track"));
		Assert.Empty(cut.FindAll(".moka-radial-menu__key"));
		Assert.Equal("true", cut.Find(".moka-radial-menu__dot").GetAttribute("aria-hidden"));
		IReadOnlyList<IElement> tags = cut.FindAll(".moka-radial-menu__slot > .moka-radial-menu__tag");
		Assert.Equal(["Open", "Limit", "Rename", "Allow", "Block", "Forget"], tags.Select(t => t.TextContent));
		Assert.All(tags, t => Assert.Equal("true", t.GetAttribute("aria-hidden")));
		Assert.Equal("1", Button(cut, "Open").GetAttribute("aria-keyshortcuts"));
	}

	[Fact]
	public async Task Labeled_PrintsEachWordWithItsDigit_AndTheHubKeepsSayingWhatTheThingIs()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(shape: MokaRadialShape.Labeled);

		IReadOnlyList<IElement> labels = cut.FindAll(".moka-radial-menu__slot > .moka-radial-menu__label");
		Assert.Equal(6, labels.Count);
		Assert.Equal("1", labels[0].QuerySelector(".moka-radial-menu__label-key")!.TextContent);
		Assert.Equal("1Open", string.Concat(labels[0].TextContent.Where(c => !char.IsWhiteSpace(c))));
		Assert.All(labels, l => Assert.Equal("true", l.GetAttribute("aria-hidden")));
		Assert.Empty(cut.FindAll(".moka-radial-menu__button > .moka-radial-menu__key"));
		Assert.Equal("6", Button(cut, "Forget").GetAttribute("aria-keyshortcuts"));

		await Press(cut, "End");

		Assert.Equal("Forget", Lit(cut));
		Assert.Equal("Device", HubWord(cut));
	}

	[Fact]
	public void Marking_DrawsALineAndAPillPerButton_AndADotForTheCentre()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(shape: MokaRadialShape.Marking);

		Assert.Empty(cut.FindAll(".moka-radial-menu__hub"));
		Assert.Empty(cut.FindAll(".moka-radial-menu__track"));
		Assert.Empty(cut.FindAll(".moka-radial-menu__key"));
		Assert.Single(cut.FindAll(".moka-radial-menu__dot"));
		Assert.All(Slots(cut), s =>
		{
			Assert.Equal("moka-radial-menu__line", s.Children[0].ClassName);
			Assert.Equal("true", s.Children[0].GetAttribute("aria-hidden"));
		});
		Assert.Equal(["Open", "Limit", "Rename", "Allow", "Block", "Forget"],
			cut.FindAll(".moka-radial-menu__button > .moka-radial-menu__text").Select(t => t.TextContent));
	}

	[Fact]
	public void TheSpecsShape_IsUsed_AndTheParameterWins()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(Device() with { Shape = MokaRadialShape.Pie });
		Assert.Contains("moka-radial-menu--pie", Ring(cut).ClassList);

		cut.Render(p => p.Add(x => x.Shape, MokaRadialShape.Compact));

		Assert.Contains("moka-radial-menu--compact", Ring(cut).ClassList);
		Assert.DoesNotContain("moka-radial-menu--pie", Ring(cut).ClassList);
	}

	[Fact]
	public void ChangingTheShapeWhileOpen_OpensItAgainInTheNewShape()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();
		_module.VerifyInvoke("bind", 1);

		cut.Render(p => p.Add(x => x.Shape, MokaRadialShape.HalfRing));

		Assert.Contains("moka-radial-menu--half-ring", Ring(cut).ClassList);
		_module.VerifyInvoke("bind", 2);
	}

	[Fact]
	public void AdaptingToEdges_IsOffByDefault()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(x: 5, y: 5);

		Assert.Equal("moka-radial-menu moka-radial-menu--success", Ring(cut).ClassName);
		Assert.DoesNotContain(_module.Invocations, i => i.Identifier == "measureViewport");
	}

	[Theory]
	[InlineData(600, 400, null)]
	[InlineData(600, 60, null)]
	[InlineData(60, 400, null)]
	[InlineData(600, 760, "moka-radial-menu--half-ring")]
	[InlineData(1150, 760, "moka-radial-menu--corner-fan moka-radial-menu--opens-left moka-radial-menu--opens-up")]
	[InlineData(50, 50, "moka-radial-menu--corner-fan moka-radial-menu--opens-right moka-radial-menu--opens-down")]
	[InlineData(50, 760, "moka-radial-menu--corner-fan moka-radial-menu--opens-right moka-radial-menu--opens-up")]
	public void AdaptingToEdges_TurnsARingIntoAHalfRingAtTheBottom_AndAFanAtACorner(double x, double y, string? expected)
	{
		_module.Setup<MokaRadialMenu.ViewportSize?>("measureViewport")
			.SetResult(new MokaRadialMenu.ViewportSize { Width = 1200, Height = 800 });

		IRenderedComponent<MokaRadialMenu> cut = RenderRing(x: x, y: y, adapt: true);

		cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".moka-radial-menu[role='menu']")), Wait);
		string shapeClasses = string.Join(' ', Ring(cut).ClassList.Where(c => c is not "moka-radial-menu" and not "moka-radial-menu--success"));
		Assert.Equal(expected ?? string.Empty, shapeClasses);
	}

	[Fact]
	public void AdaptingToEdges_LeavesOtherShapesAlone()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(x: 5, y: 5, shape: MokaRadialShape.Pie, adapt: true);

		Assert.Contains("moka-radial-menu--pie", Ring(cut).ClassList);
		Assert.DoesNotContain(_module.Invocations, i => i.Identifier == "measureViewport");
	}

	[Fact]
	public void TheSpecCanAskToAdapt()
	{
		_module.Setup<MokaRadialMenu.ViewportSize?>("measureViewport")
			.SetResult(new MokaRadialMenu.ViewportSize { Width = 1200, Height = 800 });

		IRenderedComponent<MokaRadialMenu> cut = RenderRing(Device() with { AdaptToEdges = true }, x: 600, y: 790);

		cut.WaitForAssertion(() => Assert.Contains("moka-radial-menu--half-ring", Ring(cut).ClassList), Wait);
	}

	[Theory]
	[InlineData(MokaRadialShape.Ring, true, 1.0, 0.44, false)]
	[InlineData(MokaRadialShape.Pie, true, 1.0, 0.44, true)]
	[InlineData(MokaRadialShape.HalfRing, false, 1.0, 0.48, false)]
	[InlineData(MokaRadialShape.CornerFan, false, 0.8, 0.96, false)]
	[InlineData(MokaRadialShape.Compact, true, 1.0, 0.44, false)]
	[InlineData(MokaRadialShape.Labeled, true, 1.0, 0.44, false)]
	[InlineData(MokaRadialShape.Marking, true, 1.0, 0.44, false)]
	public void TheModule_IsToldHowToFollowThePointerInEachShape(MokaRadialShape shape, bool around, double reach, double inner, bool turn)
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(shape: shape);

		cut.WaitForAssertion(() => _module.VerifyInvoke("bind"), Wait);
		using JsonDocument options = BindOptions();
		Assert.Equal(around, options.RootElement.GetProperty("around").GetBoolean());
		Assert.Equal(reach, options.RootElement.GetProperty("reach").GetDouble());
		Assert.Equal(inner, options.RootElement.GetProperty("inner").GetDouble());
		Assert.Equal(turn, options.RootElement.GetProperty("turn").GetBoolean());
		Assert.False(options.RootElement.GetProperty("arc").GetBoolean());
	}

	[Theory]
	[InlineData(MokaRadialShape.Ring)]
	[InlineData(MokaRadialShape.Pie)]
	[InlineData(MokaRadialShape.HalfRing)]
	[InlineData(MokaRadialShape.CornerFan)]
	[InlineData(MokaRadialShape.Compact)]
	[InlineData(MokaRadialShape.Labeled)]
	[InlineData(MokaRadialShape.Marking)]
	public async Task EveryShape_TakesTheSameKeys(MokaRadialShape shape)
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(shape: shape);
		cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".moka-radial-menu[role='menu']")), Wait);

		await Press(cut, "ArrowRight");
		await Press(cut, "Tab");
		Assert.Equal("Limit", Lit(cut));
		await Press(cut, "Enter");
		Assert.Equal("50 Mbit/s", Lit(cut));
		await Press(cut, "Escape");
		await Press(cut, "5");

		Assert.Equal(["block"], _picked);
	}

	[Theory]
	[InlineData(MokaRadialShape.Pie)]
	[InlineData(MokaRadialShape.HalfRing)]
	[InlineData(MokaRadialShape.Compact)]
	[InlineData(MokaRadialShape.Marking)]
	public async Task EveryShape_TakesTheModulesPointerAndFlick(MokaRadialShape shape)
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(shape: shape);

		await cut.InvokeAsync(() => cut.Instance.OnPointed(3, -1));
		await Press(cut, "ArrowRight");
		Assert.Equal("Block", Lit(cut));

		await cut.InvokeAsync(() => cut.Instance.OnFlicked(0, -1));
		cut.WaitForAssertion(() => Assert.Equal(["open"], _picked), Wait);
	}

	private static string[] Angles(IRenderedComponent<MokaRadialMenu> cut) =>
	[
		.. Slots(cut).Select(s => s.GetAttribute("style")!.Split(';')[0].Replace("--moka-radial-angle: ", "", StringComparison.Ordinal))
	];

	private JsonDocument BindOptions()
	{
		JSRuntimeInvocation bind = _module.Invocations.Last(i => i.Identifier == "bind");
		return JsonDocument.Parse(JsonSerializer.Serialize(bind.Arguments[3]));
	}

	private IRenderedComponent<MokaRadialMenu> RenderRing(MokaRadialMenuSpec? spec = null, double x = 300, double y = 300,
		MokaRadialShape? shape = null, bool? adapt = null) =>
		Render<MokaRadialMenu>(p => p
			.Add(m => m.Spec, spec ?? Device())
			.Add(m => m.Visible, true)
			.Add(m => m.X, x)
			.Add(m => m.Y, y)
			.Add(m => m.Shape, shape)
			.Add(m => m.AdaptToEdges, adapt)
			.Add(m => m.OnPick, pick => _picked.Add(pick.Answer)));

	private static Task Press(IRenderedComponent<MokaRadialMenu> cut, string key) =>
		Ring(cut).KeyDownAsync(new KeyboardEventArgs { Key = key });
}
