using System.Net;
using System.Net.Mail;

namespace devfornet.Services
{
    /// <summary>
    /// Implementation of the email service using SMTP
    /// </summary>
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailService> _logger;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _smtpServer =
                Environment.GetEnvironmentVariable("EMAIL_HOST")
                ?? throw new InvalidOperationException(
                    "EMAIL_HOST environment variable is not set"
                );
            _smtpPort = int.Parse(
                Environment.GetEnvironmentVariable("EMAIL_PORT")
                    ?? throw new InvalidOperationException(
                        "EMAIL_PORT environment variable is not set"
                    )
            );
            _smtpUsername =
                Environment.GetEnvironmentVariable("EMAIL_USERNAME")
                ?? throw new InvalidOperationException(
                    "EMAIL_USERNAME environment variable is not set"
                );
            _smtpPassword =
                Environment.GetEnvironmentVariable("EMAIL_PASSWORD")
                ?? throw new InvalidOperationException(
                    "EMAIL_PASSWORD environment variable is not set"
                );
            _fromEmail =
                Environment.GetEnvironmentVariable("EMAIL_FROM")
                ?? throw new InvalidOperationException(
                    "EMAIL_FROM environment variable is not set"
                );
            _fromName = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME") ?? "DevFor.NET";
        }

        /// <summary>
        /// Sends an email using SMTP
        /// </summary>
        public async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };
                message.To.Add(to);

                using var client = new SmtpClient(_smtpServer, _smtpPort)
                {
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                    EnableSsl = true,
                };

                await client.SendMailAsync(message);
                _logger.LogInformation(
                    "Email sent to {Email} with subject: {Subject}",
                    to,
                    subject
                );
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send email to {Email} with subject: {Subject}",
                    to,
                    subject
                );
                return false;
            }
        }
    }
}
