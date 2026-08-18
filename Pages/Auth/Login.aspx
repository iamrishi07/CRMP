<%@ Page Title="Login" Language="C#" MasterPageFile="~/MasterPages/Public.Master" AutoEventWireup="true" CodeFile="Login.aspx.cs" Inherits="CRMP.Pages.Auth.Login" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">

<div class="login-wrap">
  <div class="login-left">
    <div class="login-left-content">
      <div class="login-brand">
        <div class="sidebar-brand-icon" style="width:52px;height:52px;font-size:24px">IT</div>
        <h1 style="font-size:28px;font-weight:800;color:#fff;letter-spacing:-.5px;margin-top:16px">CRMP Portal</h1>
        <p style="color:rgba(255,255,255,.7);margin-top:8px;font-size:15px">Complaints & Requests Management</p>
      </div>
      <div class="login-features">
        <div class="login-feature"><span>✅</span><span>Multi-stage approval workflows</span></div>
        <div class="login-feature"><span>⏱️</span><span>Real-time SLA tracking</span></div>
        <div class="login-feature"><span>🔔</span><span>Instant notifications</span></div>
        <div class="login-feature"><span>📊</span><span>Live dashboards &amp; reports</span></div>
      </div>
    </div>
  </div>

  <div class="login-right">
    <div class="login-form-wrap">
      <h2 class="login-heading">Welcome back</h2>
      <p class="login-subheading">Sign in to access the portal</p>

      <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="alert-error">
        <span>⚠</span>
        <asp:Label ID="lblError" runat="server"/>
      </asp:Panel>

      <div class="form-group">
        <label class="form-label" for="txtUsername">Username or Email</label>
        <asp:TextBox ID="txtUsername" runat="server" CssClass="form-control"
                     placeholder="Enter your username" autocomplete="username"
                     ClientIDMode="Static"/>
        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtUsername"
             ErrorMessage="Username is required." CssClass="form-error" Display="Dynamic"/>
      </div>

      <div class="form-group">
        <label class="form-label" for="txtPassword">
          Password
          <a href="#" style="float:right;font-size:12px;font-weight:400">Forgot password?</a>
        </label>
        <div style="position:relative">
          <asp:TextBox ID="txtPassword" runat="server" TextMode="Password"
                       CssClass="form-control" placeholder="Enter your password"
                       autocomplete="current-password" ClientIDMode="Static"/>
          <button type="button" onclick="togglePwd()" style="position:absolute;right:12px;top:50%;transform:translateY(-50%);background:none;border:none;cursor:pointer;color:var(--gray-400);font-size:16px" title="Show/hide">👁</button>
        </div>
        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtPassword"
             ErrorMessage="Password is required." CssClass="form-error" Display="Dynamic"/>
      </div>

      <div class="form-check" style="margin-bottom:20px">
        <asp:CheckBox ID="chkRemember" runat="server"/>
        <label class="form-check-label" for="<%= chkRemember.ClientID %>">Remember me for 30 days</label>
      </div>

      <asp:Button ID="btnLogin" runat="server" Text="Sign In" CssClass="btn btn-primary w-full"
                  OnClick="btnLogin_Click" style="width:100%;justify-content:center;padding:12px"/>

      <div style="text-align:center;margin-top:20px;font-size:13px;color:var(--text-muted)">
        Don't have an account? Contact your IT Administrator.
      </div>
    </div>
  </div>
</div>

<style>
.login-wrap {
  display: flex; min-height: 100vh; background: #fff;
}
.login-left {
  width: 420px; flex-shrink: 0;
  background: linear-gradient(160deg, #0F172A 0%, #1E293B 40%, #4F46E5 100%);
  display: flex; align-items: center; justify-content: center; padding: 48px;
}
.login-left-content { width: 100%; }
.login-brand { margin-bottom: 48px; }
.login-features { display: flex; flex-direction: column; gap: 18px; }
.login-feature {
  display: flex; align-items: center; gap: 14px;
  color: rgba(255,255,255,.8); font-size: 14px; font-weight: 500;
}
.login-feature span:first-child { font-size: 20px; width: 24px; text-align: center; }
.login-right {
  flex: 1; display: flex; align-items: center; justify-content: center; padding: 48px;
  background: var(--gray-50);
}
.login-form-wrap { width: 100%; max-width: 400px; }
.login-heading { font-size: 26px; font-weight: 800; color: var(--text-primary); letter-spacing: -.5px; }
.login-subheading { font-size: 14px; color: var(--text-muted); margin-top: 6px; margin-bottom: 28px; }
.alert-error {
  display: flex; align-items: center; gap: 10px;
  padding: 12px 16px; background: #FEF2F2; color: #991B1B;
  border: 1px solid #FECACA; border-radius: var(--radius-md);
  font-size: 13.5px; margin-bottom: 20px;
}
@media (max-width: 768px) {
  .login-left { display: none; }
  .login-right { padding: 32px 24px; background: #fff; }
}
</style>
<script>
function togglePwd() {
  const p = document.getElementById('txtPassword');
  p.type = p.type === 'password' ? 'text' : 'password';
}
</script>
</asp:Content>
