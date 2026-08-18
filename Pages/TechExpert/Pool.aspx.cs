using System;
using System.Web.UI.WebControls;
using CRMP.BLL;
using CRMP.DAL;
using CRMP.Helpers;
using CRMP.Models;

namespace CRMP.Pages.TechExpert
{
    public partial class Pool : BasePage
    {
        protected override string[] GetRequiredRoles() { return new[] { "TECH_EXPERT" }; }

        private readonly RequestRepository _reqRepo = new RequestRepository();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) LoadPool();
        }

        private void LoadPool()
        {
            var pool = _reqRepo.GetTechExpertPool(CurrentUserId);
            pnlEmpty.Visible = pool.Count == 0;
            pnlPool.Visible  = pool.Count > 0;
            rptPool.DataSource = pool;
            rptPool.DataBind();
        }

        protected void btnPickup_Command(object sender, CommandEventArgs e)
        {
            if (e.CommandName != "Pickup") return;
            int requestId = int.Parse(e.CommandArgument.ToString());

            // Assign to this tech expert
            _reqRepo.AssignTechExpert(requestId, CurrentUserId);
            _reqRepo.UpdateStatus(requestId, "IN_PROGRESS");
            _reqRepo.AddTimeline(requestId, "NOTE",
                string.Format("Request picked up by technical expert {0}.", SessionHelper.FullName),
                CurrentUserId);

            // Notify submitter
            var req = _reqRepo.GetById(requestId);
            NotificationService.Notify(req.SubmitterUserId,
                "Request Being Processed",
                string.Format("Your request {0} has been picked up by a technical expert and is now in progress.", req.RequestNumber),
                string.Format("~/Pages/Employee/RequestDetail.aspx?id={0}", requestId),
                NotificationService.Types.RequestAssigned);

            ShowToast("Request picked up successfully!", "success");
            Response.Redirect(string.Format("WorkDetail.aspx?id={0}", requestId));
        }

        protected string GetSlaLabel(DateTime? deadline)
        {
            if (!deadline.HasValue) return "No SLA";
            var rem = deadline.Value - DateTime.Now;
            if (rem.TotalHours < 0) return "BREACHED";
            if (rem.TotalDays < 1) return string.Format("{0}h left", (int)rem.TotalHours);
            return string.Format("{0}d left", (int)rem.TotalDays);
        }
    }
}
