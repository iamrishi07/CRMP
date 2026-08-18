<%@ Page Title="New Request" Language="C#" MasterPageFile="~/MasterPages/Site.Master" AutoEventWireup="true" CodeFile="NewRequest.aspx.cs" Inherits="CRMP.Pages.Employee.NewRequest" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">

<div class="page-header">
  <div class="page-header-left">
    <div class="breadcrumb"><a href="Dashboard.aspx">Dashboard</a><span class="breadcrumb-sep">›</span><span>New Request</span></div>
    <h1 class="page-title">New Service Request</h1>
    <p class="page-subtitle">Choose a category, fill in the details, and submit for approval.</p>
  </div>
</div>

<!-- Wizard Steps -->
<div class="wizard-steps" id="wizardSteps">
  <div class="wizard-step" id="step1Indicator">
    <div class="wizard-step-inner">
      <div class="wizard-step-num">1</div>
      <div class="wizard-step-label">Category</div>
    </div>
  </div>
  <div class="wizard-connector" id="conn1"></div>
  <div class="wizard-step" id="step2Indicator">
    <div class="wizard-step-inner">
      <div class="wizard-step-num">2</div>
      <div class="wizard-step-label">Details</div>
    </div>
  </div>
  <div class="wizard-connector" id="conn2"></div>
  <div class="wizard-step" id="step3Indicator">
    <div class="wizard-step-inner">
      <div class="wizard-step-num">3</div>
      <div class="wizard-step-label">Attachments</div>
    </div>
  </div>
  <div class="wizard-connector" id="conn3"></div>
  <div class="wizard-step" id="step4Indicator">
    <div class="wizard-step-inner">
      <div class="wizard-step-num">4</div>
      <div class="wizard-step-label">Review</div>
    </div>
  </div>
</div>

<asp:HiddenField ID="hfStep" runat="server" Value="1"/>
<asp:HiddenField ID="hfTypeId" runat="server"/>
<asp:HiddenField ID="hfCategoryId" runat="server"/>

