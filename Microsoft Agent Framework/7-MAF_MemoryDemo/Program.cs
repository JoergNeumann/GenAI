using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Text.Json;

#pragma warning disable MEAI001

var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "";
var endpint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "";
var deploymentName = "GPT4o";

var agent = new AzureOpenAIClient(
  new Uri(endpint),
  new ApiKeyCredential(apiKey))
    .GetChatClient(deploymentName)
    .CreateAIAgent(instructions: "Du bist ein Fremdenführer.");

AgentThread thread = agent.GetNewThread();

// Dialog führen
Console.WriteLine(
    await agent.RunAsync("Kennst Du Hamburg?", 
    thread));
Console.WriteLine(
    await agent.RunAsync("Welche Stadtteile kennst Du?",
    thread));

// Thread serialisieren und speichern
var serializedJson = thread.Serialize(JsonSerializerOptions.Web)
    .GetRawText();
var filePath = Path.Combine(Path.GetTempPath(), "agent_thread.json");
await File.WriteAllTextAsync(filePath, serializedJson);

// gespeicherte Datei laden
string loadedJson = await File.ReadAllTextAsync(filePath);
JsonElement reloaded = JsonSerializer.Deserialize<JsonElement>(
    loadedJson, 
    JsonSerializerOptions.Web);

// Thread wiederherstellen
AgentThread resumedThread = agent.DeserializeThread(
    reloaded, 
    JsonSerializerOptions.Web);

// Dialog fortsetzen
var result = await agent.RunAsync(
    "Wieviele Brücken gibt es in der Stadt?",
    resumedThread);

Console.WriteLine(result);
