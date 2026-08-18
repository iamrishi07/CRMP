using System;
using System.Collections.Generic;
using System.Data;
using CRMP.DAL;
using CRMP.Helpers;
using CRMP.Models;

namespace CRMP.BLL
{
    /// <summary>
    /// SLA Engine — runs periodically (every N minutes via Global.asax timer).
    /// Marks breached requests, fires proactive warning notifications at threshold,
    /// and fires breach notifications to relevant oversight roles.
    /// </summary>
    public static class SlaEngine
    {
        private static readonly RequestRepository  _reqRepo   = new RequestRepository();
        private static readonly NotificationRepository _notifRepo = new NotificationRepository();

        public static void RunCheck()
        {
            // 1. Mark newly-breached requests
            var breachedIds = _reqRepo.GetBreachedRequestIds();
            foreach (var id in breachedIds)
            {
                _reqRepo.MarkSlaBreached(id);
                _reqRepo.AddTimeline(id, "SLA_BREACH",
                    "SLA deadline has been breached. Escalation notifications sent.", null);
                NotifySlaBreachRoles(id);
            }

            // 2. Proactive warning at threshold (e.g. 75%)
            CheckAndNotifyAtRisk();
        }

        private static void NotifySlaBreachRoles(int requestId)
        {
            var req = _reqRepo.GetById(requestId);
            if (req == null) return;

            // Notify the submitter
            _notifRepo.Create(req.SubmitterUserId, "⚠ SLA Breached",
                $"Request {req.RequestNumber} has exceeded its SLA deadline.",
                $"~/Pages/Employee/RequestDetail.aspx?id={requestId}", "SLA_BREACH");

            // Notify Moderators and OIC IT (users holding those roles)
            var modIds = GetUserIdsForRole("MODERATOR");
            var oicIds = GetUserIdsForRole("OIC_IT");
            var all    = new HashSet<int>(modIds);
            all.UnionWith(oicIds);

            foreach (var uid in all)
                _notifRepo.Create(uid, "⚠ SLA Breach",
                    $"Request {req.RequestNumber} ({req.TypeName}) from {req.DivisionName} has breached SLA.",
                    $"~/Pages/Moderator/RequestList.aspx?sla=1", "SLA_BREACH");

            // Also email if configured
            EmailService.SendSlaBreachAlert(req);
        }

        private static void CheckAndNotifyAtRisk()
        {
            // Get requests that have crossed the warn threshold but are not yet breached
            var dt = OracleHelper.ExecuteQuerySql(@"
                SELECT r.REQUEST_ID, r.REQUEST_NUMBER, r.TYPE_ID, r.SUBMITTER_USER_ID,
                       r.SUBMITTED_AT, r.SLA_DEADLINE,
                       ROUND(
                           (CAST(SYSTIMESTAMP AS DATE) - CAST(r.SUBMITTED_AT AS DATE))
                           / NULLIF((CAST(r.SLA_DEADLINE AS DATE) - CAST(r.SUBMITTED_AT AS DATE)), 0) * 100
                       ) AS PCT_CONSUMED
                FROM   REQUESTS r
                JOIN   REQUEST_TYPES rt ON rt.TYPE_ID = r.TYPE_ID
                LEFT JOIN SLA_CONFIGS sc ON sc.TYPE_ID = r.TYPE_ID AND sc.PRIORITY = r.PRIORITY
                WHERE  r.STATUS NOT IN ('RESOLVED','CLOSED','REJECTED','CANCELLED')
                  AND  r.IS_SLA_BREACHED = 0
                  AND  r.SLA_WARN_NOTIFIED = 0
                  AND  r.SLA_DEADLINE IS NOT NULL
                  AND  ROUND(
                         (CAST(SYSTIMESTAMP AS DATE) - CAST(r.SUBMITTED_AT AS DATE))
                         / NULLIF((CAST(r.SLA_DEADLINE AS DATE) - CAST(r.SUBMITTED_AT AS DATE)), 0) * 100
                       ) >= NVL(sc.WARN_THRESHOLD_PCT, 75)");

            foreach (DataRow row in dt.Rows)
            {
                int reqId      = OracleHelper.ToInt(row["REQUEST_ID"]);
                string reqNum  = OracleHelper.ToString(row["REQUEST_NUMBER"]);
                int submitter  = OracleHelper.ToInt(row["SUBMITTER_USER_ID"]);
                int pct        = OracleHelper.ToInt(row["PCT_CONSUMED"]);

                // Notify submitter
                _notifRepo.Create(submitter, "SLA Warning",
                    $"Request {reqNum} has consumed {pct}% of its SLA time. Please check progress.",
                    $"~/Pages/Employee/RequestDetail.aspx?id={reqId}", "SLA_WARNING");

                // Mark warned so we don't re-fire
                OracleHelper.ExecuteNonQuerySql(
                    "UPDATE REQUESTS SET SLA_WARN_NOTIFIED=1 WHERE REQUEST_ID=:P_ID",
                    new[] { OracleHelper.ParamInt("P_ID", reqId) });
            }
        }

        private static List<int> GetUserIdsForRole(string roleCode)
        {
            var dt = OracleHelper.ExecuteQuerySql(@"
                SELECT ur.USER_ID FROM USER_ROLES ur
                JOIN   ROLES r ON r.ROLE_ID = ur.ROLE_ID
                WHERE  r.ROLE_CODE = :P_CODE AND ur.IS_ACTIVE = 1",
                new[] { OracleHelper.ParamStr("P_CODE", roleCode, 50) });

            var ids = new List<int>();
            foreach (DataRow row in dt.Rows)
                ids.Add(OracleHelper.ToInt(row["USER_ID"]));
            return ids;
        }
    }
}
