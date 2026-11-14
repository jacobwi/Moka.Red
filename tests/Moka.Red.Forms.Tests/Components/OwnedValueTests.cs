using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.Checkbox;
using Moka.Red.Forms.ColorInput;
using Moka.Red.Forms.DatePicker;
using Moka.Red.Forms.NativeSelect;
using Moka.Red.Forms.NumericField;
using Moka.Red.Forms.PinInput;
using Moka.Red.Forms.RadioGroup;
using Moka.Red.Forms.RangeBar;
using Moka.Red.Forms.SelectField;
using Moka.Red.Forms.Slider;
using Moka.Red.Forms.Switch;
using Moka.Red.Forms.TagInput;
using Moka.Red.Forms.TextField;
using Moka.Red.Forms.TimePicker;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// A parent that passes Value and listens to ValueChanged owns the value: a change it doesn't take is undone once its
// handler has finished, a value it passes instead shows, and while its handler runs the input shows the pick. Without
// both, the input keeps its own state. Each family below is checked in every case through its own UI and markup.
public sealed partial class OwnedValueTests : BunitContext
{
	private static readonly Dictionary<string, IFamily> All = new IFamily[]
	{
		new SwitchFamily(), new CheckboxFamily(), new TextFamily(), new NumberFamily(), new SelectFamily(),
		new NativeSelectFamily(), new RadioFamily(), new SliderFamily(), new TimeFamily(), new DateFamily(),
		new PinFamily(), new TagFamily(), new ColorFamily(), new RangeFamily(), new CodeEditorFamily(),
		new RangeSliderFamily(), new RatingFamily(), new KnobFamily(), new CalendarFamily(), new ColorWheelFamily(),
		new AutoCompleteFamily(), new SearchFamily(), new TreeSelectFamily(), new DateRangeFamily(),
		new ScheduleFamily(), new SignatureFamily(), new CreditCardFamily(), new SelectValuesFamily()
	}.ToDictionary(family => family.Name);

