using System;
using System.Web;
using System.Web.UI;
using CRMP.Helpers;
using CRMP.DAL;
using CRMP.Models;
using System.Collections.Generic;

namespace CRMP
{
    /// <summary>
    /// All authenticated pages inherit from this.
    /// Enforces login, provides CurrentUser, CurrentRole, and
    /// role-based access guard methods.
    /// </summary>
    public abstract class BasePage : Page
    {
        private User _currentUser;

        protected UserRepository UserRepo
        {
            get { return new UserRepository(); }
        }

        protected User CurrentUser
        {
            get
            {
                if (_currentUser == null && SessionHelper.IsLoggedIn)
                    _currentUser = UserRepo.GetById(SessionHelper.UserId);
                return _currentUser;
            }
        }

        protected int CurrentUserId
        {
            get { return SessionHelper.UserId; }
        }

        protected string CurrentRole
        {
            get { return SessionHelper.ActiveRoleCode; }
        }

        protected string CurrentRoleName
        {
            get { return SessionHelper.ActiveRoleName; }
        }

        protected int CurrentDivisionId
        {
            get { return SessionHelper.DivisionId; }
        }

        protected int? ActiveDivisionId
        {
            get { return SessionHelper.ActiveDivisionId; }
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            if (!SessionHelper.IsLoggedIn)
            {
                Response.Redirect("~/Pages/Auth/Login.aspx?ReturnUrl=" + HttpUtility.UrlEncode(Request.RawUrl));
                return;
            }

            // Check for page-level role restrictions
            string[] requiredRoles = GetRequiredRoles();
            if (requiredRoles != null && requiredRoles.Length > 0)
            {
                bool authorized = false;
                foreach (var role in requiredRoles)
                    if (SessionHelper.ActiveRoleCode == role) { authorized = true; break; }

                // OIC_IT always has access
                if (SessionHelper.IsOicIt) authorized = true;

                if (!authorized)
                    Response.Redirect("~/Pages/Shared/AccessDenied.aspx");
            }
        }

        /// <summary>Override in pages to restrict access to specific role codes.</summary>
        protected virtual string[] GetRequiredRoles()
        {
            return null;
        }

        // ── Role guards for use in code-behind ───────────────────────────────
        protected bool IsEmployee
        {
            get { return SessionHelper.IsEmployee; }
        }

        protected bool IsApproverRole
        {
            get { return SessionHelper.IsApproverRole; }
        }

        protected bool IsTechExpert
        {
            get { return SessionHelper.IsTechExpert; }
        }

        protected bool IsModerator
        {
            get { return SessionHelper.IsModerator; }
        }

        protected bool IsOicIt
        {
            get { return SessionHelper.IsOicIt; }
        }

        // ── Convenience redirect ──────────────────────────────────────────────
        protected void RedirectToDefaultDashboard()
        {
            switch (SessionHelper.ActiveRoleCode)
            {
                case "EMPLOYEE":
                    Response.Redirect("~/Pages/Employee/Dashboard.aspx"); break;
                case "DIVISION_HEAD":
                case "ISO":
                case "ADMIN_HR":
                case "DIRECTOR":
                    Response.Redirect("~/Pages/Approver/PendingApprovals.aspx"); break;
                case "TECH_EXPERT":
                    Response.Redirect("~/Pages/TechExpert/Pool.aspx"); break;
                case "MODERATOR":
                    Response.Redirect("~/Pages/Moderator/Dashboard.aspx"); break;
                case "OIC_IT":
                    Response.Redirect("~/Pages/OIC/Dashboard.aspx"); break;
                default:
                    Response.Redirect("~/Pages/Employee/Dashboard.aspx"); break;
            }
        }

        // ── Toast helper (writes to a hidden label that JS picks up) ─────────
        protected void ShowToast(string message, string type = "success")
        {
            ClientScript.RegisterStartupScript(
                GetType(), "toast",
                "showToast('" + message.Replace("'", "\\'") + "', '" + type + "');", true);
        }
    }
}
