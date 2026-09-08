using Azure;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using OpenAI.Chat;

string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT fehlt.");
string apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? throw new InvalidOperationException("AZURE_OPENAI_API_KEY fehlt.");
string deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME fehlt.");

// Skills Provider mit SKILL.md initialisieren
var skillsProvider = new AgentSkillsProvider(
    Path.Combine(AppContext.BaseDirectory, "skills"),
    fileOptions: new AgentFileSkillsSourceOptions
    {
        SearchDepth = 2,
    },
    options: new AgentSkillsProviderOptions
    {
        // Laden von Skill-Anweisungen und Lesen von Ressourcen ohne Rückfrage.
        DisableLoadSkillApproval = true,
        DisableReadSkillResourceApproval = true,
    });

// Agent erstellen und den Provider anhängen
AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey))
    .GetChatClient(deployment)
    .AsAIAgent(new ChatClientAgentOptions()
    {
        Name = "SkillsDemoAgent",
        ChatOptions = new()
        {
            Instructions = "Du bist ein hilfsbereiter Assistent. Antworte auf Deutsch.",
        },
        AIContextProviders = [skillsProvider],
    });

// Neue Session eröffnen
AgentSession session = await agent.CreateSessionAsync();

Console.WriteLine("Skills-Demo. Tipps:");
Console.WriteLine("- 'Ich war 2 Tage in Berlin, was kann ich als Verpflegung abrechnen?'");
Console.WriteLine("- 'Ich hatte einen Arbeitsunfall. An wen kann ich mich wenden?'");
Console.WriteLine("Leere Eingabe beendet.\n");

// Chat-Loop
while (true)
{
    Console.Write("\u001b[93m> ");
    string? input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) break;

    AgentResponse response = await agent.RunAsync(input, session);
    Console.WriteLine($"\n\u001b[0mAgent: {response.Text}\n");
}
