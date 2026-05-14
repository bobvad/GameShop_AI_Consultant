namespace Game_Shop_AI_Assistent.Services
{
    public interface IEmailService
    {
        Task<bool> SendActivationKeyAsync(string toEmail, string userName, string gameName, string activationKey, string platform);
        Task<bool> SendMultipleKeysEmail(string toEmail, string userName, List<string> keysList);
    }
}