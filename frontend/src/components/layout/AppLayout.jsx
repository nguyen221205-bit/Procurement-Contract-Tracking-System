import React from 'react';
import { Outlet } from 'react-router-dom';
import Navbar from '../common/Navbar';
import Sidebar from '../common/Sidebar';
import ForceChangePasswordModal from '../common/ForceChangePasswordModal';
import { useAuth } from '../../context/AuthContext';

export const AppLayout = () => {
  const { user } = useAuth();

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col">
      <Navbar />
      <div className="flex flex-1">
        <Sidebar />
        <main className="flex-1 p-6 lg:p-8 max-w-7xl mx-auto w-full">
          <Outlet />
        </main>
      </div>

      <ForceChangePasswordModal
        isOpen={!!user?.mustChangePassword}
        onClose={() => {}}
        onPasswordChanged={() => {}}
      />
    </div>
  );
};

export default AppLayout;
