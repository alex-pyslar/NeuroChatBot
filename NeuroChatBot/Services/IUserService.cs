using System.Threading.Tasks;
using NeuroChatBot.Models;

namespace NeuroChatBot.Services
{
    public interface IUserService
    {
        Task<User> GetOrCreateUserAsync(long userId);
        Task SaveUserAsync(User user);
        Task AddChatMessageToUserAsync(long userId, ChatMessage message);
    }
}