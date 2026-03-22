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

        public EmailService(IConfiguration config)
        {
            _config = config;
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
                    Credentials = new NetworkCredential(senderEmail, appPassword),
                    Timeout = 30000
                };

                var subject = $"RegIn: Ключ активации для {gameName}";
                var body = $@"
Здравствуйте!

Спасибо за покупку в магазине RegIn.

Игра: {gameName}
Ваш ключ активации: {activationKey}

Инструкция:
1. Запустите игру или лаунчер
2. Введите ключ в поле активации
3. Наслаждайтесь игрой!

Ключ действителен бессрочно.
Если у вас возникли вопросы — ответьте на это письмо.

С уважением, команда RegIn
                ".Trim();

                using var mailMessage = new MailMessage(senderEmail, toEmail, subject, body)
                {
                    IsBodyHtml = false,
                    Priority = MailPriority.Normal
                };

                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки письма: {ex.Message}");
                return false;
            }
        }
    }
}