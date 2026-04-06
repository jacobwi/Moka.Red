using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Motion;

/// <summary>
///     Wraps child content and fades it in on first render.
///     Supports optional directional movement (up, down, left, right) via <see cref="Direction" />.
///     Pure CSS animation — zero JavaScript.
/// </summary>
public partial class MokaFadeIn : MokaComponentBase
{
	private bool _hasRendered;

	/// <summary>The content to fade in.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Animation duration in milliseconds. Defaults to 300.</summary>
	[Parameter]
	public int Duration { get; set; } = 300;

	/// <summary>Delay before the animation starts, in milliseconds. Defaults to 0.</summary>
	[Parameter]
	public int Delay { get; set; }

	/// <summary>Direction from which the content fades in. Defaults to <see cref="MokaFadeDirection.None" /> (fade in place).</summary>
	[Parameter]
	public MokaFadeDirection Direction { get; set; } = MokaFadeDirection.None;

	/// <summary>Distance to travel when a direction is specified. Defaults to "20px".</summary>
	[Parameter]
	public string Distance { get; set; } = "20px";

	/// <summary>When true, the animation plays only on the first render. Defaults to true.</summary>
	[Parameter]
	public bool Once { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-fade-in";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-fade-in--none", Direction == MokaFadeDirection.None)
		.AddClass("moka-fade-in--up", Direction == MokaFadeDirection.Up)
		.AddClass("moka-fade-in--down", Direction == MokaFadeDirection.Down)
		.AddClass("moka-fade-in--left", Direction == MokaFadeDirection.Left)
		.AddClass("moka-fade-in--right", Direction == MokaFadeDirection.Right)
		.AddClass("moka-fade-in--animated", !Once || !_hasRendered)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("--moka-fade-duration", $"{Duration}ms")
		.AddStyle("--moka-fade-delay", $"{Delay}ms")
		.AddStyle("--moka-fade-distance", Distance)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override void OnAfterRender(bool firstRender)
	{
		base.OnAfterRender(firstRender);

		if (firstRender)
		{
			_hasRendered = true;
		}
	}
}
