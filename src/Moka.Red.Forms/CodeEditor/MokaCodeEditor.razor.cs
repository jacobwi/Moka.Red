using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Common;

namespace Moka.Red.Forms.CodeEditor;

/// <summary>
///     A text area for code or configuration: monospace, line numbers, coloring from a highlighter the app passes,
///     and problems from a checker the app passes, underlined in place and listed below with a jump to each. Neither
///     is built in, so it suits any language. Meant for text of a few thousand lines at most: every edit colors the
///     whole text again.
/// </summary>
public partial class MokaCodeEditor
{
	private const string ModulePath = "./_content/Moka.Red.Forms/CodeEditor/MokaCodeEditor.razor.js";

	private readonly string _generatedId = $"moka-code-editor-{Guid.NewGuid():N}";
	private readonly Dictionary<int, MokaCodeSeverity> _lineSeverity = [];

	private string _text = string.Empty;
	private string[] _lines = [string.Empty];
	private int[] _lineStarts = [0];
	private MokaCodeSpan[] _spans = [];
	private List<MokaCodeDiagnostic> _diagnostics = [];
	private IReadOnlyList<MokaCodeDiagnostic> _checked = [];
	private string? _lastValue;
	private Func<string, IEnumerable<MokaCodeSpan>>? _lastHighlighter;
	private Func<string, IReadOnlyList<MokaCodeDiagnostic>>? _lastDiagnose;
	private IReadOnlyList<MokaCodeDiagnostic>? _lastDiagnostics;
	private bool _diagnosticsStale;
	private bool _bound;
	private Timer? _diagnoseTimer;
	private ElementReference _root;
	private ElementReference _textarea;
	private ElementReference _gutter;
	private ElementReference _highlight;
	private ElementReference _marks;

	/// <summary>Label text displayed above the editor.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text displayed below the editor.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Error text displayed below the editor when in error state.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>Whether the field is required.</summary>
	[Parameter]
	public bool Required { get; set; }

	/// <summary>Whether the field can be left empty. Shows a quiet word beside the label. <see cref="Required" /> wins.</summary>
	[Parameter]
	public bool Optional { get; set; }

	/// <summary>
	///     Colors the text: given the whole text, it returns the stretches to color, with indexes into it. Stretches
	///     can come in any order; overlaps and anything past the end are ignored. Null draws plain text.
	/// </summary>
	[Parameter]
	public Func<string, IEnumerable<MokaCodeSpan>>? Highlighter { get; set; }

	/// <summary>
	///     Checks the text and returns its problems. Runs when the value is set and <see cref="DiagnoseDelay" /> after
	///     typing stops. Null checks nothing.
	/// </summary>
	[Parameter]
	public Func<string, IReadOnlyList<MokaCodeDiagnostic>>? Diagnose { get; set; }

	/// <summary>
	///     Problems the app found itself, such as a save the server refused, shown with those from
	///     <see cref="Diagnose" />. They describe the text they were found in, so they hide once the text is edited and
	///     come back when the app passes a new list.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaCodeDiagnostic>? Diagnostics { get; set; }

	/// <summary>How long after the last key <see cref="Diagnose" /> runs, in milliseconds. Default 250.</summary>
	[Parameter]
	public int DiagnoseDelay { get; set; } = 250;

	/// <summary>The problems showing after <see cref="Diagnose" /> has run on what was typed.</summary>
	[Parameter]
	public EventCallback<IReadOnlyList<MokaCodeDiagnostic>> OnDiagnosed { get; set; }

	/// <summary>How many lines show before the editor scrolls. Default 12.</summary>
	[Parameter]
	public int Rows { get; set; } = 12;

	/// <summary>Whether line numbers run down the left edge. Default true.</summary>
	[Parameter]
	public bool ShowLineNumbers { get; set; } = true;

	/// <summary>Whether the problems are listed under the editor. Default true.</summary>
	[Parameter]
	public bool ShowProblems { get; set; } = true;

	/// <summary>The title of the problems list. Default "Problems".</summary>
	[Parameter]
	public string ProblemsLabel { get; set; } = "Problems";

	/// <summary>Writes a count in the problems list's head, such as "2 errors". Null writes English.</summary>
	[Parameter]
	public Func<MokaCodeSeverity, int, string>? CountText { get; set; }

	/// <summary>Writes where a problem is, such as "Ln 3, Col 5". Null writes that, and nothing for line 0.</summary>
	[Parameter]
	public Func<MokaCodeDiagnostic, string>? LocationText { get; set; }

