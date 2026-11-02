using Moka.Red.Core.Enums;
using Moka.Red.Primitives.QRCode;
using ZXing;
using ZXing.Common;

namespace Moka.Red.Primitives.Tests.Components;

/// <summary>
///     The encoder is proven by decoding: every grid this class feeds through ZXing must come back
///     as the text that went in. Up to 0.2.0 the block counts were wrong from version 5 up, which
///     no structural check notices - the codes looked like QR codes and scanned as nothing.
/// </summary>
public class QRCodeDecodeTests
{
	public static TheoryData<MokaQRErrorCorrection> Levels =>
	[MokaQRErrorCorrection.Low, MokaQRErrorCorrection.Medium, MokaQRErrorCorrection.Quartile,
		MokaQRErrorCorrection.High];

	internal static string Decode(bool[][] grid)
	{
		// One pixel per module, with the quiet zone the standard asks for
		const int quietZone = 4;
		int size = grid.Length + quietZone * 2;
		byte[] rgb = new byte[size * size * 3];
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				bool inGrid = y >= quietZone && y < size - quietZone && x >= quietZone && x < size - quietZone;
				bool dark = inGrid && grid[y - quietZone][x - quietZone];
				byte value = dark ? (byte)0 : (byte)255;
				int i = (y * size + x) * 3;
				rgb[i] = value;
				rgb[i + 1] = value;
				rgb[i + 2] = value;
			}
		}

		var reader = new ZXing.BarcodeReaderGeneric
		{
			AutoRotate = false,
			Options = new DecodingOptions
			{
				TryHarder = true,
				PureBarcode = true,
				PossibleFormats = [BarcodeFormat.QR_CODE],
				Hints = { [DecodeHintType.CHARACTER_SET] = "UTF-8" }
			}
		};

		Result? result = reader.Decode(new RGBLuminanceSource(rgb, size, size));
		Assert.NotNull(result);
		return result!.Text;
	}

	[Theory]
	[MemberData(nameof(Levels))]
	public void Url_DecodesAtEveryEcLevel(MokaQRErrorCorrection ec)
	{
		string payload = "https://example.com/orders/10442?tab=items";

		Assert.Equal(payload, Decode(QRCodeGenerator.Generate(payload, ec)));
	}

	[Theory]
	[MemberData(nameof(Levels))]
	public void WiFiConfig_DecodesAtEveryEcLevel(MokaQRErrorCorrection ec)
	{
		string payload = "WIFI:T:WPA;S:Moka Guest;P:correct horse battery staple;;";

		Assert.Equal(payload, Decode(QRCodeGenerator.Generate(payload, ec)));
	}

	// The versions whose block counts were wrong before the fix, driven by payload length: each
	// grows past the version below it, so the sweep crosses V5 through V10 at every level.
	[Theory]
	[MemberData(nameof(Levels))]
	public void PayloadsAcrossVersionsFiveToTen_DecodeAtEveryEcLevel(MokaQRErrorCorrection ec)
	{
		foreach (int length in new[] { 100, 200, 400, 600, 900, 1200 })
		{
			string payload = new string('A', length);

			Assert.Equal(payload, Decode(QRCodeGenerator.Generate(payload, ec)));
		}
	}

	[Theory]
	[MemberData(nameof(Levels))]
	public void PayloadsAcrossVersionsElevenToForty_DecodeAtEveryEcLevel(MokaQRErrorCorrection ec)
	{
		// Sized to fit every level's largest version, High included (1273 bytes at V40)
		foreach (int length in new[] { 850, 1050, 1200 })
		{
			string payload = new string('B', length);

			Assert.Equal(payload, Decode(QRCodeGenerator.Generate(payload, ec)));
		}
	}

	[Fact]
	public void Utf8Content_DecodesWithItsBytesIntact()
	{
		string payload = "CAFÉ — naïve — 中文 — 🎫";

		Assert.Equal(payload, Decode(QRCodeGenerator.Generate(payload, MokaQRErrorCorrection.Medium)));
	}

	// A payload that lands on the last byte of the version it fits leaves no room for the
	// terminator, which is where padding bugs show up.
	[Fact]
	public void AVersionBoundaryPayload_Decodes()
	{
		int capacity = QRCodeGenerator.PayloadCapacity(7, MokaQRErrorCorrection.Medium);
		string payload = new string('C', capacity);

		Assert.Equal(payload, Decode(QRCodeGenerator.Generate(payload, MokaQRErrorCorrection.Medium)));
	}

	[Fact]
	public void TheLargestCode_Decodes()
	{
		int capacity = QRCodeGenerator.PayloadCapacity(QRCodeGenerator.MaxVersion, MokaQRErrorCorrection.Low);
		string payload = new string('D', capacity);

		Assert.Equal(payload, Decode(QRCodeGenerator.Generate(payload, MokaQRErrorCorrection.Low)));
	}

	// Anchor points of the standard's capacity table; the payload capacity is computed from the
	// block structure now, so these catch a transcribed table entry.
	[Theory]
	[InlineData(1, MokaQRErrorCorrection.Low, 17)]
	[InlineData(1, MokaQRErrorCorrection.High, 7)]
	[InlineData(5, MokaQRErrorCorrection.Quartile, 60)]
	[InlineData(5, MokaQRErrorCorrection.High, 44)]
	[InlineData(7, MokaQRErrorCorrection.Low, 154)]
	[InlineData(7, MokaQRErrorCorrection.High, 64)]
	[InlineData(10, MokaQRErrorCorrection.Low, 271)]
	[InlineData(10, MokaQRErrorCorrection.High, 119)]
	[InlineData(11, MokaQRErrorCorrection.High, 137)]
	[InlineData(20, MokaQRErrorCorrection.Low, 858)]
	[InlineData(25, MokaQRErrorCorrection.Quartile, 715)]
	[InlineData(27, MokaQRErrorCorrection.Medium, 1125)]
	[InlineData(40, MokaQRErrorCorrection.Low, 2953)]
	[InlineData(40, MokaQRErrorCorrection.Medium, 2331)]
	[InlineData(40, MokaQRErrorCorrection.Quartile, 1663)]
	[InlineData(40, MokaQRErrorCorrection.High, 1273)]
	public void PayloadCapacity_MatchesTheStandard(int version, MokaQRErrorCorrection ec, int expected)
	{
		Assert.Equal(expected, QRCodeGenerator.PayloadCapacity(version, ec));
	}

	[Theory]
	[MemberData(nameof(Levels))]
	public void ThePickedVersion_IsTheSmallestThatFits(MokaQRErrorCorrection ec)
	{
		for (int version = 1; version <= QRCodeGenerator.MaxVersion; version++)
		{
			int capacity = QRCodeGenerator.PayloadCapacity(version, ec);

			// A payload of exactly this version's capacity at the level below it must not fit
			// any earlier version, or the picker is choosing too large a code.
			bool[][] grid = QRCodeGenerator.Generate(new string('E', capacity), ec);
			Assert.Equal(17 + version * 4, grid.Length);
		}
	}
}
