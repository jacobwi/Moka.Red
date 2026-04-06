using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Terminal;

/// <summary>
///     A styled terminal/console output display with a macOS-style title bar.
///     Renders lines in a monospace font on a dark background. Read-only by design.
/// </summary>
public partial class MokaTerminal : MokaComponentBase
{
	/// <summary>Structured terminal lines. Takes precedence over <see cref="ChildContent" />.</summary>
	[Parameter]
	public IReadOnlyList<MokaTerminalLine>? Lines { get; set; }

	/// <summary>Alternative free-form content rendered inside the terminal body.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Title displayed in the header bar. Defaults to "Terminal".</summary>
	[Parameter]
	public string Title { get; set; } = "Terminal";

	/// <summary>Whether to show the macOS-style title bar with colored dots. Defaults to true.</summary>
	[Parameter]
	public bool ShowHeader { get; set; } = true;

	/// <summary>Maximum height of the terminal body. Accepts any CSS length. Defaults to "400px".</summary>
	[Parameter]
	public string MaxHeight { get; set; } = "400px";

	/// <summary>Whether to auto-scroll to the bottom when new lines are added. Defaults to true.</summary>
	[Parameter]
	public bool AutoScroll { get; set; } = true;

	/// <summary>Whether to display line numbers alongside each line. Defaults to false.</summary>
	[Parameter]
	public bool ShowLineNumbers { get; set; }

	/// <summary>Whether to show a copy-to-clipboard button in the header. Defaults to true.</summary>
	[Parameter]
	public bool ShowCopyButton { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-terminal";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-terminal--line-numbers", ShowLineNumbers)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle(Style)
		.Build();

	private string BodyStyle => new StyleBuilder()
		.AddStyle("max-height", MaxHeight)
		.Build() ?? string.Empty;

	/// <summary>Builds the full text content for clipboard copy.</summary>
	private string CopyText
	{
		get
		{
			if (Lines is null or { Count: 0 })
			{
				return string.Empty;
			}

			return string.Join('\n', Lines.Select(l =>
				string.IsNullOrEmpty(l.Prefix) ? l.Text : $"{l.Prefix} {l.Text}"));
		}
	}

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (AutoScroll && Lines is { Count: > 0 })
		{
			await SafeJsInvokeVoidAsync("eval",
				$"document.getElementById('{Id}-body')?.scrollTo(0, 999999)");
		}

		await base.OnAfterRenderAsync(firstRender);
	}
}
