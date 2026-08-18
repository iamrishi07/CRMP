using System;
using System.Collections.Generic;

namespace CRMP.Models
{
    // ────────────────────────────────────────────────────────────
    // USER & IDENTITY
    // ────────────────────────────────────────────────────────────
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int DivisionId { get; set; }
        public string DivisionName { get; set; }
        public string Designation { get; set; }
        public int? BuildingId { get; set; }
        public string BuildingName { get; set; }
        public string FloorNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<UserRole> Roles { get; set; } = new List<UserRole>();
    }

    public class Role
    {
        public int RoleId { get; set; }
        public string RoleCode { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
        public int SortOrder { get; set; }
    }

    public class UserRole
    {
        public int UserRoleId { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public string RoleCode { get; set; }
        public string RoleName { get; set; }
        public int? DivisionId { get; set; }
        public string DivisionName { get; set; }
        public bool IsActive { get; set; }
    }

    public class RoleDelegation
    {
        public int DelegationId { get; set; }
        public int DelegatorUserId { get; set; }
        public string DelegatorName { get; set; }
        public int DelegateeUserId { get; set; }
        public string DelegateeName { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public int? DivisionId { get; set; }
        public string DivisionName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsCurrentlyActive =>
            IsActive && DateTime.Today >= StartDate && DateTime.Today <= EndDate;
    }

    // ────────────────────────────────────────────────────────────
    // REFERENCE DATA
    // ────────────────────────────────────────────────────────────
    public class Division
    {
        public int DivisionId { get; set; }
        public string DivisionName { get; set; }
        public string DivisionCode { get; set; }
        public int? ParentDivisionId { get; set; }
        public bool IsActive { get; set; }
    }

    public class Building
    {
        public int BuildingId { get; set; }
        public string BuildingName { get; set; }
        public string BuildingCode { get; set; }
        public bool IsActive { get; set; }
    }

    public class ServiceCategory
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string CategoryCode { get; set; }
        public string Description { get; set; }
        public string IconClass { get; set; }
        public string ColorHex { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public List<RequestType> RequestTypes { get; set; } = new List<RequestType>();
    }

    // ────────────────────────────────────────────────────────────
    // WORKFLOW
    // ────────────────────────────────────────────────────────────
    public class Workflow
    {
        public int WorkflowId { get; set; }
        public string WorkflowName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public List<WorkflowStage> Stages { get; set; } = new List<WorkflowStage>();
    }

    public class WorkflowStage
    {
        public int StageId { get; set; }
        public int WorkflowId { get; set; }
        public int StageOrder { get; set; }
        public string StageName { get; set; }
        public int RoleId { get; set; }
        public string RoleCode { get; set; }
        public string RoleName { get; set; }
        public bool IsConfirmationOnly { get; set; }
        public bool RemarksRequired { get; set; }
        public bool IsActive { get; set; }
    }

    // ────────────────────────────────────────────────────────────
    // DYNAMIC FORM
    // ────────────────────────────────────────────────────────────
    public class OptionList
    {
        public int ListId { get; set; }
        public string ListName { get; set; }
        public string ListCode { get; set; }
        public List<OptionListValue> Values { get; set; } = new List<OptionListValue>();
    }

    public class OptionListValue
    {
        public int ValueId { get; set; }
        public int ListId { get; set; }
        public string ValueText { get; set; }
        public string ValueCode { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class RequestType
    {
        public int TypeId { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string TypeName { get; set; }
        public string TypeCode { get; set; }
        public string Description { get; set; }
        public int SlaHours { get; set; }
        public int? WorkflowId { get; set; }
        public string WorkflowName { get; set; }
        public string IconClass { get; set; }
        public bool IsActive { get; set; }
        public bool IsConnectionType { get; set; }
        public List<FormField> Fields { get; set; } = new List<FormField>();
    }

    public class FormField
    {
        public int FieldId { get; set; }
        public int TypeId { get; set; }
        public string FieldLabel { get; set; }
        public string FieldName { get; set; }
        public string FieldType { get; set; }
        public bool IsRequired { get; set; }
        public int SortOrder { get; set; }
        public string Placeholder { get; set; }
        public string DefaultValue { get; set; }
        public int? OptionListId { get; set; }
        public List<OptionListValue> Options { get; set; } = new List<OptionListValue>();
        public int? ConditionalParentFieldId { get; set; }
        public string ConditionalShowWhenValue { get; set; }
        public string HelpText { get; set; }
        public bool IsActive { get; set; }
        public bool IsConditional => ConditionalParentFieldId.HasValue;
    }

    // ────────────────────────────────────────────────────────────
    // REQUESTS
    // ────────────────────────────────────────────────────────────
    public class Request
    {
        public int RequestId { get; set; }
        public string RequestNumber { get; set; }
        public int TypeId { get; set; }
        public string TypeName { get; set; }
        public string TypeCode { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string CategoryIconClass { get; set; }
        public string CategoryColorHex { get; set; }
        public int SubmitterUserId { get; set; }
        public string SubmitterName { get; set; }
        public int? OnBehalfOfUserId { get; set; }
        public string OnBehalfOfName { get; set; }
        public int DivisionId { get; set; }
        public string DivisionName { get; set; }
        public string Status { get; set; }
        public int? CurrentStageId { get; set; }
        public string CurrentStageName { get; set; }
        public int? TechExpertId { get; set; }
        public string TechExpertName { get; set; }
        public string Summary { get; set; }
        public string Priority { get; set; }
        public DateTime? SlaDeadline { get; set; }
        public bool IsSlaBreached { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string ResolutionNotes { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<RequestFieldValue> FieldValues { get; set; } = new List<RequestFieldValue>();
        public List<RequestAttachment> Attachments { get; set; } = new List<RequestAttachment>();
        public List<RequestApproval> Approvals { get; set; } = new List<RequestApproval>();
        public List<TimelineEvent> Timeline { get; set; } = new List<TimelineEvent>();

        // Computed helpers
        public double SlaHoursRemaining =>
            SlaDeadline.HasValue ? (SlaDeadline.Value - DateTime.Now).TotalHours : 0;

        public int SlaPercentConsumed
        {
            get
            {
                if (!SlaDeadline.HasValue) return 0;
                var totalMs = (SlaDeadline.Value - SubmittedAt).TotalMilliseconds;
                var elapsed = (DateTime.Now - SubmittedAt).TotalMilliseconds;
                if (totalMs <= 0) return 100;
                return (int)Math.Min(100, Math.Round(elapsed / totalMs * 100));
            }
        }

        public string SlaStatusClass
        {
            get
            {
                if (IsSlaBreached || SlaPercentConsumed >= 100) return "sla-breached";
                if (SlaPercentConsumed >= 75) return "sla-warning";
                return "sla-ok";
            }
        }

        public string StatusBadgeClass
        {
            get
            {
                switch (Status)
                {
                    case "PENDING_APPROVAL": return "badge-amber";
                    case "APPROVED":         return "badge-green";
                    case "REJECTED":         return "badge-red";
                    case "IN_PROGRESS":      return "badge-blue";
                    case "RESOLVED":         return "badge-teal";
                    case "CLOSED":           return "badge-gray";
                    case "CANCELLED":        return "badge-gray";
                    default:                 return "badge-gray";
                }
            }
        }

        public string StatusDisplayName
        {
            get
            {
                switch (Status)
                {
                    case "PENDING_APPROVAL": return "Pending Approval";
                    case "IN_PROGRESS":      return "In Progress";
                    default:
                        return System.Globalization.CultureInfo.CurrentCulture
                                     .TextInfo.ToTitleCase(Status.ToLower().Replace("_", " "));
                }
            }
        }
    }

    public class RequestFieldValue
    {
        public int ValueId { get; set; }
        public int RequestId { get; set; }
        public int FieldId { get; set; }
        public string FieldLabel { get; set; }
        public string FieldType { get; set; }
        public string FieldValue { get; set; }
        public string FieldValueClob { get; set; }
        public string DisplayValue => string.IsNullOrEmpty(FieldValueClob) ? FieldValue : FieldValueClob;
    }

    public class RequestAttachment
    {
        public int AttachmentId { get; set; }
        public int RequestId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long? FileSize { get; set; }
        public string MimeType { get; set; }
        public int UploadedBy { get; set; }
        public string UploaderName { get; set; }
        public DateTime UploadedAt { get; set; }

        public string FileSizeDisplay
        {
            get
            {
                if (!FileSize.HasValue) return "—";
                double kb = FileSize.Value / 1024.0;
                if (kb < 1024) return $"{kb:F1} KB";
                return $"{kb / 1024:F1} MB";
            }
        }
    }

    // ────────────────────────────────────────────────────────────
    // APPROVALS & TIMELINE
    // ────────────────────────────────────────────────────────────
    public class RequestApproval
    {
        public int ApprovalId { get; set; }
        public int RequestId { get; set; }
        public int StageId { get; set; }
        public string StageName { get; set; }
        public int StageOrder { get; set; }
        public bool IsConfirmationOnly { get; set; }
        public int ApproverUserId { get; set; }
        public string ApproverName { get; set; }
        public int? DelegatedByUserId { get; set; }
        public string DelegatedByName { get; set; }
        public string Action { get; set; }
        public string Remarks { get; set; }
        public bool IsConfirmed { get; set; }
        public DateTime? ActionedAt { get; set; }
        public int SequenceNumber { get; set; }

        public bool IsPending => Action == "PENDING";
    }

    public class TimelineEvent
    {
        public int TimelineId { get; set; }
        public int RequestId { get; set; }
        public string EventType { get; set; }
        public string EventDesc { get; set; }
        public int? PerformedBy { get; set; }
        public string PerformedByName { get; set; }
        public DateTime PerformedAt { get; set; }
        public string MetadataJson { get; set; }

        public string EventIconClass
        {
            get
            {
                switch (EventType)
                {
                    case "SUBMITTED":       return "icon-send";
                    case "APPROVED_STAGE":  return "icon-check-circle";
                    case "REJECTED":        return "icon-x-circle";
                    case "SKIPPED_STAGE":   return "icon-skip";
                    case "DELEGATED":       return "icon-users";
                    case "ASSIGNED":        return "icon-user-check";
                    case "IN_PROGRESS":     return "icon-play";
                    case "RESOLVED":        return "icon-check-square";
                    case "CLOSED":          return "icon-archive";
                    case "RATED":           return "icon-star";
                    case "SLA_BREACH":      return "icon-alert-tri";
                    case "NOTE":            return "icon-message";
                    default:               return "icon-clock";
                }
            }
        }

        public string EventColorClass
        {
            get
            {
                switch (EventType)
                {
                    case "SUBMITTED":       return "timeline-blue";
                    case "APPROVED_STAGE":  return "timeline-green";
                    case "REJECTED":        return "timeline-red";
                    case "SKIPPED_STAGE":   return "timeline-gray";
                    case "RESOLVED":        return "timeline-teal";
                    case "SLA_BREACH":      return "timeline-red";
                    default:               return "timeline-gray";
                }
            }
        }
    }

    // ────────────────────────────────────────────────────────────
    // NOTIFICATIONS
    // ────────────────────────────────────────────────────────────
    public class Notification
    {
        public int NotifId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Link { get; set; }
        public bool IsRead { get; set; }
        public string NotifType { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo
        {
            get
            {
                var ts = DateTime.Now - CreatedAt;
                if (ts.TotalMinutes < 2)  return "just now";
                if (ts.TotalMinutes < 60) return $"{(int)ts.TotalMinutes}m ago";
                if (ts.TotalHours < 24)   return $"{(int)ts.TotalHours}h ago";
                return $"{(int)ts.TotalDays}d ago";
            }
        }
    }

    public class NotificationPref
    {
        public int PrefId { get; set; }
        public int UserId { get; set; }
        public string NotifType { get; set; }
        public bool EmailEnabled { get; set; }
        public bool InAppEnabled { get; set; }
    }

    // ────────────────────────────────────────────────────────────
    // CONNECTION DIRECTORY
    // ────────────────────────────────────────────────────────────
    public class Connection
    {
        public int ConnId { get; set; }
        public int? RequestId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int DivisionId { get; set; }
        public string DivisionName { get; set; }
        public string ConnType { get; set; }
        public string ConnIdentifier { get; set; }
        public int? LocationBuildingId { get; set; }
        public string LocationBuildingName { get; set; }
        public string LocationFloor { get; set; }
        public string LocationRoom { get; set; }
        public string Notes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ────────────────────────────────────────────────────────────
    // KNOWLEDGE BASE
    // ────────────────────────────────────────────────────────────
    public class KbArticle
    {
        public int ArticleId { get; set; }
        public string Title { get; set; }
        public string ContentHtml { get; set; }
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int? TypeId { get; set; }
        public string TypeName { get; set; }
        public int AuthorId { get; set; }
        public string AuthorName { get; set; }
        public DateTime? PublishedAt { get; set; }
        public int ViewCount { get; set; }
        public bool IsPublished { get; set; }
        public string Tags { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<string> TagList => string.IsNullOrEmpty(Tags)
            ? new List<string>()
            : new List<string>(Tags.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
    }

    // ────────────────────────────────────────────────────────────
    // ANNOUNCEMENTS
    // ────────────────────────────────────────────────────────────
    public class Announcement
    {
        public int AnnId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Severity { get; set; }
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int CreatedBy { get; set; }
        public string CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; }

        public string SeverityBadgeClass
        {
            get
            {
                switch (Severity)
                {
                    case "CRITICAL": return "badge-red";
                    case "WARNING":  return "badge-amber";
                    default:         return "badge-blue";
                }
            }
        }
    }

    // ────────────────────────────────────────────────────────────
    // RATINGS & TEMPLATES
    // ────────────────────────────────────────────────────────────
    public class RequestRating
    {
        public int RatingId { get; set; }
        public int RequestId { get; set; }
        public int RatedByUserId { get; set; }
        public int Score { get; set; }
        public string Comment { get; set; }
        public DateTime RatedAt { get; set; }
    }

    public class RequestTemplate
    {
        public int TemplateId { get; set; }
        public int UserId { get; set; }
        public int TypeId { get; set; }
        public string TypeName { get; set; }
        public string CategoryName { get; set; }
        public string TemplateName { get; set; }
        public string FieldValuesJson { get; set; }
        public bool IsShared { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ────────────────────────────────────────────────────────────
    // DASHBOARD / REPORTING VIEW MODELS
    // ────────────────────────────────────────────────────────────
    public class DashboardStats
    {
        public int TotalRequests { get; set; }
        public int PendingApproval { get; set; }
        public int InProgress { get; set; }
        public int Resolved { get; set; }
        public int SlaBreached { get; set; }
        public int PendingWithMe { get; set; }
        public double AvgResolutionHours { get; set; }
        public List<ChartDataPoint> RequestsByStatus { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> RequestsByCategory { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> RequestsByDay { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> SlaComplianceByCategory { get; set; } = new List<ChartDataPoint>();
    }

    public class ChartDataPoint
    {
        public string Label { get; set; }
        public decimal Value { get; set; }
        public string Color { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    public class RequestFilter
    {
        public string Status { get; set; }
        public int? CategoryId { get; set; }
        public int? TypeId { get; set; }
        public int? DivisionId { get; set; }
        public int? SubmitterUserId { get; set; }
        public int? TechExpertId { get; set; }
        public string Priority { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public bool? SlaBreached { get; set; }
        public string SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "SUBMITTED_AT";
        public string SortDir { get; set; } = "DESC";
    }
}
