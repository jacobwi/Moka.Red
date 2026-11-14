using System.Reflection;
using System.Runtime.CompilerServices;

namespace Moka.Red.Feedback.Tests.Conventions;

public class AsyncVoidTests
{
	// An exception inside an async void method is rethrown on the thread pool where nothing can
	// catch it, which ends the process. Async lambdas passed as an Action or a TimerCallback compile
	// to the same thing, so compiler-generated methods are checked too. Hand work like that to
	// RunDetached in the base classes.
	[Fact]
	public void NoPackage_HasAsyncVoidMethods()
	{
		const BindingFlags everything = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
		                                BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

		string[] offenders = PackageAssemblies()
			.SelectMany(assembly => assembly.GetTypes())
			.SelectMany(type => type.GetMethods(everything))
			.Where(method => method.ReturnType == typeof(void)
			                 && method.IsDefined(typeof(AsyncStateMachineAttribute), false))
			.Select(method => $"{method.DeclaringType!.FullName}.{method.Name}")
			.ToArray();

		Assert.Empty(offenders);
	}

	// Only a package this test project references is in its output folder, and MokaTable's async void
	// timer went unseen for as long as Data was not on the list.
	[Fact]
	public void TheScan_CoversEveryPackage()
	{
		string[] packages = Directory.GetDirectories(Path.Combine(FindRepoRoot(), "src"))
			.Select(Path.GetFileName)
			.Order(StringComparer.Ordinal)
			.ToArray()!;

		Assert.Equal(packages, PackageAssemblies().Select(assembly => assembly.GetName().Name!).Order(StringComparer.Ordinal));
	}

	internal static IEnumerable<Assembly> PackageAssemblies() =>
		Directory.GetFiles(AppContext.BaseDirectory, "Moka.Red*.dll")
			.Select(Path.GetFileNameWithoutExtension)
			.Where(name => !name!.EndsWith(".Tests", StringComparison.Ordinal))
			.Select(name => Assembly.Load(name!));

	private static string FindRepoRoot()
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
