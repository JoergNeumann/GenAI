using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Assistants;
using OpenAI.Files;
using Azure.AI.Projects.OpenAI;

#pragma warning disable OPENAI001

string endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_ENDPOINT") ?? throw new InvalidOperationException("AZURE_FOUNDRY_PROJECT_ENDPOINT is not set.");
string deploymentName = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_DEPLOYMENT_NAME") ?? "gpt-5.4-mini";

const string AgentInstructions = "Du bist ein hilfreicher Assistent, der in hochgeladenen Dateien nach Antworten auf Fragen suchen kann.";

AIProjectClient aiProjectClient = new(new Uri(endpoint), new DefaultAzureCredential());
var projectOpenAIClient = aiProjectClient.GetProjectOpenAIClient(new ProjectOpenAIClientOptions());
var filesClient = projectOpenAIClient.GetProjectFilesClient();
var vectorStoresClient = projectOpenAIClient.GetProjectVectorStoresClient();

// 1. Erstelle eine temporäre Datei mit Testinhalt und lade sie hoch.
string searchFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + "_lookup.txt");
File.WriteAllText(
    path: searchFilePath,
    contents: """
        Mitarbeiterverzeichnis:
        - Alice Johnson, 28 Jahre alt, Softwareentwicklerin, Abteilung Entwicklung
        - Bob Smith, 35 Jahre alt, Vertriebsleiter, Vertriebsabteilung
        - Carol Williams, 42 Jahre alt, Personalleiterin, Personalabteilung
        - David Brown, 31 Jahre alt, Leiter Kundenservice, Support-Abteilung
        """
);

Console.WriteLine($"Datei wird hochgeladen: {searchFilePath}");
OpenAIFile uploadedFile = filesClient.UploadFile(
    filePath: searchFilePath,
    purpose: FileUploadPurpose.Assistants
);
Console.WriteLine($"Hochgeladene Datei, File ID: {uploadedFile.Id}");

// 2. Erstelle einen Vektorspeicher mit der hochgeladenen Datei.
var vectorStoreResult = await vectorStoresClient.CreateVectorStoreAsync(
    options: new() { FileIds = { uploadedFile.Id }, Name = "EmployeeDirectory_VectorStore" }
);
string vectorStoreId = vectorStoreResult.Value.Id;
Console.WriteLine($"Erstelle vector store, vector store ID: {vectorStoreId}");

/// Erstelle einen AIAgent mit HostedFileSearchTool.
AIAgent agent = aiProjectClient.AsAIAgent(deploymentName,
    instructions: AgentInstructions,
    name: "FileSearchAgent-RAPI",
    description: "Ein Agent, der in hochgeladenen Dateien nach Antworten auf Fragen suchen kann.",
    tools: [new HostedFileSearchTool() { Inputs = [new HostedVectorStoreContent(vectorStoreId)] }]);

// Agent ausführen
Console.WriteLine("\n--- Starte File Search Agent ---");
Console.WriteLine("\u001b[93m> Wer ist der jüngste Angestellte?\u001b[0m");

AgentResponse response = await agent.RunAsync("Wer ist der jüngste Angestellte?");
Console.WriteLine($"Antwort: {response}");

// Alle vom Tool generierten Dateizitationsanmerkungen abrufen
foreach (AIAnnotation annotation in response.Messages.SelectMany(m => m.Contents).SelectMany(c => c.Annotations ?? []))
{
    if (annotation.RawRepresentation is TextAnnotationUpdate citationAnnotation)
    {
        Console.WriteLine($$"""
            File Citation:
              File Id: {{citationAnnotation.OutputFileId}}
              Text to Replace: {{citationAnnotation.TextToReplace}}
            """);
    }
}

// Dateiresourcen bereinigen.
Console.WriteLine("\n--- Cleanup ---");
await vectorStoresClient.DeleteVectorStoreAsync(vectorStoreId);
await filesClient.DeleteFileAsync(uploadedFile.Id);
File.Delete(searchFilePath);
Console.WriteLine("Erfolgreich aufgeräumt.");

Console.ReadLine();