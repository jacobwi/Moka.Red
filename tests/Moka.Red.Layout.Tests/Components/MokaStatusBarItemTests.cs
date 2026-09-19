using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Layout.StatusBar;

namespace Moka.Red.Layout.Tests.Components;

// Enter and Space are handled by Core's moka-keys.js for the item itself, so these tests cover the
// wiring: which items are buttons, and when the key listener is bound.
public class MokaStatusBarItemTests : BunitContext
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	[Fact]
	public async Task OnlyAnItemWithOnClick_IsAButtonBoundToTheKeys()
	{
		BunitJSModuleInterop keys = JSInterop.SetupModule(KeysModule);
		keys.SetupVoid("bindActivation", _ => true).SetVoidResult();
		int clicks = 0;

		IRenderedComponent<MokaStatusBarItem> clickable = Render<MokaStatusBarItem>(p => p
			.Add(x => x.Text, "UTF-8")
			.Add(x => x.Tooltip, "Change encoding")
			.Add(x => x.OnClick, _ => clicks++));
		IRenderedComponent<MokaStatusBarItem> plain = Render<MokaStatusBarItem>(p => p.Add(x => x.Text, "Ln 42"));

		IElement item = clickable.Find(".moka-statusbar-item");
		Assert.Equal("button", item.GetAttribute("role"));
		Assert.Equal("0", item.GetAttribute("tabindex"));
		Assert.Equal("Change encoding", item.GetAttribute("title"));

		JSRuntimeInvocation bind = keys.VerifyInvoke("bindActivation");
		Assert.Equal(item.GetAttribute("blazor:elementReference"), Assert.IsType<ElementReference>(bind.Arguments[0]).Id);
		Assert.Single(bind.Arguments);

		IElement text = plain.Find(".moka-statusbar-item");
		Assert.False(text.HasAttribute("role"));
		Assert.False(text.HasAttribute("tabindex"));

		// The key listener activates the item with click(), so this is the path Enter and Space take.
		await clickable.Find(".moka-statusbar-item").ClickAsync(new MouseEventArgs());
		Assert.Equal(1, clicks);
	}
}
