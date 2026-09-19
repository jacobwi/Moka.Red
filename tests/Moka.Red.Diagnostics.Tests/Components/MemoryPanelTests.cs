using Bunit;
using Moka.Red.Diagnostics.Components.Panels;

namespace Moka.Red.Diagnostics.Tests.Components;

// "Total Memory" and "Managed Heap" both showed GC.GetTotalMemory, so the panel had one number twice.
public class MemoryPanelTests : BunitContext
{
	[Fact]
	public void Cards_ShowTheAllocatedBytesAndTheHeapOfTheLastCollection()
	{
		// A collection between the render and the reads below would move the heap figure, so a run
		// that saw one is retried.
		for (int attempt = 0; attempt < 5; attempt++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			// Allocated after the collection, so it counts toward the allocated bytes and not the heap
			// the last collection left.
			byte[] ballast = new byte[16 * 1024 * 1024];
			int collections = GC.CollectionCount(0);

			IRenderedComponent<MemoryPanel> cut = Render<MemoryPanel>();
			long heapSize = GC.GetGCMemoryInfo().HeapSizeBytes;
			GC.KeepAlive(ballast);

			if (GC.CollectionCount(0) != collections)
			{
				continue;
			}

			string[] values = cut.FindAll(".moka-diag-memory-card-value").Select(v => v.TextContent.Trim()).ToArray();
			Assert.Equal(FormatBytes(heapSize), values[1]);
			Assert.NotEqual(values[0], values[1]);
			return;
		}

		Assert.Fail("A garbage collection ran during every attempt.");
	}

	// Same formatting as the panel.
	private static string FormatBytes(long bytes) => bytes switch
	{
		>= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F2} GB",
		>= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB",
		>= 1_024 => $"{bytes / 1_024.0:F0} KB",
		_ => $"{bytes} B"
	};
}
