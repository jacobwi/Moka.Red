using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.TextArea;
using Moka.Red.Forms.TextField;
using Moka.Red.Primitives.Emoji;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// MokaTextField.EmojiButton and MokaTextArea.EmojiButton: a smile that opens the emoji panel and puts the pick in at
// the caret, off by default.
public sealed class EmojiButtonTests : BunitContext
{
	private const string EmojiModule = "./_content/Moka.Red.Primitives/moka-emoji.js";
	private const string Sparkles = "\u2728";

	private readonly BunitJSModuleInterop _emoji;
	private readonly BunitJSModuleInterop _popup;

	public EmojiButtonTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule("./_content/Moka.Red.Primitives/Emoji/MokaEmojiPanel.razor.js");
		_emoji = JSInterop.SetupModule(EmojiModule);
		_popup = TopLayerPopup.Setup(JSInterop);
	}

	[Fact]
	public void ByDefault_NeitherFieldHasOne()
	{
		IRenderedComponent<MokaTextField> field = Render<MokaTextField>();
		IRenderedComponent<MokaTextArea> area = Render<MokaTextArea>();

		Assert.Empty(field.FindAll(".moka-textfield-emoji"));
		Assert.DoesNotContain("moka-textfield-input--has-emoji", field.Find("input").ClassList);
		Assert.DoesNotContain("moka-textfield--emoji", field.Find(".moka-textfield").ClassList);
		Assert.Empty(area.FindAll(".moka-textarea-emoji"));
		Assert.DoesNotContain("moka-textarea--emoji", area.Find(".moka-textarea").ClassList);
		Assert.Empty(field.FindComponents<MokaEmojiPanel>());
		Assert.Empty(area.FindComponents<MokaEmojiPanel>());
	}

	[Fact]
	public async Task TheTextField_OpensThePanel_AndAPickGoesInAtTheCaret()
	{
		IRenderedComponent<MokaTextField> cut = Render<MokaTextField>(p => p.Add(x => x.EmojiButton, true));
		IElement button = cut.Find(".moka-textfield-emoji");
		string? anchorId = button.GetAttribute("blazor:elementReference");

		Assert.Equal("Insert emoji", button.GetAttribute("aria-label"));
		Assert.Contains("moka-textfield-emoji-tip", button.ParentElement!.ClassList);
		Assert.Contains("moka-textfield-input--has-emoji", cut.Find("input").ClassList);

		await button.ClickAsync(new MouseEventArgs());
		Assert.Equal("true", cut.Find(".moka-textfield-emoji").GetAttribute("aria-expanded"));
		TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-emoji-panel"), anchorId, "bottom-end");

		await cut.InvokeAsync(() => cut.FindComponent<MokaEmojiPanel>().Instance.OnEmojiPicked(Sparkles, Sparkles, "sparkles", "sparkles"));

		cut.WaitForAssertion(() =>
		{
			JSRuntimeInvocation insert = _emoji.VerifyInvoke("insertNear");
			Assert.Equal(anchorId, Assert.IsType<ElementReference>(insert.Arguments[0]).Id);
			Assert.Equal(".moka-textfield", insert.Arguments[1]);
			Assert.Equal(Sparkles, insert.Arguments[2]);
		});
		cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".moka-emoji-panel")));

		// The script puts focus back in the field, so the panel doesn't send it to the button.
		Assert.Empty(JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
	}

	[Fact]
	public async Task EscapeInTheFieldsPanel_PutsFocusOnTheButton()
	{
		IRenderedComponent<MokaTextField> cut = Render<MokaTextField>(p => p.Add(x => x.EmojiButton, true));
		string? anchorId = cut.Find(".moka-textfield-emoji").GetAttribute("blazor:elementReference");
		await cut.Find(".moka-textfield-emoji").ClickAsync(new MouseEventArgs());

		await cut.Find(".moka-emoji-panel").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Equal(anchorId, Assert.IsType<ElementReference>(Assert.Single(JSInterop.VerifyFocusAsyncInvoke(1)).Arguments[0]).Id);
		Assert.Equal("false", cut.Find(".moka-textfield-emoji").GetAttribute("aria-expanded"));
	}

	[Fact]
	public void BesideTheClearButton_TheEmojiButtonMovesIn()
	{
		IRenderedComponent<MokaTextField> cut = Render<MokaTextField>(p => p
			.Add(x => x.EmojiButton, true)
			.Add(x => x.Clearable, true)
			.Add(x => x.Value, "Release"));

		Assert.Contains("moka-textfield--emoji-inset", cut.Find(".moka-textfield").ClassList);
		Assert.Single(cut.FindAll(".moka-textfield-clear"));
		Assert.Single(cut.FindAll(".moka-textfield-emoji"));
	}

	[Fact]
	public void AReadOnlyField_DisablesTheButton()
	{
		IRenderedComponent<MokaTextField> cut = Render<MokaTextField>(p => p
			.Add(x => x.EmojiButton, true)
			.Add(x => x.ReadOnly, true));

		Assert.True(cut.Find(".moka-textfield-emoji").HasAttribute("disabled"));
	}

	[Fact]
	public async Task TheTextArea_OpensThePanel_AndAPickGoesInAtTheCaret()
	{
		IRenderedComponent<MokaTextArea> cut = Render<MokaTextArea>(p => p
			.Add(x => x.EmojiButton, true)
			.Add(x => x.EmojiButtonLabel, "Emoji"));
		IElement button = cut.Find(".moka-textarea-emoji");
		string? anchorId = button.GetAttribute("blazor:elementReference");

		Assert.Equal("Emoji", button.GetAttribute("aria-label"));
		Assert.Contains("moka-textarea-emoji-tip", button.ParentElement!.ClassList);
		Assert.Contains("moka-textarea--emoji", cut.Find(".moka-textarea").ClassList);

		await button.ClickAsync(new MouseEventArgs());
		await cut.InvokeAsync(() => cut.FindComponent<MokaEmojiPanel>().Instance.OnEmojiPicked(Sparkles, Sparkles, "sparkles", "sparkles"));

		cut.WaitForAssertion(() =>
		{
			JSRuntimeInvocation insert = _emoji.VerifyInvoke("insertNear");
			Assert.Equal(anchorId, Assert.IsType<ElementReference>(insert.Arguments[0]).Id);
			Assert.Equal(".moka-textarea", insert.Arguments[1]);
		});
	}

	[Theory]
	[InlineData(MokaEmojiShape.Strip, MokaEmojiShape.Popover)]
	[InlineData(MokaEmojiShape.Composer, MokaEmojiShape.Popover)]
	[InlineData(MokaEmojiShape.List, MokaEmojiShape.List)]
	public void TheButtonsPanel_IsAlwaysAPopup(MokaEmojiShape asked, MokaEmojiShape shown)
	{
		IRenderedComponent<MokaTextArea> cut = Render<MokaTextArea>(p => p
			.Add(x => x.EmojiButton, true)
			.Add(x => x.EmojiOptions, new MokaEmojiOptions { Shape = asked }));

		Assert.Equal(shown, cut.FindComponent<MokaEmojiPanel>().Instance.Options?.Shape);
	}

	[Fact]
	public void TheStylesheets_MakeRoomForTheButton()
	{
		Assert.Equal("2rem", ScopedCss.Declarations("MokaTextField.razor.css", ".moka-textfield-input--has-emoji")["padding-right"]);
		Assert.Equal("absolute", ScopedCss.Declarations("MokaTextField.razor.css", ".moka-textfield ::deep .moka-textfield-emoji-tip")["position"]);
		Assert.Null(ScopedCss.Declarations("MokaTextField.razor.css", ".moka-textfield ::deep .moka-textfield-emoji-tip").GetValueOrDefault("transform"));
		Assert.Equal("2.5rem", ScopedCss.Declarations("MokaTextArea.razor.css", ".moka-textarea--emoji .moka-textarea-input")["padding-right"]);
	}
}
