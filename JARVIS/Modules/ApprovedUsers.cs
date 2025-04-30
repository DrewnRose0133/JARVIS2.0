
using System.Collections.Generic;

namespace JARVIS.Modules
{
    public static class ApprovedUsers
    {
        private static readonly HashSet<string> approved = new HashSet<string> { "Andrew", "Admin" };

        public static bool IsApproved(string user)
        {
            Logger.Log($"Checking if user '{user}' is approved.");
            return approved.Contains(user);
        }

        public static void AddUser(string user)
        {
            approved.Add(user);
            Logger.Log($"User '{user}' added to approved list.");
        }
    }
}
