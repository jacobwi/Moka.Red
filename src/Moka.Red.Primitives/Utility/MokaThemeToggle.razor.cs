using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Icons;
using Moka.Red.ThemeGen.Appearance;

namespace Moka.Red.Primitives.Utility;

/// <summary>
///     A pre-built light and dark mode control, by default a round button with a sun or a moon. <see cref="Look" /> draws
///     it another way, and <see cref="ShowSystem" /> adds a third state for following the device. Two-way bindable through
///     <see cref="IsDark" /> or, for three states, <see cref="Mode" />.
/// </summary>
public partial class MokaThemeToggle
{
	private static readonly MokaColorMode[] TwoStates = [MokaColorMode.Light, MokaColorMode.Dark];
	private static readonly MokaColorMode[] ThreeStates = [MokaColorMode.Light, MokaColorMode.Dark, MokaColorMode.System];

	private readonly string _groupName = $"moka-theme-toggle-{Guid.NewGuid():N}";
	private MokaColorMode _mode;
	private bool? _lastIsDark;
	private MokaColorMode? _lastMode;

	/// <summary>
	///     Whether dark mode is active. Two-way bindable. While <see cref="Mode" /> is set this only says which way
	///     <see cref="MokaColorMode.System" /> is showing if the toggle has no System state of its own.
	/// </summary>
	[Parameter]
	public bool IsDark { get; set; }

	/// <summary>Callback when IsDark changes. Not raised when the new state is <see cref="MokaColorMode.System" />.</summary>
	[Parameter]
	public EventCallback<bool> IsDarkChanged { get; set; }

	/// <summary>The current state, for a toggle with three. Two-way bindable to a <see cref="MokaColorMode" />. Null follows <see cref="IsDark" />.</summary>
	[Parameter]
	public MokaColorMode? Mode { get; set; }

	/// <summary>Raised with the new state on every change.</summary>
	[Parameter]
	public EventCallback<MokaColorMode> ModeChanged { get; set; }

	/// <summary>Adds <see cref="MokaColorMode.System" /> as a third state. Default false.</summary>
	[Parameter]
	public bool ShowSystem { get; set; }

	/// <summary>How the toggle is drawn. Default <see cref="MokaThemeToggleLook.Icon" />.</summary>
	[Parameter]
	public MokaThemeToggleLook Look { get; set; }

	/// <summary>
	///     Where the tooltip that names the button opens. Defaults to <see cref="MokaTooltipPosition.Bottom" />, for a
	///     toggle in a row such as an app bar; use Top in a row at the bottom of a view, and Left or Right in a vertical
	///     rail. Where the side has no room the tooltip moves to one that has.
	/// </summary>
	[Parameter]
	public MokaTooltipPosition TooltipPosition { get; set; } = MokaTooltipPosition.Bottom;

	/// <summary>
	///     What a click does when it would turn dark mode on, in the tooltip and as the accessible name.
	///     Default "Switch to dark mode"; set it to translate.
	/// </summary>
	[Parameter]
	public string DarkModeLabel { get; set; } = "Switch to dark mode";

	/// <summary>
	///     What a click does when it would turn light mode on, in the tooltip and as the accessible name.
	///     Default "Switch to light mode"; set it to translate.
	/// </summary>
	[Parameter]
	public string LightModeLabel { get; set; } = "Switch to light mode";

	/// <summary>What a click does when it would start following the device. Default "Follow this device".</summary>
	[Parameter]
	public string SystemModeLabel { get; set; } = "Follow this device";

	/// <summary>The word for the light state. Default "Light".</summary>
	[Parameter]
	public string LightText { get; set; } = "Light";

	/// <summary>The word for the dark state. Default "Dark".</summary>
	[Parameter]
	public string DarkText { get; set; } = "Dark";

	/// <summary>The word for following the device. Default "System".</summary>
	[Parameter]
	public string SystemText { get; set; } = "System";

	/// <summary>The accessible name of the radio group the segmented looks and a three-state switch are. Default "Color mode".</summary>
	[Parameter]
	public string GroupLabel { get; set; } = "Color mode";

	/// <summary>The accessible name and tooltip of the two-state switch, which is on while dark. Default "Dark mode".</summary>
	[Parameter]
	public string SwitchLabel { get; set; } = "Dark mode";