<!-- ── STEP 1: Category & Type ──────────────────────────────────────────── -->
<asp:Panel ID="pnlStep1" runat="server">
<div class="card" style="max-width:900px;margin:0 auto">
  <div class="card-header">
    <h3 class="card-title">Step 1: Select Category & Request Type</h3>
  </div>
  <div class="card-body">

    <!-- On-behalf option -->
    <div class="card" style="background:var(--gray-50);margin-bottom:24px">
      <div class="card-body" style="padding:16px">
        <div class="form-check">
          <asp:CheckBox ID="chkOnBehalf" runat="server" AutoPostBack="true" OnCheckedChanged="chkOnBehalf_Changed"/>
          <label class="form-check-label" for="<%= chkOnBehalf.ClientID %>">
            Submit on behalf of a colleague in my division
          </label>
        </div>
        <asp:Panel ID="pnlOnBehalf" runat="server" Visible="false" style="margin-top:14px">
          <div class="form-group" style="margin-bottom:0">
            <label class="form-label">Colleague Name <span class="required">*</span></label>
            <div class="input-group">
              <asp:TextBox ID="txtBehalfSearch" runat="server" CssClass="form-control" placeholder="Type colleague's name…" autocomplete="off"/>
              <asp:Button ID="btnSearchBehalf" runat="server" Text="Search" CssClass="btn btn-secondary" OnClick="btnSearchBehalf_Click"/>
            </div>
            <asp:Panel ID="pnlBehalfResults" runat="server" Visible="false" style="margin-top:8px">
              <asp:Repeater ID="rptBehalfUsers" runat="server" OnItemCommand="rptBehalfUsers_ItemCommand">
                <ItemTemplate>
                  <div style="display:flex;align-items:center;justify-content:space-between;padding:10px 12px;border:1px solid var(--border);border-radius:8px;margin-bottom:6px">
                    <div>
                      <div style="font-size:13px;font-weight:600"><%# Eval("FullName") %></div>
                      <div style="font-size:12px;color:var(--text-muted)"><%# Eval("Designation") %></div>
                    </div>
                    <asp:Button CommandName="Select" CommandArgument='<%# Eval("UserId") %>' Text="Select" CssClass="btn btn-outline btn-sm" runat="server"/>
                  </div>
                </ItemTemplate>
              </asp:Repeater>
            </asp:Panel>
            <asp:Panel ID="pnlSelectedBehalf" runat="server" Visible="false">
              <div class="badge badge-green" style="padding:6px 12px;font-size:13px">
                ✓ On behalf of: <asp:Literal ID="litBehalfName" runat="server"/>
                <asp:LinkButton runat="server" OnClick="btnClearBehalf_Click" style="color:inherit;margin-left:8px;font-weight:700">×</asp:LinkButton>
              </div>
              <asp:HiddenField ID="hfBehalfUserId" runat="server"/>
            </asp:Panel>
          </div>
        </asp:Panel>
      </div>
    </div>

    <!-- Category cards -->
    <h4 style="font-size:14px;font-weight:600;color:var(--text-secondary);margin-bottom:14px;text-transform:uppercase;letter-spacing:.5px">Select a Category</h4>
    <div class="grid grid-4" style="gap:12px;margin-bottom:24px">
      <asp:Repeater ID="rptCategories" runat="server">
        <ItemTemplate>
          <div class="category-card" id="cat-<%# Eval("CategoryId") %>" onclick="selectCategory(<%# Eval("CategoryId") %>,'<%# Eval("CategoryName") %>')"
               style="border:2px solid var(--border);border-radius:12px;padding:16px;cursor:pointer;transition:.2s;text-align:center;background:#fff">
            <div style="width:44px;height:44px;border-radius:10px;background:<%# Eval("ColorHex") %>22;display:flex;align-items:center;justify-content:center;font-size:22px;margin:0 auto 10px">
              <span style="color:<%# Eval("ColorHex") %>">●</span>
            </div>
            <div style="font-size:13.5px;font-weight:600"><%# Eval("CategoryName") %></div>
            <div style="font-size:11px;color:var(--text-muted);margin-top:3px"><%# Eval("Description") %></div>
          </div>
        </ItemTemplate>
      </asp:Repeater>
    </div>

    <!-- Request type list (shown after category selected) -->
    <asp:Panel ID="pnlTypes" runat="server" Visible="false">
      <h4 style="font-size:14px;font-weight:600;color:var(--text-secondary);margin-bottom:14px;text-transform:uppercase;letter-spacing:.5px">
        Select Request Type
        <asp:Literal ID="litCategoryName" runat="server"/>
      </h4>
      <div style="display:flex;flex-direction:column;gap:8px" id="typeList">
        <asp:Repeater ID="rptTypes" runat="server">
          <ItemTemplate>
            <label style="display:flex;align-items:center;gap:14px;padding:14px 16px;border:2px solid var(--border);border-radius:10px;cursor:pointer;transition:.15s;background:#fff"
                   onmouseenter="this.style.borderColor='var(--color-primary)'"
                   onmouseleave="this.style.borderColor='var(--border)'">
              <input type="radio" name="requestType" value="<%# Eval("TypeId") %>" onchange="typeSelected(this)"
                     style="accent-color:var(--color-primary);width:16px;height:16px"/>
              <div style="flex:1">
                <div style="font-size:14px;font-weight:600"><%# Eval("TypeName") %></div>
                <div style="font-size:12px;color:var(--text-muted);margin-top:2px"><%# Eval("Description") %></div>
              </div>
              <div style="text-align:right;flex-shrink:0">
                <div class="badge badge-blue">SLA <%# Eval("SlaHours") %>h</div>
              </div>
            </label>
          </ItemTemplate>
        </asp:Repeater>
      </div>
    </asp:Panel>

  </div>
  <div class="card-footer" style="display:flex;justify-content:flex-end">
    <asp:Button ID="btnStep1Next" runat="server" Text="Continue →" CssClass="btn btn-primary"
                OnClick="btnStep1Next_Click" Enabled="false"/>
  </div>
</div>
</asp:Panel>

<!-- ── STEP 2: Details Form ─────────────────────────────────────────────── -->
<asp:Panel ID="pnlStep2" runat="server" Visible="false">
<div class="card" style="max-width:900px;margin:0 auto">
  <div class="card-header">
    <h3 class="card-title">Step 2: Request Details</h3>
    <span class="badge badge-indigo"><asp:Literal ID="litStep2TypeName" runat="server"/></span>
  </div>
  <div class="card-body">

    <!-- KB Suggest Area -->
    <div id="kb-suggest-wrap" style="margin-bottom:20px"></div>

    <!-- Summary -->
    <div class="form-group">
      <label class="form-label">Brief Summary <span class="required">*</span></label>
      <asp:TextBox ID="txtSummary" runat="server" CssClass="form-control" MaxLength="1000"
                   placeholder="Describe your issue or request in one sentence…"
                   oninput="DynamicForm.setupKbSuggest && handleSumInput(this.value)"/>
    </div>

    <!-- Priority -->
    <div class="form-group">
      <label class="form-label">Priority</label>
      <asp:DropDownList ID="ddlPriority" runat="server" CssClass="form-control">
        <asp:ListItem Text="Low"    Value="LOW"/>
        <asp:ListItem Text="Normal" Value="NORMAL" Selected="True"/>
        <asp:ListItem Text="High"   Value="HIGH"/>
        <asp:ListItem Text="Urgent" Value="URGENT"/>
      </asp:DropDownList>
    </div>

    <!-- Dynamic form fields rendered by server -->
    <asp:PlaceHolder ID="phFields" runat="server"/>

    <!-- Template row -->
    <div style="border-top:1px solid var(--border);padding-top:16px;margin-top:8px">
      <div style="display:flex;align-items:center;gap:12px">
        <asp:DropDownList ID="ddlTemplates" runat="server" CssClass="form-control" style="max-width:260px">
          <asp:ListItem Text="— Load a saved template —" Value=""/>
        </asp:DropDownList>
        <asp:Button ID="btnLoadTemplate" runat="server" Text="Load" CssClass="btn btn-secondary btn-sm" OnClick="btnLoadTemplate_Click"/>
      </div>
    </div>

  </div>
  <div class="card-footer" style="display:flex;justify-content:space-between">
    <asp:Button ID="btnStep2Back" runat="server" Text="← Back" CssClass="btn btn-secondary" OnClick="btnStep2Back_Click"/>
    <asp:Button ID="btnStep2Next" runat="server" Text="Continue →" CssClass="btn btn-primary" OnClick="btnStep2Next_Click"/>
  </div>
