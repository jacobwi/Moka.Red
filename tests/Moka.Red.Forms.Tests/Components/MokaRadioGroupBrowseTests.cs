using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Forms.RadioGroup;

namespace Moka.Red.Forms.Tests.Components;

// Browse turns the cards into a listbox whose highlight the group's script moves. These tests cover what .NET owns: the
// markup, the script's binding, and what the script's calls do. The keys and the pointer are the script's.
public sealed class MokaRadioGroupBrowseTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Forms/RadioGroup/MokaRadioGroup.razor.js";

	private readonly BunitJSModuleInterop _module;

	public MokaRadioGroupBrowseTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
		SetRendererInfo(new RendererInfo("Server", true));
	}

	[Fact]
	public void TheCards_AreAListboxOfOptions_WithOneTabStop()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderBrowse("b");

		IElement list = cut.Find(".moka-radiogroup");
		Assert.Equal("listbox", list.GetAttribute("role"));
		Assert.Equal("Theme", list.GetAttribute("aria-label"));
		Assert.Equal("0", list.GetAttribute("tabindex"));
		Assert.Contains("moka-radiogroup--browse", list.ClassList);
		Assert.Empty(cut.FindAll("input"));

		IReadOnlyList<IElement> options = cut.FindAll("[role=option]");
		Assert.Equal(3, options.Count);
		Assert.Equal(["false", "true", "false"], options.Select(o => o.GetAttribute("aria-selected")));
		Assert.All(options, o => Assert.False(o.HasAttribute("tabindex")));
		Assert.All(options, o => Assert.Contains("moka-radio-item--option", o.ClassList));
		Assert.Equal(3, options.Select(o => o.Id).Distinct().Count(id => !string.IsNullOrEmpty(id)));
		Assert.Equal(3, options.Select(o => o.GetAttribute("data-moka-option")).Distinct().Count(key => !string.IsNullOrEmpty(key)));
	}

	// The script writes the highlight and aria-activedescendant. .NET never renders them, so a redraw can't fight it.
	[Fact]
	public void TheHighlight_IsNeverRenderedByDotNet()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderBrowse("b");

		Assert.Empty(cut.FindAll("[data-moka-highlight]"));
		Assert.False(cut.Find("[role=listbox]").HasAttribute("aria-activedescendant"));
	}

	[Fact]
	public void TheCards_KeepThePictureNameAndHint_AndTheChosenCheck()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderBrowse("b");

		IElement first = cut.FindAll("[role=option]")[0];
		Assert.Equal(["moka-radio-item__art", "moka-radio-item__head", "moka-radio-item__hint"],
			first.Children.Select(child => child.ClassList[0]));
		Assert.Equal("true", first.QuerySelector(".moka-radio-item__art")?.GetAttribute("aria-hidden"));
		IElement check = Assert.Single(cut.FindAll(".moka-radio-item__head svg"));
		Assert.Equal("true", check.Closest("[role=option]")?.GetAttribute("aria-selected"));
	}

	[Fact]
	public void AnOptionKeepsAnIdTheAppGave()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = Render<MokaRadioGroup<string>>(p => p
			.Add(x => x.Cards, true)
			.Add(x => x.Browse, true)
			.AddChildContent<MokaRadioItem<string>>(item => item.Add(x => x.Value, "a").Add(x => x.Label, "A").Add(x => x.Id, "light-card")));

		Assert.Equal("light-card", cut.Find("[role=option]").Id);
	}

	[Fact]
	public void ADisabledCard_IsADisabledOption()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderBrowse("a", disabledCard: true);

		Assert.Equal([null, null, "true"], cut.FindAll("[role=option]").Select(o => o.GetAttribute("aria-disabled")));
		Assert.Contains("moka-radio-item--disabled", cut.FindAll("[role=option]")[2].ClassList);
	}

	[Fact]
	public void ADisabledGroup_IsADisabledListbox_OutOfTheTabOrder()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderBrowse("a", disabled: true);

		IElement list = cut.Find("[role=listbox]");
		Assert.Equal("true", list.GetAttribute("aria-disabled"));
		Assert.False(list.HasAttribute("tabindex"));
		Assert.All(cut.FindAll("[role=option]"), o => Assert.Equal("true", o.GetAttribute("aria-disabled")));
	}

	[Fact]
	public void ChoosingACard_SetsTheValue()
	{
		string? chosen = null;
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderBrowse("a", changed: value => chosen = value);

		cut.InvokeAsync(() => cut.Instance.ChooseFromBrowse(KeyOf(cut, 1)));
		cut.Render(p => p.Add(x => x.Value, chosen));

		Assert.Equal("b", chosen);
		Assert.Equal(["false", "true", "false"], cut.FindAll("[role=option]").Select(o => o.GetAttribute("aria-selected")));
	}

	[Fact]
	public void ChoosingADisabledCard_OrAnUnknownKey_ChangesNothing()
	{
		string? chosen = null;
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderBrowse("a", changed: value => chosen = value, disabledCard: true);

		cut.InvokeAsync(() => cut.Instance.ChooseFromBrowse(KeyOf(cut, 2)));
		cut.InvokeAsync(() => cut.Instance.ChooseFromBrowse("gone"));

		Assert.Null(chosen);
	}

	[Fact]
	public void ChoosingInADisabledGroup_ChangesNothing()
	{
		string? chosen = null;
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderBrowse("a", changed: value => chosen = value, disabled: true);

		cut.InvokeAsync(() => cut.Instance.ChooseFromBrowse(KeyOf(cut, 1)));

		Assert.Null(chosen);
	}

	[Fact]
	public void TheHighlight_ReportsTheCardsValue_WithoutChoosingIt()
	{
		List<string> highlights = [];
		string? chosen = null;
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderBrowse("a", changed: value => chosen = value, highlight: highlights.Add);

		cut.InvokeAsync(() => cut.Instance.HighlightFromBrowse(KeyOf(cut, 2)));

		cut.WaitForAssertion(() => Assert.Equal(["c"], highlights));
		Assert.Null(chosen);
		Assert.Equal("true", cut.FindAll("[role=option]")[0].GetAttribute("aria-selected"));
	}

	// The script sends no key when the highlight goes back to the chosen card, or rests on it.
	[Fact]
	public void NoKey_ReportsTheChosenValue()
	{
		List<string> highlights = [];
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderBrowse("b", highlight: highlights.Add);

		cut.InvokeAsync(() => cut.Instance.HighlightFromBrowse(null));

		cut.WaitForAssertion(() => Assert.Equal(["b"], highlights));
	}

	[Fact]
	public void TheScript_IsBound_AndToldWhetherThePageListens()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderBrowse("a");

		JSRuntimeInvocation bind = _module.VerifyInvoke("bindBrowse");
		Assert.Equal(false, bind.Arguments[3]);

		cut.Render(p => p.Add(x => x.HighlightChanged, (string _) => { }));

		JSRuntimeInvocation report = _module.VerifyInvoke("reportBrowse");
		Assert.Equal(bind.Arguments[0], report.Arguments[0]);
		Assert.Equal(true, report.Arguments[1]);
		_module.VerifyInvoke("bindBrowse", calledTimes: 1);
	}

	[Fact]
	public async Task TheScript_IsLetGo_WhenBrowseTurnsOff_AndOnDisposal()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderBrowse("a");
		object? key = _module.VerifyInvoke("bindBrowse").Arguments[0];

		cut.Render(p => p.Add(x => x.Browse, false));

		Assert.Equal(key, _module.VerifyInvoke("unbindBrowse").Arguments[0]);
		Assert.Equal("radiogroup", cut.Find(".moka-radiogroup").GetAttribute("role"));

		cut.Render(p => p.Add(x => x.Browse, true));
		await DisposeComponentsAsync();

		_module.VerifyInvoke("unbindBrowse", calledTimes: 2);
	}

	[Fact]
	public void WithoutCards_BrowseDoesNothing()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderBrowse("a", cards: false);

		Assert.Equal("radiogroup", cut.Find(".moka-radiogroup").GetAttribute("role"));
		Assert.Equal(3, cut.FindAll("input[type=radio]").Count);
		Assert.Empty(cut.FindAll("[role=option]"));
		_module.VerifyNotInvoke("bindBrowse");
	}

	// Without interactivity nothing could move the highlight, so the cards stay radios a static form can post.
	[Fact]
	public void BeforeTheGroupIsInteractive_TheCardsStayRadios()
	{
		SetRendererInfo(new RendererInfo("Static", false));

		IRenderedComponent<MokaRadioGroup<string>> cut = RenderBrowse("b");

		IElement group = cut.Find(".moka-radiogroup");
		Assert.Equal("radiogroup", group.GetAttribute("role"));
		Assert.False(group.HasAttribute("tabindex"));
		Assert.DoesNotContain("moka-radiogroup--browse", group.ClassList);
		Assert.Equal([false, true, false], cut.FindAll("input[type=radio]").Select(r => r.HasAttribute("checked")));
		Assert.Empty(cut.FindAll("[role=option]"));
		Assert.Empty(cut.FindAll("input[type=hidden]"));
	}

	[Fact]
	public void ANamedGroup_PostsItsValueFromAHiddenField_InTheInvariantCulture()
	{
		CultureInfo culture = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo("sv-SE");
		try
		{
			IRenderedComponent<MokaRadioGroup<double>> cut = Render<MokaRadioGroup<double>>(p => p
				.AddUnmatched("name", "size")
				.Add(x => x.Cards, true)
				.Add(x => x.Browse, true)
				.Add(x => x.Value, 1.5)
				.AddChildContent<MokaRadioItem<double>>(item => item.Add(x => x.Value, 1.5).Add(x => x.Label, "Small")));

			IElement hidden = cut.Find("input[type=hidden]");
			Assert.Equal("size", hidden.GetAttribute("name"));
			Assert.Equal("1.5", hidden.GetAttribute("value"));
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
		}
	}

	[Fact]
	public void AnUnnamedGroup_WritesNoHiddenField()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderBrowse("a");

		Assert.Empty(cut.FindAll("input"));
	}

	private static string KeyOf(IRenderedComponent<MokaRadioGroup<string>> cut, int index) =>
		cut.FindAll("[role=option]")[index].GetAttribute("data-moka-option")!;

	private IRenderedComponent<MokaRadioGroup<string>> RenderBrowse(string value, Action<string>? changed = null, Action<string>? highlight = null,
		bool cards = true, bool disabled = false, bool disabledCard = false) =>
		Render<MokaRadioGroup<string>>(p =>
		{
			p.Add(x => x.Label, "Theme")
				.Add(x => x.Cards, cards)
				.Add(x => x.Browse, true)
				.Add(x => x.Disabled, disabled)
				.Add(x => x.Value, value)
				.Add(x => x.ValueChanged, changed ?? (_ => { }))
				.AddChildContent<MokaRadioItem<string>>(item => item
					.Add(x => x.Value, "a")
					.Add(x => x.Label, "Light")
					.Add(x => x.Hint, "Bright")
					.Add(x => x.Preview, (RenderFragment)(b => b.AddMarkupContent(0, "<i class=\"pic\"></i>"))))
				.AddChildContent<MokaRadioItem<string>>(item => item.Add(x => x.Value, "b").Add(x => x.Label, "Dark"))
				.AddChildContent<MokaRadioItem<string>>(item => item.Add(x => x.Value, "c").Add(x => x.Label, "Dim").Add(x => x.Disabled, disabledCard));
			if (highlight is not null)
			{
				p.Add(x => x.HighlightChanged, highlight);
			}
		});
}
