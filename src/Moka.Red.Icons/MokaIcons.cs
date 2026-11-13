using Moka.Red.Core.Icons;

namespace Moka.Red.Icons;

/// <summary>
///     Built-in SVG icon definitions for Moka.Red. Icons use a 24x24 viewBox with
///     stroke-based paths (Lucide/Feather style).
/// </summary>
public static class MokaIcons
{
	/// <summary>Action icons - Save, Delete, Edit, Add, etc.</summary>
	public static class Action
	{
		/// <summary>Floppy disk / save icon.</summary>
		public static readonly MokaIconDefinition Save = new("save",
			"M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z M17 21v-8H7v8 M7 3v5h8");

		/// <summary>Trash can / delete icon.</summary>
		public static readonly MokaIconDefinition Delete = new("delete",
			"M3 6h18 M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2 M10 11v6 M14 11v6");

		/// <summary>Pencil / edit icon.</summary>
		public static readonly MokaIconDefinition Edit = new("edit",
			"M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7 M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z");

		/// <summary>Plus / add icon.</summary>
		public static readonly MokaIconDefinition Add = new("add",
			"M12 5v14 M5 12h14");

		/// <summary>Minus / remove icon.</summary>
		public static readonly MokaIconDefinition Remove = new("remove",
			"M5 12h14");

		/// <summary>Magnifying glass / search icon.</summary>
		public static readonly MokaIconDefinition Search = new("search",
			"M11 3a8 8 0 1 0 0 16 8 8 0 0 0 0-16z M21 21l-4.35-4.35");

		/// <summary>Gear / settings icon.</summary>
		public static readonly MokaIconDefinition Settings = new("settings",
			"M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z");

		/// <summary>Circular arrows / refresh icon.</summary>
		public static readonly MokaIconDefinition Refresh = new("refresh",
			"M1 4v6h6 M23 20v-6h-6 M20.49 9A9 9 0 0 0 5.64 5.64L1 10 M23 14l-4.64 4.36A9 9 0 0 1 3.51 15");

		/// <summary>Down arrow with line / download icon.</summary>
		public static readonly MokaIconDefinition Download = new("download",
			"M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4 M7 10l5 5 5-5 M12 15V3");

		/// <summary>Up arrow with line / upload icon.</summary>
		public static readonly MokaIconDefinition Upload = new("upload",
			"M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4 M17 8l-5-5-5 5 M12 3v12");

		/// <summary>Sun / light mode icon.</summary>
		public static readonly MokaIconDefinition Sun = new("sun",
			"M12 17a5 5 0 1 0 0-10 5 5 0 0 0 0 10z M12 1v2 M12 21v2 M4.22 4.22l1.42 1.42 M18.36 18.36l1.42 1.42 M1 12h2 M21 12h2 M4.22 19.78l1.42-1.42 M18.36 5.64l1.42-1.42");

		/// <summary>Moon / dark mode icon.</summary>
		public static readonly MokaIconDefinition Moon = new("moon",
			"M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z");

		/// <summary>Paper-plane send icon.</summary>
		public static readonly MokaIconDefinition Send = new("send",
			"M22 2L11 13 M22 2l-7 20-4-9-9-4 20-7z");

		/// <summary>Six-dot vertical drag grip.</summary>
		public static readonly MokaIconDefinition GripVertical = new("grip-vertical",
			"M9 6a1.3 1.3 0 1 0 0-2.6 1.3 1.3 0 0 0 0 2.6z M15 6a1.3 1.3 0 1 0 0-2.6 1.3 1.3 0 0 0 0 2.6z "
			+ "M9 13.3a1.3 1.3 0 1 0 0-2.6 1.3 1.3 0 0 0 0 2.6z M15 13.3a1.3 1.3 0 1 0 0-2.6 1.3 1.3 0 0 0 0 2.6z "
			+ "M9 20.6a1.3 1.3 0 1 0 0-2.6 1.3 1.3 0 0 0 0 2.6z M15 20.6a1.3 1.3 0 1 0 0-2.6 1.3 1.3 0 0 0 0 2.6z",
			filled: true);

		/// <summary>Pencil on its own, for drawing or annotating.</summary>
		public static readonly MokaIconDefinition Pencil = new("pencil",
			"M4 20l1.5-5.5L16 4a2.1 2.1 0 0 1 3 3L8.5 17.5z M14 6l3 3");

		/// <summary>Eraser, for clearing text or a drawing.</summary>
		public static readonly MokaIconDefinition Eraser = new("eraser",
			"M7 21l-4.3-4.3c-1-1-1-2.5 0-3.4l9.6-9.6c1-1 2.5-1 3.4 0l5.6 5.6c1 1 1 2.5 0 3.4L13 21 M22 21H7 M5 11l9 9");

		/// <summary>Broom, for cleaning up or pruning.</summary>
		public static readonly MokaIconDefinition Broom = new("broom",
			"M14 3l7 7 M17.5 6.5L11 13 M11 13l-2.5-2.5L3 16l5 5 5.5-5.5z M6 18.5l2-2");

		/// <summary>Three sliders, for adjustments and filters.</summary>
		public static readonly MokaIconDefinition Sliders = new("sliders",
			"M4 21v-7 M4 10V3 M12 21v-9 M12 8V3 M20 21v-5 M20 12V3 M1 14h6 M9 8h6 M17 16h6");

		/// <summary>Arrow curving back / undo icon.</summary>
		public static readonly MokaIconDefinition Undo = new("undo",
			"M3 7 H14 a5 5 0 0 1 0 10 H11 M7 3 L3 7 L7 11");

		/// <summary>Arrow curving forward / redo icon.</summary>
		public static readonly MokaIconDefinition Redo = new("redo",
			"M21 7 H10 a5 5 0 0 0 0 10 H13 M17 3 L21 7 L17 11");

		/// <summary>Arrow returning left, for putting something back.</summary>
		public static readonly MokaIconDefinition Restore = new("restore",
			"M9 14 4 9l5-5 M4 9h10.5a5.5 5.5 0 0 1 5.5 5.5 5.5 5.5 0 0 1-5.5 5.5H11");

		/// <summary>Clock with a counter-clockwise arrow / history icon.</summary>
		public static readonly MokaIconDefinition History = new("history",
			"M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8 M3 3v5h5 M12 7v5l4 2");

		/// <summary>Single clockwise arrow / try again icon.</summary>
		public static readonly MokaIconDefinition Retry = new("retry",
			"M21 12 a9 9 0 1 1 -3 -6.7 L21 8 M21 3 V8 H16");

		/// <summary>Rotate clockwise.</summary>
		public static readonly MokaIconDefinition RotateCw = new("rotate-cw",
			"M21 12a9 9 0 1 1-9-9c2.52 0 4.93 1 6.74 2.74L21 8 M21 3v5h-5");

		/// <summary>Rotate counter-clockwise.</summary>
		public static readonly MokaIconDefinition RotateCcw = new("rotate-ccw",
			"M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8 M3 3v5h5");

		/// <summary>Mirror left to right.</summary>
		public static readonly MokaIconDefinition FlipHorizontal = new("flip-horizontal",
			"M10 12H8 M16 12h-2 M22 12h-2 M4 12H2 M7.298 20.288A1 1 0 008 22h8a1 1 0 00.703-1.712l-3.991-3.99a1 1 0 00-1.424-.001z M7.298 3.712A1 1 0 018 2h8a1 1 0 01.703 1.712l-3.991 3.99a1 1 0 01-1.424.001z");

		/// <summary>Mirror top to bottom.</summary>
		public static readonly MokaIconDefinition FlipVertical = new("flip-vertical",
			"M12 14v2 M12 20v2 M12 2v2 M12 8v2 M20.288 16.703A1 1 0 0022 16V8a1 1 0 00-1.712-.703l-3.99 3.991a1 1 0 00-.001 1.424z M3.712 16.703A1 1 0 012 16V8a1 1 0 011.712-.703l3.99 3.991a1 1 0 01.001 1.424z");

		/// <summary>Crop marks.</summary>
		public static readonly MokaIconDefinition Crop = new("crop",
			"M6 2 V18 H22 M2 6 H18 V22");

		/// <summary>Find and replace.</summary>
		public static readonly MokaIconDefinition Replace = new("replace",
			"M14 4a1 1 0 0 1 1-1 M15 10a1 1 0 0 1-1-1 M21 4a1 1 0 0 0-1-1 M21 9a1 1 0 0 1-1 1 M3 7l3 3 3-3 M6 10V5a2 2 0 0 1 2-2h2 M4 14h5a1 1 0 0 1 1 1v5a1 1 0 0 1 -1 1h-5a1 1 0 0 1 -1 -1v-5a1 1 0 0 1 1 -1z");

		/// <summary>Two opposed horizontal arrows / swap icon.</summary>
		public static readonly MokaIconDefinition Swap = new("swap",
			"M7 7h13M16 3l4 4-4 4M17 17H4M8 13l-4 4 4 4");

		/// <summary>Three connected nodes / share icon.</summary>
		public static readonly MokaIconDefinition Share = new("share",
			"M18 8a3 3 0 1 0 0-6 3 3 0 0 0 0 6z M6 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z M18 22a3 3 0 1 0 0-6 3 3 0 0 0 0 6z M8.59 13.51l6.83 3.98 M15.41 6.51l-6.82 3.98");

		/// <summary>Arrow going into a tray / import icon.</summary>
		public static readonly MokaIconDefinition Import = new("import",
			"M12 3v12 M8 11l4 4 4-4 M8 5H4a2 2 0 0 0-2 2v10a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-4");

		/// <summary>Two down arrows over a line / download everything.</summary>
		public static readonly MokaIconDefinition DownloadAll = new("download-all",
			"M9 3 V12 M5.5 8.5 L9 12 L12.5 8.5 M15 6 V15 M11.5 11.5 L15 15 L18.5 11.5 M3 19 H21");

		/// <summary>Power button.</summary>
		public static readonly MokaIconDefinition Power = new("power",
			"M12 2v10 M18.4 6.6a9 9 0 1 1-12.77.04");

		/// <summary>Arrow going through a door / sign in icon.</summary>
		public static readonly MokaIconDefinition SignIn = new("sign-in",
			"M10 17l5-5-5-5 M15 12H3 M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4");

		/// <summary>Arrow leaving through a door / sign out icon.</summary>
		public static readonly MokaIconDefinition SignOut = new("sign-out",
			"M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4 M16 17l5-5-5-5 M21 12H9");

		/// <summary>Triangle / play icon.</summary>
		public static readonly MokaIconDefinition Play = new("play",
			"M7 4.5v15a1 1 0 0 0 1.5.86l12-7.5a1 1 0 0 0 0-1.72l-12-7.5A1 1 0 0 0 7 4.5z");

		/// <summary>Two bars / pause icon.</summary>
		public static readonly MokaIconDefinition Pause = new("pause",
			"M6 4h4v16H6z M14 4h4v16h-4z");

		/// <summary>Square / stop icon.</summary>
		public static readonly MokaIconDefinition Stop = new("stop",
			"M6 6h12v12H6z");

		/// <summary>Dot inside a ring / record icon.</summary>
		public static readonly MokaIconDefinition Record = new("record",
			"M22 12a10 10 0 1 1-20 0 10 10 0 0 1 20 0z M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0z");

		/// <summary>Triangle over a bar / eject icon.</summary>
		public static readonly MokaIconDefinition Eject = new("eject",
			"M12 5l7 8H5zM5 18h14");

		/// <summary>Play inside a circle.</summary>
		public static readonly MokaIconDefinition PlayCircle = new("play-circle",
			"M9 9.003a1 1 0 0 1 1.517-.859l4.997 2.997a1 1 0 0 1 0 1.718l-4.997 2.997A1 1 0 0 1 9 14.996z M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z");

		/// <summary>Pause inside a circle.</summary>
		public static readonly MokaIconDefinition PauseCircle = new("pause-circle",
			"M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z M10 15L10 9 M14 15L14 9");

		/// <summary>Stop inside a circle.</summary>
		public static readonly MokaIconDefinition StopCircle = new("stop-circle",
			"M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z M10 9h4a1 1 0 0 1 1 1v4a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1v-4a1 1 0 0 1 1 -1z");

		/// <summary>Skip to the previous track.</summary>
		public static readonly MokaIconDefinition SkipBack = new("skip-back",
			"M17.971 4.285A2 2 0 0 1 21 6v12a2 2 0 0 1-3.029 1.715l-9.997-5.998a2 2 0 0 1-.003-3.432z M3 20V4");

		/// <summary>Skip to the next track.</summary>
		public static readonly MokaIconDefinition SkipForward = new("skip-forward",
			"M21 4v16 M6.029 4.285A2 2 0 0 0 3 6v12a2 2 0 0 0 3.029 1.715l9.997-5.998a2 2 0 0 0 .003-3.432z");

		/// <summary>Double triangle pointing left / rewind icon.</summary>
		public static readonly MokaIconDefinition Rewind = new("rewind",
			"M12 6a2 2 0 0 0-3.414-1.414l-6 6a2 2 0 0 0 0 2.828l6 6A2 2 0 0 0 12 18z M22 6a2 2 0 0 0-3.414-1.414l-6 6a2 2 0 0 0 0 2.828l6 6A2 2 0 0 0 22 18z");

		/// <summary>Double triangle pointing right / fast forward icon.</summary>
		public static readonly MokaIconDefinition FastForward = new("fast-forward",
			"M12 6a2 2 0 0 1 3.414-1.414l6 6a2 2 0 0 1 0 2.828l-6 6A2 2 0 0 1 12 18z M2 6a2 2 0 0 1 3.414-1.414l6 6a2 2 0 0 1 0 2.828l-6 6A2 2 0 0 1 2 18z");

		/// <summary>Two arrows in a loop / repeat icon.</summary>
		public static readonly MokaIconDefinition Repeat = new("repeat",
			"M17 1 L21 5 L17 9 M21 5 H7 a4 4 0 0 0 -4 4 V11 M7 23 L3 19 L7 15 M3 19 H17 a4 4 0 0 0 4 -4 V13");

		/// <summary>Crossing arrows / shuffle icon.</summary>
		public static readonly MokaIconDefinition Shuffle = new("shuffle",
			"M18 14l4 4-4 4 M18 2l4 4-4 4 M2 18h1.973a4 4 0 0 0 3.3-1.7l5.454-8.6a4 4 0 0 1 3.3-1.7H22 M2 6h1.972a4 4 0 0 1 3.6 2.2 M22 18h-6.041a4 4 0 0 1-3.3-1.8l-.359-.45");
	}

