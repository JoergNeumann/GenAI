using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Agents.AI;

var endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_ENDPOINT") ?? throw new InvalidOperationException("Die Umgebungsvariable AZURE_FOUNDRY_PROJECT_ENDPOINT ist nicht gesetzt.");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

const string JokerName = "Joker";
const string JokerInstructions = "Du bist gut darin, Witze zu erzählen.";

var persistentAgentsClient = new PersistentAgentsClient(
    endpoint, 
    new AzureCliCredential());

// serverseitigen Agent erstellen
var agentMetadata = await persistentAgentsClient.Administration.CreateAgentAsync(
    model: deploymentName,
    name: JokerName,
    instructions: JokerInstructions);

// Alternativ: bestehenden Agent abrufen
AIAgent agent1 = await persistentAgentsClient.GetAIAgentAsync(agentMetadata.Value.Id);

// Alternativ: Agent erstellen und zurückgeben
AIAgent agent2 = await persistentAgentsClient.CreateAIAgentAsync(
    model: deploymentName,
    name: JokerName,
    instructions: JokerInstructions);

AgentThread thread = agent1.GetNewThread();

var result = await agent1.RunAsync(
    "Erzähl mir einen Witz über einen Piraten.", 
    thread);

Console.WriteLine(result);

// Aufräumen.
await persistentAgentsClient.Administration.DeleteAgentAsync(agent1.Id);
await persistentAgentsClient.Administration.DeleteAgentAsync(agent2.Id);
