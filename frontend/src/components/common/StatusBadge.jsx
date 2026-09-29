import React from 'react';
import { PACKAGE_STATUS_CONFIG } from '../../utils/constants';

export const StatusBadge = ({ status }) => {
  const config = PACKAGE_STATUS_CONFIG[status] || {
    label: status || 'Không xác định',
    badgeColor: 'bg-slate-100 text-slate-700 border-slate-200',
  };

  return (
    <span
      className={`inline-flex items-center px-2 py-0.5 rounded text-[11px] font-medium border ${config.badgeColor}`}
    >
      {config.label}
    </span>
  );
};

export default StatusBadge;
