using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.Cascader;
using Moka.Red.Forms.DateRangePicker;
using Moka.Red.Forms.Rating;
using Moka.Red.Forms.SchedulePicker;
using Moka.Red.Forms.SelectField;

namespace Moka.Red.Forms.Tests.Components;

// A hover listener is a round trip and a render of the whole component for the mouse passing over a row:
// 412 edits and 18 KB in a select of 200 options. Lists let the browser track it, and the rest only listen while
// hover does something.
public class HoverStateTests : BunitContext
{
	private const string HoverModule = "./_content/Moka.Red.Core/moka-hover.js";

	private static readonly List<string> Fruit = ["Apple", "Banana", "Cherry", "Damson"];

	private readonly BunitJSModuleInterop _hover;

	public HoverStateTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_hover = JSInterop.SetupModule(HoverModule);
		_hover.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void Select_OptionsHaveNoHoverListener_AndCarryTheirIndex()
	{
		IRenderedComponent<MokaSelect<string>> cut = RenderOpenSelect();

		var options = cut.FindAll(".moka-select-option");

		Assert.All(options, option => Assert.False(option.HasAttribute("blazor:onmouseenter")));
		Assert.Equal(["0", "1", "2", "3"], options.Select(option => option.GetAttribute("data-moka-index")));
	}

	[Fact]
	public void Select_TracksHoverOncePerOpening()
	{
		IRenderedComponent<MokaSelect<string>> cut = RenderOpenSelect();

		cut.Render();

		Assert.Single(_hover.Invocations["trackHover"]);
	}

	[Fact]
	public void Select_AnArrowKeyCarriesOnFromTheHoveredOption()
	{
		IRenderedComponent<MokaSelect<string>> cut = RenderOpenSelect();

		cut.InvokeAsync(() => cut.Instance.OnItemHovered(1));
		cut.Find(".moka-select-trigger").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

		Assert.Contains("moka-select-option--focused", cut.FindAll(".moka-select-option")[2].ClassList);
	}

