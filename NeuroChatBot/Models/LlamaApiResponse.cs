using System.Collections.Generic;

namespace NeuroChatBot.Models
{
    public class CompletionResponse
    {
        public List<Choice> choices { get; set; } = new List<Choice>();
        public class Choice
        {
            public string text { get; set; } = null!;
        }
    }

    public class ChatCompletionResponse
    {
        public List<Choice> choices { get; set; } = new List<Choice>();
        public class Choice
        {
            public MessageResponse message { get; set; } = null!;
        }
        public class MessageResponse
        {
            public string content { get; set; } = null!;
        }
    }
}