using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI.Assistants;
using OpenAI.Files;
using System.ClientModel;
using System.Reflection;

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

var fileClient = client.GetOpenAIFileClient();
await using var stream = ReadStream("NeoGeeks AGB 01-2024.pdf")!;
OpenAIFile fileInfo = await fileClient.UploadFileAsync(stream, "NeoGeeks AGB 01-2024.pdf", FileUploadPurpose.Assistants);

string vectorStoreId = await client.CreateVectorStoreAsync(
    [fileInfo.Id],
    waitUntilCompleted: true);

AssistantCreationOptions assistantOptions = new()
{
    Name = "Legal Assistant",
    Instructions = "Du bist ein Assistent, der rechtliche Fragen zu Verträgen beantwortet.",
    Tools = { new FileSearchToolDefinition() },
    ToolResources = new()
    {
        FileSearch = new()
        {
            VectorStoreIds = { vectorStoreId },
        }
    },
};
var assistant = await assistantClient.CreateAssistantAsync(modelName, assistantOptions);
var agent = new OpenAIAssistantAgent(assistant, assistantClient);

AgentThread thread = new OpenAIAssistantAgentThread(assistantClient);

try
{
    await InvokeAgentAsync("Was für ein Zahlungsziel hat NeoGeeks?");
    await InvokeAgentAsync("Wie hoch haftet NeoGeeks bei Sach- und Vermögensschäden?");
    await InvokeAgentAsync("Erkläre das Change-Request-Verfahren von NeoGeeks");
}
finally
{
    await thread.DeleteAsync();
    await assistantClient.DeleteAssistantAsync(agent.Id);
    await client.DeleteVectorStoreAsync(vectorStoreId);
    await client.DeleteFileAsync(fileInfo.Id);
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
    string contentExpression = string.IsNullOrWhiteSpace(message.Content) ? string.Empty : message.Content;
    bool isCode = message.Metadata?.ContainsKey(OpenAIAssistantAgent.CodeInterpreterMetadataKey) ?? false;
    if (isCode) Console.ForegroundColor = ConsoleColor.Yellow;
    string codeMarker = isCode ? "\n\x1b[33m[CODE]\n" : " ";
    if (contentExpression.IndexOf("【")>-1) contentExpression = contentExpression.Substring(0, contentExpression.IndexOf("【"));
    Console.WriteLine($"\n\u001b[32m# {message.Role}{authorExpression}:\u001b[37m{codeMarker}{contentExpression}");
    Console.ResetColor();
    Console.ForegroundColor = ConsoleColor.Yellow;

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
    Console.ResetColor();
}

static Stream? ReadStream(string fileName)
{
    var info = Assembly.GetExecutingAssembly().GetName();
    var name = info.Name;
    return Assembly
        .GetExecutingAssembly()
        .GetManifestResourceStream($"{name}.Resources." + fileName)!;
}
