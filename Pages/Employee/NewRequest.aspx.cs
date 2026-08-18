using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using CRMP.BLL;
using CRMP.DAL;
using CRMP.Helpers;
using CRMP.Models;

namespace CRMP.Pages.Employee
{
    public partial class NewRequest : BasePage
    {
        protected override string[] GetRequiredRoles() => new[] { "EMPLOYEE" };

        private readonly CatalogRepository _catRepo = new CatalogRepository();
        private readonly RequestRepository _reqRepo  = new RequestRepository();
        private readonly UserRepository    _userRepo = new UserRepository();

        private int CurrentStep  => int.TryParse(hfStep.Value, out int s) ? s : 1;
        private int SelectedType => int.TryParse(hfTypeId.Value, out int t) ? t : 0;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Pre-select type if passed in query string
                if (int.TryParse(Request.QueryString["type"], out int preType))
                {
                    hfTypeId.Value = preType.ToString();
                    var rt = _catRepo.GetRequestTypeById(preType);
                    if (rt != null) { hfCategoryId.Value = rt.CategoryId.ToString(); }
                }
                LoadStep1();
            }
            // Handle dynamic load types postback
            if (Request.Form["__EVENTTARGET"] == "loadTypes" && !string.IsNullOrEmpty(Request.Form["__EVENTARGUMENT"]))
            {
                hfCategoryId.Value = Request.Form["__EVENTARGUMENT"];
                LoadCategoryTypes(int.Parse(hfCategoryId.Value));
                pnlTypes.Visible = true;
            }
        }

        private void LoadStep1()
        {
            hfStep.Value = "1";
            pnlStep1.Visible = true; pnlStep2.Visible = false;
            pnlStep3.Visible = false; pnlStep4.Visible = false;

            rptCategories.DataSource = _catRepo.GetCategories();
            rptCategories.DataBind();

            if (!string.IsNullOrEmpty(hfCategoryId.Value))
                LoadCategoryTypes(int.Parse(hfCategoryId.Value));
        }

        private void LoadCategoryTypes(int catId)
        {
            var types = _catRepo.GetRequestTypes(catId);
            var cat = _catRepo.GetCategoryById(catId);
            litCategoryName.Text = cat != null ? $" — {cat.CategoryName}" : "";
            rptTypes.DataSource = types;
            rptTypes.DataBind();
            pnlTypes.Visible = true;
        }

        protected void chkOnBehalf_Changed(object sender, EventArgs e)
        {
            pnlOnBehalf.Visible = chkOnBehalf.Checked;
        }

        protected void btnSearchBehalf_Click(object sender, EventArgs e)
        {
            var users = _userRepo.SearchInDivision(txtBehalfSearch.Text.Trim(), CurrentDivisionId, CurrentUserId);
            pnlBehalfResults.Visible = true;
            rptBehalfUsers.DataSource = users;
            rptBehalfUsers.DataBind();
        }

        protected void rptBehalfUsers_ItemCommand(object src, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "Select")
            {
                int uid = int.Parse(e.CommandArgument.ToString());
                var user = _userRepo.GetById(uid);
                hfBehalfUserId.Value = uid.ToString();
                litBehalfName.Text = user?.FullName ?? "";
                pnlSelectedBehalf.Visible = true;
                pnlBehalfResults.Visible  = false;
            }
        }

        protected void btnClearBehalf_Click(object sender, EventArgs e)
        {
            hfBehalfUserId.Value = "";
            pnlSelectedBehalf.Visible = false;
        }

        protected void btnStep1Next_Click(object sender, EventArgs e)
        {
            if (SelectedType == 0) { ShowToast("Please select a request type.", "error"); return; }
            LoadStep2();
        }

        private void LoadStep2()
        {
            hfStep.Value = "2";
            pnlStep1.Visible = false; pnlStep2.Visible = true;
            pnlStep3.Visible = false; pnlStep4.Visible = false;

            var rt = _catRepo.GetRequestTypeById(SelectedType);
            litStep2TypeName.Text = rt?.TypeName ?? "";

            // Load templates for this type
            LoadTemplates();

            // Render dynamic form fields
            phFields.Controls.Clear();
            if (rt?.Fields != null)
            {
                foreach (var field in rt.Fields)
                    phFields.Controls.Add(BuildFieldControl(field));

                // Emit field config JSON for JS
                var json = System.Web.Script.Serialization.JavaScriptSerializer_Compat.SerializeFields(rt.Fields);
                var hf = new HiddenField { ID = "hfFieldsJson", Value = json };
                phFields.Controls.Add(hf);

                // Startup script to init DynamicForm
                Page.ClientScript.RegisterStartupScript(GetType(), "initForm",
                    $"DynamicForm.init(document.getElementById('hfFieldsJson').value);", true);
            }
        }

        private Control BuildFieldControl(FormField field)
        {
            var wrap = new HtmlGenericControl("div");
            wrap.Attributes["class"] = "form-group";
            wrap.Attributes["id"] = $"field-wrap-{field.FieldId}";

            if (field.IsConditional)
                wrap.Style["display"] = "none"; // JS controls visibility

            var label = new HtmlGenericControl("label");
            label.Attributes["class"] = "form-label";
            label.Attributes["for"] = $"ff_{field.FieldId}";
            label.InnerText = field.FieldLabel;
            if (field.IsRequired) { var req = new HtmlGenericControl("span"); req.Attributes["class"] = "required"; req.InnerText = " *"; label.Controls.Add(req); }
            wrap.Controls.Add(label);

            Control input = CreateInputControl(field);
            wrap.Controls.Add(input);

            if (!string.IsNullOrEmpty(field.HelpText))
            {
                var help = new HtmlGenericControl("div");
                help.Attributes["class"] = "form-help";
                help.InnerText = field.HelpText;
                wrap.Controls.Add(help);
            }
            return wrap;
        }

        private Control CreateInputControl(FormField field)
        {
            string id = $"ff_{field.FieldId}";
            string name = $"ff_{field.FieldId}";

            switch (field.FieldType)
            {
                case "DROPDOWN":
                    var ddl = new DropDownList { ID = id, CssClass = "form-control" };
                    if (!field.IsRequired) ddl.Items.Add(new ListItem("— Select —", ""));
                    field.Options.ForEach(o => ddl.Items.Add(new ListItem(o.ValueText, o.ValueCode)));
                    return ddl;

                case "TEXTAREA":
                    var ta = new TextBox { ID = id, CssClass = "form-control", TextMode = TextBoxMode.MultiLine };
                    if (!string.IsNullOrEmpty(field.Placeholder)) ta.Attributes["placeholder"] = field.Placeholder;
                    return ta;

                case "CHECKBOX":
                    var cb = new CheckBox { ID = id, CssClass = "form-check-input" };
                    return cb;

                case "DATE":
                    var dt = new TextBox { ID = id, CssClass = "form-control", TextMode = TextBoxMode.Date };
                    return dt;

                case "NUMBER":
                    var num = new TextBox { ID = id, CssClass = "form-control", TextMode = TextBoxMode.Number };
                    if (!string.IsNullOrEmpty(field.Placeholder)) num.Attributes["placeholder"] = field.Placeholder;
                    return num;

                case "FILE":
                    var fu = new FileUpload { ID = id, CssClass = "form-control" };
                    return fu;

                default: // TEXT, EMAIL, PHONE
                    var txt = new TextBox { ID = id, CssClass = "form-control" };
                    if (!string.IsNullOrEmpty(field.Placeholder)) txt.Attributes["placeholder"] = field.Placeholder;
                    if (field.FieldType == "EMAIL")  txt.TextMode = TextBoxMode.Email;
                    if (field.FieldType == "PHONE")  txt.Attributes["type"] = "tel";
                    return txt;
            }
        }

        private void LoadTemplates()
        {
            // Load user's saved templates for this type
            var dt = OracleHelper.ExecuteQuerySql(@"
                SELECT TEMPLATE_ID, TEMPLATE_NAME FROM REQUEST_TEMPLATES
                WHERE (USER_ID=:P_U OR IS_SHARED=1) AND TYPE_ID=:P_T
                ORDER BY TEMPLATE_NAME",
                new[] { OracleHelper.ParamInt("P_U", CurrentUserId), OracleHelper.ParamInt("P_T", SelectedType) });

            ddlTemplates.Items.Clear();
            ddlTemplates.Items.Add(new ListItem("— Load a saved template —", ""));
            foreach (System.Data.DataRow row in dt.Rows)
                ddlTemplates.Items.Add(new ListItem(row["TEMPLATE_NAME"].ToString(), row["TEMPLATE_ID"].ToString()));
        }

        protected void btnLoadTemplate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(ddlTemplates.SelectedValue)) return;
            var dt = OracleHelper.ExecuteQuerySql(
                "SELECT FIELD_VALUES_JSON FROM REQUEST_TEMPLATES WHERE TEMPLATE_ID=:P_ID",
                new[] { OracleHelper.ParamInt("P_ID", int.Parse(ddlTemplates.SelectedValue)) });
            if (dt.Rows.Count > 0)
            {
                string json = dt.Rows[0]["FIELD_VALUES_JSON"].ToString();
                Page.ClientScript.RegisterStartupScript(GetType(), "tpl",
                    $"DynamicForm.applyTemplate({System.Web.HttpUtility.JavaScriptStringEncode(json, addDoubleQuotes: true)});", true);
            }
        }

        protected void btnStep2Next_Click(object sender, EventArgs e)
        {
            // Collect all dynamic field values from form
            var rt = _catRepo.GetRequestTypeById(SelectedType);
            var values = new Dictionary<int, string>();
            if (rt?.Fields != null)
            {
                foreach (var field in rt.Fields)
                {
                    string val = Request.Form[$"ff_{field.FieldId}"] ?? "";
                    values[field.FieldId] = val;
                }
            }
            // Serialize to JSON for storage across postbacks
            hfFieldValuesJson.Value = Newtonsoft_Compat.ToJson(values);
            hfStep.Value = "3";
            pnlStep1.Visible = false; pnlStep2.Visible = false;
            pnlStep3.Visible = true;  pnlStep4.Visible = false;
        }

        protected void btnStep3Next_Click(object sender, EventArgs e)
        {
            LoadReview();
        }

        private void LoadReview()
        {
            hfStep.Value = "4";
            pnlStep1.Visible = false; pnlStep2.Visible = false;
            pnlStep3.Visible = false; pnlStep4.Visible = true;

            var rt = _catRepo.GetRequestTypeById(SelectedType);
            litReviewType.Text    = rt?.TypeName ?? "";
            litReviewSummary.Text = SecurityHelper.HtmlEncode(txtSummary.Text);
            litReviewPriority.Text = $"<span class='badge badge-priority-{ddlPriority.SelectedValue.ToLower()}'>{ddlPriority.SelectedValue}</span>";

            if (rt?.WorkflowId.HasValue == true)
            {
                var wf = _catRepo.GetWorkflowWithStages(rt.WorkflowId.Value);
                rptReviewWorkflow.DataSource = wf?.Stages;
                rptReviewWorkflow.DataBind();
            }
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            if (!chkConfirmSubmit.Checked) { ShowToast("Please confirm before submitting.", "error"); return; }

            var rt = _catRepo.GetRequestTypeById(SelectedType);
            if (rt == null) { ShowToast("Invalid request type.", "error"); return; }

            // Build SLA deadline
            DateTime slaDeadline = DateTime.Now.AddHours(rt.SlaHours);

            var request = new Request
            {
                TypeId            = SelectedType,
                SubmitterUserId   = CurrentUserId,
                OnBehalfOfUserId  = string.IsNullOrEmpty(hfBehalfUserId.Value) ? (int?)null : int.Parse(hfBehalfUserId.Value),
                DivisionId        = CurrentDivisionId,
                Summary           = txtSummary.Text.Trim(),
                Priority          = ddlPriority.SelectedValue,
                SlaDeadline       = slaDeadline
            };

            // Determine first workflow stage
            int? firstStageId = null;
            if (rt.WorkflowId.HasValue)
            {
                var wf = _catRepo.GetWorkflowWithStages(rt.WorkflowId.Value);
                if (wf?.Stages.Count > 0)
                    firstStageId = wf.Stages[0].StageId;
            }
            request.CurrentStageId = firstStageId;

            // Generate request number
            string reqNum = SecurityHelper.GenerateRequestNumber(rt.TypeCode);

            // Save request
            int requestId = _reqRepo.Create(request, reqNum);

            // Save dynamic field values
            if (!string.IsNullOrEmpty(hfFieldValuesJson.Value))
            {
                var vals = SimpleJson.Deserialize(hfFieldValuesJson.Value);
                foreach (var kv in vals)
                    _reqRepo.SaveFieldValue(requestId, int.Parse(kv.Key), kv.Value);
            }

            // Save attachments
            if (fuAttachments.HasFiles)
            {
                foreach (var file in fuAttachments.PostedFiles)
                {
                    try
                    {
                        string relPath = FileHelper.SaveUploadedFile(file, requestId);
                        _reqRepo.SaveAttachment(new RequestAttachment
                        {
                            RequestId  = requestId,
                            FileName   = file.FileName,
                            FilePath   = relPath,
                            FileSize   = file.ContentLength,
                            MimeType   = file.ContentType,
                            UploadedBy = CurrentUserId
                        });
                    }
                    catch { /* Skip invalid files silently */ }
                }
            }

            // Timeline: submitted
            _reqRepo.AddTimeline(requestId, "SUBMITTED",
                $"Request submitted by {SessionHelper.FullName}" +
                (request.OnBehalfOfUserId.HasValue ? $" on behalf of user #{request.OnBehalfOfUserId}" : ""),
                CurrentUserId);

            // Kick off workflow
            WorkflowEngine.InitiateWorkflow(requestId);

            Response.Redirect($"~/Pages/Employee/RequestDetail.aspx?id={requestId}&submitted=1");
        }

        // Step back handlers
        protected void btnStep2Back_Click(object s, EventArgs e) { hfStep.Value = "1"; pnlStep1.Visible = true; pnlStep2.Visible = false; LoadStep1(); }
        protected void btnStep3Back_Click(object s, EventArgs e) { hfStep.Value = "2"; pnlStep2.Visible = true; pnlStep3.Visible = false; LoadStep2(); }
        protected void btnStep4Back_Click(object s, EventArgs e) { hfStep.Value = "3"; pnlStep3.Visible = true; pnlStep4.Visible = false; }
    }

    // Minimal JSON helpers (in production, wire up Newtonsoft.Json via NuGet)
    internal static class SimpleJson
    {
        public static Dictionary<string, string> Deserialize(string json)
        {
            var result = new Dictionary<string, string>();
            try
            {
                var ser = new System.Web.Script.Serialization.JavaScriptSerializer();
                var dict = ser.Deserialize<Dictionary<string, string>>(json);
                if (dict != null) foreach (var kv in dict) result[kv.Key] = kv.Value;
            }
            catch { }
            return result;
        }
    }
}
