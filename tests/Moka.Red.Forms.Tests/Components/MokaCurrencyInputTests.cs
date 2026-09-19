using System.Linq.Expressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.CurrencyInput;

namespace Moka.Red.Forms.Tests.Components;

// On blur the input clamped by writing its own Value parameter. ValueChanged had already gone out
// with the unclamped amount, so the parent kept that one, the EditContext never heard of the
// clamp, and the parent's next render put the out-of-range amount back.
public class MokaCurrencyInputTests : BunitContext
{
	[Fact]
	public async Task AnAmountAboveMax_IsReportedClamped()
	{
		List<decimal?> changes = [];
		IRenderedComponent<MokaCurrencyInput> cut = Render<MokaCurrencyInput>(p => p
			.Add(x => x.Value, 50m)
			.Add(x => x.Max, 100m)
			.Add(x => x.ValueChanged, v => changes.Add(v)));

		await TypeAsync(cut, "150");

		Assert.Equal([100m], changes);
		Assert.Equal("100.00", Input(cut).GetAttribute("value"));
	}

	[Fact]
	public async Task AnAmountBelowMin_IsReportedClamped()
	{
		List<decimal?> changes = [];
		IRenderedComponent<MokaCurrencyInput> cut = Render<MokaCurrencyInput>(p => p
			.Add(x => x.Value, 50m)
			.Add(x => x.Min, 10m)
			.Add(x => x.ValueChanged, v => changes.Add(v)));

		await TypeAsync(cut, "-5");

		Assert.Equal([10m], changes);
		Assert.Equal("10.00", Input(cut).GetAttribute("value"));
	}

	// The case users saw: the amount jumped back to the out-of-range one on the next render.
	[Fact]
	public async Task TheClampedAmount_SurvivesTheParentsNextRender()
	{
		decimal? amount = 50m;
		IRenderedComponent<MokaCurrencyInput> cut = Render<MokaCurrencyInput>(p => p
			.Add(x => x.Value, amount)
			.Add(x => x.Max, 100m)
			.Add(x => x.ValueChanged, v => amount = v));

		await TypeAsync(cut, "150");
		cut.Render(p => p.Add(x => x.Value, amount));

		Assert.Equal(100m, amount);
		Assert.Equal("100.00", Input(cut).GetAttribute("value"));
	}

	[Fact]
	public async Task AnAmountInRange_IsReportedOnce()
	{
		List<decimal?> changes = [];
		IRenderedComponent<MokaCurrencyInput> cut = Render<MokaCurrencyInput>(p => p
			.Add(x => x.Value, 50m)
			.Add(x => x.Min, 0m)
			.Add(x => x.Max, 100m)
			.Add(x => x.ValueChanged, v => changes.Add(v)));

		await TypeAsync(cut, "75.5");

		Assert.Equal([75.5m], changes);
		Assert.Equal("75.50", Input(cut).GetAttribute("value"));
	}

	[Fact]
	public async Task InAnEditForm_TheModelAndTheEditContextGetTheClampedAmount()
	{
		var invoice = new Invoice { Amount = 50m };
		var context = new EditContext(invoice);
		List<decimal?> amountsOnFieldChanged = [];
		context.OnFieldChanged += (_, _) => amountsOnFieldChanged.Add(invoice.Amount);

		IRenderedComponent<EditForm> form = Render<EditForm>(p => p
			.Add(f => f.EditContext, context)
			.Add(f => f.ChildContent, _ => builder =>
			{
				builder.OpenComponent<MokaCurrencyInput>(0);
				builder.AddAttribute(1, nameof(MokaCurrencyInput.Value), invoice.Amount);
				builder.AddAttribute(2, nameof(MokaCurrencyInput.ValueChanged),
					EventCallback.Factory.Create<decimal?>(this, v => invoice.Amount = v));
				builder.AddAttribute(3, nameof(MokaCurrencyInput.ValueExpression),
					(Expression<Func<decimal?>>)(() => invoice.Amount));
				builder.AddAttribute(4, nameof(MokaCurrencyInput.Max), (decimal?)100m);
				builder.CloseComponent();
			}));

		IElement input = form.Find("input");
		await input.FocusAsync(new FocusEventArgs());
		await form.Find("input").InputAsync(new ChangeEventArgs { Value = "150" });
		await form.Find("input").BlurAsync(new FocusEventArgs());

		Assert.Equal(100m, invoice.Amount);
		Assert.Equal([100m], amountsOnFieldChanged);
		Assert.True(context.IsModified(() => invoice.Amount));
	}

	private static async Task TypeAsync(IRenderedComponent<MokaCurrencyInput> cut, string text)
	{
		await Input(cut).FocusAsync(new FocusEventArgs());
		await Input(cut).InputAsync(new ChangeEventArgs { Value = text });
		await Input(cut).BlurAsync(new FocusEventArgs());
	}

	private static IElement Input(IRenderedComponent<MokaCurrencyInput> cut) => cut.Find("input");

	private sealed class Invoice
	{
		public decimal? Amount { get; set; }
	}
}
