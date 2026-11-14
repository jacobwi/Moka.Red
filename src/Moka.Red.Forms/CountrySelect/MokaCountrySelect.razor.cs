using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Forms.Common;
using Moka.Red.Primitives.Flag;

namespace Moka.Red.Forms.CountrySelect;

/// <summary>
///     Picks a country by its flag and name and binds its ISO 3166-1 code, such as "SE". A
///     <see cref="SelectField.MokaSelect{TValue}" /> with <see cref="MokaCountries" /> and its templates already set,
///     so search finds names, codes, dialing codes and other names, or a grid of flag tiles. Binds like any input
///     (<c>@bind-Value</c>, <c>EditForm</c>, a static page's post), or a list of codes with <see cref="Multiple" />.
/// </summary>
/// <remarks>
///     On <see cref="MokaVisualInputBase{TValue}" /> rather than <c>MokaSelectBase</c>: it holds a select rather than
///     being one, and that base's <c>Items</c> and <c>ItemTemplate</c> would mean nothing here.
/// </remarks>
public partial class MokaCountrySelect : MokaVisualInputBase<string?>
{
	private readonly string _generatedId = $"moka-country-{Guid.NewGuid():N}";
	private readonly Dictionary<string, string?> _tags = new(StringComparer.Ordinal);
	private string[] _codes = [];
	private MokaCountry[] _countries = [];
	private Dictionary<string, string?> _groups = new(StringComparer.Ordinal);
	private string? _builtFrom;
	private bool _bound;
	private string? _chosen;
	private string? _lastValue;
	private IList<string>? _chosenList;
	private IList<string>? _lastSelectedValues;
	private bool _open;
	private string? _browserCountry;
	private bool _askedBrowser;
	private string _tagsQuery = string.Empty;

	private ElementReference _gridRoot;
	private ElementReference _gridSearch;
	private ElementReference _grid;
	private ElementReference _ruler;
	private DotNetObjectReference<MokaCountrySelect>? _dotNetRef;
	private bool _gridBound;
	private string _gridQuery = string.Empty;
	private int _gridColumns = 6;

	[CascadingParameter]
	private MokaTheme? CascadedTheme { get; set; }

	/// <summary>The shape. Default <see cref="MokaCountrySelectShape.Dropdown" />.</summary>
	[Parameter]
	public MokaCountrySelectShape Shape { get; set; }

	/// <summary>
	///     What each row of the list shows. Null shows the dialing code in <see cref="MokaCountrySelectShape.Prefix" />
	///     and the two-letter code everywhere else.
	/// </summary>
	[Parameter]
	public MokaCountryRowContent? RowContent { get; set; }

	/// <summary>
	///     The list's order. Default <see cref="MokaCountryOrder.SuggestedFirst" />, which is A to Z until there is a
	///     suggestion to show. The grid has no headings, so it always lists A to Z.
	/// </summary>
	[Parameter]
	public MokaCountryOrder Order { get; set; } = MokaCountryOrder.SuggestedFirst;

	/// <summary>Codes listed first under <see cref="SuggestedText" /> when <see cref="Order" /> says so.</summary>
	[Parameter]
	public IReadOnlyList<string>? Suggested { get; set; }

	/// <summary>
	///     Whether the browser's own country goes first among the suggestions, read from its language
	///     (<c>navigator.language</c>) once the page is interactive. Default true.
	/// </summary>
	[Parameter]
	public bool SuggestBrowserCountry { get; set; } = true;

	/// <summary>Limits the list to these codes. Null lists every country in <see cref="MokaCountries" />.</summary>
	[Parameter]
	public IReadOnlyList<string>? Countries { get; set; }

	/// <summary>
	///     The name shown for each country, for an app that shows names in another language. Search still finds the
	///     English name. Null shows <see cref="MokaCountry.Name" />.
	/// </summary>
	[Parameter]
	public Func<MokaCountry, string>? Names { get; set; }

