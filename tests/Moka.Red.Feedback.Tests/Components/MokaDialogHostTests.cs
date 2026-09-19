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
	private const string DialogModule = "./_content/Moka.Red.Feedback/moka-dialog.js";

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

	// The host passes Open="true" to every dialog it shows. Escape closes the dialog, and the service
	// drops the request only after the focus release has come back from the browser. A host render in
	// between (another dialog opening) used to open the closed dialog again.
	[Fact]
	public async Task DialogClosedByEscape_StaysClosedWhenTheHostRendersBeforeTheServiceDropsIt()
	{
		BunitJSModuleInterop dialogModule = JSInterop.SetupModule(DialogModule);
		dialogModule.Mode = JSRuntimeMode.Loose;
		dialogModule.Setup<int>("trapFocus", _ => true).SetResult(1);
		JSRuntimeInvocationHandler release = dialogModule.SetupVoid("releaseFocus", _ => true);

		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		Task<bool> first = Dialogs.ConfirmAsync("First?", "First");
		cut.WaitForAssertion(() => Assert.Equal(["First"], Titles(cut)));

		Task escape = cut.Find(".moka-dialog-wrapper").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
		_ = Dialogs.ConfirmAsync("Second?", "Second");

		cut.WaitForAssertion(() => Assert.Equal(["Second"], Titles(cut)));

		release.SetVoidResult();
		await escape;
		Assert.False(await first);
	}

	// Each dialog has its backdrop inside its own wrapper, and the top wrapper covers every dialog
	// below it, so a backdrop click reaches the top dialog only. Before, the backdrops sat outside
	// the wrappers and no backdrop click reached any dialog.
	[Fact]
	public async Task BackdropClick_ClosesOnlyTheTopDialog()
	{
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		Task<bool> lower = Dialogs.ConfirmAsync("Lower?", "Lower");
		Task<bool> upper = Dialogs.ConfirmAsync("Upper?", "Upper");
		cut.WaitForAssertion(() => Assert.Equal(2, Titles(cut).Count));

		IElement? topBackdrop = cut.FindAll(".moka-dialog-wrapper")[1].QuerySelector(".moka-dialog-backdrop");
		Assert.NotNull(topBackdrop);
		await topBackdrop.ClickAsync(new MouseEventArgs());

		Assert.False(await upper);
		Assert.False(lower.IsCompleted);
		cut.WaitForAssertion(() => Assert.Equal(["Lower"], Titles(cut)));
	}

	[Fact]
	public async Task BackdropClick_LeavesADialogOpenWhenItsOptionsSaySo()
	{
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		Task<bool> confirm = Dialogs.ConfirmAsync("Sure?", "Sure", o => o.CloseOnBackdropClick = false);
		cut.WaitForAssertion(() => Assert.Single(Titles(cut)));

		await cut.Find(".moka-dialog-backdrop").ClickAsync(new MouseEventArgs());

		Assert.False(confirm.IsCompleted);
		Assert.Equal(["Sure"], Titles(cut));
	}

	private static List<string> Titles(IRenderedComponent<MokaDialogHost> cut) =>
		cut.FindAll("[role=dialog] .moka-dialog-title").Select(title => title.TextContent).ToList();

	private sealed class SelfClosingComponent : ComponentBase
	{
		[CascadingParameter] public MokaDialogContext? Dialog { get; set; }
	}
}
