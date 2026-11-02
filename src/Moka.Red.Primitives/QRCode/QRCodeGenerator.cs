using System.Text;
using Moka.Red.Core.Enums;

namespace Moka.Red.Primitives.QRCode;

/// <summary>
///     Pure C# QR code encoder supporting byte mode, versions 1-40.
///     Produces a boolean grid suitable for SVG rendering.
/// </summary>
internal static class QRCodeGenerator
{
	/// <summary>The largest QR version this encoder produces: 177 modules per side.</summary>
	public const int MaxVersion = 40;

	// Error correction codewords per block: [version-1][ecLevel], ecLevel 0=L 1=M 2=Q 3=H.
	// Transcribed from the QR standard's block structure table.
	private static readonly byte[][] EcCodewordsPerBlock =
	[
		[7, 10, 13, 17], // V1
		[10, 16, 22, 28], // V2
		[15, 26, 18, 22], // V3
		[20, 18, 26, 16], // V4
		[26, 24, 18, 22], // V5
		[18, 16, 24, 28], // V6
		[20, 18, 18, 26], // V7
		[24, 22, 22, 26], // V8
		[30, 22, 20, 24], // V9
		[18, 26, 24, 28], // V10
		[20, 30, 28, 24], // V11
		[24, 22, 26, 28], // V12
		[26, 22, 24, 22], // V13
		[30, 24, 20, 24], // V14
		[22, 24, 30, 24], // V15
		[24, 28, 24, 30], // V16
		[28, 28, 28, 28], // V17
		[30, 26, 28, 28], // V18
		[28, 26, 26, 26], // V19
		[28, 26, 30, 28], // V20
		[28, 26, 28, 30], // V21
		[28, 28, 30, 24], // V22
		[30, 28, 30, 30], // V23
		[30, 28, 30, 30], // V24
		[26, 28, 30, 30], // V25
		[28, 28, 28, 30], // V26
		[30, 28, 30, 30], // V27
		[30, 28, 30, 30], // V28
		[30, 28, 30, 30], // V29
		[30, 28, 30, 30], // V30
		[30, 28, 30, 30], // V31
		[30, 28, 30, 30], // V32
		[30, 28, 30, 30], // V33
		[30, 28, 30, 30], // V34
		[30, 28, 30, 30], // V35
		[30, 28, 30, 30], // V36
		[30, 28, 30, 30], // V37
		[30, 28, 30, 30], // V38
		[30, 28, 30, 30], // V39
		[30, 28, 30, 30], // V40
];

	// Number of error correction blocks: [version-1][ecLevel]. The block structure is what a
	// decoder reconstructs the codeword interleave from, so every entry must match the standard's
	// table exactly - a wrong count produces a code that looks fine and scans as nothing.
	private static readonly byte[][] NumEcBlocks =
	[
		[1, 1, 1, 1], // V1
		[1, 1, 1, 1], // V2
		[1, 1, 2, 2], // V3
		[1, 2, 2, 4], // V4
		[1, 2, 4, 4], // V5
		[2, 4, 4, 4], // V6
		[2, 4, 6, 5], // V7
		[2, 4, 6, 6], // V8
		[2, 5, 8, 8], // V9
		[4, 5, 8, 8], // V10
		[4, 5, 8, 11], // V11
		[4, 8, 10, 11], // V12
		[4, 9, 12, 16], // V13
		[4, 9, 16, 16], // V14
		[6, 10, 12, 18], // V15
		[6, 10, 17, 16], // V16
		[6, 11, 16, 19], // V17
		[6, 13, 18, 21], // V18
		[7, 14, 21, 25], // V19
		[8, 16, 20, 25], // V20
		[8, 17, 23, 25], // V21
		[9, 17, 23, 34], // V22
		[9, 18, 25, 30], // V23
		[10, 20, 27, 32], // V24
		[12, 21, 29, 35], // V25
		[12, 23, 34, 37], // V26
		[12, 25, 34, 40], // V27
		[13, 26, 35, 42], // V28
		[14, 28, 38, 45], // V29
		[15, 29, 40, 48], // V30
		[16, 31, 43, 51], // V31
		[17, 33, 45, 54], // V32
		[18, 35, 48, 57], // V33
		[19, 37, 51, 60], // V34
		[19, 38, 53, 63], // V35
		[20, 40, 56, 66], // V36
		[21, 43, 59, 70], // V37
		[22, 45, 62, 74], // V38
		[24, 47, 65, 77], // V39
		[25, 49, 68, 81], // V40
];

