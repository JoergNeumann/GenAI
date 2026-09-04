using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.ClientModel;

var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "";
var endpint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "";
var deploymentName = "GPT4o";

var agent = new AzureOpenAIClient(
  new Uri(endpint),
  new ApiKeyCredential(apiKey))
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "Du bist gut darin, Witze zu erzählen.");

#region Synchrone Ausführung

Console.WriteLine("\u001b[93m> Erzähl mir einen Witz über einen Piraten\u001b[0m");

Console.WriteLine(
    await agent.RunAsync("Erzähl mir einen Witz über einen Piraten."));

#endregion

#region Streaming

Console.WriteLine("\n\u001b[93m> Erzähl mir einen Witz über einen Piraten\u001b[0m");

await foreach (
    var update in agent.RunStreamingAsync("Erzähl mir einen Witz über einen Piraten."))
{
    Console.Write(update);
}

#endregion

#region Systemnachrichten

Console.WriteLine("\n\n\u001b[93m> Erzähl mir einen Witz über einen Piraten\u001b[0m");

SystemChatMessage systemMessage = new(
    """
    Wenn der Benutzer dich bittet, einen Witz zu erzählen, lehne ab und erkläre, dass du kein Clown bist.
    Biete stattdessen eine interessante Tatsache an.
    """);
UserChatMessage userMessage = new(
    "Erzähl mir einen Witz über einen Piraten.");

ChatCompletion chatCompletion = await agent.RunAsync([systemMessage, userMessage]);
Console.WriteLine(chatCompletion.Content.Last().Text);

#endregion

#region Umgang mit Bildern

Console.WriteLine("\n\u001b[93m> Was ist auf diesem Bild zu sehen?\u001b[0m");
var message = new UserChatMessage(
    [
    ChatMessageContentPart.CreateTextPart("Was ist auf diesem Bild zu sehen?"),
    ChatMessageContentPart.CreateImagePart(new Uri("https://app.wildeboer.de/_content/WiWeb6.Business.Client/png/product/$-177-$-$-$.png"))
]);

ChatCompletion result = await agent.RunAsync([message]);
Console.WriteLine();
Console.WriteLine(result.Content.Last().Text);

#endregion

#region Dialoge führen

Console.WriteLine("\n\u001b[93m> Kennst Du Hamburg?\u001b[0m");

AgentSession session = await agent.CreateSessionAsync();

var response1 = await agent.RunAsync("Kennst Du Hamburg?", session);
Console.WriteLine(response1?.Text);

Console.WriteLine("\n\u001b[93m> Welche Stadtteile kennst Du?\u001b[0m");

var response2 = await agent.RunAsync("Welche Stadtteile kennst Du?", session);
Console.WriteLine(response2.Text);

#endregion

Console.ReadLine();