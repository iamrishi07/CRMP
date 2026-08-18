<%@ Page Title="My Dashboard" Language="C#" MasterPageFile="~/MasterPages/Site.Master" AutoEventWireup="true" CodeFile="Dashboard.aspx.cs" Inherits="CRMP.Pages.Employee.Dashboard" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">

<div class="page-header">
  <div class="page-header-left">
    <div class="breadcrumb"><a href="#">Home</a><span class="breadcrumb-sep">›</span><span>Dashboard</span></div>
    <h1 class="page-title">Welcome back, <asp:Literal ID="litName" runat="server"/>! 👋</h1>
    <p class="page-subtitle">Here's an overview of your service requests and what needs your attention.</p>
  </div>
  <div>
    <a href="<%=ResolveUrl("~/Pages/Employee/NewRequest.aspx")%>" class="btn btn-primary">
      ➕ New Request
    </a>
  </div>
</div>

<!-- STAT CARDS -->
<div class="grid grid-4 mb-6">
  <div class="stat-card stat-primary">
    <div class="stat-card-icon" style="background:#EEF2FF">📋</div>
    <div class="stat-card-value"><asp:Literal ID="litTotal" runat="server">0</asp:Literal></div>
    <div class="stat-card-label">Total Requests</div>
  </div>
  <div class="stat-card stat-warning">
    <div class="stat-card-icon" style="background:#FFFBEB">⏳</div>
    <div class="stat-card-value"><asp:Literal ID="litPending" runat="server">0</asp:Literal></div>
    <div class="stat-card-label">Pending Approval</div>
  </div>
  <div class="stat-card stat-info">
    <div class="stat-card-icon" style="background:#EFF6FF">🔧</div>
    <div class="stat-card-value"><asp:Literal ID="litInProgress" runat="server">0</asp:Literal></div>
    <div class="stat-card-label">In Progress</div>
  </div>
  <div class="stat-card stat-success">
    <div class="stat-card-icon" style="background:#ECFDF5">✅</div>
    <div class="stat-card-value"><asp:Literal ID="litResolved" runat="server">0</asp:Literal></div>
    <div class="stat-card-label">Resolved</div>
  </div>
</div>

