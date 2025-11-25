using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Assistants;
using OpenAI.Responses;
using System.Text;

#pragma warning disable MEAI001, OPENAI001

var endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_ENDPOINT") ?? "";
var deploymentName = "gpt-4o";

// AI Foundry Client erzeugen
AIProjectClient client = new(new Uri(endpoint), new AzureCliCredential()); // vorher mit `az login` anmelden

// Agent mit Code Interpreter Tool erstellen
AIAgent agent = await client.CreateAIAgentAsync(
    name: "CoderAgent",
    creationOptions: new AgentVersionCreationOptions(
        new PromptAgentDefinition(model: deploymentName)
        {
            Instructions =
                "Du bist ein persönlicher Mathelehrer. " +
                "Wenn du nach einer Mathematikfrage gefragt wirst, " +
                "schreibe und führe Code mit dem Python‑Tool aus, " +
                "um die Frage zu beantworten.",
            Tools = {
                ResponseTool.CreateCodeInterpreterTool(
                    new CodeInterpreterToolContainer(
                        CodeInterpreterToolContainerConfiguration.CreateAutomaticContainerConfiguration(
                            fileIds: []) // optional: Dateien bereitstellen
                    )
                ),
            }
        })
);

// Prompt absetzen
AgentRunResponse response = await agent.RunAsync("Ich muss die Gleichung sin(x) + x^2 = 42 lösen");

// Anweisungen und Code für den Code Interpreter ausgeben
CodeInterpreterToolCallContent? toolCallContent =
    response.Messages.SelectMany(m => m.Contents).OfType<CodeInterpreterToolCallContent>().FirstOrDefault();
if (toolCallContent?.Inputs is not null)
{
    DataContent? codeInput = toolCallContent.Inputs.OfType<DataContent>().FirstOrDefault();
    if (codeInput?.HasTopLevelMediaType("text") ?? false)
    {
        Console.WriteLine($"Erzeugter Code:" + Environment.NewLine +
            Encoding.UTF8.GetString(codeInput.Data.ToArray()) ?? "Nicht verfügbar");
    }
}

// Ergebnisse ausgeben
CodeInterpreterToolResultContent? toolResultContent =
    response.Messages.SelectMany(m => m.Contents).OfType<CodeInterpreterToolResultContent>().FirstOrDefault();

if (toolResultContent?.Outputs is not null &&
    toolResultContent.Outputs.OfType<TextContent>().FirstOrDefault() is { } resultOutput)
{
    Console.WriteLine($"Ergebnis des Code Interpreter-Tools:" +
        resultOutput.Text);
}

// Annotations ausgeben (wenn vorhanden)
foreach (AIAnnotation annotation in response.Messages.SelectMany(m => m.Contents).SelectMany(C => C.Annotations ?? []))
{
    if (annotation.RawRepresentation is TextAnnotationUpdate citationAnnotation)
    {
        Console.WriteLine($$"""
            File Id: {{citationAnnotation.OutputFileId}}
            Text to Replace: {{citationAnnotation.TextToReplace}}
            Filename: {{Path.GetFileName(citationAnnotation.TextToReplace)}}
            """);
    }
}
// Agenten wieder löschen
await client.Agents.DeleteAgentAsync(agent.Name);
