using System;
using System.Collections.Generic;
using CRMP.BLL;
using CRMP.DAL;
using CRMP.Helpers;
using CRMP.Models;

namespace CRMP.Pages.Approver
{
    public partial class PendingApprovals : BasePage
    {
        protected override string[] GetRequiredRoles() =>
            new[] { "DIVISION_HEAD", "ISO", "ADMIN_HR", "DIRECTOR" };

        private readonly RequestRepository _reqRepo  = new RequestRepository();
        private readonly CatalogRepository _catRepo  = new CatalogRepository();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) LoadData();
        }

        private void LoadData()
        {
            // Load categories for filter
            ddlFilterCat.Items.Clear();
            ddlFilterCat.Items.Add(new System.Web.UI.WebControls.ListItem("All Categories", ""));
            foreach (var cat in _catRepo.GetCategories())
                ddlFilterCat.Items.Add(new System.Web.UI.WebControls.ListItem(cat.CategoryName, cat.CategoryId.ToString()));

            LoadList();
        }

        protected void ApplyFilter(object sender, EventArgs e) => LoadList();

        private void LoadList()
        {
            var pending = _reqRepo.GetPendingForApprover(CurrentUserId);

            // Client-side filtering
            if (!string.IsNullOrEmpty(ddlFilterCat.SelectedValue))
                pending = pending.FindAll(r => r.CategoryId.ToString() == ddlFilterCat.SelectedValue);
            if (!string.IsNullOrEmpty(ddlFilterPriority.SelectedValue))
                pending = pending.FindAll(r => r.Priority == ddlFilterPriority.SelectedValue);
            if (ddlFilterSla.SelectedValue == "breached")
                pending = pending.FindAll(r => r.IsSlaBreached);
            else if (ddlFilterSla.SelectedValue == "risk")
                pending = pending.FindAll(r => !r.IsSlaBreached && r.SlaPercentConsumed >= 75);

            pnlEmpty.Visible = pending.Count == 0;
            pnlList.Visible  = pending.Count > 0;

            rptApprovals.DataSource = pending;
            rptApprovals.DataBind();
        }

        protected void btnBulkSubmit_Click(object sender, EventArgs e)
        {
            string idsStr   = hfBulkIds.Value;
            string action   = hfBulkAction.Value;
            string remarks  = Request.Form["bulkRemarks"] ?? "";

            if (string.IsNullOrEmpty(idsStr)) return;

            var ids = new List<int>();
            foreach (var s in idsStr.Split(','))
                if (int.TryParse(s.Trim(), out int rid)) ids.Add(rid);

            if (action == "REJECTED" && string.IsNullOrEmpty(remarks))
            { ShowToast("Rejection reason is required.", "error"); return; }

            WorkflowEngine.ProcessBulkApproval(ids, CurrentUserId, remarks, action == "APPROVED");

            ShowToast($"{ids.Count} request(s) {(action == "APPROVED" ? "approved" : "rejected")} successfully.", "success");
            LoadList();
        }

        protected string GetSlaLabel(DateTime? deadline, bool breached)
        {
            if (breached) return "BREACHED";
            if (!deadline.HasValue) return "No SLA";
            var rem = deadline.Value - DateTime.Now;
            if (rem.TotalHours < 0) return "BREACHED";
            if (rem.TotalDays < 1) return $"{(int)rem.TotalHours}h left";
            return $"{(int)rem.TotalDays}d left";
        }
    }
}
