using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Moka.Red.Forms.Common;
using Moka.Red.Forms.NativeSelect;
using Moka.Red.Forms.NumericField;
using Moka.Red.Forms.PasswordField;
using Moka.Red.Forms.SelectField;
using Moka.Red.Forms.TextArea;
using Moka.Red.Forms.TextField;
using Moka.Red.Icons;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// Optional, Mono, Suffix and a leading icon on the fields, from what MokaGate built for itself. MokaTheme.Dark and Light
// have to keep rendering as they did, so each of these only adds something when it is set.
public class MokaFieldAdditionsTests : BunitContext
{
	private const string WrapperCss = "MokaFieldWrapper.razor.css";

	private static readonly string[] Names = ["Ada"];

	private static readonly Dictionary<string, (Type Component, string Control, (string, object?)[] Parameters)> Fields = new()
	{
		["TextField"] = (typeof(MokaTextField), "input", []),
		["NumericField"] = (typeof(MokaNumericField<int>), "input", []),
		["PasswordField"] = (typeof(MokaPasswordField), "input", []),
		["TextArea"] = (typeof(MokaTextArea), "textarea", []),
		["Select"] = (typeof(MokaSelect<string>), "[role=combobox]", [("Items", Names)]),
		["NativeSelect"] = (typeof(MokaNativeSelect<string>), "select", [("Items", Names)])
	};

	public MokaFieldAdditionsTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	public static TheoryData<string> OptionalFields => [.. Fields.Keys];

	public static TheoryData<string> MonoFields => ["TextField", "NumericField", "TextArea"];

	public static TheoryData<string> SuffixFields => ["TextField", "NumericField"];

	[Fact]
	public void Wrapper_Optional_PutsTheWordInsideTheLabel()
	{
		IRenderedComponent<MokaFieldWrapper> cut = Render<MokaFieldWrapper>(p => p
			.Add(x => x.Label, "Name")
			.Add(x => x.Optional, true));

		IElement word = cut.Find("label > .moka-field-optional");
		Assert.Equal("Optional", word.TextContent);
		Assert.False(word.HasAttribute("aria-hidden"));
	}

	[Fact]
	public void Wrapper_OptionalText_ReplacesTheWord()
	{
		IRenderedComponent<MokaFieldWrapper> cut = Render<MokaFieldWrapper>(p => p
			.Add(x => x.Label, "Name")
			.Add(x => x.Optional, true)
			.Add(x => x.OptionalText, "If you like"));

		Assert.Equal("If you like", cut.Find(".moka-field-optional").TextContent);
	}

	[Fact]
	public void Wrapper_Required_WinsOverOptional()
	{
		IRenderedComponent<MokaFieldWrapper> cut = Render<MokaFieldWrapper>(p => p
			.Add(x => x.Label, "Name")
			.Add(x => x.Required, true)
			.Add(x => x.Optional, true));

		Assert.Single(cut.FindAll(".moka-field-required"));
		Assert.Empty(cut.FindAll(".moka-field-optional"));
	}

	[Fact]
	public void Wrapper_ShowsNoWord_ByDefault()
	{
		IRenderedComponent<MokaFieldWrapper> cut = Render<MokaFieldWrapper>(p => p.Add(x => x.Label, "Name"));

		IElement label = cut.Find("label");
		Assert.Equal("Name", label.TextContent);
		Assert.Empty(label.Children);
	}

	[Fact]
	public void OptionalWord_IsQuiet()
	{
		IReadOnlyDictionary<string, string> rule = ScopedCss.Declarations(WrapperCss, ".moka-field-optional");

		Assert.Equal("var(--moka-font-weight-normal)", rule["font-weight"]);
		Assert.Equal("var(--moka-color-on-surface-variant)", rule["color"]);
	}

	[Theory]
	[MemberData(nameof(OptionalFields))]
	public void Field_Optional_ReachesTheLabel(string name)
	{
		IRenderedComponent<ContainerFragment> cut = RenderField(name, ("Label", "Name"), ("Optional", true));

		Assert.Equal("Optional", cut.Find("label > .moka-field-optional").TextContent);
	}

