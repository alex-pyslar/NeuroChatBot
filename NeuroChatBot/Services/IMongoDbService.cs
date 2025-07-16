using System.Threading.Tasks;
using MongoDB.Driver;
using NeuroChatBot.Models;

namespace NeuroChatBot.Services
{
    public interface IMongoDbService
    {
        IMongoCollection<User> Users { get; }
        Task SaveUserAsync(User user);
        Task<User?> LoadUserAsync(long userId);
        Task AddChatMessageAsync(long userId, int characterIndex, ChatMessage message);
    }
}