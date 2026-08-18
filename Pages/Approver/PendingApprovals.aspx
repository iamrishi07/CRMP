<%@ Page Title="Pending Approvals" Language="C#" MasterPageFile="~/MasterPages/Site.Master" AutoEventWireup="true" CodeFile="PendingApprovals.aspx.cs" Inherits="CRMP.Pages.Approver.PendingApprovals" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">

<div class="page-header">
  <div class="page-header-left">
    <h1 class="page-title">Pending Approvals</h1>
    <p class="page-subtitle">Requests awaiting your approval — sorted by SLA urgency.</p>
  </div>
</div>

<!-- Bulk Action Bar (hidden until rows selected) -->
<div id="bulkActionBar" style="display:none;background:var(--color-primary);color:#fff;padding:12px 20px;border-radius:10px;margin-bottom:16px;align-items:center;gap:16px">
  <span class="bulk-count" style="font-weight:700"></span>
  <asp:HiddenField ID="hfBulkIds" runat="server"/>
  <asp:HiddenField ID="hfBulkAction" runat="server"/>
  <button class="btn btn-success btn-sm" onclick="triggerBulk('APPROVED')">✓ Bulk Approve</button>
  <button class="btn btn-danger btn-sm"  onclick="triggerBulk('REJECTED')">✕ Bulk Reject</button>
  <button class="btn btn-ghost btn-sm" style="color:rgba(255,255,255,.7)" onclick="clearSelections()">Cancel</button>
</div>

<!-- Filter Bar -->
<div class="filter-bar">
  <label>Category</label>
  <asp:DropDownList ID="ddlFilterCat" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="ApplyFilter">
    <asp:ListItem Text="All Categories" Value=""/>
  </asp:DropDownList>
  <label>Priority</label>
  <asp:DropDownList ID="ddlFilterPriority" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="ApplyFilter">
    <asp:ListItem Text="All" Value=""/>
    <asp:ListItem Text="Urgent" Value="URGENT"/>
    <asp:ListItem Text="High" Value="HIGH"/>
    <asp:ListItem Text="Normal" Value="NORMAL"/>
    <asp:ListItem Text="Low" Value="LOW"/>
  </asp:DropDownList>
  <label>SLA</label>
  <asp:DropDownList ID="ddlFilterSla" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="ApplyFilter">
    <asp:ListItem Text="All" Value=""/>
    <asp:ListItem Text="Breached" Value="breached"/>
    <asp:ListItem Text="At Risk (>75%)" Value="risk"/>
  </asp:DropDownList>
  <asp:Button ID="btnRefresh" runat="server" Text="Refresh" CssClass="btn btn-secondary btn-sm" OnClick="ApplyFilter"/>
</div>

<asp:Panel ID="pnlEmpty" runat="server" Visible="false">
  <div class="card">
    <div class="empty-state">
      <div class="empty-state-icon">✅</div>
      <div class="empty-state-title">All caught up!</div>
      <div class="empty-state-desc">No requests currently waiting for your approval.</div>
    </div>
  </div>
</asp:Panel>

