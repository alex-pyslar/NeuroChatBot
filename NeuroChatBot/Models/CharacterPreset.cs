using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace NeuroChatBot.Models
{
    public class CharacterPreset
    {
        [BsonElement("name")]
        public string Name { get; set; } = "AI";

        [BsonElement("prompt")]
        public string Prompt { get; set; } = "You are a useful AI assistant";

        [BsonElement("greeting")]
        public string Greeting { get; set; } = "How I can help?";

        [BsonElement("chat")]
        public List<ChatMessage> Chat { get; set; } = new List<ChatMessage>(20);
    }
}