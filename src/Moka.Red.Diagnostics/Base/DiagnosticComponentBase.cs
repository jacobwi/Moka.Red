using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Base;

/// <summary>
///     Drop-in replacement for <see cref="MokaComponentBase" /> that automatically reports
///     render timing, ShouldRender decisions, and disposal to the diagnostics overlay.
///     <para>
///         Swap your component base class from <c>MokaComponentBase</c> to <c>DiagnosticComponentBase</c>
///         during development to get full tracking. Use <c>#if DEBUG</c> conditional compilation
///         to restrict to debug builds only.
///     </para>
/// </summary>
public abstract class DiagnosticComponentBase : MokaComponentBase
{
	private static int _nextId;
	private readonly string _componentId = $"diag-{Interlocked.Increment(ref _nextId)}";
	private readonly Stopwatch _renderStopwatch = new();

	/// <summary>
	///     The diagnostics service. Nullable — when not registered, all tracking is silently skipped.
	/// </summary>
	[Inject]
	private IMokaDiagnosticsService? DiagnosticsService { get; set; }

	private string ShortTypeName => GetType().Name;

	/// <inheritdoc />
	protected override bool ShouldRender()
	{
		bool shouldRender = base.ShouldRender();

		if (!shouldRender)
		{
			DiagnosticsService?.RecordShouldRenderSkip(ShortTypeName, _componentId);
		}
		else
		{
			// Start timing just before the render happens
			_renderStopwatch.Restart();
		}

		return shouldRender;
	}

	/// <inheritdoc />
	protected override void OnAfterRender(bool firstRender)
	{
		_renderStopwatch.Stop();
		TimeSpan duration = _renderStopwatch.Elapsed;

		DiagnosticsService?.RecordRender(ShortTypeName, _componentId, duration);

		base.OnAfterRender(firstRender);
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		DiagnosticsService?.RecordDisposal(ShortTypeName, _componentId);
		await base.DisposeAsyncCore();
	}
}
