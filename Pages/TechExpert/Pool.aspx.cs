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
        protected override string[] GetRequiredRoles() => new[] { "TECH_EXPERT" };

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
                $"Request picked up by technical expert {SessionHelper.FullName}.",
                CurrentUserId);

            // Notify submitter
            var req = _reqRepo.GetById(requestId);
            NotificationService.Notify(req.SubmitterUserId,
                "Request Being Processed",
                $"Your request {req.RequestNumber} has been picked up by a technical expert and is now in progress.",
                $"~/Pages/Employee/RequestDetail.aspx?id={requestId}",
                NotificationService.Types.RequestAssigned);

            ShowToast("Request picked up successfully!", "success");
            Response.Redirect($"WorkDetail.aspx?id={requestId}");
        }

        protected string GetSlaLabel(DateTime? deadline)
        {
            if (!deadline.HasValue) return "No SLA";
            var rem = deadline.Value - DateTime.Now;
            if (rem.TotalHours < 0) return "BREACHED";
            if (rem.TotalDays < 1) return $"{(int)rem.TotalHours}h left";
            return $"{(int)rem.TotalDays}d left";
        }
    }
}