	/// <inheritdoc />
	protected override string RootClass => Look switch
	{
		MokaThemeToggleLook.Switch => "moka-theme-toggle-switch",
		MokaThemeToggleLook.Segmented or MokaThemeToggleLook.IconsAndWords => "moka-theme-toggle-segmented",
		MokaThemeToggleLook.Chip => "moka-theme-toggle-chip",
		_ => "moka-theme-toggle"
	};

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-theme-toggle--dark", ShownMode == MokaColorMode.Dark)
		.AddClass($"{RootClass}--{SizeToKebab(Size)}", Size != MokaSize.Md)
		.AddClass("moka-theme-toggle-switch--three", Look == MokaThemeToggleLook.Switch && ShowSystem)
		.AddClass($"moka-theme-toggle-switch--at-{Array.IndexOf(Modes, ShownMode)}", Look == MokaThemeToggleLook.Switch)
		.AddClass("moka-theme-toggle-switch--disabled", Look == MokaThemeToggleLook.Switch && Disabled)
		.AddClass(Class)
		.Build();

	/// <summary>The toggle changes its own state on click, outside the parameter flow.</summary>
	protected override bool ShouldRender() => true;

	private MokaColorMode[] Modes => ShowSystem ? ThreeStates : TwoStates;

	private MokaColorMode ShownMode => !ShowSystem && _mode == MokaColorMode.System
		? IsDark ? MokaColorMode.Dark : MokaColorMode.Light
		: _mode;

	private MokaColorMode NextMode => Modes[(Array.IndexOf(Modes, ShownMode) + 1) % Modes.Length];

	private string ActionLabel => NextMode switch
	{
		MokaColorMode.Dark => DarkModeLabel,
		MokaColorMode.Light => LightModeLabel,
		_ => SystemModeLabel
	};

	// It starts with the word on the chip, so what's read out matches what's on screen.
	private string ChipName => $"{WordFor(ShownMode)}. {ActionLabel}";

	// With two states the page shows which one is on, so the icon shows what a click gives. With three it can't show
	// System, so the icon shows the current state.
	private MokaIconDefinition ButtonIcon => ShowSystem
		? IconFor(ShownMode)
		: ShownMode == MokaColorMode.Dark ? MokaIcons.Action.Sun : MokaIcons.Action.Moon;

	private string ButtonIconSize => Size == MokaSize.Md ? "18px" : MapSizeToPx(Size);

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Each value only seeds the state when the parent passes a new one, so a parent render that passes the old one
		// again cannot undo a click. A device changing scheme mustn't take a three-state toggle off System.
		if (_lastIsDark != IsDark)
		{
			_lastIsDark = IsDark;
			if (Mode is null && !(ShowSystem && _mode == MokaColorMode.System))
			{
				_mode = IsDark ? MokaColorMode.Dark : MokaColorMode.Light;
			}
		}

		if (Mode is { } mode && _lastMode != mode)
		{
			_lastMode = mode;
			_mode = mode;
		}
	}

	private static MokaIconDefinition IconFor(MokaColorMode mode) => mode switch
	{
		MokaColorMode.Light => MokaIcons.Action.Sun,
		MokaColorMode.Dark => MokaIcons.Action.Moon,
		_ => MokaIcons.Content.Monitor
	};

	private string WordFor(MokaColorMode mode) => mode switch
	{
		MokaColorMode.Light => LightText,
		MokaColorMode.Dark => DarkText,
		_ => SystemText
	};

	private string StopClass(MokaColorMode mode) => new CssBuilder("moka-theme-toggle-switch__stop")
		.AddClass("moka-theme-toggle-switch__stop--on", ShownMode == mode)
		.Build();

	private Task Toggle() => SetModeAsync(NextMode);

	private Task PickAsync(string mode) => SetModeAsync(Enum.Parse<MokaColorMode>(mode));

	private async Task SetModeAsync(MokaColorMode mode)
	{
		// The disabled attribute stops clicks in the browser; this also covers events raised another way.
		if (Disabled || mode == _mode)
		{
			return;
		}

		_mode = mode;
		if (ModeChanged.HasDelegate)
		{
			await ModeChanged.InvokeAsync(mode);
		}

		if (mode != MokaColorMode.System && IsDarkChanged.HasDelegate)
		{
			await IsDarkChanged.InvokeAsync(mode == MokaColorMode.Dark);
		}
	}
}
