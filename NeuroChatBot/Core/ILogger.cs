using System;

namespace NeuroChatBot.Core
{
    public interface ILogger
    {
        LogLevel Logging { get; set; }

        void Log(LogLevel level, ConsoleColor color, params string[] messages);
        void Info(string message);
        void Info(params string[] messages);
        void DebugInfo(string message);
        void DebugInfo(params string[] messages);
        void Error(string message);
        void Error(params string[] messages);
    }
}