using System;
using System.Collections.Generic;
using System.Data;
using CRMP.DAL;
using CRMP.Helpers;
using CRMP.Models;

namespace CRMP.BLL
{
    /// <summary>
    /// Central notification dispatcher.  All code that needs to send a notification
    /// calls NotificationService — it checks user prefs and routes to in-app / email
    /// as appropriate.
    /// </summary>
    public static class NotificationService
    {
        private static readonly NotificationRepository _repo = new NotificationRepository();

        // ── Core dispatcher ───────────────────────────────────────────────────
        public static void Notify(int userId, string title, string message,
                                   string link, string notifType)
        {
            var pref = _repo.GetPref(userId, notifType);

            if (pref.InAppEnabled)
                _repo.Create(userId, title, message, link, notifType);

            if (pref.EmailEnabled)
            {
                var user = new UserRepository().GetById(userId);
                if (user != null)
                    EmailService.SendNotificationEmail(user.Email, user.FullName, title, message, link);
            }
        }

        // ── Daily digest (called once per day by Global.asax timer) ──────────
        public static void SendDailyDigest()
        {
            // Find all approvers who have pending items
            var dt = OracleHelper.ExecuteQuerySql(@"
                SELECT DISTINCT ra.APPROVER_USER_ID, u.FULL_NAME, u.EMAIL,
                       COUNT(ra.APPROVAL_ID) AS PENDING_COUNT
                FROM   REQUEST_APPROVALS ra
                JOIN   USERS u ON u.USER_ID = ra.APPROVER_USER_ID
                WHERE  ra.ACTION = 'PENDING'
                  AND  u.IS_ACTIVE = 1
                GROUP BY ra.APPROVER_USER_ID, u.FULL_NAME, u.EMAIL");

            foreach (DataRow row in dt.Rows)
            {
                int uid       = OracleHelper.ToInt(row["APPROVER_USER_ID"]);
                string name   = OracleHelper.ToString(row["FULL_NAME"]);
                string email  = OracleHelper.ToString(row["EMAIL"]);
                int count     = OracleHelper.ToInt(row["PENDING_COUNT"]);

                var pref = _repo.GetPref(uid, "DAILY_DIGEST");
                if (!pref.EmailEnabled) continue;

                EmailService.SendDigestEmail(email, name, count);
            }

            // Also notify tech experts of pending pool items
            var teDt = OracleHelper.ExecuteQuerySql(@"
                SELECT u.USER_ID, u.FULL_NAME, u.EMAIL, COUNT(r.REQUEST_ID) AS POOL_COUNT
                FROM   USERS u
                JOIN   USER_ROLES ur ON ur.USER_ID = u.USER_ID AND ur.IS_ACTIVE = 1
                JOIN   ROLES ro ON ro.ROLE_ID = ur.ROLE_ID AND ro.ROLE_CODE='TECH_EXPERT'
                JOIN   TECH_EXPERT_CATEGORIES tec ON tec.USER_ID = u.USER_ID AND tec.IS_ACTIVE=1
                JOIN   REQUESTS r ON r.TYPE_ID IN (
                           SELECT TYPE_ID FROM REQUEST_TYPES WHERE CATEGORY_ID = tec.CATEGORY_ID
                       ) AND r.STATUS='APPROVED' AND r.TECH_EXPERT_ID IS NULL
                WHERE  u.IS_ACTIVE = 1
                GROUP BY u.USER_ID, u.FULL_NAME, u.EMAIL");

            foreach (DataRow row in teDt.Rows)
            {
                int uid      = OracleHelper.ToInt(row["USER_ID"]);
                string name  = OracleHelper.ToString(row["FULL_NAME"]);
                string email = OracleHelper.ToString(row["EMAIL"]);
                int count    = OracleHelper.ToInt(row["POOL_COUNT"]);

                var pref = _repo.GetPref(uid, "DAILY_DIGEST");
                if (!pref.EmailEnabled) continue;

                EmailService.SendTechDigestEmail(email, name, count);
            }
        }

        // ── Notification type constants (used everywhere for consistency) ─────
        public static class Types
        {
            public const string RequestSubmitted  = "REQUEST_SUBMITTED";
            public const string ApprovalRequired  = "APPROVAL_REQUIRED";
            public const string RequestApproved   = "REQUEST_APPROVED";
            public const string RequestRejected   = "REQUEST_REJECTED";
            public const string RequestAssigned   = "REQUEST_ASSIGNED";
            public const string RequestResolved   = "REQUEST_RESOLVED";
            public const string SlaWarning        = "SLA_WARNING";
            public const string SlaBreach         = "SLA_BREACH";
            public const string DailyDigest       = "DAILY_DIGEST";
            public const string Announcement      = "ANNOUNCEMENT";
            public const string RatingRequested   = "RATING_REQUESTED";
        }
    }
}
