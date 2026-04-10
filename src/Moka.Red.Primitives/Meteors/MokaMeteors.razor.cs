using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Meteors;

/// <summary>
///     Renders animated meteor/shooting-star streaks as a decorative background overlay.
///     Each meteor has a bright head dot with a fading tail trail.
///     Pure CSS animation — zero JS. Wrap around content or use standalone.
/// </summary>
public partial class MokaMeteors : MokaComponentBase
{
	private MeteorData[]? _meteors;

	/// <summary>Content rendered above the meteor field.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Number of meteor streaks. Default 12.</summary>
	[Parameter]
	public int Count { get; set; } = 12;

	/// <summary>Meteor streak color. Default uses the primary accent.</summary>
	[Parameter]
	public string? Color { get; set; }

	/// <summary>Minimum animation duration in seconds. Default 3.</summary>
	[Parameter]
	public double MinDuration { get; set; } = 3;

	/// <summary>Maximum animation duration in seconds. Default 8.</summary>
	[Parameter]
	public double MaxDuration { get; set; } = 8;

	/// <summary>Meteor angle in degrees (0 = vertical down, 45 = diagonal). Default 35.</summary>
	[Parameter]
	public int Angle { get; set; } = 35;

	/// <summary>Minimum streak length in pixels. Default 30.</summary>
	[Parameter]
	public int MinLength { get; set; } = 30;

	/// <summary>Maximum streak length in pixels. Default 80.</summary>
	[Parameter]
	public int MaxLength { get; set; } = 80;

	/// <summary>Travel distance in pixels (how far the meteor moves). Default 800.</summary>
	[Parameter]
	public int TravelDistance { get; set; } = 800;

	/// <summary>Minimum height of the container. Default null (fits content).</summary>
	[Parameter]
	public string? MinHeight { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-meteors";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("--meteor-angle", $"{Angle}deg")
		.AddStyle("--meteor-color", Color ?? "var(--moka-color-primary)")
		.AddStyle("--meteor-travel", $"{TravelDistance}px")
		.AddStyle("min-height", MinHeight, !string.IsNullOrEmpty(MinHeight))
		.AddStyle(Style)
		.Build();

	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		if (_meteors is null || _meteors.Length != Count)
		{
			GenerateMeteors();
		}
	}

	private void GenerateMeteors()
	{
		_meteors = new MeteorData[Count];
		for (var i = 0; i < Count; i++)
		{
			// Deterministic pseudo-random distribution using Knuth multiplicative hash
			var h1 = (i * 2654435761u) % 1000;
			var f1 = h1 / 1000.0;
			var h2 = ((i + 7) * 2246822519u) % 1000;
			var f2 = h2 / 1000.0;
			var h3 = ((i + 13) * 3266489917u) % 1000;
			var f3 = h3 / 1000.0;

			_meteors[i] = new MeteorData
			{
				Left = (int)(f1 * 100),
				Duration = MinDuration + f2 * (MaxDuration - MinDuration),
				Delay = f1 * MaxDuration * 1.5, // stagger spawns across 1.5x the max cycle
				Length = MinLength + (int)(f2 * (MaxLength - MinLength)),
				Thickness = f3 < 0.3 ? 1 : f3 < 0.7 ? 2 : 3, // 30% thin, 40% normal, 30% thick
				HeadSize = f3 < 0.3 ? 2 : f3 < 0.7 ? 3 : 4
			};
		}
	}

	private static string MeteorStyle(MeteorData m)
	{
		return new StyleBuilder()
			.AddStyle("left", $"{m.Left}%")
			.AddStyle("animation-duration", $"{m.Duration.ToString("F1", CultureInfo.InvariantCulture)}s")
			.AddStyle("animation-delay", $"{m.Delay.ToString("F1", CultureInfo.InvariantCulture)}s")
			.AddStyle("width", $"{m.Length}px")
			.AddStyle("--meteor-thickness", $"{m.Thickness}px")
			.AddStyle("--meteor-head", $"{m.HeadSize}px")
			.Build()!;
	}

	private sealed record MeteorData
	{
		public int Left { get; init; }
		public double Duration { get; init; }
		public double Delay { get; init; }
		public int Length { get; init; }
		public int Thickness { get; init; }
		public int HeadSize { get; init; }
	}
}