	/// <summary>The problems showing now, errors first.</summary>
	public IReadOnlyList<MokaCodeDiagnostic> Problems => _diagnostics;

	/// <summary>Whether any problem showing is an error.</summary>
	public bool HasErrors => HasErrorDiagnostics;

	/// <inheritdoc />
	protected override string RootClass => "moka-code-editor";

	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	private string ProblemsId => $"{InputId}-problems";

	private bool HasError => !string.IsNullOrEmpty(ErrorText) || HasValidationError;

	private string? ResolvedErrorText => !string.IsNullOrEmpty(ErrorText) ? ErrorText : ValidationErrorText;

	private bool HasErrorDiagnostics => _diagnostics.Exists(static d => d.Severity == MokaCodeSeverity.Error);

	private bool ShowProblemList => ShowProblems && _diagnostics.Count > 0;

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-code-editor--error", HasError)
		.AddClass("moka-code-editor--readonly", ReadOnly)
		.AddClass("moka-code-editor--disabled", Disabled)
		.AddClass(CssClass)
		.AddClass(Class)
		.Build();

	// The field wrapper is the outermost element, so the margin goes there. The frame draws the editor's border, so it
	// takes the padding and the radius.

	/// <inheritdoc />
	protected override string? ComponentStyle => Style;

	private string? WrapperStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.Build();

	private string? FrameStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle("--_code-rows", Math.Max(1, Rows).ToString(CultureInfo.InvariantCulture))
		.Build();

	private IEnumerable<(MokaCodeSeverity Severity, int Count)> Counts =>
		_diagnostics.GroupBy(static d => d.Severity)
			.OrderByDescending(static g => g.Key)
			.Select(static g => (g.Key, g.Count()));

	private RenderFragment Highlight => BuildHighlight;

	private RenderFragment Marks => BuildMarks;

	/// <summary>Moves focus into the editor.</summary>
	public ValueTask FocusAsync() => _textarea.FocusAsync();

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		string incoming = Value ?? string.Empty;
		bool textChanged = false;
		if (!string.Equals(incoming, _lastValue, StringComparison.Ordinal))
		{
			_lastValue = incoming;
			if (!string.Equals(incoming, _text, StringComparison.Ordinal))
			{
				SetText(incoming);
				textChanged = true;
			}
		}

		if (textChanged || !ReferenceEquals(Highlighter, _lastHighlighter))
		{
			_lastHighlighter = Highlighter;
			Tokenize();
		}

		bool diagnosticsChanged = !ReferenceEquals(Diagnostics, _lastDiagnostics);
		if (diagnosticsChanged)
		{
			_lastDiagnostics = Diagnostics;
			_diagnosticsStale = false;
		}

		if (textChanged || !ReferenceEquals(Diagnose, _lastDiagnose))
		{
			_lastDiagnose = Diagnose;
			_checked = Diagnose?.Invoke(_text) ?? [];
			Merge();
		}
		else if (diagnosticsChanged)
		{
			Merge();
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (!_bound)
		{
			_bound = true;
			await SafeModuleInvokeVoidAsync(ModulePath, "bindEditor", _root, _textarea, _gutter, new[] { _highlight, _marks });
		}
	}

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
	{
		result = value ?? string.Empty;
		validationErrorMessage = string.Empty;
		return true;
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_diagnoseTimer is not null)
		{
			await _diagnoseTimer.DisposeAsync();
			_diagnoseTimer = null;
		}

		if (_bound)
		{
			_bound = false;
			await SafeModuleInvokeVoidAsync(ModulePath, "unbindEditor", _textarea);
		}

