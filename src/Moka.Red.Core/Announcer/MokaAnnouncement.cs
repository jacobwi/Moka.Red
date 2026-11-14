namespace Moka.Red.Core.Announcer;

/// <summary>One thing said to screen readers through <see cref="IMokaAnnouncer" />.</summary>
/// <param name="Number">Counts up from 1 for every announcement, so the same words said twice are two announcements.</param>
/// <param name="Text">The words.</param>
/// <param name="Politeness">How urgently they are spoken.</param>
public sealed record MokaAnnouncement(long Number, string Text, MokaPoliteness Politeness);
