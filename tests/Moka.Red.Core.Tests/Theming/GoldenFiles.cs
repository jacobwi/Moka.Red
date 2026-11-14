namespace Moka.Red.Core.Tests.Theming;

// Text a theme writes, kept in Theming/Goldens. MOKA_RECORD_GOLDENS=1 writes the files again after a change meant to show.
internal static class GoldenFiles
{
	private const string RecordVariable = "MOKA_RECORD_GOLDENS";

	public static void Check(string name, string text)
	{
		string file = Path.Combine(Folder(), $"{name}.txt");

		if (Environment.GetEnvironmentVariable(RecordVariable) == "1")
		{
			File.WriteAllText(file, text.Replace("\n", "\r\n", StringComparison.Ordinal));
			return;
		}

		Assert.True(File.Exists(file), $"No golden for {name}. Run the tests once with {RecordVariable}=1 to write it.");
		Assert.Equal(File.ReadAllText(file).ReplaceLineEndings("\n"), text);
	}

	private static string Folder()
	{
		for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
		{
			if (File.Exists(Path.Combine(folder.FullName, "Moka.Red.slnx")))
			{
				return Path.Combine(folder.FullName, "tests", "Moka.Red.Core.Tests", "Theming", "Goldens");
			}
		}

		throw new DirectoryNotFoundException("The repository root holding Moka.Red.slnx was not found.");
	}
}
