using System.Data;
using System.Diagnostics;
using MongoDB.Bson.Serialization.Attributes;
using NeuroChatBot.Services;

namespace NeuroChatBot.Models
{
    public class ChatMessage
    {
        [BsonElement("role")]
        public RoleEnums ERole { get; set; } // null-forgiving operator
        public string Role => ERole.String();

        [BsonElement("content")]
        public string Content { get; set; } = null!; // null-forgiving operator

        public ChatMessage(RoleEnums role, string content)
        {
            ERole = role;
            Content = content;
        }
    }
}