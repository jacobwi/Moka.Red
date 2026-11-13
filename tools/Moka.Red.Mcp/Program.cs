using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moka.Red.Mcp;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// stdout carries the protocol, so every log line has to go to stderr.
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services
	.AddMokaRedMcp()
	.WithStdioServerTransport();

await builder.Build().RunAsync();
