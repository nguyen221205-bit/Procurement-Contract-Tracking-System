import React, { useState } from 'react';
import { ShieldAlert, Lock, Eye, EyeOff, LogOut, CheckCircle2, ArrowRight, X } from 'lucide-react';
import { authApi } from '../../api/authApi';
import { useAuth } from '../../context/AuthContext';
import toast from 'react-hot-toast';

export const ForceChangePasswordModal = ({ isOpen, onClose, onPasswordChanged }) => {
  const { logout, user } = useAuth();
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showCurrent, setShowCurrent] = useState(false);
  const [showNew, setShowNew] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [loading, setLoading] = useState(false);

  if (!isOpen) return null;

  const handleSkipOrClose = () => {
    // Cho phép người dùng bỏ qua việc đổi mật khẩu và tiếp tục làm việc với mật khẩu được cấp
    if (onPasswordChanged) {
      onPasswordChanged();
    } else if (onClose) {
      onClose();
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!currentPassword || !newPassword || !confirmPassword) {
      toast.error('Vui lòng điền đầy đủ các trường thông tin');
      return;
    }

    if (newPassword.length < 8) {
      toast.error('Mật khẩu mới phải có ít nhất 8 ký tự');
      return;
    }

    if (newPassword !== confirmPassword) {
      toast.error('Mật khẩu xác nhận không khớp');
      return;
    }

    if (currentPassword === newPassword) {
      toast.error('Mật khẩu mới không được trùng với mật khẩu hiện tại');
      return;
    }

    setLoading(true);
    try {
      const response = await authApi.changePassword({
        currentPassword,
        newPassword,
        confirmNewPassword: confirmPassword,
      });

      if (response && response.success) {
        toast.success('Đổi mật khẩu thành công! Hệ thống đã ghi nhận.');

        // Cập nhật lại thông tin user trong sessionStorage
        if (user) {
          const updatedUser = { ...user, mustChangePassword: false };
          sessionStorage.setItem('user', JSON.stringify(updatedUser));
        }

        // Cập nhật token mới nếu backend trả về token mới
        if (response.data && response.data.token) {
          sessionStorage.setItem('token', response.data.token);
          if (response.data.refreshToken) {
            sessionStorage.setItem('refreshToken', response.data.refreshToken);
          }
        }

        if (onPasswordChanged) {
          onPasswordChanged();
        } else if (onClose) {
          onClose();
        }
      } else {
        toast.error(response?.message || 'Không thể đổi mật khẩu. Vui lòng kiểm tra lại.');
      }
    } catch (error) {
      toast.error(error.message || 'Lỗi khi kết nối tới máy chủ');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/70 backdrop-blur-sm animate-in fade-in duration-150">
      <div className="bg-white w-full max-w-md rounded-2xl border border-slate-200 shadow-2xl overflow-hidden animate-in fade-in zoom-in duration-200">
        {/* Header */}
        <div className="bg-amber-500/10 border-b border-amber-500/20 px-6 py-4.5 flex items-start justify-between">
          <div className="flex items-start space-x-3.5">
            <div className="p-2.5 bg-amber-500 text-white rounded-xl shadow-xs flex-shrink-0">
              <ShieldAlert className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">
                Đổi mật khẩu đăng nhập
              </h3>
              <p className="text-xs text-slate-600 mt-0.5 leading-relaxed">
                Khuyến nghị đổi sang mật khẩu cá nhân để tăng cường bảo mật. Bạn cũng có thể chọn <strong>Để sau</strong> để sử dụng mật khẩu được cấp.
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={handleSkipOrClose}
            className="text-slate-400 hover:text-slate-600 p-1 rounded-lg hover:bg-slate-200/60 transition -mr-1"
            title="Đóng / Để sau"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Form Body */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">
              Mật khẩu hiện tại / được cấp <span className="text-rose-500">*</span>
            </label>
            <div className="relative rounded-lg">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                <Lock className="w-4 h-4" />
              </div>
              <input
                type={showCurrent ? 'text' : 'password'}
                required
                value={currentPassword}
                onChange={(e) => setCurrentPassword(e.target.value)}
                placeholder="Nhập mật khẩu hiện tại"
                className="block w-full pl-9 pr-10 py-2.5 text-xs bg-white border border-slate-300 rounded-xl text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
              />
              <button
                type="button"
                onClick={() => setShowCurrent(!showCurrent)}
                className="absolute inset-y-0 right-0 pr-3 flex items-center text-slate-400 hover:text-slate-600 focus:outline-none"
              >
                {showCurrent ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
              </button>
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">
              Mật khẩu mới (Tối thiểu 8 ký tự) <span className="text-rose-500">*</span>
            </label>
            <div className="relative rounded-lg">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                <Lock className="w-4 h-4" />
              </div>
              <input
                type={showNew ? 'text' : 'password'}
                required
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                placeholder="Nhập mật khẩu mới (tối thiểu 8 ký tự)"
                className="block w-full pl-9 pr-10 py-2.5 text-xs bg-white border border-slate-300 rounded-xl text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
              />
              <button
                type="button"
                onClick={() => setShowNew(!showNew)}
                className="absolute inset-y-0 right-0 pr-3 flex items-center text-slate-400 hover:text-slate-600 focus:outline-none"
              >
                {showNew ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
              </button>
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">
              Xác nhận mật khẩu mới <span className="text-rose-500">*</span>
            </label>
            <div className="relative rounded-lg">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                <Lock className="w-4 h-4" />
              </div>
              <input
                type={showConfirm ? 'text' : 'password'}
                required
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                placeholder="Nhập lại mật khẩu mới"
                className="block w-full pl-9 pr-10 py-2.5 text-xs bg-white border border-slate-300 rounded-xl text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
              />
              <button
                type="button"
                onClick={() => setShowConfirm(!showConfirm)}
                className="absolute inset-y-0 right-0 pr-3 flex items-center text-slate-400 hover:text-slate-600 focus:outline-none"
              >
                {showConfirm ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
              </button>
            </div>
          </div>

          {/* Action buttons */}
          <div className="pt-2 flex flex-col sm:flex-row items-center gap-2">
            <div className="flex items-center space-x-2 w-full sm:w-auto">
              <button
                type="button"
                onClick={logout}
                className="py-2.5 px-3 bg-slate-100 hover:bg-slate-200 text-slate-600 font-semibold text-xs rounded-xl transition flex items-center justify-center space-x-1.5"
                title="Đăng xuất khỏi tài khoản"
              >
                <LogOut className="w-3.5 h-3.5" />
                <span>Đăng xuất</span>
              </button>

              <button
                type="button"
                onClick={handleSkipOrClose}
                className="flex-1 sm:flex-none py-2.5 px-3.5 bg-slate-100 hover:bg-sky-50 hover:text-sky-700 text-slate-700 font-semibold text-xs rounded-xl border border-slate-200 transition flex items-center justify-center space-x-1"
                title="Sử dụng mật khẩu hiện tại và vào hệ thống ngay"
              >
                <span>Để sau / Vào hệ thống</span>
                <ArrowRight className="w-3.5 h-3.5" />
              </button>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full sm:flex-1 py-2.5 px-4 bg-sky-600 hover:bg-sky-500 text-white font-bold text-xs rounded-xl shadow-xs transition flex items-center justify-center space-x-1.5 disabled:opacity-50"
            >
              {loading ? (
                <>
                  <div className="w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full animate-spin" />
                  <span>Đang lưu...</span>
                </>
              ) : (
                <>
                  <CheckCircle2 className="w-3.5 h-3.5" />
                  <span>Cập nhật mật khẩu</span>
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ForceChangePasswordModal;
