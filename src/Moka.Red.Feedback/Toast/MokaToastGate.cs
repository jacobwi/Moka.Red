using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Moka.Red.Feedback.Toast;

/// <summary>
///     One toast, drawn only when its record or the host's settings changed. A toast reporting progress changes many
///     times a second while the others stand still, so without this every update would redraw every toast and its
///     tooltip. The markup stays in <see cref="MokaToastHost" />, where its scoped CSS reaches it.
/// </summary>
internal sealed class MokaToastGate : ComponentBase
{
	private bool _changed = true;
	private MokaToastMessage? _drawnToast;
	private int _drawnSettings = -1;

	/// <summary>The toast. A new instance is a changed toast, since the service never changes one in place.</summary>
	[Parameter]
	public MokaToastMessage? Toast { get; set; }

	/// <summary>Moves on whenever something every toast reads has changed.</summary>
	[Parameter]
	public int Settings { get; set; }

	/// <summary>The toast's markup.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Opens a gate on <paramref name="builder" />, keyed by the toast's id.</summary>
	internal static void Write(RenderTreeBuilder builder, MokaToastMessage toast, int settings, RenderFragment content)
	{
		builder.OpenComponent<MokaToastGate>(0);
		builder.SetKey(toast.Id);
		builder.AddComponentParameter(1, nameof(Toast), toast);
		builder.AddComponentParameter(2, nameof(Settings), settings);
		builder.AddComponentParameter(3, nameof(ChildContent), content);
		builder.CloseComponent();
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		_changed = Settings != _drawnSettings || !ReferenceEquals(Toast, _drawnToast);
		_drawnSettings = Settings;
		_drawnToast = Toast;
	}

	/// <inheritdoc />
	protected override bool ShouldRender() => _changed;

	/// <inheritdoc />
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);
		builder.AddContent(0, ChildContent);
	}
}
