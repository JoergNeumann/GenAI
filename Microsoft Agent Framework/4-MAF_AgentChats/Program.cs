using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

#pragma warning disable MEAI001

var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "";
var endpint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "";
var deploymentName = "GPT4o";

var agent = new AzureOpenAIClient(
  new Uri(endpint),
  new ApiKeyCredential(apiKey))
    .GetChatClient(deploymentName)
    .CreateAIAgent(
        instructions: "Du bist gut darin, Witze zu erzählen.");

#region Systemnachrichten

ChatMessage systemMessage = new(
    ChatRole.System,
    """
    Wenn der Benutzer dich bittet, einen Witz zu erzählen, lehne ab und erkläre, dass du kein Clown bist.
    Biete stattdessen eine interessante Tatsache an.
    """);
ChatMessage userMessage = new(
    ChatRole.User,
    "Erzähl mir einen Witz über einen Piraten.");

Console.WriteLine(
    await agent.RunAsync([systemMessage, userMessage]));

#endregion

#region Umgang mit Bildern

//ChatMessage message = new(
//    ChatRole.User, [
//        new TextContent("Beschreibe das folgende Bild?"),
//        new UriContent("https://upload.wikimedia.org/wikipedia/commons/thumb/d/dd/Gfp-wisconsin-madison-the-nature-boardwalk.jpg/2560px-Gfp-wisconsin-madison-the-nature-boardwalk.jpg", "image/jpeg")
//]);

//Console.WriteLine(await agent.RunAsync(message));

#endregion

#region Dialoge führen

//AgentThread thread = agent.GetNewThread();
//Console.WriteLine(
//    await agent.RunAsync("Kennst Du Hamburg?", thread));
//Console.WriteLine(
//    await agent.RunAsync("Welche Stadtteile kennst Du?", thread));

#endregion
