using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Feedback.Dialog;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

/// <summary>
///     The keys an app hears from a dialog, a dialog that can't close while its action runs, where focus starts, and the
///     body that lets its one child fill it.
/// </summary>
public class MokaDialogKeysAndBusyTests : BunitContext
{
	private const string DialogModule = "./_content/Moka.Red.Feedback/moka-dialog.js";

	public MokaDialogKeysAndBusyTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task OnKeyDown_HearsTheKeysPressedInTheDialog()
	{
		List<string> keys = [];
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Pictures")
			.Add(x => x.OnKeyDown, (KeyboardEventArgs e) => keys.Add(e.Key)));

		await cut.Find(".moka-dialog-wrapper").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });
		await cut.Find(".moka-dialog-wrapper").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowLeft" });

		Assert.Equal(["ArrowRight", "ArrowLeft"], keys);
		Assert.Single(cut.FindAll("[role=dialog]"));
	}

	[Fact]
	public async Task Escape_IsHeardAndStillClosesTheDialog()
	{
		List<string> keys = [];
		List<bool> changes = [];
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Pictures")
			.Add(x => x.OpenChanged, open => changes.Add(open))
			.Add(x => x.OnKeyDown, (KeyboardEventArgs e) => keys.Add(e.Key)));

		await cut.Find(".moka-dialog-wrapper").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Equal(["Escape"], keys);
		Assert.Equal([false], changes);
	}

	[Fact]
	public void WhileNotBusy_TheMarkupHasNoBusyState()
	{
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Delete"));

		Assert.False(cut.Find("[role=dialog]").HasAttribute("aria-busy"));
		Assert.False(cut.Find(".moka-dialog-close").HasAttribute("disabled"));
	}

	[Fact]
	public void Busy_MarksTheDialogAndDisablesItsCloseButton()
	{
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Delete")
			.Add(x => x.Busy, true));

		Assert.Equal("true", cut.Find("[role=dialog]").GetAttribute("aria-busy"));
		Assert.True(cut.Find(".moka-dialog-close").HasAttribute("disabled"));
	}

	[Fact]
	public async Task Busy_KeepsEscapeAndTheBackdropFromClosingIt()
	{
		List<bool> changes = [];
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Delete")
			.Add(x => x.Busy, true)
			.Add(x => x.OpenChanged, open => changes.Add(open)));

		await cut.Find(".moka-dialog-wrapper").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
		await cut.Find(".moka-dialog-backdrop").ClickAsync(new MouseEventArgs());

		Assert.Empty(changes);
		Assert.Single(cut.FindAll("[role=dialog]"));
	}

	[Fact]
	public async Task Busy_StillLetsTheKeysThrough()
	{
		List<string> keys = [];
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Busy, true)
			.Add(x => x.OnKeyDown, (KeyboardEventArgs e) => keys.Add(e.Key)));

		await cut.Find(".moka-dialog-wrapper").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Equal(["Escape"], keys);
		Assert.Single(cut.FindAll("[role=dialog]"));
	}

	[Fact]
	public async Task Busy_StillClosesWhenTheParentSaysSo()
	{
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Busy, true));

		cut.Render(p => p.Add(x => x.Open, false));
		await cut.Instance.ExitCompletion;

		Assert.Empty(cut.FindAll("[role=dialog]"));
	}

	[Fact]
	public async Task NoLongerBusy_EscapeClosesAgain()
	{
		List<bool> changes = [];
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Busy, true)
			.Add(x => x.OpenChanged, open => changes.Add(open)));

		cut.Render(p => p.Add(x => x.Busy, false));
		await cut.Find(".moka-dialog-wrapper").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Equal([false], changes);
	}

	// The close button's tooltip shows on keyboard focus, so a dialog that opened on it covered its own first line.
	[Fact]
	public void TheCloseButton_NeverTakesTheFirstFocus()
	{
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Delete"));

		Assert.True(cut.Find(".moka-dialog-close").HasAttribute("data-moka-focus-last"));
	}

	[Theory]
	[InlineData(MokaDialogInitialFocus.Content, "content")]
	[InlineData(MokaDialogInitialFocus.FirstControl, "first")]
	[InlineData(MokaDialogInitialFocus.Dialog, "self")]
	public void InitialFocus_TellsTheTrapWhereToStart(MokaDialogInitialFocus focus, string expected)
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(DialogModule);
		module.Setup<int>("trapFocus", _ => true).SetResult(1);

		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Delete")
			.Add(x => x.InitialFocus, focus));

		cut.WaitForAssertion(() => Assert.NotEmpty(module.Invocations["trapFocus"]));
		Dictionary<string, object?> options = Assert.IsType<Dictionary<string, object?>>(module.Invocations["trapFocus"][0].Arguments[1]);
		Assert.Equal(expected, options["initial"]);
	}

	// Body, then actions, then the header: the WAI-ARIA dialog pattern's first control, with the least destructive action
	// (first among the actions by the library's convention) ahead of the header's controls.
	[Fact]
	public void ByDefault_FocusLooksInTheBodyThenTheActionsThenTheHeader()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(DialogModule);
		module.Setup<int>("trapFocus", _ => true).SetResult(1);

		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Delete"));

		cut.WaitForAssertion(() => Assert.NotEmpty(module.Invocations["trapFocus"]));
		Dictionary<string, object?> options = Assert.IsType<Dictionary<string, object?>>(module.Invocations["trapFocus"][0].Arguments[1]);
		Assert.Equal(
			[":scope > .moka-dialog-body", ":scope > .moka-dialog-actions", ":scope > .moka-dialog-header"],
			Assert.IsType<string[]>(options["order"]));
	}

	// The rule was scoped, so it only matched the dialog's own elements, and the body holds none: the app's one child
	// never filled the body (MokaArk, in Edge).
	[Fact]
	public void ABodyThatDoesNotScroll_LetsTheAppsOneChildFillIt()
	{
		IReadOnlyDictionary<string, string> rule =
			ScopedCss.Declarations("MokaDialog.razor.css", ".moka-dialog-body--no-scroll ::deep > :only-child");

		Assert.Equal("1 1 auto", rule["flex"]);
		Assert.Equal("0", rule["min-height"]);
		Assert.Empty(ScopedCss.Declarations("MokaDialog.razor.css", ".moka-dialog-body--no-scroll > :only-child"));
	}

	[Fact]
	public void ABodyThatDoesNotScroll_HoldsTheAppsChildDirectly()
	{
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.ScrollBody, false)
			.AddChildContent("<div class=\"viewer\">picture</div>"));

		IElement body = cut.Find(".moka-dialog-body--no-scroll");
		IElement child = Assert.Single(body.Children);
		Assert.Equal("viewer", child.ClassName);
	}
}