	// Format info for each EC level and mask pattern (pre-computed with BCH)
	private static readonly uint[] FormatInfoBits = ComputeAllFormatInfo();

	// Penalty rule 3 reference patterns: the 1:1:3:1:1 dark-light ratio of a finder pattern
	// with four light modules on one side. true = dark.
	private static readonly bool[][] FinderLikePatterns =
	[
		// 1 0 1 1 1 0 1 0 0 0 0
		[true, false, true, true, true, false, true, false, false, false, false],
		// 0 0 0 0 1 0 1 1 1 0 1
		[false, false, false, false, true, false, true, true, true, false, true]
	];

	/// <summary>
	///     Generates a QR code as a jagged boolean array where true = dark module.
	/// </summary>
	public static bool[][] Generate(string text, MokaQRErrorCorrection ecLevel)
	{
		ArgumentNullException.ThrowIfNull(text);

		int ecIndex = (int)ecLevel;
		byte[] dataBytes = Encoding.UTF8.GetBytes(text);
		int version = DetermineVersion(dataBytes.Length, ecIndex);

		if (version < 0)
		{
			throw new ArgumentException(
				$"Data too long for QR versions 1-{MaxVersion} at {ecLevel} error correction.");
		}

		int size = 17 + version * 4;
		bool[][] modules = CreateGrid(size);
		bool[][] isFunction = CreateGrid(size);

		// Place function patterns
		PlaceFinderPatterns(modules, isFunction, size);
		PlaceAlignmentPatterns(modules, isFunction, version, size);
		PlaceTimingPatterns(modules, isFunction, size);
		PlaceDarkModule(modules, isFunction, version);
		ReserveFormatArea(isFunction, size);
		if (version >= 7)
		{
			ReserveVersionArea(isFunction, size);
		}

		// Encode data
		byte[] dataCodewords = EncodeData(dataBytes, version, ecIndex);
		byte[][] ecCodewords = ComputeErrorCorrection(dataCodewords, version, ecIndex);
		byte[] allCodewords = InterleaveCodewords(dataCodewords, ecCodewords, version, ecIndex);
		bool[] bitStream = CodewordsToBits(allCodewords);

		// Place data
		PlaceDataBits(modules, isFunction, bitStream, size);

		// Apply best mask
		int bestMask = FindBestMask(modules, isFunction, size);
		ApplyMask(modules, isFunction, size, bestMask);

		// Write format info
		WriteFormatInfo(modules, size, ecIndex, bestMask);

		// Write version info for V7+
		if (version >= 7)
		{
			WriteVersionInfo(modules, size, version);
		}

		return modules;
	}

	/// <summary>
	///     The most bytes of payload a version holds at an error correction level: its data
	///     codewords, less the byte-mode indicator and the character count.
	/// </summary>
	public static int PayloadCapacity(int version, MokaQRErrorCorrection ecLevel)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(version, 1);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(version, MaxVersion);

