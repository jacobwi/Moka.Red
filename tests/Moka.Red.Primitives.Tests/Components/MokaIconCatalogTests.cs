using System.Reflection;
using Moka.Red.Core.Icons;
using Moka.Red.Icons;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaIconCatalogTests
{
	private static List<MokaIconDefinition> AllIcons() => typeof(MokaIcons).GetNestedTypes()
		.SelectMany(group => group.GetFields(BindingFlags.Public | BindingFlags.Static))
		.Where(field => field.FieldType == typeof(MokaIconDefinition))
		.Select(field => (MokaIconDefinition)field.GetValue(null)!)
		.ToList();

	[Fact]
	public void EveryIcon_HasANameAndAPath()
	{
		List<MokaIconDefinition> icons = AllIcons();

		Assert.NotEmpty(icons);
		Assert.All(icons, icon =>
		{
			Assert.False(string.IsNullOrWhiteSpace(icon.Name));
			Assert.False(string.IsNullOrWhiteSpace(icon.SvgPath), icon.Name);
		});
	}

	[Fact]
	public void Scale_IsInTheCatalog() => Assert.Contains(AllIcons(), icon => icon.Name == "scale" && icon == MokaIcons.Content.Scale);

	[Fact]
	public void IconNames_AreUniqueAcrossTheCatalog()
	{
		List<string> repeatedNames = AllIcons()
			.GroupBy(icon => icon.Name, StringComparer.Ordinal)
			.Where(names => names.Count() > 1)
			.Select(names => names.Key)
			.ToList();

		Assert.Empty(repeatedNames);
	}
}
