using System;
using System.Collections.Generic;
using System.Linq;
using CRMP.DAL;
using CRMP.Models;

namespace CRMP.BLL
{
    /// <summary>
    /// Heart of the CRMP — drives every request through its approval workflow.
    /// Handles: initial routing, auto-skip (no assignee / self-approval),
    /// delegation resolution, stage advancement, and rejection.
    /// </summary>
    public static class WorkflowEngine
    {
        private static readonly RequestRepository  _reqRepo  = new RequestRepository();
        private static readonly ApprovalRepository _aprRepo  = new ApprovalRepository();
        private static readonly CatalogRepository  _catRepo  = new CatalogRepository();

        // ── Called when a new request is submitted ────────────────────────────
        public static void InitiateWorkflow(int requestId)
        {
            var request = _reqRepo.GetById(requestId);
            if (request == null) return;

            var requestType = _catRepo.GetRequestTypeById(request.TypeId);
            if (requestType == null || !requestType.WorkflowId.HasValue)
            {
                // No workflow — mark directly approved and ready for tech pool
                _reqRepo.UpdateStatus(requestId, "APPROVED");
                _reqRepo.UpdateCurrentStage(requestId, null);
                _reqRepo.AddTimeline(requestId, "APPROVED_STAGE",
                    "Request auto-approved (no workflow configured) — placed in tech expert pool.", null);
                return;
            }

            var workflow = _catRepo.GetWorkflowWithStages(requestType.WorkflowId.Value);
            if (workflow == null || workflow.Stages.Count == 0)
            {
                _reqRepo.UpdateStatus(requestId, "APPROVED");
                _reqRepo.AddTimeline(requestId, "APPROVED_STAGE",
                    "No workflow stages defined — request auto-approved.", null);
                return;
            }

            // Route to the first stage
            AdvanceToNextStage(request, workflow, 0);
        }

        // ── Approver acts on a pending approval ───────────────────────────────
        public static void ProcessApproval(int requestId, int approverUserId,
                                           string action, string remarks, bool isConfirmed = false)
        {
            var pending = _aprRepo.GetPendingForApprover(requestId, approverUserId);
            if (pending == null) return;

            _aprRepo.ActionApproval(pending.ApprovalId, action, remarks, isConfirmed);

            var request = _reqRepo.GetById(requestId);
            var requestType = _catRepo.GetRequestTypeById(request.TypeId);
            var workflow = _catRepo.GetWorkflowWithStages(requestType.WorkflowId.Value);

            if (action == "APPROVED" || action == "CONFIRMED")
            {
                string actorNote = $"Approved by {pending.ApproverName}" +
                    (pending.DelegatedByUserId.HasValue ? $" (acting for {pending.DelegatedByName})" : "");
                if (!string.IsNullOrEmpty(remarks)) actorNote += $" — \"{remarks}\"";

                _reqRepo.AddTimeline(requestId, "APPROVED_STAGE",
                    $"Stage '{pending.StageName}' approved. {actorNote}", approverUserId);

                // Advance to next stage
                AdvanceToNextStage(request, workflow, pending.StageOrder);
            }
            else if (action == "REJECTED")
            {
                _reqRepo.UpdateStatus(requestId, "REJECTED");
                _reqRepo.UpdateCurrentStage(requestId, null);
                _reqRepo.AddTimeline(requestId, "REJECTED",
                    $"Rejected at stage '{pending.StageName}' by {pending.ApproverName}. Reason: {remarks}",
                    approverUserId);

                // Notify submitter
                NotificationService.Notify(request.SubmitterUserId,
                    "Request Rejected",
                    $"Your request {request.RequestNumber} was rejected at stage '{pending.StageName}'. Reason: {remarks}",
                    $"~/Pages/Employee/RequestDetail.aspx?id={requestId}",
                    "REQUEST_REJECTED");
            }
        }

        // ── Bulk approve multiple requests ────────────────────────────────────
        public static void ProcessBulkApproval(IEnumerable<int> requestIds, int approverUserId,
                                                string remarks, bool isConfirmed = false)
        {
            foreach (var rid in requestIds)
                ProcessApproval(rid, approverUserId, "APPROVED", remarks, isConfirmed);
        }

        // ── Private: move to the next stage in the workflow ───────────────────
        private static void AdvanceToNextStage(Request request, Workflow workflow, int currentStageOrder)
        {
            // Find next active stages beyond current
            var nextStages = workflow.Stages
                .Where(s => s.StageOrder > currentStageOrder && s.IsActive)
                .OrderBy(s => s.StageOrder)
                .ToList();

            if (nextStages.Count == 0)
            {
                // All stages done — fully approved
                _reqRepo.UpdateStatus(request.RequestId, "APPROVED");
                _reqRepo.UpdateCurrentStage(request.RequestId, null);
                _reqRepo.AddTimeline(request.RequestId, "APPROVED_STAGE",
                    "All approval stages completed — request approved and placed in tech expert pool.", null);

                NotificationService.Notify(request.SubmitterUserId,
                    "Request Fully Approved",
                    $"Your request {request.RequestNumber} has been fully approved and is now with the technical team.",
                    $"~/Pages/Employee/RequestDetail.aspx?id={request.RequestId}",
                    "REQUEST_APPROVED");
                return;
            }

            // Try to route to the next stage
            foreach (var stage in nextStages)
            {
                int? approverUserId = _aprRepo.ResolveApprover(stage.RoleId, request.DivisionId,
                                                                request.SubmitterUserId);

                if (approverUserId == null)
                {
                    // No one assigned to this stage — auto-skip
                    int skipId = _aprRepo.CreatePending(request.RequestId, stage.StageId, 0 /* system */, stage.StageOrder);
                    _aprRepo.ActionApproval(skipId, "SKIPPED_AUTO",
                        $"Auto-skipped: no user assigned to role '{stage.RoleName}' in division '{request.DivisionName}'.");

                    _reqRepo.AddTimeline(request.RequestId, "SKIPPED_STAGE",
                        $"Stage '{stage.StageName}' auto-skipped — no approver assigned for role '{stage.RoleName}' in this division.",
                        null);
                    continue; // Try next stage
                }

                // Route to this approver
                _aprRepo.CreatePending(request.RequestId, stage.StageId, approverUserId.Value, stage.StageOrder);
                _reqRepo.UpdateCurrentStage(request.RequestId, stage.StageId);
                _reqRepo.UpdateStatus(request.RequestId, "PENDING_APPROVAL");

                _reqRepo.AddTimeline(request.RequestId, "NOTE",
                    $"Request routed to '{stage.StageName}' for action.", null);

                // Notify the approver
                NotificationService.Notify(approverUserId.Value,
                    "Approval Required",
                    $"Request {request.RequestNumber} ({request.TypeName}) requires your approval at stage '{stage.StageName}'.",
                    $"~/Pages/Approver/ApprovalDetail.aspx?id={request.RequestId}",
                    "APPROVAL_REQUIRED");
                return;
            }

            // All remaining stages were skipped — fully approved
            _reqRepo.UpdateStatus(request.RequestId, "APPROVED");
            _reqRepo.UpdateCurrentStage(request.RequestId, null);
            _reqRepo.AddTimeline(request.RequestId, "APPROVED_STAGE",
                "All remaining stages were auto-skipped (no approvers assigned) — request approved.", null);
        }
    }
}
