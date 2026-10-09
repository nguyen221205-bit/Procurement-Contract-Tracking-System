import React from 'react';
import {
  PACKAGE_STATUS_CONFIG,
  CONTRACT_STATUS_CONFIG,
  MILESTONE_STATUS_CONFIG,
  ROLE_CONFIG,
  CONTRACTOR_VERIFICATION_CONFIG,
  EVALUATOR_PROPOSAL_CONFIG,
} from '../../utils/constants';

export const StatusBadge = ({ status, type = 'auto' }) => {
  let config = null;

  if (type === 'contract') {
    config = CONTRACT_STATUS_CONFIG[status];
  } else if (type === 'milestone') {
    config = MILESTONE_STATUS_CONFIG[status];
  } else if (type === 'package') {
    config = PACKAGE_STATUS_CONFIG[status];
  } else if (type === 'role') {
    config = ROLE_CONFIG[status];
  } else if (type === 'contractor_verification') {
    config = CONTRACTOR_VERIFICATION_CONFIG[status];
  } else if (type === 'evaluator_proposal') {
    config = EVALUATOR_PROPOSAL_CONFIG[status];
  } else if (type === 'user_status') {
    const isActive = status === true || status === 'true' || status === 'Active';
    config = isActive
      ? { label: 'Đang hoạt động', badgeColor: 'bg-emerald-50 text-emerald-700 border-emerald-200' }
      : { label: 'Đã bị khóa', badgeColor: 'bg-rose-50 text-rose-700 border-rose-200' };
  } else {
    // Auto lookup order: Contract -> Package -> Milestone -> Role -> Contractor -> EvaluatorProposal
    config =
      CONTRACT_STATUS_CONFIG[status] ||
      PACKAGE_STATUS_CONFIG[status] ||
      MILESTONE_STATUS_CONFIG[status] ||
      ROLE_CONFIG[status] ||
      CONTRACTOR_VERIFICATION_CONFIG[status] ||
      EVALUATOR_PROPOSAL_CONFIG[status];
  }

  const finalConfig = config || {
    label: status || 'Không xác định',
    badgeColor: 'bg-slate-100 text-slate-700 border-slate-200',
  };

  return (
    <span
      className={`inline-flex items-center px-2 py-0.5 rounded text-[11px] font-medium border ${finalConfig.badgeColor}`}
    >
      {finalConfig.label}
    </span>
  );
};

export default StatusBadge;

