using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "";
var endpint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "";
var deploymentName = "GPT4o";

var agent = new AzureOpenAIClient(
  new Uri(endpint),
  new ApiKeyCredential(apiKey))
    .GetChatClient(deploymentName)
    .AsAIAgent(instructions: "Du bist ein Fremdenführer.");

AgentSession session = await agent.CreateSessionAsync();

// Dialog führen
Console.WriteLine("\u001b[93m> Kennst Du Hamburg?\u001b[0m");
Console.WriteLine(
    await agent.RunAsync("Kennst Du Hamburg?", session));

Console.WriteLine("\n\u001b[93m> Welche Stadtteile kennst Du?\u001b[0m");
Console.WriteLine(
    await agent.RunAsync("Welche Stadtteile kennst Du?", session));

// Session serialisieren und speichern
var serializedJson =
    (await agent.SerializeSessionAsync(session, JsonSerializerOptions.Web)).GetRawText();
    
var filePath = Path.Combine(Path.GetTempPath(), "agent_session.json");
await File.WriteAllTextAsync(filePath, serializedJson);

// gespeicherte Datei laden
string loadedJson = await File.ReadAllTextAsync(filePath);
JsonElement reloaded = JsonSerializer.Deserialize<JsonElement>(
    loadedJson, 
    JsonSerializerOptions.Web);

// Session wiederherstellen
var resumedSession = await agent.DeserializeSessionAsync(
    reloaded, 
    JsonSerializerOptions.Web);

// Dialog fortsetzen
Console.WriteLine("\n\u001b[93m> Wieviele Brücken gibt es in der Stadt?\u001b[0m");
Console.WriteLine(
    await agent.RunAsync("Wieviele Brücken gibt es in der Stadt?", resumedSession));

Console.ReadLine();