</div>
</asp:Panel>

<!-- ── STEP 3: Attachments ─────────────────────────────────────────────── -->
<asp:Panel ID="pnlStep3" runat="server" Visible="false">
<div class="card" style="max-width:900px;margin:0 auto">
  <div class="card-header">
    <h3 class="card-title">Step 3: Attachments (Optional)</h3>
  </div>
  <div class="card-body">
    <div id="dropzone"
         style="border:2px dashed var(--gray-300);border-radius:12px;padding:40px;text-align:center;transition:.2s;cursor:pointer"
         ondragover="event.preventDefault();this.style.borderColor='var(--color-primary)'"
         ondragleave="this.style.borderColor='var(--gray-300)'"
         ondrop="handleDrop(event)">
      <div style="font-size:36px;margin-bottom:12px">📎</div>
      <div style="font-size:15px;font-weight:600;color:var(--text-primary)">Drop files here or click to upload</div>
      <div style="font-size:13px;color:var(--text-muted);margin-top:6px">PDF, DOC, XLS, PNG, JPG, ZIP · Max 10 MB each</div>
      <asp:FileUpload ID="fuAttachments" runat="server" AllowMultiple="true"
                      style="display:none" onchange="previewFiles(this)"/>
      <button type="button" class="btn btn-outline btn-sm" style="margin-top:16px"
              onclick="document.getElementById('<%= fuAttachments.ClientID %>').click()">
        Browse Files
      </button>
    </div>
    <div id="filePreview" style="margin-top:16px"></div>
  </div>
  <div class="card-footer" style="display:flex;justify-content:space-between">
    <asp:Button ID="btnStep3Back" runat="server" Text="← Back" CssClass="btn btn-secondary" OnClick="btnStep3Back_Click"/>
    <asp:Button ID="btnStep3Next" runat="server" Text="Continue →" CssClass="btn btn-primary" OnClick="btnStep3Next_Click"/>
  </div>
</div>
</asp:Panel>

