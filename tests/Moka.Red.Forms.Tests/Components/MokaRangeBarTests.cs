using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Forms.RangeBar;

namespace Moka.Red.Forms.Tests.Components;

// One track with a range dragged inside it and what's already on the scale along it. MokaRangeSlider is the plain
// two-thumb slider.
public class MokaRangeBarTests : BunitContext
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";
	private const string ModulePath = "./_content/Moka.Red.Forms/RangeBar/MokaRangeBar.razor.js";

	private readonly List<double> _ends = [];
	private readonly List<double> _starts = [];
	private readonly List<MokaRange> _values = [];

	public MokaRangeBarTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void TheBar_IsAGroupNamedByItsLabel()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar();

		IElement group = cut.Find(".moka-range-bar");
		Assert.Equal("group", group.GetAttribute("role"));
		string labelId = cut.Find("label.moka-field-label").Id!;
		Assert.Equal(labelId, group.GetAttribute("aria-labelledby"));
		Assert.Null(cut.Find("label.moka-field-label").GetAttribute("for"));
	}

	[Fact]
	public void EachHandle_IsASliderWithItsLimitsAndValue()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar();

		IElement start = Handle(cut, "start");
		Assert.Equal("slider", start.GetAttribute("role"));
		Assert.Equal("0", start.GetAttribute("tabindex"));
		Assert.Equal("Pool start", start.GetAttribute("aria-label"));
		Assert.Equal("0", start.GetAttribute("aria-valuemin"));
		Assert.Equal("60", start.GetAttribute("aria-valuemax"));
		Assert.Equal("20", start.GetAttribute("aria-valuenow"));
		Assert.Equal("20", start.GetAttribute("aria-valuetext"));

		IElement end = Handle(cut, "end");
		Assert.Equal("Pool end", end.GetAttribute("aria-label"));
		Assert.Equal("20", end.GetAttribute("aria-valuemin"));
		Assert.Equal("100", end.GetAttribute("aria-valuemax"));
		Assert.Equal("60", end.GetAttribute("aria-valuenow"));
	}

	[Fact]
	public void TheHandleNames_CanBeGiven()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p
			.Add(x => x.StartLabel, "First automatic address")
			.Add(x => x.EndLabel, "Last automatic address"));

		Assert.Equal("First automatic address", Handle(cut, "start").GetAttribute("aria-label"));
		Assert.Equal("Last automatic address", Handle(cut, "end").GetAttribute("aria-label"));
	}

	[Fact]
	public void TheIdGoesOnTheStartHandle_AndTheLabelFollowsIt()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p.Add(x => x.Id, "pool"));

		Assert.Equal("pool", Handle(cut, "start").Id);
		Assert.Equal("pool-label", cut.Find("label.moka-field-label").Id);
		Assert.Null(cut.Find(".moka-range-bar").GetAttribute("id"));
	}

	[Fact]
	public void TheRange_IsPlacedThroughTheBarsCustomProperties()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar();

		Assert.Equal(
			"--_range-start: 0.2; --_range-end: 0.6; --_range-start-chars: 2; --_range-end-chars: 2",
			cut.Find(".moka-range-bar__bar").GetAttribute("style"));
		Assert.Equal("20", cut.Find("[data-moka-flag=start]").TextContent);
		Assert.Equal("60", cut.Find("[data-moka-flag=end]").TextContent);
	}

	[Fact]
	public void WithNothingBound_TheRangeIsTheWholeScale()
	{
		IRenderedComponent<MokaRangeBar> cut = Render<MokaRangeBar>(p => p.Add(x => x.Min, 10).Add(x => x.Max, 50));

		Assert.Equal("10", Handle(cut, "start").GetAttribute("aria-valuenow"));
		Assert.Equal("50", Handle(cut, "end").GetAttribute("aria-valuenow"));
	}

	[Fact]
	public void Values_OutsideTheScaleOrOutOfOrder_AreShownInsideAndInOrder()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(start: 140, end: -5);

		Assert.Equal("0", Handle(cut, "start").GetAttribute("aria-valuenow"));
		Assert.Equal("100", Handle(cut, "end").GetAttribute("aria-valuenow"));
		Assert.Empty(_starts);
		Assert.Empty(_ends);
	}

	[Fact]
	public void ADataAttribute_CarriesTheScaleForTheScript()
	{
		IElement bar = Bar(p => p.Add(x => x.Step, 0.5)).Find(".moka-range-bar__bar");

		Assert.Equal("0", bar.GetAttribute("data-min"));
		Assert.Equal("100", bar.GetAttribute("data-max"));
		Assert.Equal("0.5", bar.GetAttribute("data-step"));
		Assert.Equal("1", bar.GetAttribute("data-decimals"));
		Assert.Equal("20", bar.GetAttribute("data-start"));
		Assert.Equal("60", bar.GetAttribute("data-end"));
		Assert.False(bar.HasAttribute("data-disabled"));
	}

	[Theory]
	[InlineData("ArrowRight", false, 21d)]
	[InlineData("ArrowUp", false, 21d)]
	[InlineData("ArrowLeft", false, 19d)]
	[InlineData("ArrowDown", false, 19d)]
	[InlineData("ArrowRight", true, 30d)]
	[InlineData("ArrowLeft", true, 10d)]
	[InlineData("PageUp", false, 30d)]
	[InlineData("PageDown", false, 10d)]
	[InlineData("Home", false, 0d)]
	[InlineData("End", false, 60d)]
	public void Keys_MoveTheStartHandle(string key, bool shift, double expected)
	{
		IRenderedComponent<MokaRangeBar> cut = Bar();

		Handle(cut, "start").KeyDown(new KeyboardEventArgs { Key = key, ShiftKey = shift });

		Assert.Equal([expected], _starts);
		Assert.Empty(_ends);
		Assert.Equal(expected.ToString(CultureInfo.InvariantCulture),
			Handle(cut, "start").GetAttribute("aria-valuenow"));
	}

	[Theory]
	[InlineData("ArrowRight", 61d)]
	[InlineData("ArrowLeft", 59d)]
	[InlineData("Home", 20d)]
	[InlineData("End", 100d)]
	public void Keys_MoveTheEndHandle(string key, double expected)
	{
		IRenderedComponent<MokaRangeBar> cut = Bar();

		Handle(cut, "end").KeyDown(new KeyboardEventArgs { Key = key });

		Assert.Equal([expected], _ends);
		Assert.Empty(_starts);
	}

	[Fact]
	public void EveryChange_IsAlsoReportedAsARange()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar();

		Handle(cut, "start").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
		Handle(cut, "end").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

		Assert.Equal([new MokaRange(21, 60), new MokaRange(21, 59)], _values);
	}

	[Fact]
	public void TheStart_NeverPassesTheEnd()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(start: 58);

		Handle(cut, "start").KeyDown(new KeyboardEventArgs { Key = "ArrowRight", ShiftKey = true });
		Handle(cut, "start").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
		Handle(cut, "start").KeyDown(new KeyboardEventArgs { Key = "PageUp" });

		Assert.Equal([60d], _starts);
		Assert.Equal("60", Handle(cut, "start").GetAttribute("aria-valuenow"));
	}

	[Fact]
	public void TheEnd_NeverPassesTheStart()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(end: 23);

		Handle(cut, "end").KeyDown(new KeyboardEventArgs { Key = "PageDown" });
		Handle(cut, "end").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

		Assert.Equal([20d], _ends);
	}

	[Fact]
	public void TheHandles_StopAtTheEndsOfTheScale()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(start: 3, end: 97);

		Handle(cut, "start").KeyDown(new KeyboardEventArgs { Key = "PageDown" });
		Handle(cut, "end").KeyDown(new KeyboardEventArgs { Key = "PageUp" });

		Assert.Equal([0d], _starts);
		Assert.Equal([100d], _ends);
	}

	[Fact]
	public void KeysWithCtrlAltOrMeta_StayWithTheBrowser()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar();

		Handle(cut, "start").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft", AltKey = true });
		Handle(cut, "start").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft", CtrlKey = true });
		Handle(cut, "start").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft", MetaKey = true });

		Assert.Empty(_starts);
	}

	[Fact]
	public void AStepWithDecimals_LandsOnTheGrid()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p.Add(x => x.Max, 1).Add(x => x.Step, 0.1), start: 0, end: 1);

		for (int i = 0; i < 7; i++)
		{
			Handle(cut, "start").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
		}

		Assert.Equal(0.7, _starts[^1]);
	}

	[Fact]
	public void WithoutAStep_TheKeysMoveAHundredthOfTheScale()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p.Add(x => x.Step, 0).Add(x => x.Max, 400));

		Handle(cut, "start").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

		Assert.Equal([24d], _starts);
	}

	[Fact]
	public void TheKeysItActsOn_AreCancelledInTheBrowser()
	{
		BunitJSModuleInterop keys = JSInterop.SetupModule(KeysModule);
		keys.Mode = JSRuntimeMode.Loose;

		Bar();

		JSRuntimeInvocation call = Assert.Single(keys.Invocations, i => i.Identifier == "preventKeys");
		using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(call.Arguments[1]));
		JsonElement rule = Assert.Single(document.RootElement.EnumerateArray());
		Assert.Equal("[role=\"slider\"]", rule.GetProperty("selector").GetString());
		string?[] cancelled = [.. rule.GetProperty("keys").EnumerateArray().Select(k => k.GetString())];
		Assert.Contains("ArrowRight", cancelled);
		Assert.Contains("Home", cancelled);
		Assert.Contains("PageDown", cancelled);
		Assert.True(rule.GetProperty("unlessModified").GetBoolean());
	}

	[Fact]
	public void TheDragScript_IsBoundOnce_AndLetGoOfOnDisposal()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
		module.Mode = JSRuntimeMode.Loose;

		IRenderedComponent<MokaRangeBar> cut = Bar();
		cut.Render(p => p.Add(x => x.Label, "Addresses"));

		Assert.Single(module.Invocations, i => i.Identifier == "bind");
	}

	[Fact]
	public async Task Disposal_UnbindsTheDragScript()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaRangeBar> cut = Bar();

		await cut.Instance.DisposeAsync();

		Assert.Single(module.Invocations, i => i.Identifier == "unbind");
	}

	[Fact]
	public async Task ADrag_ReportsWhereItWasLetGo()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar();

		await cut.InvokeAsync(() => cut.Instance.OnDragged(34.4, 60));

		cut.WaitForAssertion(() => Assert.Equal([34d], _starts));
		Assert.Empty(_ends);
		Assert.Equal([new MokaRange(34, 60)], _values);
		Assert.Equal("34", Handle(cut, "start").GetAttribute("aria-valuenow"));
		Assert.Contains("--_range-start: 0.34", cut.Find(".moka-range-bar__bar").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public async Task ADrag_ThatCrossedTheHandles_IsPutInOrder()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar();

		await cut.InvokeAsync(() => cut.Instance.OnDragged(80, 60));

		cut.WaitForAssertion(() => Assert.Equal([new MokaRange(60, 80)], _values));
	}

	[Fact]
	public async Task ADrag_ToWhereItStarted_ReportsNothing()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar();

		await cut.InvokeAsync(() => cut.Instance.OnDragged(20, 60));

		Assert.Empty(_values);
	}

	[Fact]
	public void AParentThatRendersAgain_DoesNotUndoAChange()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar();
		Handle(cut, "start").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

		cut.Render(p => p.Add(x => x.Start, 20).Add(x => x.Label, "Pool"));

		Assert.Equal("21", Handle(cut, "start").GetAttribute("aria-valuenow"));
	}

	[Fact]
	public void ANewValueFromTheParent_MovesTheHandle()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar();

		cut.Render(p => p.Add(x => x.Start, 45));

		Assert.Equal("45", Handle(cut, "start").GetAttribute("aria-valuenow"));
	}

	[Fact]
	public void Value_PlacesTheRange_WhenStartAndEndAreNotGiven()
	{
		IRenderedComponent<MokaRangeBar> cut = Render<MokaRangeBar>(p => p.Add(x => x.Value, new MokaRange(30, 70)));

		Assert.Equal("30", Handle(cut, "start").GetAttribute("aria-valuenow"));
		Assert.Equal("70", Handle(cut, "end").GetAttribute("aria-valuenow"));
	}

	[Fact]
	public void TheFormatter_WritesTheFlagsTheScaleAndWhatScreenReadersHear()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p
			.Add(x => x.Formatter, v => "." + v.ToString(CultureInfo.InvariantCulture))
			.Add(x => x.Ticks, [new MokaRangeMark(40, "laptop")]));

		Assert.Equal(".20", cut.Find("[data-moka-flag=start]").TextContent);
		Assert.Equal(".60", cut.Find("[data-moka-flag=end]").TextContent);
		Assert.Equal(".0", cut.Find(".moka-range-bar__scale--low").TextContent);
		Assert.Equal(".100", cut.Find(".moka-range-bar__scale--high").TextContent);
		Assert.Equal(".20", Handle(cut, "start").GetAttribute("aria-valuetext"));
		Assert.Equal("laptop: .40", cut.FindComponent<MokaTooltip>().Instance.Text);
		Assert.Contains("--_range-start-chars: 3", cut.Find(".moka-range-bar__bar").GetAttribute("style"), StringComparison.Ordinal);
		Assert.Equal(".55", cut.Instance.FormatValue(55));
	}

	[Fact]
	public void Parts_AreDrawnWhereTheySitWithTheirToneAndLabel()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p.Add(x => x.Parts,
		[
			new MokaRangePart(8, 22, "Fixed", MokaColor.Info),
			new MokaRangePart(74, 62, "Guests", MokaColor.Warning)
		]));

		IReadOnlyList<IElement> parts = cut.FindAll(".moka-range-bar__track > .moka-range-bar__part");
		Assert.Equal(2, parts.Count);
		Assert.Equal("left: 8%; width: 14%", parts[0].GetAttribute("style"));
		Assert.Contains("moka-range-bar__tone--info", parts[0].ClassName, StringComparison.Ordinal);
		Assert.Equal("Fixed", parts[0].QuerySelector(".moka-range-bar__label")!.TextContent);
		Assert.Equal("left: 62%; width: 12%", parts[1].GetAttribute("style"));
		Assert.Contains("moka-range-bar__tone--warning", parts[1].ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void APart_OutsideTheScale_IsLeftOut()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p.Add(x => x.Parts,
			[new MokaRangePart(120, 140, "Beyond"), new MokaRangePart(-10, 5, "Edge")]));

		IElement part = Assert.Single(cut.FindAll(".moka-range-bar__part"));
		Assert.Equal("left: 0%; width: 5%", part.GetAttribute("style"));
	}

	[Fact]
	public void AnUnnamedPart_HasNoLabel_AndAPartIsNeutralByDefault()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p.Add(x => x.Parts, [new MokaRangePart(8, 22)]));

		IElement part = cut.Find(".moka-range-bar__part");
		Assert.Empty(part.Children);
		Assert.Contains("moka-range-bar__tone--secondary", part.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void TheRangeLabel_SitsInsideTheRange()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p.Add(x => x.RangeLabel, "Handed out"));

		Assert.Equal("Handed out", cut.Find(".moka-range-bar__range .moka-range-bar__label").TextContent);
		Assert.Contains("moka-range-bar__tone--primary", cut.Find(".moka-range-bar__range").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Color_TonesTheRangeAndTheHandles()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p.Add(x => x.Color, MokaColor.Success));

		Assert.Contains("moka-range-bar__tone--success", cut.Find(".moka-range-bar__range").ClassName, StringComparison.Ordinal);
		Assert.All(cut.FindAll("[role=slider]"),
			handle => Assert.Contains("moka-range-bar__tone--success", handle.ClassName, StringComparison.Ordinal));
	}

	[Fact]
	public void TicksAndPins_AreDrawnUnderTheTrack()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p
			.Add(x => x.Ticks, [new MokaRangeMark(44), new MokaRangeMark(150)])
			.Add(x => x.Pins, [new MokaRangeMark(34, Color: MokaColor.Success), new MokaRangeMark(48)]));

		IElement tick = Assert.Single(cut.FindAll(".moka-range-bar__marks > .moka-range-bar__tick"));
		Assert.Equal("left: 44%", tick.GetAttribute("style"));
		Assert.DoesNotContain("moka-range-bar__tick--toned", tick.ClassName, StringComparison.Ordinal);
		IReadOnlyList<IElement> pins = cut.FindAll(".moka-range-bar__marks > .moka-range-bar__pin");
		Assert.Equal(2, pins.Count);
		Assert.Contains("moka-range-bar__tone--success", pins[0].ClassName, StringComparison.Ordinal);
		Assert.Contains("moka-range-bar__tone--primary", pins[1].ClassName, StringComparison.Ordinal);
		Assert.Equal("true", cut.Find(".moka-range-bar__marks").GetAttribute("aria-hidden"));
	}

	[Fact]
	public void ANamedMark_SitsInATooltipThatTakesItsPlace()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p.Add(x => x.Pins, [new MokaRangeMark(34, "printer")]));

		IRenderedComponent<MokaTooltip> tooltip = cut.FindComponent<MokaTooltip>();
		Assert.Equal("printer: 34", tooltip.Instance.Text);
		Assert.Equal("moka-range-bar__mark-tip", tooltip.Instance.Class);
		Assert.Equal("left: 34%", tooltip.Instance.Style);
		IElement pin = tooltip.Find(".moka-range-bar__pin");
		Assert.DoesNotContain("moka-range-bar__mark--placed", pin.ClassName, StringComparison.Ordinal);
		Assert.False(pin.HasAttribute("style"));
	}

	[Fact]
	public void WithoutMarks_ThereIsNoMarkRow()
	{
		Assert.Empty(Bar().FindAll(".moka-range-bar__marks"));
	}

	[Fact]
	public void ScreenReaders_HearWhatIsWhere()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p
			.Add(x => x.Parts, [new MokaRangePart(8, 22, "Fixed"), new MokaRangePart(70, 80, "Fixed")])
			.Add(x => x.Pins, [new MokaRangeMark(34, "printer")]));

		IElement list = cut.Find("ul");
		Assert.Contains("moka-visually-hidden", list.ClassName, StringComparison.Ordinal);
		List<IElement> items = [.. list.QuerySelectorAll("li")];
		Assert.Equal(2, items.Count);
		Assert.Equal("Fixed: 8 to 22, 70 to 80", items[0].TextContent.Trim());
		Assert.Equal("printer: 34", items[1].TextContent.Trim());
		Assert.Equal("true", cut.Find(".moka-range-bar__track").GetAttribute("aria-hidden"));
	}

	[Fact]
	public void WithNothingNamed_ThereIsNoList()
	{
		Assert.Empty(Bar(p => p.Add(x => x.Parts, [new MokaRangePart(8, 22)])).FindAll("ul"));
	}

	[Fact]
	public void WithoutLabels_ThePartsAreSolidAndAKeyNamesThem()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p
			.Add(x => x.Labels, false)
			.Add(x => x.RangeLabel, "Handed out")
			.Add(x => x.Color, MokaColor.Success)
			.Add(x => x.Parts,
			[
				new MokaRangePart(62, 74, "Guests", MokaColor.Warning),
				new MokaRangePart(8, 22, "Fixed addresses", MokaColor.Info)
			]));

		Assert.Contains("moka-range-bar--solid", cut.Find(".moka-range-bar").ClassName, StringComparison.Ordinal);
		Assert.Empty(cut.FindAll(".moka-range-bar__label"));
		IElement key = cut.Find("ul.moka-range-bar__key");
		Assert.DoesNotContain("moka-visually-hidden", key.ClassName, StringComparison.Ordinal);
		IReadOnlyList<IElement> items = cut.FindAll(".moka-range-bar__key-item");
		Assert.Equal(3, items.Count);
		Assert.Equal("Fixed addresses: 8 to 22", items[0].TextContent.Trim());
		Assert.Equal("Handed out: 20 to 60", items[1].TextContent.Trim());
		Assert.Equal("Guests: 62 to 74", items[2].TextContent.Trim());
		Assert.Contains("moka-range-bar__tone--success", items[1].QuerySelector(".moka-range-bar__key-dot")!.ClassName, StringComparison.Ordinal);
		Assert.Equal("true", items[0].QuerySelector(".moka-range-bar__key-dot")!.GetAttribute("aria-hidden"));
		Assert.NotNull(items[0].QuerySelector(".moka-visually-hidden"));
	}

	[Fact]
	public void ReadOnly_HasNoHandlesOrFlags_AndDrawsTheRangeAsAPart()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p
			.Add(x => x.ReadOnly, true)
			.Add(x => x.Labels, false)
			.Add(x => x.Color, MokaColor.Success)
			.Add(x => x.Id, "pool"));

		Assert.Empty(cut.FindAll("[role=slider]"));
		Assert.Empty(cut.FindAll(".moka-range-bar__flag"));
		Assert.Empty(cut.FindAll(".moka-range-bar__range"));
		IElement range = cut.Find(".moka-range-bar__part--range");
		Assert.Equal("left: 20%; width: 40%", range.GetAttribute("style"));
		Assert.Contains("moka-range-bar__tone--success", range.ClassName, StringComparison.Ordinal);
		Assert.Contains("moka-range-bar__bar--static", cut.Find(".moka-range-bar__bar").ClassName, StringComparison.Ordinal);
		Assert.Equal("pool", cut.Find(".moka-range-bar").Id);
	}

	[Fact]
	public void ReadOnly_LoadsNoScript()
	{
		Bar(p => p.Add(x => x.ReadOnly, true));

		Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "import");
	}

	[Fact]
	public void ReadOnlyWithLabels_LabelsTheRangeInsideIt()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p.Add(x => x.ReadOnly, true).Add(x => x.RangeLabel, "Handed out"));

		Assert.Equal("Handed out", cut.Find(".moka-range-bar__part--range .moka-range-bar__label").TextContent);
	}

	[Fact]
	public void ShowScaleOff_LeavesOutTheEnds()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p.Add(x => x.ShowScale, false));

		Assert.Empty(cut.FindAll(".moka-range-bar__scale"));
		Assert.Contains("moka-range-bar__bar--no-scale", cut.Find(".moka-range-bar__bar").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Disabled_TakesTheHandlesOutOfTheTabOrderAndIgnoresKeys()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p.Add(x => x.Disabled, true));

		IElement start = Handle(cut, "start");
		Assert.False(start.HasAttribute("tabindex"));
		Assert.Equal("true", start.GetAttribute("aria-disabled"));
		Assert.Equal("true", cut.Find(".moka-range-bar__bar").GetAttribute("data-disabled"));

		start.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

		Assert.Empty(_starts);
	}

	[Fact]
	public void ErrorText_ShowsTheErrorState()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p.Add(x => x.ErrorText, "Too narrow"));

		Assert.Contains("moka-range-bar--error", cut.Find(".moka-range-bar").ClassName, StringComparison.Ordinal);
		Assert.Equal("Too narrow", cut.Find(".moka-field-helper").TextContent.Trim());
	}

	[Fact]
	public void InAnEditForm_TheRangeIsValidatedAsItChanges()
	{
		Booking booking = new() { Pool = new MokaRange(10, 40) };
		IRenderedComponent<EditForm> cut = Render<EditForm>(p => p
			.Add(f => f.Model, booking)
			.Add(f => f.ChildContent, _ => builder =>
			{
				builder.OpenComponent<DataAnnotationsValidator>(0);
				builder.CloseComponent();
				builder.OpenComponent<MokaRangeBar>(1);
				builder.AddAttribute(2, nameof(MokaRangeBar.Value), booking.Pool);
				builder.AddAttribute(3, nameof(MokaRangeBar.ValueChanged),
					EventCallback.Factory.Create<MokaRange>(this, range => booking.Pool = range));
				builder.AddAttribute(4, nameof(MokaRangeBar.ValueExpression),
					(Expression<Func<MokaRange>>)(() => booking.Pool));
				builder.AddAttribute(5, nameof(MokaRangeBar.Label), "Pool");
				builder.CloseComponent();
			}));

		cut.Find("[data-moka-handle=end]").KeyDown(new KeyboardEventArgs { Key = "Home" });

		Assert.Equal(new MokaRange(10, 10), booking.Pool);
		Assert.Equal("Keep at least 20 values in the pool.", cut.Find(".moka-field-helper").TextContent.Trim());
		Assert.Contains("invalid", cut.Find(".moka-range-bar").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void TheRange_ReadsAndWritesItsText()
	{
		using RangeText text = new();

		Assert.Equal("10.5..-2", text.Write(new MokaRange(10.5, -2)));
		Assert.Equal(new MokaRange(-5, 7.25), text.Read("7.25..-5"));
		Assert.Null(text.Read("seven"));
	}

	private static IElement Handle(IRenderedComponent<MokaRangeBar> cut, string which) =>
		cut.Find($"[data-moka-handle={which}]");

	private IRenderedComponent<MokaRangeBar> Bar(
		Action<ComponentParameterCollectionBuilder<MokaRangeBar>>? extra = null, double start = 20, double end = 60) =>
		Render<MokaRangeBar>(p =>
		{
			p.Add(x => x.Start, start)
				.Add(x => x.End, end)
				.Add(x => x.Label, "Pool")
				.Add(x => x.StartChanged, EventCallback.Factory.Create<double>(this, _starts.Add))
				.Add(x => x.EndChanged, EventCallback.Factory.Create<double>(this, _ends.Add))
				.Add(x => x.ValueChanged, EventCallback.Factory.Create<MokaRange>(this, _values.Add));
			extra?.Invoke(p);
		});

	private sealed class Booking
	{
		[AtLeastWide(20)]
		public MokaRange Pool { get; set; }
	}

	[AttributeUsage(AttributeTargets.Property)]
	private sealed class AtLeastWideAttribute(double width) : ValidationAttribute("Keep at least 20 values in the pool.")
	{
		public double Width { get; } = width;

		public override bool IsValid(object? value) => value is MokaRange range && range.End - range.Start >= Width;
	}

	// The text form InputBase asks every input for.
	private sealed class RangeText : MokaRangeBar
	{
		public string? Write(MokaRange range) => FormatValueAsString(range);

		public MokaRange? Read(string text) => TryParseValueFromString(text, out MokaRange range, out _) ? range : null;
	}
}
