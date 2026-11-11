using Microsoft.AspNetCore.Components;

namespace Moka.Red.Core.Keys;

/// <summary>
///     Registers <c>@onmokakeydown</c>, a keydown that says who owns the press (<see cref="MokaKeyEventArgs" />).
///     The browser side is registered by this package's JavaScript initializer. Razor finds this class by its name.
/// </summary>
[EventHandler("onmokakeydown", typeof(MokaKeyEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
public static class EventHandlers;
