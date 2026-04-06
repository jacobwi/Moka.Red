using System.Text;
using Moka.Red.Core.Enums;

namespace Moka.Red.Primitives.QRCode;

/// <summary>
///     Minimal pure C# QR code encoder supporting byte mode, versions 1-10.
///     Produces a boolean grid suitable for SVG rendering.
/// </summary>
internal static class QRCodeGenerator
{
	// Capacity table: [version-1][ecLevel] = max data bytes (byte mode)
	private static readonly int[][] DataCapacity =
	[
		[17, 14, 11, 7], // V1
		[32, 26, 20, 14], // V2
		[53, 42, 32, 24], // V3
		[78, 62, 46, 34], // V4
		[106, 84, 60, 44], // V5
		[134, 106, 74, 58], // V6
		[154, 122, 86, 64], // V7
		[192, 152, 108, 84], // V8
		[230, 180, 130, 98], // V9
		[271, 213, 151, 119] // V10
	];

	// Total codewords per version
	private static readonly int[] TotalCodewords = [26, 44, 70, 100, 134, 172, 196, 242, 292, 346];

	// EC codewords per block: [version-1][ecLevel]
	private static readonly int[][] EcCodewordsPerBlock =
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
		[18, 26, 24, 28] // V10
	];

	// Number of EC blocks: [version-1][ecLevel]
	private static readonly int[][] NumEcBlocks =
	[
		[1, 1, 1, 1], // V1
		[1, 1, 1, 1], // V2
		[1, 1, 2, 2], // V3
		[1, 2, 2, 4], // V4
		[1, 2, 2, 2], // V5
		[2, 4, 4, 4], // V6
		[2, 4, 2, 4], // V7
		[2, 2, 4, 4], // V8
		[2, 3, 4, 4], // V9
		[2, 4, 6, 6] // V10
	];

	// Alignment pattern center positions per version (empty for V1)
	private static readonly int[][] AlignmentPositions =
	[
		[], // V1
		[6, 18], // V2
		[6, 22], // V3
		[6, 26], // V4
		[6, 30], // V5
		[6, 34], // V6
		[6, 22, 38], // V7
		[6, 24, 42], // V8
		[6, 26, 46], // V9
		[6, 28, 50] // V10
	];

	// Format info for each EC level and mask pattern (pre-computed with BCH)
	private static readonly uint[] FormatInfoBits = ComputeAllFormatInfo();

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
			throw new ArgumentException($"Data too long for QR versions 1-10 at {ecLevel} error correction.");
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

	private static int DetermineVersion(int dataLength, int ecIndex)
	{
		for (int v = 0; v < DataCapacity.Length; v++)
		{
			if (DataCapacity[v][ecIndex] >= dataLength)
			{
				return v + 1;
			}
		}

		return -1;
	}

	private static byte[] EncodeData(byte[] data, int version, int ecIndex)
	{
		int totalDataCodewords = TotalCodewords[version - 1] - GetTotalEcCodewords(version, ecIndex);
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
		byte[] padBytes = new byte[] { 0xEC, 0x11 };
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

	private static void PlaceAlignmentPatterns(bool[][] modules, bool[][] isFunction, int version, int size)
	{
		int[] positions = AlignmentPositions[version - 1];
		if (positions.Length == 0)
		{
			return;
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

	private static void WriteFormatInfo(bool[][] modules, int size, int ecIndex, int mask)
	{
		uint bits = FormatInfoBits[ecIndex * 8 + mask];

		int[] rowPositions = [0, 1, 2, 3, 4, 5, 7, 8, 8, 8, 8, 8, 8, 8, 8];
		int[] colPositions = [8, 8, 8, 8, 8, 8, 8, 8, 7, 5, 4, 3, 2, 1, 0];

		for (int i = 0; i < 15; i++)
		{
			bool bit = ((bits >> (14 - i)) & 1) == 1;
			modules[rowPositions[i]][colPositions[i]] = bit;
		}

		for (int i = 0; i < 8; i++)
		{
			bool bit = ((bits >> (14 - i)) & 1) == 1;
			modules[8][size - 1 - i] = bit;
		}

		for (int i = 8; i < 15; i++)
		{
			bool bit = ((bits >> (14 - i)) & 1) == 1;
			modules[size - 15 + i][8] = bit;
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
		byte[] gen = new byte[] { 1 };
		for (int i = 0; i < degree; i++)
		{
			byte[] factor = new byte[] { 1, GfExp[i] };
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
