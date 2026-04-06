using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Theming;
using Moka.Red.ThemeGen.Serialization;

namespace Moka.Red.ThemeGen.ImportExport;

/// <summary>
///     Import/export panel for themes. Supports JSON, CSS variables, and C# code export formats,
///     plus JSON import.
/// </summary>
public partial class MokaThemeImportExport : ComponentBase
{
	private bool _copied;

	private ExportFormat _exportFormat = ExportFormat.Json;
	private string _exportOutput = "";
	private string? _importError;
	private string _importJson = "";
	[Inject] private IJSRuntime JsRuntime { get; set; } = default!;

	/// <summary>The current theme to export.</summary>
	[Parameter]
	public MokaTheme Theme { get; set; } = MokaTheme.Light;

	/// <summary>Fires when a theme is imported and should be applied.</summary>
	[Parameter]
	public EventCallback<MokaTheme> OnImport { get; set; }

	/// <summary>Fires with JSON when the user clicks Export.</summary>
	[Parameter]
	public EventCallback<string> OnExport { get; set; }

	/// <summary>Whether to show the Export button (only if OnExport callback is bound).</summary>
	private bool ShowExportCallback => OnExport.HasDelegate;

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet() => UpdateExportOutput();

	private void SetExportFormat(ExportFormat format)
	{
		_exportFormat = format;
		_copied = false;
		UpdateExportOutput();
	}

	private void UpdateExportOutput()
	{
		_exportOutput = _exportFormat switch
		{
			ExportFormat.Json => MokaThemeSerializer.ToJson(Theme),
			ExportFormat.Css => MokaThemeSerializer.ToCss(Theme),
			ExportFormat.CSharp => MokaThemeSerializer.ToCSharp(Theme),
			_ => ""
		};
	}

	private async Task CopyExport()
	{
		try
		{
			await JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", _exportOutput);
			_copied = true;
			StateHasChanged();
		}
		catch (JSException)
		{
			// Clipboard API may not be available
		}
		catch (JSDisconnectedException)
		{
			// Circuit disconnected
		}
	}

	private async Task HandleExport()
	{
		string json = MokaThemeSerializer.ToJson(Theme);
		await OnExport.InvokeAsync(json);
	}

	private async Task HandleImport()
	{
		_importError = null;

		if (string.IsNullOrWhiteSpace(_importJson))
		{
			_importError = "Please paste theme JSON.";
			return;
		}

		MokaTheme? theme = MokaThemeSerializer.FromJson(_importJson);
		if (theme is null)
		{
			_importError = "Invalid JSON. Could not parse as a MokaTheme.";
			return;
		}

		await OnImport.InvokeAsync(theme);
		_importJson = "";
	}

	private enum ExportFormat
	{
		Json,
		Css,
		CSharp
	}
}