	/// <summary>Navigation icons - arrows, chevrons, menu, etc.</summary>
	public static class Navigation
	{
		/// <summary>Left arrow.</summary>
		public static readonly MokaIconDefinition ArrowLeft = new("arrow-left",
			"M19 12H5 M12 19l-7-7 7-7");

		/// <summary>Right arrow.</summary>
		public static readonly MokaIconDefinition ArrowRight = new("arrow-right",
			"M5 12h14 M12 5l7 7-7 7");

		/// <summary>Up arrow.</summary>
		public static readonly MokaIconDefinition ArrowUp = new("arrow-up",
			"M12 19V5 M5 12l7-7 7 7");

		/// <summary>Down arrow.</summary>
		public static readonly MokaIconDefinition ArrowDown = new("arrow-down",
			"M12 5v14 M19 12l-7 7-7-7");

		/// <summary>Left chevron.</summary>
		public static readonly MokaIconDefinition ChevronLeft = new("chevron-left",
			"M15 18l-6-6 6-6");

		/// <summary>Right chevron.</summary>
		public static readonly MokaIconDefinition ChevronRight = new("chevron-right",
			"M9 18l6-6-6-6");

		/// <summary>Up chevron.</summary>
		public static readonly MokaIconDefinition ChevronUp = new("chevron-up",
			"M18 15l-6-6-6 6");

		/// <summary>Down chevron.</summary>
		public static readonly MokaIconDefinition ChevronDown = new("chevron-down",
			"M6 9l6 6 6-6");

		/// <summary>Hamburger menu.</summary>
		public static readonly MokaIconDefinition Menu = new("menu",
			"M3 12h18 M3 6h18 M3 18h18");

		/// <summary>X / close icon.</summary>
		public static readonly MokaIconDefinition Close = new("close",
			"M18 6L6 18 M6 6l12 12");

		/// <summary>House / home icon.</summary>
		public static readonly MokaIconDefinition Home = new("home",
			"M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z M9 22V12h6v10");

		/// <summary>Double chevron pointing left (jump to first).</summary>
		public static readonly MokaIconDefinition ChevronsLeft = new("chevrons-left",
			"M11 17l-5-5 5-5 M18 17l-5-5 5-5");

		/// <summary>Double chevron pointing right (jump to last).</summary>
		public static readonly MokaIconDefinition ChevronsRight = new("chevrons-right",
			"M13 17l5-5-5-5 M6 17l5-5-5-5");

		/// <summary>Chevrons pointing up and down, on something that opens a menu.</summary>
		public static readonly MokaIconDefinition ChevronsUpDown = new("chevrons-up-down",
			"M7 15l5 5 5-5 M7 9l5-5 5 5");

		/// <summary>Panel docking back into its container.</summary>
		public static readonly MokaIconDefinition Dock = new("dock",
			"M3 3h18v18H3z M15 9l-6 6 M9 15v-4.5 M9 15h4.5");

		/// <summary>Panel floating out of its container.</summary>
		public static readonly MokaIconDefinition Undock = new("undock",
			"M3 3h18v18H3z M9 15l6-6 M15 9v4.5 M15 9h-4.5");

		/// <summary>Three horizontal dots.</summary>
		public static readonly MokaIconDefinition MoreHorizontal = new("more-horizontal",
			"M12 13a1 1 0 1 0 0-2 1 1 0 0 0 0 2z M19 13a1 1 0 1 0 0-2 1 1 0 0 0 0 2z M5 13a1 1 0 1 0 0-2 1 1 0 0 0 0 2z");

		/// <summary>Three vertical dots.</summary>
		public static readonly MokaIconDefinition MoreVertical = new("more-vertical",
			"M12 13a1 1 0 1 0 0-2 1 1 0 0 0 0 2z M12 6a1 1 0 1 0 0-2 1 1 0 0 0 0 2z M12 20a1 1 0 1 0 0-2 1 1 0 0 0 0 2z");

		/// <summary>Opposed vertical arrows.</summary>
		public static readonly MokaIconDefinition ArrowUpDown = new("arrow-up-down",
			"M7 21V3 M3 7l4-4 4 4 M17 3v18 M21 17l-4 4-4-4");

		/// <summary>Arrow rising to a line.</summary>
		public static readonly MokaIconDefinition ArrowUpToLine = new("arrow-up-to-line",
			"M4 4h16 M12 20V8 M7 13l5-5 5 5");

		/// <summary>Arrow dropping to a line.</summary>
		public static readonly MokaIconDefinition ArrowDownToLine = new("arrow-down-to-line",
			"M12 17V3 M6 11l6 6 6-6 M19 21H5");

		/// <summary>Up arrow inside a circle.</summary>
		public static readonly MokaIconDefinition ArrowUpCircle = new("arrow-up-circle",
			"M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18z M12 16.5V8 M8.5 11.5L12 8l3.5 3.5");

		/// <summary>Down arrow inside a circle.</summary>
		public static readonly MokaIconDefinition ArrowDownCircle = new("arrow-down-circle",
			"M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z M12 8v8 M8 12l4 4 4-4");

		/// <summary>Four tiles of uneven height / dashboard icon.</summary>
		public static readonly MokaIconDefinition Dashboard = new("dashboard",
			"M4 3h5a1 1 0 0 1 1 1v7a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1z M15 3h5a1 1 0 0 1 1 1v3a1 1 0 0 1-1 1h-5a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1z M15 12h5a1 1 0 0 1 1 1v7a1 1 0 0 1-1 1h-5a1 1 0 0 1-1-1v-7a1 1 0 0 1 1-1z M4 16h5a1 1 0 0 1 1 1v3a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1v-3a1 1 0 0 1 1-1z");

		/// <summary>Four tiles / grid view icon.</summary>
		public static readonly MokaIconDefinition Grid = new("grid",
			"M4 4h6v6H4z M14 4h6v6h-6z M4 14h6v6H4z M14 14h6v6h-6z");

		/// <summary>A page split into a header, a side and a body.</summary>
		public static readonly MokaIconDefinition Template = new("template",
			"M3 3 H21 V21 H3 Z M3 9 H21 M9 21 V9");

		/// <summary>Application window with a title bar.</summary>
		public static readonly MokaIconDefinition AppWindow = new("app-window",
			"M4 4h16a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2z M2 9h20 M6 6.5h.01 M9 6.5h.01");

		/// <summary>Box with a panel down its left side.</summary>
		public static readonly MokaIconDefinition PanelLeft = new("panel-left",
			"M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2z M9 3v18");

		/// <summary>Box with a panel down its right side.</summary>
		public static readonly MokaIconDefinition PanelRight = new("panel-right",
			"M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2z M15 3v18");

		/// <summary>Box with a panel along its top.</summary>
		public static readonly MokaIconDefinition PanelTop = new("panel-top",
			"M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2z M3 9h18");

		/// <summary>Box with a panel along its bottom.</summary>
		public static readonly MokaIconDefinition PanelBottom = new("panel-bottom",
			"M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2z M3 15h18");

		/// <summary>Box split side by side.</summary>
		public static readonly MokaIconDefinition SplitView = new("split-view",
			"M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2z M12 3v18");

		/// <summary>Box split one above the other.</summary>
		public static readonly MokaIconDefinition SplitStacked = new("split-stacked",
			"M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2z M3 12h18");

		/// <summary>Box split into one tall and two short panes.</summary>
		public static readonly MokaIconDefinition SplitThree = new("split-three",
			"M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2z M12 3v18 M12 12h9");

		/// <summary>Box split into four panes.</summary>
		public static readonly MokaIconDefinition SplitGrid = new("split-grid",
			"M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2z M12 3v18 M3 12h18");

		/// <summary>Four corners pointing out / maximize icon.</summary>
		public static readonly MokaIconDefinition Maximize = new("maximize",
			"M8 3H5a2 2 0 0 0-2 2v3 M21 8V5a2 2 0 0 0-2-2h-3 M3 16v3a2 2 0 0 0 2 2h3 M16 21h3a2 2 0 0 0 2-2v-3");

		/// <summary>Four corners pointing in / minimize icon.</summary>
		public static readonly MokaIconDefinition Minimize = new("minimize",
			"M8 3v3a2 2 0 0 1-2 2H3 M21 8h-3a2 2 0 0 1-2-2V3 M3 16h3a2 2 0 0 1 2 2v3 M16 21v-3a2 2 0 0 1 2-2h3");

		/// <summary>Two arrows pointing away along a diagonal / resize icon.</summary>
		public static readonly MokaIconDefinition Resize = new("resize",
			"M15 3L21 3L21 9 M9 21L3 21L3 15 M21 3L14 10 M3 21L10 14");

		/// <summary>Arrows in four directions around a dot, the pan tool.</summary>
		public static readonly MokaIconDefinition Pan = new("pan",
			"M12,1 L12,7 M10,3 L12,1 L14,3 M1,12 L7,12 M3,10 L1,12 L3,14 M12,23 L12,17 M10,21 L12,23 L14,21 M23,12 L17,12 M21,10 L23,12 L21,14 M12 14a2 2 0 1 0 0-4 2 2 0 0 0 0 4z");

		/// <summary>One line dividing into two arrows.</summary>
		public static readonly MokaIconDefinition Split = new("split",
			"M16 3h5v5 M8 3H3v5 M12 22v-8.3a4 4 0 0 0-1.17-2.87L3 3 M15 9l6-6");

		/// <summary>Winding line between two points.</summary>
		public static readonly MokaIconDefinition Route = new("route",
			"M6 19a2 2 0 1 1-4 0 2 2 0 0 1 4 0z M22 5a2 2 0 1 1-4 0 2 2 0 0 1 4 0z M6 19h7a4 4 0 0 0 0-8h-2a4 4 0 0 1 0-8h7");
	}

	/// <summary>Status icons - check, warning, error, info, etc.</summary>
	public static class Status
	{
		/// <summary>Checkmark.</summary>
		public static readonly MokaIconDefinition Check = new("check",
			"M20 6L9 17l-5-5");

		/// <summary>Checkmark inside a circle.</summary>
		public static readonly MokaIconDefinition CheckCircle = new("check-circle",
			"M22 11.08V12a10 10 0 1 1-5.93-9.14 M22 4L12 14.01l-3-3");

		/// <summary>Triangle with exclamation / warning icon.</summary>
		public static readonly MokaIconDefinition Warning = new("warning",
			"M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z M12 9v4 M12 17h.01");

		/// <summary>Circle with X / error icon.</summary>
		public static readonly MokaIconDefinition Error = new("error",
			"M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z M15 9l-6 6 M9 9l6 6");

		/// <summary>Circle with an exclamation mark, for something that failed.</summary>
		public static readonly MokaIconDefinition AlertCircle = new("alert-circle",
			"M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z M12 8v4 M12 16h.01");

		/// <summary>Circle with i / info icon.</summary>
		public static readonly MokaIconDefinition Info = new("info",
			"M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z M12 16v-4 M12 8h.01");

		/// <summary>Circle with question mark.</summary>
		public static readonly MokaIconDefinition HelpCircle = new("help-circle",
			"M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3 M12 17h.01");

		/// <summary>Clock / time icon.</summary>
		public static readonly MokaIconDefinition Clock = new("clock",
			"M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z M12 6v6l4 2");

		/// <summary>Spinning loader circle.</summary>
		public static readonly MokaIconDefinition Loading = new("loading",
			"M12 2v4 M12 18v4 M4.93 4.93l2.83 2.83 M16.24 16.24l2.83 2.83 M2 12h4 M18 12h4 M4.93 19.07l2.83-2.83 M16.24 7.76l2.83-2.83");

		/// <summary>
		///     An open ring, the arc a busy button and <c>MokaSpinner</c> draw. Give it the <c>moka-spin</c> class from
		///     <c>moka.css</c> to turn it; under reduced motion it stands still.
		/// </summary>
		public static readonly MokaIconDefinition Spinner = new("spinner",
			"M22 12a10 10 0 1 1-7.16-9.59");

		/// <summary>Bell / notification icon.</summary>
		public static readonly MokaIconDefinition Bell = new("bell",
			"M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9 M13.73 21a2 2 0 0 1-3.46 0");

		/// <summary>Scalloped badge with a checkmark / verified icon.</summary>
		public static readonly MokaIconDefinition BadgeCheck = new("badge-check",
			"M3.85 8.62a4 4 0 0 1 4.78-4.77 4 4 0 0 1 6.74 0 4 4 0 0 1 4.78 4.78 4 4 0 0 1 0 6.74 4 4 0 0 1-4.77 4.78 4 4 0 0 1-6.75 0 4 4 0 0 1-4.78-4.77 4 4 0 0 1 0-6.76Z M16 9l-5.5 5.5L8 12");

		/// <summary>Circle with a slash / blocked icon.</summary>
		public static readonly MokaIconDefinition Ban = new("ban",
			"M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z M4.9 4.9l14.2 14.2");

		/// <summary>Pulse line / activity icon.</summary>
		public static readonly MokaIconDefinition Activity = new("activity",
			"M22 12h-4l-3 9L9 3l-3 9H2");

		/// <summary>Dial with a needle / gauge icon.</summary>
		public static readonly MokaIconDefinition Gauge = new("gauge",
			"M12 14l4-4 M3.34 19a10 10 0 1 1 17.32 0");

		/// <summary>Line trending up.</summary>
		public static readonly MokaIconDefinition TrendingUp = new("trending-up",
			"M16 7h6v6 M22 7l-8.5 8.5-5-5L2 17");

		/// <summary>Line trending down.</summary>
		public static readonly MokaIconDefinition TrendingDown = new("trending-down",
			"M16 17h6v-6 M22 17l-8.5-8.5-5 5L2 7");

		/// <summary>Shield / security icon.</summary>
		public static readonly MokaIconDefinition Shield = new("shield",
			"M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z");

		/// <summary>Shield with a checkmark / protected.</summary>
		public static readonly MokaIconDefinition ShieldCheck = new("shield-check",
			"M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z M9 12l2 2 4-4");

		/// <summary>Shield with an exclamation mark / at risk.</summary>
		public static readonly MokaIconDefinition ShieldAlert = new("shield-alert",
			"M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z M12 8v4 M12 16h.01");