	/// <summary>
	///     Whether search also finds the two-letter code, the dialing code ("44" or "+44"), other names ("Holland", "UK")
	///     and the own name, best match first, with a tag saying why a row matched when it wasn't the name. Off, search
	///     matches names only. Accents are set aside either way. Default true.
	/// </summary>
	[Parameter]
	public bool ExtendedSearch { get; set; } = true;

	/// <summary>
	///     The tag a search draws on a row that matched something other than its name. Null writes "code SE",
	///     "dialing +46", "also Holland" or the own name. Return null for no tag.
	/// </summary>
	[Parameter]
	public Func<MokaCountryMatch, string?>? MatchText { get; set; }

	/// <summary>
	///     Whether several countries can be picked, shown as chips with their flags. Binds
	///     <see cref="SelectedValues" />. Applies to <see cref="MokaCountrySelectShape.Dropdown" /> and
	///     <see cref="MokaCountrySelectShape.Grid" />.
	/// </summary>
	[Parameter]
	public bool Multiple { get; set; }

	/// <summary>The codes picked when <see cref="Multiple" /> is on.</summary>
	[Parameter]
	public IList<string>? SelectedValues { get; set; }

	/// <summary>Raised with the new list of codes when <see cref="Multiple" /> is on.</summary>
	[Parameter]
	public EventCallback<IList<string>> SelectedValuesChanged { get; set; }

	/// <summary>Whether the closed field keeps the flag beside the name. Off, it shows the name only. Default true.</summary>
	[Parameter]
	public bool FlagInField { get; set; } = true;

	/// <summary>
	///     Draws every flag the select shows, in place of <see cref="MokaFlag" />: in the rows, the field, the chips, the
	///     flag button, the prefix and the grid's tiles, for an app with flag pictures of its own. A picture (an
	///     <c>img</c> or <c>svg</c>) written straight into it is sized to the text as the flag is, and its own class
	///     outranks that. The flags stay hidden from screen readers, so give a picture an empty <c>alt</c>. The flag
	///     button's globe for no country is kept. Null draws <see cref="MokaFlag" />.
	/// </summary>
	[Parameter]
	public RenderFragment<MokaCountry>? FlagTemplate { get; set; }

	/// <summary>
	///     Draws each row of the list in place of the flag, the name and <see cref="RowContent" />'s extra, as
	///     <see cref="Base.MokaSelectBase{TValue}.ItemTemplate" /> does on a select. The select still draws the highlight, the check
	///     and a search's tag. Names come from <see cref="Names" /> only where the template uses them. The grid's tiles
	///     keep their own look; <see cref="FlagTemplate" /> reaches them. Null keeps the default row.
	/// </summary>
	[Parameter]
	public RenderFragment<MokaCountry>? ItemTemplate { get; set; }

	/// <summary>
	///     Draws the chosen country in the closed field, as <see cref="SelectField.MokaSelect{TValue}.ValueTemplate" />
	///     does, in place of the flag and the name: the flag alone in <see cref="MokaCountrySelectShape.FlagButton" />, and
	///     the flag and the dialing code in <see cref="MokaCountrySelectShape.Prefix" />. Wins over
	///     <see cref="FlagInField" />. Chips under <see cref="Multiple" /> keep their look, and the button shapes keep
	///     their accessible name. Null keeps the default.
	/// </summary>
	[Parameter]
	public RenderFragment<MokaCountry>? ValueTemplate { get; set; }

	/// <summary>
	///     Whether typing on the closed field picks the first country starting with what was typed, and Up and Down
	///     step through the list, as a native select does. Alt and Down opens it. Default true.
	/// </summary>
	[Parameter]
	public bool TypeToJump { get; set; } = true;

	/// <summary>
	///     The label above the field. In <see cref="MokaCountrySelectShape.FlagButton" /> and
	///     <see cref="MokaCountrySelectShape.Prefix" /> it is the button's name and tooltip instead, and defaults to
	///     "Country" and "Country code".
	/// </summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text under the field.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>
	///     Content beside the field's box, in its row, such as a button that acts on the value. The label stays above
	///     and the helper text below, and on the new design a button here is as tall as the box.
	/// </summary>
	[Parameter]
	public RenderFragment? Actions { get; set; }

