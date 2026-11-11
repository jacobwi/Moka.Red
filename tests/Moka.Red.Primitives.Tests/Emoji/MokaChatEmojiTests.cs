using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Primitives.Chat;
using Moka.Red.Primitives.Emoji;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Emoji;

public sealed class MokaChatEmojiTests : BunitContext
{
	private const string EmojiModule = "./_content/Moka.Red.Primitives/moka-emoji.js";

	private readonly BunitJSModuleInterop _emoji;
	private readonly BunitJSModuleInterop _popup;

	public MokaChatEmojiTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule("./_content/Moka.Red.Primitives/Emoji/MokaEmojiPanel.razor.js");
		_emoji = JSInterop.SetupModule(EmojiModule);
		_popup = TopLayerPopup.Setup(JSInterop);
	}

	[Fact]
	public void ByDefault_TheInputRowHasNoEmojiButton()
	{
		IRenderedComponent<MokaChat> cut = Render<MokaChat>();

		Assert.Empty(cut.FindAll(".moka-chat__emoji-btn"));
		Assert.Empty(cut.FindComponents<MokaEmojiPanel>());
	}

	[Fact]
	public async Task ShowEmoji_OpensAPopupAboveTheButton_AndAPickGoesInAtTheCaret()
	{
		IRenderedComponent<MokaChat> cut = Render<MokaChat>(p => p.Add(x => x.ShowEmoji, true));
		IElement button = cut.Find(".moka-chat__emoji-btn");
		string? anchorId = button.GetAttribute("blazor:elementReference");
		string? fieldId = cut.Find("textarea").GetAttribute("blazor:elementReference");

		Assert.Equal("Emoji", button.GetAttribute("aria-label"));
		Assert.Equal("dialog", button.GetAttribute("aria-haspopup"));
		Assert.Equal("false", button.GetAttribute("aria-expanded"));
		Assert.Contains("moka-tooltip-trigger", button.ParentElement!.ClassList);

		await button.ClickAsync(new MouseEventArgs());
		TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-emoji-panel"), anchorId, "top-end");

		IRenderedComponent<MokaEmojiPanel> panel = cut.FindComponent<MokaEmojiPanel>();
		await cut.InvokeAsync(() => panel.Instance.OnEmojiPicked("\U0001F525", "\U0001F525", "fire", "fire"));

		cut.WaitForAssertion(() =>
		{
			JSRuntimeInvocation insert = _emoji.VerifyInvoke("insertText");
			Assert.Equal(fieldId, Assert.IsType<ElementReference>(insert.Arguments[0]).Id);
			Assert.Equal("\U0001F525", insert.Arguments[1]);
		});
		cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".moka-emoji-panel")));
	}

	[Fact]
	public async Task TheComposerShape_DocksThePanelOverTheInput_AndKeepsItOpen()
	{
		IRenderedComponent<MokaChat> cut = Render<MokaChat>(p => p
			.Add(x => x.ShowEmoji, true)
			.Add(x => x.EmojiOptions, new MokaEmojiOptions { Shape = MokaEmojiShape.Composer }));

		Assert.Empty(cut.FindAll(".moka-emoji-panel"));
		Assert.Null(cut.Find(".moka-chat__emoji-btn").GetAttribute("aria-haspopup"));
		await cut.Find(".moka-chat__emoji-btn").ClickAsync(new MouseEventArgs());

		IElement dock = cut.Find(".moka-emoji-panel");
		Assert.Contains("moka-chat__emoji-dock", dock.ClassList);
		Assert.Contains("moka-emoji-panel--composer", dock.ClassList);
		Assert.Null(dock.GetAttribute("popover"));
		Assert.Equal("moka-chat__input", dock.NextElementSibling?.ClassName);

		IRenderedComponent<MokaEmojiPanel> panel = cut.FindComponent<MokaEmojiPanel>();
		await cut.InvokeAsync(() => panel.Instance.OnEmojiPicked("\U0001F389", "\U0001F389", "party popper", "tada"));
		await cut.InvokeAsync(() => panel.Instance.OnEmojiPicked("\U0001F680", "\U0001F680", "rocket", "rocket"));
		cut.WaitForAssertion(() => Assert.Equal(2, _emoji.Invocations["insertText"].Count));
		Assert.Single(cut.FindAll(".moka-emoji-panel"));

		await cut.Find(".moka-chat__emoji-btn").ClickAsync(new MouseEventArgs());
		Assert.Empty(cut.FindAll(".moka-emoji-panel"));
	}

	[Fact]
	public void AStripShape_OpensAsThePopoverGrid()
	{
		MokaEmojiOptions? handed = null;
		IRenderedComponent<MokaChat> cut = Render<MokaChat>(p => p
			.Add(x => x.ShowEmoji, true)
			.Add(x => x.EmojiOptions, new MokaEmojiOptions { Shape = MokaEmojiShape.Strip })
			.Add(x => x.EmojiOptionsChanged, options => handed = options));

		Assert.Equal(MokaEmojiShape.Popover, cut.FindComponent<MokaEmojiPanel>().Instance.Options?.Shape);
		Assert.Null(handed);
	}

	[Fact]
	public void EmojiLabel_NamesTheButton()
	{
		IRenderedComponent<MokaChat> cut = Render<MokaChat>(p => p
			.Add(x => x.ShowEmoji, true)
			.Add(x => x.EmojiLabel, "Emoji einf\u00FCgen"));

		Assert.Equal("Emoji einf\u00FCgen", cut.Find(".moka-chat__emoji-btn").GetAttribute("aria-label"));
	}
}
