using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JARVIS.UserSettings
{
    public static class UserProfileManager
    {
        private static readonly Dictionary<string, string> UserNames = new()
    {
        { "drew", "Drew" },
        { "rose", "Rose" },
        { "guest", "Guest" },
        { "unknown", "Guest" }
    };

        public static string GetDisplayName(string userId)
        {
            return UserNames.TryGetValue(userId, out var name) ? name : "Guest";
        }
    }
}