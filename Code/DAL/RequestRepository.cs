using System;
using System.Collections.Generic;
using System.Data;
using CRMP.Helpers;
using CRMP.Models;
using Oracle.ManagedDataAccess.Client;

namespace CRMP.DAL
{
    public class RequestRepository
    {
        // ── Create a new request ─────────────────────────────────────────────
        public int Create(Request r, string requestNumber)
        {
            int newId = OracleHelper.NextVal("SEQ_REQUESTS");
            OracleHelper.ExecuteNonQuerySql(@"
                INSERT INTO REQUESTS
                    (REQUEST_ID, REQUEST_NUMBER, TYPE_ID, SUBMITTER_USER_ID,
                     ON_BEHALF_OF_USER_ID, DIVISION_ID, STATUS, PRIORITY,
                     SUMMARY, SLA_DEADLINE, SUBMITTED_AT, CURRENT_STAGE_ID)
                VALUES
                    (:P_ID, :P_NUM, :P_TYPE, :P_SUBM,
                     :P_BEHALF, :P_DIV, 'PENDING_APPROVAL', :P_PRI,
                     :P_SUM, :P_SLA, SYSTIMESTAMP, :P_STAGE)",
                new[]
                {
                    OracleHelper.ParamInt("P_ID",    newId),
                    OracleHelper.ParamStr("P_NUM",   requestNumber, 30),
                    OracleHelper.ParamInt("P_TYPE",  r.TypeId),
                    OracleHelper.ParamInt("P_SUBM",  r.SubmitterUserId),
                    OracleHelper.ParamInt("P_BEHALF",r.OnBehalfOfUserId),
                    OracleHelper.ParamInt("P_DIV",   r.DivisionId),
                    OracleHelper.ParamStr("P_PRI",   r.Priority ?? "NORMAL", 20),
                    OracleHelper.ParamStr("P_SUM",   r.Summary, 1000),
                    OracleHelper.ParamDate("P_SLA",  r.SlaDeadline),
                    OracleHelper.ParamInt("P_STAGE", r.CurrentStageId)
                });
            return newId;
        }

        public Request GetById(int requestId)
        {
            var dt = OracleHelper.ExecuteQuerySql(BuildDetailQuery() + " WHERE r.REQUEST_ID = :P_ID",
                new[] { OracleHelper.ParamInt("P_ID", requestId) });
            return dt.Rows.Count > 0 ? MapRequest(dt.Rows[0]) : null;
        }

        public PagedResult<Request> GetPaged(RequestFilter filter)
        {
            var whereClauses = new List<string>();
            var parameters   = new List<OracleParameter>();
            BuildWhereClause(filter, whereClauses, parameters);

            string where = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";
            string orderBy = "ORDER BY r." + filter.SortBy + " " + filter.SortDir;
            int offset = (filter.PageNumber - 1) * filter.PageSize;

            // Count
            string countSql = "SELECT COUNT(*) FROM REQUESTS r JOIN REQUEST_TYPES rt ON rt.TYPE_ID=r.TYPE_ID " + where;
            int total = Convert.ToInt32(OracleHelper.ExecuteScalarSql(countSql, parameters.ToArray()));

            // Data (Oracle 12c+ OFFSET/FETCH)
            string dataSql = BuildDetailQuery() + " " + where + " " + orderBy + " OFFSET " + offset.ToString() + " ROWS FETCH NEXT " + filter.PageSize.ToString() + " ROWS ONLY";
            var dt = OracleHelper.ExecuteQuerySql(dataSql, parameters.ToArray());

            var items = new List<Request>();
            foreach (DataRow row in dt.Rows)
                items.Add(MapRequest(row));

            return new PagedResult<Request>
            {
                Items = items, TotalCount = total,
                PageNumber = filter.PageNumber, PageSize = filter.PageSize
            };
        }

        // ── Approver inbox: requests where current pending approval is for this user ──
        public List<Request> GetPendingForApprover(int approverUserId)
        {
            var dt = OracleHelper.ExecuteQuerySql(BuildDetailQuery() + @"
                WHERE EXISTS (
                    SELECT 1 FROM REQUEST_APPROVALS ra2
                    WHERE ra2.REQUEST_ID = r.REQUEST_ID
                      AND ra2.APPROVER_USER_ID = :P_UID
                      AND ra2.ACTION = 'PENDING'
                )
                ORDER BY r.SLA_DEADLINE ASC NULLS LAST",
                new[] { OracleHelper.ParamInt("P_UID", approverUserId) });

            var list = new List<Request>();
            foreach (DataRow row in dt.Rows)
                list.Add(MapRequest(row));
            return list;
        }