	[Fact]
	public void Select_EnterPicksTheHoveredOption()
	{
		List<string> values = [];
		IRenderedComponent<MokaSelect<string>> cut = RenderOpenSelect(values);

		cut.InvokeAsync(() => cut.Instance.OnItemHovered(2));
		cut.Find(".moka-select-trigger").KeyDown(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal(["Cherry"], values);
	}

	// Being told about the hover isn't a reason to render: the next key's own render shows it.
	[Fact]
	public void Select_HearingAboutAHover_DoesNotRender()
	{
		IRenderedComponent<MokaSelect<string>> cut = RenderOpenSelect();
		int renders = cut.RenderCount;

		cut.InvokeAsync(() => cut.Instance.OnItemHovered(1));

		Assert.Equal(renders, cut.RenderCount);
	}

	[Fact]
	public void Cascader_OnlyTheCurrentColumnReportsAnIndex()
	{
		IRenderedComponent<MokaCascader> cut = Render<MokaCascader>(p => p
			.Add(x => x.IsOpen, true)
			.Add(x => x.Options, new List<MokaCascaderOption>
			{
				new() { Value = "earth", Children = [new MokaCascaderOption { Value = "land" }] },
				new() { Value = "mars" }
			}));

		cut.FindAll(".moka-cascader-option")[0].Click();

		var options = cut.FindAll(".moka-cascader-option");
		Assert.Equal(["-1", "-1", "0"], options.Select(option => option.GetAttribute("data-moka-index")));
		Assert.All(options, option => Assert.False(option.HasAttribute("blazor:onmouseenter")));
		Assert.Single(_hover.Invocations["trackHover"]);
	}

	[Fact]
	public void DateRangePicker_ListensToHoverOnlyWhilePickingTheEnd()
	{
		IRenderedComponent<MokaDateRangePicker> cut = Render<MokaDateRangePicker>();

		cut.Find("input").Click();
		Assert.All(EnabledDays(cut), day => Assert.False(day.HasAttribute("blazor:onmouseenter")));

		EnabledDays(cut)[10].Click();
		Assert.All(EnabledDays(cut), day => Assert.True(day.HasAttribute("blazor:onmouseenter")));

		EnabledDays(cut)[12].MouseEnter();
		Assert.NotEmpty(cut.FindAll(".moka-daterange__day--in-range"));

		EnabledDays(cut)[14].Click();
		cut.Find("input").Click();
		Assert.All(EnabledDays(cut), day => Assert.False(day.HasAttribute("blazor:onmouseenter")));
	}

	[Fact]
	public void SchedulePicker_ListensToHoverOnlyDuringADrag()
	{
		List<int> reports = [];
		IRenderedComponent<MokaSchedulePicker> cut = Render<MokaSchedulePicker>(p => p
			.Add(x => x.SelectedSlotsChanged, slots => reports.Add(slots.Count)));
		Assert.All(Slots(cut), slot => Assert.False(slot.HasAttribute("blazor:onmouseenter")));
		Assert.False(cut.Find(".moka-schedule-picker").HasAttribute("blazor:onmouseleave"));

		Slots(cut)[0].MouseDown();
		Assert.All(Slots(cut), slot => Assert.True(slot.HasAttribute("blazor:onmouseenter")));
		Assert.True(cut.Find(".moka-schedule-picker").HasAttribute("blazor:onmouseleave"));

		Slots(cut)[1].MouseEnter();
		Assert.Equal([1, 2], reports);

		cut.Find(".moka-schedule-picker").MouseUp();
		Assert.All(Slots(cut), slot => Assert.False(slot.HasAttribute("blazor:onmouseenter")));
	}

	[Fact]
	public void SchedulePicker_LeavingEndsTheDrag()
	{
		IRenderedComponent<MokaSchedulePicker> cut = Render<MokaSchedulePicker>();

		Slots(cut)[0].MouseDown();
		cut.Find(".moka-schedule-picker").MouseLeave();

		Assert.All(Slots(cut), slot => Assert.False(slot.HasAttribute("blazor:onmouseenter")));
	}

	[Fact]
	public void Rating_APreviewListensToHover()
	{
		IRenderedComponent<MokaRating> cut = Render<MokaRating>();

		cut.FindAll(".moka-rating-star")[2].MouseEnter();

		Assert.Equal(3, cut.FindAll(".moka-rating-star--filled").Count);

		cut.Find(".moka-rating").MouseLeave();
		Assert.Empty(cut.FindAll(".moka-rating-star--filled"));
	}

	[Theory]
	[InlineData(nameof(MokaRating.ReadOnly), true)]
	[InlineData(nameof(MokaRating.Disabled), true)]
	[InlineData(nameof(MokaRating.HoverPreview), false)]
	public void Rating_WithNothingToPreview_DoesNotListen(string parameter, bool value)
	{
		IRenderedComponent<MokaRating> cut = Render<MokaRating>();

		cut.Render(ParameterView.FromDictionary(new Dictionary<string, object?> { [parameter] = value }));

		Assert.All(cut.FindAll(".moka-rating-star"), star => Assert.False(star.HasAttribute("blazor:onmouseenter")));
		Assert.False(cut.Find(".moka-rating").HasAttribute("blazor:onmouseleave"));
	}

	private IRenderedComponent<MokaSelect<string>> RenderOpenSelect(List<string>? values = null) =>
		Render<MokaSelect<string>>(p => p
			.Add(x => x.Items, Fruit)
			.Add(x => x.IsOpen, true)
			.Add(x => x.ValueChanged, value => values?.Add(value)));

	private static List<AngleSharp.Dom.IElement> EnabledDays(IRenderedComponent<MokaDateRangePicker> cut) =>
		cut.FindAll(".moka-daterange__days button:not([disabled])").ToList();

	private static List<AngleSharp.Dom.IElement> Slots(IRenderedComponent<MokaSchedulePicker> cut) =>
		cut.FindAll(".moka-schedule-picker__slot").ToList();
}
