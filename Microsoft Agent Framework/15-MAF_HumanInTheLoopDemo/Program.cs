using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace HumanInTheLoopDemo;

internal static class Program
{
    private static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException(
                "Die Umgebungsvariable OPENAI_API_KEY ist nicht gesetzt.");

        string model = Environment.GetEnvironmentVariable("OPENAI_MODEL")
            ?? "gpt-4o-mini";

        // Das LLM erhält die Überweisung als Tool. Durch den Wrapper darf das
        // Framework den Tool-Aufruf aber erst nach einer Bestätigung ausführen.
        AIAgent agent = new OpenAIClient(apiKey)
            .GetChatClient(model)
            .AsIChatClient()
            .AsAIAgent(
                name: "TransferAgent",
                instructions:
                    "Du bist ein Assistent für Überweisungen. " +
                    "Wenn der Benutzer eine Überweisung verlangt, extrahiere Empfänger, " +
                    "IBAN und Betrag und rufe ExecuteTransfer auf. " +
                    "Frage nicht selbst nach einer Bestätigung; das übernimmt das System. " +
                    "Behaupte niemals, dass die Überweisung ausgeführt wurde, bevor das Tool " +
                    "ein erfolgreiches Ergebnis geliefert hat.",
                tools:
                [
                    new ApprovalRequiredAIFunction(
                        AIFunctionFactory.Create(ExecuteTransfer))
                ]);

        Workflow workflow = AgentWorkflowBuilder.BuildSequential([agent]);

        Console.WriteLine($"OpenAI-Modell: {model}");
        Console.WriteLine();
        Console.Write("Auftrag (Enter für das Beispiel): ");

        string? input = Console.ReadLine();
        string userRequest = string.IsNullOrWhiteSpace(input)
            ? "Überweise 1250 Euro an die Musterlieferant GmbH " +
              "auf die IBAN DE00 0000 0000 0000 0000 00."
            : input;

        Console.WriteLine();
        Console.WriteLine($"Benutzer: {userRequest}");
        Console.Write("Agent: ");

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, userRequest)
        };

        await using StreamingRun run =
            await InProcessExecution.RunStreamingAsync(workflow, messages);

        // Startet die nächste Agentenrunde und sorgt dafür, dass deren Ereignisse
        // über WatchStreamAsync veröffentlicht werden.
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        await foreach (WorkflowEvent workflowEvent in run.WatchStreamAsync())
        {
            switch (workflowEvent)
            {
                case AgentResponseUpdateEvent updateEvent:
                    Console.Write(updateEvent.Update.Text);
                    break;

                case RequestInfoEvent requestInfoEvent
                    when requestInfoEvent.Request.TryGetDataAs(
                        out ToolApprovalRequestContent? approvalRequest):
                {
                    bool approved = AskUserForApproval(approvalRequest);

                    // Die Antwort setzt den pausierten Workflow fort. Bei Ablehnung
                    // wird ExecuteTransfer nicht aufgerufen.
                    ExternalResponse response = requestInfoEvent.Request.CreateResponse(
                        approvalRequest.CreateResponse(approved));

                    await run.SendResponseAsync(response);
                    Console.WriteLine();
                    Console.Write("Agent: ");
                    break;
                }

                case WorkflowOutputEvent:
                    Console.WriteLine();
                    Console.WriteLine("Workflow beendet.");
                    return;

                case WorkflowErrorEvent errorEvent:
                    Console.Error.WriteLine();
                    Console.Error.WriteLine(
                        errorEvent.Exception?.Message ?? "Unbekannter Workflowfehler.");
                    return;

                case ExecutorFailedEvent failedEvent:
                    Console.Error.WriteLine();
                    Console.Error.WriteLine(
                        $"Executor '{failedEvent.ExecutorId}' ist fehlgeschlagen: " +
                        $"{failedEvent.Data?.Message ?? "unbekannter Fehler"}");
                    return;
            }
        }
        Console.ReadLine();
    }

    private static bool AskUserForApproval(
        ToolApprovalRequestContent approvalRequest)
    {
        var toolCall = (FunctionCallContent)approvalRequest.ToolCall;

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("=== BESTÄTIGUNG ERFORDERLICH ===");
        Console.WriteLine($"Tool:      {toolCall.Name}");
        Console.WriteLine(
            $"Parameter: {JsonSerializer.Serialize(toolCall.Arguments)}");

        while (true)
        {
            Console.Write("Aktion ausführen? [j/n]: ");

            switch (Console.ReadLine()?.Trim().ToLowerInvariant())
            {
                case "j":
                case "ja":
                    return true;

                case "n":
                case "nein":
                    return false;

                default:
                    Console.WriteLine("Bitte 'j' oder 'n' eingeben.");
                    break;
            }
        }
    }

    [Description("Führt eine Überweisung aus.")]
    private static async Task<string> ExecuteTransfer(
        [Description("Name des Zahlungsempfängers")] string recipient,
        [Description("IBAN des Zahlungsempfängers")] string iban,
        [Description("Zu überweisender Betrag in Euro")] decimal amount)
    {
        // Dieser Code wird erst NACH der menschlichen Zustimmung erreicht.
        Console.WriteLine();
        Console.WriteLine("[AKTION] Überweisung wird simuliert ...");
        await Task.Delay(750);

        return $"Überweisung über {amount:N2} EUR an {recipient} ({iban}) ausgeführt.";
    }
}
