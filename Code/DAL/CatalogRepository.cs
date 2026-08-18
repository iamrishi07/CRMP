using System;
using System.Collections.Generic;
using System.Data;
using CRMP.Helpers;
using CRMP.Models;
using Oracle.ManagedDataAccess.Client;

namespace CRMP.DAL
{
    public class CatalogRepository
    {
        // ── Service Categories ────────────────────────────────────────────────
        public List<ServiceCategory> GetCategories(bool activeOnly = true)
        {
            string sql = @"
                SELECT CATEGORY_ID, CATEGORY_NAME, CATEGORY_CODE, DESCRIPTION,
                       ICON_CLASS, COLOR_HEX, SORT_ORDER, IS_ACTIVE
                FROM   SERVICE_CATEGORIES
                " + (activeOnly ? "WHERE IS_ACTIVE = 1 " : "") + "ORDER BY SORT_ORDER";

            var dt = OracleHelper.ExecuteQuerySql(sql);
            var list = new List<ServiceCategory>();
            foreach (DataRow row in dt.Rows)
                list.Add(MapCategory(row));
            return list;
        }

        public ServiceCategory GetCategoryById(int id)
        {
            var dt = OracleHelper.ExecuteQuerySql(
                "SELECT * FROM SERVICE_CATEGORIES WHERE CATEGORY_ID = :P_ID",
                new[] { OracleHelper.ParamInt("P_ID", id) });
            return dt.Rows.Count > 0 ? MapCategory(dt.Rows[0]) : null;
        }

