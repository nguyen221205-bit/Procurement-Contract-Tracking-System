import React from 'react';
import {
  PACKAGE_STATUS_CONFIG,
  CONTRACT_STATUS_CONFIG,
  MILESTONE_STATUS_CONFIG,
} from '../../utils/constants';

export const StatusBadge = ({ status, type = 'auto' }) => {
  let config = null;

  if (type === 'contract') {
    config = CONTRACT_STATUS_CONFIG[status];
  } else if (type === 'milestone') {
    config = MILESTONE_STATUS_CONFIG[status];
  } else if (type === 'package') {
    config = PACKAGE_STATUS_CONFIG[status];
  } else {
    // Auto lookup order: Contract -> Package -> Milestone
    config =
      CONTRACT_STATUS_CONFIG[status] ||
      PACKAGE_STATUS_CONFIG[status] ||
      MILESTONE_STATUS_CONFIG[status];
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

