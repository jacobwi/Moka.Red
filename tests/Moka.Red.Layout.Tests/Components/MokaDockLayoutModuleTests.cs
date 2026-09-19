using Bunit;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using Moka.Red.Core.Enums;
using Moka.Red.Layout.DockLayout;

namespace Moka.Red.Layout.Tests.Components;

// Every panel asks the layout for moka-drag.js from its first OnAfterRenderAsync, before the
// import has come back. The layout used to start a new import for each of them.
public class MokaDockLayoutModuleTests : BunitContext
{
	private const string DragModule = "./_content/Moka.Red.Core/moka-drag.js";

	[Fact]
	public async Task PanelsThatAskAtOnce_ShareOneImport()
	{
		// bUnit only hands out module references through its own import, so the test takes one from
		// a stand-in module and gives it to the drag import when that is allowed to finish.
		BunitJSModuleInterop standIn = JSInterop.SetupModule("stand-in.js");
		standIn.Mode = JSRuntimeMode.Loose;
		IJSObjectReference module = await JSInterop.JSRuntime.InvokeAsync<IJSObjectReference>("import", "stand-in.js");
		using PendingImport import = new(DragModule);
		JSInterop.AddInvocationHandler(import);

		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.AddChildContent(TwoPanels));
		int imports = import.Invocations.Count;
		import.Complete(module);

		Assert.Equal(1, imports);
		cut.WaitForAssertion(() => Assert.Equal(2, standIn.Invocations["makeResizable"].Count));
	}

	private static void TwoPanels(RenderTreeBuilder builder)
	{
		builder.OpenComponent<MokaDockPanel>(0);
		builder.AddComponentParameter(1, nameof(MokaDockPanel.Dock), MokaDockPosition.Left);
		builder.CloseComponent();

		builder.OpenComponent<MokaDockPanel>(2);
		builder.AddComponentParameter(3, nameof(MokaDockPanel.Dock), MokaDockPosition.Right);
		builder.CloseComponent();

		builder.OpenComponent<MokaDockContent>(4);
		builder.CloseComponent();
	}

	// An import of one path that stays pending until the test completes it.
	private sealed class PendingImport(string path) : JSRuntimeInvocationHandlerBase<IJSObjectReference>(
		invocation => invocation.Identifier == "import" && Equals(invocation.Arguments[0], path),
		isCatchAllHandler: false)
	{
		public void Complete(IJSObjectReference module) => SetResultBase(module);
	}
}
