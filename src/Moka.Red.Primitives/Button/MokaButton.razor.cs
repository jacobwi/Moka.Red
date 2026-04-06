using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Button;

/// <summary>
///     A performant, accessible button with variant, color, and size support.
///     Renders as &lt;a&gt; when <see cref="Href" /> is set, &lt;button&gt; otherwise.
/// </summary>
public partial class MokaButton
{
	/// <summary>Button label content.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Shows a loading spinner and disables interaction.</summary>
	[Parameter]
	public bool Loading { get; set; }

	/// <summary>Stretches the button to fill its container width.</summary>
	[Parameter]
	public bool FullWidth { get; set; }

	/// <summary>Icon displayed before the label.</summary>
	[Parameter]
	public MokaIconDefinition? StartIcon { get; set; }

	/// <summary>Icon displayed after the label.</summary>
	[Parameter]
	public MokaIconDefinition? EndIcon { get; set; }

	/// <summary>When set, renders as an anchor element instead of a button.</summary>
	[Parameter]
	public string? Href { get; set; }

	/// <summary>Link target (e.g., "_blank"). Only used when <see cref="Href" /> is set.</summary>
	[Parameter]
	public string? Target { get; set; }

	/// <summary>HTML button type attribute. Defaults to "button".</summary>
	[Parameter]
	public string Type { get; set; } = "button";

	/// <summary>Click event callback.</summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-btn";

	private bool IsIconOnly => ChildContent is null && (StartIcon is not null || EndIcon is not null);
	private bool IsLink => !string.IsNullOrEmpty(Href);
	private bool IsDisabled => Disabled || Loading;

	/// <summary>Maps button size to a slightly smaller icon size.</summary>
	private MokaSize IconSize => Size switch
	{
		MokaSize.Xs => MokaSize.Xs,
		MokaSize.Sm => MokaSize.Xs,
		MokaSize.Md => MokaSize.Sm,
		MokaSize.Lg => MokaSize.Md,
		_ => MokaSize.Sm
	};

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-btn--{VariantToKebab(Variant)}")
		.AddClass($"moka-btn--{ColorToKebab(Color ?? MokaColor.Primary)}")
		.AddClass($"moka-btn--{SizeToKebab(Size)}")
		.AddClass("moka-btn--full-width", FullWidth)
		.AddClass("moka-btn--icon-only", IsIconOnly)
		.AddClass("moka-btn--loading", Loading)
		.AddClass("moka-btn--disabled", IsDisabled)
		.AddClass(Class)
		.Build();

	private async Task HandleClick(MouseEventArgs args)
	{
		if (!IsDisabled)
		{
			await OnClick.InvokeAsync(args);
		}
	}
}
