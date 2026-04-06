using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Utility;

/// <summary>
///     Pre-built light/dark mode toggle button with sun/moon icon.
///     Two-way bindable via <see cref="IsDark" /> and <see cref="IsDarkChanged" />.
/// </summary>
public partial class MokaThemeToggle
{
	/// <summary>Whether dark mode is active. Two-way bindable.</summary>
	[Parameter]
	public bool IsDark { get; set; }

	/// <summary>Callback when IsDark changes.</summary>
	[Parameter]
	public EventCallback<bool> IsDarkChanged { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-theme-toggle";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-theme-toggle--dark", IsDark)
		.AddClass(Class)
		.Build();

	private async Task Toggle()
	{
		IsDark = !IsDark;
		if (IsDarkChanged.HasDelegate)
		{
			await IsDarkChanged.InvokeAsync(IsDark);
		}
	}
}
