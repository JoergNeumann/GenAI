using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Samples;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;
using OpenAI.Chat;
using System.ClientModel;

var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "";
var endpint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "";
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "GPT4o";
var embeddingDeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT_NAME") ?? "Ada002";

var azureOpenAIClient = new AzureOpenAIClient(
  new Uri(endpint),
  new ApiKeyCredential(apiKey));

// Erstellt einen In-Memory-Vektorstore, der das Azure OpenAI Embedding-Modell zur Generierung von Embeddings verwendet.
VectorStore vectorStore = new InMemoryVectorStore(new()
{
    EmbeddingGenerator = azureOpenAIClient.GetEmbeddingClient(embeddingDeploymentName).AsIEmbeddingGenerator()
});

// Erstellt einen Store, der ein Speicherschema definiert und den Vektorstore zum Speichern und Abrufen von Dokumenten verwendet.
TextSearchStore textSearchStore = new(vectorStore, "product-and-policy-info", 3072);

// Lädt Beispiel-Dokumente in den Store hoch.
await textSearchStore.UpsertDocumentsAsync(GetSampleDocuments());

// Erstellt eine Adapterfunktion, die der TextSearchProvider verwenden kann, um Suchen im TextSearchStore auszuführen.
Func<string, CancellationToken, Task<IEnumerable<TextSearchProvider.TextSearchResult>>> SearchAdapter = async (text, ct) =>
{
    // Hier begrenzen wir die Suchergebnisse auf das jeweils oberste Ergebnis, um zu demonstrieren, dass wir
    // spezifische Suchergebnisse für jede Frage genau treffen; in einer realen Anwendung sollten jedoch mehrere Ergebnisse verwendet werden.
    var searchResults = await textSearchStore.SearchAsync(text, 1, ct);
    return searchResults.Select(r => new TextSearchProvider.TextSearchResult
    {
        SourceName = r.SourceName,
        SourceLink = r.SourceLink,
        Text = r.Text ?? string.Empty,
        RawRepresentation = r
    });
};

// Konfiguriert die Optionen für den TextSearchProvider.
TextSearchProviderOptions textSearchOptions = new()
{
    // Führt die Suche vor jedem Modellaufruf aus.
    SearchTime = TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke,
};

// Erstellt den KI-Agenten mit dem TextSearchProvider als AI-Kontextanbieter.
AIAgent agent = azureOpenAIClient
    .GetChatClient(deploymentName)
    .AsAIAgent(new ChatClientAgentOptions
    {
        ChatOptions = new() { Instructions = 
            """
            Du bist eine hilfreiche Support-Fachkraft für Contoso Outdoors. 
            Beantworte Fragen mithilfe des bereitgestellten Kontexts und
            gebe das Quelldokument an, wenn verfügbar.
            """ },
            
        AIContextProviders = [new TextSearchProvider(SearchAdapter, textSearchOptions)],

        // Da wir ChatCompletion verwenden, das den Chatverlauf lokal speichert, können wir außerdem einen Nachrichtenfilter hinzufügen,
        // der Nachrichten entfernt, die vom TextSearchProvider erzeugt wurden, bevor sie dem Chatverlauf hinzugefügt werden,
        // damit wir den Chatverlauf nicht mit allen Suchergebnissen aufblähen.
        // Standardmäßig speichert der Chatverlauf-Provider alle Nachrichten, außer denen, die ursprünglich aus dem Chatverlauf stammen.
        // Diese Ausnahme möchten wir auch hier beibehalten.
        ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions
        {
            StorageInputRequestMessageFilter = messages => messages
            .Where(m => m.GetAgentRequestMessageSourceType() != AgentRequestMessageSourceType.AIContextProvider &&
                        m.GetAgentRequestMessageSourceType() != AgentRequestMessageSourceType.ChatHistory)
        }),
    });

AgentSession session = await agent.CreateSessionAsync();

Console.WriteLine("\u001b[93m*** Frage zur Rückgabepolitik ***");
Console.WriteLine(">> Hallo! Ich benötige Hilfe, um die Rückgabebedingungen zu verstehen\n\u001b[0m");
Console.WriteLine(await agent.RunAsync("Hallo! Ich benötige Hilfe, um die Rückgabebedingungen zu verstehen.", session));

Console.WriteLine("\n\u001b[93m*** Frage zum Versand ***");
Console.WriteLine(">> Wie lange dauert der Standardversand in der Regel?\n\u001b[0m");
Console.WriteLine(await agent.RunAsync("Wie lange dauert der Standardversand in der Regel?", session));

Console.WriteLine("\n\u001b[93m*** Frage zur Produktpflege ***");
Console.WriteLine(">> Wie pflegt man das Zeltmaterial des TrailRunner am besten?\n\u001b[0m");
Console.WriteLine(await agent.RunAsync("Wie pflegt man das Zeltmaterial des TrailRunner am besten?", session));

// Erzeugt einige Beispiel-Suchdokumente.
// Jedes enthält einen Quellnamen und einen Link, die der Agent verwenden kann, um Quellen in seinen Antworten zu zitieren.
static IEnumerable<TextSearchDocument> GetSampleDocuments()
{
    yield return new TextSearchDocument
    {
        SourceId = "return-policy-001",
        SourceName = "Contoso Outdoors Return Policy",
        SourceLink = "https://contoso.com/policies/returns",
        Text = "Kunden können jeden Artikel innerhalb von 30 Tagen nach Lieferung zurückgeben. Artikel sollten unbenutzt sein und die Originalverpackung enthalten. Rückerstattungen werden innerhalb von 5 Werktagen nach Prüfung auf die ursprüngliche Zahlungsmethode ausgezahlt."
    };
    yield return new TextSearchDocument
    {
        SourceId = "shipping-guide-001",
        SourceName = "Contoso Outdoors Shipping Guide",
        SourceLink = "https://contoso.com/help/shipping",
        Text = "Standardversand ist bei Bestellungen über $50 kostenlos und trifft in der Regel innerhalb von 3–5 Werktagen im Festland der Vereinigten Staaten ein. Beschleunigte Versandoptionen sind beim Checkout verfügbar."
    };
    yield return new TextSearchDocument
    {
        SourceId = "tent-care-001",
        SourceName = "TrailRunner Tent Care Instructions",
        SourceLink = "https://contoso.com/manuals/trailrunner-tent",
        Text = "Reinigen Sie das Zeltgewebe mit lauwarmem Wasser und einer milden, nicht-waschmittelhaltigen Seife. Lassen Sie es vor der Lagerung vollständig an der Luft trocknen und vermeiden Sie längere UV-Einstrahlung, um die Lebensdauer der wasserdichten Beschichtung zu verlängern."
    };
}

Console.ReadLine();