        // ── Tech Expert pool (approved, not yet claimed, in their categories) ─
        public List<Request> GetTechExpertPool(int techExpertUserId)
        {
            var dt = OracleHelper.ExecuteQuerySql(BuildDetailQuery() + @"
                WHERE r.STATUS = 'APPROVED'
                  AND r.TECH_EXPERT_ID IS NULL
                  AND rt.CATEGORY_ID IN (
                      SELECT CATEGORY_ID FROM TECH_EXPERT_CATEGORIES
                      WHERE USER_ID = :P_UID AND IS_ACTIVE = 1
                  )
                ORDER BY r.SLA_DEADLINE ASC NULLS LAST",
                new[] { OracleHelper.ParamInt("P_UID", techExpertUserId) });

            var list = new List<Request>();
            foreach (DataRow row in dt.Rows)
                list.Add(MapRequest(row));
            return list;
        }

        public List<Request> GetMyActiveWork(int techExpertUserId)
        {
            var dt = OracleHelper.ExecuteQuerySql(BuildDetailQuery() + @"
                WHERE r.TECH_EXPERT_ID = :P_UID AND r.STATUS = 'IN_PROGRESS'
                ORDER BY r.SLA_DEADLINE ASC NULLS LAST",
                new[] { OracleHelper.ParamInt("P_UID", techExpertUserId) });

            var list = new List<Request>();
            foreach (DataRow row in dt.Rows)
                list.Add(MapRequest(row));
            return list;
        }

        // ── Status transitions ────────────────────────────────────────────────
        public void UpdateStatus(int requestId, string status)
        {
            OracleHelper.ExecuteNonQuerySql(@"
                UPDATE REQUESTS SET STATUS = :P_STATUS, UPDATED_AT = SYSTIMESTAMP
                WHERE REQUEST_ID = :P_ID",
                new[]
                {
                    OracleHelper.ParamStr("P_STATUS", status, 30),
                    OracleHelper.ParamInt("P_ID", requestId)
                });
        }

        public void AssignTechExpert(int requestId, int techExpertId)
        {
            OracleHelper.ExecuteNonQuerySql(@"
                UPDATE REQUESTS SET
                    TECH_EXPERT_ID = :P_TE,
                    STATUS = 'IN_PROGRESS',
                    UPDATED_AT = SYSTIMESTAMP
                WHERE REQUEST_ID = :P_ID",
                new[]
                {
                    OracleHelper.ParamInt("P_TE", techExpertId),
                    OracleHelper.ParamInt("P_ID", requestId)
                });
        }

        public void MarkResolved(int requestId, string resolutionNotes)
        {
            OracleHelper.ExecuteNonQuerySql(@"
                UPDATE REQUESTS SET
                    STATUS = 'RESOLVED',
                    RESOLUTION_NOTES = :P_NOTES,
                    RESOLVED_AT = SYSTIMESTAMP,
                    UPDATED_AT  = SYSTIMESTAMP
                WHERE REQUEST_ID = :P_ID",
                new[]
                {
                    OracleHelper.ParamClob("P_NOTES", resolutionNotes),
                    OracleHelper.ParamInt("P_ID", requestId)
                });
        }

        public void MarkClosed(int requestId)
        {
            OracleHelper.ExecuteNonQuerySql(@"
                UPDATE REQUESTS SET
                    STATUS = 'CLOSED',
                    CLOSED_AT = SYSTIMESTAMP,
                    UPDATED_AT = SYSTIMESTAMP
                WHERE REQUEST_ID = :P_ID",
                new[] { OracleHelper.ParamInt("P_ID", requestId) });
        }

        public void UpdateCurrentStage(int requestId, int? stageId)
        {
            OracleHelper.ExecuteNonQuerySql(@"
                UPDATE REQUESTS SET CURRENT_STAGE_ID = :P_STAGE, UPDATED_AT = SYSTIMESTAMP
                WHERE REQUEST_ID = :P_ID",
                new[]
                {
                    OracleHelper.ParamInt("P_STAGE", stageId),
                    OracleHelper.ParamInt("P_ID", requestId)
                });
        }

