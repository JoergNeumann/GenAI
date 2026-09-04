using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

var endpoint = "http://localhost:11434/";
var modelName = "llama3.2:latest";// "deepseek-r1";

AIAgent agent = new OllamaApiClient(new Uri(endpoint), modelName)
    .AsAIAgent(
        instructions: "Du bist gut darin, Witze zu erzählen.", 
        name: "Joker");

Console.WriteLine("\u001b[93m> Erzähl mir einen Witz über einen Piraten\u001b[0m");

var result = await agent.RunAsync("Erzähl mir einen Witz über einen Piraten");

Console.WriteLine(result);
Console.ReadLine();