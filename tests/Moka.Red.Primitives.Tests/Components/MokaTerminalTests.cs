using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Primitives.StatusDot;
using Moka.Red.Primitives.Terminal;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaTerminalTests : BunitContext
{
	private const string Esc = "\u001b";
	private const string Module = "./_content/Moka.Red.Core/moka-drag.js";
	private const string Stylesheet = "MokaTerminal.razor.css";

	private readonly BunitJSModuleInterop _module;

	public MokaTerminalTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
	}

	private static List<MokaTerminalLine> Numbered(int count) =>
		[.. Enumerable.Range(1, count).Select(i => new MokaTerminalLine($"line {i}"))];

	private static IReadOnlyList<MokaTerminalLine> Text(params string[] lines) =>
		[.. lines.Select(line => new MokaTerminalLine(line))];

	private IRenderedComponent<MokaTerminal> RenderLines(IReadOnlyList<MokaTerminalLine> lines, bool ansi = false) =>
		Render<MokaTerminal>(p => p.Add(x => x.Lines, lines).Add(x => x.Ansi, ansi));

	private IRenderedComponent<MokaTerminal> RenderCommands(params MokaTerminalCommand[] commands) =>
		Render<MokaTerminal>(p => p.Add(x => x.Commands, commands));

	private static int[] RenderCounts(IRenderedComponent<MokaTerminal> cut) =>
		[.. cut.FindComponents<MokaTerminalChunk>().Select(chunk => chunk.RenderCount)];

	private static IElement Action(IRenderedComponent<MokaTerminal> cut, string text) =>
		cut.FindAll(".moka-terminal-action").Single(button => button.TextContent.Trim() == text);

	[Fact]
	public void Lines_AreDrawnAsTheyWere()
	{
		IRenderedComponent<MokaTerminal> cut = Render<MokaTerminal>(p => p
			.Add(x => x.Lines, new[] { new MokaTerminalLine("build", "#4ec9b0", "$") })
			.Add(x => x.ShowLineNumbers, true));

		IElement line = cut.Find(".moka-terminal-body > .moka-terminal-line");
		Assert.Equal("moka-terminal-line", line.ClassName);
		Assert.Equal(
			"""<div class="moka-terminal-line"><span class="moka-terminal-line-number">1</span><span class="moka-terminal-prefix">$</span><span class="moka-terminal-text" style="color: #4ec9b0">build</span></div>""",
			line.OuterHtml);
	}

	[Fact]
	public void ALineWithoutColorOrPrefix_HasNeither()
	{
		IRenderedComponent<MokaTerminal> cut = RenderLines(Text("plain"));

		Assert.Equal(
			"""<div class="moka-terminal-line"><span class="moka-terminal-text">plain</span></div>""",
			cut.Find(".moka-terminal-line").OuterHtml);
	}

	[Fact]
	public void Neither_DrawsTheEmptyTerminal()
	{
		IRenderedComponent<MokaTerminal> cut = Render<MokaTerminal>();

		Assert.Empty(cut.Find(".moka-terminal-body").Children);
		Assert.Empty(cut.FindComponents<MokaTerminalOutput>());
		Assert.Equal("Terminal", cut.Find(".moka-terminal-title").TextContent);
	}

	[Fact]
	public void Neither_ShowsTheChildContent()
	{
		IRenderedComponent<MokaTerminal> cut = Render<MokaTerminal>(p => p.AddChildContent("<b>free form</b>"));

		Assert.Equal("free form", cut.Find(".moka-terminal-body b").TextContent);
	}

	[Fact]
	public void EmptyLists_DrawTheEmptyTerminal()
	{
		IRenderedComponent<MokaTerminal> cut = Render<MokaTerminal>(p => p
			.Add(x => x.Lines, Array.Empty<MokaTerminalLine>())
			.Add(x => x.Commands, Array.Empty<MokaTerminalCommand>()));

		Assert.Empty(cut.Find(".moka-terminal-body").Children);
	}

	[Fact]
	public void Output_IsCutIntoChunksOf200()
	{
		IRenderedComponent<MokaTerminal> cut = RenderLines(Numbered(450));

		IReadOnlyList<IRenderedComponent<MokaTerminalChunk>> chunks = cut.FindComponents<MokaTerminalChunk>();
		Assert.Equal(3, chunks.Count);
		Assert.Equal(200, chunks[0].FindAll(".moka-terminal-line").Count);
		Assert.Equal(200, chunks[1].FindAll(".moka-terminal-line").Count);
		Assert.Equal(50, chunks[2].FindAll(".moka-terminal-line").Count);
	}

	[Fact]
	public void Chunks_KeepTheLinesInOrder_AsDirectChildrenOfTheBody()
	{
		IRenderedComponent<MokaTerminal> cut = RenderLines(Numbered(450));

		IHtmlCollection<IElement> lines = cut.Find(".moka-terminal-body").Children;
		Assert.Equal(450, lines.Length);
		Assert.Equal("line 1", lines[0].TextContent);
		Assert.Equal("line 201", lines[200].TextContent);
		Assert.Equal("line 450", lines[449].TextContent);
	}

	[Fact]
	public void ANewChunk_StartsAtLine201()
	{
		IRenderedComponent<MokaTerminal> cut = RenderLines(Numbered(200));
		Assert.Single(cut.FindComponents<MokaTerminalChunk>());

		cut.Render(p => p.Add(x => x.Lines, Numbered(201)));

		Assert.Equal(2, cut.FindComponents<MokaTerminalChunk>().Count);
	}

	[Fact]
	public void AddingALine_DoesNotRedrawAFinishedChunk()
	{
		List<MokaTerminalLine> lines = Numbered(450);
		IRenderedComponent<MokaTerminal> cut = RenderLines(lines);
		int[] before = RenderCounts(cut);

		cut.Render(p => p.Add(x => x.Lines, new List<MokaTerminalLine>(lines) { new("line 451") }));

		int[] after = RenderCounts(cut);
		Assert.Equal(before[0], after[0]);
		Assert.Equal(before[1], after[1]);
		Assert.Equal(before[2] + 1, after[2]);
		Assert.Equal("line 451", cut.Find(".moka-terminal-body").Children[450].TextContent);
	}

	[Fact]
	public void AddingALine_ToAListGrownInPlace_DoesNotRedrawAFinishedChunk()
	{
		List<MokaTerminalLine> lines = Numbered(250);
		IRenderedComponent<MokaTerminal> cut = RenderLines(lines);
		int[] before = RenderCounts(cut);

		lines.Add(new MokaTerminalLine("line 251"));
		cut.Render(p => p.Add(x => x.Lines, lines));

		int[] after = RenderCounts(cut);
		Assert.Equal(before[0], after[0]);
		Assert.Equal(before[1] + 1, after[1]);
		Assert.Equal(251, cut.Find(".moka-terminal-body").Children.Length);
	}

	[Fact]
	public void ARenderOfTheParent_RedrawsNoChunk()
	{
		List<MokaTerminalLine> lines = Numbered(450);
		IRenderedComponent<MokaTerminal> cut = RenderLines(lines);
		int[] before = RenderCounts(cut);

		cut.Render(p => p.Add(x => x.Title, "Another title"));

		Assert.Equal(before, RenderCounts(cut));
	}

	[Fact]
	public void TheSameLinesInNewRecords_RedrawNoChunk()
	{
		IRenderedComponent<MokaTerminal> cut = RenderLines(Numbered(250));
		int[] before = RenderCounts(cut);

		cut.Render(p => p.Add(x => x.Lines, Numbered(250)));

		Assert.Equal(before, RenderCounts(cut));
	}

	[Fact]
	public void RewritingALine_RedrawsOnlyItsChunk()
	{
		List<MokaTerminalLine> lines = Numbered(450);
		IRenderedComponent<MokaTerminal> cut = RenderLines(lines);
		int[] before = RenderCounts(cut);

		List<MokaTerminalLine> rewritten = [.. lines];
		rewritten[250] = new MokaTerminalLine("rewritten");
		cut.Render(p => p.Add(x => x.Lines, rewritten));

		int[] after = RenderCounts(cut);
		Assert.Equal(before[0], after[0]);
		Assert.Equal(before[1] + 1, after[1]);
		Assert.Equal(before[2], after[2]);
		Assert.Equal("rewritten", cut.Find(".moka-terminal-body").Children[250].TextContent);
	}

	[Fact]
	public void DroppingLines_RemovesWhatTheyFilled()
	{
		IRenderedComponent<MokaTerminal> cut = RenderLines(Numbered(450));

		cut.Render(p => p.Add(x => x.Lines, Numbered(210)));

		Assert.Equal(2, cut.FindComponents<MokaTerminalChunk>().Count);
		Assert.Equal(210, cut.Find(".moka-terminal-body").Children.Length);
	}

	[Fact]
	public void LineNumbers_CarryOnAcrossChunks()
	{
		IRenderedComponent<MokaTerminal> cut = Render<MokaTerminal>(p => p
			.Add(x => x.Lines, Numbered(201))
			.Add(x => x.ShowLineNumbers, true));

		Assert.Equal("201", cut.FindAll(".moka-terminal-line-number")[200].TextContent);
	}

	[Fact]
	public void TurningOnLineNumbers_RedrawsAFinishedChunk()
	{
		IRenderedComponent<MokaTerminal> cut = RenderLines(Numbered(250));
		Assert.Empty(cut.FindAll(".moka-terminal-line-number"));

		cut.Render(p => p.Add(x => x.ShowLineNumbers, true));

		Assert.Equal(250, cut.FindAll(".moka-terminal-line-number").Count);
	}

	[Fact]
	public void Ansi_IsOffByDefault_AndTheTextStaysAsItIs()
	{
		IRenderedComponent<MokaTerminal> cut = Render<MokaTerminal>(p => p.Add(x => x.Lines, Text($"{Esc}[31mred{Esc}[0m")));

		Assert.Contains("[31mred", cut.Find(".moka-terminal-text").TextContent, StringComparison.Ordinal);
		Assert.Empty(cut.FindAll("[class*='moka-ansi']"));
	}

	[Fact]
	public void Ansi_DrawsAColorAsAClass_NotAStyle()
	{
		IRenderedComponent<MokaTerminal> cut = RenderLines(Text($"plain {Esc}[31mred{Esc}[0m rest"), true);

		IElement text = cut.Find(".moka-terminal-text");
		Assert.Equal("plain red rest", text.TextContent);
		IElement red = cut.Find(".moka-ansi-f1");
		Assert.Equal("red", red.TextContent);
		Assert.Null(red.GetAttribute("style"));
		Assert.Null(text.GetAttribute("style"));
	}

	[Fact]
	public void Ansi_LeavesPlainRunsOutOfASpan()
	{
		IRenderedComponent<MokaTerminal> cut = RenderLines(Text("just text"), true);

		Assert.Equal(
			"""<span class="moka-terminal-text">just text</span>""",
			cut.Find(".moka-terminal-text").OuterHtml);
	}

	[Theory]
	[InlineData("38;5;196", "moka-ansi-f9")]
	[InlineData("38;2;255;255;255", "moka-ansi-f15")]
	[InlineData("48;5;0", "moka-ansi-b0")]
	[InlineData("48;2;255;255;255", "moka-ansi-b15")]
	public void Ansi_DrawsExtendedColorsAsTheNearestClassicOne(string code, string expected)
	{
		IRenderedComponent<MokaTerminal> cut = RenderLines(Text($"{Esc}[{code}mx"), true);

		IElement span = cut.Find(".moka-terminal-text > span");
		Assert.Equal(expected, span.ClassName);
		Assert.Null(span.GetAttribute("style"));
	}

	[Fact]
	public void Ansi_CarriesAColorToTheNextLine()
	{
		IRenderedComponent<MokaTerminal> cut = RenderLines(Text($"{Esc}[32mstart", $"still{Esc}[0m", "plain"), true);

		IReadOnlyList<IElement> greens = cut.FindAll(".moka-ansi-f2");
		Assert.Equal(["start", "still"], greens.Select(green => green.TextContent));
		Assert.Empty(cut.FindAll(".moka-terminal-text")[2].Children);
	}

	[Fact]
	public void Ansi_CarriesAColorAcrossAChunk()
	{
		List<MokaTerminalLine> lines = Numbered(250);
		lines[199] = new MokaTerminalLine($"{Esc}[32mline 200");

		IRenderedComponent<MokaTerminal> cut = RenderLines(lines, true);

		Assert.Equal("line 201", cut.FindComponents<MokaTerminalChunk>()[1].Find(".moka-ansi-f2").TextContent);
	}

	[Fact]
	public void Ansi_RereadsTheLinesAfterOneThatChanged()
	{
		IRenderedComponent<MokaTerminal> cut = RenderLines(Text($"{Esc}[31mone", "two"), true);
		Assert.Equal(2, cut.FindAll(".moka-ansi-f1").Count);

		cut.Render(p => p.Add(x => x.Lines, Text($"{Esc}[32mone", "two")));

		Assert.Empty(cut.FindAll(".moka-ansi-f1"));
		Assert.Equal(2, cut.FindAll(".moka-ansi-f2").Count);
	}

	[Fact]
	public void Ansi_AddingALine_DoesNotRedrawAFinishedChunk()
	{
		List<MokaTerminalLine> lines = Numbered(250);
		IRenderedComponent<MokaTerminal> cut = RenderLines(lines, true);
		int[] before = RenderCounts(cut);

		cut.Render(p => p.Add(x => x.Lines, new List<MokaTerminalLine>(lines) { new("line 251") }));

		int[] after = RenderCounts(cut);
		Assert.Equal(before[0], after[0]);
		Assert.Equal(before[1] + 1, after[1]);
	}

	[Fact]
	public void Ansi_TurnedOn_RedrawsWhatIsThere()
	{
		IRenderedComponent<MokaTerminal> cut = RenderLines(Text($"{Esc}[31mred"));
		Assert.Empty(cut.FindAll(".moka-ansi-f1"));

		cut.Render(p => p.Add(x => x.Ansi, true));

		Assert.Equal("red", cut.Find(".moka-ansi-f1").TextContent);
	}

	[Fact]
	public void Ansi_KeepsTheLinesOwnColorAndPrefix()
	{
		IRenderedComponent<MokaTerminal> cut = Render<MokaTerminal>(p => p
			.Add(x => x.Lines, new[] { new MokaTerminalLine($"{Esc}[1mbold", "#4ec9b0", "$") })
			.Add(x => x.Ansi, true));

		Assert.Equal("$", cut.Find(".moka-terminal-prefix").TextContent);
		Assert.Equal("color: #4ec9b0", cut.Find(".moka-terminal-text").GetAttribute("style"));
		Assert.Equal("bold", cut.Find(".moka-ansi-bold").TextContent);
	}

	[Fact]
	public void Ansi_MarksALineWithABackground_SoItsRowsTouch()
	{
		IRenderedComponent<MokaTerminal> cut = RenderLines(Text($"{Esc}[47m  {Esc}[0m", "text"), true);

		IReadOnlyList<IElement> lines = cut.FindAll(".moka-terminal-line");
		Assert.Contains("moka-terminal-line--blocks", lines[0].ClassName, StringComparison.Ordinal);
		Assert.DoesNotContain("moka-terminal-line--blocks", lines[1].ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Copy_TakesTheCodesOut_WithAnsi()
	{
		_module.Setup<bool?>("copyToClipboard", _ => true).SetResult(true);
		IRenderedComponent<MokaTerminal> cut = Render<MokaTerminal>(p => p
			.Add(x => x.Lines, new[] { new MokaTerminalLine($"{Esc}[31mred{Esc}[0m", Prefix: "$"), new MokaTerminalLine("plain") })
			.Add(x => x.Ansi, true));

		await cut.Find(".moka-terminal-copy").ClickAsync(new MouseEventArgs());

		Assert.Equal("$ red\nplain", _module.VerifyInvoke("copyToClipboard").Arguments[0]);
	}

	[Fact]
	public async Task Copy_KeepsTheTextAsItIs_WithoutAnsi()
	{
		_module.Setup<bool?>("copyToClipboard", _ => true).SetResult(true);
		IRenderedComponent<MokaTerminal> cut = RenderLines(new[] { new MokaTerminalLine("build", Prefix: "$"), new MokaTerminalLine("ok") });

		await cut.Find(".moka-terminal-copy").ClickAsync(new MouseEventArgs());

		Assert.Equal("$ build\nok", _module.VerifyInvoke("copyToClipboard").Arguments[0]);
	}

	[Fact]
	public async Task Copy_ReadsTheLinesShowingWhenClicked()
	{
		_module.Setup<bool?>("copyToClipboard", _ => true).SetResult(true);
		List<MokaTerminalLine> lines = [new("one")];
		IRenderedComponent<MokaTerminal> cut = RenderLines(lines);

		lines.Add(new MokaTerminalLine("two"));
		await cut.Find(".moka-terminal-copy").ClickAsync(new MouseEventArgs());

		Assert.Equal("one\ntwo", _module.VerifyInvoke("copyToClipboard").Arguments[0]);
	}

	[Fact]
	public void Commands_DrawABlockEach()
	{
		IRenderedComponent<MokaTerminal> cut = RenderCommands(
			new MokaTerminalCommand("dotnet test", MokaTerminalCommandState.Succeeded, Text("Passed!")),
			new MokaTerminalCommand("dotnet pack", MokaTerminalCommandState.Running));

		IReadOnlyList<IElement> blocks = cut.FindAll(".moka-terminal-command");
		Assert.Equal(2, blocks.Count);
		Assert.Equal("dotnet test", blocks[0].GetAttribute("aria-label"));
		Assert.Equal("dotnet test", blocks[0].QuerySelector(".moka-terminal-command-text")!.TextContent);
		Assert.Equal("Passed!", blocks[0].QuerySelector(".moka-terminal-command-output .moka-terminal-text")!.TextContent);
	}

	[Fact]
	public void Commands_WithoutLines_DrawNoOutput()
	{
		IRenderedComponent<MokaTerminal> cut = RenderCommands(new MokaTerminalCommand("dotnet pack", MokaTerminalCommandState.Running));

		Assert.Empty(cut.FindAll(".moka-terminal-command-output"));
	}

	[Theory]
	[InlineData(MokaTerminalCommandState.Running, "moka-status-dot--info", "moka-terminal-command--running", "Running")]
	[InlineData(MokaTerminalCommandState.Succeeded, "moka-status-dot--success", "moka-terminal-command--succeeded", "Succeeded")]
	[InlineData(MokaTerminalCommandState.Failed, "moka-status-dot--error", "moka-terminal-command--failed", "Failed")]
	public void TheState_ShowsInTheDotAndTheBlock(MokaTerminalCommandState state, string dotClass, string blockClass, string name)
	{
		IRenderedComponent<MokaTerminal> cut = RenderCommands(new MokaTerminalCommand("x", state));

		IElement dot = cut.FindComponent<MokaStatusDot>().Find(".moka-status-dot");
		Assert.Contains(dotClass, dot.ClassName, StringComparison.Ordinal);
		Assert.Equal(name, dot.GetAttribute("aria-label"));
		Assert.Equal("img", dot.GetAttribute("role"));
		Assert.Contains(blockClass, cut.Find(".moka-terminal-command").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void OnlyARunningCommand_Pulses()
	{
		IRenderedComponent<MokaTerminal> cut = RenderCommands(
			new MokaTerminalCommand("a", MokaTerminalCommandState.Running),
			new MokaTerminalCommand("b", MokaTerminalCommandState.Succeeded));

		IReadOnlyList<IRenderedComponent<MokaStatusDot>> dots = cut.FindComponents<MokaStatusDot>();
		Assert.True(dots[0].Instance.Pulse);
		Assert.False(dots[1].Instance.Pulse);
	}

	[Fact]
	public void StateLabel_NamesTheState()
	{
		IRenderedComponent<MokaTerminal> cut = Render<MokaTerminal>(p => p
			.Add(x => x.Commands, new[] { new MokaTerminalCommand("x", MokaTerminalCommandState.Failed) })
			.Add(x => x.StateLabel, state => $"Ended: {state}"));

		Assert.Equal("Ended: Failed", cut.Find(".moka-status-dot").GetAttribute("aria-label"));
	}

	[Fact]
	public void AFailedBlock_HasTheLineBorder()
	{
		Assert.Equal(
			"var(--moka-color-error-line)",
			ScopedCss.Declarations(Stylesheet, ".moka-terminal-command--failed")["border-color"]);
	}

	[Fact]
	public void Copy_AlwaysShows_AndTheOtherButtonsOnlyWithACallback()
	{
		IRenderedComponent<MokaTerminal> cut = RenderCommands(new MokaTerminalCommand("x", MokaTerminalCommandState.Succeeded));

		IElement only = Assert.Single(cut.FindAll(".moka-terminal-action"));
		Assert.Equal("Copy", only.TextContent.Trim());
		Assert.Contains("moka-copy-btn", only.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void EachButton_ShowsOnItsOwnCallback()
	{
		IRenderedComponent<MokaTerminal> edit = Render<MokaTerminal>(p => p
			.Add(x => x.Commands, new[] { new MokaTerminalCommand("x", MokaTerminalCommandState.Failed) })
			.Add(x => x.OnEdit, EventCallback.Factory.Create<MokaTerminalCommand>(this, _ => { })));
		IRenderedComponent<MokaTerminal> again = Render<MokaTerminal>(p => p
			.Add(x => x.Commands, new[] { new MokaTerminalCommand("x", MokaTerminalCommandState.Failed) })
			.Add(x => x.OnRunAgain, EventCallback.Factory.Create<MokaTerminalCommand>(this, _ => { })));

		Assert.Equal(["Copy", "Edit"], edit.FindAll(".moka-terminal-action").Select(button => button.TextContent.Trim()));
		Assert.Equal(["Copy", "Run again"], again.FindAll(".moka-terminal-action").Select(button => button.TextContent.Trim()));
	}

	[Fact]
	public async Task TheButtons_ReportTheirOwnCommand()
	{
		MokaTerminalCommand first = new("first", MokaTerminalCommandState.Failed);
		MokaTerminalCommand second = new("second", MokaTerminalCommandState.Succeeded);
		MokaTerminalCommand? edited = null;
		MokaTerminalCommand? ranAgain = null;
		IRenderedComponent<MokaTerminal> cut = Render<MokaTerminal>(p => p
			.Add(x => x.Commands, new[] { first, second })
			.Add(x => x.OnEdit, EventCallback.Factory.Create<MokaTerminalCommand>(this, command => edited = command))
			.Add(x => x.OnRunAgain, EventCallback.Factory.Create<MokaTerminalCommand>(this, command => ranAgain = command)));

		IReadOnlyList<IElement> buttons = cut.FindAll(".moka-terminal-action");
		await buttons[1].ClickAsync(new MouseEventArgs());
		await cut.FindAll(".moka-terminal-action")[5].ClickAsync(new MouseEventArgs());

		Assert.Same(first, edited);
		Assert.Same(second, ranAgain);
	}

	[Fact]
	public void ARunningCommand_HasOnlyCopy()
	{
		IRenderedComponent<MokaTerminal> cut = Render<MokaTerminal>(p => p
			.Add(x => x.Commands, new[] { new MokaTerminalCommand("x", MokaTerminalCommandState.Running) })
			.Add(x => x.OnEdit, EventCallback.Factory.Create<MokaTerminalCommand>(this, _ => { }))
			.Add(x => x.OnRunAgain, EventCallback.Factory.Create<MokaTerminalCommand>(this, _ => { })));

		Assert.Equal("Copy", Assert.Single(cut.FindAll(".moka-terminal-action")).TextContent.Trim());
	}

	[Fact]
	public void TheLabels_CanBeChanged()
	{
		IRenderedComponent<MokaTerminal> cut = Render<MokaTerminal>(p => p
			.Add(x => x.Commands, new[] { new MokaTerminalCommand("x", MokaTerminalCommandState.Failed) })
			.Add(x => x.OnEdit, EventCallback.Factory.Create<MokaTerminalCommand>(this, _ => { }))
			.Add(x => x.OnRunAgain, EventCallback.Factory.Create<MokaTerminalCommand>(this, _ => { }))
			.Add(x => x.CopyLabel, "Kopiera")
			.Add(x => x.EditLabel, "Ändra")
			.Add(x => x.RunAgainLabel, "Kör igen"));

		Assert.Equal(["Kopiera", "Ändra", "Kör igen"], cut.FindAll(".moka-terminal-action").Select(button => button.TextContent.Trim()));
	}

	[Fact]
	public async Task ABlocksCopy_TakesTheCommandAndItsOutput()
	{
		_module.Setup<bool?>("copyToClipboard", _ => true).SetResult(true);
		IRenderedComponent<MokaTerminal> cut = RenderCommands(new MokaTerminalCommand("dotnet test", MokaTerminalCommandState.Succeeded, Text("Passed!", "1 warning")));

		await Action(cut, "Copy").ClickAsync(new MouseEventArgs());

		Assert.Equal("dotnet test\nPassed!\n1 warning", _module.VerifyInvoke("copyToClipboard").Arguments[0]);
	}

	[Fact]
	public async Task ABlocksCopy_WithoutOutput_TakesTheCommand()
	{
		_module.Setup<bool?>("copyToClipboard", _ => true).SetResult(true);
		IRenderedComponent<MokaTerminal> cut = RenderCommands(new MokaTerminalCommand("dotnet test", MokaTerminalCommandState.Running));

		await Action(cut, "Copy").ClickAsync(new MouseEventArgs());

		Assert.Equal("dotnet test", _module.VerifyInvoke("copyToClipboard").Arguments[0]);
	}

	[Fact]
	public async Task TheHeadersCopy_TakesEveryBlock()
	{
		_module.Setup<bool?>("copyToClipboard", _ => true).SetResult(true);
		IRenderedComponent<MokaTerminal> cut = RenderCommands(
			new MokaTerminalCommand("a", MokaTerminalCommandState.Succeeded, Text("one")),
			new MokaTerminalCommand("b", MokaTerminalCommandState.Running));

		await cut.Find(".moka-terminal-copy").ClickAsync(new MouseEventArgs());

		Assert.Equal("a\none\n\nb", _module.VerifyInvoke("copyToClipboard").Arguments[0]);
	}

	[Fact]
	public void Commands_WinOverLines()
	{
		IRenderedComponent<MokaTerminal> cut = Render<MokaTerminal>(p => p
			.Add(x => x.Lines, Text("a line"))
			.Add(x => x.Commands, new[] { new MokaTerminalCommand("a command", MokaTerminalCommandState.Succeeded) }));

		Assert.Single(cut.FindAll(".moka-terminal-command"));
		Assert.Empty(cut.FindAll(".moka-terminal-line"));
	}

	[Fact]
	public void EmptyCommands_LeaveLinesAlone()
	{
		IRenderedComponent<MokaTerminal> cut = Render<MokaTerminal>(p => p
			.Add(x => x.Lines, Text("a line"))
			.Add(x => x.Commands, Array.Empty<MokaTerminalCommand>()));

		Assert.Empty(cut.FindAll(".moka-terminal-commands"));
		Assert.Single(cut.FindAll(".moka-terminal-line"));
	}

	[Fact]
	public void ABlocksLines_AreChunked_AndNumberedFromOne()
	{
		IRenderedComponent<MokaTerminal> cut = Render<MokaTerminal>(p => p
			.Add(x => x.Commands, new[]
			{
				new MokaTerminalCommand("a", MokaTerminalCommandState.Succeeded, Numbered(250)),
				new MokaTerminalCommand("b", MokaTerminalCommandState.Succeeded, Numbered(5))
			})
			.Add(x => x.ShowLineNumbers, true));

		Assert.Equal(3, cut.FindComponents<MokaTerminalChunk>().Count);
		Assert.Equal("1", cut.FindAll(".moka-terminal-command")[1].QuerySelector(".moka-terminal-line-number")!.TextContent);
	}

	[Fact]
	public void ALineAddedToARunningCommand_RedrawsNoFinishedChunk()
	{
		List<MokaTerminalLine> finished = Numbered(450);
		MokaTerminalCommand done = new("a", MokaTerminalCommandState.Succeeded, finished);
		List<MokaTerminalLine> running = Numbered(250);
		IRenderedComponent<MokaTerminal> cut = RenderCommands(done, new MokaTerminalCommand("b", MokaTerminalCommandState.Running, running));
		int[] before = RenderCounts(cut);

		cut.Render(p => p.Add(x => x.Commands, new[]
		{
			done,
			new MokaTerminalCommand("b", MokaTerminalCommandState.Running, new List<MokaTerminalLine>(running) { new("line 251") })
		}));

		int[] after = RenderCounts(cut);
		Assert.Equal(before.Take(4), after.Take(4));
		Assert.Equal(before[4] + 1, after[4]);
	}

	[Fact]
	public void ACommandThatEnds_KeepsItsDrawnLines()
	{
		List<MokaTerminalLine> lines = Numbered(250);
		MokaTerminalCommand running = new("a", MokaTerminalCommandState.Running, lines);
		IRenderedComponent<MokaTerminal> cut = RenderCommands(running);
		int[] before = RenderCounts(cut);

		cut.Render(p => p.Add(x => x.Commands, new[] { running with { State = MokaTerminalCommandState.Succeeded } }));

		Assert.Equal(before, RenderCounts(cut));
		Assert.Contains("moka-terminal-command--succeeded", cut.Find(".moka-terminal-command").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void EachBlock_StartsWithoutTheColorOfTheOneBefore()
	{
		IRenderedComponent<MokaTerminal> cut = Render<MokaTerminal>(p => p
			.Add(x => x.Commands, new[]
			{
				new MokaTerminalCommand("a", MokaTerminalCommandState.Succeeded, Text($"{Esc}[31mred")),
				new MokaTerminalCommand("b", MokaTerminalCommandState.Succeeded, Text("plain"))
			})
			.Add(x => x.Ansi, true));

		Assert.Single(cut.FindAll(".moka-ansi-f1"));
		Assert.Equal("plain", cut.FindAll(".moka-terminal-command")[1].QuerySelector(".moka-terminal-text")!.TextContent);
	}

	[Fact]
	public void AutoScroll_FollowsCommandsAsItDoesLines()
	{
		_module.SetupVoid("scrollToBottom", _ => true).SetVoidResult();

		RenderCommands(new MokaTerminalCommand("a", MokaTerminalCommandState.Running));

		Assert.NotEmpty(_module.Invocations["scrollToBottom"]);
	}

	[Fact]
	public void AnEmptyTerminal_DoesNotScroll()
	{
		_module.SetupVoid("scrollToBottom", _ => true).SetVoidResult();

		Render<MokaTerminal>();

		Assert.Empty(_module.Invocations["scrollToBottom"]);
	}

	[Theory]
	[InlineData("f", "color")]
	[InlineData("b", "background")]
	public void EveryAnsiColor_ReadsTokens(string prefix, string property)
	{
		foreach (string name in Enumerable.Range(0, 16).Select(i => i.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append("i"))
		{
			string value = ScopedCss.Declarations(Stylesheet, $".moka-terminal-body ::deep .moka-ansi-{prefix}{name}")[property];

			Assert.Contains("var(--", value, StringComparison.Ordinal);
			Assert.DoesNotContain("#", value, StringComparison.Ordinal);
		}
	}

	[Fact]
	public void TheAnsiMixes_ReadTokens()
	{
		IReadOnlyDictionary<string, string> root = ScopedCss.Declarations(Stylesheet, ".moka-terminal");

		Assert.Equal("var(--moka-color-paper)", root["--_ansi-bright"]);
		Assert.Contains("var(--moka-color-error)", root["--_ansi-magenta"], StringComparison.Ordinal);
		Assert.Contains("var(--moka-color-success)", root["--_ansi-cyan"], StringComparison.Ordinal);
	}

	[Fact]
	public void TheGateLook_TakesTheTokensAsTheyAre()
	{
		IReadOnlyDictionary<string, string> gate = ScopedCss.Declarations(Stylesheet, ".moka-gate .moka-terminal");

		Assert.Equal("100%", gate["--_ansi-tone"]);
		Assert.Equal("var(--moka-color-on-surface)", gate["--_ansi-bright"]);
	}

	[Theory]
	[InlineData("moka-terminal-line", "display", "flex")]
	[InlineData("moka-terminal-text", "white-space", "pre-wrap")]
	[InlineData("moka-terminal-text", "word-break", "break-all")]
	public void TheLineRules_ReachTheChunks(string part, string property, string value)
	{
		Assert.Equal(value, ScopedCss.Declarations(Stylesheet, $".moka-terminal-body ::deep .{part}")[property]);
	}
}
