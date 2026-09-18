using System.Reflection;
using System.Runtime.CompilerServices;
using Moka.Red.Core.Base;
using Moka.Red.Feedback.Toast;
using Moka.Red.Primitives.Button;

namespace Moka.Red.Feedback.Tests.Conventions;

public class AsyncVoidTests
{
	// An exception inside an async void method is rethrown on the thread pool where nothing can
	// catch it, which ends the process. Async lambdas passed as an Action compile to the same
	// thing, so compiler-generated methods are checked too.
	[Fact]
	public void LibraryAssemblies_HaveNoAsyncVoidMethods()
	{
		Assembly[] assemblies =
		[
			typeof(MokaComponentBase).Assembly,
			typeof(MokaButton).Assembly,
			typeof(MokaToastHost).Assembly
		];

		const BindingFlags everything = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
		                                BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

		string[] offenders = assemblies
			.SelectMany(assembly => assembly.GetTypes())
			.SelectMany(type => type.GetMethods(everything))
			.Where(method => method.ReturnType == typeof(void)
			                 && method.IsDefined(typeof(AsyncStateMachineAttribute), false))
			.Select(method => $"{method.DeclaringType!.FullName}.{method.Name}")
			.ToArray();

		Assert.Empty(offenders);
	}
}
