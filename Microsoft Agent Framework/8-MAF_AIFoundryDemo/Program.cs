using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;

var endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_ENDPOINT") ?? throw new InvalidOperationException("Die Umgebungsvariable AZURE_FOUNDRY_PROJECT_ENDPOINT ist nicht gesetzt.");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

AIProjectClient aiProjectClient = new(new Uri(endpoint), new AzureCliCredential());

ProjectsAgentVersion agentVersion = await aiProjectClient.AgentAdministrationClient.CreateAgentVersionAsync(
    "JokerAgent",
    new ProjectsAgentVersionCreationOptions(
        new DeclarativeAgentDefinition(model: deploymentName)
        {
            Instructions = "Du bist gut im Witze erzählen.",
        }));

var agent = aiProjectClient.AsAIAgent(agentVersion);

Console.WriteLine("\u001b[93m> Erzähl mir einen Witz über einen Piraten\u001b[0m");

Console.WriteLine(
    await agent.RunAsync("Erzähl mir eine Witz über einen Piraten."));

await aiProjectClient.AgentAdministrationClient.DeleteAgentAsync(agent.Name);

Console.ReadLine();