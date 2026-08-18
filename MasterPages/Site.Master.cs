using System;
using System.Web.UI;
using CRMP.Helpers;
using CRMP.DAL;
using CRMP.Models;
using System.Collections.Generic;

namespace CRMP.MasterPages
{
    public partial class SiteMaster : MasterPage
    {
        protected string UserFullName  => SessionHelper.FullName;
        protected string UserDivision  => SessionHelper.DivisionName;
        protected int    ActiveRoleId  => SessionHelper.ActiveRoleId;
        protected string ActiveRoleName => SessionHelper.ActiveRoleName;
        protected string UserInitials
        {
            get
            {
                var parts = (SessionHelper.FullName ?? "").Trim().Split(' ');
                if (parts.Length >= 2) return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
                return SessionHelper.FullName?.Length > 0 ? SessionHelper.FullName[0].ToString().ToUpper() : "?";
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!SessionHelper.IsLoggedIn) return;

            // Show correct sidebar section
            string role = SessionHelper.ActiveRoleCode;
            navEmployee.Visible  = role == "EMPLOYEE";
            navApprover.Visible  = SessionHelper.IsApproverRole;
            navTechExpert.Visible= role == "TECH_EXPERT";
            navModerator.Visible = role == "MODERATOR";
            navOIC.Visible       = role == "OIC_IT";

            // Pending count badge for approvers
            if (SessionHelper.IsApproverRole)
            {
                var pending = new RequestRepository().GetPendingForApprover(SessionHelper.UserId);
                pendingCount.InnerText = pending.Count > 0 ? pending.Count.ToString() : "";
                pendingCount.Visible   = pending.Count > 0;
            }

            // Pool count for tech experts
            if (role == "TECH_EXPERT")
            {
                var pool = new RequestRepository().GetTechExpertPool(SessionHelper.UserId);
                poolCount.InnerText = pool.Count > 0 ? pool.Count.ToString() : "";
                poolCount.Visible   = pool.Count > 0;
            }

            // Load roles for switcher
            var roles = new UserRepository().GetRoles(SessionHelper.UserId);
            rptRoles.DataSource = roles;
            rptRoles.DataBind();

            // Load active announcements
            var anns = new AnnouncementRepository().GetActive();
            rptBanners.DataSource = anns;
            rptBanners.DataBind();
        }

        protected string GetRoleIcon(string roleCode)
        {
            switch (roleCode)
            {
                case "EMPLOYEE":       return "👤";
                case "DIVISION_HEAD":  return "👔";
                case "ISO":            return "🔒";
                case "ADMIN_HR":       return "📋";
                case "DIRECTOR":       return "⭐";
                case "TECH_EXPERT":    return "🔧";
                case "MODERATOR":      return "📊";
                case "OIC_IT":         return "🏛";
                default:               return "🔘";
            }
        }
    }
}
