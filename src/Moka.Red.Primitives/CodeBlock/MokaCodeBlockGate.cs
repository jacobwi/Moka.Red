using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Moka.Red.Primitives.CodeBlock;

/// <summary>
///     Draws a code block's lines only when <see cref="Version" /> moves, so the copy button saying "Copied!" and back
///     doesn't build every line of a long listing again. The version is compared before the parameters are taken,
///     because a <see cref="RenderFragment" /> parameter would otherwise render it every time.
/// </summary>
internal sealed class MokaCodeBlockGate : ComponentBase
{
	private bool _received;

	/// <summary>Moves whenever the lines would draw differently.</summary>
	[Parameter]
	public int Version { get; set; }

	/// <summary>The lines.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Content behind a gate, for markup. Razor doesn't find an internal component by its tag.</summary>
	/// <param name="version">Moves whenever the content would draw differently.</param>
	/// <param name="content">The content.</param>
	public static RenderFragment For(int version, RenderFragment content) => builder =>
	{
		builder.OpenComponent<MokaCodeBlockGate>(0);
		builder.AddComponentParameter(1, nameof(Version), version);
		builder.AddComponentParameter(2, nameof(ChildContent), content);
		builder.CloseComponent();
	};

	/// <inheritdoc />
	public override Task SetParametersAsync(ParameterView parameters)
	{
		if (_received && parameters.TryGetValue(nameof(Version), out int version) && version == Version)
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
