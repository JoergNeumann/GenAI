using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Responses;
using System.Text;

#pragma warning disable MEAI001, OPENAI001

var endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_ENDPOINT") ?? "";
var deploymentName = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_DEPLOYMENT_NAME") ?? "";

// AI Foundry Client erzeugen
AIProjectClient client = new(new Uri(endpoint), new AzureCliCredential()); // vorher mit `az login` anmelden

AIAgent agent = client.AsAIAgent(
    deploymentName,
    instructions: "Du bist ein persönlicher Assistent. ",
    name: "CoderAgent",
    tools: [new HostedCodeInterpreterTool() { Inputs = [] }]);

AgentResponse response = await agent.RunAsync("Erzeuge mir einen QR-Code für https://www.neogeeks.de.");

// Anweisungen und Code für den Code Interpreter ausgeben
CodeInterpreterToolCallContent? toolCallContent = response.Messages.SelectMany(m => m.Contents).OfType<CodeInterpreterToolCallContent>().FirstOrDefault();
if (toolCallContent?.Inputs is not null)
{
    DataContent? codeInput = toolCallContent.Inputs.OfType<DataContent>().FirstOrDefault();
    if (codeInput?.HasTopLevelMediaType("text") ?? false)
    {
        Console.WriteLine($"Code Input: {Encoding.UTF8.GetString(codeInput.Data.ToArray()) ?? "Not available"}");
    }
}

// Ergebnisse ausgeben
CodeInterpreterToolResultContent? toolResultContent = response.Messages.SelectMany(m => m.Contents).OfType<CodeInterpreterToolResultContent>().FirstOrDefault();
if (toolResultContent?.Outputs is not null && toolResultContent.Outputs.OfType<TextContent>().FirstOrDefault() is { } resultOutput)
{
    Console.WriteLine($"Code Tool Result: {resultOutput.Text}");
}

// Datei-Anmerkungen ausgeben (wenn vorhanden)
foreach (AIAnnotation annotation in response.Messages.SelectMany(m => m.Contents).SelectMany(C => C.Annotations ?? []))
{
    if (annotation.RawRepresentation is ContainerFileCitationMessageAnnotation annotationInfo)
    {
        Console.WriteLine($$"""
            File Id: {{annotationInfo.FileId}}
            Filename: {{Path.GetFileName(annotationInfo.Filename)}}
            """);
    }
}
Console.WriteLine(response.Text);
