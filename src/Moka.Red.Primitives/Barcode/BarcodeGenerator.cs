namespace Moka.Red.Primitives.Barcode;

/// <summary>
///     Internal barcode encoder supporting Code 128 Subset B.
///     Produces a boolean array of module states (true = bar, false = space).
/// </summary>
internal static class BarcodeGenerator
{
	/// <summary>
	///     Code 128B pattern table. Each entry is 6 digits representing
	///     alternating bar and space widths (bar, space, bar, space, bar, space).
	///     Each symbol totals 11 modules.
	/// </summary>
	private static readonly string[] Code128Patterns =
	[
		"212222", // 0:  Space
		"222122", // 1:  !
		"222221", // 2:  "
		"121223", // 3:  #
		"121322", // 4:  $
		"131222", // 5:  %
		"122213", // 6:  &
		"122312", // 7:  '
		"132212", // 8:  (
		"221213", // 9:  )
		"221312", // 10: *
		"231212", // 11: +
		"112232", // 12: ,
		"122132", // 13: -
		"122231", // 14: .
		"113222", // 15: /
		"123122", // 16: 0
		"123221", // 17: 1
		"223211", // 18: 2
		"221132", // 19: 3
		"221231", // 20: 4
		"213212", // 21: 5
		"223112", // 22: 6
		"312131", // 23: 7
		"311222", // 24: 8
		"321122", // 25: 9
		"321221", // 26: :
		"312212", // 27: ;
		"322112", // 28: <
		"322211", // 29: =
		"212123", // 30: >
		"212321", // 31: ?
		"232121", // 32: @
		"111323", // 33: A
		"131123", // 34: B
		"131321", // 35: C
		"112313", // 36: D
		"132113", // 37: E
		"132311", // 38: F
		"211313", // 39: G
		"231113", // 40: H
		"231311", // 41: I
		"112133", // 42: J
		"112331", // 43: K
		"132131", // 44: L
		"113123", // 45: M
		"113321", // 46: N
		"133121", // 47: O
		"313121", // 48: P
		"211331", // 49: Q
		"231131", // 50: R
		"213113", // 51: S
		"213311", // 52: T
		"213131", // 53: U
		"311123", // 54: V
		"311321", // 55: W
		"331121", // 56: X
		"312113", // 57: Y
		"312311", // 58: Z
		"332111", // 59: [
		"314111", // 60: backslash
		"221411", // 61: ]
		"431111", // 62: ^
		"111224", // 63: _
		"111422", // 64: `
		"121124", // 65: a
		"121421", // 66: b
		"141122", // 67: c
		"141221", // 68: d
		"112214", // 69: e
		"112412", // 70: f
		"122114", // 71: g
		"122411", // 72: h
		"142112", // 73: i
		"142211", // 74: j
		"241211", // 75: k
		"221114", // 76: l
		"413111", // 77: m
		"241112", // 78: n
		"134111", // 79: o
		"111242", // 80: p
		"121142", // 81: q
		"121241", // 82: r
		"114212", // 83: s
		"124112", // 84: t
		"124211", // 85: u
		"411212", // 86: v
		"421112", // 87: w
		"421211", // 88: x
		"212141", // 89: y
		"214121", // 90: z
		"412121", // 91: {
		"111143", // 92: |
		"111341", // 93: }
		"131141", // 94: ~
		"114113", // 95: DEL
		"114311", // 96: FNC3
		"411113", // 97: FNC2
		"411311", // 98: SHIFT
		"113141", // 99: CODE C
		"114131", // 100: FNC4 / CODE B
		"311141", // 101: FNC4 / CODE A
		"411131", // 102: FNC1
		"211412", // 103: Start A
		"211214", // 104: Start B
		"211232", // 105: Start C
		"233111" // 106: Stop (+ final bar)
	];

	/// <summary>
	///     Encodes text using Code 128 Subset B and returns a boolean array of module states.
	/// </summary>
	public static bool[] GenerateCode128(string text)
	{
		ArgumentNullException.ThrowIfNull(text);

		var values = new List<int>();

		// Start B = 104
		values.Add(104);

		// Encode each character
		foreach (char ch in text)
		{
			int val = ch - 32; // Code 128B: ASCII 32 maps to value 0
			if (val is < 0 or > 95)
			{
				throw new ArgumentException($"Character '{ch}' (U+{(int)ch:X4}) is not supported in Code 128B.");
			}

			values.Add(val);
		}

		// Check digit: weighted sum mod 103
		int checksum = values[0]; // start code value
		for (int i = 1; i < values.Count; i++)
		{
			checksum += values[i] * i;
		}

		checksum %= 103;
		values.Add(checksum);

		// Stop = 106
		values.Add(106);

		// Convert to module pattern
		var modules = new List<bool>();
		foreach (int val in values)
		{
			string pattern = Code128Patterns[val];
			bool isBar = true;
			foreach (char widthChar in pattern)
			{
				int width = widthChar - '0';
				for (int w = 0; w < width; w++)
				{
					modules.Add(isBar);
				}

				isBar = !isBar;
			}
		}

		// Final 2-module bar after stop
		modules.Add(true);
		modules.Add(true);

		return modules.ToArray();
	}
}
