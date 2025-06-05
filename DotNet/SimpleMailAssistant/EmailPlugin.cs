using System.ComponentModel;
using Microsoft.SemanticKernel;

#pragma warning disable CA1822, CS1998

namespace SimpleMailAssistant
{
    public class EmailPlugin
    {
        [KernelFunction("send_email")]
        [Description("Sends an email to a recipient.")]
        public async Task SendEmailAsync(
            Kernel kernel,
            List<string> recipientEmails,
            string subject,
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
