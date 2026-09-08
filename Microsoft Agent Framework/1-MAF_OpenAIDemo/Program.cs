using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;

#pragma warning disable OPENAI001

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("Die Umgebungsvariable OPENAI_API_KEY ist nicht gesetzt.");
var modelName = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";

// Chat Completions API verwenden
AIAgent agent = new OpenAIClient(apiKey)
    .GetChatClient(modelName)
    .AsAIAgent(
        instructions: "Du bist gut darin, Witze zu erzählen.",
        name: "Joker");

// Alternativ: Responses API verwenden
//AIAgent agent = new OpenAIClient(apiKey)
//    .GetResponsesClient()
//    .AsAIAgent(
//        modelName,
//        instructions: "Du bist gut darin, Witze zu erzählen.",
//        name: "Joker");

Console.WriteLine("\u001b[93m> Erzähl mir einen Witz über einen Piraten\u001b[0m");
UserChatMessage chatMessage = 
    new("Erzähl mir einen Witz über einen Piraten");

ChatCompletion chatCompletion = 
    await agent.RunAsync(new[] { chatMessage });

// Nicht-Streaming-Beispiel
Console.WriteLine(chatCompletion.Content.Last().Text);

// Streaming-Beispiel
//AsyncCollectionResult<StreamingChatCompletionUpdate> completionUpdates = 
//    agent.RunStreamingAsync(new[] { chatMessage });
//await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
//{
//    if (completionUpdate.ContentUpdate.Count >0)
//    {
//        Console.Write(completionUpdate.ContentUpdate[0].Text);
//    }
//}

Console.ReadLine();