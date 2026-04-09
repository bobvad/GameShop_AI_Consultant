// Services/EmailService.cs
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
                return true;
            }
            catch (SmtpException ex)
            {
                Console.WriteLine($"SMTP Error: {ex.StatusCode} - {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
    }
}