using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Code;
using Moka.Red.Core.Theming;
using Moka.Red.ThemeGen.Serialization;

namespace Moka.Red.ThemeGen.ImportExport;

/// <summary>
///     A theme as C#, JSON or CSS (<see cref="MokaThemeSerializer" />), each in a highlighted code block with a copy button,
///     and a field to import a theme from JSON. Only the format on show is written, and only when the theme or the format
///     changes.
/// </summary>
public partial class MokaThemeImportExport : ComponentBase
{
	private MokaThemeExportFormat _format;
	private MokaThemeExportFormat? _lastFormat;
	private (MokaTheme Theme, MokaThemeExportFormat Format)? _exportFor;
	private string? _importError;
	private string? _importJson;

	/// <summary>The theme to export.</summary>
	[Parameter]
	public MokaTheme Theme { get; set; } = MokaTheme.Default;

	/// <summary>The format on show. Two-way bindable. Default <see cref="MokaThemeExportFormat.CSharp" />.</summary>
	[Parameter]
	public MokaThemeExportFormat Format { get; set; }

	/// <summary>Raised when the person picks another format.</summary>
	[Parameter]
	public EventCallback<MokaThemeExportFormat> FormatChanged { get; set; }

	/// <summary>Raised with the theme read from the pasted JSON when the person applies it.</summary>
	[Parameter]
	public EventCallback<MokaTheme> OnImport { get; set; }

	/// <summary>Raised with the theme as JSON when the person presses Export. The button shows only while this has a listener.</summary>
	[Parameter]
	public EventCallback<string> OnExport { get; set; }

	/// <summary>Whether the import field shows. Default true.</summary>
	[Parameter]
	public bool ShowImport { get; set; } = true;

	/// <summary>How tall the code block grows before it scrolls, as a CSS length. Null lets it grow.</summary>
	[Parameter]
	public string? MaxHeight { get; set; }

	/// <summary>Every word the panel writes.</summary>
	[Parameter]
	public MokaThemeImportExportText Text { get; set; } = new();

	private string ExportOutput { get; set; } = "";

	private string FormatValue => _format.ToString();

	private string Language => _format switch
	{
		MokaThemeExportFormat.Json => "json",
		MokaThemeExportFormat.Css => "css",
		_ => "csharp"
	};

	private Func<string, IEnumerable<MokaCodeSpan>> Highlighter => _format switch
	{
		MokaThemeExportFormat.Json => MokaCodeHighlighters.Json,
		MokaThemeExportFormat.Css => MokaCodeHighlighters.Css,
		_ => MokaCodeHighlighters.CSharp
	};

	private string Hint => _format switch
	{
		MokaThemeExportFormat.Json => Text.JsonHint,
		MokaThemeExportFormat.Css => Text.CssHint,
		_ => Text.CSharpHint
	};

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		if (_lastFormat != Format)
		{
			_lastFormat = Format;
			_format = Format;
		}

		UpdateExport();
	}

	private void UpdateExport()
	{
		if (_exportFor is { } done && ReferenceEquals(done.Theme, Theme) && done.Format == _format)
		{
			return;
		}

		_exportFor = (Theme, _format);
		ExportOutput = _format switch
		{
			MokaThemeExportFormat.Json => MokaThemeSerializer.ToJson(Theme),
			MokaThemeExportFormat.Css => MokaThemeSerializer.ToCss(Theme),
			_ => MokaThemeSerializer.ToCSharp(Theme)
		};
	}

	private Task SetFormatAsync(string value)
	{
		if (!Enum.TryParse(value, out MokaThemeExportFormat format))
		{
			return Task.CompletedTask;
		}

		_format = format;
		UpdateExport();
		return FormatChanged.InvokeAsync(format);
	}

	private Task HandleExport() => OnExport.InvokeAsync(MokaThemeSerializer.ToJson(Theme));

	private async Task HandleImport()
	{
		_importError = null;
		if (string.IsNullOrWhiteSpace(_importJson))
		{
			_importError = Text.ImportEmpty;
			return;
		}

		if (!MokaThemeSerializer.TryFromJson(_importJson, out MokaTheme? theme, out string? error))
		{
			_importError = error;
			return;
		}

		_importJson = null;
		await OnImport.InvokeAsync(theme);
	}
}
