using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Theming;
using Moka.Red.DevApp.Components.Library;

namespace Moka.Red.DevApp.Components.Pages;

public sealed partial class Tokens : IAsyncDisposable
{
	private const string TableView = "table";
	private const string PurposeView = "purpose";
	private const string CompareView = "compare";
	private const string AllKinds = "all";
	private const string ComparePresets = "presets";
	private const string CompareModes = "modes";
	private const string OriginalFamily = "Original";

	private static readonly IReadOnlyList<string> PresetNames = [.. LibraryCatalog.Presets.Select(preset => preset.Name)];

	// A family is a dark preset and its light partner. Dark and Light are the original design's pair.
	private static readonly IReadOnlyList<(string Name, MokaTheme Dark, MokaTheme Light)> Families =
	[
		.. LibraryCatalog.Presets
			.Where(preset => preset.Theme.IsDark)
			.Select(preset => (preset, light: LibraryCatalog.Presets.FirstOrDefault(other =>
				string.Equals(other.Name, preset.Name == "Dark" ? "Light" : $"{preset.Name}Light", StringComparison.Ordinal))))
			.Where(pair => pair.light is not null)
			.Select(pair => (pair.preset.Name == "Dark" ? OriginalFamily : pair.preset.Name, pair.preset.Theme, pair.light!.Theme))
			.OrderBy(family => family.Item1 == OriginalFamily ? 1 : 0)
	];

	private static readonly IReadOnlyList<string> FamilyNames = [.. Families.Select(family => family.Name)];

	private static readonly string[] PaneTokens =
	[
		"--moka-color-primary", "--moka-color-background", "--moka-color-surface", "--moka-color-outline",
		"--moka-radius-md", "--moka-font-family", "--moka-shadow-popup"
	];

	private static readonly IReadOnlyList<Purpose> Purposes =
	[
		new("Surfaces", "--moka-color-", "--moka-color-*",
		[
			new(null, "color", Colors("background", "surface", "surface-variant", "surface-raised", "surface-hover", "surface-2",
				"surface-3", "neutral-bg", "paper")),
			new("Lines", "line", Colors("outline", "outline-variant", "outline-strong", "outline-hover"))
		]),
		new("Text", "--moka-color-", "--moka-color-on-*",
		[
			new(null, "ink", Colors("on-background", "on-surface", "on-surface-2", "on-surface-variant", "on-surface-tertiary",
				"on-surface-quaternary", "icon"))
		]),
		new("Accent", "--moka-color-", "--moka-color-primary-*",
		[
			new(null, "color", Colors("primary", "primary-light", "primary-dark", "primary-bg", "primary-row", "primary-pill",
				"primary-text", "primary-glow", "primary-border")),
			new("Rings", "shadow", ["--moka-focus-ring", "--moka-selected-glow", "--moka-selected-bar", "--moka-current-shadow"])
		]),
		new("Meanings", "--moka-color-", "success, warning, error, info",
		[
			.. new[] { "success", "warning", "error", "info" }.Select(meaning => new Strip(
				char.ToUpperInvariant(meaning[0]) + meaning[1..], "color",
				Colors(meaning, $"{meaning}-bg", $"{meaning}-ink", $"{meaning}-text", $"{meaning}-line", $"{meaning}-dim")))
		]),
		new("Shadows", "--moka-shadow-", "--moka-shadow-*",
		[
			new(null, "shadow", Named("--moka-shadow-", "subtle", "1", "2", "3", "4", "popup", "popup-lg", "modal", "knob"))
		]),
		new("Radii", "--moka-radius-", "--moka-radius-*",
		[
			new(null, "radius", Named("--moka-radius-", "none", "sm", "md", "lg", "xl", "card", "popup", "full"))
		]),
		new("Spacing", "--moka-spacing-", "--moka-spacing-*",
		[
			new(null, "bar", Named("--moka-spacing-", "xxs", "xs", "sm", "md", "lg", "xl", "xxl")),
			new("Layout", "bar", Named("--moka-", "page-gap", "card-gap", "grid-gap", "card-padding", "panel-padding",
				"inner-padding", "item-padding", "control-height"))
		])
	];