<!-- ── STEP 4: Review & Submit ─────────────────────────────────────────── -->
<asp:Panel ID="pnlStep4" runat="server" Visible="false">
<div class="card" style="max-width:900px;margin:0 auto">
  <div class="card-header">
    <h3 class="card-title">Step 4: Review & Submit</h3>
  </div>
  <div class="card-body">
    <div style="display:grid;grid-template-columns:1fr 1fr;gap:24px;margin-bottom:24px">
      <div>
        <div style="font-size:11px;font-weight:700;color:var(--text-muted);text-transform:uppercase;letter-spacing:.5px;margin-bottom:6px">Request Type</div>
        <div style="font-size:15px;font-weight:600"><asp:Literal ID="litReviewType" runat="server"/></div>
      </div>
      <div>
        <div style="font-size:11px;font-weight:700;color:var(--text-muted);text-transform:uppercase;letter-spacing:.5px;margin-bottom:6px">Priority</div>
        <asp:Literal ID="litReviewPriority" runat="server"/>
      </div>
      <div style="grid-column:span 2">
        <div style="font-size:11px;font-weight:700;color:var(--text-muted);text-transform:uppercase;letter-spacing:.5px;margin-bottom:6px">Summary</div>
        <div style="font-size:14px"><asp:Literal ID="litReviewSummary" runat="server"/></div>
      </div>
    </div>

    <!-- Approval workflow preview -->
    <div class="card" style="background:var(--gray-50);margin-bottom:20px">
      <div class="card-header" style="padding:12px 16px"><span style="font-size:13px;font-weight:600">Approval Workflow</span></div>
      <div class="card-body" style="padding:14px 16px">
        <asp:Repeater ID="rptReviewWorkflow" runat="server">
          <ItemTemplate>
            <div style="display:flex;align-items:center;gap:10px;padding:6px 0">
              <span style="width:24px;height:24px;border-radius:50%;background:var(--gray-200);display:flex;align-items:center;justify-content:center;font-size:11px;font-weight:700;flex-shrink:0"><%# Eval("StageOrder") %></span>
              <span style="font-size:13px"><%# Eval("StageName") %></span>
              <span style="margin-left:auto;font-size:11px;color:var(--text-muted)"><%# Eval("RoleName") %></span>
            </div>
          </ItemTemplate>
        </asp:Repeater>
      </div>
    </div>

    <div class="form-check">
      <asp:CheckBox ID="chkConfirmSubmit" runat="server"/>
      <label class="form-check-label" for="<%= chkConfirmSubmit.ClientID %>">
        I confirm the above information is accurate and complete.
      </label>
    </div>
  </div>
  <div class="card-footer" style="display:flex;justify-content:space-between">
    <asp:Button ID="btnStep4Back" runat="server" Text="← Back" CssClass="btn btn-secondary" OnClick="btnStep4Back_Click"/>
    <asp:Button ID="btnSubmit" runat="server" Text="✓ Submit Request" CssClass="btn btn-primary"
                OnClick="btnSubmit_Click" OnClientClick="return validateSubmit()"/>
  </div>
</div>
</asp:Panel>

<asp:HiddenField ID="hfFieldValuesJson" runat="server"/>

</asp:Content>

<asp:Content ID="Scripts" ContentPlaceHolderID="BodyScripts" runat="server">
<script src="<%=ResolveUrl("~/Scripts/dynamicForm.js")%>"></script>
<script>
// ── Category / Type selection ──────────────────────────────────────
function selectCategory(id, name) {
    document.querySelectorAll('.category-card').forEach(c => {
        c.style.borderColor = 'var(--border)';
        c.style.background = '#fff';
    });
    const card = document.getElementById('cat-' + id);
    if (card) { card.style.borderColor = 'var(--color-primary)'; card.style.background = 'rgba(79,70,229,.04)'; }
    document.getElementById('<%= hfCategoryId.ClientID %>').value = id;
    // Load types via postback
    __doPostBack('loadTypes', id);
}

function typeSelected(radio) {
    document.querySelectorAll('[name="requestType"]').forEach(r => {
        r.closest('label').style.borderColor = r.checked ? 'var(--color-primary)' : 'var(--border)';
        r.closest('label').style.background  = r.checked ? 'rgba(79,70,229,.04)' : '#fff';
    });
    document.getElementById('<%= hfTypeId.ClientID %>').value = radio.value;
    document.getElementById('<%= btnStep1Next.ClientID %>').disabled = false;
}

// ── File upload preview ────────────────────────────────────────────
function previewFiles(input) {
    const preview = document.getElementById('filePreview');
    preview.innerHTML = '';
    Array.from(input.files).forEach(f => {
        preview.innerHTML += `<div style="display:flex;align-items:center;gap:10px;padding:8px 12px;border:1px solid var(--border);border-radius:8px;margin-bottom:6px;font-size:13px">
            <span>📄</span><span style="flex:1">${f.name}</span><span style="color:var(--text-muted)">${(f.size/1024).toFixed(1)} KB</span>
        </div>`;
    });
}

function handleDrop(e) {
    e.preventDefault();
    const fu = document.getElementById('<%= fuAttachments.ClientID %>');
    if (fu && e.dataTransfer.files.length) {
        fu.files = e.dataTransfer.files;
        previewFiles(fu);
    }
    document.getElementById('dropzone').style.borderColor = 'var(--gray-300)';
}

// ── Validation ─────────────────────────────────────────────────────
function validateSubmit() {
    if (!document.getElementById('<%= chkConfirmSubmit.ClientID %>').checked) {
        alert('Please confirm the information before submitting.');
        return false;
    }
    return true;
}

// ── Wizard step indicators ─────────────────────────────────────────
(function() {
    const step = parseInt(document.getElementById('<%= hfStep.ClientID %>').value) || 1;
    for (let i = 1; i <= 4; i++) {
        const el = document.getElementById('step' + i + 'Indicator');
        if (!el) continue;
        if (i < step) { el.classList.add('done'); }
        else if (i === step) { el.classList.add('active'); }
        if (i < step) {
            const conn = document.getElementById('conn' + i);
            if (conn) conn.classList.add('done');
        }
    }
})();
</script>
</asp:Content>
