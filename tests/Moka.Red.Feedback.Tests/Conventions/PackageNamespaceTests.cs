namespace Moka.Red.Feedback.Tests.Conventions;

public class PackageNamespaceTests
{
	// A type that moves to a lower package takes that package's namespace, so its name says which package to reference.
	// A type forward would keep the old name working only by keeping the wrong namespace, so no package has one.
	[Fact]
	public void EveryPublicType_IsNamedAfterItsPackage()
	{
		string[] offenders = AsyncVoidTests.PackageAssemblies()
			.SelectMany(assembly => assembly.GetExportedTypes()
				.Where(type => !$"{type.Namespace}.".StartsWith($"{assembly.GetName().Name}.", StringComparison.Ordinal))
				.Select(type => $"{type.FullName} in {assembly.GetName().Name}"))
			.ToArray();

		Assert.Empty(offenders);
	}

	[Fact]
	public void NoPackage_ForwardsAType() =>
		Assert.Empty(AsyncVoidTests.PackageAssemblies().SelectMany(assembly => assembly.GetForwardedTypes()));
}
