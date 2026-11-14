using Moka.Red.Core.Theming;
using Moka.Red.Data.Table;

namespace Moka.Red.Core.Tests.Theming;

// The table's look and density as app-wide choices: new design only, unset by default so tables keep their own.
public sealed class TableChoiceTests
{
	[Fact]
	public void Tables_AreUnset_UntilAnAppSaysOtherwise()
	{
		Assert.Null(MokaTheme.Red.Tables);
		Assert.Null(MokaTheme.Red.TableDensity);
	}

	[Fact]
	public void WithTables_SetsTheLook_OnTheNewDesign()
	{
		MokaTheme theme = MokaTheme.Red.WithTables(MokaTableLook.SoftStripes).WithTableDensity(MokaTableDensity.Comfortable);

		Assert.Equal(MokaTableLook.SoftStripes, theme.Tables);
		Assert.Equal(MokaTableDensity.Comfortable, theme.TableDensity);
	}

	[Fact]
	public void TheTableChoices_DoNothing_OnTheOriginalDesign()
	{
		foreach (MokaTheme original in new[] { MokaTheme.Dark, MokaTheme.Light })
		{
			Assert.Same(original, original.WithTables(MokaTableLook.Cards));
			Assert.Same(original, original.WithTableDensity(MokaTableDensity.Standard));
		}
	}

	[Fact]
	public void TheTableChoices_WriteNoToken()
	{
		Assert.Equal(MokaTheme.Red.ToCssVariables(), MokaTheme.Red.WithTables(MokaTableLook.Sheet).ToCssVariables());
		Assert.Equal(MokaTheme.Red.ToCssVariables(), MokaTheme.Red.WithTableDensity(MokaTableDensity.Comfortable).ToCssVariables());
	}

	// The enums moved to Core with a forward in the table's package, so code built against it still finds them.
	[Fact]
	public void TheEnums_LiveInCore_UnderTheTablesNamespace()
	{
		Assert.Equal(typeof(MokaTheme).Assembly, typeof(MokaTableLook).Assembly);
		Assert.Equal(typeof(MokaTheme).Assembly, typeof(MokaTableDensity).Assembly);
		Assert.Equal("Moka.Red.Data.Table", typeof(MokaTableLook).Namespace);
	}
}
