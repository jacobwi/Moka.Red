using System.Collections.Frozen;
using System.Text;

namespace Moka.Red.Core.Utilities;

/// <summary>
///     Folds text the way people type it into a search box: lower case, Latin letters without their accents (Å reads
///     as a, ß as ss) and no apostrophes, so "cote divoire" finds "Côte d’Ivoire". A table of its own rather than an
///     accent-blind <see cref="System.Globalization.CompareInfo" />, which needs ICU: a WebAssembly app with invariant
///     globalization has none, and there culture compares act like ordinal ones.
/// </summary>
public static class MokaTextFold
{
	// Each pair is a letter and the base letter it folds to: every letter in Latin-1, Latin Extended-A and B and Latin
	// Extended Additional whose canonical decomposition starts with an ASCII letter, generated once from Unicode's data.
	private const string Pairs =
		"ÀaÁaÂaÃaÄaÅaÇcÈeÉeÊeËeÌiÍiÎiÏiÑnÒoÓoÔoÕoÖoÙuÚuÛuÜuÝyàaáaâaãaäaåaçcèeéeêeëeìiíiîiïiñnòoóoôoõoöoùu" +
		"úuûuüuýyÿyĀaāaĂaăaĄaąaĆcćcĈcĉcĊcċcČcčcĎdďdĒeēeĔeĕeĖeėeĘeęeĚeěeĜgĝgĞgğgĠgġgĢgģgĤhĥhĨiĩiĪiīiĬiĭiĮi" +
		"įiİiĴjĵjĶkķkĹlĺlĻlļlĽlľlŃnńnŅnņnŇnňnŌoōoŎoŏoŐoőoŔrŕrŖrŗrŘrřrŚsśsŜsŝsŞsşsŠsšsŢtţtŤtťtŨuũuŪuūuŬuŭu" +
		"ŮuůuŰuűuŲuųuŴwŵwŶyŷyŸyŹzźzŻzżzŽzžzƠoơoƯuưuǍaǎaǏiǐiǑoǒoǓuǔuǕuǖuǗuǘuǙuǚuǛuǜuǞaǟaǠaǡaǦgǧgǨkǩkǪoǫoǬo" +
		"ǭoǰjǴgǵgǸnǹnǺaǻaȀaȁaȂaȃaȄeȅeȆeȇeȈiȉiȊiȋiȌoȍoȎoȏoȐrȑrȒrȓrȔuȕuȖuȗuȘsșsȚtțtȞhȟhȦaȧaȨeȩeȪoȫoȬoȭoȮoȯo" +
		"ȰoȱoȲyȳyḀaḁaḂbḃbḄbḅbḆbḇbḈcḉcḊdḋdḌdḍdḎdḏdḐdḑdḒdḓdḔeḕeḖeḗeḘeḙeḚeḛeḜeḝeḞfḟfḠgḡgḢhḣhḤhḥhḦhḧhḨhḩhḪhḫh" +
		"ḬiḭiḮiḯiḰkḱkḲkḳkḴkḵkḶlḷlḸlḹlḺlḻlḼlḽlḾmḿmṀmṁmṂmṃmṄnṅnṆnṇnṈnṉnṊnṋnṌoṍoṎoṏoṐoṑoṒoṓoṔpṕpṖpṗpṘrṙrṚrṛr" +
		"ṜrṝrṞrṟrṠsṡsṢsṣsṤsṥsṦsṧsṨsṩsṪtṫtṬtṭtṮtṯtṰtṱtṲuṳuṴuṵuṶuṷuṸuṹuṺuṻuṼvṽvṾvṿvẀwẁwẂwẃwẄwẅwẆwẇwẈwẉwẊxẋx" +
		"ẌxẍxẎyẏyẐzẑzẒzẓzẔzẕzẖhẗtẘwẙyẠaạaẢaảaẤaấaẦaầaẨaẩaẪaẫaẬaậaẮaắaẰaằaẲaẳaẴaẵaẶaặaẸeẹeẺeẻeẼeẽeẾeếeỀeềe" +
		"ỂeểeỄeễeỆeệeỈiỉiỊiịiỌoọoỎoỏoỐoốoỒoồoỔoổoỖoỗoỘoộoỚoớoỜoờoỞoởoỠoỡoỢoợoỤuụuỦuủuỨuứuỪuừuỬuửuỮuữuỰuựu" +
		"ỲyỳyỴyỵyỶyỷyỸyỹy";

	private static readonly FrozenDictionary<char, char> Letters = BuildLetters();

	/// <summary>Folds text for matching: lower case, no accents, no apostrophes. Null folds to an empty string.</summary>
	/// <param name="text">The text to fold.</param>
	/// <returns>The folded text.</returns>
	public static string Fold(string? text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}

		StringBuilder folded = new(text.Length);
		foreach (char c in text)
		{
			if (c < 128)
			{
				if (c != '\'' && c != '`')
				{
					folded.Append(c is >= 'A' and <= 'Z' ? (char)(c + 32) : c);
				}
			}
			else if (Letters.TryGetValue(c, out char letter))
			{
				folded.Append(letter);
			}
			else
			{
				AppendOther(folded, c);
			}
		}

		return folded.ToString();
	}

	/// <summary>Whether <paramref name="text" /> holds <paramref name="query" />, both folded. An empty query is in every text.</summary>
	/// <param name="text">The text to look in.</param>
	/// <param name="query">What was typed.</param>
	/// <returns>True when the folded text contains the folded query.</returns>
	public static bool Contains(string? text, string? query) =>
		Fold(text).Contains(Fold(query), StringComparison.Ordinal);

	/// <summary>Whether <paramref name="text" /> starts with <paramref name="query" />, both folded.</summary>
	/// <param name="text">The text to look at.</param>
	/// <param name="query">What was typed.</param>
	/// <returns>True when the folded text starts with the folded query.</returns>
	public static bool StartsWith(string? text, string? query) =>
		Fold(text).StartsWith(Fold(query), StringComparison.Ordinal);

	private static void AppendOther(StringBuilder folded, char c)
	{
		switch (c)
		{
			case >= '\u0300' and <= '\u036F':
			case '\u2018' or '\u2019' or '\u02BB' or '\u02BC' or '\u00B4':
				return;
			case '\u00A0':
				folded.Append(' ');
				return;
			case '\u00DF':
				folded.Append("ss");
				return;
			case '\u00C6' or '\u00E6':
				folded.Append("ae");
				return;
			case '\u0152' or '\u0153':
				folded.Append("oe");
				return;
			case '\u00DE' or '\u00FE':
				folded.Append("th");
				return;
			case '\u00D8' or '\u00F8':
				folded.Append('o');
				return;
			case '\u0110' or '\u0111' or '\u00D0' or '\u00F0':
				folded.Append('d');
				return;
			case '\u0141' or '\u0142':
				folded.Append('l');
				return;
			case '\u0131':
				folded.Append('i');
				return;
			case '\u0126' or '\u0127':
				folded.Append('h');
				return;
			case '\u0166' or '\u0167':
				folded.Append('t');
				return;
			case '\u014A' or '\u014B':
				folded.Append('n');
				return;
			default:
				folded.Append(char.ToLowerInvariant(c));
				return;
		}
	}

	private static FrozenDictionary<char, char> BuildLetters()
	{
		Dictionary<char, char> letters = new(Pairs.Length / 2);
		for (int i = 0; i + 1 < Pairs.Length; i += 2)
		{
			letters[Pairs[i]] = Pairs[i + 1];
		}

		return letters.ToFrozenDictionary();
	}
}