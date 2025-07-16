using System;

namespace NeuroChatBot.Core
{
    [Flags]
    public enum LogLevel
    {
        None = 0x00000000,
        Info = 0x00000001,
        Error = 0x00000010,
        DebugInfo = 0x00000100,
        All = 0x11111111,
    }
}