	private ElementReference _page;
	private IJSObjectReference? _module;
	private MokaTheme? _theme;
	private bool _readPending;
	private IReadOnlyList<TokenInfo> _tokens = [];
	private Dictionary<string, TokenInfo> _byName = new(StringComparer.Ordinal);
	private Dictionary<string, string> _resolved = new(StringComparer.Ordinal);
	private IReadOnlyList<TokenRow> _rows = [];
	private string _view = TableView;
	private string _filter = string.Empty;
	private string _kind = AllKinds;
	private string _compare = ComparePresets;
	private string _left = "Red";
	private string _right = "Gate";
	private string _family = "Red";
	private string _host = "nas-01";
	private bool _nightly = true;

	[Inject]
	private IJSRuntime JS { get; set; } = default!;

	[Inject]
	private NavigationManager Navigation { get; set; } = default!;

	[CascadingParameter]
	private MokaTheme? Theme { get; set; }

	[SupplyParameterFromQuery(Name = "view")]
	private string? ViewQuery { get; set; }

	private string Said
	{
		get
		{
			string? preset = LibraryCatalog.PresetName(_theme);
			string basis = preset is null ? string.Empty : $", built on MokaTheme.{preset}";
			return $"{_tokens.Count} tokens, with their values in the current theme{basis}.";
		}
	}

	private IEnumerable<TokenKind> KindsPresent => TokenCatalog.Kinds.Where(kind => KindCount(kind) > 0);

	private IEnumerable<(string Name, MokaTheme Theme)> ComparedThemes
	{
		get
		{
			if (_compare == CompareModes)
			{
				(string Name, MokaTheme Dark, MokaTheme Light) family =
					Families.FirstOrDefault(f => f.Name == _family) is { Name: not null } found ? found : Families[0];
				yield return ($"{family.Name}, dark", family.Dark);
				yield return ($"{family.Name}, light", family.Light);
				yield break;
			}

			yield return (_left, PresetTheme(_left));
			yield return (_right == _left ? $"{_right} again" : _right, PresetTheme(_right));
		}
	}

	private string CompareNote => _compare == CompareModes
		? "The same preset in both modes. Every color token changes, and sizes stay put."
		: "Same markup on both sides, each in its own MokaThemeProvider. What changes is what the two themes write.";

	protected override void OnParametersSet()
	{
		_view = ViewQuery switch
		{
			PurposeView => PurposeView,
			CompareView => CompareView,
			_ => TableView
		};

		MokaTheme theme = Theme ?? MokaTheme.Default;
		if (!ReferenceEquals(theme, _theme))
		{
			_theme = theme;
			_tokens = TokenCatalog.For(theme);
			_byName = _tokens.ToDictionary(token => token.Name, StringComparer.Ordinal);
			_resolved = new Dictionary<string, string>(StringComparer.Ordinal);
			_readPending = true;
			Filter();
		}
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			_module = await JS.InvokeAsync<IJSObjectReference>("import", "./Components/Pages/Tokens.razor.js");
		}

		if (!_readPending || _module is null)
		{
			return;
		}

		_readPending = false;
		string[] names = [.. _tokens.Select(token => token.Name)];
		string[]? values = await ReadAsync(names);
		if (values is null || values.Length != names.Length)
		{
			return;
		}

		var resolved = new Dictionary<string, string>(StringComparer.Ordinal);
		for (int i = 0; i < names.Length; i++)
		{
			if (values[i].Length > 0)
			{
				resolved[names[i]] = values[i];
			}
		}

