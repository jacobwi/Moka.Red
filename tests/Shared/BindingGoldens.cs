using System.Globalization;
using System.Text.RegularExpressions;

namespace Moka.Red.Tests.Shared;

// The whole markup of a component compared with a file under tests/<project>/Goldens/Binding, rendered in the invariant
// culture with scope attributes and generated ids taken out. The files hold the markup from before the inputs moved to
// @bind:get and @bind:set, so a change is either one of those or a mistake. Set MOKA_RECORD_GOLDENS=1 to write them
// again after a change that is meant to show.
internal static partial class BindingGoldens
{
	private const string RecordVariable = "MOKA_RECORD_GOLDENS";

	public static void Check(string project, string name, Func<string> render)
	{
		CultureInfo culture = CultureInfo.CurrentCulture;
		CultureInfo uiCulture = CultureInfo.CurrentUICulture;
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
		try
		{
			string markup = Normalize(render());
			string file = Path.Combine(Folder(project), $"{name}.html");

			if (Environment.GetEnvironmentVariable(RecordVariable) == "1")
			{
				File.WriteAllText(file, markup);
				return;
			}

			Assert.True(File.Exists(file), $"No golden for {name}. Run the tests once with {RecordVariable}=1 to write it.");
			Assert.Equal(File.ReadAllText(file).ReplaceLineEndings("\n"), markup);
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = uiCulture;
		}
	}

	private static string Normalize(string markup)
	{
		string text = ScopeAttribute().Replace(markup, string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal);
		text = DashedGuid().Replace(text, "{guid}");
		return PlainGuid().Replace(text, "{guid}");
	}

	private static string Folder(string project)
	{
		for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
		{
			if (File.Exists(Path.Combine(folder.FullName, "Moka.Red.slnx")))
			{
				string goldens = Path.Combine(folder.FullName, "tests", project, "Goldens", "Binding");
				Directory.CreateDirectory(goldens);
				return goldens;
			}
		}

		Assert.Fail("The tests are not running inside the repository, so there are no goldens to read.");
		return string.Empty;
	}

	[GeneratedRegex(@" b-[a-z0-9]{10}(="""")?")]
	private static partial Regex ScopeAttribute();

	[GeneratedRegex("[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}")]
	private static partial Regex DashedGuid();

	[GeneratedRegex("[0-9a-f]{32}")]
	private static partial Regex PlainGuid();
}
