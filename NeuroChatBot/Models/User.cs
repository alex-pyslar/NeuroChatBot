using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NeuroChatBot.Models
{
    [BsonIgnoreExtraElements]
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.Int64)] // Store userId as Int64
        public long Id { get; set; }

        [BsonElement("username")]
        public string UserName { get; set; } = "Вы";

        [BsonElement("userDescription")]
        public string? UserDescription { get; set; } = null;

        [BsonElement("characters")]
        public List<CharacterPreset> Characters { get; set; } = new List<CharacterPreset>(1) { new CharacterPreset() };

        [BsonElement("currentCharacterIndex")]
        private int _currentCharacterIndex = 0;

        [BsonIgnore] // Not stored in DB, derived property
        public CharacterPreset CurrentCharacter => Characters[_currentCharacterIndex];

        [BsonIgnore] // Not stored in DB, transient state
        [JsonIgnore]
        public string? PendingCommand { get; set; } = null;

        [BsonIgnore] // Not stored in DB, transient state
        [JsonIgnore]
        public DateTime RequestTime { get; set; } = DateTime.MinValue;

        [BsonIgnore] // Not stored in DB, transient state
        [JsonIgnore]
        public int LastMessageId { get; set; } = 0;

        public User() { }
        public User(long id) { Id = id; }

        public void ChangeCurrentCharacter(int ind)
        {
            if (ind >= 0 && ind < Characters.Count)
            {
                _currentCharacterIndex = ind;
            }
        }
    }
}