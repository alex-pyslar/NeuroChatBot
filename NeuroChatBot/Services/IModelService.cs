using System.Threading.Tasks;
using NeuroChatBot.Models;

namespace NeuroChatBot.Services
{
    public interface IModelService
    {
        Task<string> GetModelResponseAsync(User user, string userMessage);
    }
}