        public void MarkSlaBreached(int requestId)
        {
            OracleHelper.ExecuteNonQuerySql(@"
                UPDATE REQUESTS SET IS_SLA_BREACHED = 1, SLA_BREACH_NOTIFIED = 0,
                                    UPDATED_AT = SYSTIMESTAMP
                WHERE REQUEST_ID = :P_ID AND IS_SLA_BREACHED = 0",
                new[] { OracleHelper.ParamInt("P_ID", requestId) });
        }

        // ── Field values ──────────────────────────────────────────────────────
        public void SaveFieldValue(int requestId, int fieldId, string value, string clobValue = null)
        {
            // Upsert
            var exists = OracleHelper.ExecuteScalarSql(
                "SELECT COUNT(*) FROM REQUEST_FIELD_VALUES WHERE REQUEST_ID=:P_R AND FIELD_ID=:P_F",
                new[] { OracleHelper.ParamInt("P_R", requestId), OracleHelper.ParamInt("P_F", fieldId) });

            if (Convert.ToInt32(exists) > 0)
            {
                OracleHelper.ExecuteNonQuerySql(@"
                    UPDATE REQUEST_FIELD_VALUES SET FIELD_VALUE=:P_V, FIELD_VALUE_CLOB=:P_C
                    WHERE REQUEST_ID=:P_R AND FIELD_ID=:P_F",
                    new[]
                    {
                        OracleHelper.ParamStr("P_V", value),
                        OracleHelper.ParamClob("P_C", clobValue),
                        OracleHelper.ParamInt("P_R", requestId),
                        OracleHelper.ParamInt("P_F", fieldId)
                    });
            }
            else
            {
                int newId = OracleHelper.NextVal("SEQ_RFV");
                OracleHelper.ExecuteNonQuerySql(@"
                    INSERT INTO REQUEST_FIELD_VALUES (VALUE_ID, REQUEST_ID, FIELD_ID, FIELD_VALUE, FIELD_VALUE_CLOB)
                    VALUES (:P_ID, :P_R, :P_F, :P_V, :P_C)",
                    new[]
                    {
                        OracleHelper.ParamInt("P_ID", newId),
                        OracleHelper.ParamInt("P_R", requestId),
                        OracleHelper.ParamInt("P_F", fieldId),
                        OracleHelper.ParamStr("P_V", value),
                        OracleHelper.ParamClob("P_C", clobValue)
                    });
            }
        }

        public List<RequestFieldValue> GetFieldValues(int requestId)
        {
            var dt = OracleHelper.ExecuteQuerySql(@"
                SELECT rfv.VALUE_ID, rfv.REQUEST_ID, rfv.FIELD_ID,
                       ff.FIELD_LABEL, ff.FIELD_TYPE, rfv.FIELD_VALUE, rfv.FIELD_VALUE_CLOB
                FROM   REQUEST_FIELD_VALUES rfv
                JOIN   FORM_FIELDS ff ON ff.FIELD_ID = rfv.FIELD_ID
                WHERE  rfv.REQUEST_ID = :P_ID
                ORDER BY ff.SORT_ORDER",
                new[] { OracleHelper.ParamInt("P_ID", requestId) });

            var list = new List<RequestFieldValue>();
            foreach (DataRow row in dt.Rows)
                list.Add(new RequestFieldValue
                {
                    ValueId      = OracleHelper.ToInt(row["VALUE_ID"]),
                    RequestId    = OracleHelper.ToInt(row["REQUEST_ID"]),
                    FieldId      = OracleHelper.ToInt(row["FIELD_ID"]),
                    FieldLabel   = OracleHelper.ToString(row["FIELD_LABEL"]),
                    FieldType    = OracleHelper.ToString(row["FIELD_TYPE"]),
                    FieldValue   = OracleHelper.ToString(row["FIELD_VALUE"]),
                    FieldValueClob = OracleHelper.ToString(row["FIELD_VALUE_CLOB"])
                });
            return list;
        }

