using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Core.Icons;
using Moka.Red.Feedback.Wizard;
using Moka.Red.Icons;

namespace Moka.Red.Feedback.Tests.Components;

// A step takes its parameters only after the wizard has drawn the indicator and the buttons, so a plain wizard drew
// what the step said one render before: a parent that made a step valid still saw Next disabled until something else
// rendered. Each test changes one step through a real parent render and checks the wizard in that same render.
public class MokaWizardStepStateTests : BunitContext
{
	public MokaWizardStepStateTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Theory]
	[InlineData(false, "Next")]
	[InlineData(true, "Continue")]
	public async Task AStepTurningValid_EnablesNext_InTheParentsRender(bool rail, string next)
	{
		IRenderedComponent<WizardHost> host = Render<WizardHost>(p => p.Add(x => x.Rail, rail).Add(x => x.FirstValid, false));
		Assert.True(Button(host, next).HasAttribute("disabled"));
		int parentRenders = host.Instance.Renders;

		await host.Instance.ChangeAsync(firstValid: true);

		Assert.Equal(parentRenders + 1, host.Instance.Renders);
		Assert.False(Button(host, next).HasAttribute("disabled"));
	}

	[Theory]
	[InlineData(false, "Next")]
	[InlineData(true, "Continue")]
	public async Task AStepTurningInvalid_DisablesNext_InTheParentsRender(bool rail, string next)
	{
		IRenderedComponent<WizardHost> host = Render<WizardHost>(p => p.Add(x => x.Rail, rail));
		Assert.False(Button(host, next).HasAttribute("disabled"));
		int parentRenders = host.Instance.Renders;

		await host.Instance.ChangeAsync(firstValid: false);

		Assert.Equal(parentRenders + 1, host.Instance.Renders);
		Assert.True(Button(host, next).HasAttribute("disabled"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task TheLastStepTurningValid_EnablesFinish_InTheParentsRender(bool rail)
	{
		IRenderedComponent<WizardHost> host = Render<WizardHost>(p => p
			.Add(x => x.Rail, rail)
			.Add(x => x.ActiveStep, 1)
			.Add(x => x.SecondValid, false));
		Assert.True(Button(host, "Finish").HasAttribute("disabled"));

		await host.Instance.ChangeAsync(secondValid: true);

		Assert.False(Button(host, "Finish").HasAttribute("disabled"));
	}

	[Fact]
	public async Task ANewTitle_ShowsInTheIndicator_InTheParentsRender()
	{
		IRenderedComponent<WizardHost> host = Render<WizardHost>();

		await host.Instance.ChangeAsync(firstTitle: "Your account");

		Assert.Equal("Your account", host.FindAll(".moka-wizard-indicator-title")[0].TextContent);
	}

	[Fact]
	public async Task ANewIcon_ShowsInTheIndicator_InTheParentsRender()
	{
		IRenderedComponent<WizardHost> host = Render<WizardHost>();
		Assert.Equal("1", Circle(host, 0).TextContent.Trim());

		await host.Instance.ChangeAsync(firstIcon: MokaIcons.Navigation.Home);

		Assert.NotNull(Circle(host, 0).QuerySelector("svg"));
	}

	// Blazor runs the second render in the same batch, and only when a step said something new.
	[Fact]
	public async Task TheWizard_RendersAgain_OnlyWhenAStepChanged()
	{
		IRenderedComponent<WizardHost> host = Render<WizardHost>();
		IRenderedComponent<MokaWizard> wizard = host.FindComponent<MokaWizard>();

		int before = wizard.RenderCount;
		await host.Instance.ChangeAsync();
		Assert.Equal(before + 1, wizard.RenderCount);

		before = wizard.RenderCount;
		await host.Instance.ChangeAsync(firstValid: false);
		Assert.Equal(before + 2, wizard.RenderCount);
	}

	private static IElement Button(IRenderedComponent<WizardHost> host, string text) =>
		host.FindAll("button").Single(button => button.TextContent.Trim() == text);

	private static IElement Circle(IRenderedComponent<WizardHost> host, int index) =>
		host.FindAll(".moka-wizard-indicator-circle")[index];

	// A parent as an app writes one: the step's parameters come from its fields, and one render passes them on.
	private sealed class WizardHost : ComponentBase
	{
		[Parameter] public bool Rail { get; set; }

		[Parameter] public int ActiveStep { get; set; }

		[Parameter] public bool FirstValid { get; set; } = true;

		[Parameter] public bool SecondValid { get; set; } = true;

		public int Renders { get; private set; }

		private bool _firstValid;
		private bool _secondValid;
		private string _firstTitle = "Account";
		private MokaIconDefinition? _firstIcon;

		public Task ChangeAsync(bool? firstValid = null, bool? secondValid = null, string? firstTitle = null,
			MokaIconDefinition? firstIcon = null) =>
			InvokeAsync(() =>
			{
				_firstValid = firstValid ?? _firstValid;
				_secondValid = secondValid ?? _secondValid;
				_firstTitle = firstTitle ?? _firstTitle;
				_firstIcon = firstIcon ?? _firstIcon;
				StateHasChanged();
			});

		protected override void OnInitialized()
		{
			_firstValid = FirstValid;
			_secondValid = SecondValid;
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			Renders++;
			builder.OpenComponent<MokaWizard>(0);
			builder.AddComponentParameter(1, nameof(MokaWizard.Rail), Rail);
			builder.AddComponentParameter(2, nameof(MokaWizard.ActiveStep), ActiveStep);
			builder.AddComponentParameter(3, nameof(MokaWizard.ChildContent), (RenderFragment)(steps =>
			{
				steps.OpenComponent<MokaWizardStep>(0);
				steps.AddComponentParameter(1, nameof(MokaWizardStep.Title), _firstTitle);
				steps.AddComponentParameter(2, nameof(MokaWizardStep.Icon), _firstIcon);
				steps.AddComponentParameter(3, nameof(MokaWizardStep.IsValid), _firstValid);
				steps.AddComponentParameter(4, nameof(MokaWizardStep.ChildContent), (RenderFragment)(inner => inner.AddContent(0, "One")));
				steps.CloseComponent();

				steps.OpenComponent<MokaWizardStep>(10);
				steps.AddComponentParameter(11, nameof(MokaWizardStep.Title), "Internet");
				steps.AddComponentParameter(12, nameof(MokaWizardStep.IsValid), _secondValid);
				steps.AddComponentParameter(13, nameof(MokaWizardStep.ChildContent), (RenderFragment)(inner => inner.AddContent(0, "Two")));
				steps.CloseComponent();
			}));
			builder.CloseComponent();
		}
	}
}
