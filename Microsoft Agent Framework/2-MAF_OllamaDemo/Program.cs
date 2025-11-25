using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

var endpoint = "http://localhost:11434/";
var modelName = "llama3.2:latest";// "deepseek-r1";

AIAgent agent = new OllamaApiClient(new Uri(endpoint), modelName)
    .CreateAIAgent(
        instructions: "Du bist gut darin, Witze zu erzählen.", 
        name: "Joker");

var result = await agent.RunAsync("Erzähl mir einen Witz über einen Piraten.");
Console.WriteLine(result);
