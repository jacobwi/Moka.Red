using System.Diagnostics.CodeAnalysis;

namespace Moka.Red.Core.PageTitle;

/// <summary>
///     What the page showing calls itself, between each <see cref="MokaPageTitle" /> and the one
///     <see cref="MokaPageTitleHost" /> in the layout. Pages say their name here rather than with their own
///     <c>PageTitle</c>, where the last one rendered wins, so the layout can put something in front of it.
/// </summary>
[SuppressMessage("Design", "CA1003:Use generic event handler instances",
	Justification = "A parameterless Action is simpler for this lightweight state-changed event.")]
public interface IMokaPageTitleService
{
	/// <summary>The name last said, or <see langword="null" /> when nothing on the page has said one.</summary>
	string? Text { get; }

	/// <summary>Raised when <see cref="Text" /> changes.</summary>
	event Action? OnChanged;

	/// <summary>Says a name. An owner that already said one replaces it where it stands.</summary>
	/// <param name="owner">Who is saying it, so only they can take it back.</param>
	/// <param name="text">The name.</param>
	void Say(object owner, string text);

	/// <summary>
	///     Takes a name back, and the one said before it counts again. A page says its name before the page it replaces
	///     is disposed, so taking back a name that is no longer the last changes nothing.
	/// </summary>
	/// <param name="owner">Who said it.</param>
	void Forget(object owner);
}
