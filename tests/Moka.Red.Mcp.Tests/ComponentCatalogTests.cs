using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;

namespace Moka.Red.Mcp.Tests;

public sealed class ComponentCatalogTests
{
	private static readonly ComponentCatalog Catalog = new();

	[Fact]
	public void ReadsTheComponentsOfEveryPackage()
	{
		string[] packages = [.. Catalog.Components.Select(component => component.Category).Distinct()];

		Assert.Superset(
			new HashSet<string>(["ContextMenu", "Core", "Data", "Diagnostics", "Feedback", "Forms", "Layout", "Navigation", "Primitives"]),
			new HashSet<string>(packages));
	}

	[Fact]
	public void RazorImportsAreNotComponents() =>
		Assert.DoesNotContain(Catalog.Components, component => component.Name.StartsWith('_'));

	[Theory]
	[InlineData("MokaButton")]
	[InlineData("Button")]
	[InlineData("mokabutton")]
	[InlineData(" MokaButton ")]
	public void Find_TakesTheNameWithOrWithoutThePrefix(string name) =>
		Assert.Equal("MokaButton", Catalog.Find(name)?.Name);

	[Fact]
	public void GenericComponents_KeepTheirTypeParameters()
	{
		MokaComponent? table = Catalog.Find("MokaTable<TItem>");

		Assert.NotNull(table);
		Assert.Equal("MokaTable<TItem>", table.DisplayName);
		Assert.Equal("IEnumerable<TItem>?", Parameter(table, "Items").TypeName);
	}

	[Fact]
	public void Parameters_HaveTheirKindTypeAndDefault()
	{
		MokaComponent button = Catalog.Find("MokaButton")!;

		ComponentParameter loading = Parameter(button, "Loading");
		Assert.Equal(("bool", ParameterKind.Value, "false"), (loading.TypeName, loading.Kind, loading.Default));
		Assert.Equal("\"Loading\"", Parameter(button, "LoadingText").Default);
		Assert.Equal(ParameterKind.Event, Parameter(button, "OnClick").Kind);
		Assert.Equal(ParameterKind.Content, Parameter(button, "ChildContent").Kind);
	}

	[Fact]
	public void InheritedParameters_ReadTheComponentsOwnDefault()
	{
		ComponentParameter variant = Parameter(Catalog.Find("MokaButton")!, "Variant");

		Assert.Equal("MokaVariant.Filled", variant.Default);
		Assert.Equal(typeof(MokaVariant), variant.EnumType);
	}

	[Fact]
	public void TheComponentBaseIsCommon() =>
		Assert.Contains(typeof(MokaComponentBase), Catalog.CommonBases);

	[Theory]
	[InlineData("MokaLineSeries")]
	[InlineData("IMokaToastService")]
	[InlineData("MokaVariant")]
	[InlineData("MokaIcons.Action")]
	public void FindType_FindsTypesThatAreNotComponents(string name) =>
		Assert.NotNull(Catalog.FindType(name));

	[Fact]
	public void FindType_LeavesComponentsToFind() =>
		Assert.Null(Catalog.FindType("MokaButton"));

	private static ComponentParameter Parameter(MokaComponent component, string name) =>
		Assert.Single(component.Parameters, parameter => parameter.Name == name);
}
