using System.Diagnostics;
using System.Text.Json;

namespace Moka.Red.Tests.Shared;

/// <summary>
///     Runs a test script in Node.js against one of the package's real scripts and reads the JSON it prints. Answers null
///     where Node.js isn't installed, and the test skips. Linked into the test projects that need it.
/// </summary>
internal static class ScriptRunner
{
	/// <summary>Runs a script from <c>Charts/Script</c> with <c>moka-chart.js</c>'s path and any further arguments.</summary>
	/// <param name="script">The script's file name in <c>Charts/Script</c>.</param>
	/// <param name="arguments">What the script takes after the chart script's path.</param>
	/// <returns>What it printed, or null without Node.js.</returns>
	public static JsonElement? Run(string script, params string[] arguments) =>
		Run(["tests", "Moka.Red.Data.Tests", "Charts", "Script", script], ["src", "Moka.Red.Data", "wwwroot", "moka-chart.js"], arguments);

	/// <summary>Runs a script with a package script's path and any further arguments, both given from the repository root.</summary>
	/// <param name="script">The test script's path, folder by folder.</param>
	/// <param name="module">The package script's path, folder by folder.</param>
	/// <param name="arguments">What the test script takes after the package script's path.</param>
	/// <returns>What it printed, or null without Node.js.</returns>
	public static JsonElement? Run(string[] script, string[] module, params string[] arguments)
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
		start.ArgumentList.Add(Path.Combine([root, .. script]));
		start.ArgumentList.Add(Path.Combine([root, .. module]));
		foreach (string argument in arguments)
		{
			start.ArgumentList.Add(argument);
		}

		using Process process = Process.Start(start) ?? throw new InvalidOperationException("Node.js didn't start.");
		Task<string> output = process.StandardOutput.ReadToEndAsync();
		Task<string> errors = process.StandardError.ReadToEndAsync();
		if (!process.WaitForExit(60_000))
		{
			process.Kill(true);
			throw new TimeoutException("The script didn't finish.");
		}

		if (process.ExitCode != 0)
		{
			throw new InvalidOperationException($"The script failed: {errors.Result}");
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