		int ecIndex = (int)ecLevel;
		int dataCodewords = TotalCodewords(version)
			- EcCodewordsPerBlock[version - 1][ecIndex] * NumEcBlocks[version - 1][ecIndex];
		int countBits = version <= 9 ? 8 : 16;
		return (dataCodewords * 8 - 4 - countBits) / 8;
	}

	private static bool[][] CreateGrid(int size)
	{
		bool[][] grid = new bool[size][];
		for (int i = 0; i < size; i++)
		{
			grid[i] = new bool[size];
		}

		return grid;
	}

	private static bool[][] CloneGrid(bool[][] grid)
	{
		bool[][] clone = new bool[grid.Length][];
		for (int i = 0; i < grid.Length; i++)
		{
			clone[i] = (bool[])grid[i].Clone();
		}

		return clone;
	}

	// Total data-and-EC codewords: every module in the symbol that is not a function pattern.
	// The alignment term applies from V2 on (V1 has no alignment patterns), the version-info
	// blocks from V7 on.
	private static int TotalCodewords(int version)
	{
		int result = (16 * version + 128) * version + 64;
		if (version >= 2)
		{
			int numAlign = version / 7 + 2;
			result -= (25 * numAlign - 10) * numAlign - 55;
			if (version >= 7)
			{
				result -= 36;
			}
		}

		return result / 8;
	}

	private static int DetermineVersion(int dataLength, int ecIndex)
	{
		for (int version = 1; version <= MaxVersion; version++)
		{
			int ecCodewords = EcCodewordsPerBlock[version - 1][ecIndex] * NumEcBlocks[version - 1][ecIndex];
			int countBits = version <= 9 ? 8 : 16;
			if ((TotalCodewords(version) - ecCodewords) * 8 - 4 - countBits >= dataLength * 8)
			{
				return version;
			}
		}

		return -1;
	}

	private static byte[] EncodeData(byte[] data, int version, int ecIndex)
	{
		int totalDataCodewords = TotalCodewords(version) - GetTotalEcCodewords(version, ecIndex);
		var bits = new List<bool>();

		// Mode indicator: byte mode = 0100
		bits.AddRange([false, true, false, false]);

		// Character count: 8 bits for V1-9, 16 bits for V10+
		int countBits = version <= 9 ? 8 : 16;
		for (int i = countBits - 1; i >= 0; i--)
		{
			bits.Add(((data.Length >> i) & 1) == 1);
		}

		// Data
		foreach (byte b in data)
		{
			for (int i = 7; i >= 0; i--)
			{
				bits.Add(((b >> i) & 1) == 1);
			}
		}

		// Terminator (up to 4 zero bits)
		int maxBits = totalDataCodewords * 8;
		int terminatorLen = Math.Min(4, maxBits - bits.Count);
		for (int i = 0; i < terminatorLen; i++)
		{
			bits.Add(false);
		}

		// Pad to byte boundary
		while (bits.Count % 8 != 0)
		{
			bits.Add(false);
		}

		// Pad with alternating 0xEC, 0x11
		byte[] padBytes = [0xEC, 0x11];
		int padIndex = 0;
		while (bits.Count < maxBits)
		{
			byte pb = padBytes[padIndex % 2];
			for (int i = 7; i >= 0; i--)
			{
				bits.Add(((pb >> i) & 1) == 1);
			}

			padIndex++;
		}

		// Convert to bytes
		byte[] result = new byte[totalDataCodewords];
		for (int i = 0; i < totalDataCodewords; i++)
		{
			int val = 0;
			for (int b = 0; b < 8; b++)
			{
				val = (val << 1) | (bits[i * 8 + b] ? 1 : 0);
			}

			result[i] = (byte)val;
		}

		return result;
	}

	private static int GetTotalEcCodewords(int version, int ecIndex) =>
		EcCodewordsPerBlock[version - 1][ecIndex] * NumEcBlocks[version - 1][ecIndex];

	private static byte[][] ComputeErrorCorrection(byte[] dataCodewords, int version, int ecIndex)
	{
		int numBlocks = NumEcBlocks[version - 1][ecIndex];
		int ecPerBlock = EcCodewordsPerBlock[version - 1][ecIndex];
		int totalData = dataCodewords.Length;
		int shortBlockSize = totalData / numBlocks;
		int longBlocks = totalData % numBlocks;
		int shortBlocks = numBlocks - longBlocks;

		byte[] generator = GetGeneratorPolynomial(ecPerBlock);
		byte[][] ecBlocks = new byte[numBlocks][];
		int offset = 0;

		for (int i = 0; i < numBlocks; i++)
		{
			int blockLen = shortBlockSize + (i >= shortBlocks ? 1 : 0);
			byte[] block = new byte[blockLen];
			Array.Copy(dataCodewords, offset, block, 0, blockLen);
			offset += blockLen;
			ecBlocks[i] = ReedSolomonEncode(block, ecPerBlock, generator);
		}

		return ecBlocks;
	}

	private static byte[][] SplitDataBlocks(byte[] dataCodewords, int version, int ecIndex)
	{
		int numBlocks = NumEcBlocks[version - 1][ecIndex];
		int totalData = dataCodewords.Length;
		int shortBlockSize = totalData / numBlocks;
		int longBlocks = totalData % numBlocks;
		int shortBlocks = numBlocks - longBlocks;

		byte[][] blocks = new byte[numBlocks][];
		int offset = 0;
		for (int i = 0; i < numBlocks; i++)
		{
			int blockLen = shortBlockSize + (i >= shortBlocks ? 1 : 0);
			blocks[i] = new byte[blockLen];
			Array.Copy(dataCodewords, offset, blocks[i], 0, blockLen);
			offset += blockLen;
		}

		return blocks;
	}

	private static byte[] InterleaveCodewords(byte[] dataCodewords, byte[][] ecBlocks, int version, int ecIndex)
	{
		byte[][] dataBlocks = SplitDataBlocks(dataCodewords, version, ecIndex);
		var result = new List<byte>();

		// Interleave data blocks
		int maxDataLen = dataBlocks.Max(b => b.Length);
		for (int i = 0; i < maxDataLen; i++)
		{
			foreach (byte[] block in dataBlocks)
			{
				if (i < block.Length)
				{
					result.Add(block[i]);
				}
			}
		}

		// Interleave EC blocks
		int ecLen = ecBlocks[0].Length;
		for (int i = 0; i < ecLen; i++)
		{
			foreach (byte[] block in ecBlocks)
			{
				if (i < block.Length)
				{
					result.Add(block[i]);
				}
			}
		}

		return result.ToArray();
	}

	private static bool[] CodewordsToBits(byte[] codewords)
	{
		bool[] bits = new bool[codewords.Length * 8];
		for (int i = 0; i < codewords.Length; i++)
		{
			for (int b = 0; b < 8; b++)
			{
				bits[i * 8 + b] = ((codewords[i] >> (7 - b)) & 1) == 1;
			}
		}

		return bits;
	}

	#region Data Placement

	private static void PlaceDataBits(bool[][] modules, bool[][] isFunction, bool[] bits, int size)
	{
		int bitIndex = 0;
		int right = size - 1;

		while (right >= 1)
		{
			if (right == 6)
			{
				right = 5;
			}

			for (int vert = 0; vert < size; vert++)
			{
				for (int j = 0; j < 2; j++)
				{
					int col = right - j;
					bool upward = (size - 1 - right + (right > 6 ? 1 : 0)) / 2 % 2 == 0;
					int row = upward ? size - 1 - vert : vert;

					if (col >= 0 && col < size && row >= 0 && row < size && !isFunction[row][col])
					{
						if (bitIndex < bits.Length)
						{
							modules[row][col] = bits[bitIndex];
							bitIndex++;
						}
						else
						{
							modules[row][col] = false;
						}
					}
				}
			}

			right -= 2;
		}
	}

	#endregion

	#region Function Patterns

	private static void PlaceFinderPatterns(bool[][] modules, bool[][] isFunction, int size)
	{
		int[][] positions = [[0, 0], [size - 7, 0], [0, size - 7]];

		foreach (int[] pos in positions)
		{
			int r = pos[0];
			int c = pos[1];
			for (int dr = -1; dr <= 7; dr++)
			{
				for (int dc = -1; dc <= 7; dc++)
				{
					int rr = r + dr;
					int cc = c + dc;
					if (rr < 0 || rr >= size || cc < 0 || cc >= size)
					{
						continue;
					}

					bool inOuter = dr is >= 0 and <= 6 && dc is >= 0 and <= 6;
					bool inInner = dr is >= 1 and <= 5 && dc is >= 1 and <= 5;
					bool inCore = dr is >= 2 and <= 4 && dc is >= 2 and <= 4;

					isFunction[rr][cc] = true;
					modules[rr][cc] = inCore || (inOuter && !inInner);
				}
			}
		}
	}

	// The alignment pattern grid: every combination of the row and column positions, except the
	// three that would land on a finder. V1 has none; from V2 the positions run from 6 to
	// size - 7, filled from the far end so the last one always lands exactly on size - 7.
	private static void PlaceAlignmentPatterns(bool[][] modules, bool[][] isFunction, int version, int size)
	{
		if (version == 1)
		{
			return;
		}

		int numAlign = version / 7 + 2;
		int step = (version * 8 + numAlign * 3 + 5) / (numAlign * 4 - 4) * 2;
		int[] positions = new int[numAlign];
		positions[0] = 6;
		for (int i = numAlign - 1, pos = size - 7; i >= 1; i--, pos -= step)
		{
			positions[i] = pos;
		}

		foreach (int r in positions)
		{
			foreach (int c in positions)
			{
				if ((r <= 8 && c <= 8) || (r <= 8 && c >= size - 8) || (r >= size - 8 && c <= 8))
				{
					continue;
				}

				for (int dr = -2; dr <= 2; dr++)
				{
					for (int dc = -2; dc <= 2; dc++)
					{
						int rr = r + dr;
						int cc = c + dc;
						if (rr < 0 || rr >= size || cc < 0 || cc >= size)
						{
							continue;
						}

						isFunction[rr][cc] = true;
						modules[rr][cc] = Math.Abs(dr) == 2 || Math.Abs(dc) == 2 || (dr == 0 && dc == 0);
					}
				}
			}
		}
	}

	private static void PlaceTimingPatterns(bool[][] modules, bool[][] isFunction, int size)
	{
		for (int i = 8; i < size - 8; i++)
		{
			if (!isFunction[6][i])
			{
				isFunction[6][i] = true;
				modules[6][i] = i % 2 == 0;
			}

			if (!isFunction[i][6])
			{
				isFunction[i][6] = true;
				modules[i][6] = i % 2 == 0;
			}
		}
	}

	private static void PlaceDarkModule(bool[][] modules, bool[][] isFunction, int version)
	{
		int row = 4 * version + 9;
		isFunction[row][8] = true;
		modules[row][8] = true;
	}

	private static void ReserveFormatArea(bool[][] isFunction, int size)
	{
		for (int i = 0; i <= 8; i++)
		{
			if (i < size)
			{
				isFunction[8][i] = true;
			}

			if (i < size)
			{
				isFunction[i][8] = true;
			}
		}

		for (int i = 0; i <= 7; i++)
		{
			if (size - 1 - i >= 0)
			{
				isFunction[8][size - 1 - i] = true;
			}
		}

		for (int i = 0; i <= 7; i++)
		{
			if (size - 1 - i >= 0)
			{
				isFunction[size - 1 - i][8] = true;
			}
		}
	}

	private static void ReserveVersionArea(bool[][] isFunction, int size)
	{
		for (int i = 0; i < 6; i++)
		for (int j = 0; j < 3; j++)
		{
			isFunction[size - 11 + j][i] = true;
		}

		for (int i = 0; i < 6; i++)
		for (int j = 0; j < 3; j++)
		{
			isFunction[i][size - 11 + j] = true;
		}
	}

	#endregion

	#region Masking

	private static bool GetMaskBit(int row, int col, int maskPattern)
	{
		return maskPattern switch
		{
			0 => (row + col) % 2 == 0,
			1 => row % 2 == 0,
			2 => col % 3 == 0,
			3 => (row + col) % 3 == 0,
			4 => (row / 2 + col / 3) % 2 == 0,
			5 => row * col % 2 + row * col % 3 == 0,
			6 => (row * col % 2 + row * col % 3) % 2 == 0,
			7 => ((row + col) % 2 + row * col % 3) % 2 == 0,
			_ => false
		};
	}

	private static int FindBestMask(bool[][] modules, bool[][] isFunction, int size)
	{
		int bestScore = int.MaxValue;
		int bestMask = 0;

		for (int mask = 0; mask < 8; mask++)
		{
			bool[][] test = CloneGrid(modules);
			ApplyMask(test, isFunction, size, mask);
			int score = EvaluatePenalty(test, size);
			if (score < bestScore)
			{
				bestScore = score;
				bestMask = mask;
			}
		}

		return bestMask;
	}

	private static void ApplyMask(bool[][] modules, bool[][] isFunction, int size, int maskPattern)
	{
		for (int r = 0; r < size; r++)
		{
			for (int c = 0; c < size; c++)
			{
				if (!isFunction[r][c] && GetMaskBit(r, c, maskPattern))
				{
					modules[r][c] = !modules[r][c];
				}
			}
		}
	}

	private static int EvaluatePenalty(bool[][] modules, int size)
	{
		int penalty = 0;

		// Rule 1: Runs of 5+ same-color modules
		for (int r = 0; r < size; r++)
		{
			int runLen = 1;
			for (int c = 1; c < size; c++)
			{
				if (modules[r][c] == modules[r][c - 1])
				{
					runLen++;
					if (runLen == 5)
					{
						penalty += 3;
					}
					else if (runLen > 5)
					{
						penalty++;
					}
				}
				else
				{
					runLen = 1;
				}
			}
		}

		for (int c = 0; c < size; c++)
		{
			int runLen = 1;
			for (int r = 1; r < size; r++)
			{
				if (modules[r][c] == modules[r - 1][c])
				{
					runLen++;
					if (runLen == 5)
					{
						penalty += 3;
					}
					else if (runLen > 5)
					{
						penalty++;
					}
				}
				else
				{
					runLen = 1;
				}
			}
		}

		// Rule 2: 2x2 same-color blocks
		for (int r = 0; r < size - 1; r++)
		{
			for (int c = 0; c < size - 1; c++)
			{
				bool val = modules[r][c];
				if (val == modules[r][c + 1] && val == modules[r + 1][c] && val == modules[r + 1][c + 1])
				{
					penalty += 3;
				}
			}
		}

		// Rule 3: Finder-like 1:1:3:1:1 patterns bordered by four light modules
		penalty += EvaluateFinderPenalty(modules, size);

		// Rule 4: Proportion of dark modules
		int darkCount = 0;
		for (int r = 0; r < size; r++)
		for (int c = 0; c < size; c++)
		{
			if (modules[r][c])
			{
				darkCount++;
			}
		}

		int totalModules = size * size;
		int darkPercent = darkCount * 100 / totalModules;
		int prev5 = darkPercent / 5 * 5;
		int next5 = prev5 + 5;
		penalty += Math.Min(Math.Abs(prev5 - 50) / 5, Math.Abs(next5 - 50) / 5) * 10;

		return penalty;
	}

	/// <summary>
	///     Penalty rule 3: 40 points for every occurrence of a finder-like pattern in any row or
	///     column. Each of the two reference patterns is matched at every offset that fits.
	/// </summary>
	private static int EvaluateFinderPenalty(bool[][] modules, int size)
	{
		int penalty = 0;

		foreach (bool[] pattern in FinderLikePatterns)
		{
			int lastOffset = size - pattern.Length;
			for (int line = 0; line < size; line++)
			{
				for (int offset = 0; offset <= lastOffset; offset++)
				{
					if (RowMatches(modules, line, offset, pattern))
					{
						penalty += 40;
					}

					if (ColumnMatches(modules, line, offset, pattern))
					{
						penalty += 40;
					}
				}
			}
		}

		return penalty;
	}

	private static bool RowMatches(bool[][] modules, int row, int startCol, bool[] pattern)
	{
		for (int i = 0; i < pattern.Length; i++)
		{
			if (modules[row][startCol + i] != pattern[i])
			{
				return false;
			}
		}

		return true;
	}

	private static bool ColumnMatches(bool[][] modules, int col, int startRow, bool[] pattern)
	{
		for (int i = 0; i < pattern.Length; i++)
		{
			if (modules[startRow + i][col] != pattern[i])
			{
				return false;
			}
		}

		return true;
	}

	#endregion

	#region Format & Version Info

	private static uint[] ComputeAllFormatInfo()
	{
		uint[] result = new uint[32];
		int[] ecIndicators = [1, 0, 3, 2];

		for (int ec = 0; ec < 4; ec++)
		{
			for (int mask = 0; mask < 8; mask++)
			{
				uint data = (uint)((ecIndicators[ec] << 3) | mask);
				uint encoded = data << 10;
				uint divisor = 0x537u;

				for (int i = 14; i >= 10; i--)
				{
					if ((encoded & (1u << i)) != 0)
					{
						encoded ^= divisor << (i - 10);
					}
				}

				uint formatBits = (data << 10) | encoded;
				formatBits ^= 0x5412;
				result[ec * 8 + mask] = formatBits;
			}
		}

		return result;
	}

	// The 15 format bits are laid out least-significant-bit first along the path they take: up
	// column 8 from the top, across row 8 to the left, and in the second copy along row 8 from the
	// right edge and down column 8 to the bottom. Writing them most-significant first, as this
	// once did, mirrors every bit of both copies - a code that looks right and scans as nothing.
	private static void WriteFormatInfo(bool[][] modules, int size, int ecIndex, int mask)
	{
		uint bits = FormatInfoBits[ecIndex * 8 + mask];

		bool Bit(int i) => ((bits >> i) & 1) == 1;

		// First copy, around the top-left finder
		for (int i = 0; i <= 5; i++)
		{
			modules[i][8] = Bit(i);
		}

		modules[7][8] = Bit(6);
		modules[8][8] = Bit(7);
		modules[8][7] = Bit(8);
		for (int i = 9; i <= 14; i++)
		{
			modules[8][14 - i] = Bit(i);
		}

		// Second copy, split by the dark module
		for (int i = 0; i <= 7; i++)
		{
			modules[8][size - 1 - i] = Bit(i);
		}

		for (int i = 8; i <= 14; i++)
		{
			modules[size - 15 + i][8] = Bit(i);
		}
	}

	private static void WriteVersionInfo(bool[][] modules, int size, int version)
	{
		if (version < 7)
		{
			return;
		}

		uint data = (uint)version;
		uint encoded = data << 12;
		uint divisor = 0x1F25u;

		for (int i = 17; i >= 12; i--)
		{
			if ((encoded & (1u << i)) != 0)
			{
				encoded ^= divisor << (i - 12);
			}
		}

		uint versionBits = (data << 12) | encoded;

		for (int i = 0; i < 18; i++)
		{
			bool bit = ((versionBits >> i) & 1) == 1;
			int row = i / 3;
			int col = i % 3;
			modules[size - 11 + col][row] = bit;
			modules[row][size - 11 + col] = bit;
		}
	}

	#endregion

	#region Reed-Solomon

	private static readonly byte[] GfExp = new byte[512];
	private static readonly byte[] GfLog = new byte[256];

	static QRCodeGenerator()
	{
		int x = 1;
		for (int i = 0; i < 255; i++)
		{
			GfExp[i] = (byte)x;
			GfLog[x] = (byte)i;
			x <<= 1;
			if (x >= 256)
			{
				x ^= 0x11D;
			}
		}

		for (int i = 255; i < 512; i++)
		{
			GfExp[i] = GfExp[i - 255];
		}
	}

	private static byte GfMul(byte a, byte b)
	{
		if (a == 0 || b == 0)
		{
			return 0;
		}

		return GfExp[GfLog[a] + GfLog[b]];
	}

	private static byte[] GetGeneratorPolynomial(int degree)
	{
		byte[] gen = [1];
		for (int i = 0; i < degree; i++)
		{
			byte[] factor = [1, GfExp[i]];
			gen = PolyMultiply(gen, factor);
		}

		return gen;
	}

	private static byte[] PolyMultiply(byte[] a, byte[] b)
	{
		byte[] result = new byte[a.Length + b.Length - 1];
		for (int i = 0; i < a.Length; i++)
		{
			for (int j = 0; j < b.Length; j++)
			{
				result[i + j] ^= GfMul(a[i], b[j]);
			}
		}

		return result;
	}

	private static byte[] ReedSolomonEncode(byte[] data, int ecCodewords, byte[] generator)
	{
		byte[] dividend = new byte[data.Length + ecCodewords];
		Array.Copy(data, dividend, data.Length);

		for (int i = 0; i < data.Length; i++)
		{
			byte coef = dividend[i];
			if (coef != 0)
			{
				for (int j = 0; j < generator.Length; j++)
				{
					dividend[i + j] ^= GfMul(generator[j], coef);
				}
			}
		}

		byte[] result = new byte[ecCodewords];
		Array.Copy(dividend, data.Length, result, 0, ecCodewords);
		return result;
	}

	#endregion
}
