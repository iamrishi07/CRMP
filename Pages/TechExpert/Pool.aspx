<%@ Page Title="Tech Expert Pool" Language="C#" MasterPageFile="~/MasterPages/Site.Master" AutoEventWireup="true" CodeFile="Pool.aspx.cs" Inherits="CRMP.Pages.TechExpert.Pool" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">

<div class="page-header">
  <div class="page-header-left">
    <h1 class="page-title">Available Request Pool</h1>
    <p class="page-subtitle">Approved requests ready for pickup — sorted by SLA urgency.</p>
  </div>
  <a href="MyWork.aspx" class="btn btn-secondary">My Active Work →</a>
</div>

<asp:Panel ID="pnlEmpty" runat="server" Visible="false">
  <div class="card">
    <div class="empty-state" style="padding:60px">
      <div class="empty-state-icon">🎉</div>
      <div class="empty-state-title">Pool is clear!</div>
      <div class="empty-state-desc">No approved requests available for your categories right now.</div>
    </div>
  </div>
</asp:Panel>

<asp:Panel ID="pnlPool" runat="server">
<div style="display:flex;flex-direction:column;gap:12px">
  <asp:Repeater ID="rptPool" runat="server">
    <ItemTemplate>
      <div class="card" style="border-left:4px solid <%# Eval("CategoryColorHex") %>">
        <div class="card-body" style="display:flex;align-items:flex-start;gap:20px;padding:18px 20px">

          <!-- Category icon -->
          <div style="width:46px;height:46px;border-radius:10px;background:<%# Eval("CategoryColorHex") %>18;display:flex;align-items:center;justify-content:center;font-size:20px;flex-shrink:0">
            💻
          </div>

          <!-- Main info -->
          <div style="flex:1;min-width:0">
            <div style="display:flex;align-items:center;gap:10px;flex-wrap:wrap">
              <a href="WorkDetail.aspx?id=<%# Eval("RequestId") %>" style="font-size:15px;font-weight:700;color:var(--color-primary)"><%# Eval("RequestNumber") %></a>
              <span class="badge badge-priority-<%# Eval("Priority").ToString().ToLower() %>"><%# Eval("Priority") %></span>
              <span class="badge <%# Eval("SlaStatusClass") %>">
                <%# GetSlaLabel((DateTime?)Eval("SlaDeadline")) %>
              </span>
            </div>
            <div style="font-size:14px;font-weight:600;color:var(--text-primary);margin-top:4px"><%# Eval("TypeName") %></div>
            <div style="font-size:13px;color:var(--text-secondary);margin-top:2px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;max-width:600px">
              <%# Eval("Summary") %>
            </div>
            <div style="display:flex;align-items:center;gap:16px;margin-top:8px;font-size:12px;color:var(--text-muted)">
              <span>👤 <%# Eval("SubmitterName") %></span>
              <span>🏢 <%# Eval("DivisionName") %></span>
              <span>📅 <%# ((DateTime)Eval("SubmittedAt")).ToString("dd MMM yyyy") %></span>
            </div>
          </div>

          <!-- Action -->
          <div style="flex-shrink:0">
            <asp:Button CommandName="Pickup" CommandArgument='<%# Eval("RequestId") %>' runat="server"
                        Text="Pick Up" CssClass="btn btn-primary"
                        OnCommand="btnPickup_Command"
                        OnClientClick="return confirm('Pick up this request? It will be assigned to you.')"/>
          </div>
        </div>
      </div>
    </ItemTemplate>
  </asp:Repeater>
</div>
</asp:Panel>

</asp:Content>
