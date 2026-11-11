using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Forms.SelectField;

namespace Moka.Red.Forms.Tests.Components;

/// <summary>
///     What MokaSelect took on for the country select, each off by default: search that matches more than the shown
///     text, folds accents, ranks its results and says why a row matched; a placeholder drawn as markup; Backspace
///     taking a chip off; and a closed select that changes its choice from the keyboard as a native one does.
/// </summary>
public sealed class MokaSelectSearchAndJumpTests : BunitContext
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	private static readonly string[] Places = ["Åland", "Albania", "Côte d'Ivoire", "Croatia", "Norway", "Nepal", "New Zealand"];

	private static readonly Dictionary<string, string> Codes = new(StringComparer.Ordinal)
	{
		["Åland"] = "AX", ["Albania"] = "AL", ["Côte d'Ivoire"] = "CI", ["Croatia"] = "HR",
		["Norway"] = "NO", ["Nepal"] = "NP", ["New Zealand"] = "NZ"
	};

	private readonly ManualClock _clock = new();

	public MokaSelectSearchAndJumpTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton<TimeProvider>(_clock);
	}

	private IRenderedComponent<MokaSelect<string>> Open(Action<ComponentParameterCollectionBuilder<MokaSelect<string>>> extra)
	{
		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p =>
		{
			p.Add(x => x.Items, Places).Add(x => x.Searchable, true);
			extra(p);
		});
		cut.Find("[role=combobox]").Click();
		return cut;
	}

	private static string[] Shown(IRenderedComponent<MokaSelect<string>> cut) =>
		[.. cut.FindAll("[role=option] .moka-select-option-content").Select(o => o.TextContent.Trim())];

	[Fact]
	public void ByDefault_SearchMatchesTheShownText_AndKeepsAccents()
	{
		IRenderedComponent<MokaSelect<string>> cut = Open(_ => { });

		cut.Find(".moka-select-search").Input("cote");

		Assert.Empty(Shown(cut));
	}

	[Fact]
	public void SearchText_MatchesWhatItReturns()
	{
		IRenderedComponent<MokaSelect<string>> cut = Open(p => p.Add(x => x.SearchText, place => $"{place} {Codes[place]}"));

		cut.Find(".moka-select-search").Input("hr");

		Assert.Equal(["Croatia"], Shown(cut));
	}

	[Fact]
	public void IgnoreAccents_FindsAccentedText()
	{
		IRenderedComponent<MokaSelect<string>> cut = Open(p => p.Add(x => x.IgnoreAccents, true));

		cut.Find(".moka-select-search").Input("cote d'iv");
		Assert.Equal(["Côte d'Ivoire"], Shown(cut));

		cut.Find(".moka-select-search").Input("ALAND");
		Assert.Equal(["Åland", "New Zealand"], Shown(cut));
	}

	[Fact]
	public void SearchItems_DecidesTheResultsAndTheirOrder_WithoutGroupHeadings()
	{
		List<string> asked = [];
		IRenderedComponent<MokaSelect<string>> cut = Open(p => p
			.Add(x => x.GroupBy, place => place.StartsWith('N') ? "N" : "Other")
			.Add(x => x.SearchItems, query =>
			{
				asked.Add(query);
				return ["New Zealand", "Norway"];
			}));
		Assert.NotEmpty(cut.FindAll(".moka-select-group-header"));

		cut.Find(".moka-select-search").Input("  n  ");

		Assert.Equal(["New Zealand", "Norway"], Shown(cut));
		Assert.Empty(cut.FindAll(".moka-select-group-header"));
		Assert.All(asked, query => Assert.Equal("n", query));
	}

	[Fact]
	public void SearchTag_ShowsOnlyWhileSearching()
	{
		IRenderedComponent<MokaSelect<string>> cut = Open(p => p
			.Add(x => x.SearchText, place => $"{place} {Codes[place]}")
			.Add(x => x.SearchTag, (place, query) => Codes[place].Equals(query, StringComparison.OrdinalIgnoreCase) ? $"code {Codes[place]}" : null));
		Assert.Empty(cut.FindAll(".moka-select-option-tag"));

		cut.Find(".moka-select-search").Input("no");

		IElement tag = Assert.Single(cut.FindAll(".moka-select-option-tag"));
		Assert.Equal("code NO", tag.TextContent);
		Assert.Contains("Norway", tag.ParentElement!.TextContent, StringComparison.Ordinal);

		cut.Find(".moka-select-search").Input("");
		Assert.Empty(cut.FindAll(".moka-select-option-tag"));
	}

	[Fact]
	public void SearchPlaceholderAndLabel_NameTheSearchBox()
	{
		IRenderedComponent<MokaSelect<string>> cut = Open(p => p
			.Add(x => x.SearchPlaceholder, "Search places")
			.Add(x => x.SearchLabel, "Search the places"));

		IElement search = cut.Find(".moka-select-search");
		Assert.Equal("Search places", search.GetAttribute("placeholder"));
		Assert.Equal("Search the places", search.GetAttribute("aria-label"));
	}

	[Fact]
	public void SearchBox_KeepsItsWords_ByDefault()
	{
		IRenderedComponent<MokaSelect<string>> cut = Open(_ => { });

		IElement search = cut.Find(".moka-select-search");
		Assert.Equal("Search...", search.GetAttribute("placeholder"));
		Assert.Equal("Search options", search.GetAttribute("aria-label"));
	}

	[Fact]
	public void PlaceholderTemplate_DrawsTheEmptyField_AndGivesWayToAChoice()
	{
		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p => p
			.Add(x => x.Items, Places)
			.Add(x => x.Placeholder, "Pick a place")
			.Add(x => x.PlaceholderTemplate, builder => builder.AddMarkupContent(0, "<i class=\"globe\"></i>")));

		Assert.Single(cut.FindAll("[role=combobox] i.globe"));
		Assert.Equal("Pick a place", cut.Find("[role=combobox]").GetAttribute("aria-label"));

		cut.Render(p => p.Add(x => x.Value, "Norway"));
		Assert.Empty(cut.FindAll("[role=combobox] i.globe"));
	}

	[Fact]
	public async Task BackspaceRemovesLast_TakesTheLastChipOff_OnlyFromAnEmptySearch()
	{
		IList<string>? reported = null;
		IRenderedComponent<MokaSelect<string>> cut = Open(p => p
			.Add(x => x.Multiple, true)
			.Add(x => x.BackspaceRemovesLast, true)
			.Add(x => x.SelectedValues, new List<string> { "Norway", "Nepal" })
			.Add(x => x.SelectedValuesChanged, values => reported = values));

		await cut.Find(".moka-select-search").InputAsync(new ChangeEventArgs { Value = "n" });
		await cut.Find(".moka-select-search").KeyDownAsync(new KeyboardEventArgs { Key = "Backspace" });
		Assert.Null(reported);

		await cut.Find(".moka-select-search").InputAsync(new ChangeEventArgs { Value = "" });
		await cut.Find(".moka-select-search").KeyDownAsync(new KeyboardEventArgs { Key = "Backspace" });
		Assert.Equal(["Norway"], reported);
	}

	[Fact]
	public async Task Backspace_TakesNothingOff_ByDefault()
	{
		IList<string>? reported = null;
		IRenderedComponent<MokaSelect<string>> cut = Open(p => p
			.Add(x => x.Multiple, true)
			.Add(x => x.SelectedValues, new List<string> { "Norway" })
			.Add(x => x.SelectedValuesChanged, values => reported = values));

		await cut.Find(".moka-select-search").KeyDownAsync(new KeyboardEventArgs { Key = "Backspace" });

		Assert.Null(reported);
	}

	private IRenderedComponent<MokaSelect<string>> Jumping(string? value, Action<string?> report, bool typeToJump = true) =>
		Render<MokaSelect<string>>(p => p
			.Add(x => x.Items, Places)
			.Add(x => x.Value, value!)
			.Add(x => x.ValueChanged, EventCallback.Factory.Create<string>(this, v => report(v)))
			.Add(x => x.TypeToJump, typeToJump));

	private static Task Key(IRenderedComponent<MokaSelect<string>> cut, string key, bool alt = false) =>
		cut.Find("[role=combobox]").KeyDownAsync(new KeyboardEventArgs { Key = key, AltKey = alt });

	[Fact]
	public async Task TypeToJump_TypingPicksTheFirstItemStartingWithIt_AccentsAside()
	{
		string? value = null;
		IRenderedComponent<MokaSelect<string>> cut = Jumping(null, v => value = v);

		await Key(cut, "c");
		Assert.Equal("Côte d'Ivoire", value);

		await Key(cut, "r");
		Assert.Equal("Croatia", value);
		Assert.Equal("false", cut.Find("[role=combobox]").GetAttribute("aria-expanded"));
	}

	[Fact]
	public async Task TypeToJump_TheSameLetterAgain_MovesOnAndWrapsRound()
	{
		string? value = null;
		IRenderedComponent<MokaSelect<string>> cut = Jumping(null, v => value = v);

		await Key(cut, "n");
		Assert.Equal("Norway", value);
		await Key(cut, "n");
		Assert.Equal("Nepal", value);
		await Key(cut, "n");
		Assert.Equal("New Zealand", value);
		await Key(cut, "n");
		Assert.Equal("Norway", value);
	}

	[Fact]
	public async Task TypeToJump_APause_StartsTheTypingOver()
	{
		string? value = null;
		IRenderedComponent<MokaSelect<string>> cut = Jumping(null, v => value = v);

		await Key(cut, "a");
		Assert.Equal("Åland", value);
		_clock.Advance(TimeSpan.FromSeconds(2));
		await Key(cut, "n");

		Assert.Equal("Norway", value);
	}

	[Fact]
	public async Task TypeToJump_UpDownHomeAndEnd_StepThroughTheItems()
	{
		string? value = null;
		IRenderedComponent<MokaSelect<string>> cut = Jumping("Croatia", v => value = v);

		await Key(cut, "ArrowDown");
		Assert.Equal("Norway", value);
		await Key(cut, "ArrowUp");
		Assert.Equal("Croatia", value);
		await Key(cut, "End");
		Assert.Equal("New Zealand", value);
		await Key(cut, "Home");
		Assert.Equal("Åland", value);
		Assert.Equal("false", cut.Find("[role=combobox]").GetAttribute("aria-expanded"));
	}

	[Fact]
	public async Task TypeToJump_AltAndDownOpensTheList()
	{
		string? value = null;
		IRenderedComponent<MokaSelect<string>> cut = Jumping("Croatia", v => value = v);

		await Key(cut, "ArrowDown", alt: true);

		Assert.Equal("true", cut.Find("[role=combobox]").GetAttribute("aria-expanded"));
		Assert.Null(value);
	}

	[Fact]
	public async Task TypeToJump_SpaceStillOpens_UnlessItFallsInsideAName()
	{
		string? value = null;
		IRenderedComponent<MokaSelect<string>> cut = Jumping(null, v => value = v);

		await Key(cut, "n");
		await Key(cut, "e");
		await Key(cut, "w");
		await Key(cut, " ");
		await Key(cut, "z");
		Assert.Equal("New Zealand", value);
		Assert.Equal("false", cut.Find("[role=combobox]").GetAttribute("aria-expanded"));

		_clock.Advance(TimeSpan.FromSeconds(2));
		await Key(cut, " ");
		Assert.Equal("true", cut.Find("[role=combobox]").GetAttribute("aria-expanded"));
	}

	[Fact]
	public async Task TypeToJump_SkipsDisabledItems_AndAnnouncesTheChoice()
	{
		string? value = null;
		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p => p
			.Add(x => x.Items, Places)
			.Add(x => x.ValueChanged, EventCallback.Factory.Create<string>(this, v => value = v))
			.Add(x => x.IsOptionDisabled, place => place == "Norway")
			.Add(x => x.TypeToJump, true));

		await Key(cut, "n");

		Assert.Equal("Nepal", value);
		Assert.Equal("Nepal", cut.Find("[role=status].moka-visually-hidden").TextContent);
	}

	[Fact]
	public async Task WithoutTypeToJump_DownOpensTheList_AndLettersDoNothing()
	{
		string? value = null;
		IRenderedComponent<MokaSelect<string>> cut = Jumping("Croatia", v => value = v, typeToJump: false);

		await Key(cut, "n");
		Assert.Null(value);
		Assert.Empty(cut.FindAll("[role=status].moka-visually-hidden"));

		await Key(cut, "ArrowDown");
		Assert.Equal("true", cut.Find("[role=combobox]").GetAttribute("aria-expanded"));
		Assert.Null(value);
	}

	[Fact]
	public void TypeToJump_AlsoCancelsHomeAndEnd_SoThePageStaysPut()
	{
		BunitJSModuleInterop keys = JSInterop.SetupModule(KeysModule);

		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p => p
			.Add(x => x.Items, Places)
			.Add(x => x.TypeToJump, true));

		Dictionary<string, object?>[] rules = Assert.IsType<Dictionary<string, object?>[]>(keys.VerifyInvoke("preventKeys").Arguments[1]);
		string[] trigger = (string[])rules.Single(r => (string?)r["selector"] == ".moka-select-trigger")["keys"]!;
		Assert.Contains("Home", trigger);
		Assert.Contains("End", trigger);

		cut.Render(p => p.Add(x => x.TypeToJump, false));
		Dictionary<string, object?>[] plain = Assert.IsType<Dictionary<string, object?>[]>(keys.Invocations["preventKeys"][^1].Arguments[1]);
		Assert.DoesNotContain("Home", (string[])plain.Single(r => (string?)r["selector"] == ".moka-select-trigger")["keys"]!);
	}

	[Fact]
	public async Task TypeToJump_DoesNothingInAMultipleSelect()
	{
		IList<string>? reported = null;
		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p => p
			.Add(x => x.Items, Places)
			.Add(x => x.Multiple, true)
			.Add(x => x.SelectedValuesChanged, values => reported = values)
			.Add(x => x.TypeToJump, true));

		await Key(cut, "n");

		Assert.Null(reported);
	}

	private sealed class ManualClock : TimeProvider
	{
		private long _now = 1_000_000;

		public override long TimestampFrequency => 1000;

		public override long GetTimestamp() => _now;

		public void Advance(TimeSpan by) => _now += (long)by.TotalMilliseconds;
	}
}
