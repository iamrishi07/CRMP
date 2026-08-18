using System;
using System.Collections.Generic;
using CRMP.DAL;
using CRMP.Helpers;
using CRMP.Models;

namespace CRMP.Pages.Employee
{
    public partial class Dashboard : BasePage
    {
        protected override string[] GetRequiredRoles() { return new[] { "EMPLOYEE" }; }

        private readonly RequestRepository _reqRepo = new RequestRepository();
        private readonly CatalogRepository _catRepo = new CatalogRepository();
        private readonly AnnouncementRepository _annRepo = new AnnouncementRepository();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) LoadData();
        }

        private void LoadData()
        {
            litName.Text = SessionHelper.FullName.Split(' ')[0]; // First name

            // Stats
            var filter = new RequestFilter { SubmitterUserId = CurrentUserId };
            var all = _reqRepo.GetPaged(filter);
            litTotal.Text     = all.TotalCount.ToString();
            litPending.Text   = _reqRepo.GetPaged(new RequestFilter { SubmitterUserId = CurrentUserId, Status = "PENDING_APPROVAL" }).TotalCount.ToString();
            litInProgress.Text= _reqRepo.GetPaged(new RequestFilter { SubmitterUserId = CurrentUserId, Status = "IN_PROGRESS" }).TotalCount.ToString();
            litResolved.Text  = _reqRepo.GetPaged(new RequestFilter { SubmitterUserId = CurrentUserId, Status = "RESOLVED" }).TotalCount.ToString();

            // Recent requests (last 10)
            var recent = _reqRepo.GetPaged(new RequestFilter { SubmitterUserId = CurrentUserId, PageSize = 10 });
            if (recent.Items.Count == 0)
            {
                pnlEmpty.Visible    = true;
                pnlRequests.Visible = false;
            }
            else
            {
                pnlEmpty.Visible    = false;
                pnlRequests.Visible = true;
                rptRequests.DataSource = recent.Items;
                rptRequests.DataBind();
            }

            // Quick types — top 5 most common
            var types = _catRepo.GetRequestTypes();
            rptQuickTypes.DataSource = types.Count > 5 ? types.GetRange(0, 5) : types;
            rptQuickTypes.DataBind();

            // Announcements
            var anns = _annRepo.GetActive();
            rptAnnouncements.DataSource = anns;
            rptAnnouncements.DataBind();
        }

        protected string GetSlaLabel(DateTime? deadline)
        {
            if (!deadline.HasValue) return "No SLA";
            var remaining = deadline.Value - DateTime.Now;
            if (remaining.TotalHours < 0) return "BREACHED";
            if (remaining.TotalHours < 1) return string.Format("{0}m left", (int)remaining.TotalMinutes);
            if (remaining.TotalDays < 1) return string.Format("{0}h left", (int)remaining.TotalHours);
            return string.Format("{0}d left", (int)remaining.TotalDays);
        }

        protected string GetAnnStyle(string severity)
        {
            switch (severity)
            {
                case "CRITICAL": return "background:#FEF2F2;border-color:#FECACA;";
                case "WARNING":  return "background:#FFFBEB;border-color:#FCD34D;";
                default:         return "background:#EFF6FF;border-color:#BFDBFE;";
            }
        }
    }
}
