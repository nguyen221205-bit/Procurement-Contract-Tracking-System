import React from 'react';
import { useAuth } from '../../context/AuthContext';
import { LogOut, ShieldCheck, User } from 'lucide-react';
import { ROLES } from '../../utils/constants';

const roleBadgeStyles = {
  [ROLES.ADMIN]: 'bg-rose-50 text-rose-700 border-rose-200',
  [ROLES.PROCUREMENT]: 'bg-indigo-50 text-indigo-700 border-indigo-200',
  [ROLES.EVALUATOR]: 'bg-amber-50 text-amber-700 border-amber-200',
  [ROLES.CONTRACTOR]: 'bg-emerald-50 text-emerald-700 border-emerald-200',
};

const roleNames = {
  [ROLES.ADMIN]: 'Quản trị viên',
  [ROLES.PROCUREMENT]: 'Bên mời thầu',
  [ROLES.EVALUATOR]: 'Giám khảo',
  [ROLES.CONTRACTOR]: 'Nhà thầu',
};

export const Navbar = () => {
  const { user, role, logout } = useAuth();

  const badgeStyle = roleBadgeStyles[role] || 'bg-slate-100 text-slate-700 border-slate-200';
  const roleDisplayName = roleNames[role] || role || 'Người dùng';

  return (
    <header className="h-16 bg-white border-b border-slate-200 px-6 flex items-center justify-between sticky top-0 z-30">
      {/* Brand logo / title */}
      <div className="flex items-center space-x-3">
        <div className="w-9 h-9 rounded-lg bg-slate-900 text-sky-400 flex items-center justify-center shadow-xs">
          <ShieldCheck className="w-5 h-5" />
        </div>
        <span className="font-bold text-sm text-slate-800 hidden sm:inline tracking-tight">
          Procurement & Contract Tracking System
        </span>
      </div>

      {/* User profile & actions */}
      <div className="flex items-center space-x-4">
        <div className="flex items-center space-x-2.5">
          <div className="w-8 h-8 rounded-full bg-slate-100 border border-slate-200 flex items-center justify-center text-slate-600 font-semibold text-xs">
            {user?.fullName ? user.fullName.charAt(0).toUpperCase() : <User className="w-4 h-4" />}
          </div>
          <div className="hidden md:block text-left">
            <p className="text-xs font-semibold text-slate-800 leading-tight">
              {user?.fullName || 'Người dùng'}
            </p>
            <span className={`inline-block text-[10px] font-medium px-1.5 py-0.2 rounded border ${badgeStyle} mt-0.5`}>
              {roleDisplayName}
            </span>
          </div>
        </div>

        {/* Logout button */}
        <button
          onClick={logout}
          className="flex items-center space-x-1.5 px-3 py-1.5 rounded-lg text-slate-500 hover:text-rose-600 hover:bg-rose-50 text-xs font-semibold transition border border-transparent hover:border-rose-100"
          title="Đăng xuất"
        >
          <LogOut className="w-4 h-4" />
          <span className="hidden sm:inline">Đăng xuất</span>
        </button>
      </div>
    </header>
  );
};

export default Navbar;
