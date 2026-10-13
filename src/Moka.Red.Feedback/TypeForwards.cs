using System.Runtime.CompilerServices;
using Moka.Red.Feedback.Tooltip;

// MokaTooltip lives in Moka.Red.Primitives since 0.1.20, under the namespace it always had, because the icon buttons
// Primitives draws need it and Primitives cannot reference this package, which references Primitives. Layout, Forms
// and Navigation reach it there too, without a dependency on Feedback. These forwards keep code compiled against
// 0.1.19, which looks for both types in this assembly, working unchanged.
[assembly: TypeForwardedTo(typeof(MokaTooltip))]
[assembly: TypeForwardedTo(typeof(MokaTooltipPosition))]
