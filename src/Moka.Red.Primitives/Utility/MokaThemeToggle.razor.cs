using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Utility;

/// <summary>
///     Pre-built light/dark mode toggle button with sun/moon icon.
///     Two-way bindable via <see cref="IsDark" /> and <see cref="IsDarkChanged" />.
/// </summary>
public partial class MokaThemeToggle
{
	private bool _isDark;
	private bool? _lastIsDark;

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
		.AddClass("moka-theme-toggle--dark", _isDark)
		.AddClass(Class)
		.Build();

	/// <summary>The toggle flips its own state on click, outside the parameter flow.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// IsDark only seeds the state when the parent passes a new value, so a parent render that
		// passes the old one again cannot undo a click.
		if (_lastIsDark != IsDark)
		{
			_lastIsDark = IsDark;
			_isDark = IsDark;
		}
	}

	private async Task Toggle()
	{
		// The disabled attribute stops clicks in the browser; this also covers events raised another way.
		if (Disabled)
		{
			return;
		}

		_isDark = !_isDark;
		if (IsDarkChanged.HasDelegate)
		{
			await IsDarkChanged.InvokeAsync(_isDark);
		}
	}
}
