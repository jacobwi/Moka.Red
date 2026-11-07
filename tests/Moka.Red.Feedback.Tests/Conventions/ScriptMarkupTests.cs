namespace Moka.Red.Feedback.Tests.Conventions;

/// <summary>
///     A script in a component's markup is one the browser parses only in a static render. In an interactive render
///     Blazor creates it, setting its text or its <c>src</c>, and both are Trusted Types sinks: under a policy that
///     requires them, MokaArk's whole signed-in UI stopped rendering on MokaAppShell's. So the library writes a script
///     only in a static render, and every file that writes one is listed here.
/// </summary>
public sealed class ScriptMarkupTests
{
	private static readonly string[] FilesWithAScript =
	[
		Path.Combine("Moka.Red.Feedback", "Reconnect", "MokaReconnectModal.razor"),
		Path.Combine("Moka.Red.Navigation", "AppShell", "MokaAppShell.razor.cs")
	];

	private const string StaticOnly = "@if (!RendererInfo.IsInteractive)";

	[Fact]
	public void OnlyTheListedFiles_WriteAScript()
	{
		string src = Path.Combine(RepositoryRoot(), "src");
		List<string> found = Directory.EnumerateFiles(src, "*.*", SearchOption.AllDirectories)
			.Where(static file => file.EndsWith(".razor", StringComparison.Ordinal) || file.EndsWith(".cs", StringComparison.Ordinal))
			.Select(file => Path.GetRelativePath(src, file))
			.Where(static relative => !relative.Split(Path.DirectorySeparatorChar).Any(static part => part is "bin" or "obj"))
			.Where(relative => File.ReadAllText(Path.Combine(src, relative)).Contains("<script", StringComparison.OrdinalIgnoreCase))
			.Order(StringComparer.Ordinal)
			.ToList();

		Assert.Equal(FilesWithAScript.Order(StringComparer.Ordinal), found);
	}

	[Theory]
	[InlineData("Moka.Red.Feedback/Reconnect/MokaReconnectModal.razor", "<script")]
	[InlineData("Moka.Red.Navigation/AppShell/MokaAppShell.razor", "(MarkupString)BootMarkup")]
	public void EachScript_IsWrittenOnlyByAStaticRender(string file, string written)
	{
		string text = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", file));
		int at = text.IndexOf(written, StringComparison.Ordinal);
		Assert.True(at >= 0, $"{file} no longer writes {written}.");

		string before = text[..at];
		int gate = before.LastIndexOf(StaticOnly, StringComparison.Ordinal);
		Assert.True(gate >= 0, $"{file} writes {written} outside {StaticOnly}.");

		// Inside the gate's block: it opened, and nothing closed it before the script.
		string between = before[(gate + StaticOnly.Length)..];
		Assert.Equal(1, between.Count(static c => c == '{'));
		Assert.Equal(0, between.Count(static c => c == '}'));
	}

	private static string RepositoryRoot()
	{
		for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
		{
			if (File.Exists(Path.Combine(folder.FullName, "Moka.Red.slnx")))
			{
				return folder.FullName;
			}
		}

		Assert.Fail("The tests are not running inside the repository, so there are no source files to read.");
		return "";
	}
}
