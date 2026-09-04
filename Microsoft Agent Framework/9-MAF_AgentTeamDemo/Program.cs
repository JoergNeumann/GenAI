using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.ClientModel;
using System.ComponentModel;

var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "";
var endpint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "";
var deploymentName = "GPT4o";

#region Funktionen als Tools einbinden

[Description("Gibt das Wetter für einen angegebenen Ort zurück.")]
static string GetWeather([Description("Der Ort, für den das Wetter abgerufen werden soll.")] string location)
    => $"Das Wetter in {location} ist bewölkt mit einer Höchsttemperatur von 15°C.";

AIAgent weatherAgent = new AzureOpenAIClient(
    new Uri(endpint),
    new ApiKeyCredential(apiKey))
    .GetChatClient(deploymentName)
    .AsAIAgent(
        name: "WeatherAgent",
        instructions: "Du beantwortest Fragen zum Wetter.",
        description: "Ein Agent, der Fragen zum Wetter beantwortet.",
        tools: [AIFunctionFactory.Create(GetWeather)]);

Console.WriteLine("\u001b[93m> Wie ist das Wetter in Amsterdam?\u001b[0m");

Console.WriteLine(await weatherAgent.RunAsync("Wie ist das Wetter in Amsterdam?"));

#endregion

#region Agent als Function Tool verwenden

AIAgent mainAgent = new AzureOpenAIClient(
    new Uri(endpint),
    new ApiKeyCredential(apiKey))
    .GetChatClient(deploymentName)
    .AsAIAgent(instructions: "Du bist ein hilfreicher Assistent, der auf Französisch antwortet.",
    tools: [weatherAgent.AsAIFunction()]);

Console.WriteLine("\n\u001b[93m> Wie ist das Wetter in Amsterdam?\u001b[0m");

Console.WriteLine(await mainAgent.RunAsync("Wie ist das Wetter in Amsterdam?"));

#endregion

#region Human Workflows

AIFunction weatherFunction = AIFunctionFactory.Create(GetWeather);
AIFunction approvalRequiredWeatherFunction =
    new ApprovalRequiredAIFunction(weatherFunction);

AIAgent hybridAgent = new AzureOpenAIClient(
    new Uri(endpint),
    new ApiKeyCredential(apiKey))
     .GetChatClient(deploymentName)
     .AsAIAgent(
        instructions: "Du bist ein hilfreicher Assistent.",
        tools: [approvalRequiredWeatherFunction]);

AgentSession session = await hybridAgent.CreateSessionAsync();
var response = await hybridAgent.RunAsync(
    "Wie ist das Wetter in Amsterdam?", session);

var functionApprovalRequests = response.Messages
    .SelectMany(x => x.Contents)
    .OfType<ToolApprovalRequestContent>()
    .ToList();

ToolApprovalRequestContent requestContent =
    functionApprovalRequests.First();

var functionCall = (FunctionCallContent)requestContent.ToolCall;

Console.WriteLine(
    $"\n\u001b[93m> Wir benötigen eine Freigabe zur Ausführung " +
    $"'{functionCall.Name}'\u001b[0m");

Console.ReadLine();
Console.WriteLine($"\u001b[32m*** Freigabe erteilt ***\u001b[0m");

var approvalMessage = new Microsoft.Extensions.AI.ChatMessage(
    ChatRole.User,
    [requestContent.CreateResponse(true)]);

var result = await hybridAgent.RunAsync(
    approvalMessage,
    session);

Console.WriteLine(result);

Console.ReadLine();

#endregion
