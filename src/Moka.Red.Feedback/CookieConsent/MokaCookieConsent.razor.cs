using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.CookieConsent;

/// <summary>
///     A GDPR cookie consent banner displayed at the top or bottom of the viewport.
///     Supports accept, reject, and optional customize actions.
/// </summary>
public partial class MokaCookieConsent : MokaComponentBase
{
	/// <summary>Optional custom message content. When provided, overrides <see cref="Message" />.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>The consent message text. Defaults to "We use cookies to improve your experience."</summary>
	[Parameter]
	public string Message { get; set; } = "We use cookies to improve your experience.";

	/// <summary>Text for the accept button. Defaults to "Accept All".</summary>
	[Parameter]
	public string AcceptText { get; set; } = "Accept All";

	/// <summary>Text for the reject button. Defaults to "Reject All".</summary>
	[Parameter]
	public string RejectText { get; set; } = "Reject All";

	/// <summary>Text for an optional customize button. When null, the button is hidden.</summary>
	[Parameter]
	public string? CustomizeText { get; set; }

	/// <summary>Banner position. Defaults to <see cref="MokaCookieConsentPosition.Bottom" />.</summary>
	[Parameter]
	public MokaCookieConsentPosition Position { get; set; } = MokaCookieConsentPosition.Bottom;

	/// <summary>Callback invoked when the user clicks Accept.</summary>
	[Parameter]
	public EventCallback OnAccept { get; set; }

	/// <summary>Callback invoked when the user clicks Reject.</summary>
	[Parameter]
	public EventCallback OnReject { get; set; }

	/// <summary>Callback invoked when the user clicks Customize.</summary>
	[Parameter]
	public EventCallback OnCustomize { get; set; }

	/// <summary>Whether the banner is visible. Defaults to true.</summary>
	[Parameter]
	public bool Visible { get; set; } = true;

	/// <summary>Callback invoked when <see cref="Visible" /> changes. Enables two-way binding.</summary>
	[Parameter]
	public EventCallback<bool> VisibleChanged { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-cookie-consent";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-cookie-consent--top", Position == MokaCookieConsentPosition.Top)
		.AddClass("moka-cookie-consent--bottom", Position == MokaCookieConsentPosition.Bottom)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	private async Task HandleAccept()
	{
		Visible = false;

		if (VisibleChanged.HasDelegate)
		{
			await VisibleChanged.InvokeAsync(false);
		}

		if (OnAccept.HasDelegate)
		{
			await OnAccept.InvokeAsync();
		}
	}

	private async Task HandleReject()
	{
		Visible = false;

		if (VisibleChanged.HasDelegate)
		{
			await VisibleChanged.InvokeAsync(false);
		}

		if (OnReject.HasDelegate)
		{
			await OnReject.InvokeAsync();
		}
	}

	private async Task HandleCustomize()
	{
		if (OnCustomize.HasDelegate)
		{
			await OnCustomize.InvokeAsync();
		}
	}
}
