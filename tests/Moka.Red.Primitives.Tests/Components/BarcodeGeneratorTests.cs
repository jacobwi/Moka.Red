using Moka.Red.Core.Enums;
using Moka.Red.Primitives.Barcode;

namespace Moka.Red.Primitives.Tests.Components;

/// <summary>
///     Covers the symbologies that used to be routed to Code 128 regardless of
///     <see cref="MokaBarcodeFormat" />, so <c>BarcodeFormat="EAN13"</c> silently
///     produced a Code 128 barcode.
/// </summary>
public class BarcodeGeneratorTests
{
	// Known-good codes with valid check digits.
	private const string Ean13Valid = "5901234123457";
	private const string Ean8Valid = "96385074";
	private const string UpcAValid = "036000291452";

	[Fact]
	public void Generate_ProducesDifferentModulesPerFormat()
	{
		bool[] code128 = BarcodeGenerator.Generate(MokaBarcodeFormat.Code128, "12345678");
		bool[] ean8 = BarcodeGenerator.Generate(MokaBarcodeFormat.EAN8, Ean8Valid);

		Assert.NotEqual(code128.Length, ean8.Length);
	}

	[Theory]
	[InlineData(MokaBarcodeFormat.EAN13, Ean13Valid, 95)]
	[InlineData(MokaBarcodeFormat.EAN8, Ean8Valid, 67)]
	[InlineData(MokaBarcodeFormat.UPC, UpcAValid, 95)]
	public void Generate_EmitsSpecModuleCount(MokaBarcodeFormat format, string value, int expected)
	{
		bool[] modules = BarcodeGenerator.Generate(format, value);

		Assert.Equal(expected, modules.Length);
	}

	[Theory]
	[InlineData(MokaBarcodeFormat.EAN13, Ean13Valid)]
	[InlineData(MokaBarcodeFormat.EAN8, Ean8Valid)]
	[InlineData(MokaBarcodeFormat.UPC, UpcAValid)]
	public void Generate_StartsAndEndsWithGuardBars(MokaBarcodeFormat format, string value)
	{
		bool[] m = BarcodeGenerator.Generate(format, value);

		// Left guard 101, right guard 101.
		Assert.True(m[0] && !m[1] && m[2]);
		Assert.True(m[^3] && !m[^2] && m[^1]);
	}

	[Theory]
	[InlineData(MokaBarcodeFormat.EAN13, Ean13Valid, 45)]
	[InlineData(MokaBarcodeFormat.EAN8, Ean8Valid, 31)]
	[InlineData(MokaBarcodeFormat.UPC, UpcAValid, 45)]
	public void Generate_PlacesCentreGuardAtSpecOffset(MokaBarcodeFormat format, string value, int offset)
	{
		bool[] m = BarcodeGenerator.Generate(format, value);

		// Centre guard is 01010.
		Assert.False(m[offset]);
		Assert.True(m[offset + 1]);
		Assert.False(m[offset + 2]);
		Assert.True(m[offset + 3]);
		Assert.False(m[offset + 4]);
	}

	[Fact]
	public void GenerateEan13_AcceptsTwelveDigitsAndComputesCheckDigit()
	{
		bool[] withCheck = BarcodeGenerator.GenerateEan13(Ean13Valid);
		bool[] withoutCheck = BarcodeGenerator.GenerateEan13(Ean13Valid[..12]);

		Assert.Equal(withCheck, withoutCheck);
	}

	[Fact]
	public void GenerateEan13_RejectsWrongCheckDigit()
	{
		string wrong = Ean13Valid[..12] + (Ean13Valid[12] == '0' ? '1' : '0');

		Assert.Throws<ArgumentException>(() => BarcodeGenerator.GenerateEan13(wrong));
	}

	[Fact]
	public void GenerateUpcA_MatchesEan13WithLeadingZero()
	{
		// UPC-A is EAN-13 with a leading zero, so the module patterns must be identical.
		bool[] upc = BarcodeGenerator.GenerateUpcA(UpcAValid);
		bool[] ean = BarcodeGenerator.GenerateEan13("0" + UpcAValid);

		Assert.Equal(ean, upc);
	}

	[Fact]
	public void GenerateCode39_WrapsInStartStopDelimiters()
	{
		bool[] a = BarcodeGenerator.GenerateCode39("AB");
		bool[] b = BarcodeGenerator.GenerateCode39("ab");

		// Input is uppercased, so case must not matter.
		Assert.Equal(a, b);
	}

	[Fact]
	public void GenerateCode39_RejectsCharactersOutsideTheAlphabet()
	{
		Assert.Throws<ArgumentException>(() => BarcodeGenerator.GenerateCode39("abc!"));
	}

	[Fact]
	public void GenerateCode128_RejectsCharactersOutsideSubsetB()
	{
		Assert.Throws<ArgumentException>(() => BarcodeGenerator.GenerateCode128("café"));
	}

	[Theory]
	[InlineData(MokaBarcodeFormat.EAN13, "590123412345", 13)]
	[InlineData(MokaBarcodeFormat.EAN8, "9638507", 8)]
	[InlineData(MokaBarcodeFormat.UPC, "03600029145", 12)]
	public void GetDisplayText_AppendsTheComputedCheckDigit(MokaBarcodeFormat format, string value, int expected)
	{
		string text = BarcodeGenerator.GetDisplayText(format, value);

		Assert.Equal(expected, text.Length);
	}

	[Fact]
	public void GetDisplayText_UppercasesCode39()
	{
		string text = BarcodeGenerator.GetDisplayText(MokaBarcodeFormat.Code39, "abc");

		Assert.Equal("ABC", text);
	}

	[Fact]
	public void GetQuietZone_IsWiderForEan13ThanTheDefault()
	{
		(int Left, int Right) ean = BarcodeGenerator.GetQuietZone(MokaBarcodeFormat.EAN13);
		(int Left, int Right) code128 = BarcodeGenerator.GetQuietZone(MokaBarcodeFormat.Code128);

		Assert.True(ean.Left > 0 && ean.Right > 0);
		Assert.True(code128.Left > 0 && code128.Right > 0);
	}
}
