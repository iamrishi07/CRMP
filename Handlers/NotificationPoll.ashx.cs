using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Script.Serialization;
using CRMP.DAL;
using CRMP.Helpers;

namespace CRMP.Handlers
{
    /// <summary>
    /// JSON endpoint for notification bell — count, list, mark-read, mark-all-read.
    /// Called via fetch() from portal.js.
    /// </summary>
    public class NotificationPollHandler : IHttpHandler
    {
        public bool IsReusable => false;

        public void ProcessRequest(HttpContext context)
        {
            // Require auth
            if (!SessionHelper.IsLoggedIn)
            {
                context.Response.StatusCode = 401;
                return;
            }

            context.Response.ContentType = "application/json";
            context.Response.Cache.SetNoStore();

            string action = context.Request.QueryString["action"] ?? "count";
            var repo = new NotificationRepository();
            int userId = SessionHelper.UserId;
            var ser = new JavaScriptSerializer();

            switch (action)
            {
                case "count":
                    int count = repo.GetUnreadCount(userId);
                    context.Response.Write(ser.Serialize(new { count }));
                    break;

                case "list":
                    var items = repo.GetUnread(userId, 15);
                    var mapped = items.ConvertAll(n => new
                    {
                        notifId  = n.NotifId,
                        title    = n.Title,
                        message  = n.Message,
                        link     = n.Link ?? "",
                        isRead   = n.IsRead,
                        timeAgo  = n.TimeAgo
                    });
                    context.Response.Write(ser.Serialize(new { items = mapped }));
                    break;

                case "read":
                    if (int.TryParse(context.Request.QueryString["id"], out int nid))
                        repo.MarkRead(nid);
                    context.Response.Write("{\"ok\":true}");
                    break;

                case "readall":
                    repo.MarkAllRead(userId);
                    context.Response.Write("{\"ok\":true}");
                    break;

                default:
                    context.Response.Write("{\"error\":\"Unknown action\"}");
                    break;
            }
        }
    }
}
