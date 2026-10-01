import React from 'react';
import { formatDate } from '../../utils/formatters';
import { ROLE_CONFIG } from '../../utils/constants';
import StatusBadge from '../common/StatusBadge';
import {
  User,
  X,
  Mail,
  Phone,
  Calendar,
  Shield,
  Building2,
  FileSpreadsheet,
  Star,
  CheckCircle2,
  AlertOctagon,
  Clock
} from 'lucide-react';

export const UserDetailModal = ({
  isOpen,
  onClose,
  user,
  onOpenEdit,
  onOpenRoles,
}) => {
  if (!isOpen || !user) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/50 backdrop-blur-xs flex items-center justify-center p-4 animate-in fade-in duration-150">
      <div
        className="bg-white rounded-2xl max-w-lg w-full p-6 shadow-xl border border-slate-100 relative space-y-6"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Nút đóng */}
        <button
          onClick={onClose}
          className="absolute top-4 right-4 text-slate-400 hover:text-slate-600 p-1 rounded-lg hover:bg-slate-100 transition"
        >
          <X className="w-5 h-5" />
        </button>

        {/* Header thông tin chính */}
        <div className="flex items-start space-x-4">
          <div className="w-14 h-14 rounded-2xl bg-gradient-to-tr from-sky-600 to-indigo-600 text-white flex items-center justify-center font-bold text-xl shadow-sm flex-shrink-0">
            {user.fullName ? user.fullName.charAt(0).toUpperCase() : 'U'}
          </div>
          <div className="space-y-1.5 flex-1 pr-6">
            <div className="flex items-center space-x-2">
              <h3 className="text-lg font-bold text-slate-900">
                {user.fullName}
              </h3>
              <StatusBadge status={user.isActive} type="user_status" />
            </div>
            <p className="text-xs text-slate-500 font-medium">
              ID tài khoản: <span className="font-mono text-slate-700">#{user.id}</span>
            </p>
          </div>
        </div>

        {/* Danh sách vai trò */}
        <div className="space-y-2">
          <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider block">
            Vai trò được cấp
          </span>
          <div className="flex flex-wrap gap-1.5">
            {user.roles && user.roles.length > 0 ? (
              user.roles.map((roleName) => {
                const config = ROLE_CONFIG[roleName] || {
                  label: roleName,
                  badgeColor: 'bg-slate-100 text-slate-700 border-slate-200',
                };
                return (
                  <span
                    key={roleName}
                    className={`inline-flex items-center space-x-1 px-2.5 py-1 rounded-lg text-xs font-semibold border ${config.badgeColor}`}
                  >
                    <Shield className="w-3 h-3" />
                    <span>{config.label}</span>
                  </span>
                );
              })
            ) : (
              <span className="text-xs text-slate-400 italic">Chưa có vai trò</span>
            )}
          </div>
        </div>

        {/* Bảng chi tiết thông tin cá nhân */}
        <div className="bg-slate-50 rounded-xl p-4 border border-slate-200/80 space-y-3">
          <span className="text-xs font-semibold text-slate-600 block pb-1 border-b border-slate-200">
            Thông tin định danh & liên hệ
          </span>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
            <div className="flex items-center space-x-2 text-slate-600">
              <Mail className="w-4 h-4 text-slate-400 flex-shrink-0" />
              <span className="truncate" title={user.email}>{user.email}</span>
            </div>

            <div className="flex items-center space-x-2 text-slate-600">
              <Phone className="w-4 h-4 text-slate-400 flex-shrink-0" />
              <span>{user.phone || 'Chưa cập nhật SĐT'}</span>
            </div>

            <div className="flex items-center space-x-2 text-slate-600">
              <Calendar className="w-4 h-4 text-slate-400 flex-shrink-0" />
              <span>Tạo ngày: {formatDate(user.createdAt)}</span>
            </div>

            <div className="flex items-center space-x-2 text-slate-600">
              <Clock className="w-4 h-4 text-slate-400 flex-shrink-0" />
              <span>Cập nhật: {user.updatedAt ? formatDate(user.updatedAt) : 'Chưa cập nhật'}</span>
            </div>
          </div>
        </div>

        {/* Khối thông tin Doanh nghiệp Nhà thầu (nếu có) */}
        {user.contractor && (
          <div className="bg-emerald-50/60 rounded-xl p-4 border border-emerald-200/80 space-y-2.5">
            <div className="flex items-center justify-between pb-1.5 border-b border-emerald-200/60">
              <div className="flex items-center space-x-1.5 text-xs font-bold text-emerald-800">
                <Building2 className="w-4 h-4 text-emerald-600" />
                <span>Hồ sơ năng lực Doanh nghiệp</span>
              </div>
              <div className="flex items-center space-x-1 bg-white px-2 py-0.5 rounded border border-emerald-200 text-xs font-bold text-amber-600">
                <Star className="w-3.5 h-3.5 fill-amber-400 text-amber-400" />
                <span>{user.contractor.rating?.toFixed(1) || '5.0'}</span>
              </div>
            </div>

            <div className="space-y-1.5 text-xs">
              <p className="font-bold text-slate-900">
                {user.contractor.companyName}
              </p>
              <div className="flex items-center space-x-4 text-slate-600 text-[11px]">
                <span>Mã số thuế: <strong className="font-mono text-slate-800">{user.contractor.taxCode || 'N/A'}</strong></span>
                <span>Mã nhà thầu: <strong className="font-mono text-slate-800">#{user.contractor.contractorId}</strong></span>
              </div>
            </div>
          </div>
        )}

        {/* Nút hành động */}
        <div className="pt-2 flex items-center justify-between border-t border-slate-100">
          <div className="flex items-center space-x-2">
            {onOpenRoles && (
              <button
                type="button"
                onClick={() => {
                  onClose();
                  onOpenRoles(user);
                }}
                className="px-3 py-1.5 text-xs font-semibold text-indigo-700 bg-indigo-50 hover:bg-indigo-100 rounded-lg transition border border-indigo-100 flex items-center space-x-1.5"
              >
                <Shield className="w-3.5 h-3.5" />
                <span>Phân quyền</span>
              </button>
            )}

            {onOpenEdit && (
              <button
                type="button"
                onClick={() => {
                  onClose();
                  onOpenEdit(user);
                }}
                className="px-3 py-1.5 text-xs font-semibold text-sky-700 bg-sky-50 hover:bg-sky-100 rounded-lg transition border border-sky-100 flex items-center space-x-1.5"
              >
                <User className="w-3.5 h-3.5" />
                <span>Sửa thông tin</span>
              </button>
            )}
          </div>

          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 text-xs font-semibold text-slate-600 hover:bg-slate-100 rounded-xl transition border border-slate-200"
          >
            Đóng
          </button>
        </div>
      </div>
    </div>
  );
};

export default UserDetailModal;
