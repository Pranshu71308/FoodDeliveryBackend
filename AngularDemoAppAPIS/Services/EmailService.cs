//using MailKit.Net.Smtp;
//using MimeKit;
//using Microsoft.Extensions.Options;
//using System.Threading.Tasks;

//public interface IEmailService
//{
//    Task SendEmailAsync(string toEmail, string subject, string body);
//}

//public class EmailService : IEmailService
//{
//    private readonly EmailSettings _emailSettings;

//    public EmailService(IOptions<EmailSettings> emailSettings)
//    {
//        _emailSettings = emailSettings.Value;
//    }

//    public async Task SendEmailAsync(string toEmail, string subject, string body)
//    {
//        var message = new MimeMessage();
//        message.From.Add(new MailboxAddress("Your Name or Company", _emailSettings.FromEmail));
//        message.To.Add(new MailboxAddress("Recipient Name", toEmail));
//        message.Subject = subject;

//        var bodyBuilder = new BodyBuilder
//        {
//            HtmlBody = body
//        };
//        message.Body = bodyBuilder.ToMessageBody();

//        using (var client = new SmtpClient())
//        {
//            await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, true);
//            await client.AuthenticateAsync(_emailSettings.SmtpUser, _emailSettings.SmtpPass);
//            await client.SendAsync(message);
//            await client.DisconnectAsync(true);
//        }
//    }
//}
// EmailService.cs
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using MailKit.Security;

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
        message.From.Add(new MailboxAddress("Your Name or Company", _emailSettings.FromEmail));
        message.To.Add(new MailboxAddress("Recipient Name", toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = body
        };
        message.Body = bodyBuilder.ToMessageBody();

        //using (var client = new SmtpClient())
        //{
        //    try
        //    {
        //        await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, true);
        //        await client.AuthenticateAsync(_emailSettings.SmtpUser, _emailSettings.SmtpPass);
        //        await client.SendAsync(message);
        //    }
        //    catch (SmtpCommandException ex)
        //    {
        //        // Handle known SMTP exceptions
        //        Console.WriteLine($"Error sending email: {ex.Message} (StatusCode: {ex.StatusCode})");
        //        throw;
        //    }
        //    catch (SmtpProtocolException ex)
        //    {
        //        // Handle protocol errors
        //        Console.WriteLine($"Protocol error while sending email: {ex.Message}");
        //        throw;
        //    }
        //    catch (Exception ex)
        //    {
        //        // Handle all other errors
        //        Console.WriteLine($"Unknown error sending email: {ex.Message}");
        //        throw;
        //    }
        //    finally
        //    {
        //        await client.DisconnectAsync(true);
        //    }
        //}
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
                // Handle known SMTP exceptions
                Console.WriteLine($"Error sending email: {ex.Message} (StatusCode: {ex.StatusCode})");
                throw;
            }
            catch (SmtpProtocolException ex)
            {
                // Handle protocol errors
                Console.WriteLine($"Protocol error while sending email: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                // Handle all other errors
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

