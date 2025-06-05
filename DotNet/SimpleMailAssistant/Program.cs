using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI.Assistants;
using SimpleMailAssistant;
using System.ClientModel;

#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0110, CS8600, CS8604, OPENAI001

#region Azure OpenAI

// Azure OpenAI
//string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
//string apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
//string modelName = "GPT4o";

//var client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

#endregion

// OpenAI
string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
string modelName = "gpt-4-turbo";

var client = OpenAIAssistantAgent.CreateOpenAIClient(new ApiKeyCredential(apiKey));
var assistantClient = client.GetAssistantClient();

AssistantCreationOptions assistantOptions = new()
{
    Name = "Mail-Assistent",
    Instructions = """
    Du bist ein freundlicher Assistent, der gerne den Regeln folgt.
    Du wirst die erforderlichen Schritte ausführen und um Genehmigung bitten, 
    bevor du irgendwelche weitreichenden Handlungen vornimmst. 
    Wenn der Benutzer nicht genügend Informationen bereitstellt, 
    um eine Aufgabe zu erledigen, wirst du weiterhin Fragen stellen, 
    bis du genügend Informationen hast, um die Aufgabe zu vervollständigen.
    """
};
var assistant = await assistantClient.CreateAssistantAsync(modelName, assistantOptions);

var agent = new OpenAIAssistantAgent(
    definition: assistant, 
    client: assistantClient,
    plugins: new List<KernelPlugin>()
    {
        KernelPluginFactory.CreateFromType<EmailPlugin>()
    });

AgentThread thread = new OpenAIAssistantAgentThread(assistantClient);

try
{
    await InvokeAgentAsync("Hallo!");
    await InvokeAgentAsync(
        "Schreibe meinem Chef Thomas eine E-Mail und bitte ihn um ein Gespräch in der nächsten Woche." +
        "Mir würde der 26.5.2025 um 9 Uhr oder der 27.5.2025 um 10:30 Uhr passen." +
        "Seine E-Mail-Adresse ist thomas.meyer@neogeeks.de. " +
        "Thema soll die Zeit- und Personalplanung des neuen IT-Projekts sein.");
    while (true)
    {

        await InvokeAgentAsync(Console.ReadLine());
    }
}
finally
{
    await assistantClient.DeleteThreadAsync(thread.Id);
    await assistantClient.DeleteAssistantAsync(agent.Id);
}

async Task InvokeAgentAsync(string input)
{
    ChatMessageContent message = new(AuthorRole.User, input);
    WriteAgentChatMessage(message);

    await foreach (ChatMessageContent response in agent.InvokeAsync(message, thread))
    {
        WriteAgentChatMessage(response);
    }
}

void WriteAgentChatMessage(ChatMessageContent message)
{
    string authorExpression = message.Role == AuthorRole.User ? string.Empty : $" - {message.AuthorName ?? "*"}";
    string contentExpression = message.Content ?? string.Empty;
    Console.WriteLine($"\n\u001b[32m# {message.Role}{authorExpression}:\u001b[37m{contentExpression}");
    Console.ResetColor();
}
