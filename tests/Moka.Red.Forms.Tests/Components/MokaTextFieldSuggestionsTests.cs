using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Theming;
using Moka.Red.Forms.TextField;
using Moka.Red.Icons;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// A field that took any text could suggest nothing, and MokaAutoComplete takes only a picked item. Suggestions makes a
// text field an editable combobox with a list: whatever is typed is the value, and a suggestion is one way to type it.
public sealed partial class MokaTextFieldSuggestionsTests : BunitContext
{
	private static readonly string[] Models = ["gpt-5", "gpt-5-mini", "claude-opus-5-5", "claude-sonnet-5-5", "llama-4"];

	private readonly BunitJSModuleInterop _hover;
	private readonly BunitJSModuleInterop _popup;

	public MokaTextFieldSuggestionsTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_popup = TopLayerPopup.Setup(JSInterop);
		_hover = JSInterop.SetupModule("./_content/Moka.Red.Core/moka-hover.js");
		_hover.SetupVoid("trackHover", _ => true).SetVoidResult();
	}

	// Recorded from 0.12.0 before Suggestions existed. Handler numbers and tooltip anchors change per render and scope
	// attributes per build, so they are left out of the comparison.
	[Theory]
	[MemberData(nameof(MarkupBeforeSuggestions))]
	public void WithoutSuggestions_TheMarkupIsAsItWas(string name, string expected)
	{
		string markup = name switch
		{
			"labelled" => Render<MokaTextField>(p => p
				.Add(x => x.Id, "name")
				.Add(x => x.Label, "Name")
				.Add(x => x.HelperText, "As on your passport")
				.Add(x => x.Placeholder, "Ada Lovelace")
				.Add(x => x.Clearable, true)
				.Add(x => x.Value, "Ada")
				.Add(x => x.StartIcon, MokaIcons.Action.Search)).Markup,
			"suffix" => Render<MokaTextField>(p => p
				.Add(x => x.Id, "rate")
				.Add(x => x.Suffix, "Mbit/s")
				.Add(x => x.Mono, true)
				.Add(x => x.EndIcon, MokaIcons.Action.Search)
				.Add(x => x.ErrorText, "Too fast")).Markup,
			_ => Render<MokaTextField>(p => p.Add(x => x.Id, "plain")).Markup
		};

		Assert.Equal(Normalize(expected), Normalize(markup));
	}

	public static TheoryData<string, string> MarkupBeforeSuggestions => new()
	{
		{
			"labelled",
			"""<div class="moka-field moka-field--md" b-imlqwjgpuk><label class="moka-field-label" for="name" id="name-label" b-imlqwjgpuk>Name</label><div class="moka-field-input-wrapper" b-imlqwjgpuk><div class="moka-textfield" b-93g2xbxz1j><span class="moka-textfield-start-icon" b-93g2xbxz1j><svg class="moka-icon moka-icon--xs" style="display:inline-block;vertical-align:middle;flex-shrink:0;width: 14px; height: 14px" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" b-1s7lc0ersg><path d="M11 3a8 8 0 1 0 0 16 8 8 0 0 0 0-16z M21 21l-4.35-4.35" b-1s7lc0ersg></path></svg></span><input class="moka-textfield-input moka-textfield-input--md moka-textfield-input--has-start-icon moka-textfield-input--has-end-icon" aria-describedby="name-help" type="text" blazor:onfocus="1" blazor:onblur="2" placeholder="Ada Lovelace" id="name" value="Ada" blazor:oninput="3" b-93g2xbxz1j /><span class="moka-tooltip-trigger moka-textfield-clear-tip" style="anchor-name: --moka-tooltip-741016f9e2de484cb3a7a30cd459ad48" b-re5evkcudu><button type="button" class="moka-textfield-clear" blazor:onclick="4" tabindex="-1" aria-label="Clear" b-93g2xbxz1j><svg class="moka-icon moka-icon--md" style="display:inline-block;vertical-align:middle;flex-shrink:0;width: 14px; height: 14px" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" b-1s7lc0ersg><path d="M18 6L6 18 M6 6l12 12" b-1s7lc0ersg></path></svg></button><span class="moka-tooltip-popup moka-tooltip-popup--bottom-end" style="position-anchor: --moka-tooltip-741016f9e2de484cb3a7a30cd459ad48; --moka-tooltip-delay: 300ms" role="tooltip" b-re5evkcudu>Clear</span></span></div></div><div class="moka-field-helper" id="name-help" b-imlqwjgpuk>As on your passport</div></div>"""
		},
		{
			"suffix",
			"""<div class="moka-field moka-field--md moka-field--error" b-imlqwjgpuk><div class="moka-field-input-wrapper" b-imlqwjgpuk><div class="moka-textfield moka-textfield--error moka-textfield--suffix-inset" b-93g2xbxz1j><input class="moka-textfield-input moka-textfield-input--md moka-textfield-input--has-end-icon moka-field-input--has-suffix moka-field-input--mono" style="--moka-field-suffix-ch: 6ch" aria-describedby="rate-suffix rate-help" spellcheck="false" type="text" blazor:onfocus="5" blazor:onblur="6" id="rate" blazor:oninput="7" b-93g2xbxz1j /><span class="moka-field-suffix" id="rate-suffix" b-93g2xbxz1j>Mbit/s</span><span class="moka-textfield-end-icon" b-93g2xbxz1j><svg class="moka-icon moka-icon--xs" style="display:inline-block;vertical-align:middle;flex-shrink:0;width: 14px; height: 14px" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" b-1s7lc0ersg><path d="M11 3a8 8 0 1 0 0 16 8 8 0 0 0 0-16z M21 21l-4.35-4.35" b-1s7lc0ersg></path></svg></span></div></div><div class="moka-field-helper moka-field-helper--error" id="rate-help" b-imlqwjgpuk>Too fast</div></div>"""
		},
		{
			"plain",
			"""<div class="moka-field moka-field--md" b-imlqwjgpuk><div class="moka-field-input-wrapper" b-imlqwjgpuk><div class="moka-textfield" b-93g2xbxz1j><input class="moka-textfield-input moka-textfield-input--md" type="text" blazor:onfocus="8" blazor:onblur="9" id="plain" blazor:oninput="10" b-93g2xbxz1j /></div></div></div>"""
		}
	};

	[Fact]
	public void Suggestions_MakeTheBoxAClosedCombobox()
	{
		IElement input = Field().Find("input");

		Assert.Equal("combobox", input.GetAttribute("role"));
		Assert.Equal("list", input.GetAttribute("aria-autocomplete"));
		Assert.Equal("listbox", input.GetAttribute("aria-haspopup"));
		Assert.Equal("false", input.GetAttribute("aria-expanded"));
		Assert.Equal("off", input.GetAttribute("autocomplete"));
		Assert.False(input.HasAttribute("aria-controls"));
		Assert.False(input.HasAttribute("aria-activedescendant"));
	}

	[Fact]
	public void Typing_ShowsTheSuggestionsThatContainIt_WhateverTheCase()
	{
		IRenderedComponent<MokaTextField> cut = Field();

		cut.Find("input").Input("CLAUDE");

		Assert.Equal(["claude-opus-5-5", "claude-sonnet-5-5"], Options(cut));
		IElement input = cut.Find("input");
		IElement listbox = cut.Find("[role=listbox]");
		Assert.Equal("true", input.GetAttribute("aria-expanded"));
		Assert.Equal(listbox.Id, input.GetAttribute("aria-controls"));
		Assert.Equal("model-label", listbox.GetAttribute("aria-labelledby"));
		Assert.False(input.HasAttribute("aria-activedescendant"));
		Assert.All(cut.FindAll("[role=option]"), option => Assert.False(option.HasAttribute("aria-selected")));
	}

	[Fact]
	public void TheList_OpensInTheTopLayer_AgainstTheField()
	{
		IRenderedComponent<MokaTextField> cut = Field();
		string? anchor = cut.Find(".moka-textfield").GetAttribute("blazor:elementReference");

		cut.Find("input").Input("gpt");

		IReadOnlyDictionary<string, object?> options =
			TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-textfield-suggestions"), anchor, "bottom-start");
		Assert.Equal("match", options["width"]);
	}

	[Fact]
	public void Down_InAnEmptyBox_ShowsTheWholeList_UpToMaxSuggestions()
	{
		IRenderedComponent<MokaTextField> cut = Field(p => p.Add(x => x.MaxSuggestions, 3));

		cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

		Assert.Equal(["gpt-5", "gpt-5-mini", "claude-opus-5-5"], Options(cut));
		Assert.Equal(Option(cut, 0).Id, cut.Find("input").GetAttribute("aria-activedescendant"));
	}

	[Fact]
	public void TextThatMatchesNothing_ShowsNoList_AndStaysTheValue()
	{
		List<string> changes = [];
		IRenderedComponent<MokaTextField> cut = Field(p => p.Add(x => x.ValueChanged, (string value) => changes.Add(value)));

		cut.Find("input").Input("my-own-model");

		Assert.Empty(cut.FindAll("[role=listbox]"));
		Assert.Equal("false", cut.Find("input").GetAttribute("aria-expanded"));
		Assert.Equal(["my-own-model"], changes);
	}

	[Fact]
	public void DownAndUp_MoveTheHighlight_AndWrap()
	{
		IRenderedComponent<MokaTextField> cut = Field();
		cut.Find("input").Input("gpt");

		Press(cut, "ArrowDown");
		Assert.Equal(0, Highlighted(cut));
		Press(cut, "ArrowDown");
		Assert.Equal(1, Highlighted(cut));
		Press(cut, "ArrowDown");
		Assert.Equal(0, Highlighted(cut));
		Press(cut, "ArrowUp");
		Assert.Equal(1, Highlighted(cut));

		IElement highlighted = Option(cut, 1);
		Assert.Equal("true", highlighted.GetAttribute("aria-selected"));
		Assert.Equal(highlighted.Id, cut.Find("input").GetAttribute("aria-activedescendant"));
		Assert.False(Option(cut, 0).HasAttribute("aria-selected"));
	}

	[Fact]
	public void UpInAClosedBox_OpensOnTheLastSuggestion()
	{
		IRenderedComponent<MokaTextField> cut = Field();

		Press(cut, "ArrowUp");

		Assert.Equal(Models.Length - 1, Highlighted(cut));
	}

	[Fact]
	public void AltDown_OpensTheList_WithoutMovingTheHighlight()
	{
		IRenderedComponent<MokaTextField> cut = Field();

		cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });

		Assert.Equal(Models.Length, Options(cut).Count);
		Assert.Equal(-1, Highlighted(cut));
	}

	[Fact]
	public void Enter_PutsTheHighlightedSuggestionInTheBox_AndRaisesValueChangedOnce()
	{
		List<string> changes = [];
		IRenderedComponent<MokaTextField> cut = Field(p => p.Add(x => x.ValueChanged, (string value) => changes.Add(value)));
		cut.Find("input").Input("son");
		changes.Clear();

		Press(cut, "ArrowDown");
		Press(cut, "Enter");

		Assert.Equal(["claude-sonnet-5-5"], changes);
		Assert.Equal("claude-sonnet-5-5", cut.Find("input").GetAttribute("value"));
		Assert.Empty(cut.FindAll("[role=listbox]"));
		Assert.Equal("false", cut.Find("input").GetAttribute("aria-expanded"));
	}

	[Fact]
	public void EnterWithNothingHighlighted_PicksNothing()
	{
		List<string> changes = [];
		IRenderedComponent<MokaTextField> cut = Field(p => p.Add(x => x.ValueChanged, (string value) => changes.Add(value)));
		cut.Find("input").Input("gpt");
		changes.Clear();

		Press(cut, "Enter");

		Assert.Empty(changes);
		Assert.Equal("gpt", cut.Find("input").GetAttribute("value"));
	}

	[Fact]
	public void Escape_ClosesTheList_AndStopsThere_ThenLeavesTheNextEscapeAlone()
	{
		IRenderedComponent<MokaTextField> cut = Field();
		cut.Find("input").Input("gpt");
		Assert.True(cut.Find(".moka-textfield").HasAttribute("blazor:onkeydown:stopPropagation"));

		Press(cut, "Escape");

		Assert.Empty(cut.FindAll("[role=listbox]"));
		Assert.False(cut.Find(".moka-textfield").HasAttribute("blazor:onkeydown:stopPropagation"));
	}

	[Fact]
	public void Tab_ClosesTheList_WithoutPicking()
	{
		List<string> changes = [];
		IRenderedComponent<MokaTextField> cut = Field(p => p.Add(x => x.ValueChanged, (string value) => changes.Add(value)));
		cut.Find("input").Input("gpt");
		Press(cut, "ArrowDown");
		changes.Clear();

		Press(cut, "Tab");

		Assert.Empty(cut.FindAll("[role=listbox]"));
		Assert.Empty(changes);
	}

	[Fact]
	public void AClick_PicksTheSuggestion_AndRaisesValueChangedOnce()
	{
		List<string> changes = [];
		IRenderedComponent<MokaTextField> cut = Field(p => p.Add(x => x.ValueChanged, (string value) => changes.Add(value)));
		cut.Find("input").Input("llama");
		changes.Clear();

		Option(cut, 0).Click();

		Assert.Equal(["llama-4"], changes);
		Assert.Equal("llama-4", cut.Find("input").GetAttribute("value"));
		Assert.Empty(cut.FindAll("[role=listbox]"));
	}

	// A press on a row keeps focus in the box, so the keys still reach the list.
	[Fact]
	public void APressOnTheList_KeepsFocusInTheBox()
	{
		IRenderedComponent<MokaTextField> cut = Field();
		cut.Find("input").Input("gpt");

		Assert.True(cut.Find("[role=listbox]").HasAttribute("blazor:onmousedown:preventDefault"));
	}

	[Fact]
	public void AClickOutside_ClosesTheList()
	{
		IRenderedComponent<MokaTextField> cut = Field();
		cut.Find("input").Input("gpt");

		cut.Find(".moka-textfield-backdrop").Click();

		Assert.Empty(cut.FindAll("[role=listbox]"));
	}

	[Fact]
	public void AReadOnlyField_DoesNotOpen()
	{
		IRenderedComponent<MokaTextField> cut = Field(p => p.Add(x => x.ReadOnly, true));

		Press(cut, "ArrowDown");

		Assert.Empty(cut.FindAll("[role=listbox]"));
	}

	[Fact]
	public void WithoutALabel_TheListTakesTheFieldsOwnName()
	{
		IRenderedComponent<MokaTextField> cut = Render<MokaTextField>(p => p
			.Add(x => x.Suggestions, Models)
			.AddUnmatched("aria-label", "Model"));

		cut.Find("input").Input("gpt");

		IElement listbox = cut.Find("[role=listbox]");
		Assert.Equal("Model", listbox.GetAttribute("aria-label"));
		Assert.False(listbox.HasAttribute("aria-labelledby"));
	}

	[Fact]
	public void TheList_KeepsTheRowsKeyedForTheSharedHoverScript()
	{
		IRenderedComponent<MokaTextField> cut = Field();
		cut.Find("input").Input("gpt");

		Assert.Equal(["0", "1"], cut.FindAll("[role=option]").Select(option => option.GetAttribute("data-moka-index")));
		Assert.Single(_hover.Invocations["trackHover"]);
	}

	[Fact]
	public void AFieldWithoutSuggestions_MakesNoScriptCalls()
	{
		IRenderedComponent<MokaTextField> cut = Render<MokaTextField>(p => p.Add(x => x.Label, "Name"));

		cut.Find("input").Input("Ada");

		Assert.Empty(JSInterop.Invocations);
	}

	[Fact]
	public void OnTheNewDesign_TheListTakesTheThemesLook()
	{
		SetRendererInfo(new RendererInfo("Server", true));
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.Theme, MokaTheme.Red.WithLists(MokaListLook.Compact))
			.AddChildContent<MokaTextField>(field => field
				.Add(x => x.Id, "model")
				.Add(x => x.Label, "Model")
				.Add(x => x.Suggestions, Models)));

		cut.Find("input").Input("gpt");

		Assert.Contains("moka-textfield--list-compact", cut.Find(".moka-textfield").ClassList);
		Assert.NotNull(cut.Find(".moka-textfield-hints"));
	}

	[Fact]
	public void TheStylesheet_DrawsOneHighlight()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "MokaTextField.razor.css"));

		Assert.Contains(".moka-textfield-suggestions:not([data-moka-pointer]) .moka-textfield-option--highlighted", css, StringComparison.Ordinal);
		Assert.Contains(".moka-textfield-suggestions[data-moka-pointer] .moka-textfield-option:hover", css, StringComparison.Ordinal);
		Assert.DoesNotContain(".moka-textfield-option:hover,", css, StringComparison.Ordinal);
	}

	private IRenderedComponent<MokaTextField> Field(Action<ComponentParameterCollectionBuilder<MokaTextField>>? more = null) =>
		Render<MokaTextField>(p =>
		{
			p.Add(x => x.Id, "model").Add(x => x.Label, "Model").Add(x => x.Suggestions, Models);
			more?.Invoke(p);
		});

	private static void Press(IRenderedComponent<MokaTextField> cut, string key) =>
		cut.Find("input").KeyDown(new KeyboardEventArgs { Key = key });

	private static List<string> Options(IRenderedComponent<MokaTextField> cut) =>
		[.. cut.FindAll("[role=option]").Select(option => option.TextContent.Trim())];

	private static IElement Option(IRenderedComponent<MokaTextField> cut, int index) => cut.FindAll("[role=option]")[index];

	private static int Highlighted(IRenderedComponent<MokaTextField> cut)
	{
		IReadOnlyList<IElement> options = cut.FindAll("[role=option]");
		for (int i = 0; i < options.Count; i++)
		{
			if (options[i].ClassList.Contains("moka-textfield-option--highlighted"))
			{
				return i;
			}
		}

		return -1;
	}

	private static string Normalize(string markup) => Scope().Replace(Variable().Replace(markup, "$1"), "b-scope");

	[GeneratedRegex(@"(blazor:on\w+=)""\d+""|--moka-tooltip-[0-9a-f]{32}")]
	private static partial Regex Variable();

	// CSS isolation scope attributes are hashed per build, so CI writes other ones than the build the markup came from.
	[GeneratedRegex(@"(?<= )b-[a-z0-9]{10}(?=[ >/])")]
	private static partial Regex Scope();
}
