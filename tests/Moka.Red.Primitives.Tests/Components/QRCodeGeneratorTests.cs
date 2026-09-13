using Moka.Red.Core.Enums;
using Moka.Red.Primitives.QRCode;

namespace Moka.Red.Primitives.Tests.Components;

public class QRCodeGeneratorTests
{
	[Theory]
	[InlineData(MokaQRErrorCorrection.Low)]
	[InlineData(MokaQRErrorCorrection.Medium)]
	[InlineData(MokaQRErrorCorrection.Quartile)]
	[InlineData(MokaQRErrorCorrection.High)]
	public void Generate_ProducesASquareGridForEveryEcLevel(MokaQRErrorCorrection ec)
	{
		bool[][] grid = QRCodeGenerator.Generate("MOKA", ec);

		Assert.NotEmpty(grid);
		Assert.All(grid, row => Assert.Equal(grid.Length, row.Length));
	}

	[Fact]
	public void Generate_SizeFollowsTheVersionFormula()
	{
		bool[][] grid = QRCodeGenerator.Generate("MOKA", MokaQRErrorCorrection.Low);

		// size = 17 + version * 4, versions 1 to 10 give 21 through 57.
		Assert.InRange(grid.Length, 21, 57);
		Assert.Equal(0, (grid.Length - 17) % 4);
	}

	[Fact]
	public void Generate_PlacesTheThreeFinderPatterns()
	{
		bool[][] g = QRCodeGenerator.Generate("MOKA", MokaQRErrorCorrection.Low);
		int n = g.Length;

		// A finder is a 7x7 dark ring with a 3x3 dark core.
		AssertFinderAt(g, 0, 0);
		AssertFinderAt(g, 0, n - 7);
		AssertFinderAt(g, n - 7, 0);
	}

	[Fact]
	public void Generate_SetsTheDarkModule()
	{
		bool[][] g = QRCodeGenerator.Generate("MOKA", MokaQRErrorCorrection.Low);
		int version = (g.Length - 17) / 4;

		Assert.True(g[(4 * version) + 9][8]);
	}

	[Fact]
	public void Generate_IsDeterministic()
	{
		bool[][] a = QRCodeGenerator.Generate("https://example.com", MokaQRErrorCorrection.Medium);
		bool[][] b = QRCodeGenerator.Generate("https://example.com", MokaQRErrorCorrection.Medium);

		Assert.Equal(a, b);
	}

	[Fact]
	public void Generate_HigherErrorCorrectionNeedsAtLeastAsMuchSpace()
	{
		string payload = new('A', 60);

		bool[][] low = QRCodeGenerator.Generate(payload, MokaQRErrorCorrection.Low);
		bool[][] high = QRCodeGenerator.Generate(payload, MokaQRErrorCorrection.High);

		Assert.True(high.Length >= low.Length);
	}

	[Fact]
	public void Generate_ThrowsWhenDataExceedsVersion10()
	{
		string tooLong = new('A', 5000);

		Assert.Throws<ArgumentException>(() => QRCodeGenerator.Generate(tooLong, MokaQRErrorCorrection.High));
	}

	private static void AssertFinderAt(bool[][] g, int row, int col)
	{
		for (int i = 0; i < 7; i++)
		{
			Assert.True(g[row][col + i]);
			Assert.True(g[row + 6][col + i]);
			Assert.True(g[row + i][col]);
			Assert.True(g[row + i][col + 6]);
		}

		for (int r = 2; r < 5; r++)
		{
			for (int c = 2; c < 5; c++)
			{
				Assert.True(g[row + r][col + c]);
			}
		}
	}
}
