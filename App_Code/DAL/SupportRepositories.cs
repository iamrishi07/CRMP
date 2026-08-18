using System;
using System.Collections.Generic;
using System.Data;
using CRMP.Helpers;
using CRMP.Models;

namespace CRMP.DAL
{
    public class NotificationRepository
    {
        public void Create(int userId, string title, string message, string link, string notifType)
        {
            int newId = OracleHelper.NextVal("SEQ_NOTIFICATIONS");
            OracleHelper.ExecuteNonQuerySql(@"
                INSERT INTO NOTIFICATIONS (NOTIF_ID, USER_ID, TITLE, MESSAGE, LINK, NOTIF_TYPE)
                VALUES (:P_ID, :P_U, :P_T, :P_M, :P_L, :P_TYPE)",
                new[]
                {
                    OracleHelper.ParamInt("P_ID",   newId),
                    OracleHelper.ParamInt("P_U",    userId),
                    OracleHelper.ParamStr("P_T",    title, 300),
                    OracleHelper.ParamStr("P_M",    message, 2000),
                    OracleHelper.ParamStr("P_L",    link, 500),
                    OracleHelper.ParamStr("P_TYPE", notifType, 50)
                });
        }

        public List<Notification> GetUnread(int userId, int limit = 15)
        {
            var dt = OracleHelper.ExecuteQuerySql($@"
                SELECT * FROM NOTIFICATIONS
                WHERE USER_ID = :P_U AND IS_READ = 0
                ORDER BY CREATED_AT DESC
                FETCH FIRST {limit} ROWS ONLY",
                new[] { OracleHelper.ParamInt("P_U", userId) });

            var list = new List<Notification>();
            foreach (DataRow row in dt.Rows)
                list.Add(MapNotif(row));
            return list;
        }

        public int GetUnreadCount(int userId)
        {
            var result = OracleHelper.ExecuteScalarSql(
                "SELECT COUNT(*) FROM NOTIFICATIONS WHERE USER_ID=:P_U AND IS_READ=0",
                new[] { OracleHelper.ParamInt("P_U", userId) });
            return Convert.ToInt32(result);
        }

        public void MarkRead(int notifId)
        {
            OracleHelper.ExecuteNonQuerySql(
                "UPDATE NOTIFICATIONS SET IS_READ=1 WHERE NOTIF_ID=:P_ID",
                new[] { OracleHelper.ParamInt("P_ID", notifId) });
        }

        public void MarkAllRead(int userId)
        {
            OracleHelper.ExecuteNonQuerySql(
                "UPDATE NOTIFICATIONS SET IS_READ=1 WHERE USER_ID=:P_U",
                new[] { OracleHelper.ParamInt("P_U", userId) });
        }

        public List<Notification> GetAll(int userId, int page = 1, int pageSize = 30)
        {
            int offset = (page - 1) * pageSize;
            var dt = OracleHelper.ExecuteQuerySql($@"
                SELECT * FROM NOTIFICATIONS WHERE USER_ID=:P_U
                ORDER BY CREATED_AT DESC
                OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY",
                new[] { OracleHelper.ParamInt("P_U", userId) });

            var list = new List<Notification>();
            foreach (DataRow row in dt.Rows)
                list.Add(MapNotif(row));
            return list;
        }

        public NotificationPref GetPref(int userId, string notifType)
        {
            var dt = OracleHelper.ExecuteQuerySql(@"
                SELECT * FROM USER_NOTIFICATION_PREFS WHERE USER_ID=:P_U AND NOTIF_TYPE=:P_T",
                new[] { OracleHelper.ParamInt("P_U", userId), OracleHelper.ParamStr("P_T", notifType, 50) });

            if (dt.Rows.Count == 0)
                return new NotificationPref { UserId = userId, NotifType = notifType, EmailEnabled = true, InAppEnabled = true };

            return new NotificationPref
            {
                PrefId        = OracleHelper.ToInt(dt.Rows[0]["PREF_ID"]),
                UserId        = OracleHelper.ToInt(dt.Rows[0]["USER_ID"]),
                NotifType     = OracleHelper.ToString(dt.Rows[0]["NOTIF_TYPE"]),
                EmailEnabled  = OracleHelper.ToBool(dt.Rows[0]["EMAIL_ENABLED"]),
                InAppEnabled  = OracleHelper.ToBool(dt.Rows[0]["INAPP_ENABLED"])
            };
        }

