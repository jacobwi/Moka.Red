using Moka.Red.Primitives.Badge;

namespace Moka.Red.Navigation.AppShell;

/// <summary>A number beside a menu entry. Show one only when it means something.</summary>
/// <param name="Count">The number.</param>
/// <param name="Tone">What kind of attention it wants.</param>
/// <param name="Why">
///     What the number means, in words: "2 blocked in the last hour". The rail's tag shows it and screen readers say it.
/// </param>
public sealed record MokaNavPill(int Count, MokaBadgeTone Tone, string Why);
