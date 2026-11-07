using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Terminal;

/// <summary>
///     A styled terminal/console output display with a macOS-style title bar.
///     Renders lines in a monospace font on a dark background. Read-only by design: with <see cref="Commands" /> the app
///     runs the commands, and this only draws them. Output is drawn in chunks that don't redraw once their lines are
///     final, so a long log stays quick while lines arrive.
/// </summary>
public partial class MokaTerminal : MokaComponentBase
{
	private ElementReference _bodyRef;

	/// <summary>Structured terminal lines. Takes precedence over <see cref="ChildContent" />.</summary>
	[Parameter]
	public IReadOnlyList<MokaTerminalLine>? Lines { get; set; }

	/// <summary>
	///     Commands, each drawn as a block with its state, its output and its buttons. A separate mode from
	///     <see cref="Lines" />, and it wins when both are set.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaTerminalCommand>? Commands { get; set; }

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

	/// <summary>
	///     Decodes ANSI escape sequences in each line's text: colors and weight are drawn, and anything else, such as
	///     moving the cursor, is dropped. Colors read theme tokens, so 256-color and 24-bit ones draw as the nearest of the
	///     16 classic colors. Off by default, which shows the text as it is.
	/// </summary>
	[Parameter]
	public bool Ansi { get; set; }

	/// <summary>
	///     Raised by a block's Edit button, which shows only when this is set and the command has ended. Usually puts the
	///     command back on the app's input line.
	/// </summary>
	[Parameter]
	public EventCallback<MokaTerminalCommand> OnEdit { get; set; }

	/// <summary>Raised by a block's Run again button, which shows only when this is set and the command has ended.</summary>
	[Parameter]
	public EventCallback<MokaTerminalCommand> OnRunAgain { get; set; }

	/// <summary>Text of a block's Copy button, which copies the command and its output. Defaults to "Copy".</summary>
	[Parameter]
	public string CopyLabel { get; set; } = "Copy";

	/// <summary>Text of a block's Edit button. Defaults to "Edit".</summary>
	[Parameter]
	public string EditLabel { get; set; } = "Edit";

	/// <summary>Text of a block's Run again button. Defaults to "Run again".</summary>
	[Parameter]
	public string RunAgainLabel { get; set; } = "Run again";

	/// <summary>
	///     Names a command's state for a screen reader, since the status dot only shows it as a color. Defaults to
	///     "Running", "Succeeded" and "Failed".
	/// </summary>
	[Parameter]
	public Func<MokaTerminalCommandState, string>? StateLabel { get; set; }

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

	private static string CommandClass(MokaTerminalCommand command) => new CssBuilder("moka-terminal-command")
		.AddClass("moka-terminal-command--running", command.State == MokaTerminalCommandState.Running)
		.AddClass("moka-terminal-command--succeeded", command.State == MokaTerminalCommandState.Succeeded)
		.AddClass("moka-terminal-command--failed", command.State == MokaTerminalCommandState.Failed)
		.Build();

	// Not the accent for Running: on the red preset it would read as Failed.
	private static MokaColor StateColor(MokaTerminalCommand command) => command.State switch
	{
		MokaTerminalCommandState.Succeeded => MokaColor.Success,
		MokaTerminalCommandState.Failed => MokaColor.Error,
		_ => MokaColor.Info
	};

	private string StateName(MokaTerminalCommand command) =>
		StateLabel?.Invoke(command.State) ?? command.State.ToString();

	private RenderFragment Output(IReadOnlyList<MokaTerminalLine> lines) => builder =>
	{
		builder.OpenComponent<MokaTerminalOutput>(0);
		builder.AddComponentParameter(1, nameof(MokaTerminalOutput.Lines), lines);
		builder.AddComponentParameter(2, nameof(MokaTerminalOutput.Ansi), Ansi);
		builder.AddComponentParameter(3, nameof(MokaTerminalOutput.ShowLineNumbers), ShowLineNumbers);
		builder.CloseComponent();
	};

	// Built when Copy is clicked. Building it on every render would copy the whole log for each line that arrives.
	private string AllText() => Commands is { Count: > 0 }
		? string.Join("\n\n", Commands.Select(CommandText))
		: LinesText(Lines);

	private string CommandText(MokaTerminalCommand command) => command.Lines is { Count: > 0 }
		? $"{command.Command}\n{LinesText(command.Lines)}"
		: command.Command;

	private string LinesText(IReadOnlyList<MokaTerminalLine>? lines)
	{
		if (lines is null or { Count: 0 })
		{
			return string.Empty;
		}

		return string.Join('\n', lines.Select(LineText));
	}

	private string LineText(MokaTerminalLine line)
	{
		string text = Ansi ? AnsiDecoder.Strip(line.Text) : line.Text;
		return string.IsNullOrEmpty(line.Prefix) ? text : $"{line.Prefix} {text}";
	}

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// Scrolled through an element reference. The old eval spliced Id into a script, which a
		// strict content security policy blocks, and without an Id it looked for "-body" and did
		// nothing.
		if (AutoScroll && (Lines is { Count: > 0 } || Commands is { Count: > 0 }))
		{
			await SafeModuleInvokeVoidAsync("./_content/Moka.Red.Core/moka-drag.js", "scrollToBottom", _bodyRef);
		}

		await base.OnAfterRenderAsync(firstRender);
	}
}
