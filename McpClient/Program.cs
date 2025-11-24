using Azure.AI.Inference;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>();

var mcpsProjectPath = Path.Combine(
    Directory.GetCurrentDirectory().Split("McpClient")[0],
    "Mcps",
    "Mcps.csproj"
);

var clientTransport = new StdioClientTransport(new()
{
    Name = "Demo Server",
    Command = "dotnet",
    Arguments = ["run", "--project", mcpsProjectPath],
});

await using var client = await McpClient.CreateAsync(clientTransport);

foreach(var tool in await client.ListToolsAsync())
{
    Console.WriteLine($"{tool.Name} ({tool.Description})");
}

var calculatorResult = await client.CallToolAsync(
    "add",
    new Dictionary<string, object?>() { ["a"] = 1, ["b"] = 3 },
    cancellationToken: CancellationToken.None);

Console.WriteLine(calculatorResult.Content.First(c => c.Type == "text").ToAIContent());

var dateResult = await client.CallToolAsync(
    "get_current_time",
    new Dictionary<string, object?>() { ["culture"] = "es-Es", ["format"] = "g", ["timeZone"] = "Europe/Madrid" },
    cancellationToken: CancellationToken.None);

Console.WriteLine(dateResult.Content.First(c => c.Type == "text").ToAIContent());

Console.ReadKey();

ChatCompletionsToolDefinition ConvertFrom(string name, string description, JsonElement jsonElement)
{
    // convert the tool to a function definition
    var functionDefinition = new FunctionDefinition(name)
    {
        Description = description,
        Parameters = BinaryData.FromObjectAsJson(new
        {
            Type = "object",
            Properties = jsonElement
        },
        new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
    };

    // create a tool definition
    var toolDefinition = new ChatCompletionsToolDefinition(functionDefinition);
    return toolDefinition;
}

async Task<List<ChatCompletionsToolDefinition>> GetMcpTools()
{
    Console.WriteLine("Listing tools");
    var tools = await client.ListToolsAsync();

    var toolDefinitions = new List<ChatCompletionsToolDefinition>();

    foreach(var tool in tools)
    {
        Console.WriteLine($"Connected to server with tools: {tool.Name}");
        Console.WriteLine($"Tool description: {tool.Description}");
        Console.WriteLine($"Tool parameters: {tool.JsonSchema}");

        JsonElement propertiesElement;
        tool.JsonSchema.TryGetProperty("properties", out propertiesElement);

        var def = ConvertFrom(tool.Name, tool.Description, propertiesElement);
        Console.WriteLine($"Tool definition: {def}");
        toolDefinitions.Add(def);

        Console.WriteLine($"Properties: {propertiesElement}");
    }

    return toolDefinitions;
}