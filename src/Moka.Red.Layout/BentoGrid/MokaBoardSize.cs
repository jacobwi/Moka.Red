namespace Moka.Red.Layout.BentoGrid;

/// <summary>How much of a board's row a <see cref="MokaBoardCard" /> takes.</summary>
public enum MokaBoardSize
{
	/// <summary>One column of four. A whole row on a phone.</summary>
	Small,

	/// <summary>Two columns of four. A whole row below 1180 px.</summary>
	Wide,

	/// <summary>A whole row.</summary>
	Full
}
