using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using MultiAgentDemo;

#pragma warning disable SKEXP0001, SKEXP0110, SKEXP0010, CS8600, CS8604

var builder = Kernel.CreateBuilder();

#region Azure OpenAI

//string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
//string apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
//string model = "GPT4o";

//builder.AddAzureOpenAIChatCompletion(
//deploymentName: modelName,
//        apiKey: apiKey,
//        endpoint: endpoint);

#endregion

// OpenAI
string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
string modelName = "gpt-4o";

builder.AddOpenAIChatCompletion(
        modelId: modelName,
        apiKey: apiKey);

builder.Plugins.AddFromType<EmailPlugin>();
Kernel kernel = builder.Build();

ChatCompletionAgent texterAgent = new()
{
    Arguments = new KernelArguments(new OpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
    Kernel = kernel,
    Instructions = """
    Du bist ein Marketingassistent und hilfst dabei Werbetexte in deutscher Sprache für die Website einer Konferenz zu entwerfen, um Kunden zu gewinnen..
    Wenn Dir etwas unklar ist, frage beim Organisator nach.
    Wenn Du fertig bist, gib den erstellten Text aus.
    """,
    Description = "Ein Chat Bot, der Webetexte für die Website schreibt.",
    Name = "MarketingTexter",
    Id = "MarketingTexter_01",
};

ChatCompletionAgent communicationAgent = new()
{
    Arguments = new KernelArguments(new OpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
    Kernel = kernel,
    Instructions = """
    Du bist ein Assistent, der bei der Kommunikation mit den Konferenzsprechern hilft.
    Deine Aufgabe ist es, den Sprechern E-Mails in deutscher Sprache zu schreiben und sie aufzufordern am "Call for Papers" teilzunehmen und Ideen für Talks und Workshops einzureichen.
    Wenn Dir etwas unklar ist, frage beim Organisator nach.
    Bitte den Organisator um Freigabe deines Textes. Bekommst Du eine Freigabe, versende die E-Mail an die potentiellen Sprecher.
    Nachdem der Text freigegeben wurde, soll die E-Mail automatisch an alle Empfänger versendet werden.
    Die Mailadressen der potentiellen Sprecher sind: "neno@loje.de"; "joerg.neumann@neogeeks.de"; "thorsten@hans.de".
    """,
    Description = "Ein Chat Bot, der die Kommunikation mit den Sprechern übernimmt und diesen E-Mails schreibt.",
    Name = "Communicator",
    Id = "Communicator_02",
};

ChatCompletionAgent organisatorAgent = new()
{
    Arguments = new KernelArguments(new OpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
    Kernel = kernel,
    Instructions = """
    Du bist ein Konferenzorganisator. Du organisierst Konferenzen und steuerst die anderen Agenten.
    Du führst die anderen Agenten und bewertest ihre Ergebnisse.
    Wenn die anderen Agenten Fragen haben, beantwortest Du diese. Wenn dir keinen sinnvolle Antwort einfällt, fragst Du den Anwender.
    Wenn Du zur Freigabe eines Arbeitsergebnisses aufgefordert wirst, bewerte den erstellten Text und gib ihn frei, wenn Du ihn gut findest.
    Wenn alle Agenten ihre Aufgaben erledigt haben, antworte mit 'FERTIG' und gib eine Zusammenfassung der Ergebnisse aus!
    Die folgenden Aufgaben müssen von den Agenten erledigt werden:
    1. Es müssen Werbetexte für die Website entworfen werden um Kunden zu gewinnen.
    2. Es müssen potentielle Sprecher angeschrieben werden und sie zum "Call for papers" eingeladen werden.
    """,
    Description = "Ein Chat Bot, der Konferenzen organisiert und hierfür andere Agenten steuert.",
    Name = "Organisator",
    Id = "Organisator_03",
};

AgentGroupChat chat = new(texterAgent, communicationAgent, organisatorAgent)
{
    ExecutionSettings = new()
    {
        TerminationStrategy = new RegexTerminationStrategy(@"\b(FERTIG)\b")
        {
            Agents = [organisatorAgent],
            MaximumIterations = 50
        }
    }
};

var goal = """
  Organisiere eine AI-Konferenz vom 15.-17.09.2025. 
  Die Konferenz richtet sich an AI-Experten und Entwickler und bietet einen Überblick der aktuellen Trends im AI-Bereich.
  Die bietet am 15.09. Ganztages-Workshops an. Am 16. und 17.09. finden in drei parallelen Tracks Talks von 45 Minuten Länge statt.
  """;

chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, goal));

Console.WriteLine($"\x1b[32m>>> {AuthorRole.User} > \x1b[37m{goal}.");
await foreach (var content in chat.InvokeAsync())
{
    Console.WriteLine($"\n\x1b[32m>>> {content.Role} [{content.AuthorName}] > \x1b[37m{content.Content}");
}
Console.WriteLine($"\x1b[32mIS COMPLETE: \u001b[37m{chat.IsComplete}");

