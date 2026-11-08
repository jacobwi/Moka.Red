using System.Runtime.CompilerServices;
using Bunit;

namespace Moka.Red.Tests.Shared;

internal static class BunitWaits
{
	// bUnit's blocking WaitFor calls each hold a pool thread while xunit runs tests in parallel. On a loaded machine
	// the renderer doesn't get to run a check inside bUnit's one second timeout, and whichever test is waiting fails
	// (the 0.3.13 publish run). Both settings are needed: raising the thread floor alone only halved the failures.
	// A longer timeout costs nothing on a test that passes, since a wait ends as soon as its condition holds.
	[ModuleInitializer]
	internal static void Apply()
	{
		ThreadPool.GetMinThreads(out int workers, out int io);
		ThreadPool.SetMinThreads(Math.Max(workers, Environment.ProcessorCount * 4), io);
		BunitContext.DefaultWaitTimeout = TimeSpan.FromSeconds(15);
	}
}