        // ── Request Types ─────────────────────────────────────────────────────
        public List<RequestType> GetRequestTypes(int? categoryId = null, bool activeOnly = true)
        {
            string where = activeOnly ? "WHERE rt.IS_ACTIVE = 1" : "WHERE 1=1";
            var parms = new List<OracleParameter>();
            if (categoryId.HasValue)
            {
                where += " AND rt.CATEGORY_ID = :P_CAT";
                parms.Add(OracleHelper.ParamInt("P_CAT", categoryId));
            }

            var dt = OracleHelper.ExecuteQuerySql($@"
                SELECT rt.TYPE_ID, rt.CATEGORY_ID, sc.CATEGORY_NAME,
                       rt.TYPE_NAME, rt.TYPE_CODE, rt.DESCRIPTION,
                       rt.SLA_HOURS, rt.WORKFLOW_ID, wf.WORKFLOW_NAME,
                       rt.ICON_CLASS, rt.IS_ACTIVE, rt.IS_CONNECTION_TYPE
                FROM   REQUEST_TYPES rt
                JOIN   SERVICE_CATEGORIES sc ON sc.CATEGORY_ID = rt.CATEGORY_ID
                LEFT JOIN WORKFLOWS wf ON wf.WORKFLOW_ID = rt.WORKFLOW_ID
                {where}
                ORDER BY sc.SORT_ORDER, rt.TYPE_NAME", parms.ToArray());

            var list = new List<RequestType>();
            foreach (DataRow row in dt.Rows)
                list.Add(MapRequestType(row));
            return list;
        }

        public RequestType GetRequestTypeById(int typeId)
        {
            var dt = OracleHelper.ExecuteQuerySql(@"
                SELECT rt.TYPE_ID, rt.CATEGORY_ID, sc.CATEGORY_NAME,
                       rt.TYPE_NAME, rt.TYPE_CODE, rt.DESCRIPTION,
                       rt.SLA_HOURS, rt.WORKFLOW_ID, wf.WORKFLOW_NAME,
                       rt.ICON_CLASS, rt.IS_ACTIVE, rt.IS_CONNECTION_TYPE
                FROM   REQUEST_TYPES rt
                JOIN   SERVICE_CATEGORIES sc ON sc.CATEGORY_ID = rt.CATEGORY_ID
                LEFT JOIN WORKFLOWS wf ON wf.WORKFLOW_ID = rt.WORKFLOW_ID
                WHERE  rt.TYPE_ID = :P_ID",
                new[] { OracleHelper.ParamInt("P_ID", typeId) });

            if (dt.Rows.Count == 0) return null;
            var rt = MapRequestType(dt.Rows[0]);
            rt.Fields = GetFormFields(typeId);
            return rt;
        }

        // ── Form Fields ───────────────────────────────────────────────────────
        public List<FormField> GetFormFields(int typeId)
        {
            var dt = OracleHelper.ExecuteQuerySql(@"
                SELECT ff.FIELD_ID, ff.TYPE_ID, ff.FIELD_LABEL, ff.FIELD_NAME, ff.FIELD_TYPE,
                       ff.IS_REQUIRED, ff.SORT_ORDER, ff.PLACEHOLDER, ff.DEFAULT_VALUE,
                       ff.OPTION_LIST_ID, ff.CONDITIONAL_PARENT_FIELD_ID,
                       ff.CONDITIONAL_SHOW_WHEN_VALUE, ff.HELP_TEXT, ff.IS_ACTIVE
                FROM   FORM_FIELDS ff
                WHERE  ff.TYPE_ID = :P_TYPE AND ff.IS_ACTIVE = 1
                ORDER BY ff.SORT_ORDER",
                new[] { OracleHelper.ParamInt("P_TYPE", typeId) });

            var fields = new List<FormField>();
            foreach (DataRow row in dt.Rows)
            {
                var field = MapFormField(row);
                if (field.OptionListId.HasValue)
                    field.Options = GetOptionListValues(field.OptionListId.Value);
                fields.Add(field);
            }
            return fields;
        }

        // ── Option Lists ──────────────────────────────────────────────────────
        public List<OptionList> GetOptionLists()
        {
            var dt = OracleHelper.ExecuteQuerySql("SELECT * FROM OPTION_LISTS ORDER BY LIST_NAME");
            var list = new List<OptionList>();
            foreach (DataRow row in dt.Rows)
                list.Add(new OptionList
                {
                    ListId      = OracleHelper.ToInt(row["LIST_ID"]),
                    ListName    = OracleHelper.ToString(row["LIST_NAME"]),
                    ListCode    = OracleHelper.ToString(row["LIST_CODE"])
                });
            return list;
        }

        public List<OptionListValue> GetOptionListValues(int listId)
        {
            var dt = OracleHelper.ExecuteQuerySql(@"
                SELECT * FROM OPTION_LIST_VALUES
                WHERE LIST_ID = :P_ID AND IS_ACTIVE = 1
                ORDER BY SORT_ORDER",
                new[] { OracleHelper.ParamInt("P_ID", listId) });

            var list = new List<OptionListValue>();
            foreach (DataRow row in dt.Rows)
                list.Add(new OptionListValue
                {
                    ValueId   = OracleHelper.ToInt(row["VALUE_ID"]),
                    ListId    = OracleHelper.ToInt(row["LIST_ID"]),
                    ValueText = OracleHelper.ToString(row["VALUE_TEXT"]),
                    ValueCode = OracleHelper.ToString(row["VALUE_CODE"]),
                    SortOrder = OracleHelper.ToInt(row["SORT_ORDER"]),
                    IsActive  = OracleHelper.ToBool(row["IS_ACTIVE"])
                });
            return list;
        }

        // ── Workflows ─────────────────────────────────────────────────────────
        public List<Workflow> GetWorkflows(bool activeOnly = true)
        {
            var dt = OracleHelper.ExecuteQuerySql(
                "SELECT * FROM WORKFLOWS" + (activeOnly ? " WHERE IS_ACTIVE=1" : "") + " ORDER BY WORKFLOW_NAME");

            var list = new List<Workflow>();
            foreach (DataRow row in dt.Rows)
                list.Add(new Workflow
                {
                    WorkflowId   = OracleHelper.ToInt(row["WORKFLOW_ID"]),
                    WorkflowName = OracleHelper.ToString(row["WORKFLOW_NAME"]),
                    Description  = OracleHelper.ToString(row["DESCRIPTION"]),
                    IsActive     = OracleHelper.ToBool(row["IS_ACTIVE"])
                });
            return list;
        }

        public Workflow GetWorkflowWithStages(int workflowId)
        {
            var dtW = OracleHelper.ExecuteQuerySql(
                "SELECT * FROM WORKFLOWS WHERE WORKFLOW_ID = :P_ID",
                new[] { OracleHelper.ParamInt("P_ID", workflowId) });

            if (dtW.Rows.Count == 0) return null;
            var wf = new Workflow
            {
                WorkflowId   = OracleHelper.ToInt(dtW.Rows[0]["WORKFLOW_ID"]),
                WorkflowName = OracleHelper.ToString(dtW.Rows[0]["WORKFLOW_NAME"]),
                IsActive     = OracleHelper.ToBool(dtW.Rows[0]["IS_ACTIVE"])
            };

            var dtS = OracleHelper.ExecuteQuerySql(@"
                SELECT ws.*, r.ROLE_CODE, r.ROLE_NAME
                FROM   WORKFLOW_STAGES ws
                JOIN   ROLES r ON r.ROLE_ID = ws.ROLE_ID
                WHERE  ws.WORKFLOW_ID = :P_WF AND ws.IS_ACTIVE = 1
                ORDER BY ws.STAGE_ORDER",
                new[] { OracleHelper.ParamInt("P_WF", workflowId) });

            foreach (DataRow row in dtS.Rows)
                wf.Stages.Add(MapWorkflowStage(row));

            return wf;
        }

        // ── Divisions & Buildings ─────────────────────────────────────────────
        public List<Division> GetDivisions(bool activeOnly = true)
        {
            var dt = OracleHelper.ExecuteQuerySql(
                "SELECT * FROM DIVISIONS" + (activeOnly ? " WHERE IS_ACTIVE=1" : "") + " ORDER BY DIVISION_NAME");

            var list = new List<Division>();
            foreach (DataRow row in dt.Rows)
                list.Add(new Division
                {
                    DivisionId   = OracleHelper.ToInt(row["DIVISION_ID"]),
                    DivisionName = OracleHelper.ToString(row["DIVISION_NAME"]),
                    DivisionCode = OracleHelper.ToString(row["DIVISION_CODE"]),
                    IsActive     = OracleHelper.ToBool(row["IS_ACTIVE"])
                });
            return list;
        }

        public List<Building> GetBuildings(bool activeOnly = true)
        {
            var dt = OracleHelper.ExecuteQuerySql(
                "SELECT * FROM BUILDINGS" + (activeOnly ? " WHERE IS_ACTIVE=1" : "") + " ORDER BY BUILDING_NAME");

            var list = new List<Building>();
            foreach (DataRow row in dt.Rows)
                list.Add(new Building
                {
                    BuildingId   = OracleHelper.ToInt(row["BUILDING_ID"]),
                    BuildingName = OracleHelper.ToString(row["BUILDING_NAME"]),
                    BuildingCode = OracleHelper.ToString(row["BUILDING_CODE"]),
                    IsActive     = OracleHelper.ToBool(row["IS_ACTIVE"])
                });
            return list;
        }

        // ── Form field admin (OIC IT) ─────────────────────────────────────────
        public void SaveFormField(FormField field)
        {
            if (field.FieldId == 0)
            {
                int newId = OracleHelper.NextVal("SEQ_FORM_FIELDS");
                OracleHelper.ExecuteNonQuerySql(@"
                    INSERT INTO FORM_FIELDS
                        (FIELD_ID, TYPE_ID, FIELD_LABEL, FIELD_NAME, FIELD_TYPE, IS_REQUIRED,
                         SORT_ORDER, PLACEHOLDER, DEFAULT_VALUE, OPTION_LIST_ID,
                         CONDITIONAL_PARENT_FIELD_ID, CONDITIONAL_SHOW_WHEN_VALUE, HELP_TEXT)
                    VALUES (:P_ID, :P_TYP, :P_LBL, :P_NM, :P_FT, :P_REQ,
                            :P_ORD, :P_PH, :P_DEF, :P_OL, :P_CPF, :P_CSV, :P_HELP)",
                    new[]
                    {
                        OracleHelper.ParamInt("P_ID",  newId),
                        OracleHelper.ParamInt("P_TYP", field.TypeId),
                        OracleHelper.ParamStr("P_LBL", field.FieldLabel, 300),
                        OracleHelper.ParamStr("P_NM",  field.FieldName, 100),
                        OracleHelper.ParamStr("P_FT",  field.FieldType, 30),
                        OracleHelper.ParamBool("P_REQ",field.IsRequired),
                        OracleHelper.ParamInt("P_ORD", field.SortOrder),
                        OracleHelper.ParamStr("P_PH",  field.Placeholder, 300),
                        OracleHelper.ParamStr("P_DEF", field.DefaultValue, 500),
                        OracleHelper.ParamInt("P_OL",  field.OptionListId),
                        OracleHelper.ParamInt("P_CPF", field.ConditionalParentFieldId),
                        OracleHelper.ParamStr("P_CSV", field.ConditionalShowWhenValue, 500),
                        OracleHelper.ParamStr("P_HELP",field.HelpText, 500)
                    });
            }
            else
            {
                OracleHelper.ExecuteNonQuerySql(@"
                    UPDATE FORM_FIELDS SET
                        FIELD_LABEL=:P_LBL, FIELD_NAME=:P_NM, FIELD_TYPE=:P_FT,
                        IS_REQUIRED=:P_REQ, SORT_ORDER=:P_ORD, PLACEHOLDER=:P_PH,
                        DEFAULT_VALUE=:P_DEF, OPTION_LIST_ID=:P_OL,
                        CONDITIONAL_PARENT_FIELD_ID=:P_CPF,
                        CONDITIONAL_SHOW_WHEN_VALUE=:P_CSV, HELP_TEXT=:P_HELP
                    WHERE FIELD_ID = :P_ID",
                    new[]
                    {
                        OracleHelper.ParamStr("P_LBL", field.FieldLabel, 300),
                        OracleHelper.ParamStr("P_NM",  field.FieldName, 100),
                        OracleHelper.ParamStr("P_FT",  field.FieldType, 30),
                        OracleHelper.ParamBool("P_REQ",field.IsRequired),
                        OracleHelper.ParamInt("P_ORD", field.SortOrder),
                        OracleHelper.ParamStr("P_PH",  field.Placeholder, 300),
                        OracleHelper.ParamStr("P_DEF", field.DefaultValue, 500),
                        OracleHelper.ParamInt("P_OL",  field.OptionListId),
                        OracleHelper.ParamInt("P_CPF", field.ConditionalParentFieldId),
                        OracleHelper.ParamStr("P_CSV", field.ConditionalShowWhenValue, 500),
                        OracleHelper.ParamStr("P_HELP",field.HelpText, 500),
                        OracleHelper.ParamInt("P_ID",  field.FieldId)
                    });
            }
        }

        public void DeleteFormField(int fieldId)
        {
            OracleHelper.ExecuteNonQuerySql(
                "UPDATE FORM_FIELDS SET IS_ACTIVE = 0 WHERE FIELD_ID = :P_ID",
                new[] { OracleHelper.ParamInt("P_ID", fieldId) });
        }

        // ── Private mappers ───────────────────────────────────────────────────
        private ServiceCategory MapCategory(DataRow row) => new ServiceCategory
        {
            CategoryId   = OracleHelper.ToInt(row["CATEGORY_ID"]),
            CategoryName = OracleHelper.ToString(row["CATEGORY_NAME"]),
            CategoryCode = OracleHelper.ToString(row["CATEGORY_CODE"]),
            Description  = OracleHelper.ToString(row["DESCRIPTION"]),
            IconClass    = OracleHelper.ToString(row["ICON_CLASS"]),
            ColorHex     = OracleHelper.ToString(row["COLOR_HEX"]),
            SortOrder    = OracleHelper.ToInt(row["SORT_ORDER"]),
            IsActive     = OracleHelper.ToBool(row["IS_ACTIVE"])
        };

        private RequestType MapRequestType(DataRow row) => new RequestType
        {
            TypeId           = OracleHelper.ToInt(row["TYPE_ID"]),
            CategoryId       = OracleHelper.ToInt(row["CATEGORY_ID"]),
            CategoryName     = OracleHelper.ToString(row["CATEGORY_NAME"]),
            TypeName         = OracleHelper.ToString(row["TYPE_NAME"]),
            TypeCode         = OracleHelper.ToString(row["TYPE_CODE"]),
            Description      = OracleHelper.ToString(row["DESCRIPTION"]),
            SlaHours         = OracleHelper.ToInt(row["SLA_HOURS"]),
            WorkflowId       = OracleHelper.ToNullableInt(row["WORKFLOW_ID"]),
            WorkflowName     = OracleHelper.ToString(row["WORKFLOW_NAME"]),
            IconClass        = OracleHelper.ToString(row["ICON_CLASS"]),
            IsActive         = OracleHelper.ToBool(row["IS_ACTIVE"]),
            IsConnectionType = OracleHelper.ToBool(row["IS_CONNECTION_TYPE"])
        };

        private FormField MapFormField(DataRow row) => new FormField
        {
            FieldId                    = OracleHelper.ToInt(row["FIELD_ID"]),
            TypeId                     = OracleHelper.ToInt(row["TYPE_ID"]),
            FieldLabel                 = OracleHelper.ToString(row["FIELD_LABEL"]),
            FieldName                  = OracleHelper.ToString(row["FIELD_NAME"]),
            FieldType                  = OracleHelper.ToString(row["FIELD_TYPE"]),
            IsRequired                 = OracleHelper.ToBool(row["IS_REQUIRED"]),
            SortOrder                  = OracleHelper.ToInt(row["SORT_ORDER"]),
            Placeholder                = OracleHelper.ToString(row["PLACEHOLDER"]),
            DefaultValue               = OracleHelper.ToString(row["DEFAULT_VALUE"]),
            OptionListId               = OracleHelper.ToNullableInt(row["OPTION_LIST_ID"]),
            ConditionalParentFieldId   = OracleHelper.ToNullableInt(row["CONDITIONAL_PARENT_FIELD_ID"]),
            ConditionalShowWhenValue   = OracleHelper.ToString(row["CONDITIONAL_SHOW_WHEN_VALUE"]),
            HelpText                   = OracleHelper.ToString(row["HELP_TEXT"]),
            IsActive                   = OracleHelper.ToBool(row["IS_ACTIVE"])
        };

        private WorkflowStage MapWorkflowStage(DataRow row) => new WorkflowStage
        {
            StageId             = OracleHelper.ToInt(row["STAGE_ID"]),
            WorkflowId          = OracleHelper.ToInt(row["WORKFLOW_ID"]),
            StageOrder          = OracleHelper.ToInt(row["STAGE_ORDER"]),
            StageName           = OracleHelper.ToString(row["STAGE_NAME"]),
            RoleId              = OracleHelper.ToInt(row["ROLE_ID"]),
            RoleCode            = OracleHelper.ToString(row["ROLE_CODE"]),
            RoleName            = OracleHelper.ToString(row["ROLE_NAME"]),
            IsConfirmationOnly  = OracleHelper.ToBool(row["IS_CONFIRMATION_ONLY"]),
            RemarksRequired     = OracleHelper.ToBool(row["REMARKS_REQUIRED"]),
            IsActive            = OracleHelper.ToBool(row["IS_ACTIVE"])
        };
    }
}