		/// <summary>Struck-through shield / unprotected.</summary>
		public static readonly MokaIconDefinition ShieldOff = new("shield-off",
			"M2 2l20 20 M5 5a1 1 0 0 0-1 1v7c0 5 3.5 7.5 7.67 8.94a1 1 0 0 0 .67.01c2.35-.82 4.48-1.97 5.9-3.71 M9.309 3.652A12.252 12.252 0 0 0 11.24 2.28a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1v7a9.784 9.784 0 0 1-.08 1.264");

		/// <summary>Shield with an X / blocked by security.</summary>
		public static readonly MokaIconDefinition ShieldX = new("shield-x",
			"M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z M14.5 9.5l-5 5 M9.5 9.5l5 5");

		/// <summary>Shield with a slash / denied.</summary>
		public static readonly MokaIconDefinition ShieldBan = new("shield-ban",
			"M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z M4.243 5.21l14.39 12.472");

		/// <summary>Shield with a plus / add protection.</summary>
		public static readonly MokaIconDefinition ShieldPlus = new("shield-plus",
			"M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z M9 12h6 M12 9v6");

		/// <summary>Shield with a question mark / unverified.</summary>
		public static readonly MokaIconDefinition ShieldQuestion = new("shield-question",
			"M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z M9.1 9a3 3 0 0 1 5.82 1c0 2-3 3-3 3 M12 17h.01");

		/// <summary>Wi-Fi arcs / connected icon.</summary>
		public static readonly MokaIconDefinition Wifi = new("wifi",
			"M12 20h.01 M2 8.82a15 15 0 0 1 20 0 M5 12.86a10 10 0 0 1 14 0 M8.5 16.43a5 5 0 0 1 7 0");

		/// <summary>Struck-through Wi-Fi arcs / offline icon.</summary>
		public static readonly MokaIconDefinition WifiOff = new("wifi-off",
			"M12 20h.01 M8.5 16.43a5 5 0 0 1 7 0 M2 8.82a15 15 0 0 1 4.17-2.65 M10.66 5c4.01-.36 8.14.9 11.34 3.76 M16.85 11.25a10 10 0 0 1 2.22 1.68 M5 13a10 10 0 0 1 5.24-2.76 M2 2l20 20");

		/// <summary>Four rising bars / signal strength.</summary>
		public static readonly MokaIconDefinition Signal = new("signal",
			"M4 20v-3M9 20v-7M14 20V9M19 20V4");

		/// <summary>Struck-through cloud / cloud unreachable.</summary>
		public static readonly MokaIconDefinition CloudOff = new("cloud-off",
			"M10.94 5.274A7 7 0 0 1 15.71 10h1.79a4.5 4.5 0 0 1 4.222 6.057 M18.796 18.81A4.5 4.5 0 0 1 17.5 19H9A7 7 0 0 1 5.79 5.78 M2 2l20 20");

		/// <summary>Struck-through server / server down.</summary>
		public static readonly MokaIconDefinition ServerOff = new("server-off",
			"M7 2h13a2 2 0 0 1 2 2v4a2 2 0 0 1-2 2h-5 M10 10 2.5 2.5C2 2 2 2.5 2 5v3a2 2 0 0 0 2 2h6z M22 17v-1a2 2 0 0 0-2-2h-1 M4 14a2 2 0 0 0-2 2v4a2 2 0 0 0 2 2h16.5l1-.5.5.5-8-8H4z M6 18h.01 M2 2l20 20");

		/// <summary>Server with a lightning bolt / server failed.</summary>
		public static readonly MokaIconDefinition ServerCrash = new("server-crash",
			"M6 10H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v4a2 2 0 0 1-2 2h-2 M6 14H4a2 2 0 0 0-2 2v4a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-4a2 2 0 0 0-2-2h-2 M6 6h.01 M6 18h.01 M13 6l-4 6h6l-4 6");

		/// <summary>Hourglass / waiting icon.</summary>
		public static readonly MokaIconDefinition Hourglass = new("hourglass",
			"M5 22h14 M5 2h14 M17 22v-4.172a2 2 0 0 0-.586-1.414L12 12l-4.414 4.414A2 2 0 0 0 7 17.828V22 M7 2v4.172a2 2 0 0 0 .586 1.414L12 12l4.414-4.414A2 2 0 0 0 17 6.172V2");

		/// <summary>Stopwatch / timer icon.</summary>
		public static readonly MokaIconDefinition Timer = new("timer",
			"M10 2L14 2 M12 14L15 11 M12 22a8 8 0 1 0 0-16 8 8 0 0 0 0 16z");

		/// <summary>Struck-through stopwatch / no time limit.</summary>
		public static readonly MokaIconDefinition TimerOff = new("timer-off",
			"M10 2h4 M4.6 11a8 8 0 0 0 1.7 8.7 8 8 0 0 0 8.7 1.7 M7.4 7.4a8 8 0 0 1 10.3 1 8 8 0 0 1 .9 10.2 M2 2l20 20 M12 12v-2");

		/// <summary>Alarm clock.</summary>
		public static readonly MokaIconDefinition AlarmClock = new("alarm-clock",
			"M12 21a8 8 0 1 0 0-16 8 8 0 0 0 0 16z M12 9v4l2 2 M5 3 2 6 M22 6l-3-3 M6.38 18.7 4 21 M17.64 18.67 20 21");
	}

	/// <summary>Content icons - copy, paste, link, filter, etc.</summary>
	public static class Content
	{
		/// <summary>Two overlapping squares / copy icon.</summary>
		public static readonly MokaIconDefinition Copy = new("copy",
			"M20 9h-9a2 2 0 0 0-2 2v9a2 2 0 0 0 2 2h9a2 2 0 0 0 2-2v-9a2 2 0 0 0-2-2z M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1");

		/// <summary>Clipboard / paste icon.</summary>
		public static readonly MokaIconDefinition Paste = new("paste",
			"M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2 M15 2H9a1 1 0 0 0-1 1v2a1 1 0 0 0 1 1h6a1 1 0 0 0 1-1V3a1 1 0 0 0-1-1z");

		/// <summary>Chain link icon.</summary>
		public static readonly MokaIconDefinition Link = new("link",
			"M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71 M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71");

		/// <summary>Calendar grid with a header bar.</summary>
		public static readonly MokaIconDefinition Calendar = new("calendar",
			"M3 4h18v18H3z M16 2v4 M8 2v4 M3 10h18");

		/// <summary>Eyedropper / colour picker.</summary>
		public static readonly MokaIconDefinition EyeDropper = new("eye-dropper",
			"M2 22l1-1h3l9-9 M3 21v-3l9-9 M15 6l3-3 3 3-3 3z M12 9l3 3");

		/// <summary>Arrow leaving a box (opens in a new tab).</summary>
		public static readonly MokaIconDefinition ExternalLink = new("external-link",
			"M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6 M15 3h6v6 M10 14L21 3");

		/// <summary>Magnifier with a plus (zoom in).</summary>
		public static readonly MokaIconDefinition ZoomIn = new("zoom-in",
			"M11 19a8 8 0 1 0 0-16 8 8 0 0 0 0 16z M21 21l-4.35-4.35 M11 8v6 M8 11h6");

		/// <summary>Broken chain link icon.</summary>
		public static readonly MokaIconDefinition Unlink = new("unlink",
			"M18.84 12.25l1.72-1.71a5 5 0 0 0-7.07-7.07l-1.72 1.71 M5.16 11.75l-1.72 1.71a5 5 0 0 0 7.07 7.07l1.72-1.71 M8 2v3 M2 8h3 M16 22v-3 M22 16h-3");

		/// <summary>Image / picture icon.</summary>
		public static readonly MokaIconDefinition Image = new("image",
			"M19 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V5a2 2 0 0 0-2-2z M8.5 10a1.5 1.5 0 1 0 0-3 1.5 1.5 0 0 0 0 3z M21 15l-5-5L5 21");

		/// <summary>Paperclip / attachment icon.</summary>
		public static readonly MokaIconDefinition Attachment = new("attachment",
			"M21.44 11.05l-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48");

		/// <summary>Funnel / filter icon.</summary>
		public static readonly MokaIconDefinition Filter = new("filter",
			"M22 3H2l8 9.46V19l4 2v-8.54L22 3z");

		/// <summary>Sort arrows icon.</summary>
		public static readonly MokaIconDefinition Sort = new("sort",
			"M11 5h10 M11 9h7 M11 13h4 M3 17l3 3 3-3 M6 18V4");

		/// <summary>Four-pane grid, used for column visibility toggles.</summary>
		public static readonly MokaIconDefinition Columns = new("columns",
			"M3 3h7v7H3z M14 3h7v7h-7z M3 14h7v7H3z M14 14h7v7h-7z");

		/// <summary>Three evenly spaced rows (comfortable density).</summary>
		public static readonly MokaIconDefinition RowsComfortable = new("rows-comfortable",
			"M4 6h16 M4 12h16 M4 18h16");

		/// <summary>Four tightly spaced rows (dense density).</summary>
		public static readonly MokaIconDefinition RowsDense = new("rows-dense",
			"M4 5h16 M4 9h16 M4 13h16 M4 17h16");

		/// <summary>Opposed chevrons indicating an unsorted, sortable column.</summary>
		public static readonly MokaIconDefinition SortNeutral = new("sort-neutral",
			"M8 10l4-4 4 4 M8 14l4 4 4-4");

		/// <summary>A globe: a place with no country, or the whole world.</summary>
		public static readonly MokaIconDefinition Globe = new("globe",
			"M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z M12 2a14.5 14.5 0 0 0 0 20 14.5 14.5 0 0 0 0-20 M2 12h20");

		/// <summary>A camera body with its lens.</summary>
		public static readonly MokaIconDefinition Camera = new("camera",
			"M14.5 4h-5L7 7H4a2 2 0 0 0-2 2v9a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V9a2 2 0 0 0-2-2h-3z M15 13a3 3 0 1 1-6 0 3 3 0 0 1 6 0z");

		/// <summary>A QR code: three finder squares and scattered modules.</summary>
		public static readonly MokaIconDefinition QrCode = new("qr-code",
			"M3 3h6v6H3z M15 3h6v6h-6z M3 15h6v6H3z M21 15h-3a2 2 0 0 0-2 2v4 M21 21v.01 M12 7v3a2 2 0 0 1-2 2H7 M3 12h.01 M12 3h.01 M12 16v.01 M16 12h1 M21 12v.01 M12 21v-1");

		/// <summary>A balance scale, for law, policy or comparing two things.</summary>
		public static readonly MokaIconDefinition Scale = new("scale",
			"M16 16l3-8 3 8c-.87.65-1.92 1-3 1s-2.13-.35-3-1z M2 16l3-8 3 8c-.87.65-1.92 1-3 1s-2.13-.35-3-1z M7 21h10 M12 3v18 M3 7h2c2 0 5-1 7-2 2 1 5 2 7 2h2");

		/// <summary>Scissors / cut icon.</summary>
		public static readonly MokaIconDefinition Cut = new("cut",
			"M9 6a3 3 0 1 1-6 0 3 3 0 0 1 6 0z M9 18a3 3 0 1 1-6 0 3 3 0 0 1 6 0z M20 4L8.12 15.88 M14.47 14.48L20 20 M8.12 8.12L12 12");

		/// <summary>Magnifier with a minus (zoom out).</summary>
		public static readonly MokaIconDefinition ZoomOut = new("zoom-out",
			"M11 19a8 8 0 1 0 0-16 8 8 0 0 0 0 16z M21 21L16.65 16.65 M8 11L14 11");

		/// <summary>Person / user icon.</summary>
		public static readonly MokaIconDefinition User = new("user",
			"M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2 M12 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z");

		/// <summary>Two people / users or group icon.</summary>
		public static readonly MokaIconDefinition Users = new("users",
			"M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2 M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z M23 21v-2a4 4 0 0 0-3-3.87 M16 3.13a4 4 0 0 1 0 7.75");

		/// <summary>Person with a plus / add user.</summary>
		public static readonly MokaIconDefinition UserPlus = new("user-plus",
			"M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2 M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z M19 8L19 14 M22 11L16 11");

		/// <summary>Person with a minus / remove user.</summary>
		public static readonly MokaIconDefinition UserMinus = new("user-minus",
			"M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2 M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z M22 11L16 11");

		/// <summary>Person with a checkmark / approved user.</summary>
		public static readonly MokaIconDefinition UserCheck = new("user-check",
			"M16 11l2 2 4-4 M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2 M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z");

		/// <summary>Person with an X / blocked user.</summary>
		public static readonly MokaIconDefinition UserX = new("user-x",
			"M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2 M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z M17 8L22 13 M22 8L17 13");

		/// <summary>Person with a gear / account settings.</summary>
		public static readonly MokaIconDefinition UserCog = new("user-cog",
			"M10 15H6a4 4 0 0 0-4 4v2 M14.305 16.53l.923-.382 M15.228 13.852l-.923-.383 M16.852 12.228l-.383-.923 M16.852 17.772l-.383.924 M19.148 12.228l.383-.923 M19.53 18.696l-.382-.924 M20.772 13.852l.924-.383 M20.772 16.148l.924.383 M18 18a3 3 0 1 0 0-6 3 3 0 0 0 0 6z M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z");

		/// <summary>Person with a lock / restricted user.</summary>
		public static readonly MokaIconDefinition UserLock = new("user-lock",
			"M19 16v-2a2 2 0 0 0-4 0v2 M9.5 15H7a4 4 0 0 0-4 4v2 M10 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z M13.899 16h6.202a.899 .899 0 0 1 .899 .899v3.202a.899 .899 0 0 1 -.899 .899h-6.202a.899 .899 0 0 1 -.899 -.899v-3.202a.899 .899 0 0 1 .899 -.899z");

		/// <summary>Person inside a circle / account icon.</summary>
		public static readonly MokaIconDefinition UserCircle = new("user-circle",
			"M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z M12 13a3 3 0 1 0 0-6 3 3 0 0 0 0 6z M7 20.662V19a2 2 0 0 1 2-2h6a2 2 0 0 1 2 2v1.662");

		/// <summary>Identity card.</summary>
		public static readonly MokaIconDefinition IdCard = new("id-card",
			"M13 19a4 4 0 00-8 0 M16 10h2 M16 14h2 M9 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z M4 5h16a2 2 0 0 1 2 2v10a2 2 0 0 1 -2 2h-16a2 2 0 0 1 -2 -2v-10a2 2 0 0 1 2 -2z");

