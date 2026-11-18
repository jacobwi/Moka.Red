using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Navigation.Stepper;

namespace Moka.Red.Navigation.Tests.Components;

// Each step draws itself where the markup puts it, but takes its number from where the stepper lists it. A step rendered
// by a component of the page's reports after the stepper's own steps, so it took the last number; Order places it.
public class MokaStepperOrderTests : BunitContext
{
	[Fact]
	public void AWrappedStepWithoutOrder_TakesTheLastNumber()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p.Add(x => x.Orders, [0, 0, 0]));

		Assert.Equal(["Account", "Billing", "Review"], Titles(host));
		Assert.Equal(["1", "3", "2"], Numbers(host));
	}

	[Fact]
	public void AWrappedStep_TakesTheNumberItsOrderGives()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p.Add(x => x.Orders, [1, 2, 3]));

		Assert.Equal(["1", "2", "3"], Numbers(host));
		Assert.Null(host.FindAll(".moka-step")[2].QuerySelector(".moka-step__connector"));
	}

	[Fact]
	public async Task ChangingAStepsOrder_KeepsTheActiveStepActive_AndTellsTheParent()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p
			.Add(x => x.Orders, [1, 2, 3])
			.Add(x => x.Active, 2));
		Assert.Equal("Review", ActiveTitle(host));

		await host.InvokeAsync(() => host.Instance.SetOrders([2, 3, 1]));

		host.WaitForAssertion(() => Assert.Equal(0, host.Instance.Active));
		Assert.Equal("Review", ActiveTitle(host));
		Assert.Equal(["2", "3", "1"], Numbers(host));
	}

	private static string[] Titles(IRenderedComponent<Host> host) =>
		[.. host.FindAll(".moka-step__title").Select(title => title.TextContent.Trim())];

	private static string[] Numbers(IRenderedComponent<Host> host) =>
		[.. host.FindAll(".moka-step__number").Select(number => number.TextContent.Trim())];

	private static string ActiveTitle(IRenderedComponent<Host> host) =>
		host.Find(".moka-step--active .moka-step__title").TextContent.Trim();

	private sealed class Host : ComponentBase
	{
		[Parameter] public int[] Orders { get; set; } = [0, 0, 0];

		[Parameter] public int Active { get; set; }

		public void SetOrders(int[] orders)
		{
			Orders = orders;
			StateHasChanged();
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<MokaStepper>(0);
			builder.AddComponentParameter(1, nameof(MokaStepper.ActiveStep), Active);
			builder.AddComponentParameter(2, nameof(MokaStepper.ActiveStepChanged),
				EventCallback.Factory.Create<int>(this, value => Active = value));
			builder.AddComponentParameter(3, nameof(MokaStepper.ChildContent), (RenderFragment)Steps);
			builder.CloseComponent();
		}

		private void Steps(RenderTreeBuilder builder)
		{
			Step(builder, 0, "Account", Orders[0]);
			builder.OpenComponent<WrappedStep>(10);
			builder.AddComponentParameter(11, nameof(WrappedStep.Title), "Billing");
			builder.AddComponentParameter(12, nameof(WrappedStep.Order), Orders[1]);
			builder.CloseComponent();
			Step(builder, 20, "Review", Orders[2]);
		}

		internal static void Step(RenderTreeBuilder builder, int sequence, string title, int order)
		{
			builder.OpenComponent<MokaStep>(sequence);
			builder.AddComponentParameter(sequence + 1, nameof(MokaStep.Title), title);
			builder.AddComponentParameter(sequence + 2, nameof(MokaStep.Order), order);
			builder.AddComponentParameter(sequence + 3, nameof(MokaStep.ChildContent), (RenderFragment)(b => b.AddContent(0, $"{title} body")));
			builder.CloseComponent();
		}
	}

	private sealed class WrappedStep : ComponentBase
	{
		[Parameter] public string Title { get; set; } = "";

		[Parameter] public int Order { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder) => Host.Step(builder, 0, Title, Order);
	}
}
