using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Theming;
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

	/// <summary>
	///     When set, renders as an anchor element instead of a button. A <c>javascript:</c>, <c>vbscript:</c>
	///     or <c>data:</c> URL is not rendered: the component stays a button.
	/// </summary>
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

	/// <summary>
	///     The quiet look for an icon-only button: a fixed square with no border or fill, and a faint icon that
	///     brightens on hover, red on hover with <c>Color="MokaColor.Error"</c>. <c>Variant</c> is ignored and
	///     <c>Size</c> changes only the icon. Name it with an <c>aria-label</c> and put it in a <c>MokaTooltip</c>.
	/// </summary>
	[Parameter]
	public bool Quiet { get; set; }

	/// <summary>
	///     How this button is drawn, ahead of the theme's <see cref="MokaTheme.Buttons" />. Null, the default, follows
	///     the theme; <see cref="MokaButtonLook.Standard" /> keeps this button standard whatever the theme says. Written
	///     as <c>data-moka-look</c>, so it applies on the new design only: under <see cref="MokaTheme.Dark" /> and
	///     <see cref="MokaTheme.Light" /> the button looks as it always did.
	/// </summary>
	/// <remarks>
	///     Not a cascading parameter: a page can hold hundreds of buttons, and each cascading parameter that can change
	///     costs a subscription. The theme's look reaches buttons through a class on the outermost theme root instead.
	/// </remarks>
	[Parameter]
	public MokaButtonLook? Look { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-btn";

	private string? LookName => Look is { } look ? MokaEnumHelpers.ToCssClass(look) : null;

	private bool IsIconOnly => ChildContent is null && (StartIcon is not null || EndIcon is not null);
	private string? LinkHref => UrlValues.SafeHref(Href);
	private bool IsLink => !string.IsNullOrEmpty(LinkHref);
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
		.AddClass($"moka-btn--{VariantToKebab(Variant)}", !Quiet)
		.AddClass($"moka-btn--{ColorToKebab(Color ?? MokaColor.Primary)}")
		.AddClass($"moka-btn--{SizeToKebab(Size)}", !Quiet)
		.AddClass("moka-btn--full-width", FullWidth)
		.AddClass("moka-fill-width", FullWidth)
		.AddClass("moka-btn--icon-only", IsIconOnly)
		.AddClass("moka-btn--quiet", Quiet)
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
