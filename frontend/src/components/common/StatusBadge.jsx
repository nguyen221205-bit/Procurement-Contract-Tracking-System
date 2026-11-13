import React from 'react';
import {
  PACKAGE_STATUS_CONFIG,
  CONTRACT_STATUS_CONFIG,
  MILESTONE_STATUS_CONFIG,
  ROLE_CONFIG,
  CONTRACTOR_VERIFICATION_CONFIG,
  EVALUATOR_PROPOSAL_CONFIG,
  SUBMISSION_STATUS_CONFIG,
} from '../../utils/constants';

const CONFIG_BY_TYPE = {
  contract: CONTRACT_STATUS_CONFIG,
  milestone: MILESTONE_STATUS_CONFIG,
  package: PACKAGE_STATUS_CONFIG,
  role: ROLE_CONFIG,
  contractor_verification: CONTRACTOR_VERIFICATION_CONFIG,
  evaluator_proposal: EVALUATOR_PROPOSAL_CONFIG,
  submission: SUBMISSION_STATUS_CONFIG,
};

const AUTO_LOOKUP_CONFIGS = [
  SUBMISSION_STATUS_CONFIG,
  CONTRACT_STATUS_CONFIG,
  PACKAGE_STATUS_CONFIG,
  MILESTONE_STATUS_CONFIG,
  ROLE_CONFIG,
  CONTRACTOR_VERIFICATION_CONFIG,
  EVALUATOR_PROPOSAL_CONFIG,
];

const resolveUserStatusConfig = (status) => {
  const isActive = status === true || status === 'true' || status === 'Active';
  return isActive
    ? { label: 'Đang hoạt động', badgeColor: 'bg-emerald-50 text-emerald-700 border-emerald-200' }
    : { label: 'Đã bị khóa', badgeColor: 'bg-rose-50 text-rose-700 border-rose-200' };
};

const resolveBadgeConfig = (status, type) => {
  if (type === 'user_status') {
    return resolveUserStatusConfig(status);
  }

  const typedDictionary = CONFIG_BY_TYPE[type];
  if (typedDictionary) {
    return typedDictionary[status];
  }

  for (const configMap of AUTO_LOOKUP_CONFIGS) {
    if (configMap[status]) {
      return configMap[status];
    }
  }

  return null;
};

export const StatusBadge = ({ status, type = 'auto' }) => {
  const finalConfig = resolveBadgeConfig(status, type) || {
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
