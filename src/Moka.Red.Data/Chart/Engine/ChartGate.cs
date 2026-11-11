using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Moka.Red.Data.Chart;

/// <summary>
///     Draws its content only when <see cref="Version" /> moves, so a chart that renders again for its title, its state
///     or its toolbar doesn't diff a plot of thousands of path vertices again. The version is compared before the
///     parameters are taken, because a <see cref="RenderFragment" /> parameter would otherwise render it every time.
/// </summary>
internal sealed class ChartGate : ComponentBase
{
	private bool _received;

	/// <summary>Moves whenever the content would draw differently.</summary>
	[Parameter]
	public long Version { get; set; }

	/// <summary>The content.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Content behind a gate, for markup: <c>@ChartGate.For(Chart.GeometryVersion, Marks)</c>. Razor doesn't find an
	///     internal component by its tag, so the gate is opened here.
	/// </summary>
	/// <param name="version">Moves whenever the content would draw differently.</param>
	/// <param name="content">The content.</param>
	public static RenderFragment For(long version, RenderFragment? content) => builder =>
	{
		builder.OpenComponent<ChartGate>(0);
		builder.AddComponentParameter(1, nameof(Version), version);
		builder.AddComponentParameter(2, nameof(ChildContent), content);
		builder.CloseComponent();
	};

	/// <inheritdoc />
	public override Task SetParametersAsync(ParameterView parameters)
	{
		if (_received && parameters.TryGetValue(nameof(Version), out long version) && version == Version)
		{
			return Task.CompletedTask;
		}

		_received = true;
		return base.SetParametersAsync(parameters);
	}

	/// <inheritdoc />
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);
		builder.AddContent(0, ChildContent);
	}
}
