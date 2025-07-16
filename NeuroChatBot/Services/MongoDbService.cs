using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using NeuroChatBot.Core;
using NeuroChatBot.Models;

namespace NeuroChatBot.Services
{
    public class MongoDbService : IMongoDbService
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly ILogger _logger;

        public IMongoCollection<User> Users => _usersCollection;

        public MongoDbService(string connectionString, string databaseName, ILogger logger)
        {
            _logger = logger;
            try
            {
                var client = new MongoClient(connectionString);
                var database = client.GetDatabase(databaseName);
                _usersCollection = database.GetCollection<User>("users");
                _logger.Info($"Connected to MongoDB database: {databaseName}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to connect to MongoDB: {ex.Message}");
                throw;
            }
        }

        public async Task SaveUserAsync(User user)
        {
            try
            {
                var filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
                var options = new ReplaceOptions { IsUpsert = true };
                await _usersCollection.ReplaceOneAsync(filter, user, options);
                _logger.DebugInfo($"User {user.Id} saved successfully.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error saving user {user.Id}: {ex.Message}");
                throw;
            }
        }

        public async Task<User?> LoadUserAsync(long userId)
        {
            try
            {
                var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
                return await _usersCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.Error($"Error loading user {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task AddChatMessageAsync(long userId, int characterIndex, ChatMessage message)
        {
            try
            {
                var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
                var update = Builders<User>.Update.Push($"characters.{characterIndex}.chat", message);

                var result = await _usersCollection.UpdateOneAsync(filter, update);

                if (result.MatchedCount == 0)
                {
                    _logger.Error($"User {userId} not found when trying to add chat message.");
                    throw new InvalidOperationException($"User {userId} not found.");
                }
                _logger.DebugInfo($"Chat message added for user {userId}, character index {characterIndex}.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error adding chat message for user {userId}, character index {characterIndex}: {ex.Message}");
                throw;
            }
        }
    }
}