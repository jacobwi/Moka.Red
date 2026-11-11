using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Primitives.Emoji;
using Moka.Red.Primitives.Reactions;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Emoji;

public sealed class MokaReactionsTests : BunitContext
{
	private const string Thumbs = "\U0001F44D";
	private const string Tada = "\U0001F389";
	private const string Eyes = "\U0001F440";

	private readonly BunitJSModuleInterop _popup;
	private readonly List<string> _toggled = [];

	public MokaReactionsTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule("./_content/Moka.Red.Primitives/Emoji/MokaEmojiPanel.razor.js");
		_popup = TopLayerPopup.Setup(JSInterop);
	}

	[Fact]
	public void TheCounts_AreNamedButtons_PressedWhereTheViewerReacted()
	{
		IRenderedComponent<MokaReactions> cut = Render(null, [new(Thumbs, 3, true), new(Tada, 1, false), new(Eyes, 0, false)]);
		IReadOnlyList<IElement> chips = cut.FindAll(".moka-reactions__chip");

		Assert.Equal(2, chips.Count);
		Assert.Equal("true", chips[0].GetAttribute("aria-pressed"));
		Assert.Equal($"{Thumbs}, 3 people, including you", chips[0].GetAttribute("aria-label"));
		Assert.Equal("false", chips[1].GetAttribute("aria-pressed"));
		Assert.Equal($"{Tada}, 1 person", chips[1].GetAttribute("aria-label"));
		Assert.Equal("3", chips[0].QuerySelector(".moka-reactions__count")!.TextContent);
		Assert.Equal("Reactions", cut.Find(".moka-reactions__chips").GetAttribute("aria-label"));
	}

	[Fact]
	public async Task ClickingACount_TogglesIt()
	{
		IRenderedComponent<MokaReactions> cut = Render(null, [new(Thumbs, 3, true)]);

		await cut.Find(".moka-reactions__chip").ClickAsync(new MouseEventArgs());

		Assert.Equal([Thumbs], _toggled);
	}

	[Fact]
	public void WithoutAMessage_ThereIsNoBar()
	{
		IRenderedComponent<MokaReactions> cut = Render(null, []);

		Assert.Empty(cut.FindAll(".moka-reactions__bar"));
		Assert.DoesNotContain("moka-reactions--message", cut.Find(".moka-reactions").ClassList);
	}

	[Fact]
	public async Task WithAMessage_TheBarHoldsSixQuickReactions_AndAnAllEmojiButton()
	{
		IRenderedComponent<MokaReactions> cut = Render("Release 0.4 is tagged.", [new(Thumbs, 1, true)]);
		IElement bar = cut.Find(".moka-reactions__bar");

		Assert.Equal("toolbar", bar.GetAttribute("role"));
		IReadOnlyList<IElement> quick = cut.FindAll(".moka-reactions__quick");
		Assert.Equal(6, quick.Count);
		Assert.Equal($"React with {Thumbs}", quick[0].GetAttribute("aria-label"));
		Assert.Equal("true", quick[0].GetAttribute("aria-pressed"));
		Assert.Equal("false", quick[1].GetAttribute("aria-pressed"));
		Assert.Contains("Release 0.4", cut.Find(".moka-reactions__content").TextContent, StringComparison.Ordinal);

		IElement more = cut.Find(".moka-reactions__more");
		Assert.Equal("All emoji", more.GetAttribute("aria-label"));
		Assert.Contains("moka-tooltip-trigger", more.ParentElement!.ClassList);

		await quick[3].ClickAsync(new MouseEventArgs());
		Assert.Equal([Tada], _toggled);
	}

	[Fact]
	public void ShowBarAndShowAdd_TakeThemAway()
	{
		IRenderedComponent<MokaReactions> cut = Render<MokaReactions>(p => p
			.Add(x => x.Reactions, [new MokaReaction(Thumbs, 1, false)])
			.Add(x => x.ShowBar, false)
			.Add(x => x.ShowAdd, false)
			.AddChildContent("Message"));

		Assert.Empty(cut.FindAll(".moka-reactions__bar"));
		Assert.Empty(cut.FindAll(".moka-reactions__add"));
		Assert.Empty(cut.FindComponents<MokaEmojiPanel>());
	}

	[Fact]
	public async Task AddReaction_OpensThePanelAgainstItself_AndAPickReacts()
	{
		IRenderedComponent<MokaReactions> cut = Render(null, [new(Thumbs, 2, true)]);
		IElement add = cut.Find(".moka-reactions__add");
		string? anchorId = add.GetAttribute("blazor:elementReference");

		await add.ClickAsync(new MouseEventArgs());

		Assert.Equal("true", cut.Find(".moka-reactions__add").GetAttribute("aria-expanded"));
		IElement panel = cut.Find(".moka-emoji-panel");
		TopLayerPopup.AssertOpened(_popup, panel, anchorId, "bottom-start");

		IRenderedComponent<MokaEmojiPanel> picker = cut.FindComponent<MokaEmojiPanel>();
		await cut.InvokeAsync(() => picker.Instance.OnEmojiPicked(Thumbs, Thumbs, "thumbs up", "+1"));
		await cut.InvokeAsync(() => picker.Instance.OnEmojiPicked(Eyes, Eyes, "eyes", "eyes"));
		cut.WaitForAssertion(() => Assert.Equal([Eyes], _toggled));
	}

	[Fact]
	public async Task AStripShape_StillOpensAsAPopover()
	{
		IRenderedComponent<MokaReactions> cut = Render<MokaReactions>(p => p
			.Add(x => x.Options, new MokaEmojiOptions { Shape = MokaEmojiShape.Strip }));

		await cut.Find(".moka-reactions__add").ClickAsync(new MouseEventArgs());

		Assert.Contains("moka-emoji-panel--popover", cut.Find(".moka-emoji-panel").ClassList);
	}

	[Fact]
	public void TheWords_CanBeReplaced()
	{
		IRenderedComponent<MokaReactions> cut = Render<MokaReactions>(p => p
			.Add(x => x.Reactions, [new MokaReaction(Thumbs, 4, true)])
			.Add(x => x.Text, MokaReactionsText.Default with { People = "{count} personer", IncludingYou = ", du med", Add = "Reagera" }));

		Assert.Equal($"{Thumbs}, 4 personer, du med", cut.Find(".moka-reactions__chip").GetAttribute("aria-label"));
		Assert.Equal("Reagera", cut.Find(".moka-reactions__add").GetAttribute("aria-label"));
	}

	[Fact]
	public void SpacingParameters_LandOnTheRoot()
	{
		IRenderedComponent<MokaReactions> cut = Render<MokaReactions>(p => p
			.Add(x => x.MarginValue, "7px")
			.Add(x => x.PaddingValue, "5px")
			.Add(x => x.RoundedValue, "3px"));

		Assert.Equal("margin: 7px; padding: 5px; border-radius: 3px", cut.Find(".moka-reactions").GetAttribute("style"));
	}

	private IRenderedComponent<MokaReactions> Render(string? message, IReadOnlyList<MokaReaction> reactions) =>
		Render<MokaReactions>(p =>
		{
			p.Add(x => x.Reactions, reactions).Add(x => x.OnToggle, emoji => _toggled.Add(emoji));
			if (message is not null)
			{
				p.AddChildContent(message);
			}
		});
}