	[Theory]
	[MemberData(nameof(OptionalFields))]
	public void Field_Optional_IsOffByDefault(string name)
	{
		IRenderedComponent<ContainerFragment> cut = RenderField(name, ("Label", "Name"));

		Assert.Empty(cut.FindAll(".moka-field-optional"));
	}

	[Theory]
	[MemberData(nameof(OptionalFields))]
	public void Field_Required_WinsOverOptional(string name)
	{
		IRenderedComponent<ContainerFragment> cut = RenderField(name, ("Label", "Name"), ("Required", true), ("Optional", true));

		Assert.Single(cut.FindAll(".moka-field-required"));
		Assert.Empty(cut.FindAll(".moka-field-optional"));
	}

	[Theory]
	[MemberData(nameof(MonoFields))]
	public void Mono_MarksTheControl(string name)
	{
		IRenderedComponent<ContainerFragment> cut = RenderField(name, ("Mono", true));

		Assert.Contains("moka-field-input--mono", cut.Find(Fields[name].Control).ClassList);
	}

	[Theory]
	[MemberData(nameof(MonoFields))]
	public void Mono_IsOffByDefault(string name)
	{
		IRenderedComponent<ContainerFragment> cut = RenderField(name);

		IElement control = cut.Find(Fields[name].Control);
		Assert.DoesNotContain("moka-field-input--mono", control.ClassList);
		Assert.False(control.HasAttribute("spellcheck"));
	}

	[Theory]
	[InlineData("TextField")]
	[InlineData("TextArea")]
	public void Mono_TurnsSpellCheckOff(string name)
	{
		IRenderedComponent<ContainerFragment> cut = RenderField(name, ("Mono", true));

		Assert.Equal("false", cut.Find(Fields[name].Control).GetAttribute("spellcheck"));
	}

	// The Gate size rules set the font size at the specificity of three classes, and this rule has to beat them.
	[Fact]
	public void Mono_ReadsTheMonoTokens_AboveTheGateSizeRules()
	{
		IReadOnlyDictionary<string, string> rule =
			ScopedCss.Declarations(WrapperCss, ".moka-field .moka-field-input-wrapper ::deep .moka-field-input--mono");

		Assert.Equal("var(--moka-font-family-mono)", rule["font-family"]);
		Assert.Equal("var(--moka-font-size-base)", rule["font-size"]);
	}

	[Theory]
	[MemberData(nameof(SuffixFields))]
	public void Suffix_IsDrawnInTheBox_AndDescribesTheInput(string name)
	{
		IRenderedComponent<ContainerFragment> cut = RenderField(name, ("Id", "speed"), ("Suffix", "Mbit/s"));

		IElement suffix = cut.Find(".moka-field-suffix");
		IElement input = cut.Find("input");
		Assert.Equal("Mbit/s", suffix.TextContent);
		Assert.Equal("speed-suffix", suffix.Id);
		Assert.Equal("speed-suffix", input.GetAttribute("aria-describedby"));
		Assert.Contains("moka-field-input--has-suffix", input.ClassList);
	}

	[Theory]
	[MemberData(nameof(SuffixFields))]
	public void Suffix_TellsTheInputHowMuchRoomToKeep(string name)
	{
		IRenderedComponent<ContainerFragment> cut = RenderField(name, ("Suffix", "Mbit/s"));

		Assert.Equal("--moka-field-suffix-ch: 6ch", cut.Find("input").GetAttribute("style"));
	}

	[Theory]
	[MemberData(nameof(SuffixFields))]
	public void Suffix_AddsToAnAriaDescribedByTheConsumerPassed(string name)
	{
		IRenderedComponent<ContainerFragment> cut =
			RenderField(name, ("Id", "speed"), ("Suffix", "Mbit/s"), ("aria-describedby", "speed-note"));

		Assert.Equal("speed-note speed-suffix", cut.Find("input").GetAttribute("aria-describedby"));
	}

	[Theory]
	[MemberData(nameof(SuffixFields))]
	public void Suffix_IsOffByDefault_AndLeavesTheConsumersAttributeAlone(string name)
	{
		IRenderedComponent<ContainerFragment> cut = RenderField(name, ("aria-describedby", "speed-note"));

		IElement input = cut.Find("input");
		Assert.Empty(cut.FindAll(".moka-field-suffix"));
		Assert.Equal("speed-note", input.GetAttribute("aria-describedby"));
		Assert.False(input.HasAttribute("style"));
		Assert.DoesNotContain("moka-field-input--has-suffix", input.ClassList);
	}

