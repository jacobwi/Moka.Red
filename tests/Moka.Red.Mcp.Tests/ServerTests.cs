using System.IO.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Moka.Red.Mcp.Tests;

// The server as an MCP client sees it, over the same stream transport stdio uses, wired up in process.
public sealed class ServerTests : IAsyncLifetime
{
	private readonly AnonymousPipeServerStream _toServer = new(PipeDirection.Out);
	private readonly AnonymousPipeServerStream _toClient = new(PipeDirection.Out);
	private AnonymousPipeClientStream? _serverInput;
	private AnonymousPipeClientStream? _clientInput;
	private IHost? _host;
	private McpClient? _client;

	private McpClient Client => _client ?? throw new InvalidOperationException("The client isn't connected.");

	public async ValueTask InitializeAsync()
	{
		_serverInput = new AnonymousPipeClientStream(PipeDirection.In, _toServer.ClientSafePipeHandle);
		_clientInput = new AnonymousPipeClientStream(PipeDirection.In, _toClient.ClientSafePipeHandle);

		HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings: null);
		builder.Services.AddMokaRedMcp().WithStreamServerTransport(_serverInput, _toClient);
		_host = builder.Build();
		await _host.StartAsync(TestContext.Current.CancellationToken);

		_client = await McpClient.CreateAsync(
			new StreamClientTransport(_toServer, _clientInput),
			cancellationToken: TestContext.Current.CancellationToken);
	}

	public async ValueTask DisposeAsync()
	{
		if (_client is not null)
		{
			await _client.DisposeAsync();
		}

		if (_host is not null)
		{
			await _host.StopAsync(CancellationToken.None);
			_host.Dispose();
		}

		await _toServer.DisposeAsync();
		await _toClient.DisposeAsync();
		if (_serverInput is not null)
		{
			await _serverInput.DisposeAsync();
		}

		if (_clientInput is not null)
		{
			await _clientInput.DisposeAsync();
		}
	}

	[Fact]
	public async Task OffersReadOnlyTools()
	{
		IList<McpClientTool> tools = await Client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

		Assert.Equal(
			["get_component", "get_component_docs", "get_getting_started", "get_type", "list_components", "search_components"],
			tools.Select(tool => tool.Name).Order(StringComparer.Ordinal));
		Assert.All(tools, tool => Assert.True(tool.ProtocolTool.Annotations?.ReadOnlyHint));
	}

	[Fact]
	public void IntroducesItselfWithTheLibraryVersion()
	{
		Implementation? server = Client.ServerInfo;

		Assert.NotNull(server);
		Assert.Equal("moka-red", server.Name);
		Assert.Equal(MokaRedMcpServer.LibraryVersion, server.Version);
		Assert.Contains("get_component", Client.ServerInstructions, StringComparison.Ordinal);
	}

	[Fact]
	public async Task GetComponent_GivesParametersDefaultsAndEnumValues()
	{
		string text = await Call("get_component", new() { ["name"] = "MokaButton" });

		Assert.StartsWith("# MokaButton", text, StringComparison.Ordinal);
		Assert.Contains("| `Loading` | `bool` | `false` |", text, StringComparison.Ordinal);
		Assert.Contains("| `Variant` | `MokaVariant` | `MokaVariant.Filled` |", text, StringComparison.Ordinal);
		Assert.Contains("- `MokaVariant`: Filled, Outlined, Text, Soft", text, StringComparison.Ordinal);
		Assert.Contains("Also from `MokaComponentBase`:", text, StringComparison.Ordinal);
	}

	[Fact]
	public async Task GetComponent_PointsAtTheTypesItsParametersTake()
	{
		string text = await Call("get_component", new() { ["name"] = "MokaLineChart" });

		Assert.Contains("`MokaLineSeries`", text, StringComparison.Ordinal);
	}

	[Fact]
	public async Task GetComponent_SuggestsTheClosestNames()
	{
		string text = await Call("get_component", new() { ["name"] = "Buton" });

		Assert.Contains("Closest: MokaButton", text, StringComparison.Ordinal);
	}

	[Fact]
	public async Task SearchComponents_FindsAComponentByWhatItDoes()
	{
		string text = await Call("search_components", new() { ["query"] = "date range" });

		Assert.StartsWith("- MokaDateRangePicker (Forms)", text, StringComparison.Ordinal);
	}

	[Fact]
	public async Task SearchComponents_NamesTheServicesAndPagesOfAFeature()
	{
		string text = await Call("search_components", new() { ["query"] = "toast" });

		Assert.Contains("`IMokaToastService`", text, StringComparison.Ordinal);
		Assert.Contains("MokaToastHost", text, StringComparison.Ordinal);
	}

	[Fact]
	public async Task ListComponents_FiltersByPackage()
	{
		string text = await Call("list_components", new() { ["package"] = "Forms" });

		Assert.Contains("## Forms (", text, StringComparison.Ordinal);
		Assert.DoesNotContain("## Data (", text, StringComparison.Ordinal);
	}

	[Fact]
	public async Task GetComponentDocs_GivesOneSection()
	{
		string text = await Call("get_component_docs", new() { ["name"] = "MokaButton", ["section"] = "Parameters" });

		Assert.StartsWith("## Parameters", text, StringComparison.Ordinal);
		Assert.Contains("### MokaButtonGroup", text, StringComparison.Ordinal);
	}

	[Fact]
	public async Task GetType_DescribesARecordAComponentTakes()
	{
		string text = await Call("get_type", new() { ["name"] = "MokaLineSeries" });

		Assert.StartsWith("# MokaLineSeries (record)", text, StringComparison.Ordinal);
		Assert.Contains("| `Values` (init) | `IReadOnlyList<double>` |", text, StringComparison.Ordinal);
	}

	[Fact]
	public async Task GetType_ListsTheIconsOfASet()
	{
		string text = await Call("get_type", new() { ["name"] = "MokaIcons.Action" });

		Assert.Contains("Save, Delete", text, StringComparison.Ordinal);
	}

	[Fact]
	public async Task GetGettingStarted_StartsWithInstallationAndNamesTheOtherTopics()
	{
		string text = await Call("get_getting_started", []);

		Assert.Contains("Other topics:", text, StringComparison.Ordinal);
		Assert.Contains("imports", text, StringComparison.Ordinal);
	}

	private async Task<string> Call(string tool, Dictionary<string, object?> arguments)
	{
		CallToolResult result = await Client.CallToolAsync(tool, arguments, cancellationToken: TestContext.Current.CancellationToken);

		Assert.NotEqual(true, result.IsError);
		return string.Concat(result.Content.OfType<TextContentBlock>().Select(block => block.Text));
	}
}
