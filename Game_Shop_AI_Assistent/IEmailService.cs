using System.Threading.Tasks;

namespace Game_Shop_AI_Assistent.Services
{
    public interface IEmailService
    {
        Task<bool> SendActivationKeyAsync(string toEmail, string gameName, string activationKey);
    }
}