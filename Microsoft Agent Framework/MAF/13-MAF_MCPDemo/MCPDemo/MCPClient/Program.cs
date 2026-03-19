using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;
using OpenAI.Chat;

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("Die Umgebungsvariable OPENAI_API_KEY ist nicht gesetzt.");
var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";

// MCP-Server via stdio starten
await using var mcpClient = await ModelContextProtocol.Client.McpClient.CreateAsync(
    new StdioClientTransport(new()
    {
        Name = "WeatherMcpServer",
        Command = "dotnet",
        Arguments =
        [
            "run",
            "--project",
            "../../../../McpDemo/McpServer.csproj"
        ]
    }));

// Verfügbare MCP-Tools laden
var mcpTools = await mcpClient.ListToolsAsync().ConfigureAwait(false);

// OpenAI-ChatClient erzeugen
AIAgent agent = new OpenAIClient(apiKey)
    .GetChatClient(model)
    .AsAIAgent(
    name: "WeatherAgent",
    instructions: """
        Du bist ein hilfreicher Assistent.
        Wenn der Nutzer nach Wetter in einer Stadt fragt,
        verwende das MCP-Tool.
        Antworte kurz und auf Deutsch.
        """,
    tools: [.. mcpTools.Cast<AITool>()]
);

// Testlauf
var prompt = "Wie ist das Wetter in Berlin?";
var result = await agent.RunAsync(prompt);

Console.WriteLine(result);