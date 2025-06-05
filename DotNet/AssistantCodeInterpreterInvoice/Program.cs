using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI.Assistants;
using OpenAI.Files;
using System.ClientModel;
using System.Reflection;

#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0110, CS8600, CS8604, CS8600, OPENAI001

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

var fileClient = client.GetOpenAIFileClient();
await using var stream = ReadStream("R240004.pdf")!;
OpenAIFile fileInfo = await fileClient.UploadFileAsync(stream, "R240004.pdf", FileUploadPurpose.Assistants);

AssistantCreationOptions assistantOptions = new()
{
    Name = "Document Data Analyser",
    Instructions = "Du bist ein Assistent, der Rechnungen analysiert.",
    Tools = { new CodeInterpreterToolDefinition() },
    ToolResources = new ToolResources()
    {
        CodeInterpreter = new()
        {
            FileIds = { fileInfo.Id }
        }
    }
};
var assistant = await assistantClient.CreateAssistantAsync(modelName, assistantOptions);
var agent = new OpenAIAssistantAgent(assistant, assistantClient);

AgentThread thread = new OpenAIAssistantAgentThread(assistantClient);

try
{
    await InvokeAgentAsync("Bitte extrahiere die Rechnungsnummer und den Bruttobetrag aus der hochgeladenen PDF-Datei und gebe sie im JSON-Format zurück.");
    //await InvokeAgentAsync("Bitte extrahiere die Rechnungsnummer und den Bruttobetrag aus der hochgeladenen PDF-Datei unter Verwendung von Optical Character Recognition und gebe sie im JSON-Format zurück.");
}
finally
{
    await assistantClient.DeleteThreadAsync(thread.Id);
    await assistantClient.DeleteAssistantAsync(agent.Id);
    await client.DeleteFileAsync(fileInfo.Id);
}
Console.ReadLine();

async Task InvokeAgentAsync(string input)
{
    ChatMessageContent message = new(AuthorRole.User, input);
    WriteAgentChatMessage(message);

    await foreach (ChatMessageContent response in agent.InvokeAsync(message, thread))
    {
        WriteAgentChatMessage(response);
    }
}

/// <summary>
/// Common method to write formatted agent chat content to the console.
/// </summary>
void WriteAgentChatMessage(ChatMessageContent message)
{
    // Include ChatMessageContent.AuthorName in output, if present.
    string authorExpression = message.Role == AuthorRole.User ? string.Empty : $" - {message.AuthorName ?? "*"}";
    // Include TextContent (via ChatMessageContent.Content), if present.
    string contentExpression = string.IsNullOrWhiteSpace(message.Content) ? string.Empty : message.Content;
    bool isCode = message.Metadata?.ContainsKey(OpenAIAssistantAgent.CodeInterpreterMetadataKey) ?? false;
    if (isCode) Console.ForegroundColor = ConsoleColor.Yellow;
    string codeMarker = isCode ? "\n\x1b[33m[CODE]\n" : " ";
    Console.WriteLine($"\n\u001b[32m# {message.Role}{authorExpression}:\u001b[37m{codeMarker}{contentExpression}");
    Console.ResetColor();

    // Provide visibility for inner content (that isn't TextContent).
    foreach (KernelContent item in message.Items)
    {
        if (item is AnnotationContent annotation)
        {
            Console.WriteLine($"  [{item.GetType().Name}] {annotation.Label}: File #{annotation.ReferenceId}");
        }
        else if (item is FileReferenceContent fileReference)
        {
            Console.WriteLine($"  [{item.GetType().Name}] File #{fileReference.FileId}");
        }
        else if (item is ImageContent image)
        {
            Console.WriteLine($"  [{item.GetType().Name}] {image.Uri?.ToString() ?? image.DataUri ?? $"{image.Data?.Length} bytes"}");
        }
        else if (item is FunctionCallContent functionCall)
        {
            Console.WriteLine($"  [{item.GetType().Name}] {functionCall.Id}");
        }
        else if (item is FunctionResultContent functionResult)
        {
            Console.WriteLine($"  [{item.GetType().Name}] {functionResult.CallId}");
        }
    }
}

static Stream? ReadStream(string fileName)
{
    var info = Assembly.GetExecutingAssembly().GetName();
    var name = info.Name;
    return Assembly
        .GetExecutingAssembly()
        .GetManifestResourceStream($"{name}.Resources." + fileName)!;
}