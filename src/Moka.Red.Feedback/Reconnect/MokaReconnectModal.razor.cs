using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.Reconnect;

/// <summary>
///     Blazor Server's reconnect screen in the theme's look, in place of the plain dialog Blazor draws when
///     the connection to the server drops. Put one in <c>App.razor</c>, outside any render mode, next to
///     <c>blazor.web.js</c>. WebAssembly has no connection to lose and doesn't need it.
/// </summary>
/// <remarks>
///     Blazor finds the dialog by its id and sets its state as a class on it. <see cref="MokaComponentBase.Id" />
///     is ignored for that reason. Rendered outside a <c>MokaThemeProvider</c>, it reads the tokens the
///     outermost provider writes to <c>:root</c>, so it follows the theme, and it needs nothing from .NET
///     while it's on screen, because by then .NET can't answer.
/// </remarks>
public partial class MokaReconnectModal : MokaVisualComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Feedback/Reconnect/MokaReconnectModal.razor.js";

	/// <summary>The words shown in each state. Set only the ones you want to change.</summary>
	[Parameter]
	public MokaReconnectText Text { get; set; } = new();

	/// <summary>Drawn in the middle of the rings above the heading, such as your logo. Defaults to a refresh icon.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-reconnect";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	// The text before and after the seconds, which Blazor writes into an element of its own.
	private (string Before, string? After) RetryingParts
	{
		get
		{
			int marker = Text.RetryingMessage.IndexOf("{0}", StringComparison.Ordinal);
			return marker < 0
				? (Text.RetryingMessage, null)
				: (Text.RetryingMessage[..marker], Text.RetryingMessage[(marker + 3)..]);
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// Only an interactive render gets here. Placed in an interactive layout rather than App.razor, the
		// dialog this binds to replaced the prerendered one, which is what the script tag bound.
		if (firstRender)
		{
			await SafeModuleInvokeVoidAsync(ModulePath, "bind");
		}
	}
}
