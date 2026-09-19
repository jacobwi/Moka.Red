using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.Dialog;
using Moka.Red.Feedback.Extensions;

namespace Moka.Red.Feedback.Tests.Components;

/// <summary>
///     The host shows every open service dialog, stacked, so a dialog opened from another one's
///     action appears on top of it instead of queueing behind it.
/// </summary>
public class MokaDialogHostTests : BunitContext
{
	public MokaDialogHostTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddMokaFeedback();
	}

	private IMokaDialogService Dialogs => Services.GetRequiredService<IMokaDialogService>();

	[Fact]
	public void ShowsEveryOpenDialog_InTheOrderTheyOpened()
	{
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();

		_ = Dialogs.ConfirmAsync("First?", "One");
		_ = Dialogs.ConfirmAsync("Second?", "Two");

		cut.WaitForAssertion(() => Assert.Equal(["One", "Two"], Titles(cut)));
	}

	[Fact]
	public void DialogsOpenedBeforeTheHostMounts_StillShow()
	{
		_ = Dialogs.ConfirmAsync("Early?", "Early");

		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();

		Assert.Equal(["Early"], Titles(cut));
	}

	[Fact]
	public async Task ConfirmingTheTopDialog_ClosesOnlyThatOne()
	{
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		Task<bool> lower = Dialogs.ConfirmAsync("Lower?", "Lower");
		Task<bool> upper = Dialogs.ConfirmAsync("Upper?", "Upper");
		cut.WaitForAssertion(() => Assert.Equal(2, Titles(cut).Count));

		IElement top = cut.FindAll("[role=dialog]")[1];
		await top.QuerySelectorAll("button").Single(b => b.TextContent.Trim() == "OK").ClickAsync(new MouseEventArgs());

		Assert.True(await upper);
		Assert.False(lower.IsCompleted);
		cut.WaitForAssertion(() => Assert.Equal(["Lower"], Titles(cut)));
	}

	// The context used to close whichever dialog was current, so a component closing itself while
	// another dialog sat on top closed the wrong one.
	[Fact]
	public async Task AComponentDialog_ClosesItsOwnDialog_NotTheOneOnTop()
	{
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		Task<object?> picker = Dialogs.ShowComponentAsync<SelfClosingComponent>("Picker");
		Task<bool> confirm = Dialogs.ConfirmAsync("On top?", "Top");
		cut.WaitForAssertion(() => Assert.Equal(2, Titles(cut).Count));

		MokaDialogContext context = cut.FindComponent<SelfClosingComponent>().Instance.Dialog!;
		await cut.InvokeAsync(() => context.Close("picked"));

		Assert.Equal("picked", await picker);
		Assert.False(confirm.IsCompleted);
		cut.WaitForAssertion(() => Assert.Equal(["Top"], Titles(cut)));
	}

	[Fact]
	public async Task CloseAll_EmptiesTheHost()
	{
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		Task<bool> first = Dialogs.ConfirmAsync("First?", "One");
		Task<string?> second = Dialogs.PromptAsync("Name?", "Two");
		cut.WaitForAssertion(() => Assert.Equal(2, Titles(cut).Count));

		Dialogs.CloseAll();

		Assert.False(await first);
		Assert.Null(await second);
		cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role=dialog]")));
	}

	private static List<string> Titles(IRenderedComponent<MokaDialogHost> cut) =>
		cut.FindAll("[role=dialog] .moka-dialog-title").Select(title => title.TextContent).ToList();

	private sealed class SelfClosingComponent : ComponentBase
	{
		[CascadingParameter] public MokaDialogContext? Dialog { get; set; }
	}
}
