using System.Runtime.CompilerServices;
using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.Table;
using Moka.Red.Tests.Shared;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Table;

// Keys typed while a cell's editor is drawn, a stream that breaks after rows came, and the editor of a Field column over
// a nullable number.
public class MokaTableReleaseFixTests : BunitContext
{
	private readonly List<MokaTableRowSave<Part>> _saves = [];

	public MokaTableReleaseFixTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	// ── Keys while the editor opens ──

	[Fact]
	public async Task EnterOnAnEditableCell_SaysAnEditorOpened_SoTheScriptHandsItTheKeysTypedMeanwhile()
	{
		IRenderedComponent<MokaTable<Part>> cut = RenderParts([new Part("Bolt", 4, null)]);

		bool opened = await cut.InvokeAsync(() => cut.Instance.OnCellKey(0, 1, "Enter"));

		Assert.True(opened);
		Assert.Equal("1", cut.Find("td[data-editing]").GetAttribute("data-col-index"));
	}

	[Fact]
	public async Task AKeyThatOpensNoEditor_SaysSo_SoTheCellKeepsItsKeys()
	{
		IRenderedComponent<MokaTable<Part>> cut = RenderParts([new Part("Bolt", 4, null)]);

		Assert.False(await cut.InvokeAsync(() => cut.Instance.OnCellKey(0, 0, "Enter")));
		Assert.False(await cut.InvokeAsync(() => cut.Instance.OnCellKey(7, 1, "Enter")));
		Assert.False(await cut.InvokeAsync(() => cut.Instance.OnCellKey(0, 1, " ")));
		Assert.Empty(cut.FindAll("td[data-editing]"));
	}

