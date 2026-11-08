using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Moka.Red.Feedback.JobList;

/// <summary>
///     One job's row, which draws only when its job or the list's settings changed. Jobs report many times a second and
///     most of a long list has stopped moving, so without this every tick would redraw every row and its tooltips. The
///     markup stays in <see cref="MokaJobList" />, where its scoped CSS reaches it.
/// </summary>
internal sealed class MokaJobListRow : ComponentBase
{
	private bool _changed = true;
	private MokaJob? _drawnJob;
	private int _drawnSettings = -1;

	/// <summary>The job this row shows.</summary>
	[Parameter]
	public MokaJob? Job { get; set; }

	/// <summary>Moves on whenever something every row reads has changed.</summary>
	[Parameter]
	public int Settings { get; set; }

	/// <summary>Draws on every list render, for a row holding an app's slot, which can read anything.</summary>
	[Parameter]
	public bool AlwaysDraw { get; set; }

	/// <summary>The row's markup.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		_changed = AlwaysDraw || Settings != _drawnSettings || !Equals(Job, _drawnJob);
		_drawnSettings = Settings;
		_drawnJob = Job;
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
