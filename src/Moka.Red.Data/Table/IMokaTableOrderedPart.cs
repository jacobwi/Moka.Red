namespace Moka.Red.Data.Table;

// A column or a heading group, which the table and a group list by its Order.
internal interface IMokaTableOrderedPart
{
	int Order { get; }

	static int OrderOf(object part) => part is IMokaTableOrderedPart ordered ? ordered.Order : 0;
}
