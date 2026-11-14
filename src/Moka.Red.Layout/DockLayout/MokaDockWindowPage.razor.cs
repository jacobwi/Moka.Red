using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Theming;

namespace Moka.Red.Layout.DockLayout;

/// <summary>
///     The page a panel opens in when it moves to another browser window (<see cref="MokaDockLayout.PopOutPanelAsync" />).
///     It renders the panel's component in its own circuit and follows the parameters the layout passes it through
///     <see cref="MokaDockWindows" />. A Blazor Server host lists this assembly with <c>AddAdditionalAssemblies</c> so the
///     router finds it.
/// </summary>
public sealed partial class MokaDockWindowPage : IDisposable
{
	private MokaDockWindowSession? _session;

	/// <summary>The session key from the window's address.</summary>
	[SupplyParameterFromQuery(Name = "session")]
	public string? Session { get; set; }

	[Inject]
	private IServiceProvider Services { get; set; } = default!;

	private static MokaDockText Words => MokaDockText.Default;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		MokaDockWindowSession? session = Services.GetService<MokaDockWindows>()?.Find(Session);
		if (ReferenceEquals(session, _session))
		{
			return;
		}

		if (_session is not null)
		{
			_session.Changed -= OnSessionChanged;
		}

		_session = session;
		if (_session is not null)
		{
			_session.Changed += OnSessionChanged;
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (_session is not null)
		{
			_session.Changed -= OnSessionChanged;
		}
	}

	private void OnSessionChanged(object? sender, EventArgs e) => _ = InvokeAsync(StateHasChanged);

	private void BringBack() => _session?.RequestReturn();
}

/// <summary>The layout of <see cref="MokaDockWindowPage" />: the app's theme around the panel, and nothing else.</summary>
public sealed class MokaDockWindowLayout : LayoutComponentBase
{
	[CascadingParameter]
	private MokaTheme? Theme { get; set; }

	/// <inheritdoc />
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);
		builder.OpenComponent<MokaThemeProvider>(0);
		if (Theme is not null)
		{
			builder.AddComponentParameter(1, nameof(MokaThemeProvider.Theme), Theme);
		}

		builder.AddComponentParameter(2, nameof(MokaThemeProvider.ChildContent), Body);
		builder.CloseComponent();
	}
}
