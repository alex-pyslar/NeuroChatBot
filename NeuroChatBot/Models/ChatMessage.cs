using MongoDB.Bson.Serialization.Attributes;

namespace NeuroChatBot.Models
{
    public class ChatMessage
    {
        [BsonElement("role")]
        public string Role { get; set; } = null!; // null-forgiving operator

        [BsonElement("content")]
        public string Content { get; set; } = null!; // null-forgiving operator

        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }
}