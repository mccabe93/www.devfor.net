namespace devfornet.Services
{
    /// <summary>
    /// Interface for email services
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body (HTML format)</param>
        /// <returns>True if email was sent successfully</returns>
        Task<bool> SendEmailAsync(string to, string subject, string body);
    }
}
