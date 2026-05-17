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
            Instructions = "You are good at telling jokes.",
        }));

var agent = aiProjectClient.AsAIAgent(agentVersion);

Console.WriteLine(await agent.RunAsync("Tell me a joke about a pirate."));

await aiProjectClient.AgentAdministrationClient.DeleteAgentAsync(agent.Name);
