using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.Dialog;
using Moka.Red.Feedback.Extensions;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

/// <summary>
///     The dialog's placement modes, its backdrop effects, and the exit animation that closes it.
/// </summary>
public class MokaDialogPositionAndBackdropTests : BunitContext
{
	private const string Css = "MokaDialog.razor.css";
	private const string DragModule = "./_content/Moka.Red.Core/moka-drag.js";

	public MokaDialogPositionAndBackdropTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddMokaFeedback();
	}

	private IMokaDialogService Dialogs => Services.GetRequiredService<IMokaDialogService>();

	[Fact]
	public void CenteredByDefault_AddsNoPositionClass()
	{
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Settings"));

		Assert.Equal("moka-dialog-wrapper", cut.Find(".moka-dialog-wrapper").ClassName);
		Assert.DoesNotContain("moka-dialog--center", cut.Find("[role=dialog]").ClassName,
			StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(MokaDialogPosition.Left, "left")]
	[InlineData(MokaDialogPosition.Right, "right")]
	[InlineData(MokaDialogPosition.Top, "top")]
	[InlineData(MokaDialogPosition.Bottom, "bottom")]
	public void EdgePosition_AnchorsTheBoxAndTheWrapper(MokaDialogPosition position, string kebab)
	{
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Position, position)
			.Add(x => x.Title, "Panel"));

		Assert.Contains($"moka-dialog-wrapper--{kebab}", cut.Find(".moka-dialog-wrapper").ClassList);
		Assert.Contains($"moka-dialog--{kebab}", cut.Find("[role=dialog]").ClassList);
	}

	// A panel pinned to an edge has nowhere to be dragged to or resized from, so the dialog keeps
	// its centering-only affordances off it.
	[Fact]
	public void EdgePosition_IgnoresDraggableAndResizable()
	{
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Position, MokaDialogPosition.Right)
			.Add(x => x.Draggable, true)
			.Add(x => x.Resizable, true)
			.Add(x => x.Title, "Panel"));

		IElement box = cut.Find("[role=dialog]");
		Assert.DoesNotContain("moka-dialog--draggable", box.ClassList);
		Assert.DoesNotContain("moka-dialog--resizable", box.ClassList);
		Assert.DoesNotContain("moka-dialog-header--draggable", cut.Find(".moka-dialog-header").ClassList);
	}

	// A dialog dragged in the center that is then re-rendered onto an edge leaves its drag handle
	// behind rather than dragging the anchored panel around.
	[Fact]
	public void MovingOffCenterWhileDragging_DetachesTheDrag()
	{
		BunitJSModuleInterop dragModule = JSInterop.SetupModule(DragModule);
		dragModule.Mode = JSRuntimeMode.Loose;

		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Position, MokaDialogPosition.Center)
			.Add(x => x.Draggable, true)
			.Add(x => x.Title, "Move me"));
		Assert.Single(dragModule.Invocations["makeDraggable"]);

		cut.Render(p => p.Add(x => x.Position, MokaDialogPosition.Left));

		cut.WaitForAssertion(() => dragModule.VerifyInvoke("removeDraggable"));
	}

	[Fact]
	public void DefaultBackdrop_AddsNoModifier()
	{
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Settings"));

		Assert.Equal("moka-dialog-backdrop", cut.Find(".moka-dialog-backdrop").ClassName);
	}

	[Theory]
	[InlineData(MokaDialogBackdrop.Dim, "dim")]
	[InlineData(MokaDialogBackdrop.Frosted, "frosted")]
	[InlineData(MokaDialogBackdrop.Dark, "dark")]
	[InlineData(MokaDialogBackdrop.None, "none")]
	public void BackdropEffect_AddsItsModifier(MokaDialogBackdrop backdrop, string kebab)
	{
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Backdrop, backdrop)
			.Add(x => x.Title, "Settings"));

		Assert.Contains($"moka-dialog-backdrop--{kebab}", cut.Find(".moka-dialog-backdrop").ClassList);
	}

	// The close is decided the moment it starts, but the markup plays the exit first: the wrapper
	// takes the closing class, the box stays, and OnExited reports when it is really gone.
	[Fact]
	public async Task ClosingByEscape_PlaysTheExitThenRemovesTheMarkup()
	{
		int exits = 0;
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Settings")
			.Add(x => x.OnExited, () => exits++));

		await cut.Find(".moka-dialog-wrapper").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		IElement wrapper = cut.Find(".moka-dialog-wrapper");
		Assert.Contains("moka-dialog-wrapper--closing", wrapper.ClassList);
		Assert.Single(cut.FindAll("[role=dialog]"));
		Assert.Equal(0, exits);

		await cut.Instance.ExitCompletion;

		Assert.Empty(cut.FindAll(".moka-dialog-wrapper"));
		Assert.Equal(1, exits);
	}

	[Fact]
	public async Task ClosingFromTheParent_PlaysTheExitToo()
	{
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Settings"));

		cut.Render(p => p.Add(x => x.Open, false));

		Assert.Contains("moka-dialog-wrapper--closing", cut.Find(".moka-dialog-wrapper").ClassList);
		await cut.Instance.ExitCompletion;
		Assert.Empty(cut.FindAll(".moka-dialog-wrapper"));
	}

	// A reopen while the previous close is still animating out wins: the pending removal stands
	// down and the still-mounted box plays its entrance again.
	[Fact]
	public async Task ReopeningDuringTheExit_CancelsTheRemoval()
	{
		int exits = 0;
		bool open = true;
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, open)
			.Add(x => x.Title, "Settings")
			.Add(x => x.OpenChanged, v => open = v)
			.Add(x => x.OnExited, () => exits++));

		await cut.Find(".moka-dialog-wrapper").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
		Assert.False(open);

		cut.Render(p => p.Add(x => x.Open, open));
		cut.Render(p => p.Add(x => x.Open, true));

		await cut.Instance.ExitCompletion;

		Assert.Single(cut.FindAll("[role=dialog]"));
		Assert.DoesNotContain("moka-dialog-wrapper--closing", cut.Find(".moka-dialog-wrapper").ClassList);
		Assert.Equal(0, exits);
	}

	[Fact]
	public async Task DisposedWhileOpen_NeverReportsItselfExited()
	{
		int exits = 0;
		Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Settings")
			.Add(x => x.OnExited, () => exits++));

		await DisposeComponentsAsync();

		Assert.Equal(0, exits);
	}

	// The service carries the new options into the dialog it renders, so a confirm can be a panel.
	[Fact]
	public async Task ServiceDialog_TakesPositionAndBackdropFromOptions()
	{
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		_ = Dialogs.ConfirmAsync("Save changes?", "Save",
			o =>
			{
				o.Position = MokaDialogPosition.Right;
				o.Backdrop = MokaDialogBackdrop.Frosted;
			});

		cut.WaitForAssertion(() =>
		{
			Assert.Contains("moka-dialog--right", cut.Find("[role=dialog]").ClassList);
			Assert.Contains("moka-dialog-backdrop--frosted", cut.Find(".moka-dialog-backdrop").ClassList);
		});
	}

	// The enums are public API; renumbering any value is a breaking change.
	[Fact]
	public void Position_KeepsItsDeclaredValues()
	{
		Assert.Equal(0, (int)MokaDialogPosition.Center);
		Assert.Equal(1, (int)MokaDialogPosition.Left);
		Assert.Equal(2, (int)MokaDialogPosition.Right);
		Assert.Equal(3, (int)MokaDialogPosition.Top);
		Assert.Equal(4, (int)MokaDialogPosition.Bottom);
	}

	[Fact]
	public void Backdrop_KeepsItsDeclaredValues()
	{
		Assert.Equal(0, (int)MokaDialogBackdrop.Default);
		Assert.Equal(1, (int)MokaDialogBackdrop.Dim);
		Assert.Equal(2, (int)MokaDialogBackdrop.Frosted);
		Assert.Equal(3, (int)MokaDialogBackdrop.Dark);
		Assert.Equal(4, (int)MokaDialogBackdrop.None);
	}

	[Fact]
	public void SidePanels_FillTheirEdge()
	{
		IReadOnlyDictionary<string, string> left = ScopedCss.Declarations(Css, ".moka-dialog--left");
		Assert.Equal("100%", left["height"]);
		// The last of the fallback chain, the one a browser that knows dvh keeps
		Assert.Equal("100dvh", left["max-height"]);
	}

	[Fact]
	public void TopAndBottomPanels_TakeHalfTheViewportByDefault()
	{
		// The last of the vh/dvh fallback chain, the one a browser that knows dvh keeps
		Assert.Equal("50dvh",
			ScopedCss.Declarations(Css, ".moka-dialog--top:not(.moka-dialog--fullscreen)")["height"]);
	}

	[Fact]
	public void ClosingBackdrop_FadesOut()
	{
		string animation = ScopedCss.Declarations(Css, ".moka-dialog-wrapper--closing .moka-dialog-backdrop")["animation"];
		Assert.Contains("moka-dialog-backdrop-out", animation, StringComparison.Ordinal);
		Assert.Contains("forwards", animation, StringComparison.Ordinal);
	}

	[Fact]
	public void ClosingSidePanel_SlidesBackToItsEdge()
	{
		Assert.Contains("moka-dialog-slide-out-right",
			ScopedCss.Declarations(Css, ".moka-dialog-wrapper--closing .moka-dialog--right")["animation"],
			StringComparison.Ordinal);
	}

	[Fact]
	public void NoneBackdrop_StaysTransparent()
	{
		IReadOnlyDictionary<string, string> none = ScopedCss.Declarations(Css, ".moka-dialog-backdrop--none");
		Assert.Equal("transparent", none["background"]);
		Assert.Equal("none", none["backdrop-filter"]);
	}

	// Later in the file than the size presets, so on a narrow screen the preset width gives way to
	// the whole viewport, exactly as the browser would resolve it.
	[Fact]
	public void OnNarrowScreens_TheSizePresetsGoFullWidth()
	{
		Assert.Equal("100%", ScopedCss.Declarations(Css, ".moka-dialog--md")["width"]);
	}

	// Buttons keep their labels whole, so a row too narrow for them wraps instead of running out of the dialog.
	[Fact]
	public void TheActionsRow_Wraps()
	{
		Assert.Equal("wrap", ScopedCss.Declarations(Css, ".moka-dialog-actions")["flex-wrap"]);
	}
}
