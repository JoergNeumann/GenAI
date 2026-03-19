using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "";
var endpint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "";
var deploymentName = "GPT4o";

AIAgent agent = new AzureOpenAIClient(
    new Uri(endpint),
    new ApiKeyCredential(apiKey))
        .GetChatClient(deploymentName)
        .AsAIAgent(new ChatClientAgentOptions()
        {
            Name = "HelpfulAssistant",
            ChatOptions = new ChatOptions()
            {
                Instructions = "Du bist ein hilfreicher Assistent.",
                ResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat.ForJsonSchema<PersonInfo>()
            }
        });

var response = await agent.RunAsync(
    "Bitte gib Informationen über John Smith, der ein 35-jähriger Softwareentwickler ist.");
var personInfo = JsonSerializer.Deserialize<PersonInfo>(response.Text, JsonSerializerOptions.Web)!;// response.Deserialize<PersonInfo>(JsonSerializerOptions.Web);
Console.WriteLine($"Name: {personInfo.Name}, Alter: {personInfo.Age}, Beruf: {personInfo.Occupation}");

//Alternativ: Streaming - Antwort
//IAsyncEnumerable<AgentResponseUpdate> updates = agent.RunStreamingAsync("Bitte gib Informationen über John Smith, der ein 35-jähriger Softwareentwickler ist.");
//AgentResponse nonGenericResponse = await updates.ToAgentResponseAsync();
//PersonInfo personInfo2 = JsonSerializer.Deserialize<PersonInfo>(nonGenericResponse.Text, JsonSerializerOptions.Web)!;
//Console.WriteLine($"Name: {personInfo2.Name}, Alter: {personInfo2.Age}, Beruf: {personInfo2.Occupation}");


