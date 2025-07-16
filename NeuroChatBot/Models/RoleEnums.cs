using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroChatBot.Models
{
    public enum RoleEnums
    {
        System,
        Assistant,
        User
    }
    public static class RoleExt
    {
        public static string String(this RoleEnums role) => role switch
        {
            RoleEnums.System => "system",
            RoleEnums.Assistant => "assistant",
            RoleEnums.User => "user"
        };
    }
        
}
