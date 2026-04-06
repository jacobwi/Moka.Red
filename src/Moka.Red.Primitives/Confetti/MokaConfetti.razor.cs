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

	private CancellationTokenSource? _cts;

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

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (Active && _particles.Count == 0)
		{
			GenerateParticles();
			ScheduleReset();
		}
		else if (!Active)
		{
			_particles = [];
		}
	}

#pragma warning disable CA5394 // Random is not used for security purposes — visual confetti positions only
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
		CancellationToken token = _cts.Token;

		_ = Task.Delay(Duration + 200, token).ContinueWith(async _ =>
		{
			if (!token.IsCancellationRequested)
			{
				Active = false;
				_particles = [];

				if (ActiveChanged.HasDelegate)
				{
					await InvokeAsync(() => ActiveChanged.InvokeAsync(false));
				}

				await InvokeAsync(StateHasChanged);
			}
		}, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Current);
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
