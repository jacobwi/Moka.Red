using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Navigation.Stepper;

namespace Moka.Red.Navigation.Tests.Components;

// Each step's header is a button, so Tab reaches it and Enter or Space activate it. The click from
// the button bubbles to the step, which also takes mouse clicks on its connector.
public class MokaStepperTests : BunitContext
{
	[Fact]
	public void Steps_AreButtons_AndTheActiveOneIsTheCurrentStep()
	{
		IRenderedComponent<MokaStepper> cut = RenderStepper(p => p.Add(x => x.ActiveStep, 1));

		IReadOnlyList<IElement> buttons = cut.FindAll("button.moka-step__button");
		Assert.Equal(3, buttons.Count);
		Assert.All(buttons, button => Assert.Equal("button", button.GetAttribute("type")));
		Assert.False(buttons[0].HasAttribute("aria-current"));
		Assert.Equal("step", buttons[1].GetAttribute("aria-current"));
		Assert.False(buttons[2].HasAttribute("aria-current"));
	}

	[Fact]
	public async Task ActivatingAStepButton_MakesItTheCurrentStep()
	{
		int? changed = null;
		IRenderedComponent<MokaStepper> cut = RenderStepper(p => p.Add(x => x.ActiveStepChanged, i => changed = i));

		await cut.FindAll("button.moka-step__button")[2].ClickAsync(new MouseEventArgs());

		Assert.Equal(2, changed);
		Assert.Equal("step", cut.FindAll("button.moka-step__button")[2].GetAttribute("aria-current"));
	}

	// A step whose own parameters did not change was never told the active step moved, so it kept
	// showing as active.
	[Fact]
	public async Task EveryStep_FollowsTheActiveStep()
	{
		IRenderedComponent<MokaStepper> cut = RenderStepper();

		await cut.FindAll(".moka-step")[2].ClickAsync(new MouseEventArgs());

		IReadOnlyList<IElement> steps = cut.FindAll(".moka-step");
		Assert.False(steps[0].ClassList.Contains("moka-step--active"));
		Assert.True(steps[2].ClassList.Contains("moka-step--active"));
	}

