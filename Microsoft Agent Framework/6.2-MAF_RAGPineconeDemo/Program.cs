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
string pineconeIndexName = GetRequired("PINECONE_INDEX_NAME") ?? "test";
string pineconeNamespace = Environment.GetEnvironmentVariable("PINECONE_NAMESPACE") ?? "docs";

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

// 1. Demo Content erstellen
var documents = new[]
{
    new SourceDocument(
        "handbook-1",
        "Contoso Travel Policy",
        "https://contoso.local/policies/travel",
        """
        Geschäftsreisen müssen vorab von der Teamleitung genehmigt werden.
        Hotelkosten bis 180 EUR pro Nacht sind ohne Zusatzfreigabe erlaubt.
        Bahnreisen sind gegenüber Inlandsflügen zu bevorzugen.
        Bewirtungskosten müssen mit Beleg eingereicht werden.
        """
    ),
    new SourceDocument(
        "handbook-2",
        "Contoso Laptop Policy",
        "https://contoso.local/policies/laptops",
        """
        Mitarbeitende erhalten standardmäßig ein Business-Laptop-Modell.
        Der Austauschzyklus beträgt 36 Monate.
        Defekte Geräte werden über den IT-Service-Desk gemeldet.
        Private Software darf nur installiert werden, wenn sie freigegeben ist.
        """
    ),
    new SourceDocument(
        "handbook-3",
        "Contoso Remote Work Policy",
        "https://contoso.local/policies/remote-work",
        """
        Remote Work ist an bis zu drei Tagen pro Woche möglich.
        Mitarbeitende müssen während der Kernzeit von 10 bis 15 Uhr erreichbar sein.
        Für Arbeiten im Ausland ist vorab eine Freigabe von HR erforderlich.
        """
    )
};

// 2. Texte chunken, in Vektoren umwandeln und in Pinecone speichern
Console.WriteLine("Indexiere Demo-Dokumente in Pinecone ...");

var chunks = documents
    .SelectMany(d => ChunkDocument(d, maxChunkLength: 220))
    .ToList();

var vectors = new List<Vector>();

foreach (var chunk in chunks)
{
    float[] embedding = await CreateEmbeddingAsync(embeddingClient, chunk.Text);

    vectors.Add(new Vector
    {
        Id = chunk.Id,
        Values = embedding,
        Metadata = new Metadata
        {
            ["docId"] = chunk.DocumentId,
            ["title"] = chunk.Title,
            ["url"] = chunk.Url,
            ["text"] = chunk.Text
        }
    });
}

await pineconeIndex.UpsertAsync(new UpsertRequest
{
    Namespace = pineconeNamespace,
    Vectors = vectors
});

Console.WriteLine($"Fertig. {vectors.Count} Chunks wurden gespeichert.");
Console.WriteLine();

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
Console.WriteLine("  - Wie hoch darf ein Hotel bei Dienstreisen kosten?");
Console.WriteLine("  - Wie oft ist Remote Work erlaubt?");
Console.WriteLine("  - Wann wird ein Laptop ersetzt?");
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
    float[] queryEmbedding = await CreateEmbeddingAsync(embeddingClient, query);

    var response = await pineconeIndex.QueryAsync(new QueryRequest
    {
        Namespace = pineconeNamespace,
        Vector = queryEmbedding,
        TopK = 3,
        IncludeMetadata = true,
        IncludeValues = false
    });

    var results = new List<TextSearchProvider.TextSearchResult>();

    if (response?.Matches is null)
    {
        return results;
    }

    foreach (var match in response.Matches)
    {
        string title = GetMetadataString(match.Metadata, "title") ?? "Unbekannte Quelle";
        string url = GetMetadataString(match.Metadata, "url") ?? "";
        string text = GetMetadataString(match.Metadata, "text") ?? "";

        if (!string.IsNullOrWhiteSpace(text))
        {
            results.Add(new TextSearchProvider.TextSearchResult
            {
                SourceName = title,
                SourceLink = url,
                Text = text
            });
        }
    }

    return results;
}

// Hilfsfunktionen
static async Task<float[]> CreateEmbeddingAsync(EmbeddingClient client, string text)
{
    OpenAIEmbedding embedding = await client.GenerateEmbeddingAsync(text);
    return embedding.ToFloats().ToArray();
}

static IEnumerable<DocumentChunk> ChunkDocument(SourceDocument doc, int maxChunkLength)
{
    var sentences = doc.Content
        .Split(['.', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .ToList();

    var current = new StringBuilder();
    int chunkIndex = 0;

    foreach (var sentence in sentences)
    {
        var candidate = current.Length == 0
            ? sentence
            : current + ". " + sentence;

        if (candidate.Length > maxChunkLength && current.Length > 0)
        {
            yield return new DocumentChunk(
                Id: $"{doc.Id}-chunk-{chunkIndex++}",
                DocumentId: doc.Id,
                Title: doc.Title,
                Url: doc.Url,
                Text: current.ToString().Trim() + "."
            );

            current.Clear();
            current.Append(sentence);
        }
        else
        {
            if (current.Length > 0)
            {
                current.Append(". ");
            }

            current.Append(sentence);
        }
    }

    if (current.Length > 0)
    {
        yield return new DocumentChunk(
            Id: $"{doc.Id}-chunk-{chunkIndex++}",
            DocumentId: doc.Id,
            Title: doc.Title,
            Url: doc.Url,
            Text: current.ToString().Trim() + "."
        );
    }
}

static string? GetMetadataString(Metadata? metadata, string key)
{
    if (metadata is null || !metadata.TryGetValue(key, out var value) || value is null)
    {
        return null;
    }

    return value.ToString();
}

static string GetRequired(string name) =>
    Environment.GetEnvironmentVariable(name)
    ?? throw new InvalidOperationException($"Umgebungsvariable '{name}' fehlt.");

record SourceDocument(string Id, string Title, string Url, string Content);

record DocumentChunk(
    string Id,
    string DocumentId,
    string Title,
    string Url,
    string Text);
