using System.Buffers.Binary;

namespace Moka.Red.Data.Chart;

/// <summary>
///     Packs a chart's hover geometry into the bytes the chart script reads. Little endian: <c>u16</c> format,
///     <c>u16</c> series count, <c>u32</c> point count, <c>f64</c> x at the plot's left edge, <c>f64</c> x at its right
///     edge, <c>f64</c> geometry version, then the shared x shares as <c>u16</c> (share times 65534, 65535 a gap), then
///     each series' y shares the same way. A byte array crosses the interop without base64.
/// </summary>
internal static class ChartHoverEncoding
{
	/// <summary>The format this file writes.</summary>
	public const ushort Format = 1;

	/// <summary>The value that marks a point the pointer can't land on: a gap, or an x outside the plot.</summary>
	public const ushort Gap = ushort.MaxValue;

	/// <summary>A share of 1 written as a number.</summary>
	public const double Scale = ushort.MaxValue - 1;

	/// <summary>The header's length in bytes.</summary>
	public const int HeaderLength = 32;

	// A point a hair outside the plot still counts, so rounding in the chart's own geometry never drops the ends.
	private const double Slack = 1e-6;

	/// <summary>Packs the geometry.</summary>
	/// <param name="data">Where the drawn points sit.</param>
	/// <param name="version">The geometry version it was built at.</param>
	/// <returns>The bytes the script decodes.</returns>
	public static byte[] Encode(ChartHoverData data, long version)
	{
		ArgumentNullException.ThrowIfNull(data);

		int points = data.X.Length;
		int seriesCount = data.Y.Count;
		int length = HeaderLength + 2 * points * (1 + seriesCount);

		byte[] bytes = new byte[length];
		Span<byte> span = bytes;
		BinaryPrimitives.WriteUInt16LittleEndian(span, Format);
		BinaryPrimitives.WriteUInt16LittleEndian(span[2..], (ushort)Math.Min(seriesCount, ushort.MaxValue));
		BinaryPrimitives.WriteUInt32LittleEndian(span[4..], (uint)points);
		BinaryPrimitives.WriteDoubleLittleEndian(span[8..], data.XMin);
		BinaryPrimitives.WriteDoubleLittleEndian(span[16..], data.XMax);
		BinaryPrimitives.WriteDoubleLittleEndian(span[24..], version);

		int offset = HeaderLength;
		offset = WriteShares(span, offset, data.X.Span, points, true);
		foreach (ReadOnlyMemory<float> y in data.Y)
		{
			offset = WriteShares(span, offset, y.Span, points, false);
		}

		return bytes;
	}

	/// <summary>One share as the script reads it.</summary>
	/// <param name="share">The share, 0 to 1.</param>
	/// <param name="isX">An x outside the plot can't be pointed at, so it is a gap; a y outside it sits on the edge.</param>
	public static ushort Share(float share, bool isX)
	{
		if (!float.IsFinite(share))
		{
			return Gap;
		}

		if (isX && (share < -Slack || share > 1 + Slack))
		{
			return Gap;
		}

		return (ushort)Math.Round(Math.Clamp(share, 0, 1) * Scale);
	}

	private static int WriteShares(Span<byte> span, int offset, ReadOnlySpan<float> shares, int count, bool isX)
	{
		for (int i = 0; i < count; i++)
		{
			ushort value = i < shares.Length ? Share(shares[i], isX) : Gap;
			BinaryPrimitives.WriteUInt16LittleEndian(span[offset..], value);
			offset += 2;
		}

		return offset;
	}
}
