using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Layout.BentoGrid;
using Moka.Red.Layout.GlassCard;

namespace Moka.Red.Layout.Tests.Components;

// MokaBentoItem and MokaGlassCard share the clickable-card behaviour: Enter and Space are handled
// by Core's moka-keys.js for the card itself, so these tests cover the wiring.
public class MokaClickableCardTests : BunitContext
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	[Fact]
	public void ClickableBentoItem_IsATabStopActivatedInTheBrowser()
	{
		BunitJSModuleInterop keys = SetupKeysModule();

		IRenderedComponent<MokaBentoItem> cut = Render<MokaBentoItem>(p => p
			.Add(x => x.Clickable, true)
			.Add(x => x.OnClick, () => { }));

		AssertBoundTabStop(cut.Find(".moka-bento-item"), keys);
	}

	[Fact]
	public void ClickableGlassCard_IsATabStopActivatedInTheBrowser()
	{
		BunitJSModuleInterop keys = SetupKeysModule();

		IRenderedComponent<MokaGlassCard> cut = Render<MokaGlassCard>(p => p
			.Add(x => x.Clickable, true)
			.Add(x => x.OnClick, () => { }));

		AssertBoundTabStop(cut.Find(".moka-glass-card"), keys);
	}

	// A static card used to carry tabindex="-1", so a click inside it still focused the card.
	[Fact]
	public void StaticCards_TakeNoFocusAndLoadNoScript()
	{
		IElement bento = Render<MokaBentoItem>().Find(".moka-bento-item");
		IElement glass = Render<MokaGlassCard>().Find(".moka-glass-card");

		Assert.False(bento.HasAttribute("tabindex"));
		Assert.False(bento.HasAttribute("role"));
		Assert.False(glass.HasAttribute("tabindex"));
		Assert.False(glass.HasAttribute("role"));
		Assert.Empty(JSInterop.Invocations);
	}

	[Fact]
	public void Click_InvokesOnClick()
	{
		// The key listener activates a card with click(), so this is the path Enter and Space take.
		SetupKeysModule();
		int clicks = 0;

		IRenderedComponent<MokaBentoItem> bento = Render<MokaBentoItem>(p => p
			.Add(x => x.Clickable, true)
			.Add(x => x.OnClick, () => clicks++));
		IRenderedComponent<MokaGlassCard> glass = Render<MokaGlassCard>(p => p
			.Add(x => x.Clickable, true)
			.Add(x => x.OnClick, () => clicks++));

		bento.Find(".moka-bento-item").Click();
		glass.Find(".moka-glass-card").Click();

		Assert.Equal(2, clicks);
	}

	private BunitJSModuleInterop SetupKeysModule()
	{
		BunitJSModuleInterop keys = JSInterop.SetupModule(KeysModule);
		keys.SetupVoid("bindActivation", _ => true).SetVoidResult();
		return keys;
	}

	private static void AssertBoundTabStop(IElement card, BunitJSModuleInterop keys)
	{
		Assert.Equal("0", card.GetAttribute("tabindex"));
		Assert.Equal("button", card.GetAttribute("role"));

		JSRuntimeInvocation bind = keys.VerifyInvoke("bindActivation");
		ElementReference bound = Assert.IsType<ElementReference>(bind.Arguments[0]);
		Assert.Equal(card.GetAttribute("blazor:elementReference"), bound.Id);
		Assert.Single(bind.Arguments);
	}
}
