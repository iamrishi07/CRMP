using System;
using System.Collections.Generic;
using System.Data;
using CRMP.Helpers;
using CRMP.Models;
using Oracle.ManagedDataAccess.Client;

namespace CRMP.DAL
{
    public class ApprovalRepository
    {
        // ── Create pending approval record ────────────────────────────────────
        public int CreatePending(int requestId, int stageId, int approverUserId, int sequenceNumber)
        {
            int newId = OracleHelper.NextVal("SEQ_APPROVALS");
            OracleHelper.ExecuteNonQuerySql(@"
                INSERT INTO REQUEST_APPROVALS
                    (APPROVAL_ID, REQUEST_ID, STAGE_ID, APPROVER_USER_ID, ACTION, SEQUENCE_NUMBER)
                VALUES (:P_ID, :P_R, :P_S, :P_U, 'PENDING', :P_SEQ)",
                new[]
                {
                    OracleHelper.ParamInt("P_ID",  newId),
                    OracleHelper.ParamInt("P_R",   requestId),
                    OracleHelper.ParamInt("P_S",   stageId),
                    OracleHelper.ParamInt("P_U",   approverUserId),
                    OracleHelper.ParamInt("P_SEQ", sequenceNumber)
                });
            return newId;
        }

        // ── Act on an approval (approve / reject / skip) ──────────────────────
        public void ActionApproval(int approvalId, string action, string remarks,
                                   bool isConfirmed = false, int? delegatedBy = null)
        {
            OracleHelper.ExecuteNonQuerySql(@"
                UPDATE REQUEST_APPROVALS SET
                    ACTION                = :P_ACT,
                    REMARKS               = :P_REM,
                    IS_CONFIRMED          = :P_CONF,
                    DELEGATED_BY_USER_ID  = :P_DEL,
                    ACTIONED_AT           = SYSTIMESTAMP
                WHERE APPROVAL_ID = :P_ID",
                new[]
                {
                    OracleHelper.ParamStr("P_ACT",  action, 30),
                    OracleHelper.ParamStr("P_REM",  remarks, 2000),
                    OracleHelper.ParamBool("P_CONF",isConfirmed),
                    OracleHelper.ParamInt("P_DEL",  delegatedBy),
                    OracleHelper.ParamInt("P_ID",   approvalId)
                });
        }

        public List<RequestApproval> GetForRequest(int requestId)
        {
            var dt = OracleHelper.ExecuteQuerySql(@"
                SELECT ra.APPROVAL_ID, ra.REQUEST_ID, ra.STAGE_ID,
                       ws.STAGE_NAME, ws.STAGE_ORDER, ws.IS_CONFIRMATION_ONLY,
                       ra.APPROVER_USER_ID, u.FULL_NAME AS APPROVER_NAME,
                       ra.DELEGATED_BY_USER_ID, du.FULL_NAME AS DELEGATED_BY_NAME,
                       ra.ACTION, ra.REMARKS, ra.IS_CONFIRMED, ra.ACTIONED_AT,
                       ra.SEQUENCE_NUMBER
                FROM   REQUEST_APPROVALS ra
                JOIN   WORKFLOW_STAGES ws ON ws.STAGE_ID = ra.STAGE_ID
                JOIN   USERS u ON u.USER_ID = ra.APPROVER_USER_ID
                LEFT JOIN USERS du ON du.USER_ID = ra.DELEGATED_BY_USER_ID
                WHERE  ra.REQUEST_ID = :P_ID
                ORDER BY ra.SEQUENCE_NUMBER",
                new[] { OracleHelper.ParamInt("P_ID", requestId) });

            var list = new List<RequestApproval>();
            foreach (DataRow row in dt.Rows)
                list.Add(MapApproval(row));
            return list;
        }

