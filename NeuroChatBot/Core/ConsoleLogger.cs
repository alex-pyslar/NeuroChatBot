using System;

namespace NeuroChatBot.Core
{
    public class ConsoleLogger : ILogger
    {
        public LogLevel Logging { get; set; } = LogLevel.None; // Default to None

        public void Log(LogLevel level, ConsoleColor color, params string[] messages)
        {
            if ((Logging & level) != level)
                return;

            Console.ForegroundColor = ConsoleColor.Blue;
            if (messages.Length == 1)
                Console.Write($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}][{level}]");
            else
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}][{level}]");

            Console.ForegroundColor = color;
            foreach (var message in messages)
                Console.WriteLine($"{message}");
            Console.ResetColor();
        }

        public void Info(string message) => Info(messages: message);
        public void Info(params string[] messages) => Log(LogLevel.Info, ConsoleColor.Green, messages);
        public void DebugInfo(string message) => DebugInfo(messages: message);
        public void DebugInfo(params string[] messages) => Log(LogLevel.DebugInfo, ConsoleColor.DarkYellow, messages);
        public void Error(string message) => Error(messages: message);
        public void Error(params string[] messages) => Log(LogLevel.Error, ConsoleColor.Red, messages);
    }
}