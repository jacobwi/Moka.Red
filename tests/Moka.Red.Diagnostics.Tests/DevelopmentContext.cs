using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Moka.Red.Diagnostics.Tests;

/// <summary>
///     A bUnit context in the Development environment. bUnit registers a WebAssembly host environment that says
///     Production, and the diagnostics stay off outside Development, so tests of what they show run here.
/// </summary>
public abstract class DevelopmentContext : BunitContext
{
	protected DevelopmentContext() => Services.AddSingleton<IHostEnvironment>(new TestHostEnvironment(Environments.Development));
}

/// <summary>A server host environment with the given name.</summary>
public sealed class TestHostEnvironment(string name) : IHostEnvironment
{
	public string EnvironmentName { get; set; } = name;

	public string ApplicationName { get; set; } = "Moka.Red.Diagnostics.Tests";

	public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

	public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
