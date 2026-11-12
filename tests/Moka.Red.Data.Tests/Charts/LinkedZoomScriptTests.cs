using System.Diagnostics;
using System.Text.Json;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     What moka-chart.js tells .NET when charts link their zoom, run in Node.js over a small stand-in for the DOM
///     (<c>Script/linked-zoom.mjs</c>). Skipped where Node.js isn't installed.
/// </summary>
public sealed class LinkedZoomScriptTests
{
	private static readonly Lazy<JsonElement?> Result = new(Run);

	[Fact]
	public void AKeyZoom_TellsItsOwnChartAndTheLinkedOne_TheSameRangeOnce()
	{
		JsonElement calls = Results().GetProperty("key").GetProperty("calls");

		JsonElement own = Assert.Single(calls.GetProperty("a").EnumerateArray());
		JsonElement linked = Assert.Single(calls.GetProperty("b").EnumerateArray());
		Assert.Equal("OnRange", own[0].GetString());
		Assert.Equal(own.GetRawText(), linked.GetRawText());
		Assert.True(own[1].GetDouble() > 1000 && own[2].GetDouble() < 2000);
	}

	[Fact]
	public void ALinkedChart_StretchesWhileTheGestureRuns_AndChartsThatDontMatchHearNothing()
	{
		JsonElement key = Results().GetProperty("key");

		Assert.True(key.GetProperty("stretched").GetBoolean());
		Assert.Empty(key.GetProperty("calls").GetProperty("c").EnumerateArray());
		Assert.Empty(key.GetProperty("calls").GetProperty("d").EnumerateArray());
	}

	[Fact]
	public void AZoomChosenOnTheServer_GoesOnlyToTheOthers_AndSoDoesAReset()
	{
		JsonElement share = Results().GetProperty("share");

		Assert.Empty(share.GetProperty("zoomed").GetProperty("a").EnumerateArray());
		Assert.Equal("[[\"OnRange\",20,40]]", share.GetProperty("zoomed").GetProperty("b").GetRawText());
		Assert.Empty(share.GetProperty("reset").GetProperty("a").EnumerateArray());
		Assert.Equal("[[\"OnRange\",20,40],[\"OnRange\",null,null]]", share.GetProperty("reset").GetProperty("b").GetRawText());
	}

	private static JsonElement Results()
	{
		Assert.SkipUnless(Result.Value is not null, "Node.js isn't installed, so the chart script can't be run.");
		return Result.Value!.Value;
	}

	private static JsonElement? Run()
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
		start.ArgumentList.Add(Path.Combine(root, "tests", "Moka.Red.Data.Tests", "Charts", "Script", "linked-zoom.mjs"));
		start.ArgumentList.Add(Path.Combine(root, "src", "Moka.Red.Data", "wwwroot", "moka-chart.js"));

		using Process process = Process.Start(start) ?? throw new InvalidOperationException("Node.js didn't start.");
		Task<string> output = process.StandardOutput.ReadToEndAsync();
		Task<string> errors = process.StandardError.ReadToEndAsync();
		if (!process.WaitForExit(60_000))
		{
			process.Kill(true);
			throw new TimeoutException("The chart script didn't finish.");
		}

		if (process.ExitCode != 0)
		{
			throw new InvalidOperationException($"The chart script failed: {errors.Result}");
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