        public RequestApproval GetPendingForApprover(int requestId, int approverUserId)
        {
            var dt = OracleHelper.ExecuteQuerySql(@"
                SELECT ra.APPROVAL_ID, ra.REQUEST_ID, ra.STAGE_ID,
                       ws.STAGE_NAME, ws.STAGE_ORDER, ws.IS_CONFIRMATION_ONLY,
                       ra.APPROVER_USER_ID, u.FULL_NAME AS APPROVER_NAME,
                       ra.DELEGATED_BY_USER_ID, NULL AS DELEGATED_BY_NAME,
                       ra.ACTION, ra.REMARKS, ra.IS_CONFIRMED, ra.ACTIONED_AT,
                       ra.SEQUENCE_NUMBER
                FROM   REQUEST_APPROVALS ra
                JOIN   WORKFLOW_STAGES ws ON ws.STAGE_ID = ra.STAGE_ID
                JOIN   USERS u ON u.USER_ID = ra.APPROVER_USER_ID
                WHERE  ra.REQUEST_ID = :P_R
                  AND  ra.APPROVER_USER_ID = :P_U
                  AND  ra.ACTION = 'PENDING'",
                new[]
                {
                    OracleHelper.ParamInt("P_R", requestId),
                    OracleHelper.ParamInt("P_U", approverUserId)
                });
            return dt.Rows.Count > 0 ? MapApproval(dt.Rows[0]) : null;
        }

        // ── Find the effective approver for a stage in a division ─────────────
        /// <summary>
        /// Returns the USER_ID of whoever should currently action this stage.
        /// Checks for active delegations first.  Returns null if nobody is assigned.
        /// </summary>
        public int? ResolveApprover(int roleId, int divisionId, int requestSubmitterId)
        {
            // Find users holding this role in this division (active)
            var candidates = OracleHelper.ExecuteQuerySql(@"
                SELECT ur.USER_ID
                FROM   USER_ROLES ur
                WHERE  ur.ROLE_ID = :P_ROLE
                  AND  (ur.DIVISION_ID = :P_DIV OR ur.DIVISION_ID IS NULL)
                  AND  ur.IS_ACTIVE = 1",
                new[]
                {
                    OracleHelper.ParamInt("P_ROLE", roleId),
                    OracleHelper.ParamInt("P_DIV", divisionId)
                });

            foreach (DataRow row in candidates.Rows)
            {
                int userId = OracleHelper.ToInt(row["USER_ID"]);

                // Skip if this person is the submitter (prevent self-approval)
                if (userId == requestSubmitterId) continue;

                // Check if they have an active delegation for this role
                var delegateeResult = OracleHelper.ExecuteScalarSql(@"
                    SELECT DELEGATEE_USER_ID FROM ROLE_DELEGATIONS
                    WHERE  DELEGATOR_USER_ID = :P_U
                      AND  ROLE_ID = :P_ROLE
                      AND  IS_ACTIVE = 1
                      AND  TRUNC(SYSDATE) BETWEEN START_DATE AND END_DATE
                      AND  ROWNUM = 1",
                    new[]
                    {
                        OracleHelper.ParamInt("P_U", userId),
                        OracleHelper.ParamInt("P_ROLE", roleId)
                    });

                if (delegateeResult != null && delegateeResult != DBNull.Value)
                    return Convert.ToInt32(delegateeResult);

                return userId;
            }

            return null; // Nobody assigned — stage will be auto-skipped
        }

        private RequestApproval MapApproval(DataRow row) => new RequestApproval
        {
            ApprovalId          = OracleHelper.ToInt(row["APPROVAL_ID"]),
            RequestId           = OracleHelper.ToInt(row["REQUEST_ID"]),
            StageId             = OracleHelper.ToInt(row["STAGE_ID"]),
            StageName           = OracleHelper.ToString(row["STAGE_NAME"]),
            StageOrder          = OracleHelper.ToInt(row["STAGE_ORDER"]),
            IsConfirmationOnly  = OracleHelper.ToBool(row["IS_CONFIRMATION_ONLY"]),
            ApproverUserId      = OracleHelper.ToInt(row["APPROVER_USER_ID"]),
            ApproverName        = OracleHelper.ToString(row["APPROVER_NAME"]),
            DelegatedByUserId   = OracleHelper.ToNullableInt(row["DELEGATED_BY_USER_ID"]),
            DelegatedByName     = OracleHelper.ToString(row["DELEGATED_BY_NAME"]),
            Action              = OracleHelper.ToString(row["ACTION"]),
            Remarks             = OracleHelper.ToString(row["REMARKS"]),
            IsConfirmed         = OracleHelper.ToBool(row["IS_CONFIRMED"]),
            ActionedAt          = OracleHelper.ToNullableDateTime(row["ACTIONED_AT"]),
            SequenceNumber      = OracleHelper.ToInt(row["SEQUENCE_NUMBER"])
        };
    }
}
