using System.ComponentModel.DataAnnotations;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Forms.Common;

namespace Moka.Red.Forms.Tests.Components;

// A form had nowhere to show the messages that belong to the whole form rather than one field, and nothing that
// gathered the field messages in one place at the top.
public class MokaValidationSummaryTests : BunitContext
{
	[Fact]
	public void AValidFormShowsNothing()
	{
		IRenderedComponent<Host> cut = Render<Host>(p => p.Add(x => x.Name, "Ada"));

		Assert.Empty(cut.FindAll(".moka-validation-summary"));
	}

	[Fact]
	public async Task AnInvalidFormListsItsMessages()
	{
		IRenderedComponent<Host> cut = Render<Host>(p => p.Add(x => x.Name, ""));

		await cut.InvokeAsync(() => cut.Instance.Context!.Validate());

		Assert.Equal(
			["A name is required"],
			cut.FindAll(".moka-validation-summary__item").Select(i => i.TextContent.Trim()));
	}

	// It is a live region, so it is read out when it fills rather than sitting empty and announcing itself.
	[Fact]
	public async Task TheSummaryIsALiveRegionTitledAboveItsList()
	{
		IRenderedComponent<Host> cut = Render<Host>(p => p.Add(x => x.Name, ""));
		await cut.InvokeAsync(() => cut.Instance.Context!.Validate());

		IElement summary = cut.Find(".moka-validation-summary");
		Assert.Equal("alert", summary.GetAttribute("role"));

		IElement title = cut.Find(".moka-validation-summary__title");
		Assert.Equal("Please fix the following", title.TextContent.Trim());
		Assert.Equal(title.Id, cut.Find(".moka-validation-summary__list").GetAttribute("aria-labelledby"));
	}

	[Fact]
	public async Task AnEmptyTitleLeavesOnlyTheList()
	{
		IRenderedComponent<Host> cut = Render<Host>(p => p.Add(x => x.Name, "").Add(x => x.Title, ""));
		await cut.InvokeAsync(() => cut.Instance.Context!.Validate());

		Assert.Empty(cut.FindAll(".moka-validation-summary__title"));
		Assert.False(cut.Find(".moka-validation-summary__list").HasAttribute("aria-labelledby"));
	}

	// Fixing the form empties the summary again, which is what the subscription is for.
	[Fact]
	public async Task FixingTheFormClearsTheSummary()
	{
		IRenderedComponent<Host> cut = Render<Host>(p => p.Add(x => x.Name, ""));
		await cut.InvokeAsync(() => cut.Instance.Context!.Validate());
		Assert.NotEmpty(cut.FindAll(".moka-validation-summary__item"));

		cut.Render(p => p.Add(x => x.Name, "Ada"));
		await cut.InvokeAsync(() => cut.Instance.Context!.Validate());

		Assert.Empty(cut.FindAll(".moka-validation-summary"));
	}

    // Outside an EditForm there is no context to read, and the summary has to stay quiet rather than throw.
	[Fact]
	public void WithoutAFormItRendersNothing()
	{
		IRenderedComponent<MokaValidationSummary> cut = Render<MokaValidationSummary>();

		Assert.Empty(cut.FindAll(".moka-validation-summary"));
	}

	private sealed class Model
	{
		[Required(ErrorMessage = "A name is required")]
		public string? Name { get; set; }
	}

	private sealed class Host : ComponentBase
	{
		private readonly Model _model = new();

		[Parameter] public string? Name { get; set; }

		[Parameter] public string? Title { get; set; } = "Please fix the following";

		public EditContext? Context { get; private set; }

		protected override void OnParametersSet()
		{
			_model.Name = Name;
			Context ??= new EditContext(_model);
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<EditForm>(0);
			builder.AddComponentParameter(1, nameof(EditForm.EditContext), Context);
			builder.AddComponentParameter(2, nameof(EditForm.ChildContent),
				(RenderFragment<EditContext>)(_ => Body));
			builder.CloseComponent();
		}

		private void Body(RenderTreeBuilder builder)
		{
			builder.OpenComponent<DataAnnotationsValidator>(0);
			builder.CloseComponent();

			builder.OpenComponent<MokaValidationSummary>(2);
			builder.AddComponentParameter(3, nameof(MokaValidationSummary.Title), Title);
			builder.CloseComponent();
		}
	}
}
