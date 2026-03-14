using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.ComponentModel;

#pragma warning disable OPENAI001

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("Die Umgebungsvariable OPENAI_API_KEY ist nicht gesetzt.");
var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";

[Description("Gibt das Wetter für einen angegebenen Ort zurück.")]
static string GetWeather([Description("Der Ort, für den das Wetter abgerufen werden soll.")] string location)
    => $"Das Wetter in {location} ist bewölkt mit einer Höchsttemperatur von 15°C.";

AIAgent agent = new OpenAIClient(apiKey)
    .GetChatClient(model)             // Chat Completions API verwenden
    //.GetOpenAIResponseClient(model) // Alternativ: Responses API verwenden
    .CreateAIAgent(
        instructions: "Du bist gut darin, Witze zu erzählen.", 
        name: "Joker",
        tools: [AIFunctionFactory.Create(GetWeather)]);

UserChatMessage chatMessage = 
    new("Wie ist gerade das Wetter in Amsterdam?");

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
        Console.Write(completionUpdate.ContentUpdate[0].Text);
    }
}
