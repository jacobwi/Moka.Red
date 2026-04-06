using Microsoft.AspNetCore.Components;

namespace Moka.Red.Feedback.Dialog;

/// <summary>
///     Internal record carrying data for a service-triggered dialog.
/// </summary>
public sealed record MokaDialogRequest
{
	/// <summary>Dialog title.</summary>
	public string? Title { get; init; }

	/// <summary>Simple text message (used by Confirm/Prompt).</summary>
	public string? Message { get; init; }

	/// <summary>Rich content (used by ShowAsync).</summary>
	public RenderFragment? Content { get; init; }

	/// <summary>Configuration options.</summary>
	public required MokaDialogOptions Options { get; init; }

	/// <summary>The type of dialog being requested.</summary>
	public required MokaDialogType Type { get; init; }

	/// <summary>Default value for prompt dialogs.</summary>
	public string? DefaultValue { get; init; }

	/// <summary>Component type to render (for Component dialogs).</summary>
	public Type? ComponentType { get; init; }

	/// <summary>Parameters to pass to the component (for Component dialogs).</summary>
	public Dictionary<string, object>? ComponentParameters { get; init; }

	/// <summary>Completion source for async dialog results.</summary>
	public TaskCompletionSource<object?>? Completion { get; init; }
}

/// <summary>
///     The type of service-triggered dialog.
/// </summary>
public enum MokaDialogType
{
	/// <summary>A confirmation dialog returning bool.</summary>
	Confirm,

	/// <summary>A prompt dialog returning string?.</summary>
	Prompt,

	/// <summary>A custom content dialog.</summary>
	Show,

	/// <summary>A dialog rendering a typed component.</summary>
	Component
}
