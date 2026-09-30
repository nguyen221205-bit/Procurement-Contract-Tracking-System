import React from 'react';
import { Link } from 'react-router-dom';
import StatusBadge from '../common/StatusBadge';
import { PACKAGE_TYPES } from '../../utils/constants';
import { formatVND, formatDate } from '../../utils/formatters';
import { Clock, FileText, User, ArrowRight } from 'lucide-react';

export const PackageCard = ({ pkg }) => {
  const typeConfig = PACKAGE_TYPES[pkg.type] || {
    label: pkg.type || 'Hàng hóa',
    color: 'bg-slate-50 text-slate-700 border-slate-200',
  };

  const displayStatus =
    pkg.status === 'Contracted' || pkg.status === 3
      ? 'Contracted'
      : pkg.isAwarded || pkg.status === 'Awarded'
      ? 'Awarded'
      : pkg.status;

  // Tính toán thời hạn còn lại
  const getDeadlineInfo = (deadlineStr) => {
    if (!deadlineStr) return { text: '-', isExpired: true };
    const deadline = new Date(deadlineStr);
    const now = new Date();
    const diffTime = deadline.getTime() - now.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

    if (diffDays < 0) {
      return { text: 'Đã hết hạn', isExpired: true };
    }
    if (diffDays === 0) {
      return { text: 'Hết hạn hôm nay', isExpired: false };
    }
    return { text: `Còn ${diffDays} ngày`, isExpired: false };
  };

  const deadlineInfo = getDeadlineInfo(pkg.deadline);

  return (
    <div className="bg-white rounded-2xl border border-slate-200 shadow-xs hover:border-sky-300 hover:shadow-sm transition p-5 flex flex-col justify-between space-y-4">
      {/* Header: Mã gói thầu, Loại & Trạng thái */}
      <div>
        <div className="flex items-center justify-between gap-2">
          <span className="font-mono text-xs font-bold text-slate-600 tracking-tight">
            {pkg.code}
          </span>
          <div className="flex items-center space-x-1.5 flex-shrink-0">
            <span className={`text-[10px] px-1.5 py-0.5 rounded font-medium border ${typeConfig.color}`}>
              {typeConfig.label}
            </span>
            <StatusBadge status={displayStatus} />
          </div>
        </div>

        {/* Tên gói thầu */}
        <h3
          className="font-bold text-slate-900 text-sm leading-snug line-clamp-2 mt-2.5"
          title={pkg.name}
        >
          {pkg.name}
        </h3>
      </div>

      {/* Ngân sách dự toán */}
      <div className="py-2.5 px-3 bg-slate-50/80 rounded-xl border border-slate-100 flex items-center justify-between">
        <span className="text-xs text-slate-500 font-medium">Ngân sách dự toán:</span>
        <span className="text-sm font-extrabold text-slate-900 font-mono">
          {formatVND(pkg.budget)}
        </span>
      </div>

      {/* Thông tin phụ: Hạn nộp, số hồ sơ, người tạo */}
      <div className="space-y-1.5 text-xs text-slate-500">
        <div className="flex items-center justify-between">
          <span className="flex items-center space-x-1.5 text-slate-500">
            <Clock className="w-3.5 h-3.5 text-slate-400" />
            <span>Hạn nộp: {formatDate(pkg.deadline)}</span>
          </span>
          <span
            className={`text-[11px] font-medium ${
              deadlineInfo.isExpired ? 'text-slate-400' : 'text-emerald-600'
            }`}
          >
            {deadlineInfo.text}
          </span>
        </div>

        <div className="flex items-center justify-between pt-0.5 border-t border-slate-100 text-[11px]">
          <span className="flex items-center space-x-1 text-slate-500">
            <FileText className="w-3.5 h-3.5 text-slate-400" />
            <span>{pkg.submissionsCount || 0} hồ sơ dự thầu</span>
          </span>
          <span className="flex items-center space-x-1 text-slate-400 truncate max-w-[120px]" title={pkg.createdByName}>
            <User className="w-3 h-3 text-slate-400" />
            <span className="truncate">{pkg.createdByName || 'Hệ thống'}</span>
          </span>
        </div>
      </div>

      {/* Nút Xem chi tiết */}
      <div className="pt-2 border-t border-slate-100">
        <Link
          to={`/packages/${pkg.id}`}
          className="w-full py-2 px-3 bg-slate-50 hover:bg-slate-900 text-slate-700 hover:text-white rounded-lg text-xs font-semibold transition flex items-center justify-center space-x-1.5 group"
        >
          <span>Xem chi tiết</span>
          <ArrowRight className="w-3.5 h-3.5 group-hover:translate-x-0.5 transition-transform" />
        </Link>
      </div>
    </div>
  );
};

export default PackageCard;
