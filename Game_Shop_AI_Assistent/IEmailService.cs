using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace Game_Shop_AI_Assistent.Services
{
    public interface IEmailService
    {
        Task<bool> SendActivationKeyAsync(string toEmail, string gameName, string activationKey);
    }

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

                var subject = $"GameStore — Ключ для {gameName}";

                var body = $@"
                    <h2>Спасибо за покупку</h2>
                    <p><b>Игра:</b> {gameName}</p>
                    <p><b>Ваш ключ активации:</b></p>
                    <h1 style='color:#4CAF50'>{activationKey}</h1>
                    <p>С уважением,<br/>GameStore</p>
                ";

                using var mailMessage = new MailMessage(senderEmail, toEmail, subject, body)
                {
                    IsBodyHtml = true
                };

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Письмо отправлено на {Email}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки email");
                return false;
            }
        }
    }
}