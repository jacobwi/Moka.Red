using Moka.Red.Core.Icons;

namespace Moka.Red.Icons;

/// <summary>
///     Built-in SVG icon definitions for Moka.Red. Icons use a 24x24 viewBox with
///     stroke-based paths (Lucide/Feather style).
/// </summary>
public static class MokaIcons
{
	/// <summary>Action icons — Save, Delete, Edit, Add, etc.</summary>
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
	}

	/// <summary>Navigation icons — arrows, chevrons, menu, etc.</summary>
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

		/// <summary>Three horizontal dots.</summary>
		public static readonly MokaIconDefinition MoreHorizontal = new("more-horizontal",
			"M12 13a1 1 0 1 0 0-2 1 1 0 0 0 0 2z M19 13a1 1 0 1 0 0-2 1 1 0 0 0 0 2z M5 13a1 1 0 1 0 0-2 1 1 0 0 0 0 2z");

		/// <summary>Three vertical dots.</summary>
		public static readonly MokaIconDefinition MoreVertical = new("more-vertical",
			"M12 13a1 1 0 1 0 0-2 1 1 0 0 0 0 2z M12 6a1 1 0 1 0 0-2 1 1 0 0 0 0 2z M12 20a1 1 0 1 0 0-2 1 1 0 0 0 0 2z");
	}

	/// <summary>Status icons — check, warning, error, info, etc.</summary>
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

		/// <summary>Bell / notification icon.</summary>
		public static readonly MokaIconDefinition Bell = new("bell",
			"M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9 M13.73 21a2 2 0 0 1-3.46 0");
	}

	/// <summary>Content icons — copy, paste, link, filter, etc.</summary>
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
	}

	/// <summary>Toggle icons — eye, lock, star, heart, thumbs.</summary>
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
			"M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z");

		/// <summary>Outlined star icon.</summary>
		public static readonly MokaIconDefinition StarOutline = new("star-outline",
			"M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z");

		/// <summary>Filled heart icon.</summary>
		public static readonly MokaIconDefinition Heart = new("heart",
			"M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z");

		/// <summary>Outlined heart icon.</summary>
		public static readonly MokaIconDefinition HeartOutline = new("heart-outline",
			"M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z");

		/// <summary>Thumb up icon.</summary>
		public static readonly MokaIconDefinition ThumbUp = new("thumb-up",
			"M14 9V5a3 3 0 0 0-3-3l-4 9v11h11.28a2 2 0 0 0 2-1.7l1.38-9a2 2 0 0 0-2-2.3H14z M7 22H4a2 2 0 0 1-2-2v-7a2 2 0 0 1 2-2h3");

		/// <summary>Thumb down icon.</summary>
		public static readonly MokaIconDefinition ThumbDown = new("thumb-down",
			"M10 15V19a3 3 0 0 0 3 3l4-9V2H5.72a2 2 0 0 0-2 1.7l-1.38 9a2 2 0 0 0 2 2.3H10z M17 2h2.67A2.31 2.31 0 0 1 22 4v7a2.31 2.31 0 0 1-2.33 2H17");
	}

	/// <summary>File icons — document, folder, code, terminal, etc.</summary>
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
	}
}
