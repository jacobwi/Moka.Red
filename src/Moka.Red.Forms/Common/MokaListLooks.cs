using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.Common;

/// <summary>What MokaSelect, MokaAutoComplete and MokaTreeSelect share about <see cref="MokaListLook" />.</summary>
internal static class MokaListLooks
{
	/// <summary>Watches which side of the field an attached list opened on.</summary>
	public const string SideModule = "./_content/Moka.Red.Forms/moka-list-side.js";

	/// <summary>
	///     The look a list draws: the component's own, else the theme's. The original design keeps its list whatever
	///     either says, since its rules and markup were never drawn for these looks.
	/// </summary>
	public static MokaListLook Resolve(MokaListLook? look, MokaTheme? theme) =>
		theme?.Look == MokaLook.Gate ? look ?? theme.Lists : MokaListLook.Standard;

	/// <summary>The root class for a look, such as <c>moka-select--list-check</c>, or null for Standard.</summary>
	public static string? RootClass(string root, MokaListLook look) =>
		look == MokaListLook.Standard ? null : $"{root}--list-{MokaEnumHelpers.ToCssClass(look)}";

	/// <summary>Whether rows start with a mark: a radio circle or check box, or the Terminal look's brackets.</summary>
	public static bool DrawsMark(MokaListLook look) => look is MokaListLook.Radio or MokaListLook.Terminal;

	/// <summary>Whether a chosen row ends with a check. The looks that start rows with a mark show the choice there.</summary>
	public static bool DrawsEndCheck(MokaListLook look) => !DrawsMark(look);
}