		/// <summary>Crown, for an owner or top tier.</summary>
		public static readonly MokaIconDefinition Crown = new("crown",
			"M11.562 3.266a.5.5 0 0 1 .876 0L15.39 8.87a1 1 0 0 0 1.516.294L21.183 5.5a.5.5 0 0 1 .798.519l-2.834 10.246a1 1 0 0 1-.956.734H5.81a1 1 0 0 1-.957-.734L2.02 6.02a.5.5 0 0 1 .798-.519l4.276 3.664a1 1 0 0 0 1.516-.294z M5 21h14");

		/// <summary>Key.</summary>
		public static readonly MokaIconDefinition Key = new("key",
			"M21 2l-2 2m-7.61 7.61a5.5 5.5 0 1 1-7.78 7.78 5.5 5.5 0 0 1 7.78-7.78zm0 0L15.5 7.5m0 0l3 3L22 7l-3-3m-3.5 3.5L19 4");

		/// <summary>Key with a round head, for passkeys.</summary>
		public static readonly MokaIconDefinition KeyRound = new("key-round",
			"M2.586 17.414A2 2 0 0 0 2 18.828V21a1 1 0 0 0 1 1h3a1 1 0 0 0 1-1v-1a1 1 0 0 1 1-1h1a1 1 0 0 0 1-1v-1a1 1 0 0 1 1-1h.172a2 2 0 0 0 1.414-.586l.814-.814a6.5 6.5 0 1 0-4-4z M16.5 8a.5 .5 0 1 0 0-1 .5 .5 0 0 0 0 1z");

		/// <summary>Fingerprint.</summary>
		public static readonly MokaIconDefinition Fingerprint = new("fingerprint",
			"M12 10a2 2 0 0 0-2 2c0 1.02-.1 2.51-.26 4 M14 13.12c0 2.38 0 6.38-1 8.88 M17.29 21.02c.12-.6.43-2.3.5-3.02 M2 12a10 10 0 0 1 18-6 M2 16h.01 M21.8 16c.2-2 .131-5.354 0-6 M5 19.5C5.5 18 6 15 6 12a6 6 0 0 1 .34-2 M8.65 22c.21-.66.45-1.32.57-2 M9 6.8a6 6 0 0 1 9 5.2v2");

		/// <summary>Face inside viewfinder corners.</summary>
		public static readonly MokaIconDefinition ScanFace = new("scan-face",
			"M3 7V5a2 2 0 0 1 2-2h2 M17 3h2a2 2 0 0 1 2 2v2 M21 17v2a2 2 0 0 1-2 2h-2 M7 21H5a2 2 0 0 1-2-2v-2 M8 14s1.5 2 4 2 4-2 4-2 M9 9h.01 M15 9h.01");

		/// <summary>Certificate with a rosette.</summary>
		public static readonly MokaIconDefinition Certificate = new("certificate",
			"M4 4h16v11H13 M4 4v11h3 M8 8h8 M8 11h3 M14 16a2.5 2.5 0 1 1-5 0 2.5 2.5 0 0 1 5 0z M10 18v4l1.5-1 1.5 1v-4");

		/// <summary>Pen over a signature line.</summary>
		public static readonly MokaIconDefinition Signature = new("signature",
			"M3 21c3 0 4-3 6-3s2 2 4 2 3-2 5-2 M14.5 3.5l3 3L9 15l-4 1 1-4z");

		/// <summary>Tray / inbox icon.</summary>
		public static readonly MokaIconDefinition Inbox = new("inbox",
			"M22 12h-6l-2 3h-4l-2-3H2 M5.45 5.11L2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z");

		/// <summary>Square speech bubble / comment icon.</summary>
		public static readonly MokaIconDefinition Comment = new("comment",
			"M4 5 H20 V16 H13 L8 21 V16 H4 Z");

		/// <summary>Desktop monitor.</summary>
		public static readonly MokaIconDefinition Monitor = new("monitor",
			"M4 3h16a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2z M8 21h8 M12 17v4");

		/// <summary>Laptop.</summary>
		public static readonly MokaIconDefinition Laptop = new("laptop",
			"M20 16V7a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v9m16 0H4m16 0 1.28 2.55a1 1 0 0 1-.9 1.45H3.62a1 1 0 0 1-.9-1.45L4 16");

		/// <summary>Smartphone.</summary>
		public static readonly MokaIconDefinition Smartphone = new("smartphone",
			"M7 2h10a2 2 0 0 1 2 2v16a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2z M12 18h.01");

		/// <summary>Tablet.</summary>
		public static readonly MokaIconDefinition Tablet = new("tablet",
			"M6 2h12a1 1 0 0 1 1 1v18a1 1 0 0 1-1 1H6a1 1 0 0 1-1-1V3a1 1 0 0 1 1-1zM11 18h2");

		/// <summary>Wrist watch.</summary>
		public static readonly MokaIconDefinition Watch = new("watch",
			"M8 7h8a1 1 0 0 1 1 1v8a1 1 0 0 1-1 1H8a1 1 0 0 1-1-1V8a1 1 0 0 1 1-1zM9 7l1-4h4l1 4M9 17l1 4h4l1-4");

		/// <summary>Television.</summary>
		public static readonly MokaIconDefinition Tv = new("tv",
			"M17 2l-5 5-5-5 M4 7h16a2 2 0 0 1 2 2v11a2 2 0 0 1 -2 2h-16a2 2 0 0 1 -2 -2v-11a2 2 0 0 1 2 -2z");

		/// <summary>Game controller.</summary>
		public static readonly MokaIconDefinition Gamepad = new("gamepad",
			"M6 9h12a4 4 0 0 1 0 8c-2 0-3-2-6-2s-4 2-6 2a4 4 0 0 1 0-8zM8 11v3M6.5 12.5h3");

		/// <summary>Printer.</summary>
		public static readonly MokaIconDefinition Printer = new("printer",
			"M6 9V3h12v6M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2M6 14h12v7H6z");

		/// <summary>Loudspeaker cabinet.</summary>
		public static readonly MokaIconDefinition Speaker = new("speaker",
			"M6 2h12a1 1 0 0 1 1 1v18a1 1 0 0 1-1 1H6a1 1 0 0 1-1-1V3a1 1 0 0 1 1-1zM8 15a4 4 0 1 0 8 0a4 4 0 1 0-8 0M12 6h.01");

		/// <summary>Keyboard.</summary>
		public static readonly MokaIconDefinition Keyboard = new("keyboard",
			"M4 4h16a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2z M6 8h.01 M10 8h.01 M14 8h.01 M18 8h.01 M8 12h.01 M12 12h.01 M16 12h.01 M7 16h10");

		/// <summary>Command key symbol.</summary>
		public static readonly MokaIconDefinition Command = new("command",
			"M15 6v12a3 3 0 1 0 3-3H6a3 3 0 1 0 3 3V6a3 3 0 1 0-3 3h12a3 3 0 1 0-3-3");

		/// <summary>Mouse pointer.</summary>
		public static readonly MokaIconDefinition Cursor = new("cursor",
			"M7,2 L7,18.5 L11.2,14.5 L15,22 L17.2,21 L13.5,13.2 L19,12.7 Z",
			filled: true);

		/// <summary>Processor chip.</summary>
		public static readonly MokaIconDefinition Cpu = new("cpu",
			"M6 4h12a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2z M9 9h6v6H9z M9 1v3 M15 1v3 M9 20v3 M15 20v3 M20 9h3 M20 14h3 M1 9h3 M1 14h3");

		/// <summary>Two stacked server units.</summary>
		public static readonly MokaIconDefinition Server = new("server",
			"M4 2h16a2 2 0 0 1 2 2v4a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2z M4 14h16a2 2 0 0 1 2 2v4a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2v-4a2 2 0 0 1 2-2z M6 6h.01 M6 18h.01");

		/// <summary>Network router with antennas.</summary>
		public static readonly MokaIconDefinition Router = new("router",
			"M3 14h18v5H3zM7 16.5h.01M11 16.5h.01M8 14l-2-7M16 14l2-7");

		/// <summary>Ethernet socket.</summary>
		public static readonly MokaIconDefinition EthernetPort = new("ethernet-port",
			"M4 5h16a1 1 0 0 1 1 1v9a1 1 0 0 1-1 1h-3l-2 3H9l-2-3H4a1 1 0 0 1-1-1V6a1 1 0 0 1 1-1zM7 9v2M10.3 9v2M13.7 9v2M17 9v2");

		/// <summary>Three connected boxes / network icon.</summary>
		public static readonly MokaIconDefinition Network = new("network",
			"M16 16h6v6h-6z M2 16h6v6H2z M9 2h6v6H9z M5 16v-3a1 1 0 0 1 1-1h12a1 1 0 0 1 1 1v3 M12 12V8");

		/// <summary>Brick wall / firewall icon.</summary>
		public static readonly MokaIconDefinition Firewall = new("firewall",
			"M3 5h18v14H3zM3 10h18M3 15h18M9 5v5M15 10v5M9 15v4");

		/// <summary>Tunnel mouth, for a tunnelled connection.</summary>
		public static readonly MokaIconDefinition Tunnel = new("tunnel",
			"M2 12a10 6 0 0 1 20 0v6 M2 12v6 M7 12a5 3 0 0 1 10 0 M7 18v-6 M17 18v-6 M2 15h20");

		/// <summary>Dot with arcs on both sides / broadcast icon.</summary>
		public static readonly MokaIconDefinition Broadcast = new("broadcast",
			"M4.9 19.1a10 10 0 0 1 0-14.2 M7.8 16.2a6 6 0 0 1 0-8.4 M16.2 7.8a6 6 0 0 1 0 8.4 M19.1 4.9a10 10 0 0 1 0 14.2 M12 10a2 2 0 1 1 0 4 2 2 0 0 1 0-4z");

		/// <summary>Radio mast.</summary>
		public static readonly MokaIconDefinition RadioTower = new("radio-tower",
			"M4.9 16.1C1 12.2 1 5.8 4.9 1.9 M7.8 4.7a6.14 6.14 0 0 0-.8 7.5 M12 11a2 2 0 1 0 0-4 2 2 0 0 0 0 4z M16.2 4.8c2 2 2.26 5.11.8 7.47 M19.1 1.9a9.96 9.96 0 0 1 0 14.1 M9.5 18h5 M8 22l4-11 4 11");

		/// <summary>Bluetooth rune.</summary>
		public static readonly MokaIconDefinition Bluetooth = new("bluetooth",
			"M7 7l10 10-5 5V2l5 5L7 17");

		/// <summary>Feed arcs / RSS icon.</summary>
		public static readonly MokaIconDefinition Rss = new("rss",
			"M4 11a9 9 0 0 1 9 9 M4 4a16 16 0 0 1 16 16 M5 20a1 1 0 1 0 0-2 1 1 0 0 0 0 2z");

		/// <summary>Cable with two plugs.</summary>
		public static readonly MokaIconDefinition Cable = new("cable",
			"M17 19a1 1 0 0 1-1-1v-2a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2v2a1 1 0 0 1-1 1z M17 21v-2 M19 14V6.5a1 1 0 0 0-7 0v11a1 1 0 0 1-7 0V10 M21 21v-2 M3 5V3 M4 10a2 2 0 0 1-2-2V6a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2a2 2 0 0 1-2 2z M7 5V3");

		/// <summary>Power plug / connect icon.</summary>
		public static readonly MokaIconDefinition Plug = new("plug",
			"M12 22v-5 M9 8V2 M15 8V2 M18 8v5a4 4 0 0 1-4 4h-4a4 4 0 0 1-4-4V8z");

		/// <summary>Plug pulled apart / disconnect icon.</summary>
		public static readonly MokaIconDefinition Unplug = new("unplug",
			"M19 5l3-3 M2 22l3-3 M6.3 20.3a2.4 2.4 0 0 0 3.4 0L12 18l-6-6-2.3 2.3a2.4 2.4 0 0 0 0 3.4z M7.5 13.5L10 11 M10.5 16.5L13 14 M12 6l6 6 2.3-2.3a2.4 2.4 0 0 0 0-3.4l-2.6-2.6a2.4 2.4 0 0 0-3.4 0z");

		/// <summary>Points joined in a path.</summary>
		public static readonly MokaIconDefinition Waypoints = new("waypoints",
			"M10.586 5.414l-5.172 5.172 M18.586 13.414l-5.172 5.172 M6 12h12 M12 22a2 2 0 1 0 0-4 2 2 0 0 0 0 4z M12 6a2 2 0 1 0 0-4 2 2 0 0 0 0 4z M20 14a2 2 0 1 0 0-4 2 2 0 0 0 0 4z M4 14a2 2 0 1 0 0-4 2 2 0 0 0 0 4z");

		/// <summary>Webhook.</summary>
		public static readonly MokaIconDefinition Webhook = new("webhook",
			"M18 16.98h-5.99c-1.1 0-1.95.94-2.48 1.9A4 4 0 0 1 2 17c.01-.7.2-1.4.57-2 M6 17l3.13-5.78c.53-.97.1-2.18-.5-3.1a4 4 0 1 1 6.89-4.06 M12 6l3.13 5.73C15.66 12.7 16.9 13 18 13a4 4 0 0 1 0 8");

		/// <summary>Globe with a lock / private network.</summary>
		public static readonly MokaIconDefinition GlobeLock = new("globe-lock",
			"M15.686 15A14.5 14.5 0 0 1 12 22a14.5 14.5 0 0 1 0-20 10 10 0 1 0 9.542 13 M2 12h8.5 M20 6V4a2 2 0 1 0-4 0v2 M15 6h6a1 1 0 0 1 1 1v3a1 1 0 0 1 -1 1h-6a1 1 0 0 1 -1 -1v-3a1 1 0 0 1 1 -1z");

		/// <summary>Cloud.</summary>
		public static readonly MokaIconDefinition Cloud = new("cloud",
			"M7 18h10.5a4 4 0 0 0 .5-7.97A6 6 0 0 0 6.34 9.5 4.25 4.25 0 0 0 7 18z");

		/// <summary>Cloud with an up arrow.</summary>
		public static readonly MokaIconDefinition CloudUpload = new("cloud-upload",
			"M12 13v8 M4 14.899A7 7 0 1 1 15.71 8h1.79a4.5 4.5 0 0 1 2.5 8.242 M8 17l4-4 4 4");

		/// <summary>Cloud with a down arrow.</summary>
		public static readonly MokaIconDefinition CloudDownload = new("cloud-download",
			"M12 13v8l-4-4 M12 21l4-4 M4.393 15.269A7 7 0 1 1 15.71 8h1.79a4.5 4.5 0 0 1 2.436 8.284");