	// Blazor passes every parameter again when the parent re-renders, so writing the choice into
	// the ActiveStep parameter let the parent's one-way value undo it. In a linear stepper that
	// then blocked the step after the one the user was on.
	[Fact]
	public async Task TheUsersChoice_SurvivesAParentRerender()
	{
		int? changed = null;
		IRenderedComponent<MokaStepper> cut = RenderStepper(p => p
			.Add(x => x.ActiveStep, 0)
			.Add(x => x.Linear, true)
			.Add(x => x.ActiveStepChanged, i => changed = i));

		await cut.FindAll(".moka-step")[1].ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.ActiveStep, 0));
		await cut.FindAll(".moka-step")[2].ClickAsync(new MouseEventArgs());

		Assert.Equal(2, changed);
		Assert.True(cut.FindAll(".moka-step")[2].ClassList.Contains("moka-step--active"));
	}

	[Fact]
	public void ANewActiveStep_FromTheParent_StillApplies()
	{
		IRenderedComponent<MokaStepper> cut = RenderStepper(p => p.Add(x => x.ActiveStep, 0));

		cut.Render(p => p.Add(x => x.ActiveStep, 1));

		Assert.True(cut.FindAll(".moka-step")[1].ClassList.Contains("moka-step--active"));
	}

	[Fact]
	public void StepsThatCannotBeActivated_AreDisabledButtons()
	{
		IRenderedComponent<MokaStepper> cut = Render<MokaStepper>(p => p
			.Add(x => x.Linear, true)
			.AddChildContent<MokaStep>(step => step.Add(x => x.Title, "Account"))
			.AddChildContent<MokaStep>(step => step.Add(x => x.Title, "Profile"))
			.AddChildContent<MokaStep>(step => step.Add(x => x.Title, "Review"))
			.AddChildContent<MokaStep>(step => step
				.Add(x => x.Title, "Archive")
				.Add(x => x.Disabled, true)));

		IReadOnlyList<IElement> buttons = cut.FindAll("button.moka-step__button");
		Assert.False(buttons[0].HasAttribute("disabled"));
		Assert.False(buttons[1].HasAttribute("disabled"));
		Assert.True(buttons[2].HasAttribute("disabled"));
		Assert.True(buttons[3].HasAttribute("disabled"));
	}

	// The check and error icons are decorative, so the state they show was invisible to screen readers.
	[Fact]
	public void CompletedAndFailedSteps_DescribeTheirState()
	{
		IRenderedComponent<MokaStepper> cut = Render<MokaStepper>(p => p
			.AddChildContent<MokaStep>(step => step
				.Add(x => x.Title, "Upload")
				.Add(x => x.Completed, true))
			.AddChildContent<MokaStep>(step => step
				.Add(x => x.Title, "Validate")
				.Add(x => x.HasError, true))
			.AddChildContent<MokaStep>(step => step.Add(x => x.Title, "Process")));

		IReadOnlyList<IElement> buttons = cut.FindAll("button.moka-step__button");
		Assert.Equal("Completed", Description(cut, buttons[0]));
		Assert.Equal("Error", Description(cut, buttons[1]));
		Assert.False(buttons[2].HasAttribute("aria-describedby"));
	}

	private static string? Description(IRenderedComponent<MokaStepper> cut, IElement button)
	{
		string? id = button.GetAttribute("aria-describedby");
		return id is null ? null : cut.Find($"[id='{id}']").TextContent;
	}

	// A step's ChildContent was documented as shown while it is active, and never rendered.
	[Fact]
	public async Task AHorizontalStepper_ShowsTheActiveStepsContentBelowTheSteps()
	{
		IRenderedComponent<MokaStepper> cut = RenderStepperWithContent(MokaStepperOrientation.Horizontal);

		Assert.Equal("Pick a plan", cut.Find(".moka-stepper__content").TextContent.Trim());

		await cut.FindAll("button.moka-step__button")[1].ClickAsync(new MouseEventArgs());

		Assert.Equal("Enter payment", cut.Find(".moka-stepper__content").TextContent.Trim());
		Assert.Empty(cut.FindAll(".moka-step__content"));
	}

	[Fact]
	public async Task AVerticalStepper_ShowsTheContentUnderTheActiveStep()
	{
		IRenderedComponent<MokaStepper> cut = RenderStepperWithContent(MokaStepperOrientation.Vertical);

		IElement content = Assert.Single(cut.FindAll(".moka-step__content"));
		Assert.Equal("Pick a plan", content.TextContent.Trim());
		Assert.Contains("moka-step--active", content.ParentElement!.ClassList);

		await cut.FindAll("button.moka-step__button")[1].ClickAsync(new MouseEventArgs());

		Assert.Equal("Enter payment", Assert.Single(cut.FindAll(".moka-step__content")).TextContent.Trim());
		Assert.Empty(cut.FindAll(".moka-stepper__content"));
	}

	// The content comes from the step's current parameters, not the ones it had a render earlier.
	[Fact]
	public void TheShownContent_FollowsTheParent()
	{
		IRenderedComponent<MokaStepper> cut = Render<MokaStepper>(p => p
			.AddChildContent<MokaStep>(step => step.Add(x => x.Title, "Plan").AddChildContent("First")));

		cut.Render(p => p.AddChildContent<MokaStep>(step => step.Add(x => x.Title, "Plan").AddChildContent("Second")));

		Assert.Equal("Second", cut.Find(".moka-stepper__content").TextContent.Trim());
	}

	private IRenderedComponent<MokaStepper> RenderStepperWithContent(MokaStepperOrientation orientation) =>
		Render<MokaStepper>(p => p
			.Add(x => x.Orientation, orientation)
			.AddChildContent<MokaStep>(step => step.Add(x => x.Title, "Plan").AddChildContent("Pick a plan"))
			.AddChildContent<MokaStep>(step => step.Add(x => x.Title, "Payment").AddChildContent("Enter payment")));

	private IRenderedComponent<MokaStepper> RenderStepper(
		Action<ComponentParameterCollectionBuilder<MokaStepper>>? configure = null) =>
		Render<MokaStepper>(p =>
		{
			configure?.Invoke(p);
			p.AddChildContent<MokaStep>(step => step.Add(x => x.Title, "Account"))
				.AddChildContent<MokaStep>(step => step.Add(x => x.Title, "Profile"))
				.AddChildContent<MokaStep>(step => step.Add(x => x.Title, "Review"));
		});
}
