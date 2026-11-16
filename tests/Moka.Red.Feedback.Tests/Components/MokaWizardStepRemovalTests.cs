using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Feedback.Wizard;

namespace Moka.Red.Feedback.Tests.Components;

// A step registered itself and never left, so a step the page stopped rendering stayed in the indicator and in the count,
// and the steps after it kept their old numbers.
public class MokaWizardStepRemovalTests : BunitContext
{
	public MokaWizardStepRemovalTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task ARemovedStep_LeavesTheIndicator()
	{
		IRenderedComponent<Host> host = Render<Host>();
		Assert.Equal(["Account", "Billing", "Review"], Titles(host));

		await host.InvokeAsync(() => host.Instance.Set(showBilling: false));

		Assert.Equal(["Account", "Review"], Titles(host));
	}

	[Fact]
	public async Task RemovingAStepBeforeTheOneShowing_KeepsThatStepOnScreen_AndTellsTheParent()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p.Add(x => x.Active, 2));
		Assert.Equal("Review body", host.Find(".moka-wizard-step").TextContent.Trim());

		await host.InvokeAsync(() => host.Instance.Set(showBilling: false));

		host.WaitForAssertion(() => Assert.Equal(1, host.Instance.Active));
		Assert.Equal("Review body", host.Find(".moka-wizard-step").TextContent.Trim());
		Assert.Contains("moka-wizard-indicator-step--active", host.FindAll(".moka-wizard-indicator-step")[1].ClassList);
	}

	[Fact]
	public async Task RemovingAStepAfterTheOneShowing_LeavesThePositionAlone()
	{
		IRenderedComponent<Host> host = Render<Host>();
		int changes = host.Instance.Changes;

		await host.InvokeAsync(() => host.Instance.Set(showBilling: false));

		Assert.Equal(0, host.Instance.Active);
		Assert.Equal(changes, host.Instance.Changes);
		Assert.Equal("Account body", host.Find(".moka-wizard-step").TextContent.Trim());
	}

	private static string[] Titles(IRenderedComponent<Host> host) =>
		[.. host.FindAll(".moka-wizard-indicator-title").Select(title => title.TextContent.Trim())];

	private sealed class Host : ComponentBase
	{
		private bool _showBilling = true;

		[Parameter] public int Active { get; set; }

		public int Changes { get; private set; }

		public void Set(bool showBilling)
		{
			_showBilling = showBilling;
			StateHasChanged();
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<MokaWizard>(0);
			builder.AddComponentParameter(1, nameof(MokaWizard.ActiveStep), Active);
			builder.AddComponentParameter(2, nameof(MokaWizard.ActiveStepChanged), EventCallback.Factory.Create<int>(this, step =>
			{
				Active = step;
				Changes++;
			}));
			builder.AddComponentParameter(3, nameof(MokaWizard.ChildContent), (RenderFragment)Steps);
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

		private static void Step(RenderTreeBuilder builder, int sequence, string title)
		{
			builder.OpenComponent<MokaWizardStep>(sequence);
			builder.AddComponentParameter(sequence + 1, nameof(MokaWizardStep.Title), title);
			builder.AddComponentParameter(sequence + 2, nameof(MokaWizardStep.ChildContent), (RenderFragment)(b => b.AddContent(0, $"{title} body")));
			builder.CloseComponent();
		}
	}
}
