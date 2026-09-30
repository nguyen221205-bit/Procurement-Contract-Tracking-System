import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';
import { AuthProvider } from './context/AuthContext';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import DashboardPage from './pages/DashboardPage';
import PackagesPage from './pages/PackagesPage';
import PackageDetailPage from './pages/PackageDetailPage';
import AppLayout from './components/layout/AppLayout';
import ProtectedRoute from './components/common/ProtectedRoute';

// Temporary placeholder for pages to be built in subsequent steps
const PagePlaceholder = ({ title, description }) => (
  <div className="bg-white p-8 rounded-2xl shadow-xs border border-slate-200 text-center space-y-2 max-w-lg mx-auto mt-10">
    <h3 className="text-lg font-bold text-slate-800">{title}</h3>
    <p className="text-xs text-slate-500">{description}</p>
  </div>
);

function App() {
  return (
    <AuthProvider>
      <Toaster position="top-right" reverseOrder={false} />
      <BrowserRouter>
        <Routes>
          {/* Public Routes */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />

          {/* Protected App Routes */}
          <Route element={<ProtectedRoute />}>
            <Route element={<AppLayout />}>
              <Route path="/dashboard" element={<DashboardPage />} />
              <Route path="/packages" element={<PackagesPage />} />
              <Route path="/packages/:id" element={<PackageDetailPage />} />
              <Route path="/contracts" element={<PagePlaceholder title="Hợp đồng" description="Màn hình quản lý hợp đồng và mốc thanh toán." />} />
              <Route path="/evaluation" element={<PagePlaceholder title="Chấm điểm" description="Màn hình chấm điểm và xếp hạng trúng thầu." />} />
              <Route path="/users" element={<PagePlaceholder title="Người dùng" description="Màn hình quản trị người dùng hệ thống." />} />
            </Route>
          </Route>

          {/* Catch-all redirect */}
          <Route path="*" element={<Navigate to="/login" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
