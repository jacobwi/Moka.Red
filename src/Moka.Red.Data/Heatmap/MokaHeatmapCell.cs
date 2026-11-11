namespace Moka.Red.Data.Heatmap;

/// <summary>A cell of a <see cref="MokaHeatmap" /> grid, by its row and column, counting from 0.</summary>
/// <param name="Row">The row, its position in <c>GridValues</c>.</param>
/// <param name="Column">The column, its position in that row.</param>
public readonly record struct MokaHeatmapCell(int Row, int Column);
