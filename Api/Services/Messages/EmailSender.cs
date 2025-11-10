using brevo_csharp.Api;
using brevo_csharp.Client;
using brevo_csharp.Model;
using System;
using Task = System.Threading.Tasks.Task;

namespace Api.Services
{
    public class EmailSender
    {
        public static async Task SendEmailAsync(string recipientEmail, string recipientName, int templateId)
        {
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                Console.WriteLine("Recipient email is null or empty. Email not sent.");
                return;
            }
            
            try
            {
                var apiInstance = new TransactionalEmailsApi();
                
                var sendSmtpEmail = new SendSmtpEmail
                {
                    To = [new(recipientEmail, recipientName)],
                    TemplateId = templateId,
                    Params = new Dictionary<string, object>
                    {
                        { "LASTNAME", recipientName }
                    }
                };

                var response = await apiInstance.SendTransacEmailAsync(sendSmtpEmail);
                Console.WriteLine("Email sent successfully. Message ID: " + response.MessageId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending email: " + ex.Message);
            }
        }
    }
}