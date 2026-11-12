using Microsoft.AspNetCore.Components;

namespace Moka.Red.Core.Clipboard;

/// <summary>
///     Registers <c>@onmokacopied</c> (<see cref="MokaCopyEventArgs" />), raised once a copy made inside a click has
///     settled, and <c>@onmokacopyask</c>, raised by an element carrying <c>data-moka-copy-ask</c> when it needs its text
///     from .NET, which answers through <c>answerCopy(element, text)</c> in <c>moka-copy.js</c>. The browser side is
///     registered by this package's JavaScript initializer. Razor finds this class by its name.
/// </summary>
[EventHandler("onmokacopied", typeof(MokaCopyEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("onmokacopyask", typeof(EventArgs), enableStopPropagation: true, enablePreventDefault: true)]
public static class EventHandlers;