        public void SavePref(int userId, string notifType, bool emailEnabled, bool inAppEnabled)
        {
            var exists = OracleHelper.ExecuteScalarSql(
                "SELECT COUNT(*) FROM USER_NOTIFICATION_PREFS WHERE USER_ID=:P_U AND NOTIF_TYPE=:P_T",
                new[] { OracleHelper.ParamInt("P_U", userId), OracleHelper.ParamStr("P_T", notifType, 50) });

            if (Convert.ToInt32(exists) > 0)
            {
                OracleHelper.ExecuteNonQuerySql(@"
                    UPDATE USER_NOTIFICATION_PREFS
                    SET EMAIL_ENABLED=:P_E, INAPP_ENABLED=:P_I
                    WHERE USER_ID=:P_U AND NOTIF_TYPE=:P_T",
                    new[]
                    {
                        OracleHelper.ParamBool("P_E", emailEnabled),
                        OracleHelper.ParamBool("P_I", inAppEnabled),
                        OracleHelper.ParamInt("P_U",  userId),
                        OracleHelper.ParamStr("P_T",  notifType, 50)
                    });
            }
            else
            {
                int newId = OracleHelper.NextVal("SEQ_NOTIF_PREFS");
                OracleHelper.ExecuteNonQuerySql(@"
                    INSERT INTO USER_NOTIFICATION_PREFS (PREF_ID, USER_ID, NOTIF_TYPE, EMAIL_ENABLED, INAPP_ENABLED)
                    VALUES (:P_ID, :P_U, :P_T, :P_E, :P_I)",
                    new[]
                    {
                        OracleHelper.ParamInt("P_ID", newId),
                        OracleHelper.ParamInt("P_U",  userId),
                        OracleHelper.ParamStr("P_T",  notifType, 50),
                        OracleHelper.ParamBool("P_E", emailEnabled),
                        OracleHelper.ParamBool("P_I", inAppEnabled)
                    });
            }
        }

        private Notification MapNotif(DataRow row) => new Notification
        {
            NotifId   = OracleHelper.ToInt(row["NOTIF_ID"]),
            UserId    = OracleHelper.ToInt(row["USER_ID"]),
            Title     = OracleHelper.ToString(row["TITLE"]),
            Message   = OracleHelper.ToString(row["MESSAGE"]),
            Link      = OracleHelper.ToString(row["LINK"]),
            IsRead    = OracleHelper.ToBool(row["IS_READ"]),
            NotifType = OracleHelper.ToString(row["NOTIF_TYPE"]),
            CreatedAt = OracleHelper.ToDateTime(row["CREATED_AT"])
        };
    }

