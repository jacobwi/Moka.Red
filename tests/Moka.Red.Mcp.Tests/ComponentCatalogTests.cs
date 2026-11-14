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

	[Theory]
	[InlineData("MokaCodeHighlighters", "Moka.Red.Core.Code")]
	[InlineData("MokaCodeSpan", "Moka.Red.Core.Code")]
	[InlineData("MokaCodeTokenKind", "Moka.Red.Core.Code")]
	[InlineData("MokaOklch", "Moka.Red.Core.Colors")]
	[InlineData("MokaCssColor", "Moka.Red.Core.Colors")]
	[InlineData("MokaContrast", "Moka.Red.Core.Colors")]
	[InlineData("IMokaAnnouncer", "Moka.Red.Core.Announcer")]
	[InlineData("MokaPoliteness", "Moka.Red.Core.Announcer")]
	[InlineData("MokaThemeSeed", "Moka.Red.Core.Theming")]
	[InlineData("MokaTokens", "Moka.Red.Core.Theming")]
	[InlineData("MokaContrastReport", "Moka.Red.Core.Theming")]
	[InlineData("MokaToastHandle", "Moka.Red.Feedback.Toast")]
	[InlineData("MokaToastProgress", "Moka.Red.Feedback.Toast")]
	[InlineData("MokaToastRepeats", "Moka.Red.Feedback.Toast")]
	[InlineData("MokaToastDismissReason", "Moka.Red.Feedback.Toast")]
	[InlineData("IMokaBannerService", "Moka.Red.Feedback.Banner")]
	[InlineData("MokaBanner", "Moka.Red.Feedback.Banner")]
	[InlineData("IMokaSystemNotifier", "Moka.Red.Feedback.SystemNotification")]
	[InlineData("MokaSystemNotification", "Moka.Red.Feedback.SystemNotification")]
	[InlineData("MokaTableState", "Moka.Red.Data.Table")]
	[InlineData("MokaTableRequest", "Moka.Red.Data.Table")]
	[InlineData("MokaTableResult", "Moka.Red.Data.Table")]
	[InlineData("MokaTableFilter", "Moka.Red.Data.Table")]
	[InlineData("MokaFilterOperator", "Moka.Red.Data.Table")]
	[InlineData("MokaColumnEditor", "Moka.Red.Data.Table")]
	[InlineData("MokaTableRowSave", "Moka.Red.Data.Table")]
	[InlineData("MokaDockState", "Moka.Red.Layout.DockLayout")]
	[InlineData("MokaThemePair", "Moka.Red.ThemeGen")]
	[InlineData("MokaNavEntry", "Moka.Red.Navigation.AppShell")]
	[InlineData("MokaInputValue", "Moka.Red.Core.Base")]
	public void FindType_FindsTheTypesNewIn060(string name, string space) =>
		Assert.Equal(space, Catalog.FindType(name)?.Namespace);

	[Theory]
	[InlineData("MokaThemeStudio")]
	[InlineData("MokaBannerHost")]
	[InlineData("MokaAnnouncerHost")]
	[InlineData("MokaDockLayout")]
	[InlineData("MokaDockGroup")]
	[InlineData("MokaDockRow")]
	[InlineData("MokaDockColumn")]
	[InlineData("MokaDockWindowPage")]
	[InlineData("MokaEditColumn")]
	[InlineData("MokaNumberColumn")]
	[InlineData("MokaColumnGroup")]
	[InlineData("MokaTablePager")]
	public void Find_FindsTheComponentsNewIn060(string name) =>
		Assert.Equal(name, Catalog.Find(name)?.Name);

	[Fact]
	public void NavEntries_HoldChildren() =>
		Assert.NotNull(Catalog.FindType("MokaNavEntry")?.GetProperty("Children"));

	// Parts marked [EditorBrowsable(Never)] are the library's own; an app never writes them.
	[Theory]
	[InlineData("MokaTableCellEditor")]
	[InlineData("MokaTableFilterPopup")]
	[InlineData("MokaChartFrame")]
	[InlineData("MokaDockFrame")]
	[InlineData("MokaDockGroupView")]
	public void PartsTheLibraryDrawsForItself_AreNotOffered(string name)
	{
		Assert.Null(Catalog.Find(name));
		Assert.Null(Catalog.FindType(name));
	}

	[Theory]
	[InlineData("MokaThemeEditor")]
	[InlineData("MokaPaletteEditor")]
	[InlineData("MokaTypographyEditor")]
	[InlineData("MokaSpacingEditor")]
	[InlineData("MokaTableCellEditResult")]
	public void RemovedApis_AreGone(string name)
	{
		Assert.Null(Catalog.Find(name));
		Assert.Null(Catalog.FindType(name));
	}

	[Theory]
	[InlineData("MokaTable", "ShowFilters")]
	[InlineData("MokaTable", "OnCellEdit")]
	[InlineData("MokaTable", "Layout")]
	[InlineData("MokaColumn", "Sticky")]
	[InlineData("MokaColumn", "OnCellEdited")]
	public void RemovedParameters_AreGone(string component, string parameter) =>
		Assert.DoesNotContain(Catalog.Find(component)!.Parameters, found => found.Name == parameter);

	[Fact]
	public void ToastOptions_HaveADurationAndNoDurationMs()
	{
		Type options = Catalog.FindType("MokaToastOptions")!;

		Assert.NotNull(options.GetProperty("Duration"));
		Assert.Null(options.GetProperty("DurationMs"));
	}

	// The Razor compiler reads [EditorRequired] off the declaration markup binds to, so an override without it is optional.
	[Fact]
	public void AnOverrideThatDropsEditorRequired_IsOptional()
	{
		Assert.True(Parameter(Catalog.Find("MokaActionsColumn")!, "Actions").Required);
		Assert.False(Parameter(Catalog.Find("MokaEditColumn")!, "Actions").Required);
	}

	private static ComponentParameter Parameter(MokaComponent component, string name) =>
		Assert.Single(component.Parameters, parameter => parameter.Name == name);
}