	/// <summary>Error text under the field. Without it, the <c>EditForm</c>'s message for this field shows.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>Whether a country has to be picked.</summary>
	[Parameter]
	public bool Required { get; set; }

	/// <summary>Whether the field can be left empty, shown as a quiet word beside the label.</summary>
	[Parameter]
	public bool Optional { get; set; }

	/// <summary>The closed field's text while nothing is picked. Default "Choose a country".</summary>
	[Parameter]
	public string Placeholder { get; set; } = "Choose a country";

	/// <summary>Whether the choice can be cleared from the field.</summary>
	[Parameter]
	public bool Clearable { get; set; }

	/// <summary>
	///     How the list's rows are drawn, as on <see cref="SelectField.MokaSelect{TValue}" />. Null takes the theme's,
	///     except that a <see cref="Multiple" /> list on the theme's standard rows starts each with a check box.
	/// </summary>
	[Parameter]
	public MokaListLook? Look { get; set; }

	/// <summary>
	///     The search box's placeholder. Null is "Search countries", or "Country, code or +46" in
	///     <see cref="MokaCountrySelectShape.Prefix" />.
	/// </summary>
	[Parameter]
	public string? SearchPlaceholder { get; set; }

	/// <summary>What the list says when a search finds nothing. Default "No country matches".</summary>
	[Parameter]
	public string NoResultsText { get; set; } = "No country matches";

	/// <summary>The heading over the suggestions. Default "Suggested".</summary>
	[Parameter]
	public string SuggestedText { get; set; } = "Suggested";

	/// <summary>The heading over everyone else when suggestions show and the list isn't by region. Default "All countries".</summary>
	[Parameter]
	public string AllCountriesText { get; set; } = "All countries";

	/// <summary>The continents' headings under <see cref="MokaCountryOrder.ByRegion" />. Null uses English names.</summary>
	[Parameter]
	public Func<MokaWorldRegion, string>? RegionText { get; set; }

	/// <summary>
	///     The grid's height in <see cref="MokaCountrySelectShape.Grid" />, any CSS length. Null is about seven rows of
	///     tiles.
	/// </summary>
	[Parameter]
	public string? GridHeight { get; set; }

	/// <summary>Where the button's tooltip opens in the shapes that show one. Default Bottom.</summary>
	[Parameter]
	public MokaTooltipPosition TooltipPosition { get; set; } = MokaTooltipPosition.Bottom;

	/// <inheritdoc />
	protected override string RootClass => "moka-country-select";

	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	private string GridId => $"{InputId}-grid";

	private MokaCountryRowContent Rows => RowContent ??
	                                      (Shape == MokaCountrySelectShape.Prefix
		                                      ? MokaCountryRowContent.NameAndDialingCode
		                                      : MokaCountryRowContent.NameAndCode);

	private bool IsButton => Shape is MokaCountrySelectShape.FlagButton or MokaCountrySelectShape.Prefix;

	private bool SuggestsFirst => Order is MokaCountryOrder.SuggestedFirst or MokaCountryOrder.SuggestedThenByRegion;

	private bool ListMultiple => Multiple && Shape == MokaCountrySelectShape.Dropdown;

	private string ShownSearchPlaceholder => SearchPlaceholder ??
	                                         (Shape == MokaCountrySelectShape.Prefix ? "Country, code or +46" : "Search countries");

	private string ButtonName => Label ?? (Shape == MokaCountrySelectShape.Prefix ? "Country code" : "Country");

	private MokaCountry? ChosenCountry => MokaCountries.Find(_chosen);

	private string? ButtonValueText => ChosenCountry is { } country
		? Shape == MokaCountrySelectShape.Prefix && country.DialingCode is { } dial ? $"{ShownName(country)} {dial}" : ShownName(country)
		: null;

	private string ButtonTooltip => ButtonValueText is { } text
		? Shape == MokaCountrySelectShape.Prefix ? text : $"{ButtonName}: {text}"
		: ButtonName;

