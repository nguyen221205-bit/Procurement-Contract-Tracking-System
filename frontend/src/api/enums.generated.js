// Generated from ProcurementSystem.Core/Enums. Run npm run generate:enums.
export const ENUMS = Object.freeze({
  "AcceptanceStatus": {
    "Pending": 0,
    "Approved": 1,
    "Rejected": 2,
    "TechnicalApproved": 3,
    "ReviewInvalidated": 4
  },
  "AddendumStatus": {
    "Draft": 0,
    "Approved": 1,
    "Rejected": 2,
    "Pending": 3
  },
  "AddendumType": {
    "TimelineExtension": 0,
    "ValueAdjustment": 1,
    "ScopeChange": 2,
    "GeneralTerms": 3
  },
  "ApprovalStatus": {
    "Pending": 0,
    "Approved": 1,
    "Rejected": 2
  },
  "BidPackageStatus": {
    "Open": 0,
    "Closed": 1,
    "Evaluating": 2,
    "Awarded": 3,
    "Contracted": 4,
    "Cancelled": 5
  },
  "BidPackageType": {
    "Goods": 0,
    "Construction": 1,
    "Service": 2
  },
  "ClarificationStatus": {
    "Pending": 0,
    "Answered": 1
  },
  "ClarificationType": {
    "PackageClarification": 0,
    "SubmissionClarification": 1
  },
  "ContractStatus": {
    "Draft": 0,
    "Active": 1,
    "Completed": 2,
    "Terminated": 3
  },
  "GuaranteeStatus": {
    "PendingApproval": 0,
    "Valid": 1,
    "Released": 2,
    "Claimed": 3,
    "Expired": 4
  },
  "InvitationStatus": {
    "Pending": 0,
    "Accepted": 1,
    "Revoked": 2,
    "Expired": 3
  },
  "MemberStatus": {
    "Active": 0,
    "Suspended": 1,
    "Revoked": 2
  },
  "MilestoneStatus": {
    "Pending": 0,
    "InProgress": 1,
    "Completed": 2,
    "Overdue": 3
  },
  "NotificationType": {
    "Info": 0,
    "Warning": 1,
    "ContractExpiring": 2,
    "DeadlineApproaching": 3,
    "ProgressOverdue": 4,
    "Success": 5
  },
  "ProcuringEntityMemberRole": {
    "Manager": 0,
    "Officer": 1,
    "Finance": 2,
    "Signer": 3
  },
  "SigningAuthorityStatus": {
    "PendingVerification": 0,
    "Active": 1,
    "Rejected": 2,
    "Revoked": 3,
    "Expired": 4
  },
  "SigningAuthorityType": {
    "DisbursementApproval": 0,
    "ContractSigning": 1,
    "FullLegalRepresentative": 2
  },
  "SubmissionFileType": {
    "Quotation": 0,
    "Capability": 1,
    "Schedule": 2,
    "Other": 3
  }
});
