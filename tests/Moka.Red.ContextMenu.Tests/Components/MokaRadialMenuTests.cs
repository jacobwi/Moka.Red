using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Enums;
using Moka.Red.Icons;
using static Moka.Red.ContextMenu.Tests.RadialDom;

namespace Moka.Red.ContextMenu.Tests.Components;

// Focus stays on the ring and aria-activedescendant names the lit button or choice, so "highlighted" here is
// what Lit reads. The pointer's highlight lives in the module, which tests reach through OnPointed and OnFlicked.
public class MokaRadialMenuTests : BunitContext
{
	private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

	private readonly BunitJSModuleInterop _module;
	private readonly List<string> _picked = [];
	private readonly List<string> _specPicks = [];
	private int _closes;

	public MokaRadialMenuTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
	}

	[Fact]
	public void Closed_DrawsNothing()
	{
		IRenderedComponent<MokaRadialMenu> cut = Render<MokaRadialMenu>(p => p.Add(x => x.Spec, Device()));

		Assert.Empty(cut.FindAll(".moka-radial-menu"));
		Assert.Empty(cut.FindAll(".moka-radial-menu-backdrop"));
	}

	[Fact]
	public void WithoutASpec_DrawsNothing()
	{
		IRenderedComponent<MokaRadialMenu> cut = Render<MokaRadialMenu>(p => p.Add(x => x.Visible, true));

		Assert.Empty(cut.FindAll(".moka-radial-menu"));
	}

	[Fact]
	public void Open_DrawsAMenuItemPerButton_NamedByItsLabel()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		IElement ring = Ring(cut);
		Assert.Equal("menu", ring.GetAttribute("role"));
		Assert.Equal("Office desktop", ring.GetAttribute("aria-label"));
		IReadOnlyList<IElement> buttons = cut.FindAll(".moka-radial-menu__button");
		Assert.Equal(["Open", "Limit", "Rename", "Allow", "Block", "Forget"], buttons.Select(b => b.GetAttribute("aria-label")));
		Assert.All(buttons, b =>
		{
			Assert.Equal("menuitem", b.GetAttribute("role"));
			Assert.Equal("button", b.GetAttribute("type"));
			Assert.Equal("-1", b.GetAttribute("tabindex"));
			Assert.False(b.HasAttribute("title"));
		});
	}

	[Fact]
	public void TheHub_ShowsTheTitleAndSub_ForTheEyeOnly()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		IElement hub = cut.Find(".moka-radial-menu__hub");
		Assert.Equal("true", hub.GetAttribute("aria-hidden"));
		Assert.Equal("Office desktop", hub.QuerySelector(".moka-radial-menu__title")!.TextContent);
		Assert.Equal("Device", HubWord(cut));
		Assert.Equal("Device", hub.GetAttribute("data-moka-sub"));
		Assert.NotNull(hub.QuerySelector(".moka-radial-menu__hub-icon"));
		Assert.Equal("", hub.QuerySelector(".moka-radial-menu__pointer-word")!.TextContent);
	}

	[Fact]
	public void TheHubColor_IsAClass_ExceptForTheAccent()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();
		Assert.Contains("moka-radial-menu--success", Ring(cut).ClassName, StringComparison.Ordinal);

		cut.Render(p => p.Add(x => x.Spec, Device() with { Color = MokaColor.Primary }));
		Assert.Equal("moka-radial-menu", Ring(cut).ClassName);
	}

	[Fact]
	public void Tones_AndDisabled_AreClassesOnTheSlots()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();
		IReadOnlyList<IElement> slots = Slots(cut);

		Assert.Contains("moka-radial-menu__slot--good", slots[3].ClassName, StringComparison.Ordinal);
		Assert.Contains("moka-radial-menu__slot--danger", slots[4].ClassName, StringComparison.Ordinal);
		Assert.Contains("moka-radial-menu__slot--disabled", slots[5].ClassName, StringComparison.Ordinal);
		Assert.Equal("moka-radial-menu__slot", slots[0].ClassName);
		Assert.Equal("true", Button(cut, "Forget").GetAttribute("aria-disabled"));
		Assert.False(Button(cut, "Open").HasAttribute("aria-disabled"));
	}

	[Fact]
	public void EachSlot_CarriesItsAngleAndOpeningDelay()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();
		IReadOnlyList<IElement> slots = Slots(cut);

		Assert.Equal("--moka-radial-angle: -90deg; --moka-radial-delay: 0ms", slots[0].GetAttribute("style"));
		Assert.Equal("--moka-radial-angle: -30deg; --moka-radial-delay: 16ms", slots[1].GetAttribute("style"));
		Assert.Equal("--moka-radial-angle: 210deg; --moka-radial-delay: 80ms", slots[5].GetAttribute("style"));
	}

	[Fact]
	public void TheRing_IsCentredOnThePoint()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(x: 320.5, y: 410);

		Assert.Equal("--moka-radial-x: 320.5px; --moka-radial-y: 410px", Ring(cut).GetAttribute("style"));
	}

	[Fact]
	public void TheFirstNineButtons_ShowTheirDigit_AndSayItsShortcut()
	{
		MokaRadialItem[] items = [.. Enumerable.Range(1, 10).Select(i => new MokaRadialItem($"i{i}", $"Item {i}", MokaIcons.Action.Add))];
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(new MokaRadialMenuSpec("Ten", items));

		IReadOnlyList<IElement> buttons = cut.FindAll(".moka-radial-menu__button");
		Assert.Equal("1", buttons[0].GetAttribute("aria-keyshortcuts"));
		Assert.Equal("1", buttons[0].QuerySelector(".moka-radial-menu__key")!.TextContent);
		Assert.Equal("true", buttons[0].QuerySelector(".moka-radial-menu__key")!.GetAttribute("aria-hidden"));
		Assert.Equal("9", buttons[8].GetAttribute("aria-keyshortcuts"));
		Assert.False(buttons[9].HasAttribute("aria-keyshortcuts"));
		Assert.Null(buttons[9].QuerySelector(".moka-radial-menu__key"));
	}

	[Fact]
	public void AButtonWithChoices_SaysItOpensAMenu_AndListsThem()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		IElement limit = Button(cut, "Limit");
		Assert.Equal("menu", limit.GetAttribute("aria-haspopup"));
		Assert.Equal("false", limit.GetAttribute("aria-expanded"));
		Assert.NotNull(limit.QuerySelector(".moka-radial-menu__more"));
		Assert.False(Button(cut, "Open").HasAttribute("aria-expanded"));

		IElement list = cut.Find(".moka-radial-menu__choices");
		Assert.Equal("menu", list.GetAttribute("role"));
		Assert.Equal("Limit", list.GetAttribute("aria-label"));
		List<IElement> choices = [.. list.QuerySelectorAll(".moka-radial-menu__choice")];
		Assert.Equal(["false", "true", "false"], choices.Select(c => c.GetAttribute("aria-checked")));
		Assert.All(choices, c => Assert.Equal("menuitemradio", c.GetAttribute("role")));
		Assert.NotNull(choices[1].QuerySelector(".moka-radial-menu__check"));
		Assert.Null(choices[0].QuerySelector(".moka-radial-menu__check"));
	}

	[Theory]
	[InlineData(0, "top")]
	[InlineData(1, "right")]
	[InlineData(2, "right")]
	[InlineData(3, "bottom")]
	[InlineData(4, "left")]
	[InlineData(5, "left")]
	public void AList_OpensOnTheSideAwayFromTheHub(int position, string side)
	{
		MokaRadialItem[] items = [.. Enumerable.Range(0, 6).Select(i => new MokaRadialItem($"i{i}", $"Item {i}", MokaIcons.Action.Add,
			Choices: [new MokaRadialChoice("a", "A")]))];
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(new MokaRadialMenuSpec("Six", items));

		Assert.Contains($"moka-radial-menu__choices--{side}", cut.FindAll(".moka-radial-menu__choices")[position].ClassName,
			StringComparison.Ordinal);
	}

	[Fact]
	public void ADisabledButton_ListsNoChoices()
	{
		MokaRadialMenuSpec spec = new("Thing",
			[new MokaRadialItem("limit", "Limit", MokaIcons.Status.Clock, Disabled: true, Choices: [new MokaRadialChoice("a", "A")])]);
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(spec);

		Assert.Empty(cut.FindAll(".moka-radial-menu__choices"));
		Assert.False(Button(cut, "Limit").HasAttribute("aria-expanded"));
	}

	[Fact]
	public void OpenedByPointer_HighlightsNothing()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		Assert.Null(Lit(cut));
	}

	[Fact]
	public void OpenedFromTheKeyboard_StartsOnTheFirstButton()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(keyboard: true);

		Assert.Equal("Open", Lit(cut));
		Assert.Equal("Open", HubWord(cut));
	}

	[Theory]
	[InlineData("ArrowRight", false)]
	[InlineData("ArrowDown", false)]
	[InlineData("Tab", false)]
	public async Task NextKeys_GoClockwise_AndWrap(string key, bool shift)
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		await Press(cut, key, shift);
		Assert.Equal("Open", Lit(cut));
		await Press(cut, key, shift);
		Assert.Equal("Limit", Lit(cut));

		for (int i = 0; i < 4; i++)
		{
			await Press(cut, key, shift);
		}

		Assert.Equal("Forget", Lit(cut));
		await Press(cut, key, shift);
		Assert.Equal("Open", Lit(cut));
	}

	[Theory]
	[InlineData("ArrowLeft", false)]
	[InlineData("ArrowUp", false)]
	[InlineData("Tab", true)]
	public async Task PreviousKeys_GoAnticlockwise_StartingFromTheLast(string key, bool shift)
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		await Press(cut, key, shift);
		Assert.Equal("Forget", Lit(cut));
		await Press(cut, key, shift);
		Assert.Equal("Block", Lit(cut));
	}

	[Fact]
	public async Task HomeAndEnd_GoToTheFirstAndLastButtons()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		await Press(cut, "End");
		Assert.Equal("Forget", Lit(cut));
		await Press(cut, "Home");
		Assert.Equal("Open", Lit(cut));
	}

	[Fact]
	public async Task TheHub_SaysTheHighlightedButton()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		await Press(cut, "End");

		Assert.Equal("Forget", HubWord(cut));
	}

	[Theory]
	[InlineData("Enter")]
	[InlineData(" ")]
	public async Task EnterOrSpace_PicksTheHighlightedButton_AfterClosing(string key)
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(keyboard: true);

		await Press(cut, key);

		Assert.Equal(["open"], _specPicks);
		Assert.Equal(["open"], _picked);
		Assert.Equal(1, _closes);
		Assert.Empty(cut.FindAll(".moka-radial-menu"));
	}

	[Fact]
	public async Task AHeldEnter_PicksNothing()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(keyboard: true);

		await Ring(cut).KeyDownAsync(new KeyboardEventArgs { Key = "Enter", Repeat = true });

		Assert.Empty(_picked);
	}

	[Fact]
	public async Task ADigit_PicksItsButton()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		await Press(cut, "4");

		Assert.Equal(["allow"], _picked);
	}

	[Fact]
	public async Task ADigitPastTheLastButton_DoesNothing()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		await Press(cut, "8");

		Assert.Empty(_picked);
		Assert.Null(Lit(cut));
	}

	[Fact]
	public async Task ADisabledButton_IsReachedButNotPicked()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		await Press(cut, "6");
		Assert.Equal("Forget", Lit(cut));
		await Press(cut, "Enter");
		await Button(cut, "Forget").ClickAsync(new MouseEventArgs());

		Assert.Empty(_picked);
		Assert.Equal(0, _closes);
	}

	[Fact]
	public async Task Modified_KeysAreLeftAlone()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		await Ring(cut).KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight", AltKey = true });
		await Ring(cut).KeyDownAsync(new KeyboardEventArgs { Key = "1", CtrlKey = true });

		Assert.Null(Lit(cut));
		Assert.Empty(_picked);
	}

	[Fact]
	public async Task EnterOnAButtonWithChoices_OpensThem_OnTheCurrentChoice()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		await Press(cut, "2");

		Assert.Empty(_picked);
		Assert.Equal("50 Mbit/s", Lit(cut));
		Assert.Equal("50 Mbit/s", HubWord(cut));
		Assert.Equal("true", Button(cut, "Limit").GetAttribute("aria-expanded"));
		_module.VerifyInvoke("fitChoices");
	}

	[Fact]
	public async Task ChoiceKeys_MoveDownAndUp_AndWrap()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();
		await Press(cut, "2");

		await Press(cut, "ArrowDown");
		Assert.Equal("100 Mbit/s", Lit(cut));
		await Press(cut, "ArrowDown");
		Assert.Equal("10 Mbit/s", Lit(cut));
		await Press(cut, "ArrowUp");
		Assert.Equal("100 Mbit/s", Lit(cut));
	}

	[Fact]
	public async Task PickingAChoice_AnswersTheButtonAndTheChoice()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();
		await Press(cut, "2");
		await Press(cut, "ArrowDown");

		await Press(cut, "Enter");

		Assert.Equal(["limit:100"], _picked);
		Assert.Equal(["limit:100"], _specPicks);
		Assert.Equal(1, _closes);
	}

	[Theory]
	[InlineData("ArrowLeft")]
	[InlineData("Escape")]
	public async Task LeftOrEscapeInAList_GoesBackToItsButton(string key)
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();
		await Press(cut, "2");

		await Press(cut, key);

		Assert.Equal("Limit", Lit(cut));
		Assert.Equal(0, _closes);
		Assert.Single(cut.FindAll(".moka-radial-menu"));
	}

	[Fact]
	public async Task RightInAList_MovesToTheNextButton()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();
		await Press(cut, "2");

		await Press(cut, "ArrowRight");

		Assert.Equal("Rename", Lit(cut));
		Assert.Equal("false", Button(cut, "Limit").GetAttribute("aria-expanded"));
	}

	[Fact]
	public async Task ClickingAButton_PicksIt()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		await Button(cut, "Block").ClickAsync(new MouseEventArgs());

		Assert.Equal(["block"], _picked);
		Assert.Equal(1, _closes);
	}

	[Fact]
	public async Task ClickingAChoice_PicksIt_WithoutOpeningItsButtonFirst()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		await Choice(cut, "10 Mbit/s").ClickAsync(new MouseEventArgs());

		Assert.Equal(["limit:10"], _picked);
	}

	[Fact]
	public async Task ClickingAButtonWithChoices_OpensThem()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		await Button(cut, "Limit").ClickAsync(new MouseEventArgs());

		Assert.Empty(_picked);
		Assert.Equal("50 Mbit/s", Lit(cut));
	}

	[Fact]
	public void ButtonsAndChoices_KeepFocusOnTheRing_WhenPressed()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		Assert.All(cut.FindAll(".moka-radial-menu__button, .moka-radial-menu__choice"),
			b => Assert.True(b.HasAttribute("blazor:onmousedown:preventDefault")));
	}

	[Fact]
	public async Task Escape_ClosesTheRing_AndPutsFocusBack()
	{
		_module.Setup<string?>("captureFocus").SetResult("token-1");
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		await Press(cut, "Escape");

		Assert.Equal(1, _closes);
		Assert.Empty(_picked);
		Assert.Empty(cut.FindAll(".moka-radial-menu"));
		JSRuntimeInvocation restore = _module.VerifyInvoke("restoreFocus");
		Assert.Equal("token-1", restore.Arguments[0]);
	}

	[Fact]
	public async Task AClickOutside_ClosesTheRing()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		await cut.Find(".moka-radial-menu-backdrop").ClickAsync(new MouseEventArgs());

		Assert.Equal(1, _closes);
		Assert.Empty(cut.FindAll(".moka-radial-menu"));
	}

	[Fact]
	public async Task ARightClickOutside_ClosesTheRing_InsteadOfTheBrowsersMenu()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();
		IElement backdrop = cut.Find(".moka-radial-menu-backdrop");
		Assert.True(backdrop.HasAttribute("blazor:oncontextmenu:preventDefault"));

		await backdrop.ContextMenuAsync(new MouseEventArgs { Button = 2 });

		Assert.Equal(1, _closes);
	}

	[Fact]
	public void KeysStopAtTheRing_AndItsOwnContextMenuIsCancelled()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();
		IElement ring = Ring(cut);

		Assert.True(ring.HasAttribute("blazor:onkeydown:stopPropagation"));
		Assert.True(ring.HasAttribute("blazor:oncontextmenu:preventDefault"));
		Assert.True(ring.HasAttribute("blazor:oncontextmenu:stopPropagation"));
	}

	[Fact]
	public void Opening_TakesFocus_AndCancelsTheBrowsersPartOfTheKeys()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		_module.VerifyInvoke("captureFocus");
		JSInterop.VerifyFocusAsyncInvoke();
		JSRuntimeInvocation call = _module.VerifyInvoke("preventKeys");
		using JsonDocument rules = JsonDocument.Parse(JsonSerializer.Serialize(call.Arguments[1]));
		JsonElement[] all = [.. rules.RootElement.EnumerateArray()];
		Assert.Equal(2, all.Length);
		Assert.Equal(JsonValueKind.Null, all[0].GetProperty("selector").ValueKind);
		Assert.Equal("button", all[1].GetProperty("selector").GetString());
		Assert.All(all, rule =>
		{
			string?[] keys = [.. rule.GetProperty("keys").EnumerateArray().Select(k => k.GetString())];
			Assert.Contains("ArrowRight", keys);
			Assert.Contains("Tab", keys);
			Assert.Contains(" ", keys);
			Assert.True(rule.GetProperty("unlessModified").GetBoolean());
		});
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void Opening_HandsThePointerToTheModule_SayingWhetherAButtonIsHeld(bool flick)
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(flick: flick);

		JSRuntimeInvocation bind = _module.VerifyInvoke("bind");
		Assert.IsType<ElementReference>(bind.Arguments[0]);
		Assert.IsType<DotNetObjectReference<MokaRadialMenu>>(bind.Arguments[1]);
		Assert.Equal(flick, bind.Arguments[2]);
	}

	[Fact]
	public async Task WhereThePointerWas_IsWhereTheNextKeyStarts()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		await cut.InvokeAsync(() => cut.Instance.OnPointed(3, -1));
		await Press(cut, "ArrowRight");
		Assert.Equal("Block", Lit(cut));

		await cut.InvokeAsync(() => cut.Instance.OnPointed(1, 2));
		await Press(cut, "Enter");
		Assert.Equal(["limit:100"], _picked);
	}

	[Fact]
	public async Task ThePointerOnTheHub_LeavesNothingForTheKeys()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(keyboard: true);

		await cut.InvokeAsync(() => cut.Instance.OnPointed(-1, -1));
		await Press(cut, "Enter");

		Assert.Empty(_picked);
	}

	[Fact]
	public async Task AFlick_PicksWhatItWasLetGoOver()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(flick: true);

		await cut.InvokeAsync(() => cut.Instance.OnFlicked(1, 0));

		cut.WaitForAssertion(() => Assert.Equal(["limit:10"], _picked), Wait);
		Assert.Equal(1, _closes);
	}

	[Fact]
	public async Task AFlickOntoADisabledButton_PicksNothing()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(flick: true);

		await cut.InvokeAsync(() => cut.Instance.OnFlicked(5, -1));
		await cut.InvokeAsync(() => cut.Instance.OnFlicked(17, -1));

		Assert.Empty(_picked);
		Assert.Single(cut.FindAll(".moka-radial-menu"));
	}

	// A release over a button can be followed by a click on it, and the second must not pick again.
	[Fact]
	public async Task OnlyTheFirstPick_Lands()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(flick: true);

		await cut.InvokeAsync(() => cut.Instance.OnFlicked(4, -1));
		cut.WaitForAssertion(() => Assert.Equal(["block"], _picked), Wait);
		await cut.InvokeAsync(() => cut.Instance.OnFlicked(3, -1));

		Assert.Equal(["block"], _picked);
		Assert.Equal(1, _closes);
	}

	[Fact]
	public void AnchoredToFocus_MeasuresFocusFirst_ThenCentresOnIt()
	{
		_module.Setup<MokaRadialMenu.FocusCenter?>("measureFocusCenter")
			.SetResult(new MokaRadialMenu.FocusCenter { X = 200, Y = 150.25 });

		IRenderedComponent<MokaRadialMenu> cut = RenderRing(keyboard: true, anchorToFocus: true);

		cut.WaitForAssertion(() =>
			Assert.Equal("--moka-radial-x: 200px; --moka-radial-y: 150.25px", Ring(cut).GetAttribute("style")), Wait);
		JSRuntimeInvocation[] calls = [.. _module.Invocations.Where(i => i.Identifier is "captureFocus" or "measureFocusCenter" or "bind")];
		Assert.Equal(["captureFocus", "measureFocusCenter", "bind"], calls.Select(c => c.Identifier));
		Assert.Equal("Open", Lit(cut));
	}

	[Fact]
	public void AnchoredToFocus_WithNothingFocused_StaysOnThePoint()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(x: 40, y: 50, anchorToFocus: true);

		cut.WaitForAssertion(() =>
			Assert.Equal("--moka-radial-x: 40px; --moka-radial-y: 50px", Ring(cut).GetAttribute("style")), Wait);
	}

	[Fact]
	public void ClosedFromOutside_PutsFocusBack()
	{
		_module.Setup<string?>("captureFocus").SetResult("token-2");
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		cut.Render(p => p.Add(x => x.Visible, false));

		Assert.Empty(cut.FindAll(".moka-radial-menu"));
		JSRuntimeInvocation restore = _module.VerifyInvoke("restoreFocus");
		Assert.Equal("token-2", restore.Arguments[0]);
		Assert.Equal(0, _closes);
	}

	[Fact]
	public async Task AfterClosingItself_ItStaysClosed_UntilOpenedAgain()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();
		await Press(cut, "Escape");

		cut.Render(p => p.Add(x => x.Spec, Device()));
		Assert.Empty(cut.FindAll(".moka-radial-menu"));

		cut.Render(p => p.Add(x => x.Visible, false));
		cut.Render(p => p.Add(x => x.Visible, true));
		Assert.Single(cut.FindAll(".moka-radial-menu"));
	}

	[Fact]
	public async Task ANewSpecWhileOpen_KeepsTheHighlight()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();
		await Press(cut, "End");

		cut.Render(p => p.Add(x => x.Spec, Device()));

		Assert.Equal("Forget", Lit(cut));
		_module.VerifyInvoke("bind", 1);
	}

	[Fact]
	public void ClassStyleIdAndAttributes_ReachTheRing()
	{
		IRenderedComponent<MokaRadialMenu> cut = Render<MokaRadialMenu>(p => p
			.Add(x => x.Spec, Device() with { Color = MokaColor.Primary })
			.Add(x => x.Visible, true)
			.Add(x => x.X, 10)
			.Add(x => x.Y, 20)
			.Add(x => x.Class, "mine")
			.Add(x => x.Style, "color: red")
			.Add(x => x.Id, "ring")
			.AddUnmatched("data-test", "yes"));

		IElement ring = Ring(cut);
		Assert.Equal("moka-radial-menu mine", ring.ClassName);
		Assert.Equal("--moka-radial-x: 10px; --moka-radial-y: 20px; color: red", ring.GetAttribute("style"));
		Assert.Equal("ring", ring.Id);
		Assert.Equal("yes", ring.GetAttribute("data-test"));
	}

	private IRenderedComponent<MokaRadialMenu> RenderRing(MokaRadialMenuSpec? spec = null, double x = 300, double y = 300,
		bool keyboard = false, bool anchorToFocus = false, bool flick = false) =>
		Render<MokaRadialMenu>(p => p
			.Add(m => m.Spec, spec ?? Device(pick =>
			{
				_specPicks.Add(pick.Answer);
				return Task.CompletedTask;
			}))
			.Add(m => m.Visible, true)
			.Add(m => m.X, x)
			.Add(m => m.Y, y)
			.Add(m => m.OpenedFromKeyboard, keyboard)
			.Add(m => m.AnchorToFocus, anchorToFocus)
			.Add(m => m.Flick, flick)
			.Add(m => m.OnPick, pick => _picked.Add(pick.Answer))
			.Add(m => m.OnClose, () => _closes++));

	private static Task Press(IRenderedComponent<MokaRadialMenu> cut, string key, bool shift = false) =>
		Ring(cut).KeyDownAsync(new KeyboardEventArgs { Key = key, ShiftKey = shift });
}
