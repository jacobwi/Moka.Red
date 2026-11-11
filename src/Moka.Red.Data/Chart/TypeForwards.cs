using System.Runtime.CompilerServices;
using Moka.Red.Data.Chart;

// MokaSparkline lives in Moka.Red.Primitives since the chart work, under the namespace it always had, because
// MokaStat draws one and Primitives cannot reference this package. The forward keeps code compiled against an older
// Moka.Red.Data, which looks for the type in this assembly, working unchanged.
[assembly: TypeForwardedTo(typeof(MokaSparkline))]
