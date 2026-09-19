using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Moka.Red.Forms.Checkbox;
using Moka.Red.Forms.ColorPicker;
using Moka.Red.Forms.CurrencyInput;
using Moka.Red.Forms.DatePicker;
using Moka.Red.Forms.PhoneInput;
using Moka.Red.Forms.RadioGroup;
using Moka.Red.Forms.Switch;
using Moka.Red.Forms.TimePicker;

namespace Moka.Red.Forms.Tests.Components;

// TextField, Select and the other text inputs put the framework's field classes (modified, valid,
// invalid) on their own element, before the consumer's Class. These inputs left them out, so a
// form's CSS could not mark them.
public class ValidationClassTests : BunitContext
{
	public ValidationClassTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	public static TheoryData<string> Inputs =>
		["DatePicker", "TimePicker", "ColorPicker", "PhoneInput", "CurrencyInput", "Checkbox", "Switch", "RadioGroup"];

	[Theory]
	[MemberData(nameof(Inputs))]
	public void TheFieldClasses_GoOnTheComponentsOwnElement_BeforeClass(string name)
	{
		var order = new Order();
		(Type component, LambdaExpression field) = Input(name, order);
		IRenderedComponent<EditForm> cut = Render<EditForm>(p => p
			.Add(f => f.Model, order)
			.Add(f => f.ChildContent, _ => builder =>
			{
				builder.OpenComponent<DataAnnotationsValidator>(0);
				builder.CloseComponent();
				builder.OpenComponent(1, component);
				builder.AddAttribute(2, "Class", "probe-class");
				builder.AddAttribute(3, "ValueExpression", field);
				builder.CloseComponent();
			}));

		IElement own = cut.Find(".probe-class");
		Assert.Contains("valid", own.ClassList);
		Assert.EndsWith(" probe-class", own.GetAttribute("class"), StringComparison.Ordinal);

		cut.Find("form").Submit();

		own = cut.Find(".probe-class");
		Assert.Contains("invalid", own.ClassList);
		Assert.DoesNotContain("valid", own.ClassList);
		Assert.EndsWith(" probe-class", own.GetAttribute("class"), StringComparison.Ordinal);
	}

	private static (Type Component, LambdaExpression Field) Input(string name, Order order) => name switch
	{
		"DatePicker" => (typeof(MokaDatePicker), (Expression<Func<DateTime?>>)(() => order.Date)),
		"TimePicker" => (typeof(MokaTimePicker), (Expression<Func<TimeSpan?>>)(() => order.Time)),
		"ColorPicker" => (typeof(MokaColorPicker), (Expression<Func<string?>>)(() => order.Colour)),
		"PhoneInput" => (typeof(MokaPhoneInput), (Expression<Func<string?>>)(() => order.Phone)),
		"CurrencyInput" => (typeof(MokaCurrencyInput), (Expression<Func<decimal?>>)(() => order.Amount)),
		"Checkbox" => (typeof(MokaCheckbox), (Expression<Func<bool>>)(() => order.Accepted)),
		"Switch" => (typeof(MokaSwitch), (Expression<Func<bool>>)(() => order.Notify)),
		"RadioGroup" => (typeof(MokaRadioGroup<string>), (Expression<Func<string?>>)(() => order.Plan)),
		_ => throw new ArgumentOutOfRangeException(nameof(name))
	};

	private sealed class Order
	{
		[Required]
		public DateTime? Date { get; set; }

		[Required]
		public TimeSpan? Time { get; set; }

		[Required]
		public string? Colour { get; set; }

		[Required]
		public string? Phone { get; set; }

		[Required]
		public decimal? Amount { get; set; }

		[Range(typeof(bool), "true", "true")]
		public bool Accepted { get; set; }

		[Range(typeof(bool), "true", "true")]
		public bool Notify { get; set; }

		[Required]
		public string? Plan { get; set; }
	}
}
