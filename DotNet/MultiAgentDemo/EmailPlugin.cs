using Microsoft.SemanticKernel;
using System.ComponentModel;

#pragma warning disable CS1998, CA1822

namespace MultiAgentDemo
{
    public class EmailPlugin
    {
        [KernelFunction("send_email")]
        [Description("Sendet eine E-Mail an die Empfänger.")]
        public async Task SendEmailAsync(
            Kernel kernel,
            [Description("Liste der E-Mail-Adressen der Empfänger.")]
            List<string> recipientEmails,
            [Description("Betreff")]
            string subject,
            [Description("E-Mail-Text.")]
            string body
        )
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"""
            ############ E-Mail wird gesendet: ############
            --------------------------------------------------------------------
            ## Empfänger ##: {string.Join("; ", recipientEmails)}
            ## Betreff ##: {subject}
            {body}
            --------------------------------------------------------------------
            ############ E-Mail versendet! ############
            """);
            Console.ResetColor();
        }
    }
}
