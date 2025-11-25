using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.ComponentModel;

#pragma warning disable MEAI001

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
    .CreateAIAgent(
        name: "WeatherAgent",
        instructions: "Du beantwortest Fragen zum Wetter.",
        description: "Ein Agent, der Fragen zum Wetter beantwortet.",
        tools: [AIFunctionFactory.Create(GetWeather)]);

Console.WriteLine(await weatherAgent.RunAsync("Wie ist das Wetter in Amsterdam?"));

#endregion

#region Agent als Function Tool verwenden

AIAgent mainAgent = new AzureOpenAIClient(
    new Uri(endpint),
    new ApiKeyCredential(apiKey))
    .GetChatClient(deploymentName)
    .CreateAIAgent(instructions: "Du bist ein hilfreicher Assistent, der auf Französisch antwortet.",
    tools: [weatherAgent.AsAIFunction()]);

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
     .CreateAIAgent(instructions: "Du bist ein hilfreicher Assistent.",
     tools: [approvalRequiredWeatherFunction]);

AgentThread thread = hybridAgent.GetNewThread();
AgentRunResponse response = await hybridAgent.RunAsync("Wie ist das Wetter in Amsterdam?", thread);

var functionApprovalRequests = response.Messages
    .SelectMany(x => x.Contents)
    .OfType<FunctionApprovalRequestContent>()
    .ToList();

FunctionApprovalRequestContent requestContent = 
    functionApprovalRequests.First();

Console.WriteLine(
    $"Wir benötigen eine Freigabe zur Ausführung " +
    $"'{requestContent.FunctionCall.Name}'");

Console.ReadLine();

var approvalMessage = new ChatMessage(
    ChatRole.User, 
    [requestContent.CreateResponse(true)]);

var result = await hybridAgent.RunAsync(
    approvalMessage,
    thread);

Console.WriteLine(result);

#endregion