		/// <summary>Cloud with a checkmark / saved to the cloud.</summary>
		public static readonly MokaIconDefinition CloudCheck = new("cloud-check",
			"M17 15l-5.5 5.5L9 18 M5.516 16.07A7 7 0 1 1 15.71 8h1.79a4.5 4.5 0 0 1 3.501 7.327");

		/// <summary>Cloud with circling arrows / syncing.</summary>
		public static readonly MokaIconDefinition CloudSync = new("cloud-sync",
			"M17 18l-1.535 1.605a5 5 0 0 1-8-1.5 M17 22v-4h-4 M20.996 15.251A4.5 4.5 0 0 0 17.495 8h-1.79a7 7 0 1 0-12.709 5.607 M7 10v4h4 M7 14l1.535-1.605a5 5 0 0 1 8 1.5");

		/// <summary>Struck-through circular arrows / sync off.</summary>
		public static readonly MokaIconDefinition RefreshOff = new("refresh-off",
			"M21 8L18.74 5.74A9.75 9.75 0 0 0 12 3C11 3 10.03 3.16 9.13 3.47 M8 16H3v5 M3 12C3 9.51 4 7.26 5.64 5.64 M3 16l2.26 2.26A9.75 9.75 0 0 0 12 21c2.49 0 4.74-1 6.36-2.64 M21 12c0 1-.16 1.97-.47 2.87 M21 3v5h-5 M22 22 2 2");

		/// <summary>Lightning bolt.</summary>
		public static readonly MokaIconDefinition Zap = new("zap",
			"M15.914 4a1.5 1.5 0 00-2.474-1.561l-9 9A1.5 1.5 0 005.5 14h4.002a.5.5 0 01.471.666L8.086 20a1.5 1.5 0 002.475 1.56l9-9A1.5 1.5 0 0018.5 10h-3.997a.5.5 0 01-.472-.667z");

		/// <summary>Sparkle with small plus marks.</summary>
		public static readonly MokaIconDefinition Sparkles = new("sparkles",
			"M12 3l1.9 5.1L19 10l-5.1 1.9L12 17l-1.9-5.1L5 10l5.1-1.9z M19 3v4 M21 5h-4 M5 17v4 M7 19H3");

		/// <summary>Robot head.</summary>
		public static readonly MokaIconDefinition Bot = new("bot",
			"M12 3v3M7 6h10a3 3 0 0 1 3 3v7a3 3 0 0 1-3 3H7a3 3 0 0 1-3-3V9a3 3 0 0 1 3-3zM9 11.5h.01M15 11.5h.01M9.5 15.5h5M2 12v3M22 12v3");

		/// <summary>Puzzle piece / plugin icon.</summary>
		public static readonly MokaIconDefinition Puzzle = new("puzzle",
			"M9 3h6v4a2 2 0 1 0 0 4v4h-4a2 2 0 1 1-4 0H3V9h4a2 2 0 1 0 0-4V3z");

		/// <summary>Wrench / tools icon.</summary>
		public static readonly MokaIconDefinition Wrench = new("wrench",
			"M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.8-3.8a6 6 0 0 1-7.9 7.9l-6.9 6.9a2.1 2.1 0 0 1-3-3l6.9-6.9a6 6 0 0 1 7.9-7.9z");

		/// <summary>Hammer / build icon.</summary>
		public static readonly MokaIconDefinition Hammer = new("hammer",
			"M13.5 4.5l6 6-2.5 2.5-6-6z M12.25 8.25L4 16.5 7.5 20l8.25-8.25");

		/// <summary>Toolbox.</summary>
		public static readonly MokaIconDefinition Toolbox = new("toolbox",
			"M3 9h18v10a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1z M8 9V6a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1v3 M3 13h18 M10 13v2h4v-2");

		/// <summary>Paint palette / theme icon.</summary>
		public static readonly MokaIconDefinition Palette = new("palette",
			"M12 22a10 10 0 1 1 10-10c0 2.5-2 4-4.5 4H15a2 2 0 0 0-1.6 3.2A1.7 1.7 0 0 1 12 22z M7.5 10.5h.01 M10.5 7.5h.01 M14.5 7.5h.01 M17.5 10.5h.01");

		/// <summary>Paintbrush.</summary>
		public static readonly MokaIconDefinition Paintbrush = new("paintbrush",
			"M14.622 17.897l-10.68-2.913 M18.376 2.622a1 1 0 1 1 3.002 3.002L17.36 9.643a.5.5 0 0 0 0 .707l.944.944a2.41 2.41 0 0 1 0 3.408l-.944.944a.5.5 0 0 1-.707 0L8.354 7.348a.5.5 0 0 1 0-.707l.944-.944a2.41 2.41 0 0 1 3.408 0l.944.944a.5.5 0 0 0 .707 0z M9 8c-1.804 2.71-3.97 3.46-6.583 3.948a.507.507 0 0 0-.302.819l7.32 8.883a1 1 0 0 0 1.185.204C12.735 20.405 16 16.792 16 15");

		/// <summary>Paint bucket / fill icon.</summary>
		public static readonly MokaIconDefinition PaintBucket = new("paint-bucket",
			"M11 7 6 2 M18.992 12H2.041 M21.145 18.38A3.34 3.34 0 0 1 20 16.5a3.3 3.3 0 0 1-1.145 1.88c-.575.46-.855 1.02-.855 1.595A2 2 0 0 0 20 22a2 2 0 0 0 2-2.025c0-.58-.285-1.13-.855-1.595 M8.5 4.5l2.148-2.148a1.205 1.205 0 0 1 1.704 0l7.296 7.296a1.205 1.205 0 0 1 0 1.704l-7.592 7.592a3.615 3.615 0 0 1-5.112 0l-3.888-3.888a3.615 3.615 0 0 1 0-5.112L5.67 7.33");

		/// <summary>Water drop.</summary>
		public static readonly MokaIconDefinition Droplet = new("droplet",
			"M12 2.7c3.5 4.3 6 7.9 6 11.3a6 6 0 0 1-12 0c0-3.4 2.5-7 6-11.3z");

		/// <summary>Highlighter pen.</summary>
		public static readonly MokaIconDefinition Highlighter = new("highlighter",
			"M9 11l-6 6v3h9l3-3 M22 12l-4.6 4.6a2 2 0 0 1-2.8 0l-5.2-5.2a2 2 0 0 1 0-2.8L14 4");

		/// <summary>Vector pen tool.</summary>
		public static readonly MokaIconDefinition PenTool = new("pen-tool",
			"M12 19l7-7 3 3-7 7-3-3z M18 13l-8-8-4 4 8 8 M6 8a2 2 0 1 0 0-4 2 2 0 0 0 0 4z");

		/// <summary>Magic wand.</summary>
		public static readonly MokaIconDefinition Wand = new("wand",
			"M15 4V2 M15 10V8 M12.5 6.5h-2 M19.5 6.5h-2 M3 21l12-12 M14 10l4 4");

		/// <summary>Rubber stamp.</summary>
		public static readonly MokaIconDefinition Stamp = new("stamp",
			"M7 21h10 M5 17h14v-2H5z M8 12V7a4 4 0 1 1 8 0v5 M8 12h8");

		/// <summary>Ruler.</summary>
		public static readonly MokaIconDefinition Ruler = new("ruler",
			"M21 7L7 21l-4-4L17 3z M8 7l2 2 M11 10l2 2 M14 13l2 2 M17 16l2 2");

		/// <summary>Circle and square together.</summary>
		public static readonly MokaIconDefinition Shapes = new("shapes",
			"M8 13a5 5 0 1 0 0-10 5 5 0 0 0 0 10z M13 12h7a1 1 0 0 1 1 1v7a1 1 0 0 1 -1 1h-7a1 1 0 0 1 -1 -1v-7a1 1 0 0 1 1 -1z");

		/// <summary>Diamond, the mark of a keyframe.</summary>
		public static readonly MokaIconDefinition Diamond = new("diamond",
			"M12 3 L18 12 L12 21 L6 12 Z");

		/// <summary>Cube / box icon.</summary>
		public static readonly MokaIconDefinition Cube = new("cube",
			"M12 2l9 5v10l-9 5-9-5V7zM3 7l9 5 9-5M12 12v10");

		/// <summary>Stacked sheets / layers icon.</summary>
		public static readonly MokaIconDefinition Layers = new("layers",
			"M12 3l9 4.5-9 4.5-9-4.5z M3 12l9 4.5 9-4.5 M3 16.5l9 4.5 9-4.5");

		/// <summary>Stack of pictures / gallery icon.</summary>
		public static readonly MokaIconDefinition Images = new("images",
			"M22 11l-1.296-1.296a2.4 2.4 0 0 0-3.408 0L11 16 M4 8a2 2 0 0 0-2 2v10a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2 M13 8a1 1 0 1 0 0-2 1 1 0 0 0 0 2z M10 2h10a2 2 0 0 1 2 2v10a2 2 0 0 1 -2 2h-10a2 2 0 0 1 -2 -2v-10a2 2 0 0 1 2 -2z");

		/// <summary>Picture with a plus.</summary>
		public static readonly MokaIconDefinition ImagePlus = new("image-plus",
			"M16 5h6 M19 2v6 M21 11.5V19a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h7.5 M21 15l-3.086-3.086a2 2 0 0 0-2.828 0L6 21 M9 11a2 2 0 1 0 0-4 2 2 0 0 0 0 4z");

		/// <summary>Struck-through picture / missing image.</summary>
		public static readonly MokaIconDefinition ImageOff = new("image-off",
			"M2 2L22 22 M10.41 10.41a2 2 0 1 1-2.83-2.83 M13.5 13.5L6 21 M18 12L21 15 M3.59 3.59A1.99 1.99 0 0 0 3 5v14a2 2 0 0 0 2 2h14c.55 0 1.052-.22 1.41-.59 M21 15V5a2 2 0 0 0-2-2H9");

		/// <summary>Struck-through camera.</summary>
		public static readonly MokaIconDefinition CameraOff = new("camera-off",
			"M14.564 14.558a3 3 0 1 1-4.122-4.121 M2 2l20 20 M20 20H4a2 2 0 0 1-2-2V9a2 2 0 0 1 2-2h1.997a2 2 0 0 0 .819-.175 M9.695 4.024A2 2 0 0 1 10.004 4h3.993a2 2 0 0 1 1.76 1.05l.486.9A2 2 0 0 0 18.003 7H20a2 2 0 0 1 2 2v7.344");

		/// <summary>Film strip.</summary>
		public static readonly MokaIconDefinition Film = new("film",
			"M4 3h16a1 1 0 0 1 1 1v16a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1z M7 3v18 M17 3v18 M3 7.5h4 M3 12h18 M3 16.5h4 M17 7.5h4 M17 16.5h4");

		/// <summary>Film clapperboard.</summary>
		public static readonly MokaIconDefinition Clapperboard = new("clapperboard",
			"M12.296 3.464l3.02 3.956 M20.2 6 3 11l-.9-2.4c-.3-1.1.3-2.2 1.3-2.5l13.5-4c1.1-.3 2.2.3 2.5 1.3z M3 11h18v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z M6.18 5.276l3.1 3.899");

		/// <summary>Music notes.</summary>
		public static readonly MokaIconDefinition Music = new("music",
			"M9 18 V5 L20 3 V16 M9 18 a3 3 0 1 1 -6 0 a3 3 0 1 1 6 0 M20 16 a3 3 0 1 1 -6 0 a3 3 0 1 1 6 0");

		/// <summary>Vertical bars of a sound wave.</summary>
		public static readonly MokaIconDefinition AudioLines = new("audio-lines",
			"M3 10 V14 M7 8 V16 M11 5 V19 M15 8 V16 M19 10 V14");

		/// <summary>Headphones.</summary>
		public static readonly MokaIconDefinition Headphones = new("headphones",
			"M3 14h3a2 2 0 0 1 2 2v3a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-7a9 9 0 0 1 18 0v7a2 2 0 0 1-2 2h-1a2 2 0 0 1-2-2v-3a2 2 0 0 1 2-2h3");

		/// <summary>Cast to a screen.</summary>
		public static readonly MokaIconDefinition Cast = new("cast",
			"M2 8V6a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2h-6 M2 12a9 9 0 0 1 8 8 M2 16a5 5 0 0 1 4 4 M2 20L2.01 20");

		/// <summary>Screen with an outgoing arrow / share screen.</summary>
		public static readonly MokaIconDefinition ScreenShare = new("screen-share",
			"M13 3H4a2 2 0 0 0-2 2v10a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-3 M8 21h8 M12 17v4 M17 8l5-5 M17 3h5v5");

		/// <summary>Small frame inside a large one.</summary>
		public static readonly MokaIconDefinition PictureInPicture = new("picture-in-picture",
			"M3 5 H21 V19 H3 Z M13 11 H19 V17 H13 Z");

		/// <summary>Frame with opposing corner marks / aspect ratio.</summary>
		public static readonly MokaIconDefinition AspectRatio = new("aspect-ratio",
			"M3 5 H21 V19 H3 Z M7 9 H12 V14 M17 10 V15 H12");

		/// <summary>Frame with lines of text / captions.</summary>
		public static readonly MokaIconDefinition Captions = new("captions",
			"M3 5 H21 V19 H3 Z M7 11 H10 M7 15 H11 M14 11 H17 M14 15 H18");

		/// <summary>Ear.</summary>
		public static readonly MokaIconDefinition Ear = new("ear",
			"M6 8.5a6.5 6.5 0 1 1 13 0c0 6-6 6-6 10a3.5 3.5 0 1 1-7 0 M15 8.5a2.5 2.5 0 0 0-5 0v1a2 2 0 1 1 0 4");

		/// <summary>Vertical bars on an axis / bar chart.</summary>
		public static readonly MokaIconDefinition BarChart = new("bar-chart",
			"M4 20V12 M10 20V5 M16 20v-9 M22 20H2");

		/// <summary>Horizontal bars on an axis.</summary>
		public static readonly MokaIconDefinition BarChartHorizontal = new("bar-chart-horizontal",
			"M3 3v16a2 2 0 0 0 2 2h16 M7 16h8 M7 11h12 M7 6h3");

		/// <summary>Line on an axis / line chart.</summary>
		public static readonly MokaIconDefinition LineChart = new("line-chart",
			"M3 3v16a2 2 0 0 0 2 2h16 M19 9l-5 5-4-4-3 3");

