using Bunit;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Diagnostics.Base;
using Moka.Red.Diagnostics.Extensions;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Tests.Base;

public class DiagnosticComponentBaseTests : DevelopmentContext
{
	public DiagnosticComponentBaseTests()
	{
		Services.AddMokaDiagnostics();
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void RecordsRender_OnFirstRender()
	{
		IRenderedComponent<TestDiagComponent> cut = Render<TestDiagComponent>();

		IMokaDiagnosticsService service = Services.GetRequiredService<IMokaDiagnosticsService>();
		IReadOnlyList<ComponentRenderEntry> entries = service.GetRenderEntries();

		Assert.NotEmpty(entries);
		Assert.Contains(entries, e => e.ComponentType == "TestDiagComponent");
	}

	// Blazor doesn't ask ShouldRender before a first render, where the timing started, so every first render was
	// recorded as 0.0 ms.
	[Fact]
	public void FirstRender_IsTimed()
	{
		Render<TestDiagComponent>();

		ComponentRenderEntry entry = Services.GetRequiredService<IMokaDiagnosticsService>().GetRenderEntries()
			.Single(e => e.ComponentType == "TestDiagComponent");
		Assert.Equal(1, entry.RenderCount);
		Assert.True(entry.LastRenderDuration > TimeSpan.Zero);
	}

	[Fact]
	public void RecordsRender_OnSubsequentRenders()
	{
		IRenderedComponent<TestDiagComponent> cut = Render<TestDiagComponent>();

		// Force a re-render by changing a parameter
		cut.Render(parameters => parameters
			.Add(p => p.Class, "updated"));

		IMokaDiagnosticsService service = Services.GetRequiredService<IMokaDiagnosticsService>();
		IReadOnlyList<ComponentRenderEntry> entries = service.GetRenderEntries();
		ComponentRenderEntry entry = entries.First(e => e.ComponentType == "TestDiagComponent");

		Assert.True(entry.RenderCount >= 2);
	}

	[Fact]
	public async Task RecordsDisposal_WhenDisposed()
	{
		IRenderedComponent<TestDiagComponent> cut = Render<TestDiagComponent>();

		await cut.Instance.DisposeAsync();

		IMokaDiagnosticsService service = Services.GetRequiredService<IMokaDiagnosticsService>();
		Assert.Equal(1, service.DisposedComponentCount);
	}

	[Fact]
	public void RenderCount_IncrementsCorrectly()
	{
		IRenderedComponent<TestDiagComponent> cut = Render<TestDiagComponent>();

		IMokaDiagnosticsService service = Services.GetRequiredService<IMokaDiagnosticsService>();
		IReadOnlyList<ComponentRenderEntry> initialEntries = service.GetRenderEntries();
		int initialCount = initialEntries.First(e => e.ComponentType == "TestDiagComponent").RenderCount;

		// Force re-render via parameter change
		cut.Render(parameters => parameters
			.Add(p => p.Class, "change-1"));

		IReadOnlyList<ComponentRenderEntry> updatedEntries = service.GetRenderEntries();
		int updatedCount = updatedEntries.First(e => e.ComponentType == "TestDiagComponent").RenderCount;

		Assert.True(updatedCount > initialCount);
	}

	/// <summary>
	///     Minimal concrete component for testing DiagnosticComponentBase behavior.
	/// </summary>
	public sealed class TestDiagComponent : DiagnosticComponentBase
	{
		protected override string RootClass => "test-diag-component";

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			builder.OpenElement(0, "div");
			builder.AddAttribute(1, "class", CssClass);

			if (CssStyle is not null)
			{
				builder.AddAttribute(2, "style", CssStyle);
			}

			if (Id is not null)
			{
				builder.AddAttribute(3, "id", Id);
			}

			builder.AddMultipleAttributes(4, AdditionalAttributes);
			builder.CloseElement();
		}
	}
}
