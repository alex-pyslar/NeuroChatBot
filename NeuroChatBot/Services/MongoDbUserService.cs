using NeuroChatBot.Core;
using NeuroChatBot.Models;
using NeuroChatBot.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NeuroChatBot.Services
{
    public class MongoDbUserService : IUserService
    {
        private readonly IMongoDbService _mongoDbService;
        private readonly ILogger _logger;
        private readonly Dictionary<long, User> _cachedUsers = new Dictionary<long, User>(); // Simple in-memory cache

        public MongoDbUserService(IMongoDbService mongoDbService, ILogger logger)
        {
            _mongoDbService = mongoDbService;
            _logger = logger;
        }

        public async Task<User> GetOrCreateUserAsync(long userId)
        {
            if (_cachedUsers.TryGetValue(userId, out var cachedUser))
            {
                return cachedUser;
            }

            var user = await _mongoDbService.LoadUserAsync(userId);
            if (user == null)
            {
                _logger.Info($"Creating new user for ID: {userId}");
                user = new User(userId);
                await _mongoDbService.SaveUserAsync(user); // Save initial user to DB
            }
            _cachedUsers[userId] = user; // Add to cache
            return user;
        }

        public async Task SaveUserAsync(User user)
        {
            await _mongoDbService.SaveUserAsync(user);
            _cachedUsers[user.Id] = user; // Update cache
        }

        public async Task AddChatMessageToUserAsync(long userId, ChatMessage message)
        {
            if (_cachedUsers.TryGetValue(userId, out var user))
            {
                // Ensure chat history doesn't grow indefinitely in cache before saving
                if (user.CurrentCharacter.Chat.Count >= 20)
                {
                    user.CurrentCharacter.Chat.RemoveAt(0); // Remove oldest
                    user.CurrentCharacter.Chat.RemoveAt(0); // Remove next oldest to keep a balanced size
                }
                user.CurrentCharacter.Chat.Add(message);
                // Optionally, save to DB immediately or periodically
                await _mongoDbService.AddChatMessageAsync(userId, user.Characters.IndexOf(user.CurrentCharacter), message);
            }
            else
            {
                _logger.Error($"Attempted to add chat message to uncached user {userId}. Data might be inconsistent.");
                // In a real application, you might want to load the user here and then add the message
            }
        }
    }
}