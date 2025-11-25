using System.ClientModel;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;

#pragma warning disable OPENAI001

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("Die Umgebungsvariable OPENAI_API_KEY ist nicht gesetzt.");
var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";

AIAgent agent = new OpenAIClient(apiKey)
    .GetChatClient(model)             // Chat Completions API verwenden
    //.GetOpenAIResponseClient(model) // Alternativ: Responses API verwenden
    .CreateAIAgent(
        instructions: "Du bist gut darin, Witze zu erzählen.", 
        name: "Joker");

UserChatMessage chatMessage = 
    new("Erzähl mir einen Witz über einen Piraten.");

ChatCompletion chatCompletion = 
    await agent.RunAsync(new[] { chatMessage });

// Nicht-Streaming-Beispiel
Console.WriteLine(chatCompletion.Content.Last().Text);

// Streaming-Beispiel
AsyncCollectionResult<StreamingChatCompletionUpdate> completionUpdates = 
    agent.RunStreamingAsync(new[] { chatMessage });
await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
{
    if (completionUpdate.ContentUpdate.Count >0)
    {
        Console.WriteLine(completionUpdate.ContentUpdate[0].Text);
    }
}
