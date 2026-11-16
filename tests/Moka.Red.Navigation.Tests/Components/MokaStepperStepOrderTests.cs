using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Navigation.Stepper;

namespace Moka.Red.Navigation.Tests.Components;

// A step registered by appending, so one inside an @if that the page rendered again went last: it took the last number,
// the step after it showed the connector the last step doesn't have, and the active index pointed at another step.
public class MokaStepperStepOrderTests : BunitContext
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AStepThatComesBack_TakesItsPlaceAgain(bool withContent)
	{
		IRenderedComponent<Host> host = Render<Host>(p => p.Add(x => x.WithContent, withContent));

		await host.InvokeAsync(() => host.Instance.Set(showBilling: false));
		await host.InvokeAsync(() => host.Instance.Set(showBilling: true));

		Assert.Equal(["Account", "Billing", "Review"], Titles(host));
		Assert.Equal(["1", "2", "3"], Numbers(host));
		IReadOnlyList<IElement> steps = host.FindAll(".moka-step");
		Assert.NotNull(steps[1].QuerySelector(".moka-step__connector"));
		Assert.Null(steps[2].QuerySelector(".moka-step__connector"));
	}

	[Fact]
	public async Task RemovingAStepBeforeTheActiveOne_KeepsThatStepActive_AndTellsTheParent()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p
			.Add(x => x.WithContent, true)
			.Add(x => x.Active, 2));

		await host.InvokeAsync(() => host.Instance.Set(showBilling: false));

		host.WaitForAssertion(() => Assert.Equal(1, host.Instance.Active));
		Assert.Equal("Review", ActiveTitle(host));
		Assert.Equal("Review body", host.Find(".moka-stepper__content").TextContent.Trim());
	}

	[Fact]
	public async Task AStepComingBackBeforeTheActiveOne_KeepsThatStepActive_AndTellsTheParent()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p
			.Add(x => x.WithContent, true)
			.Add(x => x.Active, 2));
		await host.InvokeAsync(() => host.Instance.Set(showBilling: false));
		host.WaitForAssertion(() => Assert.Equal(1, host.Instance.Active));

		await host.InvokeAsync(() => host.Instance.Set(showBilling: true));

		host.WaitForAssertion(() => Assert.Equal(2, host.Instance.Active));
		Assert.Equal("Review", ActiveTitle(host));
		Assert.Equal("Review body", host.Find(".moka-stepper__content").TextContent.Trim());
	}

	private static string[] Titles(IRenderedComponent<Host> host) =>
		[.. host.FindAll(".moka-step__title").Select(title => title.TextContent.Trim())];

	private static string[] Numbers(IRenderedComponent<Host> host) =>
		[.. host.FindAll(".moka-step__number").Select(number => number.TextContent.Trim())];

	private static string ActiveTitle(IRenderedComponent<Host> host) =>
		host.Find(".moka-step--active .moka-step__title").TextContent.Trim();

	private sealed class Host : ComponentBase
	{
		private bool _showBilling = true;

		[Parameter] public bool WithContent { get; set; }

		[Parameter] public int Active { get; set; }

		public void Set(bool showBilling)
		{
			_showBilling = showBilling;
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
			Step(builder, 0, "Account");
			if (_showBilling)
			{
				Step(builder, 10, "Billing");
			}

			Step(builder, 20, "Review");
		}

		private void Step(RenderTreeBuilder builder, int sequence, string title)
		{
			builder.OpenComponent<MokaStep>(sequence);
			builder.AddComponentParameter(sequence + 1, nameof(MokaStep.Title), title);
			if (WithContent)
			{
				builder.AddComponentParameter(sequence + 2, nameof(MokaStep.ChildContent), (RenderFragment)(b => b.AddContent(0, $"{title} body")));
			}

			builder.CloseComponent();
		}
	}
}
