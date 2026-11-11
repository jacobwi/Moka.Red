using System.Linq.Expressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.EmojiPicker;
using Moka.Red.Primitives.Emoji;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

public sealed class MokaEmojiPickerTests : BunitContext
{
	private const string Plane = "\u2708\uFE0F";
	private const string Rocket = "\U0001F680";

	private readonly BunitJSModuleInterop _popup;

	public MokaEmojiPickerTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule("./_content/Moka.Red.Primitives/Emoji/MokaEmojiPanel.razor.js");
		_popup = TopLayerPopup.Setup(JSInterop);
	}

	[Fact]
	public void Empty_ShowsASmileWithAPlus_NamedByTheLabelAndNone()
	{
		IRenderedComponent<MokaEmojiPicker> cut = Render<MokaEmojiPicker>(p => p
			.Add(x => x.Label, "Status emoji")
			.Add(x => x.Id, "status"));

		IElement button = cut.Find(".moka-emoji-picker__button");
		Assert.Equal("status", button.Id);
		Assert.Equal("status-label status-value", button.GetAttribute("aria-labelledby"));
		Assert.Equal("None", cut.Find("#status-value").TextContent);
		Assert.Equal("dialog", button.GetAttribute("aria-haspopup"));
		Assert.Equal("false", button.GetAttribute("aria-expanded"));
		Assert.NotNull(button.QuerySelector("svg"));
		Assert.Contains("moka-emoji-picker__button--empty", button.ClassList);
		Assert.Null(cut.Find("label.moka-field-label").GetAttribute("for"));
		Assert.Contains("moka-tooltip-trigger", button.ParentElement!.ClassList);
	}

	[Fact]
	public void WithAValue_ShowsTheEmoji()
	{
		IRenderedComponent<MokaEmojiPicker> cut = Render<MokaEmojiPicker>(p => p
			.Add(x => x.Value, Plane)
			.Add(x => x.Id, "status"));

		Assert.Equal(Plane, cut.Find(".moka-emoji-picker__glyph").TextContent);
		Assert.Equal("true", cut.Find(".moka-emoji-picker__glyph").GetAttribute("aria-hidden"));
		Assert.Equal(Plane, cut.Find("#status-value").TextContent);
		Assert.Equal("status-value", cut.Find("button.moka-emoji-picker__button").GetAttribute("aria-labelledby"));
	}

	[Fact]
	public async Task TheButton_OpensThePanelAgainstItself_AndAPickBindsTheValue()
	{
		string? value = null;
		IRenderedComponent<MokaEmojiPicker> cut = Render<MokaEmojiPicker>(p => p
			.Add(x => x.Id, "status")
			.Add(x => x.Placement, Moka.Red.Feedback.Tooltip.MokaTooltipPosition.BottomEnd)
			.Add(x => x.ValueChanged, v => value = v));
		IElement button = cut.Find(".moka-emoji-picker__button");
		string? anchorId = button.GetAttribute("blazor:elementReference");

		await button.ClickAsync(new MouseEventArgs());

		Assert.Equal("true", cut.Find(".moka-emoji-picker__button").GetAttribute("aria-expanded"));
		TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-emoji-panel"), anchorId, "bottom-end");

		MokaEmojiPanel panel = cut.FindComponent<MokaEmojiPanel>().Instance;
		await cut.InvokeAsync(() => panel.OnEmojiPicked(Rocket, Rocket, "rocket", "rocket"));

		cut.WaitForAssertion(() => Assert.Equal(Rocket, value));
		cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".moka-emoji-panel")));
		Assert.Equal("rocket", cut.Find("#status-value").TextContent);
		Assert.Equal(Rocket, panel.Selected);
		Assert.Equal(anchorId, Assert.IsType<ElementReference>(Assert.Single(JSInterop.VerifyFocusAsyncInvoke(1)).Arguments[0]).Id);
	}

	[Fact]
	public async Task Clearable_ClearsTheChoice()
	{
		string? value = Plane;
		IRenderedComponent<MokaEmojiPicker> cut = Render<MokaEmojiPicker>(p => p
			.Add(x => x.Value, Plane)
			.Add(x => x.Clearable, true)
			.Add(x => x.ValueChanged, v => value = v));

		IElement clear = cut.Find(".moka-emoji-picker__clear");
		Assert.Equal("Clear emoji", clear.GetAttribute("aria-label"));
		Assert.Contains("moka-tooltip-trigger", clear.ParentElement!.ClassList);

		await clear.ClickAsync(new MouseEventArgs());

		Assert.Null(value);
	}

	[Fact]
	public void InAForm_ItPostsThroughAHiddenField_AndShowsItsValidation()
	{
		Model model = new();
		EditContext context = new(model);
		ValidationMessageStore messages = new(context);
		messages.Add(context.Field(nameof(Model.Icon)), "Pick an icon.");

		IRenderedComponent<MokaEmojiPicker> cut = Render<MokaEmojiPicker>(p => p
			.AddCascadingValue(context)
			.Add(x => x.Value, Rocket)
			.Add(x => x.Required, true)
			.Add(x => x.ValueExpression, (Expression<Func<string>>)(() => model.Icon!)));

		IElement hidden = cut.Find("input[type=hidden]");
		Assert.Equal("model.Icon", hidden.GetAttribute("name"));
		Assert.Equal(Rocket, hidden.GetAttribute("value"));

		IElement button = cut.Find(".moka-emoji-picker__button");
		Assert.Equal("true", button.GetAttribute("aria-invalid"));
		Assert.Equal("true", button.GetAttribute("aria-required"));
		Assert.Equal("Pick an icon.", cut.Find(".moka-field-helper--error").TextContent.Trim());
	}

	[Fact]
	public async Task APickInAForm_MarksTheFieldModified()
	{
		Model model = new();
		EditContext context = new(model);
		IRenderedComponent<MokaEmojiPicker> cut = Render<MokaEmojiPicker>(p => p
			.AddCascadingValue(context)
			.Add(x => x.Value, model.Icon)
			.Add(x => x.ValueChanged, v => model.Icon = v)
			.Add(x => x.ValueExpression, (Expression<Func<string>>)(() => model.Icon!)));
		await cut.Find(".moka-emoji-picker__button").ClickAsync(new MouseEventArgs());

		await cut.InvokeAsync(() => cut.FindComponent<MokaEmojiPanel>().Instance.OnEmojiPicked(Rocket, Rocket, "rocket", "rocket"));

		cut.WaitForAssertion(() => Assert.Equal(Rocket, model.Icon));
		Assert.True(context.IsModified(context.Field(nameof(Model.Icon))));
	}

	[Fact]
	public void TheStripShape_IsTheQuickPicksWithNoButton()
	{
		IRenderedComponent<MokaEmojiPicker> cut = Render<MokaEmojiPicker>(p => p
			.Add(x => x.Label, "Mood")
			.Add(x => x.Id, "mood")
			.Add(x => x.Value, "\U0001F354")
			.Add(x => x.Options, new MokaEmojiOptions { Shape = MokaEmojiShape.Strip }));

		Assert.Empty(cut.FindAll(".moka-emoji-picker__button"));
		IElement strip = cut.Find(".moka-emoji-panel");
		Assert.Equal("mood", strip.Id);
		Assert.Equal("mood-label", strip.GetAttribute("aria-labelledby"));
		Assert.Equal("\U0001F354", cut.Find(".moka-emoji-panel__quick[aria-pressed=true]").GetAttribute("data-emoji"));
	}

	[Fact]
	public async Task TheComposerShape_DocksThePanelUnderTheButton()
	{
		IRenderedComponent<MokaEmojiPicker> cut = Render<MokaEmojiPicker>(p => p
			.Add(x => x.Options, new MokaEmojiOptions { Shape = MokaEmojiShape.Composer }));

		Assert.Null(cut.Find(".moka-emoji-picker__button").GetAttribute("aria-haspopup"));
		await cut.Find(".moka-emoji-picker__button").ClickAsync(new MouseEventArgs());

		IElement panel = cut.Find(".moka-emoji-panel");
		Assert.Contains("moka-emoji-picker__dock", panel.ClassList);
		Assert.Null(panel.GetAttribute("popover"));
	}

	[Fact]
	public async Task TheListShape_OpensTheList()
	{
		IRenderedComponent<MokaEmojiPicker> cut = Render<MokaEmojiPicker>(p => p
			.Add(x => x.Options, new MokaEmojiOptions { Shape = MokaEmojiShape.List }));

		await cut.Find(".moka-emoji-picker__button").ClickAsync(new MouseEventArgs());

		Assert.Equal("listbox", cut.Find(".moka-emoji-panel__scroll").GetAttribute("role"));
		Assert.Contains("moka-emoji-panel--popup", cut.Find(".moka-emoji-panel").ClassList);
	}

	[Fact]
	public void Disabled_DisablesTheButton()
	{
		IRenderedComponent<MokaEmojiPicker> cut = Render<MokaEmojiPicker>(p => p.Add(x => x.Disabled, true));

		Assert.True(cut.Find(".moka-emoji-picker__button").HasAttribute("disabled"));
	}

	[Fact]
	public void TheSize_SetsTheButtonsSizeClass()
	{
		IRenderedComponent<MokaEmojiPicker> cut = Render<MokaEmojiPicker>(p => p.Add(x => x.Size, Moka.Red.Core.Enums.MokaSize.Lg));

		Assert.Contains("moka-emoji-picker--lg", cut.Find(".moka-emoji-picker").ClassList);
	}

	[Fact]
	public void TheGlyph_DrawsInMokaFlagsFontFirst_SoFlagsShowOnWindows()
	{
		Assert.StartsWith("'Twemoji Country Flags'", ScopedCss.Declarations("MokaEmojiPicker.razor.css", ".moka-emoji-picker")["--_emoji"],
			StringComparison.Ordinal);
		Assert.Equal("var(--_emoji)", ScopedCss.Declarations("MokaEmojiPicker.razor.css", ".moka-emoji-picker__glyph")["font-family"]);
	}

	private sealed class Model
	{
		public string? Icon { get; set; }
	}
}
