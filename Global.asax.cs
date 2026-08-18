using System;
using System.Web;
using System.Web.Security;
using System.Threading;
using CRMP.BLL;

namespace CRMP
{
    public class Global : HttpApplication
    {
        private static Timer _slaTimer;
        private static Timer _digestTimer;

        protected void Application_Start(object sender, EventArgs e)
        {
            // Start SLA background check engine
            int slaIntervalMs = int.Parse(System.Configuration.ConfigurationManager.AppSettings["SlaCheckIntervalMinutes"]) * 60 * 1000;
            _slaTimer = new Timer(SlaCheckCallback, null, TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(slaIntervalMs));

            // Start daily digest scheduler
            _digestTimer = new Timer(DigestCheckCallback, null, TimeSpan.FromMinutes(5), TimeSpan.FromHours(1));
        }

        private static void SlaCheckCallback(object state)
        {
            try { SlaEngine.RunCheck(); }
            catch { /* log silently */ }
        }

        private static void DigestCheckCallback(object state)
        {
            try
            {
                int targetHour = int.Parse(System.Configuration.ConfigurationManager.AppSettings["DigestSendHourUtc"]);
                if (DateTime.UtcNow.Hour == targetHour && DateTime.UtcNow.Minute < 60)
                    NotificationService.SendDailyDigest();
            }
            catch { /* log silently */ }
        }

        protected void Application_End(object sender, EventArgs e)
        {
            _slaTimer?.Dispose();
            _digestTimer?.Dispose();
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
            // Forms auth ticket decoding — role context already in session
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError();
            // TODO: wire to a logging provider (e.g., log4net or EventLog)
            // For now, let customErrors in Web.config redirect gracefully
        }

        protected void Session_End(object sender, EventArgs e) { }
    }
}
