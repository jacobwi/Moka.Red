using System.Diagnostics;
using System.Text.Json;

namespace Moka.Red.Layout.Tests.Components;

// Runs Script/dock-layout.mjs in Node.js against the real MokaDockLayout.razor.js and reads the JSON it prints. Answers
// null where Node.js isn't installed, and the tests skip.
internal static class DockScriptRunner
{
	public static JsonElement? Run()
	{
		string? node = Node();
		if (node is null)
		{
			return null;
		}

		string root = RepositoryRoot();
		ProcessStartInfo start = new(node)
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false
		};
		start.ArgumentList.Add(Path.Combine(root, "tests", "Moka.Red.Layout.Tests", "Components", "Script", "dock-layout.mjs"));
		start.ArgumentList.Add(Path.Combine(root, "src", "Moka.Red.Layout", "DockLayout", "MokaDockLayout.razor.js"));

		using Process process = Process.Start(start) ?? throw new InvalidOperationException("Node.js didn't start.");
		Task<string> output = process.StandardOutput.ReadToEndAsync();
		Task<string> errors = process.StandardError.ReadToEndAsync();
		if (!process.WaitForExit(60_000))
		{
			process.Kill(true);
			throw new TimeoutException("The dock layout script didn't finish.");
		}

		if (process.ExitCode != 0)
		{
			throw new InvalidOperationException($"The dock layout script failed: {errors.Result}");
		}

		using JsonDocument document = JsonDocument.Parse(output.Result);
		return document.RootElement.Clone();
	}

	private static string? Node()
	{
		string name = OperatingSystem.IsWindows() ? "node.exe" : "node";
		foreach (string directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator))
		{
			string candidate = Path.Combine(directory.Trim('"'), name);
			if (directory.Length > 0 && File.Exists(candidate))
			{
				return candidate;
			}
		}

		return null;
	}

	private static string RepositoryRoot()
	{
		string? directory = AppContext.BaseDirectory;
		while (directory is not null && !File.Exists(Path.Combine(directory, "Moka.Red.slnx")))
		{
			directory = Path.GetDirectoryName(directory);
		}

		return directory ?? throw new InvalidOperationException("No Moka.Red.slnx above the test's folder.");
	}
}
