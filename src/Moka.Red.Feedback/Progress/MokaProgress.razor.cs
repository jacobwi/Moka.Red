using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.Progress;

/// <summary>
///     A progress indicator supporting linear and circular modes, determinate and indeterminate states.
///     Uses <c>role="progressbar"</c> with appropriate ARIA attributes.
/// </summary>
public partial class MokaProgress : MokaVisualComponentBase
{
	private const double CircleRadius = 18;
	private const double CircleCircumference = 2 * Math.PI * CircleRadius;

	/// <summary>
	///     Progress value from 0 to 100. Null indicates an indeterminate state.
	/// </summary>
	[Parameter]
	public double? Value { get; set; }

	/// <summary>Progress indicator type. Defaults to <see cref="MokaProgressType.Linear" />.</summary>
	[Parameter]
	public MokaProgressType ProgressType { get; set; } = MokaProgressType.Linear;

	/// <summary>Whether to show the numeric percentage value. Defaults to false.</summary>
	[Parameter]
	public bool ShowValue { get; set; }

	/// <summary>Whether to show animated stripes on the linear bar. Defaults to false.</summary>
	[Parameter]
	public bool Striped { get; set; }

	/// <summary>Whether to use rounded bar ends on the linear progress bar. Defaults to true.</summary>
	[Parameter]
	public bool RoundedEnds { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-progress";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(ProgressType == MokaProgressType.Linear ? "moka-progress--linear" : "moka-progress--circular")
		.AddClass("moka-progress--indeterminate", !Value.HasValue)
		.AddClass("moka-progress--striped", Striped && ProgressType == MokaProgressType.Linear)
		.AddClass("moka-progress--rounded", RoundedEnds && ProgressType == MokaProgressType.Linear)
		.AddClass(Class)
		.Build();

	private bool IsIndeterminate => !Value.HasValue;

	private double ClampedValue => Value.HasValue ? Math.Clamp(Value.Value, 0, 100) : 0;

	private string BarWidth => $"width: {ClampedValue}%";

	private double StrokeDashoffset =>
		CircleCircumference - ClampedValue / 100.0 * CircleCircumference;
}
