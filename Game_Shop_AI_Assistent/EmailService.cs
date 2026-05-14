using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

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

        public async Task<bool> SendActivationKeyAsync(
            string toEmail,
            string userName,
            string gameName,
            string activationKey,
            string platform)
        {
            try
            {
                var email = new MimeMessage();

                email.From.Add(new MailboxAddress(
                    "GameStore",
                    _config["Email:SenderEmail"]
                ));

                email.To.Add(MailboxAddress.Parse(toEmail));

                email.Subject = $"GameStore — ключ для {gameName}";

                email.Body = new TextPart("html")
                {
                    Text = $@"
<h2>GameStore</h2>

<p>Здравствуйте, {userName}!</p>

<p>Ключ для <b>{gameName}</b> ({platform}):</p>

<h1 style='background:#2d3436;color:#00cec9;padding:10px'>
{activationKey}
</h1>

<p>Скачайте {platform} и активируйте продукт этим ключом.</p>

<hr>

<p>2026 GameStore</p>"
                };

                using var smtp = new MailKit.Net.Smtp.SmtpClient();

                smtp.Timeout = 10000;

                await smtp.ConnectAsync(
                    _config["Email:SmtpServer"],
                    int.Parse(_config["Email:SmtpPort"]),
                    SecureSocketOptions.StartTls
                );

                await smtp.AuthenticateAsync(
                    _config["Email:SenderEmail"],
                    _config["Email:AppPassword"]
                );

                await smtp.SendAsync(email);

                await smtp.DisconnectAsync(true);

                _logger.LogInformation($"Email отправлен: {toEmail}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка email");

                Console.WriteLine(ex.ToString());

                return false;
            }
        }

        public async Task<bool> SendMultipleKeysEmail(
            string toEmail,
            string userName,
            List<string> keysList)
        {
            try
            {
                var email = new MimeMessage();

                email.From.Add(new MailboxAddress(
                    "GameStore",
                    _config["Email:SenderEmail"]
                ));

                email.To.Add(MailboxAddress.Parse(toEmail));

                email.Subject = "GameStore — ваши ключи";

                var keysHtml = string.Join("<br>", keysList);

                email.Body = new TextPart("html")
                {
                    Text = $@"
<h2>GameStore</h2>

<p>Здравствуйте, {userName}!</p>

<p>Ваши ключи:</p>

{keysHtml}

<hr>

<p>2026 GameStore</p>"
                };

                using var smtp = new MailKit.Net.Smtp.SmtpClient();

                smtp.Timeout = 10000;

                await smtp.ConnectAsync(
                    _config["Email:SmtpServer"],
                    int.Parse(_config["Email:SmtpPort"]),
                    SecureSocketOptions.StartTls
                );

                await smtp.AuthenticateAsync(
                    _config["Email:SenderEmail"],
                    _config["Email:AppPassword"]
                );

                await smtp.SendAsync(email);

                await smtp.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка email");

                Console.WriteLine(ex.ToString());

                return false;
            }
        }
    }
}