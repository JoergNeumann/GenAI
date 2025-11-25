using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Text.Json;

#pragma warning disable MEAI001

var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "";
var endpint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "";
var deploymentName = "GPT4o";

JsonElement schema = AIJsonUtilities.CreateJsonSchema(typeof(PersonInfo));

ChatOptions chatOptions = new()
{
    ResponseFormat = ChatResponseFormat.ForJsonSchema(
        schema: schema,
        schemaName: "PersonInfo",
        schemaDescription: "Informationen über eine Person, einschließlich ihres Namens, Alters und Berufs")
};

AIAgent agent = new AzureOpenAIClient(
    new Uri(endpint),
    new ApiKeyCredential(apiKey))
        .GetChatClient(deploymentName)
        .CreateAIAgent(new ChatClientAgentOptions()
        {
            Name = "HelpfulAssistant",
            Instructions = "Du bist ein hilfreicher Assistent.",
            ChatOptions = chatOptions
        });

var response = await agent.RunAsync(
    "Bitte gib Informationen über John Smith, der ein 35-jähriger Softwareentwickler ist.");
var personInfo = response.Deserialize<PersonInfo>(JsonSerializerOptions.Web);

//Alternativ: Streaming - Antwort
//var updates = agent.RunStreamingAsync("Bitte gib Informationen über John Smith, der ein 35-jähriger Softwareentwickler ist.");
//var personInfo = (await updates.ToAgentRunResponseAsync()).Deserialize<PersonInfo>(JsonSerializerOptions.Web);

Console.WriteLine($"Name: {personInfo.Name}, Alter: {personInfo.Age}, Beruf: {personInfo.Occupation}");