		/// <summary>Filled region on an axis / area chart.</summary>
		public static readonly MokaIconDefinition AreaChart = new("area-chart",
			"M3 3v16a2 2 0 0 0 2 2h16 M7 11.207a.5.5 0 0 1 .146-.353l2-2a.5.5 0 0 1 .708 0l3.292 3.292a.5.5 0 0 0 .708 0l4.292-4.292a.5.5 0 0 1 .854.353V16a1 1 0 0 1-1 1H8a1 1 0 0 1-1-1z");

		/// <summary>Pie with a slice out / pie chart.</summary>
		public static readonly MokaIconDefinition PieChart = new("pie-chart",
			"M21 12c.552 0 1.005-.449.95-.998a10 10 0 0 0-8.953-8.951c-.55-.055-.998.398-.998.95v8a1 1 0 0 0 1 1z M21.21 15.89A10 10 0 1 1 8 2.83");

		/// <summary>Staggered bars on an axis / timeline.</summary>
		public static readonly MokaIconDefinition Timeline = new("timeline",
			"M3 3v18h18 M7 7h8 M9 12h9 M7 17h6");

		/// <summary>Three uneven columns / kanban board.</summary>
		public static readonly MokaIconDefinition Kanban = new("kanban",
			"M5 3v14 M12 3v8 M19 3v18");

		/// <summary>Table grid.</summary>
		public static readonly MokaIconDefinition Table = new("table",
			"M12 3v18 M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1 -2 2h-14a2 2 0 0 1 -2 -2v-14a2 2 0 0 1 2 -2z M3 9h18 M3 15h18");

		/// <summary>Box divided into three columns.</summary>
		public static readonly MokaIconDefinition ColumnsView = new("columns-view",
			"M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2z M9 3v18 M15 3v18");

		/// <summary>Percent sign.</summary>
		public static readonly MokaIconDefinition Percent = new("percent",
			"M19 5L5 19 M6.5 9a2.5 2.5 0 1 0 0-5 2.5 2.5 0 0 0 0 5z M17.5 20a2.5 2.5 0 1 0 0-5 2.5 2.5 0 0 0 0 5z");

		/// <summary>Sigma / sum icon.</summary>
		public static readonly MokaIconDefinition Sigma = new("sigma",
			"M18 7V5a1 1 0 0 0-1-1H6.5a.5.5 0 0 0-.4.8l4.5 6a2 2 0 0 1 0 2.4l-4.5 6a.5.5 0 0 0 .4.8H17a1 1 0 0 0 1-1v-2");

		/// <summary>Capital T / text icon.</summary>
		public static readonly MokaIconDefinition Type = new("type",
			"M4 7 V4 H20 V7 M9 20 H15 M12 4 V20");

		/// <summary>Bold text.</summary>
		public static readonly MokaIconDefinition Bold = new("bold",
			"M6 4h8a4 4 0 0 1 4 4 4 4 0 0 1-4 4H6z M6 12h9a4 4 0 0 1 4 4 4 4 0 0 1-4 4H6z");

		/// <summary>Italic text.</summary>
		public static readonly MokaIconDefinition Italic = new("italic",
			"M19 4L10 4 M14 20L5 20 M15 4L9 20");

		/// <summary>Underlined text.</summary>
		public static readonly MokaIconDefinition Underline = new("underline",
			"M6 4v6a6 6 0 0 0 12 0V4 M4 20L20 20");

		/// <summary>Struck-through text.</summary>
		public static readonly MokaIconDefinition Strikethrough = new("strikethrough",
			"M16 4H9a3 3 0 0 0-2.83 4 M14 12a4 4 0 0 1 0 8H6 M4 12L20 12");

		/// <summary>Capital H / heading icon.</summary>
		public static readonly MokaIconDefinition Heading = new("heading",
			"M6 4v16 M18 4v16 M6 12h12");

		/// <summary>First level heading.</summary>
		public static readonly MokaIconDefinition Heading1 = new("heading-1",
			"M4 12h8 M4 18V6 M12 18V6 M17 12l3-2v8");

		/// <summary>Second level heading.</summary>
		public static readonly MokaIconDefinition Heading2 = new("heading-2",
			"M4 12h8 M4 18V6 M12 18V6 M21 18h-4c0-4 4-3 4-6 0-1.5-2-2.5-4-1");

		/// <summary>Third level heading.</summary>
		public static readonly MokaIconDefinition Heading3 = new("heading-3",
			"M4 12h8 M4 18V6 M12 18V6 M17.5 10.5c1.7-1 3.5 0 3.5 1.5a2 2 0 0 1-2 2 M17 17.5c2 1.5 4 .3 4-1.5a2 2 0 0 0-2-2");

		/// <summary>Quotation marks.</summary>
		public static readonly MokaIconDefinition Quote = new("quote",
			"M3 21c3 0 7-1 7-8V5c0-1-1-2-2-2H4c-1 0-2 1-2 2v6c0 1 1 2 2 2h3 M15 21c3 0 7-1 7-8V5c0-1-1-2-2-2h-4c-1 0-2 1-2 2v6c0 1 1 2 2 2h3");

		/// <summary>Number sign.</summary>
		public static readonly MokaIconDefinition Hash = new("hash",
			"M9 4L7 20 M17 4l-2 16 M4 9h17 M3 15h17");

		/// <summary>Asterisk and dot / regular expression.</summary>
		public static readonly MokaIconDefinition Regex = new("regex",
			"M6 18h.01 M15 4v8 M11.5 6l7 4 M11.5 10l7-4");

		/// <summary>Text aligned left.</summary>
		public static readonly MokaIconDefinition AlignLeft = new("align-left",
			"M21 5H3 M15 12H3 M17 19H3");

		/// <summary>Text centred.</summary>
		public static readonly MokaIconDefinition AlignCenter = new("align-center",
			"M21 5H3 M17 12H7 M19 19H5");

		/// <summary>Text aligned right.</summary>
		public static readonly MokaIconDefinition AlignRight = new("align-right",
			"M21 5H3 M21 12H9 M21 19H7");

		/// <summary>Text justified.</summary>
		public static readonly MokaIconDefinition AlignJustify = new("align-justify",
			"M3 5h18 M3 12h18 M3 19h18");

		/// <summary>Bulleted list.</summary>
		public static readonly MokaIconDefinition List = new("list",
			"M8 6h13 M8 12h13 M8 18h13 M3 6h.01 M3 12h.01 M3 18h.01");

		/// <summary>Numbered list.</summary>
		public static readonly MokaIconDefinition ListOrdered = new("list-ordered",
			"M11 5h10 M11 12h10 M11 19h10 M4 4h1v5 M4 9h2 M6.5 20H3.4c0-1 2.6-1.925 2.6-3.5a1.5 1.5 0 0 0-2.6-1.02");

		/// <summary>Checklist.</summary>
		public static readonly MokaIconDefinition ListChecks = new("list-checks",
			"M13 5h8 M13 12h8 M13 19h8 M3 17l2 2 4-4 M3 7l2 2 4-4");

		/// <summary>Nested list.</summary>
		public static readonly MokaIconDefinition ListTree = new("list-tree",
			"M8 5h13 M13 12h8 M13 19h8 M3 10a2 2 0 0 0 2 2h3 M3 5v12a2 2 0 0 0 2 2h3");

		/// <summary>Increase indent.</summary>
		public static readonly MokaIconDefinition IndentIncrease = new("indent-increase",
			"M21 5H11 M21 12H11 M21 19H11 M3 8l4 4-4 4");

		/// <summary>Decrease indent.</summary>
		public static readonly MokaIconDefinition IndentDecrease = new("indent-decrease",
			"M21 5H11 M21 12H11 M21 19H11 M7 8l-4 4 4 4");

		/// <summary>Letters with a checkmark / spell check.</summary>
		public static readonly MokaIconDefinition SpellCheck = new("spell-check",
			"M20 15l-5.5 5.5L12 18 M4 16l6-12 5.115 10.23 M6 12h8");

		/// <summary>Sticky note.</summary>
		public static readonly MokaIconDefinition StickyNote = new("sticky-note",
			"M15.5 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V8.5z M15 3v6h6");

		/// <summary>Closed book.</summary>
		public static readonly MokaIconDefinition Book = new("book",
			"M4 19.5A2.5 2.5 0 0 1 6.5 17H20 M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z");

		/// <summary>Tag / label icon.</summary>
		public static readonly MokaIconDefinition Tag = new("tag",
			"M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z M7 7h.01");

		/// <summary>Calendar with its days marked.</summary>
		public static readonly MokaIconDefinition CalendarDays = new("calendar-days",
			"M8 2v3 M16 2v3 M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1 -2 2h-14a2 2 0 0 1 -2 -2v-14a2 2 0 0 1 2 -2z M3 9h18 M8 13h.01 M12 13h.01 M16 13h.01 M8 17h.01 M12 17h.01 M16 17h.01");

		/// <summary>Calendar with a checkmark.</summary>
		public static readonly MokaIconDefinition CalendarCheck = new("calendar-check",
			"M8 2v3 M16 2v3 M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1 -2 2h-14a2 2 0 0 1 -2 -2v-14a2 2 0 0 1 2 -2z M3 9h18 M9 15l2 2 4-4");

		/// <summary>Calendar with a plus / add event.</summary>
		public static readonly MokaIconDefinition CalendarPlus = new("calendar-plus",
			"M16 18h6 M16 2v3 M19 15v6 M21 11.5V5a2 2 0 00-2-2H5a2 2 0 00-2 2v14a2 2 0 002 2h8.3 M3 9h18 M8 2v3");

		/// <summary>Calendar with an X / cancelled event.</summary>
		public static readonly MokaIconDefinition CalendarX = new("calendar-x",
			"M8 2v3 M16 2v3 M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1 -2 2h-14a2 2 0 0 1 -2 -2v-14a2 2 0 0 1 2 -2z M3 9h18 M14 13l-4 4 M10 13l4 4");

		/// <summary>Calendar with a clock / scheduled.</summary>
		public static readonly MokaIconDefinition CalendarClock = new("calendar-clock",
			"M16 14v2.2l1.6 1 M16 2v3 M21 7.338V5a2 2 0 00-2-2H5a2 2 0 00-2 2v14a2 2 0 002 2h2.338 M3 9h5.859 M8 2v3 M16 22a6 6 0 1 0 0-12 6 6 0 0 0 0 12z");

		/// <summary>Calendar with a span marked / date range.</summary>
		public static readonly MokaIconDefinition CalendarRange = new("calendar-range",
			"M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1 -2 2h-14a2 2 0 0 1 -2 -2v-14a2 2 0 0 1 2 -2z M16 2v3 M3 9h18 M8 2v3 M17 13h-6 M13 17H7 M7 13h.01 M17 17h.01");

		/// <summary>Smiling face / emoji, and the Smileys group of an emoji panel.</summary>
		public static readonly MokaIconDefinition Smile = new("smile",
			"M2 12a10 10 0 1 0 20 0a10 10 0 1 0 -20 0 M8 14s1.5 2 4 2 4-2 4-2 M9 9h.01 M15 9h.01");

		/// <summary>Smiling face with a plus / add an emoji or a reaction.</summary>
		public static readonly MokaIconDefinition SmilePlus = new("smile-plus",
			"M22 11v1a10 10 0 1 1-9-10 M8 14s1.5 2 4 2 4-2 4-2 M9 9h.01 M15 9h.01 M16 5h6 M19 2v6");

		/// <summary>Open hand, and the People group of an emoji panel.</summary>
		public static readonly MokaIconDefinition Hand = new("hand",
			"M18 11V6a2 2 0 0 0-4 0 M14 10V4a2 2 0 0 0-4 0v2 M10 10.5V6a2 2 0 0 0-4 0v8 M18 8a2 2 0 1 1 4 0v6a8 8 0 0 1-8 8h-2c-2.8 0-4.5-.86-5.99-2.34l-3.6-3.6a2 2 0 0 1 2.83-2.82L7 15");

		/// <summary>Leaf / nature, and the Animals and nature group of an emoji panel.</summary>
		public static readonly MokaIconDefinition Leaf = new("leaf",
			"M11 20A7 7 0 0 1 9.8 6.1C15.5 5 17 4.48 19 2c1 2 2 4.18 2 8 0 5.5-4.78 10-10 10Z M2 21c0-3 1.85-5.36 5.08-6C9.5 14.52 12 13 13 12");

		/// <summary>Apple / food, and the Food and drink group of an emoji panel.</summary>
		public static readonly MokaIconDefinition Apple = new("apple",
			"M12 20.94c1.5 0 2.75 1.06 4 1.06 3 0 6-8 6-12.22A4.91 4.91 0 0 0 17 5c-2.22 0-4 1.44-5 2-1-.56-2.78-2-5-2a4.9 4.9 0 0 0-5 4.78C2 14 5 22 8 22c1.25 0 2.5-1.06 4-1.06Z M10 2c1 .5 2 2 2 5");

		/// <summary>Plane / travel, and the Travel and places group of an emoji panel.</summary>
		public static readonly MokaIconDefinition Plane = new("plane",
			"M17.8 19.2 16 11l3.5-3.5C21 6 21.5 4 21 3c-1-.5-3 0-4.5 1.5L13 8 4.8 6.2c-.5-.1-.9.1-1.1.5l-.3.5c-.2.5-.1 1 .3 1.3L9 12l-2 3H4l-1 1 3 2 2 3 1-1v-3l3-2 3.5 5.3c.3.4.8.5 1.3.3l.5-.2c.4-.3.6-.7.5-1.2z");

		/// <summary>Trophy / achievement, and the Activities group of an emoji panel.</summary>
		public static readonly MokaIconDefinition Trophy = new("trophy",
			"M6 9H4.5a2.5 2.5 0 0 1 0-5H6 M18 9h1.5a2.5 2.5 0 0 0 0-5H18 M4 22h16 M10 14.66V17c0 .55-.47.98-.97 1.21C7.85 18.75 7 20.24 7 22 M14 14.66V17c0 .55.47.98.97 1.21C16.15 18.75 17 20.24 17 22 M18 2H6v7a6 6 0 0 0 12 0V2Z");

		/// <summary>Light bulb / idea, and the Objects group of an emoji panel.</summary>
		public static readonly MokaIconDefinition Lightbulb = new("lightbulb",
			"M15 14c.2-1 .7-1.7 1.5-2.5 1-.9 1.5-2.2 1.5-3.5A6 6 0 0 0 6 8c0 1 .2 2.2 1.5 3.5.7.7 1.3 1.5 1.5 2.5 M9 18h6 M10 22h4");

