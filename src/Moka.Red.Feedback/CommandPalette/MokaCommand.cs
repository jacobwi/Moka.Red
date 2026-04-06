using Moka.Red.Core.Icons;

namespace Moka.Red.Feedback.CommandPalette;

/// <summary>A single command/action in the palette.</summary>
public sealed class MokaCommand
{
	/// <summary>Unique identifier.</summary>
	public required string Id { get; init; }

	/// <summary>Display text (searchable).</summary>
	public required string Title { get; init; }

	/// <summary>Optional description (searchable).</summary>
	public string? Description { get; init; }

	/// <summary>Icon shown before title.</summary>
	public MokaIconDefinition? Icon { get; init; }

	/// <summary>Keyboard shortcut hint (e.g., "Ctrl+S").</summary>
	public string? Shortcut { get; init; }

	/// <summary>Category/group name for grouping in the palette.</summary>
	public string? Group { get; init; }

	/// <summary>Action to execute when selected.</summary>
	public Func<Task>? OnExecute { get; init; }

	/// <summary>Sync action alternative.</summary>
	public Action? OnExecuteSync { get; init; }

	/// <summary>Whether this command is currently disabled.</summary>
	public bool Disabled { get; init; }

	/// <summary>Keywords for search matching (not displayed).</summary>
	public string? Keywords { get; init; }

	/// <summary>URL to navigate to instead of executing an action.</summary>
	public string? Href { get; init; }
}
