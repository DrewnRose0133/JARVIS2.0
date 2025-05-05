namespace JARVIS.UserSettings
{
    public static class UserSessionManager
    {
        public static string CurrentUserId { get; private set; } = "unknown";
        public static PermissionLevel CurrentPermission { get; private set; } = PermissionLevel.Guest;

        public static void Authenticate(string userId, PermissionLevel permissionLevel)
        {
            CurrentUserId = userId;
            CurrentPermission = permissionLevel;
        }

        public static void Reset()
        {
            CurrentUserId = "unknown";
            CurrentPermission = PermissionLevel.Guest;
        }
    }
}