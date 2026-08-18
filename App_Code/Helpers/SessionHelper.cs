using System;
using System.Web;
using System.Web.Security;
using CRMP.Models;

namespace CRMP.Helpers
{
    /// <summary>
    /// All session-state management in one place.
    /// Keys are private constants so there are no magic strings in pages.
    /// </summary>
    public static class SessionHelper
    {
        private const string KEY_USER_ID       = "CRMP_UserId";
        private const string KEY_USERNAME      = "CRMP_Username";
        private const string KEY_FULL_NAME     = "CRMP_FullName";
        private const string KEY_EMAIL         = "CRMP_Email";
        private const string KEY_DIVISION_ID   = "CRMP_DivisionId";
        private const string KEY_DIVISION_NAME = "CRMP_DivisionName";
        private const string KEY_ACTIVE_ROLE_ID   = "CRMP_ActiveRoleId";
        private const string KEY_ACTIVE_ROLE_CODE = "CRMP_ActiveRoleCode";
        private const string KEY_ACTIVE_ROLE_NAME = "CRMP_ActiveRoleName";
        private const string KEY_ACTIVE_DIV_ID    = "CRMP_ActiveDivId";   // division scope for current role

        // ── Store after successful login ──────────────────────────────────────
        public static void Login(User user, UserRole activeRole)
        {
            var session = HttpContext.Current.Session;
            session[KEY_USER_ID]       = user.UserId;
            session[KEY_USERNAME]      = user.Username;
            session[KEY_FULL_NAME]     = user.FullName;
            session[KEY_EMAIL]         = user.Email;
            session[KEY_DIVISION_ID]   = user.DivisionId;
            session[KEY_DIVISION_NAME] = user.DivisionName;

            SetActiveRole(activeRole);
        }

        public static void SetActiveRole(UserRole role)
        {
            var session = HttpContext.Current.Session;
            session[KEY_ACTIVE_ROLE_ID]   = role.RoleId;
            session[KEY_ACTIVE_ROLE_CODE] = role.RoleCode;
            session[KEY_ACTIVE_ROLE_NAME] = role.RoleName;
            session[KEY_ACTIVE_DIV_ID]    = role.DivisionId;
        }

        // ── Readers ───────────────────────────────────────────────────────────
        public static int UserId =>
            HttpContext.Current.Session[KEY_USER_ID] != null
                ? (int)HttpContext.Current.Session[KEY_USER_ID] : 0;

        public static string Username =>
            HttpContext.Current.Session[KEY_USERNAME]?.ToString() ?? "";

        public static string FullName =>
            HttpContext.Current.Session[KEY_FULL_NAME]?.ToString() ?? "";

        public static string Email =>
            HttpContext.Current.Session[KEY_EMAIL]?.ToString() ?? "";

        public static int DivisionId =>
            HttpContext.Current.Session[KEY_DIVISION_ID] != null
                ? (int)HttpContext.Current.Session[KEY_DIVISION_ID] : 0;

        public static string DivisionName =>
            HttpContext.Current.Session[KEY_DIVISION_NAME]?.ToString() ?? "";

        public static int ActiveRoleId =>
            HttpContext.Current.Session[KEY_ACTIVE_ROLE_ID] != null
                ? (int)HttpContext.Current.Session[KEY_ACTIVE_ROLE_ID] : 0;

        public static string ActiveRoleCode =>
            HttpContext.Current.Session[KEY_ACTIVE_ROLE_CODE]?.ToString() ?? "";

        public static string ActiveRoleName =>
            HttpContext.Current.Session[KEY_ACTIVE_ROLE_NAME]?.ToString() ?? "";

        public static int? ActiveDivisionId =>
            HttpContext.Current.Session[KEY_ACTIVE_DIV_ID] as int?;

        public static bool IsLoggedIn => UserId > 0;

        // ── Role helpers ──────────────────────────────────────────────────────
        public static bool IsEmployee      => ActiveRoleCode == "EMPLOYEE";
        public static bool IsDivisionHead  => ActiveRoleCode == "DIVISION_HEAD";
        public static bool IsIso           => ActiveRoleCode == "ISO";
        public static bool IsAdminHr       => ActiveRoleCode == "ADMIN_HR";
        public static bool IsDirector      => ActiveRoleCode == "DIRECTOR";
        public static bool IsTechExpert    => ActiveRoleCode == "TECH_EXPERT";
        public static bool IsModerator     => ActiveRoleCode == "MODERATOR";
        public static bool IsOicIt         => ActiveRoleCode == "OIC_IT";

        public static bool IsApproverRole =>
            IsDivisionHead || IsIso || IsAdminHr || IsDirector;

        // ── Clear session on logout ───────────────────────────────────────────
        public static void Logout()
        {
            HttpContext.Current.Session.Abandon();
            FormsAuthentication.SignOut();
        }
    }
}
