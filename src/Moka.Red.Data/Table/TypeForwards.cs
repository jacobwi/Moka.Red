using System.Runtime.CompilerServices;
using Moka.Red.Data.Table;

// The table's look and density live in Moka.Red.Core, under the namespace they always had, because the theme names them
// (MokaTheme.WithTables, WithTableDensity) and Core cannot reference this package. The forwards keep code compiled against
// this package, which looks for the types here, working unchanged.
[assembly: TypeForwardedTo(typeof(MokaTableLook))]
[assembly: TypeForwardedTo(typeof(MokaTableDensity))]