<div class="grid grid-2 gap-6">

  <!-- Recent Requests -->
  <div class="card" style="grid-column:span 2">
    <div class="card-header">
      <h3 class="card-title">My Recent Requests</h3>
      <a href="<%=ResolveUrl("~/Pages/Employee/MyRequests.aspx")%>" class="btn btn-ghost btn-sm">View all →</a>
    </div>
    <asp:Panel ID="pnlEmpty" runat="server" Visible="false">
      <div class="empty-state">
        <div class="empty-state-icon">📋</div>
        <div class="empty-state-title">No requests yet</div>
        <div class="empty-state-desc">Submit your first service request to get started.</div>
        <a href="<%=ResolveUrl("~/Pages/Employee/NewRequest.aspx")%>" class="btn btn-primary mt-4">New Request</a>
      </div>
    </asp:Panel>
    <asp:Panel ID="pnlRequests" runat="server">
      <table class="data-table">
        <thead>
          <tr>
            <th>Request No.</th>
            <th>Type</th>
            <th>Category</th>
            <th>Status</th>
            <th>Priority</th>
            <th>SLA</th>
            <th>Submitted</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <asp:Repeater ID="rptRequests" runat="server">
            <ItemTemplate>
              <tr>
                <td><a href="<%=ResolveUrl("~/Pages/Employee/RequestDetail.aspx")%>?id=<%# Eval("RequestId") %>" style="font-weight:600;color:var(--color-primary)"><%# Eval("RequestNumber") %></a></td>
                <td><%# Eval("TypeName") %></td>
                <td>
                  <span style="display:inline-flex;align-items:center;gap:6px">
                    <span style="width:8px;height:8px;border-radius:50%;background:<%# Eval("CategoryColorHex") %>;flex-shrink:0"></span>
                    <%# Eval("CategoryName") %>
                  </span>
                </td>
                <td><span class="badge <%# Eval("StatusBadgeClass") %> badge-dot"><%# Eval("StatusDisplayName") %></span></td>
                <td><span class="badge badge-priority-<%# Eval("Priority").ToString().ToLower() %>"><%# Eval("Priority") %></span></td>
                <td>
                  <asp:Panel runat="server" Visible='<%# Eval("SlaDeadline") != null %>'>
                    <div class="badge <%# Eval("SlaStatusClass") %>" style="font-size:11px">
                      <%# GetSlaLabel((DateTime?)Eval("SlaDeadline")) %>
                    </div>
                  </asp:Panel>
                </td>
                <td style="color:var(--text-muted);font-size:12px"><%# ((DateTime)Eval("SubmittedAt")).ToString("dd MMM yyyy") %></td>
                <td><a href="<%=ResolveUrl("~/Pages/Employee/RequestDetail.aspx")%>?id=<%# Eval("RequestId") %>" class="btn btn-ghost btn-sm">View</a></td>
              </tr>
            </ItemTemplate>
          </asp:Repeater>
        </tbody>
      </table>
    </asp:Panel>
  </div>

  <!-- Quick Submit -->
  <div class="card">
    <div class="card-header"><h3 class="card-title">Quick Submit</h3></div>
    <div class="card-body">
      <p style="font-size:13px;color:var(--text-muted);margin-bottom:16px">Common request types — click to start a new request.</p>
      <div style="display:flex;flex-direction:column;gap:8px">
        <asp:Repeater ID="rptQuickTypes" runat="server">
          <ItemTemplate>
            <a href="<%=ResolveUrl("~/Pages/Employee/NewRequest.aspx")%>?type=<%# Eval("TypeId") %>"
               style="display:flex;align-items:center;gap:12px;padding:10px 14px;border:1px solid var(--border);border-radius:8px;text-decoration:none;transition:.15s;color:var(--text-primary)"
               onmouseenter="this.style.borderColor='var(--color-primary)';this.style.background='rgba(79,70,229,.04)'"
               onmouseleave="this.style.borderColor='var(--border)';this.style.background=''">
              <span style="width:32px;height:32px;border-radius:8px;background:<%# Eval("CategoryColorHex") %>22;display:flex;align-items:center;justify-content:center;font-size:16px"><%# Eval("IconClass") %></span>
              <div>
                <div style="font-size:13px;font-weight:600"><%# Eval("TypeName") %></div>
                <div style="font-size:11px;color:var(--text-muted)"><%# Eval("CategoryName") %> · SLA <%# Eval("SlaHours") %>h</div>
              </div>
              <span style="margin-left:auto;color:var(--text-muted)">→</span>
            </a>
          </ItemTemplate>
        </asp:Repeater>
      </div>
    </div>
  </div>

  <!-- My Announcements -->
  <div class="card">
    <div class="card-header">
      <h3 class="card-title">Status Board</h3>
      <a href="<%=ResolveUrl("~/Pages/Shared/StatusBoard.aspx")%>" class="btn btn-ghost btn-sm">View all →</a>
    </div>
    <div class="card-body">
      <asp:Repeater ID="rptAnnouncements" runat="server">
        <ItemTemplate>
          <div style="padding:12px;border-radius:8px;margin-bottom:10px;border:1px solid;<%# GetAnnStyle(Eval("Severity").ToString()) %>">
            <div style="display:flex;align-items:center;gap:8px;margin-bottom:4px">
              <span class="badge <%# Eval("SeverityBadgeClass") %>"><%# Eval("Severity") %></span>
              <span style="font-size:13px;font-weight:600"><%# Eval("Title") %></span>
            </div>
            <p style="font-size:12.5px;color:var(--text-muted);margin:0"><%# Eval("Content") %></p>
            <div style="font-size:11px;color:var(--text-muted);margin-top:6px"><%# ((DateTime)Eval("CreatedAt")).ToString("dd MMM yyyy HH:mm") %></div>
          </div>
        </ItemTemplate>
        <FooterTemplate>
          <asp:Panel ID="pnlNoAnn" runat="server">
            <div class="empty-state" style="padding:24px">
              <div class="empty-state-icon" style="font-size:32px">🌐</div>
              <div class="empty-state-title">No active announcements</div>
              <div class="empty-state-desc">All systems operational.</div>
            </div>
          </asp:Panel>
        </FooterTemplate>
      </asp:Repeater>
    </div>
  </div>

</div><!-- /grid -->

</asp:Content>

<asp:Content ID="Scripts" ContentPlaceHolderID="BodyScripts" runat="server">
<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js"></script>
</asp:Content>
