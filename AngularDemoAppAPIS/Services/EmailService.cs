using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;

    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Food App", _emailSettings.FromEmail));
        message.To.Add(new MailboxAddress("Recipient Name", toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = body
        };
        message.Body = bodyBuilder.ToMessageBody();
        using (var client = new SmtpClient())
        {
            try
            {
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_emailSettings.SmtpUser, _emailSettings.SmtpPass);
                await client.SendAsync(message);
            }
            catch (SmtpCommandException ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message} (StatusCode: {ex.StatusCode})");
                throw;
            }
            catch (SmtpProtocolException ex)
            {
                Console.WriteLine($"Protocol error while sending email: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unknown error sending email: {ex.Message}");
                throw;
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }

    }
}