        // ── Attachments ───────────────────────────────────────────────────────
        public void SaveAttachment(RequestAttachment att)
        {
            int newId = OracleHelper.NextVal("SEQ_ATTACHMENTS");
            OracleHelper.ExecuteNonQuerySql(@"
                INSERT INTO REQUEST_ATTACHMENTS
                    (ATTACHMENT_ID, REQUEST_ID, FILE_NAME, FILE_PATH, FILE_SIZE, MIME_TYPE, UPLOADED_BY)
                VALUES (:P_ID, :P_R, :P_FN, :P_FP, :P_FS, :P_MT, :P_UB)",
                new[]
                {
                    OracleHelper.ParamInt("P_ID", newId),
                    OracleHelper.ParamInt("P_R",  att.RequestId),
                    OracleHelper.ParamStr("P_FN", att.FileName, 500),
                    OracleHelper.ParamStr("P_FP", att.FilePath, 1000),
                    OracleHelper.Param("P_FS", Oracle.ManagedDataAccess.Client.OracleDbType.Int64, att.FileSize),
                    OracleHelper.ParamStr("P_MT", att.MimeType, 200),
                    OracleHelper.ParamInt("P_UB", att.UploadedBy)
                });
        }

        public List<RequestAttachment> GetAttachments(int requestId)
        {
            var dt = OracleHelper.ExecuteQuerySql(@"
                SELECT a.*, u.FULL_NAME AS UPLOADER_NAME
                FROM   REQUEST_ATTACHMENTS a
                JOIN   USERS u ON u.USER_ID = a.UPLOADED_BY
                WHERE  a.REQUEST_ID = :P_ID",
                new[] { OracleHelper.ParamInt("P_ID", requestId) });

            var list = new List<RequestAttachment>();
            foreach (DataRow row in dt.Rows)
                list.Add(new RequestAttachment
                {
                    AttachmentId = OracleHelper.ToInt(row["ATTACHMENT_ID"]),
                    RequestId    = OracleHelper.ToInt(row["REQUEST_ID"]),
                    FileName     = OracleHelper.ToString(row["FILE_NAME"]),
                    FilePath     = OracleHelper.ToString(row["FILE_PATH"]),
                    FileSize     = OracleHelper.ToNullableInt(row["FILE_SIZE"]),
                    MimeType     = OracleHelper.ToString(row["MIME_TYPE"]),
                    UploadedBy   = OracleHelper.ToInt(row["UPLOADED_BY"]),
                    UploaderName = OracleHelper.ToString(row["UPLOADER_NAME"]),
                    UploadedAt   = OracleHelper.ToDateTime(row["UPLOADED_AT"])
                });
            return list;
        }

        // ── Timeline ──────────────────────────────────────────────────────────
        public void AddTimeline(int requestId, string eventType, string desc, int? performedBy, string meta = null)
        {
            int newId = OracleHelper.NextVal("SEQ_TIMELINE");
            OracleHelper.ExecuteNonQuerySql(@"
                INSERT INTO REQUEST_TIMELINE
                    (TIMELINE_ID, REQUEST_ID, EVENT_TYPE, EVENT_DESC, PERFORMED_BY, METADATA_JSON)
                VALUES (:P_ID, :P_R, :P_TYPE, :P_DESC, :P_BY, :P_META)",
                new[]
                {
                    OracleHelper.ParamInt("P_ID",  newId),
                    OracleHelper.ParamInt("P_R",   requestId),
                    OracleHelper.ParamStr("P_TYPE",eventType, 50),
                    OracleHelper.ParamStr("P_DESC",desc, 2000),
                    OracleHelper.ParamInt("P_BY",  performedBy),
                    OracleHelper.ParamClob("P_META", meta)
                });
        }

        public List<TimelineEvent> GetTimeline(int requestId)
        {
            var dt = OracleHelper.ExecuteQuerySql(@"
                SELECT tl.*, u.FULL_NAME AS PERFORMED_BY_NAME
                FROM   REQUEST_TIMELINE tl
                LEFT JOIN USERS u ON u.USER_ID = tl.PERFORMED_BY
                WHERE  tl.REQUEST_ID = :P_ID
                ORDER BY tl.PERFORMED_AT ASC",
                new[] { OracleHelper.ParamInt("P_ID", requestId) });

            var list = new List<TimelineEvent>();
            foreach (DataRow row in dt.Rows)
                list.Add(new TimelineEvent
                {
                    TimelineId      = OracleHelper.ToInt(row["TIMELINE_ID"]),
                    RequestId       = OracleHelper.ToInt(row["REQUEST_ID"]),
                    EventType       = OracleHelper.ToString(row["EVENT_TYPE"]),
                    EventDesc       = OracleHelper.ToString(row["EVENT_DESC"]),
                    PerformedBy     = OracleHelper.ToNullableInt(row["PERFORMED_BY"]),
                    PerformedByName = OracleHelper.ToString(row["PERFORMED_BY_NAME"]),
                    PerformedAt     = OracleHelper.ToDateTime(row["PERFORMED_AT"]),
                    MetadataJson    = OracleHelper.ToString(row["METADATA_JSON"])
                });
            return list;
        }

