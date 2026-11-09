using System.Runtime.CompilerServices;
using Moka.Red.ThemeGen.Appearance;

// MokaColorMode moved to Moka.Red.Core for MokaThemeToggle in Primitives, under the namespace it always had. This keeps
// code compiled against the older package, which looks for the type in this assembly, working unchanged.
[assembly: TypeForwardedTo(typeof(MokaColorMode))]