	[Theory]
	[MemberData(nameof(SuffixFields))]
	public void Suffix_Empty_DrawsNothing(string name)
	{
		IRenderedComponent<ContainerFragment> cut = RenderField(name, ("Suffix", ""));

		Assert.Empty(cut.FindAll(".moka-field-suffix"));
		Assert.False(cut.Find("input").HasAttribute("aria-describedby"));
	}

	[Fact]
	public void Suffix_StaysOutOfThePointersWay_TwelvePixelsFromTheEdge()
	{
		IReadOnlyDictionary<string, string> rule =
			ScopedCss.Declarations(WrapperCss, ".moka-field-input-wrapper ::deep .moka-field-suffix");

		Assert.Equal("none", rule["pointer-events"]);
		Assert.Equal("absolute", rule["position"]);
		Assert.Equal("var(--moka-field-suffix-end, 0.75rem)", rule["right"]);
		Assert.Equal("var(--moka-font-size-sm)", rule["font-size"]);
		Assert.Equal("var(--moka-color-on-surface-variant)", rule["color"]);
	}

	[Fact]
	public void Suffix_InputPadding_MakesRoomForIt()
	{
		IReadOnlyDictionary<string, string> rule =
			ScopedCss.Declarations(WrapperCss, ".moka-field .moka-field-input-wrapper ::deep .moka-field-input--has-suffix");

		Assert.Contains("var(--moka-field-suffix-ch, 4ch)", rule["padding-right"], StringComparison.Ordinal);
		Assert.Contains("var(--moka-field-suffix-end, 0.75rem)", rule["padding-right"], StringComparison.Ordinal);
	}

	[Fact]
	public void Suffix_MovesInsideAnEndIcon()
	{
		IRenderedComponent<MokaTextField> cut = Render<MokaTextField>(p => p
			.Add(x => x.Suffix, "min")
			.Add(x => x.EndIcon, MokaIcons.Action.Save));

		Assert.Contains("moka-textfield--suffix-inset", cut.Find(".moka-textfield").ClassList);
		Assert.Single(cut.FindAll(".moka-textfield-end-icon"));
		Assert.Single(cut.FindAll(".moka-field-suffix"));
		Assert.Equal("2.25rem", ScopedCss.Declarations("MokaTextField.razor.css", ".moka-textfield--suffix-inset")["--moka-field-suffix-end"]);
	}

	[Fact]
	public void Suffix_MovesInsideTheClearButton()
	{
		IRenderedComponent<MokaTextField> cut = Render<MokaTextField>(p => p
			.Add(x => x.Value, "5")
			.Add(x => x.Suffix, "min")
			.Add(x => x.Clearable, true));

		Assert.Contains("moka-textfield--suffix-inset", cut.Find(".moka-textfield").ClassList);
		Assert.Single(cut.FindAll(".moka-textfield-clear"));
	}

	[Fact]
	public void Suffix_StaysAtTheEdge_WithNothingElseThere()
	{
		IRenderedComponent<MokaTextField> cut = Render<MokaTextField>(p => p.Add(x => x.Suffix, "min"));

		Assert.DoesNotContain("moka-textfield--suffix-inset", cut.Find(".moka-textfield").ClassList);
	}

	[Fact]
	public void NumericField_StartIcon_SitsBeforeTheInput()
	{
		IRenderedComponent<MokaNumericField<int>> cut = Render<MokaNumericField<int>>(p => p.Add(x => x.StartIcon, MokaIcons.Action.Save));

		IElement icon = cut.Find(".moka-numericfield > .moka-field-start-icon");
		Assert.NotNull(icon.QuerySelector("svg"));
		Assert.Equal("INPUT", icon.NextElementSibling?.TagName);
		Assert.Contains("moka-field-input--has-start-icon", cut.Find("input").ClassList);
	}

	[Fact]
	public void NumericField_StartIcon_IsOffByDefault()
	{
		IRenderedComponent<MokaNumericField<int>> cut = Render<MokaNumericField<int>>();

		Assert.Empty(cut.FindAll(".moka-field-start-icon"));
		Assert.DoesNotContain("moka-field-input--has-start-icon", cut.Find("input").ClassList);
	}

