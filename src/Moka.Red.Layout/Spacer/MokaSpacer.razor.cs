using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.Spacer;

/// <summary>
///     A flexible spacer that grows to fill available space in a flex container.
///     Place between siblings to push them apart.
/// </summary>
public partial class MokaSpacer : MokaComponentBase
{
	/// <summary>Flex-grow value. Default 1.</summary>
	[Parameter]
	public double Grow { get; set; } = 1;

	/// <inheritdoc />
	protected override string RootClass => "moka-spacer";

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("flex-grow", Grow.ToString(CultureInfo.InvariantCulture))
		.AddStyle(Style)
		.Build();
}
