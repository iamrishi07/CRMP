using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Script.Serialization;
using CRMP.DAL;
using CRMP.Helpers;
using CRMP.Models;

namespace CRMP.Pages.OIC
{
    public partial class Dashboard : BasePage
    {
        protected override string[] GetRequiredRoles() => new[] { "OIC_IT" };

        private readonly RequestRepository _reqRepo = new RequestRepository();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) LoadDashboard();
        }

        private void LoadDashboard()
        {
            // Stats
            var total    = _reqRepo.GetPaged(new RequestFilter { PageSize = 1 });
            var pending  = _reqRepo.GetPaged(new RequestFilter { Status = "PENDING_APPROVAL", PageSize = 1 });
            var breached = _reqRepo.GetPaged(new RequestFilter { SlaBreached = true, PageSize = 1 });
            var resolvedMonth = _reqRepo.GetPaged(new RequestFilter
            {
                Status = "RESOLVED",
                DateFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                PageSize = 1
            });

            litTotal.Text    = total.TotalCount.ToString();
            litPending.Text  = pending.TotalCount.ToString();
            litBreached.Text = breached.TotalCount.ToString();
            litResolved.Text = resolvedMonth.TotalCount.ToString();

            // SLA Breached list (top 10)
            var breachedList = _reqRepo.GetPaged(new RequestFilter { SlaBreached = true, PageSize = 10 });
            pnlNoBreached.Visible = breachedList.Items.Count == 0;
            rptBreached.DataSource = breachedList.Items;
            rptBreached.DataBind();

            // Chart Data
            BuildChartData();
        }

        private void BuildChartData()
        {
            var ser = new JavaScriptSerializer();

            // Status chart
            var statusDt = _reqRepo.GetStatusSummary();
            var statusColors = new Dictionary<string, string>
            {
                ["PENDING_APPROVAL"] = "#F59E0B", ["APPROVED"] = "#10B981",
                ["REJECTED"] = "#EF4444", ["IN_PROGRESS"] = "#3B82F6",
                ["RESOLVED"] = "#14B8A6", ["CLOSED"] = "#64748B",
                ["CANCELLED"] = "#94A3B8", ["DRAFT"] = "#CBD5E1"
            };
            var statusLabels = new List<string>(); var statusVals = new List<int>(); var statusClrs = new List<string>();
            foreach (DataRow r in statusDt.Rows)
            {
                string s = r["STATUS"].ToString();
                statusLabels.Add(StatusDisplayName(s));
                statusVals.Add(Convert.ToInt32(r["CNT"]));
                statusClrs.Add(statusColors.ContainsKey(s) ? statusColors[s] : "#94A3B8");
            }
            hfStatusJson.Value = ser.Serialize(new { labels = statusLabels, values = statusVals, colors = statusClrs });

            // Category chart
            var catDt = _reqRepo.GetRequestsByCategory();
            var catLabels = new List<string>(); var catVals = new List<int>(); var catClrs = new List<string>();
            foreach (DataRow r in catDt.Rows)
            {
                catLabels.Add(r["CATEGORY_NAME"].ToString());
                catVals.Add(Convert.ToInt32(r["CNT"]));
                catClrs.Add(r["COLOR_HEX"].ToString());
            }
            hfCategoryJson.Value = ser.Serialize(new { labels = catLabels, values = catVals, colors = catClrs });

            // Trend (last 30 days)
            var trendDt = _reqRepo.GetRequestsPerDay(30);
            var trendLabels = new List<string>(); var trendVals = new List<int>();
            foreach (DataRow r in trendDt.Rows)
            {
                trendLabels.Add(Convert.ToDateTime(r["REQ_DATE"]).ToString("dd MMM"));
                trendVals.Add(Convert.ToInt32(r["CNT"]));
            }
            hfTrendJson.Value = ser.Serialize(new { labels = trendLabels, values = trendVals });
        }

        protected string GetAge(DateTime submittedAt)
        {
            var age = DateTime.Now - submittedAt;
            if (age.TotalDays >= 1) return $"{(int)age.TotalDays}d ago";
            return $"{(int)age.TotalHours}h ago";
        }

        private string StatusDisplayName(string s)
        {
            switch (s)
            {
                case "PENDING_APPROVAL": return "Pending Approval";
                case "IN_PROGRESS":      return "In Progress";
                default: return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.ToLower().Replace("_", " "));
            }
        }
    }
}
