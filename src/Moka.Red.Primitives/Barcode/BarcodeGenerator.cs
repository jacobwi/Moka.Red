using Moka.Red.Core.Enums;

namespace Moka.Red.Primitives.Barcode;

/// <summary>
///     Internal 1D barcode encoder. Supports Code 128 Subset B, Code 39, EAN-13, EAN-8 and UPC-A.
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

	/// <summary>The 43 Code 39 data characters, in symbol-value order.</summary>
	private const string Code39Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";

	/// <summary>
	///     Code 39 element widths, one entry per character of <see cref="Code39Alphabet" />.
	///     Nine elements alternating bar and space starting with a bar, three of which are wide.
	///     'n' is one module, 'w' is three.
	/// </summary>
	private static readonly string[] Code39Patterns =
	[
		"nnnwwnwnn", // 0
		"wnnwnnnnw", // 1
		"nnwwnnnnw", // 2
		"wnwwnnnnn", // 3
		"nnnwwnnnw", // 4
		"wnnwwnnnn", // 5
		"nnwwwnnnn", // 6
		"nnnwnnwnw", // 7
		"wnnwnnwnn", // 8
		"nnwwnnwnn", // 9
		"wnnnnwnnw", // A
		"nnwnnwnnw", // B
		"wnwnnwnnn", // C
		"nnnnwwnnw", // D
		"wnnnwwnnn", // E
		"nnwnwwnnn", // F
		"nnnnnwwnw", // G
		"wnnnnwwnn", // H
		"nnwnnwwnn", // I
		"nnnnwwwnn", // J
		"wnnnnnnww", // K
		"nnwnnnnww", // L
		"wnwnnnnwn", // M
		"nnnnwnnww", // N
		"wnnnwnnwn", // O
		"nnwnwnnwn", // P
		"nnnnnnwww", // Q
		"wnnnnnwwn", // R
		"nnwnnnwwn", // S
		"nnnnwnwwn", // T
		"wwnnnnnnw", // U
		"nwwnnnnnw", // V
		"wwwnnnnnn", // W
		"nwnnwnnnw", // X
		"wwnnwnnnn", // Y
		"nwwnwnnnn", // Z
		"nwnnnnwnw", // -
		"wwnnnnwnn", // .
		"nwwnnnwnn", // space
		"nwnwnwnnn", // $
		"nwnwnnnwn", // /
		"nwnnnwnwn", // +
		"nnnwnwnwn" // %
	];

	/// <summary>Code 39 start and stop delimiter, printed as an asterisk.</summary>
	private const string Code39Delimiter = "nwnnwnwnn";

	/// <summary>EAN/UPC left-hand odd parity (L) digit patterns. 1 = bar.</summary>
	private static readonly string[] EanLeftOdd =
	[
		"0001101", "0011001", "0010011", "0111101", "0100011",
		"0110001", "0101111", "0111011", "0110111", "0001011"
	];

	/// <summary>EAN left-hand even parity (G) digit patterns, the reverse of the R patterns.</summary>
	private static readonly string[] EanLeftEven =
	[
		"0100111", "0110011", "0011011", "0100001", "0011101",
		"0111001", "0000101", "0010001", "0001001", "0010111"
	];

	/// <summary>EAN/UPC right-hand (R) digit patterns, the complement of the L patterns.</summary>
	private static readonly string[] EanRight =
	[
		"1110010", "1100110", "1101100", "1000010", "1011100",
		"1001110", "1010000", "1000100", "1001000", "1110100"
	];

	/// <summary>
	///     EAN-13 left-half parity selection, indexed by the first digit. 'L' is odd parity,
	///     'G' is even. The first digit is never encoded directly, only through this pattern.
	/// </summary>
	private static readonly string[] Ean13Parity =
	[
		"LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG",
		"LGGLLG", "LGGGLL", "LGLGLG", "LGLGGL", "LGGLGL"
	];

	private const string EanGuard = "101";
	private const string EanCentreGuard = "01010";

	/// <summary>
	///     Encodes text in the requested symbology and returns module states (true = bar).
	/// </summary>
	/// <param name="format">The symbology to encode with.</param>
	/// <param name="text">The data to encode.</param>
	/// <exception cref="ArgumentException">The text is not valid for the symbology.</exception>
	public static bool[] Generate(MokaBarcodeFormat format, string text)
	{
		return format switch
		{
			MokaBarcodeFormat.Code128 => GenerateCode128(text),
			MokaBarcodeFormat.Code39 => GenerateCode39(text),
			MokaBarcodeFormat.EAN13 => GenerateEan13(text),
			MokaBarcodeFormat.EAN8 => GenerateEan8(text),
			MokaBarcodeFormat.UPC => GenerateUpcA(text),
			_ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown barcode format.")
		};
	}

	/// <summary>
	///     Returns the human-readable text printed under the symbol. EAN and UPC codes are
	///     normalized to their full length including the computed check digit.
	/// </summary>
	/// <param name="format">The symbology being rendered.</param>
	/// <param name="text">The data passed to <see cref="Generate" />.</param>
	public static string GetDisplayText(MokaBarcodeFormat format, string text)
	{
		ArgumentNullException.ThrowIfNull(text);

		return format switch
		{
			MokaBarcodeFormat.Code39 => text.ToUpperInvariant(),
			MokaBarcodeFormat.EAN13 => WithCheckDigit(text, 12, 1, "EAN-13"),
			MokaBarcodeFormat.EAN8 => WithCheckDigit(text, 7, 3, "EAN-8"),
			MokaBarcodeFormat.UPC => WithCheckDigit(text, 11, 3, "UPC-A"),
			_ => text
		};
	}

	/// <summary>
	///     Returns the left and right quiet zone widths in modules. A symbol printed flush to the
	///     edge of its canvas cannot be read, so every format reserves margin.
	/// </summary>
	/// <param name="format">The symbology being rendered.</param>
	public static (int Left, int Right) GetQuietZone(MokaBarcodeFormat format)
	{
		return format switch
		{
			MokaBarcodeFormat.EAN13 => (11, 7),
			MokaBarcodeFormat.EAN8 => (7, 7),
			MokaBarcodeFormat.UPC => (9, 9),
			_ => (10, 10)
		};
	}

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

	/// <summary>
	///     Encodes text using Code 39 (43 characters, narrow:wide ratio 1:3) with the standard
	///     asterisk start and stop delimiters. Lowercase input is uppercased.
	/// </summary>
	public static bool[] GenerateCode39(string text)
	{
		ArgumentNullException.ThrowIfNull(text);

		string upper = text.ToUpperInvariant();
		var modules = new List<bool>(upper.Length * 16 + 32);

		AppendCode39Symbol(modules, Code39Delimiter);

		foreach (char ch in upper)
		{
			int index = Code39Alphabet.AsSpan().IndexOf(ch);
			if (index < 0)
			{
				throw new ArgumentException($"Character '{ch}' (U+{(int)ch:X4}) is not supported in Code 39.");
			}

			// Narrow inter-character gap
			modules.Add(false);
			AppendCode39Symbol(modules, Code39Patterns[index]);
		}

		modules.Add(false);
		AppendCode39Symbol(modules, Code39Delimiter);

		return modules.ToArray();
	}

	/// <summary>
	///     Encodes an EAN-13 symbol. Accepts 12 data digits, or 13 when the trailing check
	///     digit is supplied (it is then verified).
	/// </summary>
	public static bool[] GenerateEan13(string text)
	{
		string data = NormalizeNumericInput(text, 12, 1, "EAN-13");
		int check = ComputeCheckDigit(data.AsSpan(), 1);
		string parity = Ean13Parity[data[0] - '0'];

		var modules = new List<bool>(95);
		AppendBinaryPattern(modules, EanGuard);

		for (int i = 1; i <= 6; i++)
		{
			int digit = data[i] - '0';
			AppendBinaryPattern(modules, parity[i - 1] == 'G' ? EanLeftEven[digit] : EanLeftOdd[digit]);
		}

		AppendBinaryPattern(modules, EanCentreGuard);

		for (int i = 7; i < 12; i++)
		{
			AppendBinaryPattern(modules, EanRight[data[i] - '0']);
		}

		AppendBinaryPattern(modules, EanRight[check]);
		AppendBinaryPattern(modules, EanGuard);

		return modules.ToArray();
	}

	/// <summary>
	///     Encodes an EAN-8 symbol. Accepts 7 data digits, or 8 when the trailing check
	///     digit is supplied (it is then verified).
	/// </summary>
	public static bool[] GenerateEan8(string text)
	{
		string data = NormalizeNumericInput(text, 7, 3, "EAN-8");
		int check = ComputeCheckDigit(data.AsSpan(), 3);

		var modules = new List<bool>(67);
		AppendBinaryPattern(modules, EanGuard);

		for (int i = 0; i < 4; i++)
		{
			AppendBinaryPattern(modules, EanLeftOdd[data[i] - '0']);
		}

		AppendBinaryPattern(modules, EanCentreGuard);

		for (int i = 4; i < 7; i++)
		{
			AppendBinaryPattern(modules, EanRight[data[i] - '0']);
		}

		AppendBinaryPattern(modules, EanRight[check]);
		AppendBinaryPattern(modules, EanGuard);

		return modules.ToArray();
	}

	/// <summary>
	///     Encodes a UPC-A symbol. Accepts 11 data digits, or 12 when the trailing check
	///     digit is supplied (it is then verified).
	/// </summary>
	public static bool[] GenerateUpcA(string text)
	{
		string data = NormalizeNumericInput(text, 11, 3, "UPC-A");
		int check = ComputeCheckDigit(data.AsSpan(), 3);

		var modules = new List<bool>(95);
		AppendBinaryPattern(modules, EanGuard);

		for (int i = 0; i < 6; i++)
		{
			AppendBinaryPattern(modules, EanLeftOdd[data[i] - '0']);
		}

		AppendBinaryPattern(modules, EanCentreGuard);

		for (int i = 6; i < 11; i++)
		{
			AppendBinaryPattern(modules, EanRight[data[i] - '0']);
		}

		AppendBinaryPattern(modules, EanRight[check]);
		AppendBinaryPattern(modules, EanGuard);

		return modules.ToArray();
	}

	/// <summary>Expands a Code 39 narrow/wide element string into module states.</summary>
	private static void AppendCode39Symbol(List<bool> modules, string pattern)
	{
		bool isBar = true;
		foreach (char element in pattern)
		{
			int width = element == 'w' ? 3 : 1;
			for (int i = 0; i < width; i++)
			{
				modules.Add(isBar);
			}

			isBar = !isBar;
		}
	}

	/// <summary>Expands an EAN/UPC '0'/'1' pattern into module states.</summary>
	private static void AppendBinaryPattern(List<bool> modules, string pattern)
	{
		foreach (char bit in pattern)
		{
			modules.Add(bit == '1');
		}
	}

	/// <summary>
	///     Modulo-10 check digit used by EAN and UPC. <paramref name="firstWeight" /> is applied to the
	///     leftmost digit and the weights then alternate between 1 and 3.
	/// </summary>
	private static int ComputeCheckDigit(ReadOnlySpan<char> digits, int firstWeight)
	{
		int sum = 0;
		int weight = firstWeight;
		foreach (char ch in digits)
		{
			sum += (ch - '0') * weight;
			weight = weight == 1 ? 3 : 1;
		}

		return (10 - sum % 10) % 10;
	}

	/// <summary>
	///     Validates digit-only input of <paramref name="dataLength" /> digits, or one more when a
	///     check digit is supplied, and returns the data digits without the check digit.
	/// </summary>
	private static string NormalizeNumericInput(string text, int dataLength, int firstWeight, string symbology)
	{
		ArgumentNullException.ThrowIfNull(text);

		string trimmed = text.Trim();

		foreach (char ch in trimmed)
		{
			if (!char.IsAsciiDigit(ch))
			{
				throw new ArgumentException($"{symbology} accepts digits only; '{ch}' is not a digit.");
			}
		}

		if (trimmed.Length == dataLength)
		{
			return trimmed;
		}

		if (trimmed.Length == dataLength + 1)
		{
			int expected = ComputeCheckDigit(trimmed.AsSpan(0, dataLength), firstWeight);
			int supplied = trimmed[dataLength] - '0';
			if (expected != supplied)
			{
				throw new ArgumentException($"{symbology} check digit is {supplied} but should be {expected}.");
			}

			return trimmed[..dataLength];
		}

		throw new ArgumentException(
			$"{symbology} requires {dataLength} or {dataLength + 1} digits but got {trimmed.Length}.");
	}

	/// <summary>Returns the normalized data digits followed by the computed check digit.</summary>
	private static string WithCheckDigit(string text, int dataLength, int firstWeight, string symbology)
	{
		string data = NormalizeNumericInput(text, dataLength, firstWeight, symbology);
		return data + (char)('0' + ComputeCheckDigit(data.AsSpan(), firstWeight));
	}
}
