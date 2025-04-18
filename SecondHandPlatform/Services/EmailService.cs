using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net.Mail;
using MailKitSmtp = MailKit.Net.Smtp.SmtpClient;
using MailKitSecurity = MailKit.Security;
using SecondHandPlatform.Models;
using SecondHandPlatform.Services;

namespace SecondHandPlatformTest.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtp;
    public EmailService(IOptions<SmtpSettings> options) => _smtp = options.Value;

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromEmail));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;
        msg.Body = new TextPart("plain") { Text = body };

        using var smtp = new MailKit.Net.Smtp.SmtpClient();
        await smtp.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_smtp.Username, _smtp.Password);
        await smtp.SendAsync(msg);
        await smtp.DisconnectAsync(true);
    }
}