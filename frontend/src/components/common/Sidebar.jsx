import React from 'react';
import { NavLink } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { ROLES } from '../../utils/constants';
import {
  LayoutDashboard,
  FileText,
  FileCheck,
  Scale,
  Users
} from 'lucide-react';

const MENU_ITEMS = [
  {
    name: 'Tổng quan',
    path: '/dashboard',
    icon: LayoutDashboard,
    roles: [ROLES.ADMIN],
  },
  {
    name: 'Gói thầu',
    path: '/packages',
    icon: FileText,
    roles: [ROLES.ADMIN, ROLES.PROCUREMENT, ROLES.EVALUATOR, ROLES.CONTRACTOR],
  },
  {
    name: 'Hợp đồng',
    path: '/contracts',
    icon: FileCheck,
    roles: [ROLES.ADMIN, ROLES.PROCUREMENT, ROLES.CONTRACTOR],
  },
  {
    name: 'Chấm điểm',
    path: '/evaluation',
    icon: Scale,
    roles: [ROLES.ADMIN, ROLES.PROCUREMENT, ROLES.EVALUATOR],
  },
  {
    name: 'Người dùng',
    path: '/users',
    icon: Users,
    roles: [ROLES.ADMIN],
  },
];

export const Sidebar = () => {
  const { hasRole } = useAuth();

  const visibleItems = MENU_ITEMS.filter((item) => hasRole(item.roles));

  return (
    <aside className="w-56 bg-white border-r border-slate-200 flex flex-col flex-shrink-0 min-h-[calc(100vh-4rem)]">
      <nav className="p-3 space-y-1">
        {visibleItems.map((item) => {
          const Icon = item.icon;
          return (
            <NavLink
              key={item.path}
              to={item.path}
              className={({ isActive }) =>
                `flex items-center space-x-3 px-3.5 py-2.5 rounded-xl text-xs font-semibold transition ${
                  isActive
                    ? 'bg-slate-900 text-white shadow-xs'
                    : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
                }`
              }
            >
              <Icon className="w-4 h-4 flex-shrink-0" />
              <span>{item.name}</span>
            </NavLink>
          );
        })}
      </nav>
    </aside>
  );
};

export default Sidebar;
