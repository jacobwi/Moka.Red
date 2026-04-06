using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Icons;

namespace Moka.Red.Primitives.Callout;

/// <summary>
///     An informational callout/admonition box (like GitHub alerts: Note, Tip, Important, Warning, Caution).
///     Features a left border accent, colored background tint, auto-mapped icon, and optional close button.
/// </summary>
public partial class MokaCallout
{
	private bool _visible = true;

	/// <summary>Callout body content.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Optional title. When null, the <see cref="Type" /> name is used.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>Callout type controlling color accent and default icon. Default <see cref="MokaCalloutType.Note" />.</summary>
	[Parameter]
	public MokaCalloutType Type { get; set; } = MokaCalloutType.Note;

	/// <summary>Custom icon override. When null, the type-mapped icon is used.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>Whether to show a close button. Default false.</summary>
	[Parameter]
	public bool Closable { get; set; }

	/// <summary>Callback invoked when the callout is closed.</summary>
	[Parameter]
	public EventCallback OnClose { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-callout";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-callout--{MokaEnumHelpers.ToCssClass(Type)}")
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private MokaIconDefinition ResolvedIcon => Icon ?? Type switch
	{
		MokaCalloutType.Tip => MokaIcons.Status.CheckCircle,
		MokaCalloutType.Important => MokaIcons.Status.Info,
		MokaCalloutType.Warning => MokaIcons.Status.Warning,
		MokaCalloutType.Caution => MokaIcons.Status.Error,
		_ => MokaIcons.Status.Info
	};

	/// <summary>Has internal visibility state.</summary>
	protected override bool ShouldRender() => true;

	private async Task HandleClose()
	{
		_visible = false;
		if (OnClose.HasDelegate)
		{
			await OnClose.InvokeAsync();
		}
	}
}
