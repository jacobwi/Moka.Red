using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;
using Moka.Red.Feedback.EmojiTypeahead;
using Moka.Red.Feedback.SlashMenu;
using Moka.Red.Primitives.Emoji;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

public sealed class MokaEmojiTypeaheadTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Feedback/EmojiTypeahead/MokaEmojiTypeahead.razor.js";

	private static readonly string[][] Rows =
	[
		["\U0001F680", "rocket", "rocket", "launch space"],
		["\U0001FAA8", "rock", "rock", "boulder stone"]
	];

	private readonly BunitJSModuleInterop _module;

	public MokaEmojiTypeaheadTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
		TopLayerPopup.Setup(JSInterop);
	}

	[Fact]
	public void ItWrapsTheField_WatchesIt_AndStartsClosed()
	{
		IRenderedComponent<MokaEmojiTypeahead> cut = Render<MokaEmojiTypeahead>(p => p
			.Add(x => x.Options, new MokaEmojiOptions { SkinTone = MokaSkinTone.Dark, MaxVersion = 15.1, SearchKeywords = false })
			.Add(x => x.MaxResults, 5)
			.AddChildContent("<textarea></textarea>"));

		Assert.NotNull(cut.Find(".moka-emoji-typeahead textarea"));
		Assert.Empty(cut.FindAll(".moka-slash-menu"));

		JSRuntimeInvocation watch = _module.VerifyInvoke("watch");
		Assert.IsType<ElementReference>(watch.Arguments[0]);
		Assert.Null(watch.Arguments[1]);
		IReadOnlyDictionary<string, object?> options = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(watch.Arguments[3]);
		Assert.Equal(5, options["tone"]);
		Assert.Equal(15.1, options["maxVersion"]);
		Assert.Equal(false, options["keywords"]);
		Assert.Equal(5, options["max"]);
		Assert.Equal(2, options["minLength"]);
		Assert.Equal(true, options["convert"]);
		Assert.Equal(false, options["disabled"]);

		cut.Render();
		_module.VerifyInvoke("watch", 1);
	}

	[Fact]
	public async Task AQuery_OpensTheSlashMenu_WithTheEmojiInPlaceOfIcons()
	{
		IRenderedComponent<MokaEmojiTypeahead> cut = Render<MokaEmojiTypeahead>(p => p.AddChildContent("<textarea></textarea>"));

		await cut.InvokeAsync(() => cut.Instance.OnQuery("roc", Rows));

		IReadOnlyList<IElement> options = cut.FindAll(".moka-slash-menu [role=option]");
		Assert.Equal(2, options.Count);
		Assert.Equal("\U0001F680", options[0].QuerySelector(".moka-slash-menu-glyph")!.TextContent);
		Assert.Equal(":rocket:", options[0].QuerySelector(".moka-slash-menu-title")!.TextContent);
		Assert.Equal("rocket", options[0].QuerySelector(".moka-slash-menu-desc")!.TextContent);
		Assert.Empty(options[0].QuerySelectorAll("svg"));

		JSRuntimeInvocation describe = _module.Invocations["describe"][^1];
		Assert.Equal(cut.FindComponent<MokaSlashMenu>().Instance.ListboxId, describe.Arguments[1]);
		Assert.Equal(options[0].Id, describe.Arguments[2]);
	}

	[Fact]
	public async Task TheKeys_MoveAndPick_AndAPickIsInsertedByTheModule()
	{
		IRenderedComponent<MokaEmojiTypeahead> cut = Render<MokaEmojiTypeahead>(p => p.AddChildContent("<textarea></textarea>"));
		await cut.InvokeAsync(() => cut.Instance.OnQuery("roc", Rows));

		await cut.InvokeAsync(() => cut.Instance.OnKey("ArrowDown"));
		cut.WaitForAssertion(() => Assert.Equal("true", cut.FindAll(".moka-slash-menu [role=option]")[1].GetAttribute("aria-selected")));

		await cut.InvokeAsync(() => cut.Instance.OnKey("Tab"));

		cut.WaitForAssertion(() =>
		{
			JSRuntimeInvocation insert = _module.VerifyInvoke("insert");
			Assert.Equal("\U0001FAA8", insert.Arguments[1]);
		});
		cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".moka-slash-menu")));
		_module.VerifyInvoke("closed");
		Assert.Null(_module.Invocations["describe"][^1].Arguments[1]);
	}

	[Fact]
	public async Task TheModuleClosingIt_ClosesTheMenu()
	{
		IRenderedComponent<MokaEmojiTypeahead> cut = Render<MokaEmojiTypeahead>(p => p.AddChildContent("<textarea></textarea>"));
		await cut.InvokeAsync(() => cut.Instance.OnQuery("roc", Rows));

		await cut.InvokeAsync(() => cut.Instance.OnClosed());

		Assert.Empty(cut.FindAll(".moka-slash-menu"));
	}

	[Fact]
	public async Task NoMatches_SaysWhatWasTyped()
	{
		IRenderedComponent<MokaEmojiTypeahead> cut = Render<MokaEmojiTypeahead>(p => p
			.AddCascadingValue(MokaTheme.Red)
			.AddChildContent("<textarea></textarea>"));

		await cut.InvokeAsync(() => cut.Instance.OnQuery("zzq", []));

		Assert.Contains("No emoji for :zzq", cut.Find(".moka-slash-menu").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Disabled_TellsTheModule_AndIgnoresQueries()
	{
		IRenderedComponent<MokaEmojiTypeahead> cut = Render<MokaEmojiTypeahead>(p => p
			.Add(x => x.Disabled, true)
			.AddChildContent("<textarea></textarea>"));

		Assert.Equal(true, Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(_module.VerifyInvoke("watch").Arguments[3])["disabled"]);
		await cut.InvokeAsync(() => cut.Instance.OnQuery("roc", Rows));

		Assert.Empty(cut.FindAll(".moka-slash-menu"));
	}

	[Fact]
	public void For_WatchesAndAnchorsOnTheGivenField()
	{
		ElementReference field = new("plain-field");
		IRenderedComponent<MokaEmojiTypeahead> cut = Render<MokaEmojiTypeahead>(p => p.Add(x => x.For, field));

		Assert.Equal("plain-field", Assert.IsType<ElementReference>(_module.VerifyInvoke("watch").Arguments[1]).Id);
		Assert.Equal("plain-field", cut.FindComponent<MokaSlashMenu>().Instance.Anchor?.Id);
	}
}