	private string ButtonAccessibleName => ButtonValueText is { } text ? $"{ButtonName}, {text}" : ButtonName;

	private bool HasError => !string.IsNullOrEmpty(ErrorText) || HasValidationError;

	private string? ResolvedErrorText => !string.IsNullOrEmpty(ErrorText) ? ErrorText : ValidationErrorText;

	private MokaListLook? ListLook =>
		Look ?? (Multiple && CascadedTheme is { Look: MokaLook.Gate, Lists: MokaListLook.Standard } ? MokaListLook.Radio : null);

	private string RootCssClass => new CssBuilder(RootClass)
		.AddClass($"moka-country-select--{MokaEnumHelpers.ToCssClass(Shape)}")
		.AddClass("moka-country-select--two-line", Rows == MokaCountryRowContent.OwnName)
		.AddClass(Shape == MokaCountrySelectShape.Grid ? CssClass : null)
		.AddClass(Class)
		.Build();

	private string? RootStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle(Style)
		.Build();

	private string? GridStyle => new StyleBuilder()
		.AddStyle("--_cols", _gridColumns.ToString(CultureInfo.InvariantCulture))
		.AddStyle("height", GridHeight)
		.Build();

	private string? GridPanelStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private string GridCssClass => new CssBuilder("moka-country-grid")
		.AddClass("moka-country-grid--error", HasError)
		.AddClass("moka-country-grid--disabled", Disabled)
		.Build();

	// Attributes MokaSelect takes through splatting: its ValueExpression only when this one was bound, since a
	// null one throws and a made-up one would post a field nobody asked for, and the name for the button shapes.
	private IReadOnlyDictionary<string, object> SelectAttributes
	{
		get
		{
			Dictionary<string, object> attributes = AdditionalAttributes is null
				? new Dictionary<string, object>(StringComparer.Ordinal)
				: new Dictionary<string, object>(AdditionalAttributes, StringComparer.Ordinal);
			if (_bound && ValueExpression is not null)
			{
				attributes["ValueExpression"] = ValueExpression;
			}

			if (IsButton)
			{
				attributes["aria-label"] = ButtonAccessibleName;
			}

			return attributes;
		}
	}

	/// <inheritdoc />
	public override Task SetParametersAsync(ParameterView parameters)
	{
		_bound = parameters.TryGetValue<Expression<Func<string?>>>(nameof(ValueExpression), out var expression) && expression is not null;
		return base.SetParametersAsync(parameters);
	}

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (!string.Equals(Value, _lastValue, StringComparison.Ordinal))
		{
			_lastValue = Value;
			_chosen = Value;
		}

		if (!ReferenceEquals(SelectedValues, _lastSelectedValues))
		{
			_lastSelectedValues = SelectedValues;
			_chosenList = SelectedValues;
		}

		Rebuild();
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		if (!_askedBrowser && SuggestBrowserCountry && SuggestsFirst && Shape != MokaCountrySelectShape.Grid)
		{
			_askedBrowser = true;
			string? region = await AskBrowserCountryAsync();
			if (MokaCountries.Find(region) is { } country)
			{
				_browserCountry = country.Code;
				Rebuild();
				StateHasChanged();
			}
		}

