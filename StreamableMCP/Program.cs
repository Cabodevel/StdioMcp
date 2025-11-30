using ModelContextProtocol;

var builder = McpBuilder.Create();
await builder
    .UseStreamableHttp() 
    .Build()
    .RunAsync();