		/// <summary>Triangle, a chart event marker.</summary>
		public static readonly MokaIconDefinition Triangle = new("triangle",
			"M13.73 4a2 2 0 0 0-3.46 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z");

		/// <summary>Circle, a chart event marker.</summary>
		public static readonly MokaIconDefinition Circle = new("circle",
			"M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z");
	}

	/// <summary>Toggle icons - eye, lock, star, heart, thumbs.</summary>
	public static class Toggle
	{
		/// <summary>Open eye / visible icon.</summary>
		public static readonly MokaIconDefinition Eye = new("eye",
			"M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z");

		/// <summary>Eye with slash / hidden icon.</summary>
		public static readonly MokaIconDefinition EyeOff = new("eye-off",
			"M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94 M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19 M14.12 14.12a3 3 0 1 1-4.24-4.24 M1 1l22 22");

		/// <summary>Closed lock icon.</summary>
		public static readonly MokaIconDefinition Lock = new("lock",
			"M19 11H5a2 2 0 0 0-2 2v7a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7a2 2 0 0 0-2-2z M7 11V7a5 5 0 0 1 10 0v4");

		/// <summary>Open lock icon.</summary>
		public static readonly MokaIconDefinition Unlock = new("unlock",
			"M19 11H5a2 2 0 0 0-2 2v7a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7a2 2 0 0 0-2-2z M7 11V7a5 5 0 0 1 9.9-1");

		/// <summary>Filled star icon.</summary>
		public static readonly MokaIconDefinition Star = new("star",
			"M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z",
			filled: true);

		/// <summary>Outlined star icon.</summary>
		public static readonly MokaIconDefinition StarOutline = new("star-outline",
			"M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z");

		/// <summary>Filled heart icon.</summary>
		public static readonly MokaIconDefinition Heart = new("heart",
			"M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z",
			filled: true);

		/// <summary>Outlined heart icon.</summary>
		public static readonly MokaIconDefinition HeartOutline = new("heart-outline",
			"M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z");

		/// <summary>Thumb up icon.</summary>
		public static readonly MokaIconDefinition ThumbUp = new("thumb-up",
			"M14 9V5a3 3 0 0 0-3-3l-4 9v11h11.28a2 2 0 0 0 2-1.7l1.38-9a2 2 0 0 0-2-2.3H14z M7 22H4a2 2 0 0 1-2-2v-7a2 2 0 0 1 2-2h3");

		/// <summary>Thumb down icon.</summary>
		public static readonly MokaIconDefinition ThumbDown = new("thumb-down",
			"M10 15V19a3 3 0 0 0 3 3l4-9V2H5.72a2 2 0 0 0-2 1.7l-1.38 9a2 2 0 0 0 2 2.3H10z M17 2h2.67A2.31 2.31 0 0 1 22 4v7a2.31 2.31 0 0 1-2.33 2H17");

		/// <summary>Push pin.</summary>
		public static readonly MokaIconDefinition Pin = new("pin",
			"M12 17v5 M9 10.76a2 2 0 0 1-1.11 1.79l-1.78.9A2 2 0 0 0 5 15.24V16a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1v-.76a2 2 0 0 0-1.11-1.79l-1.78-.9A2 2 0 0 1 15 10.76V7a1 1 0 0 1 1-1 2 2 0 0 0 0-4H8a2 2 0 0 0 0 4 1 1 0 0 1 1 1z");

		/// <summary>Struck-through push pin / unpin.</summary>
		public static readonly MokaIconDefinition PinOff = new("pin-off",
			"M12 17v5 M15 9.34V7a1 1 0 0 1 1-1 2 2 0 0 0 0-4H7.89 M2 2l20 20 M9 9v1.76a2 2 0 0 1-1.11 1.79l-1.78.9A2 2 0 0 0 5 15.24V16a1 1 0 0 0 1 1h11");

		/// <summary>Bookmark ribbon.</summary>
		public static readonly MokaIconDefinition Bookmark = new("bookmark",
			"M6 3 H18 V21 L12 16 L6 21 Z");

		/// <summary>Flag.</summary>
		public static readonly MokaIconDefinition Flag = new("flag",
			"M4 21 V4 H17 L15 8 L17 12 H4");

		/// <summary>Microphone.</summary>
		public static readonly MokaIconDefinition Mic = new("mic",
			"M12 2a3 3 0 0 0-3 3v7a3 3 0 0 0 6 0V5a3 3 0 0 0-3-3Z M19 10v2a7 7 0 0 1-14 0v-2 M12 19L12 22");

		/// <summary>Struck-through microphone / muted.</summary>
		public static readonly MokaIconDefinition MicOff = new("mic-off",
			"M12 19v3 M15 9.34V5a3 3 0 0 0-5.68-1.33 M16.95 16.95A7 7 0 0 1 5 12v-2 M18.89 13.23A7 7 0 0 0 19 12v-2 M2 2l20 20 M9 9v3a3 3 0 0 0 5.12 2.12");

		/// <summary>Video camera.</summary>
		public static readonly MokaIconDefinition Video = new("video",
			"M16 13l5.22 3.48a.5.5 0 0 0 .78-.42V7.94a.5.5 0 0 0-.78-.42L16 11 M4 6h10a2 2 0 0 1 2 2v8a2 2 0 0 1 -2 2h-10a2 2 0 0 1 -2 -2v-8a2 2 0 0 1 2 -2z");

		/// <summary>Struck-through video camera.</summary>
		public static readonly MokaIconDefinition VideoOff = new("video-off",
			"M10.66 6H14a2 2 0 0 1 2 2v2.5l5.248-3.062A.5.5 0 0 1 22 7.87v8.196 M16 16a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h2 M2 2l20 20");

		/// <summary>Speaker with sound waves.</summary>
		public static readonly MokaIconDefinition Volume = new("volume",
			"M11 5 L6 9 H2 V15 H6 L11 19 Z M15 9 a4 4 0 0 1 0 6 M18 6 a8 8 0 0 1 0 12");

		/// <summary>Speaker with an X / muted.</summary>
		public static readonly MokaIconDefinition VolumeOff = new("volume-off",
			"M11 5 L6 9 H2 V15 H6 L11 19 Z M23 9 L17 15 M17 9 L23 15");

		/// <summary>Rounded square / unchecked box.</summary>
		public static readonly MokaIconDefinition Square = new("square",
			"M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2z");

		/// <summary>Checkmark in a square / checked box.</summary>
		public static readonly MokaIconDefinition CheckSquare = new("check-square",
			"M9 11L12 14L22 4 M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11");
	}

	/// <summary>File icons - document, folder, code, terminal, etc.</summary>
	public static class File
	{
		/// <summary>Generic document / file icon.</summary>
		public static readonly MokaIconDefinition Document = new("document",
			"M13 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z M13 2v7h7");

		/// <summary>Closed folder icon.</summary>
		public static readonly MokaIconDefinition Folder = new("folder",
			"M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z");

		/// <summary>Open folder icon.</summary>
		public static readonly MokaIconDefinition FolderOpen = new("folder-open",
			"M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2v1 M2 10l2.64 9.23a1 1 0 0 0 .96.77h12.8a1 1 0 0 0 .96-.77L22 10H2z");

		/// <summary>Document with lines / text file icon.</summary>
		public static readonly MokaIconDefinition FileText = new("file-text",
			"M13 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z M13 2v7h7 M16 13H8 M16 17H8 M10 9H8");

		/// <summary>Code brackets icon.</summary>
		public static readonly MokaIconDefinition Code = new("code",
			"M16 18l6-6-6-6 M8 6l-6 6 6 6");

		/// <summary>Terminal / console icon.</summary>
		public static readonly MokaIconDefinition Terminal = new("terminal",
			"M4 17l6-6-6-6 M12 19h8");

		/// <summary>Database / cylinder icon.</summary>
		public static readonly MokaIconDefinition Database = new("database",
			"M12 2C6.48 2 2 3.79 2 6v12c0 2.21 4.48 4 10 4s10-1.79 10-4V6c0-2.21-4.48-4-10-4z M2 6c0 2.21 4.48 4 10 4s10-1.79 10-4 M2 12c0 2.21 4.48 4 10 4s10-1.79 10-4");

		/// <summary>Overlapping documents.</summary>
		public static readonly MokaIconDefinition Files = new("files",
			"M15 2h-4a2 2 0 0 0-2 2v11a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2V8 M16.706 2.706A2.4 2.4 0 0 0 15 2v5a1 1 0 0 0 1 1h5a2.4 2.4 0 0 0-.706-1.706z M5 7a2 2 0 0 0-2 2v11a2 2 0 0 0 2 2h8a2 2 0 0 0 1.732-1");

		/// <summary>Document with a plus / new file.</summary>
		public static readonly MokaIconDefinition FilePlus = new("file-plus",
			"M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z M14 2v6h6 M12 18v-6 M9 15h6");

		/// <summary>Document with a minus.</summary>
		public static readonly MokaIconDefinition FileMinus = new("file-minus",
			"M6 22a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h8a2.4 2.4 0 0 1 1.704.706l3.588 3.588A2.4 2.4 0 0 1 20 8v12a2 2 0 0 1-2 2z M14 2v5a1 1 0 0 0 1 1h5 M9 15h6");

		/// <summary>Document with a checkmark.</summary>
		public static readonly MokaIconDefinition FileCheck = new("file-check",
			"M6 22a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h8a2.4 2.4 0 0 1 1.704.706l3.588 3.588A2.4 2.4 0 0 1 20 8v12a2 2 0 0 1-2 2z M14 2v5a1 1 0 0 0 1 1h5 M9 15l2 2 4-4");

		/// <summary>Document with an X.</summary>
		public static readonly MokaIconDefinition FileX = new("file-x",
			"M6 22a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h8a2.4 2.4 0 0 1 1.704.706l3.588 3.588A2.4 2.4 0 0 1 20 8v12a2 2 0 0 1-2 2z M14 2v5a1 1 0 0 0 1 1h5 M14.5 12.5l-5 5 M9.5 12.5l5 5");

		/// <summary>Document with a pen / edit file.</summary>
		public static readonly MokaIconDefinition FilePen = new("file-pen",
			"M12.659 22H18a2 2 0 0 0 2-2V8a2.4 2.4 0 0 0-.706-1.706l-3.588-3.588A2.4 2.4 0 0 0 14 2H6a2 2 0 0 0-2 2v9.34 M14 2v5a1 1 0 0 0 1 1h5 M10.378 12.622a1 1 0 0 1 3 3.003L8.36 20.637a2 2 0 0 1-.854.506l-2.867.837a.5.5 0 0 1-.62-.62l.836-2.869a2 2 0 0 1 .506-.853z");

		/// <summary>Document with a magnifier.</summary>
		public static readonly MokaIconDefinition FileSearch = new("file-search",
			"M6 22a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h8a2.4 2.4 0 0 1 1.704.706l3.588 3.588A2.4 2.4 0 0 1 20 8v12a2 2 0 0 1-2 2z M14 2v5a1 1 0 0 0 1 1h5 M11.5 17a2.5 2.5 0 1 0 0-5 2.5 2.5 0 0 0 0 5z M13.3 16.3 15 18");

		/// <summary>Document with a lock.</summary>
		public static readonly MokaIconDefinition FileLock = new("file-lock",
			"M4 9.8V4a2 2 0 0 1 2-2h8a2.4 2.4 0 0 1 1.706.706l3.588 3.588A2.4 2.4 0 0 1 20 8v12a2 2 0 0 1-2 2h-3 M14 2v5a1 1 0 0 0 1 1h5 M9 17v-2a2 2 0 0 0-4 0v2 M4 17h6a1 1 0 0 1 1 1v3a1 1 0 0 1 -1 1h-6a1 1 0 0 1 -1 -1v-3a1 1 0 0 1 1 -1z");

		/// <summary>Document with a key.</summary>
		public static readonly MokaIconDefinition FileKey = new("file-key",
			"M14 2v5a1 1 0 0 0 1 1h5 M4 12v6 M4 14h2 M9.65 22H18a2 2 0 0 0 2-2V8a2.4 2.4 0 0 0-.706-1.706l-3.588-3.588A2.4 2.4 0 0 0 14 2H6a2 2 0 0 0-2 2v4 M4 22a2 2 0 1 0 0-4 2 2 0 0 0 0 4z");

		/// <summary>Document with a clock.</summary>
		public static readonly MokaIconDefinition FileClock = new("file-clock",
			"M16 22h2a2 2 0 0 0 2-2V8a2.4 2.4 0 0 0-.706-1.706l-3.588-3.588A2.4 2.4 0 0 0 14 2H6a2 2 0 0 0-2 2v2.85 M14 2v5a1 1 0 0 0 1 1h5 M8 14v2.2l1.6 1 M8 22a6 6 0 1 0 0-12 6 6 0 0 0 0 12z");

		/// <summary>Document with an exclamation mark.</summary>
		public static readonly MokaIconDefinition FileWarning = new("file-warning",
			"M6 22a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h8a2.4 2.4 0 0 1 1.704.706l3.588 3.588A2.4 2.4 0 0 1 20 8v12a2 2 0 0 1-2 2z M12 9v4 M12 17h.01");

		/// <summary>Document with an up arrow.</summary>
		public static readonly MokaIconDefinition FileUpload = new("file-upload",
			"M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z M14 2v6h6 M12 18v-6 M9 15l3-3 3 3");

		/// <summary>Document with a down arrow.</summary>
		public static readonly MokaIconDefinition FileDownload = new("file-download",
			"M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z M14 2v6h6 M12 12v6 M9 15l3 3 3-3");

		/// <summary>Document with a bent arrow / symbolic link.</summary>
		public static readonly MokaIconDefinition FileSymlink = new("file-symlink",
			"M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z M14 2v6h6 M8 16v-1a3 3 0 0 1 3-3h5 M13 9l3 3-3 3");

		/// <summary>Image file.</summary>
		public static readonly MokaIconDefinition FileImage = new("file-image",
			"M6 22a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h8a2.4 2.4 0 0 1 1.704.706l3.588 3.588A2.4 2.4 0 0 1 20 8v12a2 2 0 0 1-2 2z M14 2v5a1 1 0 0 0 1 1h5 M10 14a2 2 0 1 0 0-4 2 2 0 0 0 0 4z M20 17l-1.296-1.296a2.41 2.41 0 0 0-3.408 0L9 22");

