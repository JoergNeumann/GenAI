using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using Pinecone;
using System.Text;

#pragma warning disable MEAI001

// vorher: Account und Index auf https://app.pinecone.io/ anlegen, API-Key und Index-Name in Umgebungsvariablen speichern
// Indextyp: text-embedding-3-small, Vector type: Dense, Dimension: 1024, Metric: cosine

string endpoint = GetRequired("AZURE_OPENAI_ENDPOINT");
string chatDeployment = GetRequired("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "GPT4o";
string embeddingDeployment = GetRequired("AZURE_OPENAI_EMBEDDING_DEPLOYMENT_NAME") ?? "text-embedding-3-small";

string pineconeApiKey = GetRequired("PINECONE_API_KEY");
string pineconeIndexName = "onbo-knowledge";
string pineconeNamespace = "__default__";

// Azure OpenAI Client
var azureOpenAi = new AzureOpenAIClient(
    new Uri(endpoint),
    new DefaultAzureCredential());

// Chat Client
var chatClient = azureOpenAi.GetChatClient(chatDeployment);

// Azure OpenAI Embeddings Client
EmbeddingClient embeddingClient = azureOpenAi.GetEmbeddingClient(embeddingDeployment);

// Pinecone .NET Client
var pinecone = new PineconeClient(pineconeApiKey);
var pineconeIndex = pinecone.Index(pineconeIndexName);

// 3. Agent mit TextSearchProvider bauen
TextSearchProviderOptions ragOptions = new()
{
    SearchTime = TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke,
    RecentMessageMemoryLimit = 4
};

AIAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    Name = "PolicyAgent",
    ChatOptions = new()
    {
        Instructions = """
        Du bist ein interner Assistent für Contoso.
        Beantworte Fragen nur auf Basis des bereitgestellten Kontexts.
        Wenn der Kontext keine ausreichende Antwort enthält, sage das offen.
        Zitiere nach Möglichkeit Titel und Link der Quelle.
        """
    },
    AIContextProviders = [new TextSearchProvider(SearchAdapterAsync, ragOptions)]
});

// 4. Fragen stellen
Console.WriteLine("Frag den Agenten etwas. Beispiel:");
Console.WriteLine("  - Wo kann ich Urlaub beantragen?");
Console.WriteLine("  - Kann ich aus dem Ausland arbeiten?");
Console.WriteLine("  - Wie läuft das mit Krankmeldungen?");
Console.WriteLine();

while (true)
{
    Console.Write("> ");
    var question = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(question) ||
        question.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    var answer = await agent.RunAsync(question);
    Console.WriteLine();
    Console.WriteLine(answer);
    Console.WriteLine();
}

// Such-Methode, die von TextSearchProvider verwendet wird
async Task<IEnumerable<TextSearchProvider.TextSearchResult>> SearchAdapterAsync(
    string query,
    CancellationToken cancellationToken)
{
    // 1. Embeddings für die Suchanfrage generieren
    var embeddingResult = await embeddingClient.GenerateEmbeddingAsync(query);
    var embedding = embeddingResult.Value;
    var vector = embedding.ToFloats().ToArray();

    // 2. Pinecone Suche
    var response = await pineconeIndex.QueryAsync(new QueryRequest
    {
        Namespace = pineconeNamespace,
        Vector = vector,
        TopK = 3,
        IncludeMetadata = true
    });

    var results = new List<TextSearchProvider.TextSearchResult>();

    if (response?.Matches == null)
        return results;

    // 3. Umwandeln in Agent Framework Suchergebnisse
    foreach (var match in response.Matches)
    {
        var metadata = match.Metadata;

        if (metadata == null)
            continue;

        string text = metadata.TryGetValue("text", out var t) ? t?.ToString() ?? "" : "";
        string title = metadata.TryGetValue("title", out var ti) ? ti?.ToString() ?? "" : "";
        string url = metadata.TryGetValue("url", out var u) ? u?.ToString() ?? "" : "";

        if (string.IsNullOrWhiteSpace(text))
            continue;

        results.Add(new TextSearchProvider.TextSearchResult
        {
            SourceName = title,
            SourceLink = url,
            Text = text
        });
    }

    return results;
}

static string GetRequired(string name) =>
    Environment.GetEnvironmentVariable(name)
    ?? throw new InvalidOperationException($"Umgebungsvariable '{name}' fehlt.");