    // ────────────────────────────────────────────────────────────────────────────
    public class KnowledgeBaseRepository
    {
        public List<KbArticle> Search(string query, int? categoryId = null, int limit = 5)
        {
            var parms = new List<OracleParameter> { OracleHelper.ParamStr("P_Q", query) };
            string catWhere = categoryId.HasValue ? "AND a.CATEGORY_ID=:P_CAT" : "";
            if (categoryId.HasValue) parms.Add(OracleHelper.ParamInt("P_CAT", categoryId));

            var dt = OracleHelper.ExecuteQuerySql($@"
                SELECT a.ARTICLE_ID, a.TITLE, a.CATEGORY_ID, sc.CATEGORY_NAME,
                       a.TYPE_ID, NULL AS TYPE_NAME, a.AUTHOR_ID, u.FULL_NAME AS AUTHOR_NAME,
                       a.PUBLISHED_AT, a.VIEW_COUNT, a.IS_PUBLISHED, a.TAGS,
                       a.CREATED_AT, a.UPDATED_AT, NULL AS CONTENT_HTML
                FROM   KB_ARTICLES a
                JOIN   USERS u ON u.USER_ID = a.AUTHOR_ID
                LEFT JOIN SERVICE_CATEGORIES sc ON sc.CATEGORY_ID = a.CATEGORY_ID
                WHERE  a.IS_PUBLISHED = 1
                  AND  (UPPER(a.TITLE) LIKE '%'||UPPER(:P_Q)||'%'
                     OR UPPER(a.TAGS)  LIKE '%'||UPPER(:P_Q)||'%') {catWhere}
                ORDER BY a.VIEW_COUNT DESC
                FETCH FIRST {limit} ROWS ONLY", parms.ToArray());

            var list = new List<KbArticle>();
            foreach (DataRow row in dt.Rows)
                list.Add(MapArticle(row));
            return list;
        }

        public KbArticle GetById(int articleId)
        {
            var dt = OracleHelper.ExecuteQuerySql(@"
                SELECT a.*, u.FULL_NAME AS AUTHOR_NAME, sc.CATEGORY_NAME
                FROM   KB_ARTICLES a
                JOIN   USERS u ON u.USER_ID = a.AUTHOR_ID
                LEFT JOIN SERVICE_CATEGORIES sc ON sc.CATEGORY_ID = a.CATEGORY_ID
                WHERE  a.ARTICLE_ID = :P_ID",
                new[] { OracleHelper.ParamInt("P_ID", articleId) });

            if (dt.Rows.Count == 0) return null;
            var art = MapArticle(dt.Rows[0]);
            art.ContentHtml = OracleHelper.ToString(dt.Rows[0]["CONTENT_HTML"]);
            // Increment view count
            OracleHelper.ExecuteNonQuerySql(
                "UPDATE KB_ARTICLES SET VIEW_COUNT = VIEW_COUNT + 1 WHERE ARTICLE_ID = :P_ID",
                new[] { OracleHelper.ParamInt("P_ID", articleId) });
            return art;
        }

        public List<KbArticle> GetAll(bool publishedOnly = true)
        {
            string where = publishedOnly ? "WHERE a.IS_PUBLISHED=1" : "";
            var dt = OracleHelper.ExecuteQuerySql($@"
                SELECT a.ARTICLE_ID, a.TITLE, a.CATEGORY_ID, sc.CATEGORY_NAME,
                       a.TYPE_ID, NULL AS TYPE_NAME, a.AUTHOR_ID, u.FULL_NAME AS AUTHOR_NAME,
                       a.PUBLISHED_AT, a.VIEW_COUNT, a.IS_PUBLISHED, a.TAGS,
                       a.CREATED_AT, a.UPDATED_AT, NULL AS CONTENT_HTML
                FROM   KB_ARTICLES a
                JOIN   USERS u ON u.USER_ID = a.AUTHOR_ID
                LEFT JOIN SERVICE_CATEGORIES sc ON sc.CATEGORY_ID = a.CATEGORY_ID
                {where}
                ORDER BY a.PUBLISHED_AT DESC");

            var list = new List<KbArticle>();
            foreach (DataRow row in dt.Rows)
                list.Add(MapArticle(row));
            return list;
        }

        public void Save(KbArticle article)
        {
            if (article.ArticleId == 0)
            {
                int newId = OracleHelper.NextVal("SEQ_KB_ARTICLES");
                OracleHelper.ExecuteNonQuerySql(@"
                    INSERT INTO KB_ARTICLES (ARTICLE_ID,TITLE,CONTENT_HTML,CATEGORY_ID,TYPE_ID,AUTHOR_ID,IS_PUBLISHED,TAGS,PUBLISHED_AT)
                    VALUES (:P_ID,:P_T,:P_C,:P_CAT,:P_TYP,:P_AUTH,:P_PUB,:P_TAGS,:P_PUBAT)",
                    new[]
                    {
                        OracleHelper.ParamInt("P_ID",    newId),
                        OracleHelper.ParamStr("P_T",     article.Title, 500),
                        OracleHelper.ParamClob("P_C",    article.ContentHtml),
                        OracleHelper.ParamInt("P_CAT",   article.CategoryId),
                        OracleHelper.ParamInt("P_TYP",   article.TypeId),
                        OracleHelper.ParamInt("P_AUTH",  article.AuthorId),
                        OracleHelper.ParamBool("P_PUB",  article.IsPublished),
                        OracleHelper.ParamStr("P_TAGS",  article.Tags, 500),
                        OracleHelper.ParamDate("P_PUBAT",article.IsPublished ? DateTime.Now : (DateTime?)null)
                    });
            }
            else
            {
                OracleHelper.ExecuteNonQuerySql(@"
                    UPDATE KB_ARTICLES SET
                        TITLE=:P_T, CONTENT_HTML=:P_C, CATEGORY_ID=:P_CAT, TYPE_ID=:P_TYP,
                        IS_PUBLISHED=:P_PUB, TAGS=:P_TAGS, UPDATED_AT=SYSTIMESTAMP,
                        PUBLISHED_AT=CASE WHEN :P_PUB2=1 AND PUBLISHED_AT IS NULL THEN SYSTIMESTAMP ELSE PUBLISHED_AT END
                    WHERE ARTICLE_ID=:P_ID",
                    new[]
                    {
                        OracleHelper.ParamStr("P_T",    article.Title, 500),
                        OracleHelper.ParamClob("P_C",   article.ContentHtml),
                        OracleHelper.ParamInt("P_CAT",  article.CategoryId),
                        OracleHelper.ParamInt("P_TYP",  article.TypeId),
                        OracleHelper.ParamBool("P_PUB", article.IsPublished),
                        OracleHelper.ParamStr("P_TAGS", article.Tags, 500),
                        OracleHelper.ParamBool("P_PUB2",article.IsPublished),
                        OracleHelper.ParamInt("P_ID",   article.ArticleId)
                    });
            }
        }

        private KbArticle MapArticle(DataRow row) => new KbArticle
        {
            ArticleId    = OracleHelper.ToInt(row["ARTICLE_ID"]),
            Title        = OracleHelper.ToString(row["TITLE"]),
            CategoryId   = OracleHelper.ToNullableInt(row["CATEGORY_ID"]),
            CategoryName = OracleHelper.ToString(row["CATEGORY_NAME"]),
            TypeId       = OracleHelper.ToNullableInt(row["TYPE_ID"]),
            AuthorId     = OracleHelper.ToInt(row["AUTHOR_ID"]),
            AuthorName   = OracleHelper.ToString(row["AUTHOR_NAME"]),
            PublishedAt  = OracleHelper.ToNullableDateTime(row["PUBLISHED_AT"]),
            ViewCount    = OracleHelper.ToInt(row["VIEW_COUNT"]),
            IsPublished  = OracleHelper.ToBool(row["IS_PUBLISHED"]),
            Tags         = OracleHelper.ToString(row["TAGS"]),
            CreatedAt    = OracleHelper.ToDateTime(row["CREATED_AT"]),
            UpdatedAt    = OracleHelper.ToDateTime(row["UPDATED_AT"])
        };
    }

    // ────────────────────────────────────────────────────────────────────────────
    public class AnnouncementRepository
    {
        public List<Announcement> GetActive()
        {
            var dt = OracleHelper.ExecuteQuerySql(@"
                SELECT a.*, u.FULL_NAME AS CREATED_BY_NAME, sc.CATEGORY_NAME
                FROM   ANNOUNCEMENTS a
                JOIN   USERS u ON u.USER_ID = a.CREATED_BY
                LEFT JOIN SERVICE_CATEGORIES sc ON sc.CATEGORY_ID = a.CATEGORY_ID
                WHERE  a.IS_ACTIVE = 1
                  AND  (a.EXPIRES_AT IS NULL OR a.EXPIRES_AT > SYSTIMESTAMP)
                ORDER BY a.SEVERITY DESC, a.CREATED_AT DESC");

            var list = new List<Announcement>();
            foreach (DataRow row in dt.Rows)
                list.Add(MapAnn(row));
            return list;
        }

        public List<Announcement> GetAll()
        {
            var dt = OracleHelper.ExecuteQuerySql(@"
                SELECT a.*, u.FULL_NAME AS CREATED_BY_NAME, sc.CATEGORY_NAME
                FROM   ANNOUNCEMENTS a
                JOIN   USERS u ON u.USER_ID = a.CREATED_BY
                LEFT JOIN SERVICE_CATEGORIES sc ON sc.CATEGORY_ID = a.CATEGORY_ID
                ORDER BY a.CREATED_AT DESC");

            var list = new List<Announcement>();
            foreach (DataRow row in dt.Rows)
                list.Add(MapAnn(row));
            return list;
        }

        public void Save(Announcement ann)
        {
            if (ann.AnnId == 0)
            {
                int newId = OracleHelper.NextVal("SEQ_ANNOUNCEMENTS");
                OracleHelper.ExecuteNonQuerySql(@"
                    INSERT INTO ANNOUNCEMENTS (ANN_ID,TITLE,CONTENT,SEVERITY,CATEGORY_ID,CREATED_BY,EXPIRES_AT,IS_ACTIVE)
                    VALUES (:P_ID,:P_T,:P_C,:P_SEV,:P_CAT,:P_CB,:P_EXP,:P_ACT)",
                    new[]
                    {
                        OracleHelper.ParamInt("P_ID",   newId),
                        OracleHelper.ParamStr("P_T",    ann.Title, 300),
                        OracleHelper.ParamStr("P_C",    ann.Content, 4000),
                        OracleHelper.ParamStr("P_SEV",  ann.Severity, 20),
                        OracleHelper.ParamInt("P_CAT",  ann.CategoryId),
                        OracleHelper.ParamInt("P_CB",   ann.CreatedBy),
                        OracleHelper.ParamDate("P_EXP", ann.ExpiresAt),
                        OracleHelper.ParamBool("P_ACT", ann.IsActive)
                    });
            }
            else
            {
                OracleHelper.ExecuteNonQuerySql(@"
                    UPDATE ANNOUNCEMENTS SET TITLE=:P_T,CONTENT=:P_C,SEVERITY=:P_SEV,
                        CATEGORY_ID=:P_CAT,EXPIRES_AT=:P_EXP,IS_ACTIVE=:P_ACT
                    WHERE ANN_ID=:P_ID",
                    new[]
                    {
                        OracleHelper.ParamStr("P_T",   ann.Title, 300),
                        OracleHelper.ParamStr("P_C",   ann.Content, 4000),
                        OracleHelper.ParamStr("P_SEV", ann.Severity, 20),
                        OracleHelper.ParamInt("P_CAT", ann.CategoryId),
                        OracleHelper.ParamDate("P_EXP",ann.ExpiresAt),
                        OracleHelper.ParamBool("P_ACT",ann.IsActive),
                        OracleHelper.ParamInt("P_ID",  ann.AnnId)
                    });
            }
        }

        public void Deactivate(int annId)
        {
            OracleHelper.ExecuteNonQuerySql(
                "UPDATE ANNOUNCEMENTS SET IS_ACTIVE=0 WHERE ANN_ID=:P_ID",
                new[] { OracleHelper.ParamInt("P_ID", annId) });
        }

        private Announcement MapAnn(DataRow row) => new Announcement
        {
            AnnId         = OracleHelper.ToInt(row["ANN_ID"]),
            Title         = OracleHelper.ToString(row["TITLE"]),
            Content       = OracleHelper.ToString(row["CONTENT"]),
            Severity      = OracleHelper.ToString(row["SEVERITY"]),
            CategoryId    = OracleHelper.ToNullableInt(row["CATEGORY_ID"]),
            CategoryName  = OracleHelper.ToString(row["CATEGORY_NAME"]),
            CreatedBy     = OracleHelper.ToInt(row["CREATED_BY"]),
            CreatedByName = OracleHelper.ToString(row["CREATED_BY_NAME"]),
            CreatedAt     = OracleHelper.ToDateTime(row["CREATED_AT"]),
            ExpiresAt     = OracleHelper.ToNullableDateTime(row["EXPIRES_AT"]),
            IsActive      = OracleHelper.ToBool(row["IS_ACTIVE"])
        };
    }

    // ────────────────────────────────────────────────────────────────────────────
    public class ConnectionRepository
    {
        public void Create(Connection conn)
        {
            int newId = OracleHelper.NextVal("SEQ_CONNECTIONS");
            OracleHelper.ExecuteNonQuerySql(@"
                INSERT INTO CONNECTIONS
                    (CONN_ID,REQUEST_ID,USER_ID,DIVISION_ID,CONN_TYPE,CONN_IDENTIFIER,
                     LOCATION_BUILDING_ID,LOCATION_FLOOR,LOCATION_ROOM,NOTES)
                VALUES (:P_ID,:P_R,:P_U,:P_D,:P_CT,:P_CI,:P_B,:P_F,:P_ROOM,:P_N)",
                new[]
                {
                    OracleHelper.ParamInt("P_ID",   newId),
                    OracleHelper.ParamInt("P_R",    conn.RequestId),
                    OracleHelper.ParamInt("P_U",    conn.UserId),
                    OracleHelper.ParamInt("P_D",    conn.DivisionId),
                    OracleHelper.ParamStr("P_CT",   conn.ConnType, 100),
                    OracleHelper.ParamStr("P_CI",   conn.ConnIdentifier, 200),
                    OracleHelper.ParamInt("P_B",    conn.LocationBuildingId),
                    OracleHelper.ParamStr("P_F",    conn.LocationFloor, 20),
                    OracleHelper.ParamStr("P_ROOM", conn.LocationRoom, 50),
                    OracleHelper.ParamStr("P_N",    conn.Notes, 2000)
                });
        }

        public List<Connection> GetForUser(int userId)
        {
            return Query("WHERE c.USER_ID = :P_U ORDER BY c.CREATED_AT DESC",
                         new[] { OracleHelper.ParamInt("P_U", userId) });
        }

        public List<Connection> GetForDivision(int divisionId)
        {
            return Query("WHERE c.DIVISION_ID = :P_D ORDER BY c.CREATED_AT DESC",
                         new[] { OracleHelper.ParamInt("P_D", divisionId) });
        }

        public List<Connection> Search(string query, int? divisionId = null)
        {
            string where = "WHERE (UPPER(c.CONN_IDENTIFIER) LIKE '%'||UPPER(:P_Q)||'%' OR UPPER(u.FULL_NAME) LIKE '%'||UPPER(:P_Q)||'%')";
            var parms = new List<OracleParameter> { OracleHelper.ParamStr("P_Q", query) };
            if (divisionId.HasValue) { where += " AND c.DIVISION_ID=:P_D"; parms.Add(OracleHelper.ParamInt("P_D", divisionId)); }
            return Query(where + " ORDER BY c.CREATED_AT DESC", parms.ToArray());
        }

        private List<Connection> Query(string whereOrderBy, OracleParameter[] parms)
        {
            var dt = OracleHelper.ExecuteQuerySql($@"
                SELECT c.CONN_ID, c.REQUEST_ID, c.USER_ID, u.FULL_NAME AS USER_NAME,
                       c.DIVISION_ID, d.DIVISION_NAME, c.CONN_TYPE, c.CONN_IDENTIFIER,
                       c.LOCATION_BUILDING_ID, b.BUILDING_NAME, c.LOCATION_FLOOR,
                       c.LOCATION_ROOM, c.NOTES, c.IS_ACTIVE, c.CREATED_AT
                FROM   CONNECTIONS c
                JOIN   USERS u ON u.USER_ID = c.USER_ID
                JOIN   DIVISIONS d ON d.DIVISION_ID = c.DIVISION_ID
                LEFT JOIN BUILDINGS b ON b.BUILDING_ID = c.LOCATION_BUILDING_ID
                {whereOrderBy}", parms);

            var list = new List<Connection>();
            foreach (DataRow row in dt.Rows)
                list.Add(new Connection
                {
                    ConnId               = OracleHelper.ToInt(row["CONN_ID"]),
                    RequestId            = OracleHelper.ToNullableInt(row["REQUEST_ID"]),
                    UserId               = OracleHelper.ToInt(row["USER_ID"]),
                    UserName             = OracleHelper.ToString(row["USER_NAME"]),
                    DivisionId           = OracleHelper.ToInt(row["DIVISION_ID"]),
                    DivisionName         = OracleHelper.ToString(row["DIVISION_NAME"]),
                    ConnType             = OracleHelper.ToString(row["CONN_TYPE"]),
                    ConnIdentifier       = OracleHelper.ToString(row["CONN_IDENTIFIER"]),
                    LocationBuildingId   = OracleHelper.ToNullableInt(row["LOCATION_BUILDING_ID"]),
                    LocationBuildingName = OracleHelper.ToString(row["BUILDING_NAME"]),
                    LocationFloor        = OracleHelper.ToString(row["LOCATION_FLOOR"]),
                    LocationRoom         = OracleHelper.ToString(row["LOCATION_ROOM"]),
                    Notes                = OracleHelper.ToString(row["NOTES"]),
                    IsActive             = OracleHelper.ToBool(row["IS_ACTIVE"]),
                    CreatedAt            = OracleHelper.ToDateTime(row["CREATED_AT"])
                });
            return list;
        }
    }
}
