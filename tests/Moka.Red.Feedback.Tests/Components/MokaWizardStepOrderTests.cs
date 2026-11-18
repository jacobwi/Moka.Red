using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Feedback.Wizard;

namespace Moka.Red.Feedback.Tests.Components;

// A step rendered by a component of the page's gets its parameters when that component renders, after the wizard's own
// steps, so the wizard can't tell where it stands in the markup. Order says where.
public class MokaWizardStepOrderTests : BunitContext
{
	public MokaWizardStepOrderTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void AWrappedStepWithoutOrder_JoinsAfterTheWizardsOwnSteps()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p.Add(x => x.Orders, [0, 0, 0]));

		Assert.Equal(["Account", "Review", "Billing"], Titles(host));
	}

	[Fact]
	public void AWrappedStep_StandsWhereItsOrderSays()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p.Add(x => x.Orders, [1, 2, 3]));

		Assert.Equal(["Account", "Billing", "Review"], Titles(host));
	}

	[Fact]
	public void StepsWithTheSameOrder_KeepTheMarkupsOrder()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p
			.Add(x => x.Orders, [1, 1, 0])
			.Add(x => x.WrapBilling, false));

		Assert.Equal(["Review", "Account", "Billing"], Titles(host));
	}

	[Fact]
	public async Task ChangingAStepsOrder_MovesIt_AndTheStepShowingStaysOnScreen()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p
			.Add(x => x.Orders, [1, 2, 3])
			.Add(x => x.Active, 1));
		Assert.Equal("Billing body", host.Find(".moka-wizard-step").TextContent.Trim());

		await host.InvokeAsync(() => host.Instance.SetOrders([1, 4, 3]));

		Assert.Equal(["Account", "Review", "Billing"], Titles(host));
		host.WaitForAssertion(() => Assert.Equal(2, host.Instance.Active));
		Assert.Equal("Billing body", host.Find(".moka-wizard-step").TextContent.Trim());
		Assert.Contains("moka-wizard-indicator-step--active", host.FindAll(".moka-wizard-indicator-step")[2].ClassList);
	}

	private static string[] Titles(IRenderedComponent<Host> host) =>
		[.. host.FindAll(".moka-wizard-indicator-title").Select(title => title.TextContent.Trim())];

	private sealed class Host : ComponentBase
	{
		[Parameter] public int[] Orders { get; set; } = [0, 0, 0];

		[Parameter] public bool WrapBilling { get; set; } = true;

		[Parameter] public int Active { get; set; }

		public void SetOrders(int[] orders)
		{
			Orders = orders;
			StateHasChanged();
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<MokaWizard>(0);
			builder.AddComponentParameter(1, nameof(MokaWizard.ActiveStep), Active);
			builder.AddComponentParameter(2, nameof(MokaWizard.ActiveStepChanged),
				EventCallback.Factory.Create<int>(this, step => Active = step));
			builder.AddComponentParameter(3, nameof(MokaWizard.ChildContent), (RenderFragment)Steps);
			builder.CloseComponent();
		}

		private void Steps(RenderTreeBuilder builder)
		{
			Step(builder, 0, "Account", Orders[0]);
			if (WrapBilling)
			{
				builder.OpenComponent<WrappedStep>(10);
				builder.AddComponentParameter(11, nameof(WrappedStep.Title), "Billing");
				builder.AddComponentParameter(12, nameof(WrappedStep.Order), Orders[1]);
				builder.CloseComponent();
			}
			else
			{
				Step(builder, 20, "Billing", Orders[1]);
			}

			Step(builder, 30, "Review", Orders[2]);
		}

		internal static void Step(RenderTreeBuilder builder, int sequence, string title, int order)
		{
			builder.OpenComponent<MokaWizardStep>(sequence);
			builder.AddComponentParameter(sequence + 1, nameof(MokaWizardStep.Title), title);
			builder.AddComponentParameter(sequence + 2, nameof(MokaWizardStep.Order), order);
			builder.AddComponentParameter(sequence + 3, nameof(MokaWizardStep.ChildContent), (RenderFragment)(b => b.AddContent(0, $"{title} body")));
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
