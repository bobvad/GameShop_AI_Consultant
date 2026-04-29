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

        // Services/EmailService.cs — улучшенная версия с логами

        public async Task<bool> SendActivationKeyAsync(string toEmail, string gameName, string activationKey)
        {
            try
            {
                var smtpServer = _config["Email:SmtpServer"];
                var smtpPort = int.Parse(_config["Email:SmtpPort"]);
                var senderEmail = _config["Email:SenderEmail"];
                var appPassword = _config["Email:AppPassword"];

                _logger.LogInformation("SMTP: {Server}:{Port}, From: {From}, To: {To}",
                    smtpServer, smtpPort, senderEmail, toEmail);

                using var smtpClient = new SmtpClient(smtpServer, smtpPort)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(senderEmail, appPassword),
                    Timeout = 60000,
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                var subject = $"GameStore: Ключ активации для {gameName}";
                var body = $@"Здравствуйте!

Игра: {gameName}
Ваш ключ активации: {activationKey}

С уважением, команда GameStore";

                using var mailMessage = new MailMessage(senderEmail, toEmail, subject, body)
                {
                    IsBodyHtml = false
                };

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("✅ Письмо успешно отправлено на {Email}", toEmail);
                return true;
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "❌ SMTP ошибка: {StatusCode} — {Message}", ex.StatusCode, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Общая ошибка отправки: {Message}", ex.Message);
                return false;
            }
        }
    }
}