		/// <summary>Video file.</summary>
		public static readonly MokaIconDefinition FileVideo = new("file-video",
			"M4 12V4a2 2 0 0 1 2-2h8a2.4 2.4 0 0 1 1.706.706l3.588 3.588A2.4 2.4 0 0 1 20 8v12a2 2 0 0 1-2 2 M14 2v5a1 1 0 0 0 1 1h5 M10 17.843l3.033-1.755a.64.64 0 0 1 .967.56v4.704a.65.65 0 0 1-.967.56L10 20.157 M4 16h5a1 1 0 0 1 1 1v4a1 1 0 0 1 -1 1h-5a1 1 0 0 1 -1 -1v-4a1 1 0 0 1 1 -1z");

		/// <summary>Audio file.</summary>
		public static readonly MokaIconDefinition FileAudio = new("file-audio",
			"M11.65 22H18a2 2 0 0 0 2-2V8a2.4 2.4 0 0 0-.706-1.706l-3.588-3.588A2.4 2.4 0 0 0 14 2H6a2 2 0 0 0-2 2v10.35 M14 2v5a1 1 0 0 0 1 1h5 M8 20v-7l3 1.474 M6 22a2 2 0 1 0 0-4 2 2 0 0 0 0 4z");

		/// <summary>Source code file.</summary>
		public static readonly MokaIconDefinition FileCode = new("file-code",
			"M6 22a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h8a2.4 2.4 0 0 1 1.704.706l3.588 3.588A2.4 2.4 0 0 1 20 8v12a2 2 0 0 1-2 2z M14 2v5a1 1 0 0 0 1 1h5 M10 12.5 8 15l2 2.5 M14 12.5l2 2.5-2 2.5");

		/// <summary>JSON file.</summary>
		public static readonly MokaIconDefinition FileJson = new("file-json",
			"M6 22a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h8a2.4 2.4 0 0 1 1.704.706l3.588 3.588A2.4 2.4 0 0 1 20 8v12a2 2 0 0 1-2 2z M14 2v5a1 1 0 0 0 1 1h5 M10 12a1 1 0 0 0-1 1v1a1 1 0 0 1-1 1 1 1 0 0 1 1 1v1a1 1 0 0 0 1 1 M14 18a1 1 0 0 0 1-1v-1a1 1 0 0 1 1-1 1 1 0 0 1-1-1v-1a1 1 0 0 0-1-1");

		/// <summary>Script file.</summary>
		public static readonly MokaIconDefinition FileTerminal = new("file-terminal",
			"M6 22a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h8a2.4 2.4 0 0 1 1.704.706l3.588 3.588A2.4 2.4 0 0 1 20 8v12a2 2 0 0 1-2 2z M14 2v5a1 1 0 0 0 1 1h5 M8 16l2-2-2-2 M12 18h4");

		/// <summary>Font file.</summary>
		public static readonly MokaIconDefinition FileType = new("file-type",
			"M6 22a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h8a2.4 2.4 0 0 1 1.704.706l3.588 3.588A2.4 2.4 0 0 1 20 8v12a2 2 0 0 1-2 2z M14 2v5a1 1 0 0 0 1 1h5 M11 18h2 M12 12v6 M9 13v-.5a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 .5.5v.5");

		/// <summary>Spreadsheet file.</summary>
		public static readonly MokaIconDefinition FileSpreadsheet = new("file-spreadsheet",
			"M6 22a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h8a2.4 2.4 0 0 1 1.704.706l3.588 3.588A2.4 2.4 0 0 1 20 8v12a2 2 0 0 1-2 2z M14 2v5a1 1 0 0 0 1 1h5 M8 13h2 M14 13h2 M8 17h2 M14 17h2");

		/// <summary>Compressed file.</summary>
		public static readonly MokaIconDefinition FileArchive = new("file-archive",
			"M13.659 22H18a2 2 0 0 0 2-2V8a2.4 2.4 0 0 0-.706-1.706l-3.588-3.588A2.4 2.4 0 0 0 14 2H6a2 2 0 0 0-2 2v11.5 M14 2v5a1 1 0 0 0 1 1h5 M8 12v-1 M8 18v-2 M8 7V6 M8 22a2 2 0 1 0 0-4 2 2 0 0 0 0 4z");

		/// <summary>Overlapping folders.</summary>
		public static readonly MokaIconDefinition Folders = new("folders",
			"M20 5a2 2 0 0 1 2 2v7a2 2 0 0 1-2 2H9a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h2.5a1.5 1.5 0 0 1 1.2.6l.6.8a1.5 1.5 0 0 0 1.2.6z M3 8.268a2 2 0 0 0-1 1.738V19a2 2 0 0 0 2 2h11a2 2 0 0 0 1.732-1");

		/// <summary>Folder with a plus / new folder.</summary>
		public static readonly MokaIconDefinition FolderPlus = new("folder-plus",
			"M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z M12 11v6 M9 14h6");

		/// <summary>Folder with a minus.</summary>
		public static readonly MokaIconDefinition FolderMinus = new("folder-minus",
			"M9 13h6 M20 20a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.9a2 2 0 0 1-1.69-.9L9.6 3.9A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13a2 2 0 0 0 2 2Z");

		/// <summary>Folder with a checkmark.</summary>
		public static readonly MokaIconDefinition FolderCheck = new("folder-check",
			"M20 20a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.9a2 2 0 0 1-1.69-.9L9.6 3.9A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13a2 2 0 0 0 2 2Z M9 13l2 2 4-4");

		/// <summary>Folder with an X.</summary>
		public static readonly MokaIconDefinition FolderX = new("folder-x",
			"M20 20a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.9a2 2 0 0 1-1.69-.9L9.6 3.9A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13a2 2 0 0 0 2 2Z M9.5 10.5l5 5 M14.5 10.5l-5 5");

		/// <summary>Folder with an arrow going in / move to folder.</summary>
		public static readonly MokaIconDefinition FolderInput = new("folder-input",
			"M2 9V5a2 2 0 0 1 2-2h4l2 3h8a2 2 0 0 1 2 2v1 M2 13h12 M10 9l4 4-4 4 M22 13v6a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2v-2");

		/// <summary>Folder with an up arrow.</summary>
		public static readonly MokaIconDefinition FolderUp = new("folder-up",
			"M20 20a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.9a2 2 0 0 1-1.69-.9L9.6 3.9A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13a2 2 0 0 0 2 2Z M12 10v6 M9 13l3-3 3 3");

		/// <summary>Folder with a down arrow.</summary>
		public static readonly MokaIconDefinition FolderDown = new("folder-down",
			"M20 20a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.9a2 2 0 0 1-1.69-.9L9.6 3.9A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13a2 2 0 0 0 2 2Z M12 10v6 M15 13l-3 3-3-3");

		/// <summary>Folder with a magnifier.</summary>
		public static readonly MokaIconDefinition FolderSearch = new("folder-search",
			"M10.7 20H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h3.9a2 2 0 0 1 1.69.9l.81 1.2a2 2 0 0 0 1.67.9H20a2 2 0 0 1 2 2v4.1 M21 21l-1.9-1.9 M17 20a3 3 0 1 0 0-6 3 3 0 0 0 0 6z");

		/// <summary>Folder with a lock.</summary>
		public static readonly MokaIconDefinition FolderLock = new("folder-lock",
			"M15 17h6a1 1 0 0 1 1 1v3a1 1 0 0 1 -1 1h-6a1 1 0 0 1 -1 -1v-3a1 1 0 0 1 1 -1z M10 20H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h3.9a2 2 0 0 1 1.69.9l.81 1.2a2 2 0 0 0 1.67.9H20a2 2 0 0 1 2 2v2.5 M20 17v-2a2 2 0 1 0-4 0v2");

		/// <summary>Folder with a gear.</summary>
		public static readonly MokaIconDefinition FolderCog = new("folder-cog",
			"M10.3 20H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h3.98a2 2 0 0 1 1.69.9l.66 1.2A2 2 0 0 0 12 6h8a2 2 0 0 1 2 2v3.3 M14.305 19.53l.923-.382 M15.228 16.852l-.923-.383 M16.852 15.228l-.383-.923 M16.852 20.772l-.383.924 M19.148 15.228l.383-.923 M19.53 21.696l-.382-.924 M20.772 16.852l.924-.383 M20.772 19.148l.924.383 M18 21a3 3 0 1 0 0-6 3 3 0 0 0 0 6z");

		/// <summary>Folders in a tree.</summary>
		public static readonly MokaIconDefinition FolderTree = new("folder-tree",
			"M20 10a1 1 0 0 0 1-1V6a1 1 0 0 0-1-1h-2.5a1 1 0 0 1-.8-.4l-.9-1.2A1 1 0 0 0 15 3h-2a1 1 0 0 0-1 1v5a1 1 0 0 0 1 1Z M20 21a1 1 0 0 0 1-1v-3a1 1 0 0 0-1-1h-2.9a1 1 0 0 1-.88-.55l-.42-.85a1 1 0 0 0-.92-.6H13a1 1 0 0 0-1 1v5a1 1 0 0 0 1 1Z M3 5a2 2 0 0 0 2 2h3 M3 3v13a2 2 0 0 0 2 2h3");

		/// <summary>Box with a lid / archive icon.</summary>
		public static readonly MokaIconDefinition Archive = new("archive",
			"M21 8v13H3V8 M1 3h22v5H1z M10 12h4");

		/// <summary>Archive box with an up arrow / take out of the archive.</summary>
		public static readonly MokaIconDefinition ArchiveRestore = new("archive-restore",
			"M3 3h18a1 1 0 0 1 1 1v3a1 1 0 0 1 -1 1h-18a1 1 0 0 1 -1 -1v-3a1 1 0 0 1 1 -1z M4 8v11a2 2 0 0 0 2 2h2 M20 8v11a2 2 0 0 1-2 2h-2 M9 15l3-3 3 3 M12 12v9");

		/// <summary>Archive box with an X.</summary>
		public static readonly MokaIconDefinition ArchiveX = new("archive-x",
			"M3 3h18a1 1 0 0 1 1 1v3a1 1 0 0 1 -1 1h-18a1 1 0 0 1 -1 -1v-3a1 1 0 0 1 1 -1z M4 8v11a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8 M9.5 17l5-5 M9.5 12l5 5");

		/// <summary>Wrapped parcel / package icon.</summary>
		public static readonly MokaIconDefinition Package = new("package",
			"M16.5 9.4l-9-5.19 M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z M3.27 6.96L12 12.01l8.73-5.05 M12 22.08V12");

		/// <summary>Open parcel.</summary>
		public static readonly MokaIconDefinition PackageOpen = new("package-open",
			"M12 22v-9 M15.17 2.21a1.67 1.67 0 0 1 1.63 0L21 4.57a1.93 1.93 0 0 1 0 3.36L8.82 14.79a1.655 1.655 0 0 1-1.64 0L3 12.43a1.93 1.93 0 0 1 0-3.36z M20 13v3.87a2.06 2.06 0 0 1-1.11 1.83l-6 3.08a1.93 1.93 0 0 1-1.78 0l-6-3.08A2.06 2.06 0 0 1 4 16.87V13 M21 12.43a1.93 1.93 0 0 0 0-3.36L8.83 2.2a1.64 1.64 0 0 0-1.63 0L3 4.57a1.93 1.93 0 0 0 0 3.36l12.18 6.86a1.636 1.636 0 0 0 1.63 0z");

		/// <summary>Parcel with a checkmark.</summary>
		public static readonly MokaIconDefinition PackageCheck = new("package-check",
			"M12 22V12 M16 17l2 2 4-4 M21 11.127V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.729l7 4a2 2 0 0 0 2 .001l1.32-.753 M3.29 7 12 12l8.71-5 M7.5 4.27l8.997 5.148");

		/// <summary>Scroll with lines of text / logs icon.</summary>
		public static readonly MokaIconDefinition Logs = new("logs",
			"M15 12h-5 M15 8h-5 M19 17V5a2 2 0 0 0-2-2H4 M8 21h12a2 2 0 0 0 2-2v-1a1 1 0 0 0-1-1H11a1 1 0 0 0-1 1v1a2 2 0 1 1-4 0V5a2 2 0 1 0-4 0v2a1 1 0 0 0 1 1h3");

		/// <summary>Curly braces / structured data.</summary>
		public static readonly MokaIconDefinition Braces = new("braces",
			"M9 4H8a2 2 0 0 0-2 2v4a2 2 0 0 1-2 2 2 2 0 0 1 2 2v4a2 2 0 0 0 2 2h1 M15 4h1a2 2 0 0 1 2 2v4a2 2 0 0 0 2 2 2 2 0 0 0-2 2v4a2 2 0 0 1-2 2h-1");

		/// <summary>Hard drive.</summary>
		public static readonly MokaIconDefinition HardDrive = new("hard-drive",
			"M22 12H2 M5.45 5.11L2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z M6 16h.01 M10 16h.01");

		/// <summary>Hard drive with an up arrow.</summary>
		public static readonly MokaIconDefinition HardDriveUpload = new("hard-drive-upload",
			"M16 6l-4-4-4 4 M12 2v8 M4 14h16a2 2 0 0 1 2 2v4a2 2 0 0 1 -2 2h-16a2 2 0 0 1 -2 -2v-4a2 2 0 0 1 2 -2z M6 18h.01 M10 18h.01");

		/// <summary>Hard drive with a down arrow.</summary>
		public static readonly MokaIconDefinition HardDriveDownload = new("hard-drive-download",
			"M12 2v8 M16 6l-4 4-4-4 M4 14h16a2 2 0 0 1 2 2v4a2 2 0 0 1 -2 2h-16a2 2 0 0 1 -2 -2v-4a2 2 0 0 1 2 -2z M6 18h.01 M10 18h.01");

		/// <summary>Database with a circling arrow / backup.</summary>
		public static readonly MokaIconDefinition DatabaseBackup = new("database-backup",
			"M12 8a9 3 0 1 0 0-6 9 3 0 0 0 0 6z M3 12a9 3 0 0 0 5 2.69 M21 9.3V5 M3 5v14a9 3 0 0 0 6.47 2.88 M12 12v4h4 M13 20a5 5 0 0 0 9-3 4.5 4.5 0 0 0-4.5-4.5c-1.33 0-2.54.54-3.41 1.41L12 16");
	}
}
