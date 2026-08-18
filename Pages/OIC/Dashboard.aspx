<%@ Page Title="OIC IT Dashboard" Language="C#" MasterPageFile="~/MasterPages/Site.Master" AutoEventWireup="true" CodeFile="Dashboard.aspx.cs" Inherits="CRMP.Pages.OIC.Dashboard" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">

<div class="page-header">
  <div class="page-header-left">
    <h1 class="page-title">OIC IT Dashboard</h1>
    <p class="page-subtitle">Organization-wide service request visibility, performance metrics, and control.</p>
  </div>
  <div style="display:flex;gap:10px">
    <a href="AllRequests.aspx" class="btn btn-secondary">View All Requests</a>
    <a href="Reports.aspx" class="btn btn-primary">📊 Reports</a>
  </div>
</div>

<!-- TOP STAT CARDS -->
<div class="grid grid-4 mb-6">
  <div class="stat-card stat-primary">
    <div class="stat-card-icon" style="background:#EEF2FF;color:var(--color-primary);font-size:22px">📋</div>
    <div class="stat-card-value"><asp:Literal ID="litTotal" runat="server">0</asp:Literal></div>
    <div class="stat-card-label">Total Requests (All Time)</div>
  </div>
  <div class="stat-card stat-warning">
    <div class="stat-card-icon" style="background:#FFFBEB;font-size:22px">⏳</div>
    <div class="stat-card-value"><asp:Literal ID="litPending" runat="server">0</asp:Literal></div>
    <div class="stat-card-label">Pending Approval</div>
  </div>
  <div class="stat-card stat-danger">
    <div class="stat-card-icon" style="background:#FEF2F2;font-size:22px">⚠️</div>
    <div class="stat-card-value"><asp:Literal ID="litBreached" runat="server">0</asp:Literal></div>
    <div class="stat-card-label">SLA Breached</div>
  </div>
  <div class="stat-card stat-success">
    <div class="stat-card-icon" style="background:#ECFDF5;font-size:22px">✅</div>
    <div class="stat-card-value"><asp:Literal ID="litResolved" runat="server">0</asp:Literal></div>
    <div class="stat-card-label">Resolved This Month</div>
  </div>
</div>

<!-- CHARTS ROW -->
<div class="grid grid-2 gap-6 mb-6">
  <div class="card">
    <div class="card-header"><h3 class="card-title">Requests by Status</h3></div>
    <div class="card-body">
      <div class="chart-wrap" style="height:260px">
        <canvas id="chartStatus"></canvas>
      </div>
    </div>
  </div>
  <div class="card">
    <div class="card-header"><h3 class="card-title">Requests by Category</h3></div>
    <div class="card-body">
      <div class="chart-wrap" style="height:260px">
        <canvas id="chartCategory"></canvas>
      </div>
    </div>
  </div>
  <div class="card" style="grid-column:span 2">
    <div class="card-header">
      <h3 class="card-title">Request Volume — Last 30 Days</h3>
    </div>
    <div class="card-body">
      <div class="chart-wrap" style="height:220px">
        <canvas id="chartTrend"></canvas>
      </div>
    </div>
  </div>
</div>

