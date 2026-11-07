using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Feedback.Reconnect;

namespace Moka.Red.Feedback.Tests.Components;

// Blazor's reconnect code only knows ids: it finds the dialog and the seconds element by them, and a typo means
// Blazor falls back on its own dialog with no error anywhere. The script in the component finds the buttons the same way.
public class MokaReconnectModalTests : BunitContext
{
	// It goes in App.razor, outside any render mode, so most of these render it as that does.
	public MokaReconnectModalTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		SetRendererInfo(new RendererInfo("Static", false));
	}

	[Fact]
	public void Dialog_HasTheIdBlazorLooksFor()
	{
		var cut = Render<MokaReconnectModal>();

		IElement dialog = cut.Find("dialog");
		Assert.Equal("components-reconnect-modal", dialog.Id);
		Assert.Contains("moka-reconnect", dialog.ClassList);
	}

	[Fact]
	public void Dialog_KeepsItsIdWhenAConsumerPassesOne()
	{
		var cut = Render<MokaReconnectModal>(p => p
			.Add(x => x.Id, "mine")
			.AddUnmatched("id", "also-mine"));

		Assert.Equal("components-reconnect-modal", cut.Find("dialog").Id);
	}

	[Fact]
	public void Dialog_StaysClosed_SoNothingShowsUntilTheScriptOpensIt()
	{
		var cut = Render<MokaReconnectModal>();

		Assert.False(cut.Find("dialog").HasAttribute("open"));
	}

	[Fact]
	public void Dialog_IsNamedAndDescribed()
	{
		var cut = Render<MokaReconnectModal>();

		IElement dialog = cut.Find("dialog");
		Assert.Equal("moka-reconnect-title", dialog.GetAttribute("aria-labelledby"));
		Assert.Equal("moka-reconnect-message", dialog.GetAttribute("aria-describedby"));
		Assert.NotNull(cut.Find("#moka-reconnect-title"));
		Assert.NotNull(cut.Find("#moka-reconnect-message"));
	}

	[Fact]
	public void Dialog_CantBeDismissed()
	{
		var cut = Render<MokaReconnectModal>();

		Assert.Equal("none", cut.Find("dialog").GetAttribute("closedby"));
	}

	[Fact]
	public void Script_IsLinkedSoItRunsOnAStaticPage()
	{
		var cut = Render<MokaReconnectModal>();

		IElement script = cut.Find("script");
		Assert.Equal("module", script.GetAttribute("type"));
		Assert.Equal("_content/Moka.Red.Feedback/Reconnect/MokaReconnectModal.razor.js", script.GetAttribute("src"));
	}

	// In an interactive layout the render replaces the prerendered dialog the script tag bound.
	[Fact]
	public void AnInteractiveRender_BindsTheNewDialog()
	{
		SetRendererInfo(new RendererInfo("Server", true));
		BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Moka.Red.Feedback/Reconnect/MokaReconnectModal.razor.js");
		module.SetupVoid("bind").SetVoidResult();

		Render<MokaReconnectModal>();

		Assert.Single(module.Invocations, invocation => invocation.Identifier == "bind");
	}

	// Blazor would create the element and set its src, a Trusted Types sink, for a module the render imports anyway.
	[Theory]
	[InlineData("Server")]
	[InlineData("WebAssembly")]
	public void AnInteractiveRender_HasNoScriptElement(string platform)
	{
		SetRendererInfo(new RendererInfo(platform, true));

		var cut = Render<MokaReconnectModal>();

		Assert.Empty(cut.FindAll("script"));
		Assert.NotNull(cut.Find("dialog#components-reconnect-modal"));
	}

	[Theory]
	[InlineData("components-reconnect-button")]
	[InlineData("components-resume-button")]
	[InlineData("moka-reconnect-reload")]
	public void Buttons_HaveTheIdsTheScriptFinds(string id)
	{
		var cut = Render<MokaReconnectModal>();

		Assert.Equal("button", cut.Find($"#{id}").GetAttribute("type"));
	}

	[Fact]
	public void Seconds_SitInTheElementBlazorWritesTo()
	{
		var cut = Render<MokaReconnectModal>();

		IElement retrying = cut.Find(".moka-reconnect__message .moka-reconnect__retrying");
		Assert.NotNull(retrying.QuerySelector("#components-seconds-to-next-attempt"));
		Assert.Contains("Trying again in", retrying.TextContent, StringComparison.Ordinal);
		Assert.EndsWith("s.", retrying.TextContent.Trim(), StringComparison.Ordinal);
	}

	[Fact]
	public void Seconds_CanMoveWithTheWords()
	{
		var cut = Render<MokaReconnectModal>(p => p
			.Add(x => x.Text, new MokaReconnectText { RetryingMessage = "{0} seconds to the next try" }));

		IElement retrying = cut.Find(".moka-reconnect__message .moka-reconnect__retrying");
		Assert.NotNull(retrying.QuerySelector("#components-seconds-to-next-attempt"));
		Assert.EndsWith("seconds to the next try", retrying.TextContent.Trim(), StringComparison.Ordinal);
	}

	[Fact]
	public void Seconds_AreLeftOutWhenTheWordsDontAskForThem()
	{
		var cut = Render<MokaReconnectModal>(p => p
			.Add(x => x.Text, new MokaReconnectText { RetryingMessage = "Trying again soon." }));

		Assert.Empty(cut.FindAll("#components-seconds-to-next-attempt"));
		Assert.Contains("Trying again soon.", cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public void Text_ReplacesOnlyWhatsSet()
	{
		var cut = Render<MokaReconnectModal>(p => p
			.Add(x => x.Text, new MokaReconnectText { FailedTitle = "Can't reach your router" }));

		Assert.Equal("Can't reach your router", cut.Find(".moka-reconnect__title .moka-reconnect__failed").TextContent);
		Assert.Equal("Reconnecting", cut.Find(".moka-reconnect__title .moka-reconnect__first").TextContent);
	}

	[Fact]
	public void TheMarkDefaultsToAnIcon_AndChildContentReplacesIt()
	{
		Assert.NotEmpty(Render<MokaReconnectModal>().FindAll(".moka-reconnect__logo svg"));

		var cut = Render<MokaReconnectModal>(p => p
			.Add(x => x.ChildContent, (RenderFragment)(builder => builder.AddMarkupContent(0, "<img alt=\"\" src=\"logo.svg\">"))));

		Assert.Empty(cut.FindAll(".moka-reconnect__logo svg"));
		Assert.NotNull(cut.Find(".moka-reconnect__logo img"));
	}

	[Fact]
	public void ClassAndStyle_GoOnTheDialog()
	{
		var cut = Render<MokaReconnectModal>(p => p
			.Add(x => x.Class, "mine")
			.Add(x => x.Style, "width: 30rem"));

		IElement dialog = cut.Find("dialog");
		Assert.Contains("mine", dialog.ClassList);
		Assert.Contains("width: 30rem", dialog.GetAttribute("style"), StringComparison.Ordinal);
	}
}
