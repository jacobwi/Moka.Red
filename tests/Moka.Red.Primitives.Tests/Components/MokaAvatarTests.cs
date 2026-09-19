using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Primitives.Avatar;

namespace Moka.Red.Primitives.Tests.Components;

// Enter and Space are handled by Core's moka-keys.js in the browser, so these tests cover the
// wiring: which avatars are buttons, what they are called, and when the key listener is bound.
public class MokaAvatarTests : BunitContext
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	[Fact]
	public void OnlyAnAvatarWithOnClick_IsAButtonBoundToTheKeys()
	{
		BunitJSModuleInterop keys = SetupKeysModule();

		IRenderedComponent<MokaAvatar> clickable = Render<MokaAvatar>(p => p
			.Add(x => x.Initials, "JD")
			.Add(x => x.OnClick, _ => { }));
		IRenderedComponent<MokaAvatar> staticAvatar = Render<MokaAvatar>(p => p.Add(x => x.Initials, "AB"));

		IElement button = clickable.Find(".moka-avatar");
		Assert.Equal("button", button.GetAttribute("role"));
		Assert.Equal("0", button.GetAttribute("tabindex"));

		JSRuntimeInvocation bind = keys.VerifyInvoke("bindActivation");
		ElementReference bound = Assert.IsType<ElementReference>(bind.Arguments[0]);
		Assert.Equal(button.GetAttribute("blazor:elementReference"), bound.Id);
		Assert.Single(bind.Arguments);

		IElement plain = staticAvatar.Find(".moka-avatar");
		Assert.False(plain.HasAttribute("role"));
		Assert.False(plain.HasAttribute("tabindex"));
	}

	[Fact]
	public async Task ClickableAvatar_IsNamedByAlt_AndClicks()
	{
		SetupKeysModule();
		int clicks = 0;

		IRenderedComponent<MokaAvatar> cut = Render<MokaAvatar>(p => p
			.Add(x => x.Initials, "JD")
			.Add(x => x.Alt, "Jane Doe")
			.Add(x => x.OnClick, _ => clicks++));

		// The key listener activates the avatar with click(), so this is the path Enter and Space take.
		await cut.Find("[role=button]").ClickAsync(new MouseEventArgs());

		Assert.Equal("Jane Doe", cut.Find("[role=button]").GetAttribute("aria-label"));
		Assert.Equal(1, clicks);
	}

	// A consumer's own aria-label goes on last and wins.
	[Fact]
	public void TheConsumersAriaLabel_WinsOverAlt()
	{
		SetupKeysModule();

		IRenderedComponent<MokaAvatar> cut = Render<MokaAvatar>(p => p
			.Add(x => x.Src, "/me.png")
			.Add(x => x.Alt, "Jane Doe")
			.Add(x => x.OnClick, _ => { })
			.AddUnmatched("aria-label", "Open account menu"));

		IElement button = cut.Find("[role=button]");
		Assert.Equal("Open account menu", button.GetAttribute("aria-label"));
		Assert.Equal("Jane Doe", button.QuerySelector("img")?.GetAttribute("alt"));
	}

	private BunitJSModuleInterop SetupKeysModule()
	{
		BunitJSModuleInterop keys = JSInterop.SetupModule(KeysModule);
		keys.SetupVoid("bindActivation", _ => true).SetVoidResult();
		return keys;
	}
}
