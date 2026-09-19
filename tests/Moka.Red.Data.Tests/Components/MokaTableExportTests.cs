using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.Table;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

public class MokaTableExportTests : BunitContext
{
	// A spreadsheet runs a cell that starts with =, +, -, @, a tab or a carriage return as a
	// formula, so row data like =HYPERLINK(...) ran when the export was opened (OWASP, CSV
	// injection). Those cells now start with a quote, unless they are plain numbers such as -5,
	// which hold no formula and stay numbers.
	[Fact]
	public async Task CsvExport_TurnsFormulaCellsIntoText()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;
		JSInterop.Mode = JSRuntimeMode.Loose;
		Person[] rows =
		[
			new("=HYPERLINK(\"https://example.com\")", "+1", -5, "@SUM(A1)"),
			new("\tTab", "\rReturn", 7, "-2+3+cmd|' /C calc'!A0")
		];
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, rows)
			.Add(t => t.Exportable, true)
			.Add(t => t.ChildContent, DefaultColumns));

		await cut.Find("button[title='Export']").ClickAsync(new MouseEventArgs());

		string csv = Encoding.UTF8.GetString(
			Convert.FromBase64String((string)module.VerifyInvoke("downloadCsv").Arguments[0]!));
		string[] lines = csv.Split(Environment.NewLine);
		Assert.Equal("Name,Team,Age,City", lines[0]);
		Assert.Equal("\"'=HYPERLINK(\"\"https://example.com\"\")\",+1,-5,'@SUM(A1)", lines[1]);
		Assert.Equal("'\tTab,\"'\rReturn\",7,'-2+3+cmd|' /C calc'!A0", lines[2]);
	}
}
