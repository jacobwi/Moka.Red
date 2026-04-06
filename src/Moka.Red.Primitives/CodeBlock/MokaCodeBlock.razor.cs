using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.CodeBlock;

/// <summary>
///     A styled code display block with optional line numbers, language label, and copy button.
///     Unlike <c>MokaCode</c> which is an inline code element, this renders a full
///     <c>&lt;pre&gt;&lt;code&gt;</c> block suitable for multi-line code snippets.
/// </summary>
public partial class MokaCodeBlock
{
	private bool _copied;
	private bool _disposed;
	private CancellationTokenSource? _resetCts;

	/// <summary>The code text to display. Required.</summary>
	[Parameter]
	[EditorRequired]
	public string Code { get; set; } = string.Empty;

	/// <summary>Optional language hint label displayed in the header (e.g., "C#", "JSON", "HTML").</summary>
	[Parameter]
	public string? Language { get; set; }

	/// <summary>Whether to show line numbers in the gutter. Defaults to true.</summary>
	[Parameter]
	public bool ShowLineNumbers { get; set; } = true;

	/// <summary>Whether to show a copy-to-clipboard button. Defaults to true.</summary>
	[Parameter]
	public bool ShowCopyButton { get; set; } = true;

	/// <summary>Maximum height of the code area. When set, the block becomes scrollable (e.g., "300px").</summary>
	[Parameter]
	public string? MaxHeight { get; set; }

	/// <summary>Whether to wrap long lines instead of scrolling horizontally. Defaults to false.</summary>
	[Parameter]
	public bool Wrap { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-code-block";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-code-block--line-numbers", ShowLineNumbers)
		.AddClass("moka-code-block--wrap", Wrap)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle(Style)
		.Build();

	private string? PreStyle => new StyleBuilder()
		.AddStyle("max-height", MaxHeight, MaxHeight is not null)
		.Build();

	private string[] Lines => Code.Split('\n');

	private bool ShowHeader => Language is not null || ShowCopyButton;

	private async Task HandleCopy()
	{
		try
		{
			IJSObjectReference module =
				await GetJsModuleAsync("./_content/Moka.Red.Primitives/CodeBlock/MokaCodeBlock.razor.js");
			await module.InvokeVoidAsync("copyToClipboard", Code);

			_copied = true;
			ForceRender();

			if (_resetCts is not null)
			{
				await _resetCts.CancelAsync();
			}

			_resetCts = new CancellationTokenSource();
			CancellationToken token = _resetCts.Token;

			_ = Task.Delay(2000, token).ContinueWith(_ =>
			{
				if (!token.IsCancellationRequested && !_disposed)
				{
					_copied = false;
					InvokeAsync(ForceRender);
				}
			}, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Current);
		}
		catch (JSException)
		{
			// Clipboard API may not be available
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		_disposed = true;
		if (_resetCts is not null)
		{
			await _resetCts.CancelAsync();
			_resetCts.Dispose();
		}

		await base.DisposeAsyncCore();
	}
}
