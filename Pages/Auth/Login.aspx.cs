using System;
using System.Web;
using System.Web.Security;
using CRMP.DAL;
using CRMP.Helpers;
using CRMP.Models;

namespace CRMP.Pages.Auth
{
    public partial class Login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Handle logout
            if (Request.QueryString["logout"] == "1")
            {
                SessionHelper.Logout();
                Response.Redirect("~/Pages/Auth/Login.aspx");
                return;
            }
            if (SessionHelper.IsLoggedIn)
                RedirectToDefault();
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            var userRepo = new UserRepository();
            var user = userRepo.GetByUsername(username);

            if (user == null)
            {
                ShowError("Invalid username or password.");
                return;
            }

            string storedHash = userRepo.GetPasswordHash(user.UserId);
            if (!SecurityHelper.VerifyPassword(password, storedHash))
            {
                ShowError("Invalid username or password.");
                return;
            }

            if (!user.IsActive)
            {
                ShowError("Your account has been deactivated. Please contact IT Administration.");
                return;
            }

            // Load roles
            var roles = userRepo.GetRoles(user.UserId);
            if (roles.Count == 0)
            {
                ShowError("Your account has no roles assigned. Please contact IT Administration.");
                return;
            }

            // Default active role: Employee first, otherwise first role
            UserRole defaultRole = roles.Find(r => r.RoleCode == "EMPLOYEE") ?? roles[0];
            SessionHelper.Login(user, defaultRole);

            // Forms auth ticket
            bool persist = chkRemember.Checked;
            var ticket = new FormsAuthenticationTicket(1, user.Username,
                DateTime.Now, DateTime.Now.AddDays(persist ? 30 : 1),
                persist, user.UserId.ToString());
            string encrypted = FormsAuthentication.Encrypt(ticket);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted)
            {
                Expires = persist ? DateTime.Now.AddDays(30) : DateTime.MinValue,
                HttpOnly = true, Secure = Request.IsSecureConnection
            };
            Response.Cookies.Add(cookie);

            // Update last login
            userRepo.UpdateLastLogin(user.UserId);

            string returnUrl = Request.QueryString["ReturnUrl"];
            if (!string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith("/"))
                Response.Redirect(returnUrl);
            else
                RedirectToDefault();
        }

        private void ShowError(string message)
        {
            pnlError.Visible = true;
            lblError.Text = message;
        }

        private void RedirectToDefault()
        {
            switch (SessionHelper.ActiveRoleCode)
            {
                case "EMPLOYEE":      Response.Redirect("~/Pages/Employee/Dashboard.aspx"); break;
                case "TECH_EXPERT":   Response.Redirect("~/Pages/TechExpert/Pool.aspx"); break;
                case "MODERATOR":     Response.Redirect("~/Pages/Moderator/Dashboard.aspx"); break;
                case "OIC_IT":        Response.Redirect("~/Pages/OIC/Dashboard.aspx"); break;
                default:              Response.Redirect("~/Pages/Approver/PendingApprovals.aspx"); break;
            }
        }
    }
}
