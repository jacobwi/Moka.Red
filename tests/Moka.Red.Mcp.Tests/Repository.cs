namespace Moka.Red.Mcp.Tests;

internal static class Repository
{
	internal static string Root()
	{
		for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
		{
			if (File.Exists(Path.Combine(folder.FullName, "Moka.Red.slnx")))
			{
				return folder.FullName;
			}
		}

		throw new InvalidOperationException("Moka.Red.slnx not found above the test output.");
	}
}