	[Fact]
	public async Task EnterWhileARowSaves_OpensNothing()
	{
		TaskCompletionSource saving = new();
		Part[] parts = [new Part("Bolt", 4, null), new Part("Nut", 9, null)];
		IRenderedComponent<MokaTable<Part>> cut = RenderParts(parts, async _ => await saving.Task);
		await cut.InvokeAsync(() => cut.Instance.StartEditAsync(parts[0], "Stock"));
		await cut.Find("td[data-editing] input").InputAsync(new ChangeEventArgs { Value = "5" });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));

		bool opened = await cut.InvokeAsync(() => cut.Instance.OnCellKey(1, 1, "Enter"));

		saving.SetResult();
		Assert.False(opened);
	}

	// What is typed on the cell waits for the editor, and reaches it as if typed there. Run in Node against the real
	// moka-table.js.
	[Fact]
	public void TheScript_HoldsTypingForTheEditor_AndTypesItOverTheSelection()
	{
		JsonElement? result = ScriptRunner.Run(
			["tests", "Moka.Red.Data.Tests", "Table", "Script", "table-held-keys.mjs"],
			["src", "Moka.Red.Data", "wwwroot", "moka-table.js"]);
		Assert.SkipUnless(result is not null, "Node.js isn't installed, so the table script can't be run.");
		JsonElement json = result!.Value;

		foreach (string typed in (string[])["letter", "space", "backspace", "emoji", "altGraphLetter"])
		{
			Assert.Equal("type", json.GetProperty(typed).GetString());
		}

		foreach (string ending in (string[])["enter", "escape", "tab"])
		{
			Assert.Equal("end", json.GetProperty(ending).GetString());
		}

		foreach (string staying in (string[])["heldEnter", "shiftEnter", "arrow", "f2"])
		{
			Assert.Equal("stay", json.GetProperty(staying).GetString());
		}

		foreach (string passing in (string[])["copy", "delete", "composing"])
		{
			Assert.Equal(JsonValueKind.Null, json.GetProperty(passing).ValueKind);
		}

		AssertTyped(json, "overSelection", "42", 2);
		AssertTyped(json, "withBackspace", "42", 2);
		AssertTyped(json, "backspaceFirst", "D", 1);
		AssertTyped(json, "atCaret", "Lamps", 5);
		AssertTyped(json, "backspacePastStart", "x", 1);
	}

	// ── A stream that breaks ──

	[Fact]
	public void AStreamThatBreaks_KeepsTheRowsThatCame_WithTheFailureAndRetryUnderThem()
	{
		List<Exception> reported = [];
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.ItemStream, Breaking(2, pause: true))
			.Add(t => t.OnLoadFailed, ex => reported.Add(ex))
			.Add(t => t.ChildContent, DefaultColumns));

		cut.WaitForAssertion(() => Assert.True(cut.Instance.HasLoadFailed));

		Assert.Equal(["Ada", "Grace"], ColumnTexts(cut, 0));
		Assert.False(cut.Instance.IsStreaming);
		Assert.Empty(cut.FindAll(".moka-table-stream-row"));
		IElement failure = cut.Find("tbody tr:last-child .moka-table-failure");
		Assert.Contains("The log was cut off.", failure.TextContent, StringComparison.Ordinal);
		Assert.Contains("Try again", failure.QuerySelector("button")!.TextContent, StringComparison.Ordinal);
		Assert.Equal("4", cut.Find("tbody tr:last-child").GetAttribute("aria-rowindex"));
		Assert.Equal("4", cut.Find("table").GetAttribute("aria-rowcount"));
		Assert.Equal("The log was cut off.", Assert.Single(reported).Message);
	}

	[Fact]
	public void RowsThatArrivedJustBeforeTheBreak_AreKeptToo()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.ItemStream, Breaking(3, pause: false))
			.Add(t => t.ChildContent, DefaultColumns));

		cut.WaitForAssertion(() => Assert.True(cut.Instance.HasLoadFailed));

		Assert.Equal(["Ada", "Grace", "Alan"], ColumnTexts(cut, 0));
		Assert.NotNull(cut.Find(".moka-table-failure"));
	}

	[Fact]
	public async Task SortingTheRowsThatCame_KeepsTheFailure()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.ItemStream, Breaking(2, pause: true))
			.Add(t => t.ChildContent, DefaultColumns));
		cut.WaitForAssertion(() => Assert.True(cut.Instance.HasLoadFailed));

		await HeaderCell(cut, "Name").ClickAsync(new MouseEventArgs());
		await HeaderCell(cut, "Name").ClickAsync(new MouseEventArgs());

		Assert.Equal(["Grace", "Ada"], ColumnTexts(cut, 0));
		Assert.NotNull(cut.Find("tbody tr:last-child .moka-table-failure"));
	}

	[Fact]
	public async Task Retry_ReadsTheStreamAgain_FromTheStart()
	{
		BreaksOnce source = new();
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.ItemStream, source.Read())
			.Add(t => t.ChildContent, DefaultColumns));
		cut.WaitForAssertion(() => Assert.True(cut.Instance.HasLoadFailed));
		Assert.Equal(["Ada"], ColumnTexts(cut, 0));

		await cut.Find(".moka-table-failure button").ClickAsync(new MouseEventArgs());

		cut.WaitForAssertion(() => Assert.Equal(["Ada", "Grace", "Alan", "Barbara"], ColumnTexts(cut, 0)));
		Assert.False(cut.Instance.HasLoadFailed);
		Assert.Empty(cut.FindAll(".moka-table-failure"));
		Assert.Equal(2, source.Reads);
	}

	[Fact]
	public void AStreamThatBreaksBeforeAnyRow_ShowsTheFailureInTheirPlace()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.ItemStream, Breaking(0, pause: true))
			.Add(t => t.ChildContent, DefaultColumns));

		cut.WaitForAssertion(() => Assert.True(cut.Instance.HasLoadFailed));

		Assert.Empty(DataRows(cut));
		Assert.Single(cut.FindAll("tbody tr"));
		Assert.Equal("2", cut.Find("tbody tr").GetAttribute("aria-rowindex"));
		Assert.NotNull(cut.Find(".moka-table-failure"));
	}

	// ── A Field over a nullable number ──

	[Fact]
	public async Task AFieldOverANullableNumber_OpensANumberBox_OnARowWithNoValue_AndSavesTheColumnsType()
	{
		Part[] parts = [new Part("Bolt", null, null), new Part("Nut", 12, 0.5m)];
		IRenderedComponent<MokaTable<Part>> cut = RenderParts(parts);

		await cut.InvokeAsync(() => cut.Instance.StartEditAsync(parts[0], "Stock"));
		Assert.NotNull(cut.Find("td[data-editing] input[inputmode='numeric']"));
		await cut.Find("td[data-editing] input").InputAsync(new ChangeEventArgs { Value = "7" });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));

		MokaTableRowSave<Part> save = Assert.Single(_saves);
		Assert.Equal(7, save.Updated.Stock);
		Assert.IsType<int>(Assert.Single(save.Changes).NewValue);
	}

	[Fact]
	public async Task ADecimalWithNoValue_OpensANumberBox_AndSavesADecimal()
	{
		Part[] parts = [new Part("Bolt", 1, null), new Part("Nut", 12, 0.5m)];
		IRenderedComponent<MokaTable<Part>> cut = RenderParts(parts);

		await cut.InvokeAsync(() => cut.Instance.StartEditAsync(parts[0], "Price"));
		Assert.NotNull(cut.Find("td[data-editing] input[inputmode='decimal']"));
		await cut.Find("td[data-editing] input").InputAsync(new ChangeEventArgs { Value = "2.5" });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));

		Assert.Equal(2.5m, Assert.Single(_saves).Updated.Price);
	}

	[Fact]
	public async Task AValue_CanBeCleared_WhenAnotherRowOfTheColumnIsEmpty()
	{
		Part[] parts = [new Part("Bolt", null, null), new Part("Nut", 12, 0.5m)];
		IRenderedComponent<MokaTable<Part>> cut = RenderParts(parts);

		await cut.InvokeAsync(() => cut.Instance.StartEditAsync(parts[1], "Stock"));
		await cut.Find("td[data-editing] input").InputAsync(new ChangeEventArgs { Value = "" });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));

		Assert.Null(Assert.Single(_saves).Updated.Stock);
	}

	[Fact]
	public async Task AValue_IsRequired_WhenNoRowOfTheColumnIsEmpty()
	{
		Part[] parts = [new Part("Bolt", 3, null), new Part("Nut", 12, 0.5m)];
		IRenderedComponent<MokaTable<Part>> cut = RenderParts(parts);

		await cut.InvokeAsync(() => cut.Instance.StartEditAsync(parts[1], "Stock"));
		await cut.Find("td[data-editing] input").InputAsync(new ChangeEventArgs { Value = "" });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));

		Assert.Empty(_saves);
		Assert.Contains("Enter a valid value for Stock", cut.Find("td[data-editing]").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public async Task AColumnNoRowGivesAValue_TakesTheTypeItsEditorEdits()
	{
		Part[] parts = [new Part("Bolt", null, null)];
		IRenderedComponent<MokaTable<Part>> cut = Render<MokaTable<Part>>(p => p
			.Add(t => t.Items, parts)
			.Add(t => t.OnRowSave, save => _saves.Add(save))
			.Add(t => t.ChildContent, (RenderFragment)(builder =>
			{
				builder.OpenComponent<MokaColumn<Part>>(0);
				builder.AddComponentParameter(1, "Title", "Stock");
				builder.AddComponentParameter(2, "Field", (Func<Part, object?>)(static x => x.Stock));
				builder.AddComponentParameter(3, "Editable", true);
				builder.AddComponentParameter(4, "Editor", MokaColumnEditor.Number);
				builder.CloseComponent();
			})));

		await cut.InvokeAsync(() => cut.Instance.StartEditAsync(parts[0], "Stock"));
		await cut.Find("td[data-editing] input").InputAsync(new ChangeEventArgs { Value = "7" });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));

		Assert.Equal(new MokaTableCellChange("Stock", null, 7.0), Assert.Single(Assert.Single(_saves).Changes));
	}

	private static void AssertTyped(JsonElement json, string name, string value, int caret)
	{
		JsonElement typed = json.GetProperty(name);
		Assert.Equal(value, typed.GetProperty("value").GetString());
		Assert.Equal(caret, typed.GetProperty("caret").GetInt32());
	}

	private static async IAsyncEnumerable<Person> Breaking(int rows, bool pause,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
		foreach (Person person in People.Take(rows))
		{
			cancellationToken.ThrowIfCancellationRequested();
			yield return person;
		}

		if (pause)
		{
			await Task.Yield();
		}

		throw new InvalidOperationException("The log was cut off.");
	}

	private IRenderedComponent<MokaTable<Part>> RenderParts(Part[] parts, Func<MokaTableRowSave<Part>, Task>? save = null) =>
		Render<MokaTable<Part>>(p => p
			.Add(t => t.Items, parts)
			.Add(t => t.OnRowSave, save ?? (row =>
			{
				_saves.Add(row);
				return Task.CompletedTask;
			}))
			.Add(t => t.ChildContent, (RenderFragment)PartColumns));

	private static void PartColumns(RenderTreeBuilder builder)
	{
		builder.OpenComponent<MokaColumn<Part>>(0);
		builder.AddComponentParameter(1, "Title", "Name");
		builder.AddComponentParameter(2, "Field", (Func<Part, object?>)(static x => x.Name));
		builder.CloseComponent();

		builder.OpenComponent<MokaColumn<Part>>(10);
		builder.AddComponentParameter(11, "Title", "Stock");
		builder.AddComponentParameter(12, "Field", (Func<Part, object?>)(static x => x.Stock));
		builder.AddComponentParameter(13, "Editable", true);
		builder.AddComponentParameter(14, "SetField", (Func<Part, object?, Part>)(static (p, v) => p with { Stock = (int?)v }));
		builder.CloseComponent();

		builder.OpenComponent<MokaColumn<Part>>(20);
		builder.AddComponentParameter(21, "Title", "Price");
		builder.AddComponentParameter(22, "Field", (Func<Part, object?>)(static x => x.Price));
		builder.AddComponentParameter(23, "Editable", true);
		builder.AddComponentParameter(24, "SetField", (Func<Part, object?, Part>)(static (p, v) => p with { Price = (decimal?)v }));
		builder.CloseComponent();
	}

	internal sealed record Part(string Name, int? Stock, decimal? Price);

	// Breaks after its first row the first time it is read, and gives every row the next.
	private sealed class BreaksOnce
	{
		public int Reads { get; private set; }

		public async IAsyncEnumerable<Person> Read([EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			Reads++;
			bool breaks = Reads == 1;
			foreach (Person person in breaks ? People.Take(1) : People)
			{
				cancellationToken.ThrowIfCancellationRequested();
				yield return person;
			}

			await Task.Yield();
			if (breaks)
			{
				throw new InvalidOperationException("The log was cut off.");
			}
		}
	}
}
