namespace Moka.Red.Mcp.Tests;

public sealed class TypeDescriptionsTests
{
	private static readonly ComponentCatalog Catalog = new();
	private static readonly DocsLibrary Docs = new();

	[Fact]
	public void PositionalRecord_IsWrittenWithItsConstructor()
	{
		string text = Describe("MokaTimeSlot");

		Assert.Contains("- `new MokaTimeSlot(DayOfWeek Day, int StartHour, int StartMinute, int DurationMinutes)`", text, StringComparison.Ordinal);
		Assert.Contains("Positional record: create it with `new MokaTimeSlot(Day, StartHour, StartMinute, DurationMinutes)`.", text, StringComparison.Ordinal);
		Assert.Contains("| `Day` (positional) | `DayOfWeek` |", text, StringComparison.Ordinal);
		Assert.Contains("| `DurationMinutes` (positional) | `int` |", text, StringComparison.Ordinal);
		Assert.DoesNotContain("(init)", text, StringComparison.Ordinal);
		Assert.DoesNotContain("new MokaTimeSlot(MokaTimeSlot", text, StringComparison.Ordinal);
	}

	[Fact]
	public void PositionalRecord_ShowsItsDefaultsAndOptionalArguments()
	{
		string text = Describe("MokaCodeDiagnostic");

		Assert.Contains(
			"- `new MokaCodeDiagnostic(MokaCodeSeverity Severity, string Message, int Line = 0, int Column = 0, int Length = 0, string? Code = null)`",
			text,
			StringComparison.Ordinal);
		Assert.Contains(
			"Positional record: create it with `new MokaCodeDiagnostic(Severity, Message)`, with Line, Column, Length and Code as optional arguments.",
			text,
			StringComparison.Ordinal);
		Assert.Contains("| `Code` (positional) | `string?` |", text, StringComparison.Ordinal);
	}

	[Fact]
	public void PositionalRecordWithInitProperties_MarksEachKind()
	{
		string text = Describe("MokaNavEntry");

		Assert.Contains("| `Group` (positional) | `string` |", text, StringComparison.Ordinal);
		Assert.Contains("| `Pill` (positional) | `MokaNavPill?` |", text, StringComparison.Ordinal);
		Assert.Contains("| `Bottom` (init) | `bool` |", text, StringComparison.Ordinal);
		Assert.Contains("| `Children` (init) | `IReadOnlyList<MokaNavEntry>` |", text, StringComparison.Ordinal);
		Assert.Contains("| `Href` (read-only) | `string` |", text, StringComparison.Ordinal);
		Assert.Contains(
			"Positional record: create it with `new MokaNavEntry(Group, Label, Route, Icon) { Bottom = ... }`, with Keywords and Pill as optional arguments and the (init) properties in the initializer.",
			text,
			StringComparison.Ordinal);
		Assert.True(
			text.IndexOf("## Constructors", StringComparison.Ordinal) < text.IndexOf("## Properties", StringComparison.Ordinal),
			"Constructors come before properties.");
	}

	[Fact]
	public void PlainClassWithAParameterlessConstructor_ReadsAsBefore()
	{
		string text = Describe("MokaToastOptions");

		Assert.StartsWith("# MokaToastOptions (class)", text, StringComparison.Ordinal);
		Assert.Equal("## Properties", text.Split('\n').Select(line => line.TrimEnd('\r')).First(line => line.StartsWith("## ", StringComparison.Ordinal)));
		Assert.Contains("| `Message` | `string` |", text, StringComparison.Ordinal);
		Assert.DoesNotContain("## Constructors", text, StringComparison.Ordinal);
		Assert.DoesNotContain("(positional)", text, StringComparison.Ordinal);
		Assert.DoesNotContain("Positional", text, StringComparison.Ordinal);
	}

	// Its constructor takes nothing and carries the compiler's [Obsolete] for required members.
	[Fact]
	public void RecordWithRequiredInitProperties_KeepsInitAndListsNoConstructor()
	{
		string text = Describe("MokaLineSeries");

		Assert.Contains("| `Name` (init) | `string` |", text, StringComparison.Ordinal);
		Assert.DoesNotContain("## Constructors", text, StringComparison.Ordinal);
	}

	[Fact]
	public void TypeThatIsNotARecord_ListsItsConstructorsWithoutAHint()
	{
		string text = Describe("MokaIconDefinition");

		Assert.Contains("- `new MokaIconDefinition(string name, string svgPath, string viewBox = \"0 0 24 24\", bool filled = false)`", text, StringComparison.Ordinal);
		Assert.DoesNotContain("Positional", text, StringComparison.Ordinal);
	}

	private static string Describe(string name)
	{
		Type type = Catalog.FindType(name) ?? throw new InvalidOperationException($"{name} isn't in the catalog.");
		return TypeDescriptions.Describe(type, Catalog, Docs);
	}
}