        // ── SLA-related ───────────────────────────────────────────────────────
        public List<Request> GetSlaAtRisk(int warnThresholdPct = 75)
        {
            var dt = OracleHelper.ExecuteQuerySql(BuildDetailQuery() + @"
                WHERE r.STATUS NOT IN ('RESOLVED','CLOSED','REJECTED','CANCELLED')
                  AND r.SLA_DEADLINE IS NOT NULL
                  AND r.IS_SLA_BREACHED = 0
                  AND ((CAST(SYSTIMESTAMP AS DATE) - CAST(r.SUBMITTED_AT AS DATE)) * 24)
                       / ((CAST(r.SLA_DEADLINE AS DATE) - CAST(r.SUBMITTED_AT AS DATE)) * 24) * 100
                       >= :P_PCT
                ORDER BY r.SLA_DEADLINE ASC");

            var list = new List<Request>();
            foreach (DataRow row in dt.Rows)
                list.Add(MapRequest(row));
            return list;
        }

        public List<int> GetBreachedRequestIds()
        {
            var dt = OracleHelper.ExecuteQuerySql(@"
                SELECT REQUEST_ID FROM REQUESTS
                WHERE  SLA_DEADLINE < SYSTIMESTAMP
                  AND  IS_SLA_BREACHED = 0
                  AND  STATUS NOT IN ('RESOLVED','CLOSED','REJECTED','CANCELLED')");

            var ids = new List<int>();
            foreach (DataRow row in dt.Rows)
                ids.Add(OracleHelper.ToInt(row["REQUEST_ID"]));
            return ids;
        }

        // ── Dashboard / reporting ─────────────────────────────────────────────
        public DataTable GetStatusSummary(int? divisionId = null, int? categoryId = null)
        {
            string where = "WHERE 1=1";
            var parms = new List<OracleParameter>();
            if (divisionId.HasValue) { where += " AND r.DIVISION_ID = :P_D"; parms.Add(OracleHelper.ParamInt("P_D", divisionId)); }
            if (categoryId.HasValue) { where += " AND rt.CATEGORY_ID = :P_C"; parms.Add(OracleHelper.ParamInt("P_C", categoryId)); }

            return OracleHelper.ExecuteQuerySql(@"
                SELECT r.STATUS, COUNT(*) AS CNT
                FROM   REQUESTS r
                JOIN   REQUEST_TYPES rt ON rt.TYPE_ID = r.TYPE_ID
                " + where + @"
                GROUP BY r.STATUS", parms.ToArray());
        }

        public DataTable GetRequestsByCategory(int? divisionId = null)
        {
            var parms = new List<OracleParameter>();
            string where = divisionId.HasValue ? "WHERE r.DIVISION_ID = :P_D" : "";
            if (divisionId.HasValue) parms.Add(OracleHelper.ParamInt("P_D", divisionId));

            return OracleHelper.ExecuteQuerySql(@"
                SELECT sc.CATEGORY_NAME, sc.COLOR_HEX, COUNT(r.REQUEST_ID) AS CNT
                FROM   SERVICE_CATEGORIES sc
                LEFT JOIN REQUEST_TYPES rt ON rt.CATEGORY_ID = sc.CATEGORY_ID
                LEFT JOIN REQUESTS r ON r.TYPE_ID = rt.TYPE_ID " + where + @"
                GROUP BY sc.CATEGORY_NAME, sc.COLOR_HEX, sc.SORT_ORDER
                ORDER BY sc.SORT_ORDER", parms.ToArray());
        }

        public DataTable GetRequestsPerDay(int days = 30, int? categoryId = null)
        {
            var parms = new List<OracleParameter> { OracleHelper.ParamInt("P_DAYS", days) };
            string catWhere = categoryId.HasValue ? "AND rt.CATEGORY_ID = :P_C" : "";
            if (categoryId.HasValue) parms.Add(OracleHelper.ParamInt("P_C", categoryId));

            return OracleHelper.ExecuteQuerySql(@"
                SELECT TRUNC(r.SUBMITTED_AT) AS REQ_DATE, COUNT(*) AS CNT
                FROM   REQUESTS r
                JOIN   REQUEST_TYPES rt ON rt.TYPE_ID = r.TYPE_ID
                WHERE  r.SUBMITTED_AT >= SYSTIMESTAMP - :P_DAYS " + catWhere + @"
                GROUP BY TRUNC(r.SUBMITTED_AT)
                ORDER BY TRUNC(r.SUBMITTED_AT)", parms.ToArray());
        }

        // ── Rating ────────────────────────────────────────────────────────────
        public void SaveRating(int requestId, int userId, int score, string comment)
        {
            int newId = OracleHelper.NextVal("SEQ_RATINGS");
            OracleHelper.ExecuteNonQuerySql(@"
                INSERT INTO REQUEST_RATINGS (RATING_ID, REQUEST_ID, RATED_BY_USER_ID, SCORE, COMMENT)
                VALUES (:P_ID, :P_R, :P_U, :P_SCORE, :P_COMM)",
                new[]
                {
                    OracleHelper.ParamInt("P_ID",    newId),
                    OracleHelper.ParamInt("P_R",     requestId),
                    OracleHelper.ParamInt("P_U",     userId),
                    OracleHelper.ParamInt("P_SCORE", score),
                    OracleHelper.ParamStr("P_COMM",  comment, 1000)
                });
        }

        // ── Private helpers ───────────────────────────────────────────────────
        private static string BuildDetailQuery()
        {
            return @"
            SELECT r.REQUEST_ID, r.REQUEST_NUMBER, r.TYPE_ID, rt.TYPE_NAME, rt.TYPE_CODE,
                   sc.CATEGORY_ID, sc.CATEGORY_NAME, sc.ICON_CLASS AS CATEGORY_ICON, sc.COLOR_HEX,
                   r.SUBMITTER_USER_ID, su.FULL_NAME AS SUBMITTER_NAME,
                   r.ON_BEHALF_OF_USER_ID, ob.FULL_NAME AS ONBEHALF_NAME,
                   r.DIVISION_ID, d.DIVISION_NAME,
                   r.STATUS, r.CURRENT_STAGE_ID, ws.STAGE_NAME AS CURRENT_STAGE_NAME,
                   r.TECH_EXPERT_ID, te.FULL_NAME AS TECH_EXPERT_NAME,
                   r.SUMMARY, r.PRIORITY,
                   r.SLA_DEADLINE, r.IS_SLA_BREACHED,
                   r.SUBMITTED_AT, r.RESOLVED_AT, r.CLOSED_AT, r.RESOLUTION_NOTES,
                   r.UPDATED_AT
            FROM   REQUESTS r
            JOIN   REQUEST_TYPES rt ON rt.TYPE_ID = r.TYPE_ID
            JOIN   SERVICE_CATEGORIES sc ON sc.CATEGORY_ID = rt.CATEGORY_ID
            JOIN   USERS su ON su.USER_ID = r.SUBMITTER_USER_ID
            JOIN   DIVISIONS d ON d.DIVISION_ID = r.DIVISION_ID
            LEFT JOIN USERS ob ON ob.USER_ID = r.ON_BEHALF_OF_USER_ID
            LEFT JOIN WORKFLOW_STAGES ws ON ws.STAGE_ID = r.CURRENT_STAGE_ID
            LEFT JOIN USERS te ON te.USER_ID = r.TECH_EXPERT_ID";
        }

        private static void BuildWhereClause(RequestFilter f, List<string> clauses, List<OracleParameter> parms)
        {
            if (!string.IsNullOrEmpty(f.Status))
            { clauses.Add("r.STATUS = :P_STATUS"); parms.Add(OracleHelper.ParamStr("P_STATUS", f.Status, 30)); }
            if (f.CategoryId.HasValue)
            { clauses.Add("sc.CATEGORY_ID = :P_CAT"); parms.Add(OracleHelper.ParamInt("P_CAT", f.CategoryId)); }
            if (f.TypeId.HasValue)
            { clauses.Add("r.TYPE_ID = :P_TYPE"); parms.Add(OracleHelper.ParamInt("P_TYPE", f.TypeId)); }
            if (f.DivisionId.HasValue)
            { clauses.Add("r.DIVISION_ID = :P_DIV"); parms.Add(OracleHelper.ParamInt("P_DIV", f.DivisionId)); }
            if (f.SubmitterUserId.HasValue)
            { clauses.Add("r.SUBMITTER_USER_ID = :P_SUBM"); parms.Add(OracleHelper.ParamInt("P_SUBM", f.SubmitterUserId)); }
            if (f.TechExpertId.HasValue)
            { clauses.Add("r.TECH_EXPERT_ID = :P_TE"); parms.Add(OracleHelper.ParamInt("P_TE", f.TechExpertId)); }
            if (!string.IsNullOrEmpty(f.Priority))
            { clauses.Add("r.PRIORITY = :P_PRI"); parms.Add(OracleHelper.ParamStr("P_PRI", f.Priority, 20)); }
            if (f.DateFrom.HasValue)
            { clauses.Add("r.SUBMITTED_AT >= :P_FROM"); parms.Add(OracleHelper.ParamDate("P_FROM", f.DateFrom)); }
            if (f.DateTo.HasValue)
            { clauses.Add("r.SUBMITTED_AT <= :P_TO"); parms.Add(OracleHelper.ParamDate("P_TO", f.DateTo.Value.AddDays(1))); }
            if (f.SlaBreached.HasValue)
            { clauses.Add("r.IS_SLA_BREACHED = " + (f.SlaBreached.Value ? "1" : "0")); }
            if (!string.IsNullOrEmpty(f.SearchTerm))
            { clauses.Add("(UPPER(r.REQUEST_NUMBER) LIKE '%'||UPPER(:P_Q)||'%' OR UPPER(r.SUMMARY) LIKE '%'||UPPER(:P_Q)||'%')");
              parms.Add(OracleHelper.ParamStr("P_Q", f.SearchTerm)); }
        }

        private static Request MapRequest(DataRow row)
        {
            return new Request
        {
            RequestId         = OracleHelper.ToInt(row["REQUEST_ID"]),
            RequestNumber     = OracleHelper.ToString(row["REQUEST_NUMBER"]),
            TypeId            = OracleHelper.ToInt(row["TYPE_ID"]),
            TypeName          = OracleHelper.ToString(row["TYPE_NAME"]),
            TypeCode          = OracleHelper.ToString(row["TYPE_CODE"]),
            CategoryId        = OracleHelper.ToInt(row["CATEGORY_ID"]),
            CategoryName      = OracleHelper.ToString(row["CATEGORY_NAME"]),
            CategoryIconClass = OracleHelper.ToString(row["CATEGORY_ICON"]),
            CategoryColorHex  = OracleHelper.ToString(row["COLOR_HEX"]),
            SubmitterUserId   = OracleHelper.ToInt(row["SUBMITTER_USER_ID"]),
            SubmitterName     = OracleHelper.ToString(row["SUBMITTER_NAME"]),
            OnBehalfOfUserId  = OracleHelper.ToNullableInt(row["ON_BEHALF_OF_USER_ID"]),
            OnBehalfOfName    = OracleHelper.ToString(row["ONBEHALF_NAME"]),
            DivisionId        = OracleHelper.ToInt(row["DIVISION_ID"]),
            DivisionName      = OracleHelper.ToString(row["DIVISION_NAME"]),
            Status            = OracleHelper.ToString(row["STATUS"]),
            CurrentStageId    = OracleHelper.ToNullableInt(row["CURRENT_STAGE_ID"]),
            CurrentStageName  = OracleHelper.ToString(row["CURRENT_STAGE_NAME"]),
            TechExpertId      = OracleHelper.ToNullableInt(row["TECH_EXPERT_ID"]),
            TechExpertName    = OracleHelper.ToString(row["TECH_EXPERT_NAME"]),
            Summary           = OracleHelper.ToString(row["SUMMARY"]),
            Priority          = OracleHelper.ToString(row["PRIORITY"]),
            SlaDeadline       = OracleHelper.ToNullableDateTime(row["SLA_DEADLINE"]),
            IsSlaBreached     = OracleHelper.ToBool(row["IS_SLA_BREACHED"]),
            SubmittedAt       = OracleHelper.ToDateTime(row["SUBMITTED_AT"]),
            ResolvedAt        = OracleHelper.ToNullableDateTime(row["RESOLVED_AT"]),
            ClosedAt          = OracleHelper.ToNullableDateTime(row["CLOSED_AT"]),
            ResolutionNotes   = OracleHelper.ToString(row["RESOLUTION_NOTES"]),
            UpdatedAt         = OracleHelper.ToDateTime(row["UPDATED_AT"])
        };
        }
    }
}
