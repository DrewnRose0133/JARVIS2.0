using System;
using System.Collections.Generic;

namespace JARVIS.UserSettings
{
    public enum PermissionLevel
    {
        Guest,
        User,
        Admin
    }

    public class UserPermissionManager
    {
        private readonly Dictionary<string, PermissionLevel> _userPermissions = new(StringComparer.OrdinalIgnoreCase)
        {
            { "drew", PermissionLevel.Admin },
            { "rose", PermissionLevel.User },
            { "guest", PermissionLevel.Guest },
            { "unknown", PermissionLevel.Guest }
        };

        public PermissionLevel GetPermission(string userId)
        {
            if (_userPermissions.TryGetValue(userId, out var level))
                return level;

            return PermissionLevel.Guest;
        }
    }
}
