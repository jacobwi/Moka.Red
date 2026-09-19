using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Confetti;

/// <summary>
///     A celebration confetti burst effect triggered programmatically.
///     Generates CSS-animated particle spans with random positions, rotations, and colors.
///     Set <see cref="Active" /> to true to trigger a burst; it auto-resets when the animation completes.
/// </summary>
public partial class MokaConfetti : MokaComponentBase
{
	private static readonly string[] DefaultColors =
		["#ef5350", "#42a5f5", "#66bb6a", "#ffa726", "#ab47bc", "#26c6da", "#ec407a", "#ffee58"];

	private bool _active;
	private CancellationTokenSource? _cts;
	private bool? _lastActive;

	private List<ConfettiParticle> _particles = [];

	/// <summary>Whether the confetti burst is active. Two-way bindable. Auto-resets to false after the animation completes.</summary>
	[Parameter]
	public bool Active { get; set; }

	/// <summary>Callback invoked when the active state changes.</summary>
	[Parameter]
	public EventCallback<bool> ActiveChanged { get; set; }

	/// <summary>Number of confetti particles to generate. Defaults to 50.</summary>
	[Parameter]
	public int ParticleCount { get; set; } = 50;

	/// <summary>Total animation duration in milliseconds. Defaults to 2000.</summary>
	[Parameter]
	public int Duration { get; set; } = 2000;

	/// <summary>
	///     Colors to use for particles. Defaults to a rainbow palette.
	///     Accepts any CSS color strings (hex, rgb, hsl, etc.).
	/// </summary>
	[Parameter]
	public IReadOnlyList<string>? Colors { get; set; }

	/// <summary>Spread angle in degrees for the confetti burst. Defaults to 60.</summary>
	[Parameter]
	public double Spread { get; set; } = 60;

	/// <inheritdoc />
	protected override string RootClass => "moka-confetti";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("--moka-confetti-duration", $"{Duration}ms")
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	// CSS only parses '.' as the decimal separator, so every numeric value here is formatted
	// invariantly. A comma-decimal locale would otherwise emit "0,85" and the browser would
	// drop the whole custom property.
	private static string? ParticleStyle(ConfettiParticle p) => new StyleBuilder()
		.AddStyle("--cx", $"{p.X.ToString("F0", CultureInfo.InvariantCulture)}px")
		.AddStyle("--cy", $"{p.Y.ToString("F0", CultureInfo.InvariantCulture)}px")
		.AddStyle("--cr", $"{p.Rotation.ToString(CultureInfo.InvariantCulture)}deg")
		.AddStyle("--cre", $"{p.RotationEnd.ToString(CultureInfo.InvariantCulture)}deg")
		.AddStyle("--cs", p.Scale.ToString("F2", CultureInfo.InvariantCulture))
		.AddStyle("--cd", $"{p.Delay.ToString(CultureInfo.InvariantCulture)}ms")
		.AddStyle("background-color", p.Color)
		.Build();

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Only a new value from the parent starts or stops a burst. The burst turns itself off, so
		// reading an unbound Active="true" on every parent render fired it again each time.
		if (_lastActive == Active)
		{
			return;
		}

		_lastActive = Active;
		_active = Active;

		if (_active && _particles.Count == 0)
		{
			GenerateParticles();
			ScheduleReset();
		}
		else if (!_active)
		{
			_particles = [];
		}
	}

#pragma warning disable CA5394 // Random is not used for security purposes - visual confetti positions only
	private void GenerateParticles()
	{
		IReadOnlyList<string> colors = Colors ?? DefaultColors;
		_particles = new List<ConfettiParticle>(ParticleCount);

		double halfSpread = Spread / 2;

		for (int i = 0; i < ParticleCount; i++)
		{
			double angle = -90 + (Random.Shared.NextDouble() * 2 - 1) * halfSpread;
			double angleRad = angle * Math.PI / 180;
			double velocity = 300 + Random.Shared.NextDouble() * 400;

			double dx = Math.Cos(angleRad) * velocity;
			double dy = Math.Sin(angleRad) * velocity;

			_particles.Add(new ConfettiParticle
			{
				Color = colors[Random.Shared.Next(colors.Count)],
				X = dx,
				Y = dy,
				Rotation = Random.Shared.Next(0, 360),
				RotationEnd = Random.Shared.Next(-720, 720),
				Scale = 0.5 + Random.Shared.NextDouble() * 0.8,
				Delay = Random.Shared.Next(0, Duration / 5),
				Shape = Random.Shared.Next(3) // 0=square, 1=rectangle, 2=circle
			});
		}
	}
#pragma warning restore CA5394

	private void ScheduleReset()
	{
		_cts?.Cancel();
		_cts = new CancellationTokenSource();
		_ = ResetAfterBurstAsync(_cts.Token);
	}

	private async Task ResetAfterBurstAsync(CancellationToken token)
	{
		try
		{
			await Task.Delay(Duration + 200, token);

			// The delay can finish on a thread-pool thread, and the next render reads this state.
			await InvokeAsync(async () =>
			{
				if (token.IsCancellationRequested)
				{
					return;
				}

				_active = false;
				_particles = [];
				StateHasChanged();
				await ActiveChanged.InvokeAsync(false);
			});
		}
		catch (OperationCanceledException)
		{
			// A newer burst or disposal took over.
		}
		catch (ObjectDisposedException)
		{
			// The renderer went away during the delay.
		}
		catch (Exception ex) when (!token.IsCancellationRequested)
		{
			// Unobserved, an exception from ActiveChanged would vanish. Blazor's error handling
			// gets it instead, as it would from a lifecycle method.
			await DispatchExceptionAsync(ex);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_cts is not null)
		{
			await _cts.CancelAsync();
			_cts.Dispose();
		}

		await base.DisposeAsyncCore();
	}

	private sealed class ConfettiParticle
	{
		public string Color { get; init; } = "";
		public double X { get; init; }
		public double Y { get; init; }
		public int Rotation { get; init; }
		public int RotationEnd { get; init; }
		public double Scale { get; init; }
		public int Delay { get; init; }
		public int Shape { get; init; }
	}
}