<asp:Panel ID="pnlList" runat="server">
<div class="table-wrap">
<table class="data-table">
  <thead>
    <tr>
      <th class="col-check"><input type="checkbox" id="chkSelectAll" onchange="initSelectAll('chkSelectAll','row-check');this.dispatchEvent(new Event('change'))"/></th>
      <th>Request</th>
      <th>Type</th>
      <th>Submitted By</th>
      <th>Division</th>
      <th>Priority</th>
      <th>Current Stage</th>
      <th>SLA</th>
      <th>Submitted</th>
      <th class="col-actions">Action</th>
    </tr>
  </thead>
  <tbody>
    <asp:Repeater ID="rptApprovals" runat="server">
      <ItemTemplate>
        <tr>
          <td><input type="checkbox" class="row-check" value="<%# Eval("RequestId") %>" onchange="updateBulkBar()"/></td>
          <td>
            <a href="ApprovalDetail.aspx?id=<%# Eval("RequestId") %>" style="font-weight:700;color:var(--color-primary)">
              <%# Eval("RequestNumber") %>
            </a>
            <div style="font-size:11px;color:var(--text-muted);margin-top:2px;max-width:200px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap"><%# Eval("Summary") %></div>
          </td>
          <td>
            <span style="display:inline-flex;align-items:center;gap:6px">
              <span style="width:8px;height:8px;border-radius:50%;background:<%# Eval("CategoryColorHex") %>"></span>
              <%# Eval("TypeName") %>
            </span>
          </td>
          <td><%# Eval("SubmitterName") %></td>
          <td><span class="badge badge-gray"><%# Eval("DivisionName") %></span></td>
          <td><span class="badge badge-priority-<%# Eval("Priority").ToString().ToLower() %>"><%# Eval("Priority") %></span></td>
          <td style="font-size:12px"><%# Eval("CurrentStageName") %></td>
          <td>
            <span class="badge <%# Eval("SlaStatusClass") %>">
              <%# GetSlaLabel((DateTime?)Eval("SlaDeadline"), (bool)Eval("IsSlaBreached")) %>
            </span>
          </td>
          <td style="color:var(--text-muted);font-size:12px"><%# ((DateTime)Eval("SubmittedAt")).ToString("dd MMM yyyy") %></td>
          <td>
            <a href="ApprovalDetail.aspx?id=<%# Eval("RequestId") %>" class="btn btn-primary btn-sm">Review</a>
          </td>
        </tr>
      </ItemTemplate>
    </asp:Repeater>
  </tbody>
</table>
</div>
</asp:Panel>

<!-- Bulk Action Modal -->
<div id="bulkModal" class="modal-overlay" style="display:none">
  <div class="modal">
    <div class="modal-header">
      <h3 class="modal-title" id="bulkModalTitle">Bulk Action</h3>
      <button class="modal-close" onclick="closeModal('bulkModal')">×</button>
    </div>
    <div class="modal-body">
      <div class="form-group">
        <label class="form-label">Remarks <span id="bulkRemarksReq"></span></label>
        <textarea id="bulkRemarks" class="form-control" rows="4" placeholder="Enter remarks for all selected requests…"></textarea>
      </div>
    </div>
    <div class="modal-footer">
      <button class="btn btn-secondary" onclick="closeModal('bulkModal')">Cancel</button>
      <asp:Button ID="btnBulkSubmit" runat="server" CssClass="btn btn-primary" Text="Confirm" OnClick="btnBulkSubmit_Click" OnClientClick="prepBulkSubmit()"/>
    </div>
  </div>
</div>

</asp:Content>

<asp:Content ID="Scripts" ContentPlaceHolderID="BodyScripts" runat="server">
<script>
document.addEventListener('DOMContentLoaded', () => initSelectAll('chkSelectAll', 'row-check'));

function triggerBulk(action) {
    const ids = getSelectedIds('row-check');
    if (!ids.length) return;
    document.getElementById('<%= hfBulkAction.ClientID %>').value = action;
    document.getElementById('bulkModalTitle').textContent = action === 'APPROVED' ? 'Bulk Approve' : 'Bulk Reject';
    document.getElementById('bulkRemarksReq').textContent = action === 'REJECTED' ? '(required)' : '(optional)';
    openModal('bulkModal');
}

function prepBulkSubmit() {
    const ids = getSelectedIds('row-check');
    const remarks = document.getElementById('bulkRemarks').value.trim();
    const action = document.getElementById('<%= hfBulkAction.ClientID %>').value;
    if (action === 'REJECTED' && !remarks) { alert('Please provide a rejection reason.'); return false; }
    document.getElementById('<%= hfBulkIds.ClientID %>').value = ids.join(',');
    return true;
}

function clearSelections() {
    document.querySelectorAll('.row-check').forEach(c => c.checked = false);
    document.getElementById('chkSelectAll').checked = false;
    updateBulkBar();
}
</script>
</asp:Content>