	[Fact]
	public void StartIcon_SitsTwelvePixelsIn_WithTheInputPaddedPastIt()
	{
		IReadOnlyDictionary<string, string> icon =
			ScopedCss.Declarations(WrapperCss, ".moka-field-input-wrapper ::deep .moka-field-start-icon");
		IReadOnlyDictionary<string, string> input =
			ScopedCss.Declarations(WrapperCss, ".moka-field .moka-field-input-wrapper ::deep .moka-field-input--has-start-icon");

		Assert.Equal("0.75rem", icon["left"]);
		Assert.Equal("none", icon["pointer-events"]);
		Assert.Equal("2.375rem", input["padding-left"]);
	}

	[Fact]
	public void NumericField_TakesEveryAdditionTogether()
	{
		IRenderedComponent<MokaNumericField<int>> cut = Render<MokaNumericField<int>>(p => p
			.Add(x => x.Id, "n")
			.Add(x => x.Label, "Speed")
			.Add(x => x.Optional, true)
			.Add(x => x.Mono, true)
			.Add(x => x.Suffix, "Mbit/s")
			.Add(x => x.StartIcon, MokaIcons.Action.Save)
			.Add(x => x.Class, "mine"));

		Assert.Equal(
			"moka-numericfield-input moka-numericfield-input--md moka-field-input--has-start-icon moka-field-input--has-suffix moka-field-input--mono",
			cut.Find("input").GetAttribute("class"));
		Assert.EndsWith(" mine", " " + cut.Find(".moka-numericfield").GetAttribute("class"), StringComparison.Ordinal);
	}

	public static TheoryData<string, string> DefaultControls => new()
	{
		{
			"TextField",
			"class=moka-textfield-input moka-textfield-input--md type=text blazor:onfocus blazor:onblur id=f value=v blazor:oninput"
		},
		{
			"NumericField",
			"class=moka-numericfield-input moka-numericfield-input--md type=text inputmode=numeric blazor:onfocus blazor:onblur id=f value=3 blazor:oninput"
		},
		{
			"PasswordField",
			"class=moka-passwordfield-input moka-passwordfield-input--md moka-passwordfield-input--has-end-icon type=password blazor:onfocus blazor:onblur id=f value=v blazor:oninput"
		},
		{
			"TextArea",
			"class=moka-textarea-input moka-textarea-input--md rows=3 id=f value=v blazor:oninput"
		}
	};

	// Recorded from the components before any of these parameters existed. The value and its input listener come
	// last because the control is bound with @bind:get and @bind:set, which Razor writes after the other attributes.
	[Theory]
	[MemberData(nameof(DefaultControls))]
	public void DefaultMarkup_OfTheControl_IsUnchanged(string name, string expected)
	{
		object? value = name == "NumericField" ? 3 : "v";
		IRenderedComponent<ContainerFragment> cut = RenderField(name, ("Id", "f"), ("Label", "Name"), ("Value", value));

		Assert.Equal(expected, AttributesOf(cut.Find(Fields[name].Control)));
		Assert.Equal("class=moka-field-label for=f id=f-label", AttributesOf(cut.Find("label")));
		Assert.Equal("Name", cut.Find("label").InnerHtml);
	}

	private IRenderedComponent<ContainerFragment> RenderField(string name, params (string Name, object? Value)[] parameters)
	{
		(Type component, string _, (string, object?)[] required) = Fields[name];
		RenderFragment fragment = builder =>
		{
			builder.OpenComponent(0, component);
			int sequence = 1;
			foreach ((string parameter, object? value) in required.Concat(parameters))
			{
				builder.AddAttribute(sequence++, parameter, value);
			}

			builder.CloseComponent();
		};

		return Render(fragment);
	}

	// Scope attributes and event handler numbers change without the markup meaning anything else.
	private static string AttributesOf(IElement element) => string.Join(" ", element.Attributes
		.Where(attribute => !attribute.Name.StartsWith("b-", StringComparison.Ordinal))
		.Select(attribute => attribute.Name.StartsWith("blazor:", StringComparison.Ordinal)
			? attribute.Name
			: $"{attribute.Name}={attribute.Value}"));
}
