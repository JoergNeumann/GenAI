using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using OpenAI;
using System.ClientModel;

#pragma warning disable MEAI001

// Ausgaben über STDIO unterdrücken
Console.SetOut(TextWriter.Null);
Console.SetError(TextWriter.Null);

var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "";
var endpint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "";
var deploymentName = "GPT4o";

var agent = new AzureOpenAIClient(
  new Uri(endpint),
  new ApiKeyCredential(apiKey))
    .GetChatClient(deploymentName)
    .CreateAIAgent(
        name: "Joker",
        instructions: "Du bist gut darin, Witze zu erzählen.");

McpServerTool tool = McpServerTool.Create(agent.AsAIFunction());
HostApplicationBuilder builder =
    Host.CreateEmptyApplicationBuilder(settings: null);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools([tool]);

await builder.Build().RunAsync();


// In VSCode einfügen:
// -- in GitHub Copilot Chat:
// -- Shift+STRG+P
// -- MCP: Add Server...
// Command (stdio): dotnet run --project C:\\MeinVerzeichnis\\MAF_MCPDemo.csproj
// global