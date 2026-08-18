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
        protected string UserFullName
        {
            get { return SessionHelper.FullName; }
        }

        protected string UserDivision
        {
            get { return SessionHelper.DivisionName; }
        }

        protected int ActiveRoleId
        {
            get { return SessionHelper.ActiveRoleId; }
        }

        protected string ActiveRoleName
        {
            get { return SessionHelper.ActiveRoleName; }
        }

        protected string UserInitials
        {
            get
            {
                string fullName = SessionHelper.FullName;
                var parts = (fullName ?? "").Trim().Split(' ');
                if (parts.Length >= 2)
                    return (parts[0][0].ToString() + parts[parts.Length - 1][0].ToString()).ToUpper();
                return (fullName != null && fullName.Length > 0)
                    ? fullName[0].ToString().ToUpper()
                    : "?";
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!SessionHelper.IsLoggedIn) return;

            // Show correct sidebar section
            string role = SessionHelper.ActiveRoleCode;
            navEmployee.Visible   = role == "EMPLOYEE";
            navApprover.Visible   = SessionHelper.IsApproverRole;
            navTechExpert.Visible = role == "TECH_EXPERT";
            navModerator.Visible  = role == "MODERATOR";
            navOIC.Visible        = role == "OIC_IT";

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
                case "EMPLOYEE":      return "\U0001F464";
                case "DIVISION_HEAD": return "\U0001F454";
                case "ISO":           return "\U0001F512";
                case "ADMIN_HR":      return "\U0001F4CB";
                case "DIRECTOR":      return "\U00002B50";
                case "TECH_EXPERT":   return "\U0001F527";
                case "MODERATOR":     return "\U0001F4CA";
                case "OIC_IT":        return "\U0001F3DB";
                default:              return "\U0001F518";
            }
        }
    }
}
