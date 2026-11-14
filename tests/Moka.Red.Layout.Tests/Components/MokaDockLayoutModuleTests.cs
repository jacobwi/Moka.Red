using Bunit;
using Moka.Red.Layout.DockLayout;

namespace Moka.Red.Layout.Tests.Components;

// The panels used to import moka-drag.js and bind a splitter each. The layout binds one module once now, and its
// listeners on the root serve every gutter, tab and window, so a render that adds groups binds nothing.
public class MokaDockLayoutModuleTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Layout/DockLayout/MokaDockLayout.razor.js";

	private readonly BunitJSModuleInterop _module;

	public MokaDockLayoutModuleTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
		_module.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void TheLayout_BindsItsModuleOnce_WithOptionsAsADictionary()
	{
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p
			.Add(x => x.DropIndicator, MokaDockDropIndicator.Compass)
			.Add(x => x.NarrowBelow, "30rem")
			.AddChildContent(DockKit.Ide()));

		cut.Render(p => p.AddChildContent(DockKit.Ide()));

		JSRuntimeInvocation bind = Assert.Single(_module.Invocations["bind"]);
		Dictionary<string, object?> options = Assert.IsType<Dictionary<string, object?>>(bind.Arguments[2]);
		Assert.Equal("compass", options["indicator"]);
		Assert.Equal("30rem", options["narrowBelow"]);
		Assert.Equal(true, options["drag"]);
		Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "makeResizable");
	}

	[Fact]
	public void ChangedOptions_AreSentToTheBoundModule()
	{
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.AddChildContent(DockKit.Ide()));

		cut.Render(p => p.Add(x => x.AllowDragging, false).AddChildContent(DockKit.Ide()));

		JSRuntimeInvocation update = Assert.Single(_module.Invocations["update"]);
		Assert.Equal(false, Assert.IsType<Dictionary<string, object?>>(update.Arguments[1])["drag"]);
	}

	[Fact]
	public async Task Disposing_UnbindsWhatItBound()
	{
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.AddChildContent(DockKit.Ide()));

		await cut.Instance.DisposeAsync();

		Assert.Single(_module.Invocations["unbind"]);
	}
}
