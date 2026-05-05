using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace Game_Shop_AI_Assistent.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> SendActivationKeyAsync(string toEmail, string gameName, string activationKey)
        {
            try
            {
                var smtpServer = _config["Email:SmtpServer"];
                var smtpPort = int.Parse(_config["Email:SmtpPort"]);
                var senderEmail = _config["Email:SenderEmail"];
                var appPassword = _config["Email:AppPassword"];

                using var smtpClient = new SmtpClient(smtpServer, smtpPort)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(senderEmail, appPassword)
                };

                var subject = $"GameStore — ключ для {gameName}";

                var body = $@"
<h2>Спасибо за покупку!</h2>
<p>Игра: {gameName}</p>
<h1>{activationKey}</h1>";

                using var message = new MailMessage(senderEmail, toEmail, subject, body)
                {
                    IsBodyHtml = true
                };

                await smtpClient.SendMailAsync(message);

                _logger.LogInformation("Email sent to {Email}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email send error");
                return false;
            }
        }
    }
}