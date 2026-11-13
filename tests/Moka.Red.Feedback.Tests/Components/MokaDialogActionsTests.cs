using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.Dialog;
using Moka.Red.Feedback.Extensions;

namespace Moka.Red.Feedback.Tests.Components;

// A component opened with ShowComponentAsync had nowhere to put its buttons but its own body, above the dialog's
// padding and without the footer's divider. MokaDialogActions puts them in the dialog's own footer.
public class MokaDialogActionsTests : BunitContext
{
	public MokaDialogActionsTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddMokaFeedback();
	}

	private IMokaDialogService Dialogs => Services.GetRequiredService<IMokaDialogService>();

	[Fact]
	public void TheButtons_GoInTheFooter_NotTheBody()
	{
		IRenderedComponent<MokaDialogHost> cut = Open<SaveDialog>("Save file?");

		Assert.Equal(["Cancel", "Save"], cut.FindAll(".moka-dialog-actions > button").Select(b => b.TextContent.Trim()));
		Assert.Empty(cut.FindAll(".moka-dialog-body button"));
		Assert.Equal("report.pdf", cut.Find(".moka-dialog-body p").TextContent);
	}

	// The buttons belong to the component, so they redraw with it.
	[Fact]
	public async Task TheButtons_FollowTheComponentsState()
	{
		IRenderedComponent<MokaDialogHost> cut = Open<SaveDialog>("Save file?");
		SaveDialog dialog = cut.FindComponent<SaveDialog>().Instance;

		Task click = Footer(cut, "Save").ClickAsync(new MouseEventArgs());

		cut.WaitForAssertion(() => Assert.Equal("true", Footer(cut, "Save").GetAttribute("aria-busy")));
		Assert.True(Footer(cut, "Cancel").HasAttribute("disabled"));

		dialog.Saved.SetResult();
		await click;
		cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role=dialog]")));
	}

	[Fact]
	public async Task TheButtons_CloseThroughTheContext()
	{
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		Task<object?> saved = Dialogs.ShowComponentAsync<SaveDialog>("Save file?");
		cut.WaitForAssertion(() => cut.Find("[role=dialog]"));
		Task<object?> cancelled = Dialogs.ShowComponentAsync<SaveDialog>("Save another?");
		cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[role=dialog]").Count));

		await cut.FindAll("[role=dialog]")[1].QuerySelectorAll(".moka-dialog-actions > button").Single(b => b.TextContent == "Cancel")
			.ClickAsync(new MouseEventArgs());
		Assert.Null(await cancelled);
		cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[role=dialog]")));

		Task click = Footer(cut, "Save").ClickAsync(new MouseEventArgs());
		cut.FindComponent<SaveDialog>().Instance.Saved.SetResult();
		await click;
		Assert.Equal("report.pdf", await saved);
	}

	// Each dialog has its own footer, so stacked dialogs never take each other's buttons.
	[Fact]
	public void StackedDialogs_KeepTheirOwnButtons()
	{
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		_ = Dialogs.ShowComponentAsync<SaveDialog>("First", p => p["Name"] = "one.pdf");
		_ = Dialogs.ShowComponentAsync<SaveDialog>("Second", p => p["Name"] = "two.pdf");
		cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[role=dialog]").Count));

		IReadOnlyList<IElement> dialogs = cut.FindAll("[role=dialog]");
		Assert.All(dialogs, dialog => Assert.Equal(2, dialog.QuerySelectorAll(".moka-dialog-actions > button").Length));
		Assert.Equal("Save one.pdf", dialogs[0].QuerySelector(".moka-dialog-actions > button:last-child")?.GetAttribute("aria-label"));
		Assert.Equal("Save two.pdf", dialogs[1].QuerySelector(".moka-dialog-actions > button:last-child")?.GetAttribute("aria-label"));
	}

	// Without it a component dialog's footer is as it always was.
	[Fact]
	public void AComponentWithoutIt_LeavesTheFooterEmpty()
	{
		IRenderedComponent<MokaDialogHost> cut = Open<PlainDialog>("Plain");

		Assert.Empty(cut.Find(".moka-dialog-actions").Children);
		Assert.Equal("Just text", cut.Find(".moka-dialog-body p").TextContent);
	}

	[Fact]
	public void TakenAway_ItEmptiesTheFooter()
	{
		IRenderedComponent<MokaDialogHost> cut = Open<SaveDialog>("Save file?");
		IRenderedComponent<SaveDialog> dialog = cut.FindComponent<SaveDialog>();

		dialog.Render(p => p.Add(x => x.ShowActions, false));

		cut.WaitForAssertion(() => Assert.Empty(cut.Find(".moka-dialog-actions").Children));
	}

	// Outside a service dialog there is no footer to reach, so the content stays where it is written.
	[Fact]
	public void OutsideADialog_ItDrawsItsContentInPlace()
	{
		IRenderedComponent<MokaDialogActions> cut = Render<MokaDialogActions>(p => p.AddChildContent("<button>Done</button>"));

		Assert.Equal("Done", cut.Find("button").TextContent);
	}

	private IRenderedComponent<MokaDialogHost> Open<TComponent>(string title) where TComponent : IComponent
	{
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		_ = Dialogs.ShowComponentAsync<TComponent>(title);
		cut.WaitForAssertion(() => cut.Find("[role=dialog]"));
		return cut;
	}

	private static IElement Footer(IRenderedComponent<MokaDialogHost> cut, string text) =>
		cut.FindAll(".moka-dialog-actions > button").Last(b => b.TextContent.Trim() == text);

	private sealed class SaveDialog : ComponentBase
	{
		private bool _saving;

		[CascadingParameter]
		public MokaDialogContext? Dialog { get; set; }

		[Parameter]
		public string Name { get; set; } = "report.pdf";

		[Parameter]
		public bool ShowActions { get; set; } = true;

		public TaskCompletionSource Saved { get; } = new();

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenElement(0, "p");
			builder.AddContent(1, Name);
			builder.CloseElement();
			if (!ShowActions)
			{
				return;
			}

			builder.OpenComponent<MokaDialogActions>(2);
			builder.AddComponentParameter(3, nameof(MokaDialogActions.ChildContent), (RenderFragment)Buttons);
			builder.CloseComponent();
		}

		private void Buttons(RenderTreeBuilder builder)
		{
			builder.OpenElement(0, "button");
			builder.AddAttribute(1, "disabled", _saving);
			builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => Dialog!.Cancel()));
			builder.AddContent(3, "Cancel");
			builder.CloseElement();
			builder.OpenElement(4, "button");
			builder.AddAttribute(5, "aria-label", $"Save {Name}");
			builder.AddAttribute(6, "aria-busy", _saving ? "true" : null);
			builder.AddAttribute(7, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, SaveAsync));
			builder.AddContent(8, "Save");
			builder.CloseElement();
		}

		private async Task SaveAsync()
		{
			_saving = true;
			StateHasChanged();
			await Saved.Task;
			Dialog!.Close(Name);
		}
	}

	private sealed class PlainDialog : ComponentBase
	{
		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenElement(0, "p");
			builder.AddContent(1, "Just text");
			builder.CloseElement();
		}
	}
}
