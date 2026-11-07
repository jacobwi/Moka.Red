using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.TimePicker;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

public class MokaTimePickerTypedTests : BunitContext
{
	private const string Css = "MokaTimePicker.razor.css";

	private static readonly TimeSpan NineThirtyPm = new(21, 30, 0);

	[Fact]
	public void TypedOff_TheInputReadsAsItDid()
	{
		IRenderedComponent<MokaTimePicker> cut = Render<MokaTimePicker>(p => p
			.Add(x => x.Id, "f")
			.Add(x => x.Value, NineThirtyPm));

		Assert.Equal(
			"type=text class=moka-timepicker-input moka-timepicker-input--md value=21:30 readonly= placeholder=Select time... " +
			"aria-haspopup=dialog aria-expanded=false blazor:onclick blazor:onkeydown id=f",
			AttributesOf(cut.Find("input")));
	}

	[Fact]
	public void TypedOff_AClickOnTheInputOpensThePopup()
	{
		IRenderedComponent<MokaTimePicker> cut = Render<MokaTimePicker>();

		cut.Find("input").Click();

		Assert.Single(cut.FindAll(".moka-timepicker-dropdown"));
	}

	[Fact]
	public async Task TypedOff_SpaceOnTheInputOpensThePopup()
	{
		IRenderedComponent<MokaTimePicker> cut = Render<MokaTimePicker>();

		await cut.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = " " });

		Assert.Single(cut.FindAll(".moka-timepicker-dropdown"));
	}

	[Fact]
	public void Typed_TheInputTakesText()
	{
		IRenderedComponent<MokaTimePicker> cut = Render<MokaTimePicker>(p => p.Add(x => x.Typed, true));

		IElement input = cut.Find("input");
		Assert.False(input.HasAttribute("readonly"));
		Assert.False(input.HasAttribute("aria-haspopup"));
		Assert.False(input.HasAttribute("aria-expanded"));
		Assert.Equal("off", input.GetAttribute("autocomplete"));
		Assert.Contains("moka-timepicker-input--typed", input.ClassList);
		Assert.Equal("dialog", cut.Find(".moka-timepicker-toggle").GetAttribute("aria-haspopup"));
	}

	[Fact]
	public void Typed_ReadOnlyStaysReadOnly()
	{
		IRenderedComponent<MokaTimePicker> cut = Render<MokaTimePicker>(p => p
			.Add(x => x.Typed, true)
			.Add(x => x.ReadOnly, true));

		Assert.True(cut.Find("input").HasAttribute("readonly"));
	}

	[Theory]
	[InlineData(true, "21:30")]
	[InlineData(false, "9:30 PM")]
	public void Typed_ThePlaceholderShowsAnExample_InTheFieldsFormat(bool format24, string expected)
	{
		IRenderedComponent<MokaTimePicker> cut = Render<MokaTimePicker>(p => p
			.Add(x => x.Typed, true)
			.Add(x => x.Format24, format24));

		Assert.Equal(expected, cut.Find("input").GetAttribute("placeholder"));
	}

	[Fact]
	public void Typed_APlaceholderYouPassedIsKept()
	{
		IRenderedComponent<MokaTimePicker> cut = Render<MokaTimePicker>(p => p
			.Add(x => x.Typed, true)
			.Add(x => x.Placeholder, "Start"));

		Assert.Equal("Start", cut.Find("input").GetAttribute("placeholder"));
	}

	[Fact]
	public void Typed_AClickOnTheInputPutsTheCaretThere_AndTheButtonOpensThePopup()
	{
		IRenderedComponent<MokaTimePicker> cut = Render<MokaTimePicker>(p => p.Add(x => x.Typed, true));

		Assert.False(cut.Find("input").HasAttribute("blazor:onclick"));

		cut.Find(".moka-timepicker-toggle").Click();

		Assert.Single(cut.FindAll(".moka-timepicker-dropdown"));
	}

	[Fact]
	public async Task Typed_SpaceIsTyped_AndArrowDownStillOpensThePopup()
	{
		IRenderedComponent<MokaTimePicker> cut = Render<MokaTimePicker>(p => p.Add(x => x.Typed, true));

		await cut.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = " " });
		Assert.Empty(cut.FindAll(".moka-timepicker-dropdown"));

		await cut.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
		Assert.Single(cut.FindAll(".moka-timepicker-dropdown"));
	}

	[Theory]
	[InlineData("21:30")]
	[InlineData("9:30 pm")]
	[InlineData("9.30p")]
	[InlineData("21h30")]
	[InlineData("2130")]
	[InlineData("930pm")]
	public async Task Typed_ReadsEveryForm_AndShowsItBack(string text)
	{
		foreach ((bool format24, string shown) in new[] { (true, "21:30"), (false, "09:30 PM") })
		{
			List<TimeSpan?> changes = [];
			IRenderedComponent<MokaTimePicker> cut = RenderTyped(format24, null, changes);

			await Commit(cut, text);

			Assert.Equal([NineThirtyPm], changes);
			Assert.Equal(shown, cut.Find("input").GetAttribute("value"));
			Assert.False(cut.Find("input").HasAttribute("aria-invalid"));
		}
	}

	[Theory]
	[InlineData(true, "21:00")]
	[InlineData(false, "09:00 PM")]
	public async Task Typed_ReadsAnHourWithAMarker_OrOverTwelve(bool format24, string shown)
	{
		foreach (string text in new[] { "9 pm", "21" })
		{
			IRenderedComponent<MokaTimePicker> cut = RenderTyped(format24, null);

			await Commit(cut, text);

			Assert.Equal(new TimeSpan(21, 0, 0), cut.Instance.Value);
			Assert.Equal(shown, cut.Find("input").GetAttribute("value"));
		}
	}

	[Theory]
	[InlineData(true, null, "09:00")]
	[InlineData(true, 14, "09:00")]
	[InlineData(false, null, "09:00 AM")]
	[InlineData(false, 8, "09:00 AM")]
	[InlineData(false, 14, "09:00 PM")]
	public async Task Typed_ABareNineFollowsTheSetting(bool format24, int? heldHour, string shown)
	{
		TimeSpan? held = heldHour is { } hour ? new TimeSpan(hour, 0, 0) : null;
		IRenderedComponent<MokaTimePicker> cut = RenderTyped(format24, held);

		await Commit(cut, "9");

		Assert.Equal(shown, cut.Find("input").GetAttribute("value"));
	}

	// The last render already holds "21:30" when "2130" is typed over it, so without a render of the raw text in
	// between, Blazor would see no change and leave "2130" in the box.
	[Fact]
	public async Task Typed_TheSameTimeWrittenAnotherWay_IsWrittenBackOverIt()
	{
		List<TimeSpan?> changes = [];
		IRenderedComponent<MokaTimePicker> cut = RenderTyped(true, NineThirtyPm, changes);
		List<string?> rendered = [];
		cut.OnMarkupUpdated += (_, _) => rendered.Add(cut.Find("input").GetAttribute("value"));

		await Commit(cut, "2130");

		Assert.Empty(changes);
		Assert.Equal(["2130", "21:30"], rendered.Distinct());
	}

	[Theory]
	[InlineData(true, "25:00")]
	[InlineData(true, "abc")]
	[InlineData(true, "12:60")]
	[InlineData(false, "25:00")]
	[InlineData(false, "abc")]
	[InlineData(false, "12:60")]
	public async Task Typed_NonsenseKeepsTheLastGoodValue_AndShowsAnError(bool format24, string text)
	{
		List<TimeSpan?> changes = [];
		IRenderedComponent<MokaTimePicker> cut = RenderTyped(format24, NineThirtyPm, changes);

		await Commit(cut, text);

		IElement input = cut.Find("input");
		Assert.Empty(changes);
		Assert.Equal(NineThirtyPm, cut.Instance.Value);
		Assert.Equal(text, input.GetAttribute("value"));
		Assert.Equal("true", input.GetAttribute("aria-invalid"));
		Assert.Contains("moka-timepicker-input--invalid", input.ClassList);
		Assert.Contains("moka-field--error", cut.Find(".moka-field").ClassList);
		Assert.Contains(format24 ? "21:30" : "9:30 PM", cut.Find(".moka-field-helper").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Typed_EmptyingTheFieldClearsTheValue()
	{
		List<TimeSpan?> changes = [];
		IRenderedComponent<MokaTimePicker> cut = RenderTyped(true, NineThirtyPm, changes);

		await Commit(cut, "");

		Assert.Equal([null], changes);
		Assert.Equal("", cut.Find("input").GetAttribute("value"));
		Assert.False(cut.Find("input").HasAttribute("aria-invalid"));
	}

	[Fact]
	public async Task Typed_AGoodTimeAfterABadOne_ClearsTheError()
	{
		IRenderedComponent<MokaTimePicker> cut = RenderTyped(true, NineThirtyPm);
		await Commit(cut, "abc");

		await Commit(cut, "7");

		IElement input = cut.Find("input");
		Assert.Equal(new TimeSpan(7, 0, 0), cut.Instance.Value);
		Assert.Equal("07:00", input.GetAttribute("value"));
		Assert.False(input.HasAttribute("aria-invalid"));
		Assert.DoesNotContain("moka-field--error", cut.Find(".moka-field").ClassList);
		Assert.Empty(cut.FindAll(".moka-field-helper"));
	}

	[Fact]
	public async Task Typed_ThePopupKeepsWorking_AfterABadTime()
	{
		IRenderedComponent<MokaTimePicker> cut = RenderTyped(true, NineThirtyPm);
		await Commit(cut, "abc");

		await cut.Find(".moka-timepicker-toggle").ClickAsync(new MouseEventArgs());
		await cut.FindAll(".moka-timepicker-column")[0].QuerySelectorAll("button")[8].ClickAsync(new MouseEventArgs());

		IElement input = cut.Find("input");
		Assert.Equal(new TimeSpan(8, 30, 0), cut.Instance.Value);
		Assert.Equal("08:30", input.GetAttribute("value"));
		Assert.False(input.HasAttribute("aria-invalid"));
		Assert.Empty(cut.FindAll(".moka-field-helper"));
	}

	[Fact]
	public async Task Typed_ThePopupFollowsATimeTypedWhileItIsOpen()
	{
		IRenderedComponent<MokaTimePicker> cut = RenderTyped(true, NineThirtyPm);
		await cut.Find(".moka-timepicker-toggle").ClickAsync(new MouseEventArgs());

		await Commit(cut, "7:15");
		await cut.FindAll(".moka-timepicker-column")[1].QuerySelectorAll("button")[20].ClickAsync(new MouseEventArgs());

		Assert.Equal(new TimeSpan(7, 20, 0), cut.Instance.Value);
	}

	[Fact]
	public async Task Typed_ClearAndNowDropARejectedText()
	{
		IRenderedComponent<MokaTimePicker> cut = RenderTyped(true, NineThirtyPm);
		await Commit(cut, "abc");

		await cut.Find(".moka-timepicker-toggle").ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-timepicker-clear").ClickAsync(new MouseEventArgs());

		Assert.Null(cut.Instance.Value);
		Assert.Equal("", cut.Find("input").GetAttribute("value"));
		Assert.False(cut.Find("input").HasAttribute("aria-invalid"));

		await Commit(cut, "abc");
		await cut.Find(".moka-timepicker-now").ClickAsync(new MouseEventArgs());

		Assert.NotNull(cut.Instance.Value);
		Assert.False(cut.Find("input").HasAttribute("aria-invalid"));
	}

	[Theory]
	[InlineData("07:00")]
	[InlineData("18:00")]
	public async Task Typed_MinAndMaxStillApply(string text)
	{
		List<TimeSpan?> changes = [];
		IRenderedComponent<MokaTimePicker> cut = Render<MokaTimePicker>(p => p
			.Add(x => x.Typed, true)
			.Add(x => x.Value, new TimeSpan(9, 0, 0))
			.Add(x => x.Min, new TimeSpan(8, 0, 0))
			.Add(x => x.Max, new TimeSpan(17, 0, 0))
			.Add(x => x.ValueChanged, EventCallback.Factory.Create<TimeSpan?>(this, changes.Add)));

		await Commit(cut, text);

		Assert.Empty(changes);
		Assert.Equal(new TimeSpan(9, 0, 0), cut.Instance.Value);
		Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
		Assert.Equal("From 08:00 to 17:00.", cut.Find(".moka-field-helper").TextContent.Trim());

		await Commit(cut, "16:59");

		Assert.Equal(new TimeSpan(16, 59, 0), cut.Instance.Value);
		Assert.False(cut.Find("input").HasAttribute("aria-invalid"));
	}

	[Theory]
	[InlineData(8, null, "07:00", "At least 08:00.")]
	[InlineData(null, 17, "18:00", "At most 17:00.")]
	public async Task Typed_OneLimit_IsNamedOnItsOwn(int? min, int? max, string text, string message)
	{
		IRenderedComponent<MokaTimePicker> cut = Render<MokaTimePicker>(p => p
			.Add(x => x.Typed, true)
			.Add(x => x.Min, min is { } low ? new TimeSpan(low, 0, 0) : null)
			.Add(x => x.Max, max is { } high ? new TimeSpan(high, 0, 0) : null));

		await Commit(cut, text);

		Assert.Equal(message, cut.Find(".moka-field-helper").TextContent.Trim());
	}

	[Fact]
	public async Task Typed_AParentRenderWithTheSameValue_KeepsTheRejectedText()
	{
		IRenderedComponent<MokaTimePicker> cut = RenderTyped(true, NineThirtyPm);
		await Commit(cut, "abc");

		cut.Render(p => p.Add(x => x.Value, NineThirtyPm));

		Assert.Equal("abc", cut.Find("input").GetAttribute("value"));
		Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
	}

	[Fact]
	public async Task Typed_ANewValueFromTheParent_ReplacesTheRejectedText()
	{
		IRenderedComponent<MokaTimePicker> cut = RenderTyped(true, NineThirtyPm);
		await Commit(cut, "abc");

		cut.Render(p => p.Add(x => x.Value, new TimeSpan(6, 0, 0)));

		Assert.Equal("06:00", cut.Find("input").GetAttribute("value"));
		Assert.False(cut.Find("input").HasAttribute("aria-invalid"));
	}

	[Fact]
	public void Typed_ErrorTextMarksTheInput()
	{
		IRenderedComponent<MokaTimePicker> cut = Render<MokaTimePicker>(p => p
			.Add(x => x.Typed, true)
			.Add(x => x.ErrorText, "Too late"));

		Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
		Assert.Equal("Too late", cut.Find(".moka-field-helper").TextContent.Trim());
	}

	[Fact]
	public async Task Typed_InAForm_ABadTimeHoldsBackTheSubmit_UntilItIsFixed()
	{
		Shift shift = new() { Start = NineThirtyPm };
		int valid = 0;
		int invalid = 0;
		IRenderedComponent<EditForm> cut = RenderForm(shift, () => valid++, () => invalid++);

		await Commit(cut, "abc");
		await cut.Find("form").SubmitAsync();

		Assert.Equal((0, 1), (valid, invalid));
		Assert.Equal(NineThirtyPm, shift.Start);
		Assert.Contains("21:30", cut.Find(".moka-field-helper").TextContent, StringComparison.Ordinal);

		await Commit(cut, "7");
		await cut.Find("form").SubmitAsync();

		Assert.Equal((1, 1), (valid, invalid));
		Assert.Equal(new TimeSpan(7, 0, 0), shift.Start);
		Assert.Empty(cut.FindAll(".moka-field-helper"));
	}

	[Fact]
	public async Task Typed_InAForm_APickFromThePopupTakesTheMessageBackOut()
	{
		Shift shift = new() { Start = NineThirtyPm };
		int valid = 0;
		IRenderedComponent<EditForm> cut = RenderForm(shift, () => valid++, () => { });

		await Commit(cut, "abc");
		await cut.Find(".moka-timepicker-toggle").ClickAsync(new MouseEventArgs());
		await cut.FindAll(".moka-timepicker-column")[0].QuerySelectorAll("button")[8].ClickAsync(new MouseEventArgs());
		await cut.Find("form").SubmitAsync();

		Assert.Equal(1, valid);
		Assert.Equal(new TimeSpan(8, 30, 0), shift.Start);
		Assert.Empty(cut.FindAll(".moka-field-helper"));
	}

	[Fact]
	public void Css_TypedTakesACaret_AndAnErrorDrawsTheBorder()
	{
		Assert.Equal("text", ScopedCss.Declarations(Css, ".moka-timepicker-input--typed")["cursor"]);
		Assert.Equal("var(--moka-color-error)", ScopedCss.Declarations(Css, ".moka-timepicker-input--invalid")["border-color"]);
		Assert.Equal("var(--moka-focus-ring-error)", ScopedCss.Declarations(Css, ".moka-timepicker-input--invalid:focus")["box-shadow"]);
		Assert.Equal(
			"var(--moka-color-error)",
			ScopedCss.Declarations(Css, ".moka-gate .moka-timepicker-input--invalid.moka-timepicker-input:not(:disabled)")["border-color"]);
	}

	private IRenderedComponent<MokaTimePicker> RenderTyped(bool format24, TimeSpan? value, List<TimeSpan?>? changes = null) =>
		Render<MokaTimePicker>(p =>
		{
			p.Add(x => x.Typed, true).Add(x => x.Format24, format24).Add(x => x.Value, value);
			if (changes is not null)
			{
				p.Add(x => x.ValueChanged, EventCallback.Factory.Create<TimeSpan?>(this, changes.Add));
			}
		});

	private IRenderedComponent<EditForm> RenderForm(Shift shift, Action onValid, Action onInvalid) => Render<EditForm>(p => p
		.Add(f => f.Model, shift)
		.Add(f => f.OnValidSubmit, EventCallback.Factory.Create<EditContext>(this, onValid))
		.Add(f => f.OnInvalidSubmit, EventCallback.Factory.Create<EditContext>(this, onInvalid))
		.Add(f => f.ChildContent, _ => builder =>
		{
			builder.OpenComponent<DataAnnotationsValidator>(0);
			builder.CloseComponent();
			builder.OpenComponent<MokaTimePicker>(1);
			builder.AddAttribute(2, nameof(MokaTimePicker.Typed), true);
			builder.AddAttribute(3, nameof(MokaTimePicker.Value), shift.Start);
			builder.AddAttribute(4, nameof(MokaTimePicker.ValueChanged),
				EventCallback.Factory.Create<TimeSpan?>(this, value => shift.Start = value));
			builder.AddAttribute(5, nameof(MokaTimePicker.ValueExpression), (Expression<Func<TimeSpan?>>)(() => shift.Start));
			builder.CloseComponent();
		}));

	private static Task Commit(IRenderedComponent<IComponent> cut, string text) =>
		cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = text });

	private static string AttributesOf(IElement element) => string.Join(" ", element.Attributes
		.Where(attribute => !attribute.Name.StartsWith("b-", StringComparison.Ordinal) &&
		                    !attribute.Name.Equals("blazor:elementreference", StringComparison.OrdinalIgnoreCase))
		.Select(attribute => attribute.Name.StartsWith("blazor:", StringComparison.Ordinal)
			? attribute.Name
			: $"{attribute.Name}={attribute.Value}"));

	private sealed class Shift
	{
		[Required]
		public TimeSpan? Start { get; set; }
	}
}