		_resolved = resolved;
		Filter();
		StateHasChanged();
	}

	public async ValueTask DisposeAsync() => await _module.SafeDisposeAsync();

	// Every value comes back in one call: one round trip per theme rather than one per token.
	private async Task<string[]?> ReadAsync(string[] names)
	{
		try
		{
			return await _module!.InvokeAsync<string[]>("readTokens", _page, names);
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
			return null;
		}
	}

	private void PickView(string view) =>
		Navigation.NavigateTo(Navigation.GetUriWithQueryParameter("view", view == TableView ? null : view), replace: true);

	private void PickCompare(string compare) => _compare = compare;

	private void SetFilter(string filter)
	{
		_filter = filter;
		Filter();
	}

	private void SetKind(string kind)
	{
		_kind = kind;
		Filter();
	}

	private void Filter()
	{
		string[] words = _filter.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		_rows =
		[
			.. _tokens
				.Where(token => _kind == AllKinds || KindValue(token.Kind) == _kind)
				.Select(token => new TokenRow(token, ValueOf(token)))
				.Where(row => words.All(word =>
					row.Token.Name.Contains(word, StringComparison.OrdinalIgnoreCase)
					|| row.Value.Contains(word, StringComparison.OrdinalIgnoreCase)
					|| row.Token.Written.Contains(word, StringComparison.OrdinalIgnoreCase)
					|| row.KindLabel.Contains(word, StringComparison.OrdinalIgnoreCase)))
		];
	}

	private string ValueOf(TokenInfo token) =>
		_resolved.TryGetValue(token.Name, out string? value) ? value : token.Written;

	private int KindCount(TokenKind kind) => _tokens.Count(token => token.Kind == kind);

	private static string KindValue(TokenKind kind) => kind.ToString();

	private IReadOnlyList<TokenInfo> Present(IReadOnlyList<string> names) =>
		[.. names.Select(name => _byName.GetValueOrDefault(name)).OfType<TokenInfo>()];

	private static string Trim(string shortName, string prefix)
	{
		string bare = prefix.StartsWith("--moka-", StringComparison.Ordinal) ? prefix[7..] : prefix;
		return shortName.StartsWith(bare, StringComparison.Ordinal) && shortName.Length > bare.Length
			? shortName[bare.Length..]
			: shortName;
	}

	private static string SampleText(TokenRow row) => row.Sample switch
	{
		"family" or "size" or "weight" => "Ag",
		"case" or "tracking" => "Aa",
		"text" when row.Token.Kind == TokenKind.ZIndex => "z",
		_ => string.Empty
	};

	private static MokaTheme PresetTheme(string name) =>
		LibraryCatalog.Presets.FirstOrDefault(preset => preset.Name == name)?.Theme ?? MokaTheme.Default;

	private static IEnumerable<(string Token, string Value)> PaneValues(MokaTheme theme)
	{
		Dictionary<string, string> written = TokenCatalog.Declarations(theme.ToCssVariables())
			.GroupBy(declaration => declaration.Name, StringComparer.Ordinal)
			.ToDictionary(group => group.Key, group => group.First().Value, StringComparer.Ordinal);
		return PaneTokens
			.Where(written.ContainsKey)
			.Select(name => (name[7..], written[name]));
	}

	private static string[] Colors(params string[] names) => Named("--moka-color-", names);

	private static string[] Named(string prefix, params string[] names) => [.. names.Select(name => prefix + name)];

	private sealed record TokenRow(TokenInfo Token, string Value)
	{
		public string Sample { get; } = TokenCatalog.SampleOf(Token, Value);

		public string KindLabel => TokenCatalog.Label(Token.Kind);

		public string Source => Token.FromTheme ? "theme" : "moka.css";
	}

	private sealed record Strip(string? Label, string Sample, IReadOnlyList<string> Names);

	private sealed record Purpose(string Title, string Prefix, string Hint, IReadOnlyList<Strip> Strips)
	{
		public string Id => $"tk-{Title}";
	}
}
