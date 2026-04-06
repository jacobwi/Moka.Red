using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Toast;
using Moka.Red.Icons;

namespace Moka.Red.Feedback.Alert;

/// <summary>
///     An inline alert banner for displaying contextual messages with severity-based styling.
///     Uses <c>role="alert"</c> for screen reader accessibility.
/// </summary>
public partial class MokaAlert : MokaComponentBase
{
	private bool _visible = true;

	/// <summary>Alert body content.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Severity level controlling color and icon. Defaults to <see cref="MokaToastSeverity.Info" />.</summary>
	[Parameter]
	public MokaToastSeverity Severity { get; set; } = MokaToastSeverity.Info;

	/// <summary>Optional title displayed in bold above the content.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>Whether to show a close button. Defaults to false.</summary>
	[Parameter]
	public bool Closable { get; set; }

	/// <summary>Callback invoked when the alert is closed.</summary>
	[Parameter]
	public EventCallback OnClose { get; set; }

	/// <summary>Whether to use an outlined style instead of filled. Defaults to false.</summary>
	[Parameter]
	public bool Outlined { get; set; }

	/// <summary>Whether to use a compact layout with reduced padding. Defaults to false.</summary>
	[Parameter]
	public bool Dense { get; set; }

	/// <summary>Custom icon override. When null, the severity-mapped icon is used.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-alert";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-alert--{MokaEnumHelpers.ToCssClass(Severity)}")
		.AddClass("moka-alert--outlined", Outlined)
		.AddClass("moka-alert--dense", Dense)
		.AddClass(Class)
		.Build();

	private MokaIconDefinition ResolvedIcon => Icon ?? Severity switch
	{
		MokaToastSeverity.Success => MokaIcons.Status.CheckCircle,
		MokaToastSeverity.Warning => MokaIcons.Status.Warning,
		MokaToastSeverity.Error => MokaIcons.Status.Error,
		_ => MokaIcons.Status.Info
	};

	/// <summary>Alert has internal visibility state that changes when closed.</summary>
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