		bool wantsGrid = Shape == MokaCountrySelectShape.Grid && !Disabled;
		if (wantsGrid != _gridBound)
		{
			_gridBound = wantsGrid;
			if (wantsGrid)
			{
				_dotNetRef ??= DotNetObjectReference.Create(this);
				await SafeModuleInvokeVoidAsync(ModulePath, "bindGrid", _gridRoot, _gridSearch, _grid, _ruler, _dotNetRef);
			}
			else
			{
				await SafeModuleInvokeVoidAsync(ModulePath, "unbindGrid", _grid);
			}
		}
	}

	/// <summary>Called by the grid's script when a tile is clicked or picked with Enter or Space.</summary>
	/// <param name="code">The tile's country code.</param>
	[JSInvokable]
	public async Task PickFromGrid(string code)
	{
		if (Disabled || MokaCountries.Find(code) is not { } country)
		{
			return;
		}

		if (Multiple)
		{
			List<string> next = _chosenList is null ? [] : [.. _chosenList];
			int at = next.FindIndex(c => string.Equals(c, country.Code, StringComparison.OrdinalIgnoreCase));
			if (at >= 0)
			{
				next.RemoveAt(at);
			}
			else
			{
				next.Add(country.Code);
			}

			_chosenList = next;
			await SelectedValuesChanged.InvokeAsync(next);
		}
		else
		{
			_chosen = country.Code;
			CurrentValue = country.Code;
		}

		StateHasChanged();
	}

	/// <summary>Called by the grid's script when the number of tiles that fit in a row changes.</summary>
	/// <param name="columns">Tiles in a row.</param>
	[JSInvokable]
	public void SetGridColumns(int columns)
	{
		int next = Math.Clamp(columns, 1, 24);
		if (next != _gridColumns)
		{
			_gridColumns = next;
			StateHasChanged();
		}
	}

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out string? result, out string validationErrorMessage)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			result = null;
			validationErrorMessage = string.Empty;
			return true;
		}

		if (MokaCountries.Find(value) is { } country)
		{
			result = country.Code;
			validationErrorMessage = string.Empty;
			return true;
		}

		result = null;
		validationErrorMessage = $"'{value}' is not a country code.";
		return false;
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_gridBound)
		{
			_gridBound = false;
			await SafeModuleInvokeVoidAsync(ModulePath, "unbindGrid", _grid);
		}

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}

	private const string ModulePath = "./_content/Moka.Red.Forms/CountrySelect/MokaCountrySelect.razor.js";

	private async Task<string?> AskBrowserCountryAsync()
	{
		try
		{
			IJSObjectReference module = await GetJsModuleAsync(ModulePath);
			return await module.InvokeAsync<string?>("browserCountry");
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
			return null;
		}
		catch (JSException)
		{
			return null;
		}
	}

	private string ShownName(MokaCountry country) => Names?.Invoke(country) ?? country.Name;

	private string ShownName(string code) => MokaCountries.Find(code) is { } country ? ShownName(country) : code;

	private string Folded(MokaCountry country) => MokaTextFold.Fold(ShownName(country));

	// The list's codes in display order, and each one's heading, built again only when what they come from changes.
	private void Rebuild()
	{
		string key = string.Join("|",
			Countries is null ? "*" : string.Join(",", Countries),
			Suggested is null ? "" : string.Join(",", Suggested),
			_browserCountry,
			((int)Order).ToString(CultureInfo.InvariantCulture),
			Names?.GetHashCode().ToString(CultureInfo.InvariantCulture),
			RegionText?.GetHashCode().ToString(CultureInfo.InvariantCulture),
			SuggestedText,
			AllCountriesText);
		if (string.Equals(key, _builtFrom, StringComparison.Ordinal))
		{
			return;
		}

		_builtFrom = key;
		IEnumerable<MokaCountry> allowed = Countries is null
			? MokaCountries.All
			: Countries.Select(MokaCountries.Find).OfType<MokaCountry>().DistinctBy(c => c.Code);
		_countries = [.. allowed.OrderBy(Folded, StringComparer.Ordinal)];
		HashSet<string> allowedCodes = [.. _countries.Select(c => c.Code)];

		List<string> suggested = [];
		if (SuggestsFirst)
		{
			IEnumerable<string?> wanted = new[] { _browserCountry }.Concat(Suggested ?? []);
			foreach (string? code in wanted)
			{
				if (MokaCountries.Find(code) is { } country && allowedCodes.Contains(country.Code) && !suggested.Contains(country.Code))
				{
					suggested.Add(country.Code);
				}
			}
		}

		bool byRegion = Order is MokaCountryOrder.ByRegion or MokaCountryOrder.SuggestedThenByRegion;
		IEnumerable<MokaCountry> rest = _countries.Where(c => !suggested.Contains(c.Code));
		if (byRegion)
		{
			rest = rest.OrderBy(c => c.Region);
		}

		_codes = [.. suggested, .. rest.Select(c => c.Code)];
		_groups = new Dictionary<string, string?>(StringComparer.Ordinal);
		foreach (string code in suggested)
		{
			_groups[code] = SuggestedText;
		}

		foreach (MokaCountry country in rest)
		{
			_groups[country.Code] = byRegion ? RegionName(country.Region) : suggested.Count > 0 ? AllCountriesText : null;
		}
	}

	private string RegionName(MokaWorldRegion region) => RegionText?.Invoke(region) ?? MokaCountries.RegionName(region);

	private Func<string, string?>? GroupOf =>
		_groups.Values.Any(group => group is not null) ? code => _groups.GetValueOrDefault(code) : null;

	private IEnumerable<string> SearchCountries(string query) => Matches(query).Select(match => match.Country.Code);

	// MokaSelect asks for the results and then for each row's tag in the same render, so one search serves both.
	private IReadOnlyList<MokaCountryMatch> Matches(string query)
	{
		IReadOnlyList<MokaCountryMatch> matches = MokaCountries.Search(query, _countries, Names);
		_tags.Clear();
		_tagsQuery = query;
		foreach (MokaCountryMatch match in matches)
		{
			_tags[match.Country.Code] = TagText(match);
		}

		return matches;
	}

	private string? TagFor(string code, string query)
	{
		if (!string.Equals(query, _tagsQuery, StringComparison.Ordinal))
		{
			_ = Matches(query);
		}

		return _tags.GetValueOrDefault(code);
	}

	private string? TagText(MokaCountryMatch match)
	{
		if (MatchText is not null)
		{
			return MatchText(match);
		}

		// The row already ends with the dialing code, so saying so again is noise.
		if (match.Kind == MokaCountryMatchKind.DialingCode && Rows == MokaCountryRowContent.NameAndDialingCode)
		{
			return null;
		}

		return match.Kind switch
		{
			MokaCountryMatchKind.Code => $"code {match.Matched}",
			MokaCountryMatchKind.DialingCode => $"dialing {match.Matched}",
			MokaCountryMatchKind.OtherName => $"also {match.Matched}",
			MokaCountryMatchKind.NativeName => match.Matched,
			_ => null
		};
	}

	private Task OnSelectValueChanged(string? value)
	{
		_chosen = value;
		return ValueChanged.InvokeAsync(value);
	}

	private Task OnSelectValuesChanged(IList<string> values)
	{
		_chosenList = values;
		return SelectedValuesChanged.InvokeAsync(values);
	}

	private void OnOpenChanged(bool open) => _open = open;

	// A to Z, since the grid has no headings to set suggestions or continents apart, or the grid's own search: names
	// only, or everything ExtendedSearch covers, best first.
	private IReadOnlyList<MokaCountry> GridCountries
	{
		get
		{
			if (string.IsNullOrWhiteSpace(_gridQuery))
			{
				return _countries;
			}

			if (ExtendedSearch)
			{
				return [.. MokaCountries.Search(_gridQuery, _countries, Names).Select(match => match.Country)];
			}

			string query = MokaTextFold.Fold(_gridQuery);
			return [.. _countries.Where(c => Folded(c).Contains(query, StringComparison.Ordinal))];
		}
	}

	private bool IsChosen(string code) => Multiple
		? _chosenList?.Any(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase)) == true
		: string.Equals(_chosen, code, StringComparison.OrdinalIgnoreCase);

	private void SetGridQuery(string? query) => _gridQuery = query ?? string.Empty;

	private string TileId(string code) => $"{InputId}-tile-{code}";
	private string? DescribedBy => MokaAttributes.DescribedBy(AdditionalAttributes,
		MokaFieldWrapper.HelperIdIfShown(GridId, HelperText, ResolvedErrorText));

	private IReadOnlyDictionary<string, object>? ControlAttributes => MokaAttributes.WithoutDescribedBy(AdditionalAttributes);
}
