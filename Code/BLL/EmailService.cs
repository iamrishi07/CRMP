using System;
using System.Net;
using System.Net.Mail;
using System.Configuration;
using System.Text;
using CRMP.Models;

namespace CRMP.BLL
{
    public static class EmailService
    {
        private static readonly string _host     = ConfigurationManager.AppSettings["SmtpHost"];
        private static readonly int    _port     = int.Parse(ConfigurationManager.AppSettings["SmtpPort"] ?? "25");
        private static readonly bool   _ssl      = bool.Parse(ConfigurationManager.AppSettings["SmtpUseSsl"] ?? "false");
        private static readonly string _from     = ConfigurationManager.AppSettings["SmtpFromAddress"];
        private static readonly string _fromName = ConfigurationManager.AppSettings["SmtpFromName"];
        private static readonly string _user     = ConfigurationManager.AppSettings["SmtpUsername"];
        private static readonly string _pass     = ConfigurationManager.AppSettings["SmtpPassword"];
        private static readonly string _appName  = ConfigurationManager.AppSettings["AppName"] ?? "CRMP Portal";
        private static readonly string _orgName  = ConfigurationManager.AppSettings["OrganizationName"] ?? "Your Organization";

        public static void SendNotificationEmail(string toEmail, string toName,
                                                  string subject, string bodyText, string link)
        {
            string html = BuildEmailHtml(subject, bodyText, link, "View Details");
            Send(toEmail, toName, subject, html);
        }

        public static void SendDigestEmail(string toEmail, string toName, int pendingCount)
        {
            string subject = "[" + _appName + "] Daily Digest - " + pendingCount.ToString() + " item(s) awaiting your approval";
            string body = "You have <strong>" + pendingCount.ToString() + "</strong> request(s) pending your approval. Please log in to review them.";
            string html = BuildEmailHtml(subject, body, "~/Pages/Approver/PendingApprovals.aspx", "View Pending Approvals");
            Send(toEmail, toName, subject, html);
        }

        public static void SendTechDigestEmail(string toEmail, string toName, int poolCount)
        {
            string subject = "[" + _appName + "] Daily Digest - " + poolCount.ToString() + " request(s) in your pool";
            string body = "There are <strong>" + poolCount.ToString() + "</strong> approved request(s) waiting in your tech expert pool.";
            string html = BuildEmailHtml(subject, body, "~/Pages/TechExpert/Pool.aspx", "View Pool");
            Send(toEmail, toName, subject, html);
        }

        public static void SendSlaBreachAlert(Request req)
        {
            string subject = "[" + _appName + "] SLA Breach - " + req.RequestNumber;
            string body = "Request <strong>" + req.RequestNumber + "</strong> (" + req.TypeName + ") submitted by " + req.SubmitterName + " from " + req.DivisionName + " has exceeded its SLA deadline.";
            string html = BuildEmailHtml(subject, body, "~/Pages/Employee/RequestDetail.aspx?id=" + req.RequestId.ToString(), "View Request");
            // In production, get emails of moderators/OIC — here we just have the structure ready
        }

        // ── Core send ─────────────────────────────────────────────────────────
        private static void Send(string toEmail, string toName, string subject, string htmlBody)
        {
            try
            {
                using (var client = new SmtpClient(_host, _port))
                {
                    client.EnableSsl = _ssl;
                    if (!string.IsNullOrEmpty(_user))
                        client.Credentials = new NetworkCredential(_user, _pass);

                    var msg = new MailMessage
                    {
                        From       = new MailAddress(_from, _fromName),
                        Subject    = subject,
                        Body       = htmlBody,
                        IsBodyHtml = true
                    };
                    msg.To.Add(new MailAddress(toEmail, toName));
                    client.Send(msg);
                }
            }
            catch { /* Log silently — email failure should not crash the application */ }
        }

        // ── HTML email template ───────────────────────────────────────────────
        private static string BuildEmailHtml(string heading, string bodyContent, string link, string btnText)
        {
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'/>");
            sb.Append("<style>");
            sb.Append("body{font-family:Inter,Arial,sans-serif;background:#f1f5f9;margin:0;padding:20px}");
            sb.Append(".wrap{max-width:580px;margin:0 auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,.08)}");
            sb.Append(".hdr{background:linear-gradient(135deg,#4F46E5,#7C3AED);padding:28px 32px;color:#fff}");
            sb.Append(".hdr h1{margin:0;font-size:18px;font-weight:700;letter-spacing:-.3px}");
            sb.Append(".hdr p{margin:4px 0 0;font-size:12px;opacity:.8}");
            sb.Append(".body{padding:28px 32px}");
            sb.Append(".body p{color:#374151;font-size:15px;line-height:1.6}");
            sb.Append(".btn{display:inline-block;background:#4F46E5;color:#fff!important;padding:12px 24px;");
            sb.Append("border-radius:8px;text-decoration:none;font-size:14px;font-weight:600;margin-top:16px}");
            sb.Append(".ftr{background:#f8fafc;padding:16px 32px;font-size:12px;color:#94a3b8;border-top:1px solid #e2e8f0}");
            sb.Append("</style></head>");
            sb.Append("<body><div class='wrap'>");
            sb.Append("<div class='hdr'><h1>" + _appName + "</h1><p>" + _orgName + "</p></div>");
            sb.Append("<div class='body'>");
            sb.Append("<h2 style='color:#1e293b;font-size:20px;margin-top:0'>" + heading + "</h2>");
            sb.Append("<p>" + bodyContent + "</p>");
            sb.Append("<a href='" + link + "' class='btn'>" + btnText + "</a>");
            sb.Append("</div>");
            sb.Append("<div class='ftr'>This is an automated notification from " + _appName + ". Please do not reply to this email.</div>");
            sb.Append("</div></body></html>");
            return sb.ToString();
        }
    }
}