		await base.DisposeAsyncCore();
	}

	private void HandleInput(string? typed)
	{
		string text = typed ?? string.Empty;
		SetText(text);
		_lastValue = text;
		_diagnosticsStale = true;
		Tokenize();
		Merge();
		HandleInputWithDebounce(text);
		ScheduleDiagnose();
	}

	private void SetText(string text)
	{
		_text = text;
		_lines = text.Split('\n');
		_lineStarts = new int[_lines.Length];
		int offset = 0;
		for (int i = 0; i < _lines.Length; i++)
		{
			_lineStarts[i] = offset;
			offset += _lines[i].Length + 1;
		}
	}

	// Sorted, clipped to the text and with overlaps cut, so drawing them is one pass.
	private void Tokenize()
	{
		if (Highlighter is null || _text.Length == 0)
		{
			_spans = [];
			return;
		}

		List<MokaCodeSpan> spans = [];
		int position = 0;
		foreach (MokaCodeSpan span in Highlighter(_text).Where(s => s.Length > 0).OrderBy(s => s.Start))
		{
			int start = Math.Max(span.Start, position);
			int end = Math.Min(span.Start + span.Length, _text.Length);
			if (end <= start)
			{
				continue;
			}

			spans.Add(span with { Start = start, Length = end - start });
			position = end;
		}

		_spans = [.. spans];
	}

	private void ScheduleDiagnose()
	{
		if (Diagnose is null)
		{
			return;
		}

		if (DiagnoseDelay <= 0)
		{
			RunDiagnose();
			return;
		}

		_diagnoseTimer ??= new Timer(
			_ => RunDetached(async () =>
			{
				RunDiagnose();
				await OnDiagnosed.InvokeAsync(_diagnostics);
				StateHasChanged();
			}),
			null,
			Timeout.Infinite,
			Timeout.Infinite);
		_diagnoseTimer.Change(DiagnoseDelay, Timeout.Infinite);
	}

	private void RunDiagnose()
	{
		_checked = Diagnose?.Invoke(_text) ?? [];
		Merge();
	}

	private void Merge()
	{
		IEnumerable<MokaCodeDiagnostic> outside = _diagnosticsStale ? [] : Diagnostics ?? [];
		_diagnostics = [.. _checked
			.Concat(outside)
			.DistinctBy(static d => (d.Severity, d.Line, d.Column, d.Message))
			.OrderByDescending(static d => d.Severity)
			.ThenBy(static d => d.Line)
			.ThenBy(static d => d.Column)];

		_lineSeverity.Clear();
		foreach (MokaCodeDiagnostic diagnostic in _diagnostics)
		{
			int index = diagnostic.Line - 1;
			if (index < 0 || index >= _lines.Length)
			{
				continue;
			}

			if (!_lineSeverity.TryGetValue(index, out MokaCodeSeverity existing) || diagnostic.Severity > existing)
			{
				_lineSeverity[index] = diagnostic.Severity;
			}
		}
	}

	private async Task RevealAsync(MokaCodeDiagnostic diagnostic)
	{
		if (diagnostic.Line <= 0 || diagnostic.Line > _lines.Length)
		{
			await SafeFocusAsync();
			return;
		}

		(int start, int length) = Range(diagnostic);
		int offset = _lineStarts[diagnostic.Line - 1];
		await SafeModuleInvokeVoidAsync(ModulePath, "selectRange", _textarea, offset + start, offset + start + length);
	}

	private async Task SafeFocusAsync()
	{
		try
		{
			await FocusAsync();
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex) || ex is JSException or InvalidOperationException)
		{
			// The circuit ended, or the textarea left the page before the call arrived.
		}
	}

	// The columns a problem covers on its line: from its column for its length, the rest of the line when it has no
	// length, and the line's text without its indent when it has no column.
	private (int Start, int Length) Range(MokaCodeDiagnostic diagnostic)
	{
		string line = _lines[diagnostic.Line - 1];
		int trimmedEnd = line.TrimEnd().Length;
		int firstText = line.Length - line.TrimStart().Length;
		if (diagnostic.Column <= 0)
		{
			return (firstText, Math.Max(0, trimmedEnd - firstText));
		}

		int start = Math.Min(diagnostic.Column - 1, line.Length);
		int length = diagnostic.Length > 0
			? Math.Min(diagnostic.Length, line.Length - start)
			: Math.Max(0, trimmedEnd - start);
		return (start, length);
	}

	private string GutterClass(int index) => _lineSeverity.TryGetValue(index, out MokaCodeSeverity severity)
		? new CssBuilder("moka-code-editor__line-number")
			.AddClass($"moka-code-editor__line-number--{SeverityName(severity)}")
			.Build()
		: "moka-code-editor__line-number";

	private static string ProblemClass(MokaCodeDiagnostic diagnostic) => new CssBuilder("moka-code-editor__problem")
		.AddClass($"moka-code-editor__problem--{SeverityName(diagnostic.Severity)}")
		.Build();

	private static string MarkClass(MokaCodeSeverity severity) => new CssBuilder("moka-code-editor__mark")
		.AddClass($"moka-code-editor__mark--{SeverityName(severity)}")
		.Build();

	private static string CountClass(MokaCodeSeverity severity) => new CssBuilder("moka-code-editor__count")
		.AddClass($"moka-code-editor__count--{SeverityName(severity)}")
		.Build();

	private static string SeverityName(MokaCodeSeverity severity) => severity switch
	{
		MokaCodeSeverity.Error => "error",
		MokaCodeSeverity.Warning => "warning",
		_ => "info"
	};

	private string CountLabel(MokaCodeSeverity severity, int count)
	{
		if (CountText is not null)
		{
			return CountText(severity, count);
		}

		string noun = severity switch
		{
			MokaCodeSeverity.Error => count == 1 ? "error" : "errors",
			MokaCodeSeverity.Warning => count == 1 ? "warning" : "warnings",
			_ => count == 1 ? "note" : "notes"
		};

		return string.Create(CultureInfo.CurrentCulture, $"{count} {noun}");
	}

	private string? Where(MokaCodeDiagnostic diagnostic)
	{
		if (LocationText is not null)
		{
			return LocationText(diagnostic);
		}

		return diagnostic.Line > 0
			? string.Create(CultureInfo.CurrentCulture, $"Ln {diagnostic.Line}, Col {Math.Max(1, diagnostic.Column)}")
			: null;
	}

	private static string TokenClass(MokaCodeTokenKind kind) => kind switch
	{
		MokaCodeTokenKind.Heading => "moka-code-editor__token--heading",
		MokaCodeTokenKind.Keyword => "moka-code-editor__token--keyword",
		MokaCodeTokenKind.Key => "moka-code-editor__token--key",
		MokaCodeTokenKind.Value => "moka-code-editor__token--value",
		MokaCodeTokenKind.Literal => "moka-code-editor__token--literal",
		MokaCodeTokenKind.Number => "moka-code-editor__token--number",
		MokaCodeTokenKind.Constant => "moka-code-editor__token--constant",
		MokaCodeTokenKind.Comment => "moka-code-editor__token--comment",
		MokaCodeTokenKind.Punctuation => "moka-code-editor__token--punctuation",
		_ => "moka-code-editor__token"
	};

	// The trailing space keeps a last empty line as tall as the textarea draws it.
	private void BuildHighlight(RenderTreeBuilder builder)
	{
		int position = 0;
		foreach (MokaCodeSpan span in _spans)
		{
			if (span.Start > position)
			{
				builder.AddContent(0, _text[position..span.Start]);
			}

			if (span.Kind == MokaCodeTokenKind.Plain)
			{
				builder.AddContent(1, _text.Substring(span.Start, span.Length));
			}
			else
			{
				builder.OpenElement(2, "span");
				builder.AddAttribute(3, "class", TokenClass(span.Kind));
				builder.AddContent(4, _text.Substring(span.Start, span.Length));
				builder.CloseElement();
			}

			position = span.Start + span.Length;
		}

		if (position < _text.Length)
		{
			builder.AddContent(5, _text[position..]);
		}

		builder.AddContent(6, "\n ");
	}

	private void BuildMarks(RenderTreeBuilder builder)
	{
		for (int i = 0; i < _lines.Length; i++)
		{
			string line = _lines[i];
			int number = i + 1;
			int position = 0;
			foreach ((int start, int length, MokaCodeSeverity severity) in _diagnostics
				         .Where(d => d.Line == number)
				         .Select(d => (Range(d), d.Severity))
				         .Select(r => (r.Item1.Start, r.Item1.Length, r.Severity))
				         .OrderBy(r => r.Start))
			{
				int from = Math.Max(start, position);
				int to = Math.Max(from, start + length);
				if (from > position)
				{
					builder.AddContent(10, line[position..from]);
				}

				builder.OpenElement(11, "span");
				builder.AddAttribute(12, "class", MarkClass(severity));
				builder.AddContent(13, to > from ? line[from..to] : "  ");
				builder.CloseElement();
				position = Math.Max(position, to);
			}

			if (position < line.Length)
			{
				builder.AddContent(14, line[position..]);
			}

			builder.AddContent(15, "\n");
		}

		builder.AddContent(16, " ");
	}
	private string? DescribedBy => MokaAttributes.DescribedBy(AdditionalAttributes,
		ShowProblemList ? ProblemsId : null, MokaFieldWrapper.HelperIdIfShown(InputId, HelperText, ResolvedErrorText));

	private IReadOnlyDictionary<string, object>? ControlAttributes => MokaAttributes.WithoutDescribedBy(AdditionalAttributes);
}