<!-- BOTTOM PANELS -->
<div class="grid grid-2 gap-6">

  <!-- SLA Breached Requests -->
  <div class="card">
    <div class="card-header">
      <h3 class="card-title" style="color:var(--color-danger)">⚠ SLA Breached</h3>
      <a href="AllRequests.aspx?sla=1" class="btn btn-ghost btn-sm">View all →</a>
    </div>
    <div style="overflow-y:auto;max-height:320px">
      <table class="data-table">
        <thead><tr><th>Request</th><th>Type</th><th>Division</th><th>Age</th></tr></thead>
        <tbody>
          <asp:Repeater ID="rptBreached" runat="server">
            <ItemTemplate>
              <tr>
                <td><a href="../OIC/AllRequests.aspx?search=<%# Eval("RequestNumber") %>" style="font-weight:700;color:var(--color-danger)"><%# Eval("RequestNumber") %></a></td>
                <td style="font-size:12px"><%# Eval("TypeName") %></td>
                <td><span class="badge badge-gray" style="font-size:11px"><%# Eval("DivisionName") %></span></td>
                <td style="font-size:12px;color:var(--color-danger)"><%# GetAge((DateTime)Eval("SubmittedAt")) %></td>
              </tr>
            </ItemTemplate>
          </asp:Repeater>
        </tbody>
      </table>
      <asp:Panel ID="pnlNoBreached" runat="server" Visible="false">
        <div class="empty-state" style="padding:30px"><div class="empty-state-icon" style="font-size:28px">✅</div><div class="empty-state-title">No SLA breaches</div></div>
      </asp:Panel>
    </div>
  </div>

  <!-- Quick Admin Links -->
  <div class="card">
    <div class="card-header"><h3 class="card-title">Administration</h3></div>
    <div class="card-body">
      <div style="display:grid;grid-template-columns:1fr 1fr;gap:10px">
        <%
          var adminLinks = new[]{
            new{Href="UserManagement.aspx",Icon="👥",Title="Users & Roles",Desc="Manage users and role assignments"},
            new{Href="WorkflowAdmin.aspx",Icon="⚙️",Title="Workflows",Desc="Configure approval workflows"},
            new{Href="FormBuilder.aspx",Icon="📝",Title="Form Builder",Desc="Manage request type forms"},
            new{Href="SLAConfig.aspx",Icon="⏱️",Title="SLA Config",Desc="Set SLA hours per request type"},
            new{Href="KnowledgeBase.aspx",Icon="📚",Title="Knowledge Base",Desc="Manage KB articles"},
            new{Href="Announcements.aspx",Icon="📢",Title="Announcements",Desc="Post status updates"}
          };
          foreach(var l in adminLinks) { %>
        <a href="<%=l.Href%>" style="display:flex;align-items:flex-start;gap:10px;padding:12px;border:1px solid var(--border);border-radius:10px;text-decoration:none;transition:.15s;color:var(--text-primary)"
           onmouseenter="this.style.borderColor='var(--color-primary)';this.style.background='rgba(79,70,229,.04)'"
           onmouseleave="this.style.borderColor='var(--border)';this.style.background=''">
          <span style="font-size:20px;flex-shrink:0"><%=l.Icon%></span>
          <div>
            <div style="font-size:13px;font-weight:600"><%=l.Title%></div>
            <div style="font-size:11px;color:var(--text-muted)"><%=l.Desc%></div>
          </div>
        </a>
        <% } %>
      </div>
    </div>
  </div>

</div>

<asp:HiddenField ID="hfStatusJson" runat="server"/>
<asp:HiddenField ID="hfCategoryJson" runat="server"/>
<asp:HiddenField ID="hfTrendJson" runat="server"/>

</asp:Content>

<asp:Content ID="Scripts" ContentPlaceHolderID="BodyScripts" runat="server">
<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js"></script>
<script>
const statusData   = JSON.parse(document.getElementById('<%= hfStatusJson.ClientID %>').value   || '{}');
const categoryData = JSON.parse(document.getElementById('<%= hfCategoryJson.ClientID %>').value || '{}');
const trendData    = JSON.parse(document.getElementById('<%= hfTrendJson.ClientID %>').value    || '{}');

// Status donut
new Chart(document.getElementById('chartStatus'), {
    type: 'doughnut',
    data: {
        labels: statusData.labels || [],
        datasets: [{ data: statusData.values || [], backgroundColor: statusData.colors || [] }]
    },
    options: { responsive:true, maintainAspectRatio:false, plugins:{ legend:{ position:'right', labels:{ font:{family:'Inter',size:12}, padding:14 } } } }
});

// Category bar
new Chart(document.getElementById('chartCategory'), {
    type: 'bar',
    data: {
        labels: categoryData.labels || [],
        datasets: [{ data: categoryData.values || [], backgroundColor: categoryData.colors || [], borderRadius:6, borderSkipped:false }]
    },
    options: { responsive:true, maintainAspectRatio:false, plugins:{ legend:{display:false} }, scales:{ y:{ beginAtZero:true, ticks:{stepSize:1} }, x:{ grid:{display:false} } } }
});

// Trend line
new Chart(document.getElementById('chartTrend'), {
    type: 'line',
    data: {
        labels: trendData.labels || [],
        datasets: [{ label:'Requests', data: trendData.values || [],
            borderColor:'#4F46E5', backgroundColor:'rgba(79,70,229,.08)',
            tension:.4, fill:true, pointBackgroundColor:'#4F46E5', pointRadius:3 }]
    },
    options: { responsive:true, maintainAspectRatio:false, plugins:{legend:{display:false}}, scales:{ y:{beginAtZero:true,ticks:{stepSize:1}}, x:{grid:{display:false}} } }
});
</script>
</asp:Content>
