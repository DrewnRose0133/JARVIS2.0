// === UserPermissionManager.cs ===
using System;
using System.Collections.Generic;

namespace JARVIS.UserPermissions
{
    public enum PermissionLevel
    {
        Guest,
        User,
        Admin
    }

    public class UserPermissionManager
    {
        private readonly Dictionary<string, PermissionLevel> _userPermissions = new()
        {
            { "drew", PermissionLevel.Admin },
            { "rose", PermissionLevel.User },
            { "guest", PermissionLevel.Guest },
            { "unknown", PermissionLevel.Guest }
        };

        public PermissionLevel GetPermission(string userId)
        {
            return _userPermissions.TryGetValue(userId.ToLower(), out var level)
                ? level
                : PermissionLevel.Guest;
        }
    }
}
