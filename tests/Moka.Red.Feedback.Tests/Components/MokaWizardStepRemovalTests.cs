using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Feedback.Wizard;

namespace Moka.Red.Feedback.Tests.Components;

// A step registered itself and never left, so a step the page stopped rendering stayed in the indicator and in the count,
// and the steps after it kept their old numbers. A step that came back went last, since it registered by appending; the
// wizard now rebuilds its step list on every render in the order the steps render, as QuickGrid does for its columns.
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

	[Fact]
	public async Task AStepThatComesBack_TakesItsPlaceAgain()
	{
		IRenderedComponent<Host> host = Render<Host>();

		await host.InvokeAsync(() => host.Instance.Set(showBilling: false));
		await host.InvokeAsync(() => host.Instance.Set(showBilling: true));

		Assert.Equal(["Account", "Billing", "Review"], Titles(host));
	}

	[Fact]
	public async Task AStepComingBackBeforeTheOneShowing_KeepsThatStepOnScreen_AndTellsTheParent()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p.Add(x => x.Active, 2));
		await host.InvokeAsync(() => host.Instance.Set(showBilling: false));
		host.WaitForAssertion(() => Assert.Equal(1, host.Instance.Active));

		await host.InvokeAsync(() => host.Instance.Set(showBilling: true));

		host.WaitForAssertion(() => Assert.Equal(2, host.Instance.Active));
		Assert.Equal("Review body", host.Find(".moka-wizard-step").TextContent.Trim());
		Assert.Contains("moka-wizard-indicator-step--active", host.FindAll(".moka-wizard-indicator-step")[2].ClassList);
	}

	[Fact]
	public async Task AStepInsideAnotherComponent_StaysInTheWizard_AndKeepsItsPlace()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p.Add(x => x.WrapBilling, true));
		string[] before = Titles(host);
		Assert.Equal(3, before.Length);
		Assert.Contains("Billing", before);

		await host.InvokeAsync(() => host.Instance.Set(showBilling: true));
		await host.FindAll(".moka-wizard-nav .moka-btn")[^1].ClickAsync();
		Assert.Contains("moka-wizard-indicator-step--active", host.FindAll(".moka-wizard-indicator-step")[1].ClassList);

		Assert.Equal(before, Titles(host));
	}

	private static string[] Titles(IRenderedComponent<Host> host) =>
		[.. host.FindAll(".moka-wizard-indicator-title").Select(title => title.TextContent.Trim())];

	private sealed class Host : ComponentBase
	{
		private bool _showBilling = true;

		[Parameter] public int Active { get; set; }

		[Parameter] public bool WrapBilling { get; set; }

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
			if (_showBilling && WrapBilling)
			{
				builder.OpenComponent<WrappedStep>(5);
				builder.AddComponentParameter(6, nameof(WrappedStep.Title), "Billing");
				builder.CloseComponent();
			}
			else if (_showBilling)
			{
				Step(builder, 10, "Billing");
			}

			Step(builder, 20, "Review");
		}

		internal static void Step(RenderTreeBuilder builder, int sequence, string title)
		{
			builder.OpenComponent<MokaWizardStep>(sequence);
			builder.AddComponentParameter(sequence + 1, nameof(MokaWizardStep.Title), title);
			builder.AddComponentParameter(sequence + 2, nameof(MokaWizardStep.ChildContent), (RenderFragment)(b => b.AddContent(0, $"{title} body")));
			builder.CloseComponent();
		}
	}

	// Only a string parameter, so it doesn't render again when the wizard does, and neither does the step inside it.
	private sealed class WrappedStep : ComponentBase
	{
		[Parameter] public string Title { get; set; } = "";

		protected override void BuildRenderTree(RenderTreeBuilder builder) => Host.Step(builder, 0, Title);
	}
}