	public OwnedValueTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		TopLayerPopup.Setup(JSInterop);
	}

	public static TheoryData<string> Families => [.. All.Keys];

	public static TheoryData<string> ThreeValueFamilies => [.. All.Values.Where(f => f.HasThreeValues).Select(f => f.Name)];

	[Theory]
	[MemberData(nameof(Families))]
	public Task ARefusalAtOnce_PutsTheParentsValueBack(string family) => All[family].RefuseAtOnceAsync(this);

	[Theory]
	[MemberData(nameof(Families))]
	public Task AnAcceptedChange_Stays(string family) => All[family].AcceptAsync(this);

	[Theory]
	[MemberData(nameof(ThreeValueFamilies))]
	public Task AValueTheParentPassesInstead_Shows(string family) => All[family].ClampAsync(this);

	[Theory]
	[MemberData(nameof(Families))]
	public Task AChangeAcceptedLater_ShowsAllAlong(string family) => All[family].AcceptLaterAsync(this);

	[Theory]
	[MemberData(nameof(Families))]
	public Task AChangeRefusedLater_ShowsUntilTheRefusal(string family) => All[family].RefuseLaterAsync(this);

	[Theory]
	[MemberData(nameof(Families))]
	public Task AParentThatRendersWhileBusy_LeavesThePickShowing(string family) => All[family].RefuseLaterWhileBusyAsync(this);

	[Theory]
	[MemberData(nameof(ThreeValueFamilies))]
	public Task TheFirstOfTwoChanges_RefusedLate_DoesNotUndoTheSecond(string family) =>
		All[family].FirstOfTwoRefusedLateAsync(this);

	[Theory]
	[MemberData(nameof(Families))]
	public Task AParentThatOnlyListens_LeavesTheInputItsOwnState(string family) => All[family].ListenerOnlyAsync(this);

	[Theory]
	[MemberData(nameof(Families))]
	public Task AnUnboundInput_KeepsItsOwnState(string family) => All[family].UnboundAsync(this);

	[Theory]
	[MemberData(nameof(Families))]
	public Task BindValue_TakesTheChange(string family) => All[family].BoundAcceptAsync(this);

	[Theory]
	[MemberData(nameof(Families))]
	public Task BindValue_WithASetterThatRefuses_PutsTheValueBack(string family) => All[family].BoundRefuseAsync(this);

	public interface IFamily
	{
		string Name { get; }

		bool HasThreeValues { get; }

		Task RefuseAtOnceAsync(BunitContext context);

		Task AcceptAsync(BunitContext context);

		Task ClampAsync(BunitContext context);

		Task AcceptLaterAsync(BunitContext context);

		Task RefuseLaterAsync(BunitContext context);

		Task RefuseLaterWhileBusyAsync(BunitContext context);

		Task FirstOfTwoRefusedLateAsync(BunitContext context);

		Task ListenerOnlyAsync(BunitContext context);

		Task UnboundAsync(BunitContext context);

		Task BoundAcceptAsync(BunitContext context);

		Task BoundRefuseAsync(BunitContext context);
	}

	private abstract class Family<TInput, TValue> : IFamily
		where TInput : IComponent
	{
		public virtual string Name => typeof(TInput).Name.Split('`')[0];

		public virtual bool HasThreeValues => true;

		// The value the parent starts with, a pick, a second pick, and a value the parent passes in place of a pick.
		protected abstract TValue Initial { get; }

		protected abstract TValue Picked { get; }

		protected virtual TValue Second => throw new NotSupportedException();

		protected virtual TValue Clamped => throw new NotSupportedException();

		// What the input shows when nobody passes it a value.
		protected abstract TValue Default { get; }

		protected virtual IReadOnlyDictionary<string, object?>? With => null;

		protected virtual string ValueName => "Value";

		// Whether the input has an expression parameter for @bind to fill.
		protected virtual bool HasExpression => true;

		// A parameter the parent changes while its handler runs, so it renders the input again.
		protected virtual string BusyParameter => "Disabled";

		public async Task RefuseAtOnceAsync(BunitContext context)
		{
			IRenderedComponent<ValueParent<TInput, TValue>> cut = Parent(context, p => p.Add(x => x.Keep, _ => Initial));
			Assert.Equal(Text(Initial), Shown(cut));

			await ChangeAsync(cut, Picked);

			Assert.Equal(new TValue?[] { Picked }, cut.Instance.Asked);
			Assert.Equal(Initial, cut.Instance.Held);
			Assert.Equal(Text(Initial), Shown(cut));
		}

		public async Task AcceptAsync(BunitContext context)
		{
			IRenderedComponent<ValueParent<TInput, TValue>> cut = Parent(context);

			await ChangeAsync(cut, Picked);
			cut.Render();

			Assert.Equal(Picked, cut.Instance.Held);
			Assert.Equal(Text(Picked), Shown(cut));
		}

		public async Task ClampAsync(BunitContext context)
		{
			IRenderedComponent<ValueParent<TInput, TValue>> cut = Parent(context, p => p.Add(x => x.Keep, _ => Clamped));

			await ChangeAsync(cut, Picked);

			Assert.Equal(new TValue?[] { Picked }, cut.Instance.Asked);
			Assert.Equal(Text(Clamped), Shown(cut));
		}

		public async Task AcceptLaterAsync(BunitContext context)
		{
			TaskCompletionSource<TValue?> answer = new();
			IRenderedComponent<ValueParent<TInput, TValue>> cut = Parent(context, p => p.Add(x => x.Answer, _ => answer.Task));

			await ChangeAsync(cut, Picked);
			Assert.Equal(Text(Picked), Shown(cut));

			await cut.InvokeAsync(() => answer.SetResult(Picked));

			Assert.Equal(Picked, cut.Instance.Held);
			Assert.Equal(Text(Picked), Shown(cut));
		}

		public async Task RefuseLaterAsync(BunitContext context)
		{
			TaskCompletionSource<TValue?> answer = new();
			IRenderedComponent<ValueParent<TInput, TValue>> cut = Parent(context, p => p.Add(x => x.Answer, _ => answer.Task));

			await ChangeAsync(cut, Picked);
			Assert.Equal(Text(Picked), Shown(cut));

			await cut.InvokeAsync(() => answer.SetResult(Initial));

			Assert.Equal(Text(Initial), Shown(cut));
		}

		// The parent renders again while its handler runs, with the value it had: the input still shows the pick.
		public async Task RefuseLaterWhileBusyAsync(BunitContext context)
		{
			TaskCompletionSource<TValue?> answer = new();
			IRenderedComponent<ValueParent<TInput, TValue>> cut = Parent(context, p => p
				.Add(x => x.Answer, _ => answer.Task)
				.Add(x => x.BusyParameter, BusyParameter));

			await ChangeAsync(cut, Picked);
			Assert.Equal(Text(Picked), Shown(cut));

			await cut.InvokeAsync(() => answer.SetResult(Initial));

			Assert.Equal(Text(Initial), Shown(cut));
		}

		public async Task FirstOfTwoRefusedLateAsync(BunitContext context)
		{
			TaskCompletionSource<TValue?> first = new();
			TaskCompletionSource<TValue?> second = new();
			int calls = 0;
			IRenderedComponent<ValueParent<TInput, TValue>> cut = Parent(context, p => p
				.Add(x => x.Answer, _ => ++calls == 1 ? first.Task : second.Task));

			await ChangeAsync(cut, Picked);
			await ChangeAsync(cut, Second);
			await cut.InvokeAsync(() => first.SetResult(Initial));

			Assert.Equal(Text(Second), Shown(cut));

			await cut.InvokeAsync(() => second.SetResult(Second));

			Assert.Equal(new TValue?[] { Picked, Second }, cut.Instance.Asked);
			Assert.Equal(Second, cut.Instance.Held);
			Assert.Equal(Text(Second), Shown(cut));
		}

		public async Task ListenerOnlyAsync(BunitContext context)
		{
			IRenderedComponent<ValueParent<TInput, TValue>> cut = Parent(context, p => p
				.Add(x => x.PassValue, false)
				.Add(x => x.Keep, _ => Initial));
			Assert.Equal(Text(Default), Shown(cut));

			await ChangeAsync(cut, Picked);
			cut.Render();

			Assert.Equal(Picked, cut.Instance.Asked[^1]);
			Assert.Equal(Text(Picked), Shown(cut));
		}

		public async Task UnboundAsync(BunitContext context)
		{
			IRenderedComponent<ValueParent<TInput, TValue>> cut = Parent(context, p => p
				.Add(x => x.PassValue, false)
				.Add(x => x.Listen, false));

			await ChangeAsync(cut, Picked);
			cut.Render();

			Assert.Equal(Text(Picked), Shown(cut));
		}

		public async Task BoundAcceptAsync(BunitContext context)
		{
			IRenderedComponent<ValueParent<TInput, TValue>> cut = Parent(context, p => p.Add(x => x.Bind, HasExpression));

			await ChangeAsync(cut, Picked);
			cut.Render();

			Assert.Equal(Picked, cut.Instance.Held);
			Assert.Equal(Text(Picked), Shown(cut));
		}

		public async Task BoundRefuseAsync(BunitContext context)
		{
			IRenderedComponent<ValueParent<TInput, TValue>> cut = Parent(context, p => p
				.Add(x => x.Bind, HasExpression)
				.Add(x => x.Keep, _ => Initial));

			await ChangeAsync(cut, Picked);

			Assert.Equal(Text(Initial), Shown(cut));
		}

		// Picks the value through the input's own UI, from what it shows now.
		protected abstract Task ChangeAsync(IRenderedComponent<IComponent> cut, TValue to);

		// What the input's markup shows.
		protected abstract string Shown(IRenderedComponent<IComponent> cut);

		protected abstract string Text(TValue value);

		private IRenderedComponent<ValueParent<TInput, TValue>> Parent(BunitContext context,
			Action<ComponentParameterCollectionBuilder<ValueParent<TInput, TValue>>>? configure = null) =>
			context.Render<ValueParent<TInput, TValue>>(p =>
			{
				p.Add(x => x.Value, Initial).Add(x => x.With, With).Add(x => x.ValueName, ValueName);
				configure?.Invoke(p);
			});
	}

	private abstract class ToggleFamily<TInput> : Family<TInput, bool>
		where TInput : IComponent
	{
		public override bool HasThreeValues => false;

		protected override bool Initial => false;

		protected override bool Picked => true;

		protected override bool Default => false;

		protected override Task ChangeAsync(IRenderedComponent<IComponent> cut, bool to) =>
			cut.Find("input[type=checkbox]").ChangeAsync(new ChangeEventArgs { Value = to });

		protected override string Shown(IRenderedComponent<IComponent> cut) =>
			Text(cut.Find("input[type=checkbox]").HasAttribute("checked"));

		protected override string Text(bool value) => value ? "on" : "off";
	}

	private sealed class SwitchFamily : ToggleFamily<MokaSwitch>;

	private sealed class CheckboxFamily : ToggleFamily<MokaCheckbox>;

	private abstract class BoxFamily<TInput, TValue> : Family<TInput, TValue>
		where TInput : IComponent
	{
		protected override Task ChangeAsync(IRenderedComponent<IComponent> cut, TValue to) =>
			cut.Find("input").InputAsync(new ChangeEventArgs { Value = Text(to) });

		protected override string Shown(IRenderedComponent<IComponent> cut) => cut.Find("input").GetAttribute("value") ?? "";
	}

	private sealed class TextFamily : BoxFamily<MokaTextField, string>
	{
		protected override string Initial => "Ada";

		protected override string Picked => "Grace";

		protected override string Second => "Linus";

		protected override string Clamped => "Gra";

		protected override string Default => "";

		protected override string Text(string value) => value;
	}

	private sealed class NumberFamily : BoxFamily<MokaNumericField<int>, int>
	{
		protected override int Initial => 5;

		protected override int Picked => 12;

		protected override int Second => 20;

		protected override int Clamped => 10;

		protected override int Default => 0;

		protected override string Text(int value) => value.ToString(CultureInfo.InvariantCulture);
	}

	private sealed class ColorFamily : BoxFamily<MokaColorInput, string>
	{
		protected override string Initial => "#ff0000";

		protected override string Picked => "#00ff00";

		protected override string Second => "#0000ff";

		protected override string Clamped => "#123456";

		protected override string Default => "";

		protected override string Text(string value) => value;
	}

	private sealed class TimeFamily : Family<MokaTimePicker, TimeSpan?>
	{
		protected override TimeSpan? Initial => new TimeSpan(9, 30, 0);

		protected override TimeSpan? Picked => new TimeSpan(10, 15, 0);

		protected override TimeSpan? Second => new TimeSpan(11, 45, 0);

		protected override TimeSpan? Clamped => new TimeSpan(10, 0, 0);

		protected override TimeSpan? Default => null;

		protected override IReadOnlyDictionary<string, object?> With => new Dictionary<string, object?> { ["Typed"] = true };

		protected override Task ChangeAsync(IRenderedComponent<IComponent> cut, TimeSpan? to) =>
			cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = Text(to) });

		protected override string Shown(IRenderedComponent<IComponent> cut) => cut.Find("input").GetAttribute("value") ?? "";

		protected override string Text(TimeSpan? value) => value is { } time ? $"{time.Hours:D2}:{time.Minutes:D2}" : "";
	}

	private sealed class DateFamily : Family<MokaDatePicker, DateTime?>
	{
		public override bool HasThreeValues => false;

		protected override DateTime? Initial => DateTime.Today.AddDays(-3);

		protected override DateTime? Picked => DateTime.Today;

		protected override DateTime? Default => null;

		protected override async Task ChangeAsync(IRenderedComponent<IComponent> cut, DateTime? to)
		{
			await cut.Find(".moka-datepicker-toggle").ClickAsync(new MouseEventArgs());
			await cut.Find(".moka-datepicker-today").ClickAsync(new MouseEventArgs());
		}

		protected override string Shown(IRenderedComponent<IComponent> cut) => cut.Find("input").GetAttribute("value") ?? "";

		protected override string Text(DateTime? value) =>
			value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
	}

	private sealed class SliderFamily : Family<MokaSlider, double>
	{
		protected override double Initial => 10;

		protected override double Picked => 40;

		protected override double Second => 70;

		protected override double Clamped => 50;

		protected override double Default => 0;

		protected override Task ChangeAsync(IRenderedComponent<IComponent> cut, double to) =>
			cut.Find("input[type=range]").InputAsync(new ChangeEventArgs { Value = Text(to) });

		protected override string Shown(IRenderedComponent<IComponent> cut) =>
			cut.Find("input[type=range]").GetAttribute("value") ?? "";

		protected override string Text(double value) => value.ToString(CultureInfo.InvariantCulture);
	}

	private abstract class ListFamily<TInput> : Family<TInput, string?>
		where TInput : IComponent
	{
		protected static readonly string[] Letters = ["a", "b", "c", "d"];

		protected override string? Initial => "a";

		protected override string? Picked => "b";

		protected override string? Second => "c";

		protected override string? Clamped => "d";

		protected override string? Default => null;
	}

	private sealed class SelectFamily : ListFamily<MokaSelect<string>>
	{
		protected override IReadOnlyDictionary<string, object?> With => new Dictionary<string, object?> { ["Items"] = Letters };

		protected override async Task ChangeAsync(IRenderedComponent<IComponent> cut, string? to)
		{
			await cut.Find("[role=combobox]").ClickAsync(new MouseEventArgs());
			await cut.FindAll("[role=option]")[Array.IndexOf(Letters, to)].ClickAsync(new MouseEventArgs());
		}

		protected override string Shown(IRenderedComponent<IComponent> cut) => cut.Find(".moka-select-value").TextContent.Trim();

		protected override string Text(string? value) => value ?? "Select...";
	}

	private sealed class NativeSelectFamily : ListFamily<MokaNativeSelect<string>>
	{
		protected override IReadOnlyDictionary<string, object?> With => new Dictionary<string, object?> { ["Items"] = Letters };

		// Bound to a field, the options carry the letters; otherwise their positions.
		protected override Task ChangeAsync(IRenderedComponent<IComponent> cut, string? to)
		{
			IElement select = cut.Find("select");
			string? posted = select.HasAttribute("name") ? to : Array.IndexOf(Letters, to).ToString(CultureInfo.InvariantCulture);
			return select.ChangeAsync(new ChangeEventArgs { Value = posted });
		}

		protected override string Shown(IRenderedComponent<IComponent> cut)
		{
			IElement select = cut.Find("select");
			string value = select.GetAttribute("value") ?? "";
			return select.HasAttribute("name") || value.Length == 0
				? value
				: Letters[int.Parse(value, CultureInfo.InvariantCulture)];
		}

		protected override string Text(string? value) => value ?? "";
	}

	private sealed class RadioFamily : ListFamily<MokaRadioGroup<string>>
	{
		protected override IReadOnlyDictionary<string, object?> With => new Dictionary<string, object?>
		{
			["ChildContent"] = (RenderFragment)(builder =>
			{
				foreach (string letter in Letters)
				{
					builder.OpenComponent<MokaRadioItem<string>>(0);
					builder.AddComponentParameter(1, "Value", letter);
					builder.AddComponentParameter(2, "Label", letter.ToUpperInvariant());
					builder.CloseComponent();
				}
			})
		};

		protected override Task ChangeAsync(IRenderedComponent<IComponent> cut, string? to) =>
			cut.FindAll("input[type=radio]")[Array.IndexOf(Letters, to)].ChangeAsync(new ChangeEventArgs { Value = "on" });

		protected override string Shown(IRenderedComponent<IComponent> cut)
		{
			IElement[] checkedRadios = [.. cut.FindAll("input[type=radio]").Where(radio => radio.HasAttribute("checked"))];
			return checkedRadios.Length == 0 ? "" : Assert.Single(checkedRadios).GetAttribute("value") ?? "";
		}

		protected override string Text(string? value) => value ?? "";
	}

	private sealed class PinFamily : Family<MokaPinInput, string?>
	{
		protected override string? Initial => "";

		protected override string? Picked => "1";

		protected override string? Second => "12";

		protected override string? Clamped => "9";

		protected override string? Default => "";

		protected override IReadOnlyDictionary<string, object?> With => new Dictionary<string, object?> { ["Masked"] = false };

		// Types the last digit into its box.
		protected override Task ChangeAsync(IRenderedComponent<IComponent> cut, string? to) =>
			cut.FindAll("input")[to!.Length - 1].InputAsync(new ChangeEventArgs { Value = to[^1].ToString() });

		protected override string Shown(IRenderedComponent<IComponent> cut) =>
			string.Concat(cut.FindAll("input").Select(box => box.GetAttribute("value")));

		protected override string Text(string? value) => value ?? "";
	}

	private sealed class TagFamily : Family<MokaTagInput, IList<string>>
	{
		protected override IList<string> Initial => [];

		protected override IList<string> Picked => ["a"];

		protected override IList<string> Second => ["a", "b"];

		protected override IList<string> Clamped => ["z"];

		protected override IList<string> Default => [];

		protected override string ValueName => "Values";

		protected override bool HasExpression => false;

		// Adds the last tag.
		protected override async Task ChangeAsync(IRenderedComponent<IComponent> cut, IList<string> to)
		{
			IElement input = cut.Find(".moka-taginput-container input");
			await input.InputAsync(new ChangeEventArgs { Value = to[^1] });
			await cut.Find(".moka-taginput-container input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
		}

		protected override string Shown(IRenderedComponent<IComponent> cut) =>
			string.Join(",", cut.FindAll(".moka-taginput-tag-text").Select(tag => tag.TextContent));

		protected override string Text(IList<string> value) => string.Join(",", value);
	}

	private sealed class RangeFamily : Family<MokaRangeBar, MokaRange>
	{
		protected override MokaRange Initial => new(20, 60);

		protected override MokaRange Picked => new(30, 60);

		protected override MokaRange Second => new(40, 60);

		protected override MokaRange Clamped => new(35, 60);

		protected override MokaRange Default => new(0, 100);

		// A drag let go, which the script reports.
		protected override Task ChangeAsync(IRenderedComponent<IComponent> cut, MokaRange to)
		{
			IRenderedComponent<MokaRangeBar> bar = cut.FindComponent<MokaRangeBar>();
			return bar.InvokeAsync(() => bar.Instance.OnDragged(to.Start, to.End));
		}

		protected override string Shown(IRenderedComponent<IComponent> cut) =>
			$"{cut.Find("[data-moka-handle=start]").GetAttribute("aria-valuenow")}-{cut.Find("[data-moka-handle=end]").GetAttribute("aria-valuenow")}";

		protected override string Text(MokaRange value) =>
			string.Create(CultureInfo.InvariantCulture, $"{value.Start}-{value.End}");
	}
}
