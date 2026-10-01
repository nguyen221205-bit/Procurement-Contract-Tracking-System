import React, { useState, useEffect } from 'react';
import { userApi } from '../../api/userApi';
import toast from 'react-hot-toast';
import { Edit3, X, User, Phone, Mail, Building2 } from 'lucide-react';

export const EditUserModal = ({
  isOpen,
  onClose,
  user,
  onSuccess,
}) => {
  const [fullName, setFullName] = useState('');
  const [phone, setPhone] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (user) {
      setFullName(user.fullName || '');
      setPhone(user.phone || '');
    }
  }, [user]);

  if (!isOpen || !user) return null;

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!fullName.trim()) {
      toast.error('Họ và tên không được để trống.');
      return;
    }

    setLoading(true);
    try {
      const res = await userApi.updateUser(user.id, {
        fullName: fullName.trim(),
        phone: phone.trim() || null,
      });

      if (res && res.success) {
        toast.success(res.message || 'Cập nhật thông tin người dùng thành công!');
        if (onSuccess) onSuccess();
        onClose();
      } else {
        toast.error(res?.message || 'Không thể cập nhật thông tin');
      }
    } catch (error) {
      toast.error(error.message || 'Lỗi khi cập nhật thông tin người dùng');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/50 backdrop-blur-xs flex items-center justify-center p-4 animate-in fade-in duration-150">
      <div
        className="bg-white rounded-2xl max-w-md w-full p-6 shadow-xl border border-slate-100 relative space-y-5"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Nút đóng */}
        <button
          onClick={onClose}
          disabled={loading}
          className="absolute top-4 right-4 text-slate-400 hover:text-slate-600 p-1 rounded-lg hover:bg-slate-100 transition"
        >
          <X className="w-5 h-5" />
        </button>

        {/* Tiêu đề */}
        <div className="flex items-start space-x-3.5">
          <div className="p-2.5 rounded-xl border flex-shrink-0 bg-sky-50 text-sky-600 border-sky-100">
            <Edit3 className="w-5 h-5" />
          </div>
          <div className="space-y-1 pr-6">
            <h3 className="text-base font-bold text-slate-900">
              Chỉnh sửa thông tin
            </h3>
            <p className="text-xs text-slate-500">
              Cập nhật thông tin liên hệ cơ bản của người dùng
            </p>
          </div>
        </div>

        {/* Form nhập liệu */}
        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Email (Read-only) */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">
              Email đăng nhập (Không thể thay đổi)
            </label>
            <div className="relative rounded-lg">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                <Mail className="w-4 h-4" />
              </div>
              <input
                type="email"
                disabled
                value={user.email || ''}
                className="block w-full pl-9 pr-3 py-2 text-xs bg-slate-100 border border-slate-200 rounded-lg text-slate-500 cursor-not-allowed"
              />
            </div>
          </div>

          {/* Họ và tên */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">
              Họ và tên <span className="text-rose-500">*</span>
            </label>
            <div className="relative rounded-lg">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                <User className="w-4 h-4" />
              </div>
              <input
                type="text"
                required
                maxLength={100}
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                placeholder="Nhập họ và tên đầy đủ"
                className="block w-full pl-9 pr-3 py-2 text-xs bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
              />
            </div>
          </div>

          {/* Số điện thoại */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">
              Số điện thoại
            </label>
            <div className="relative rounded-lg">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                <Phone className="w-4 h-4" />
              </div>
              <input
                type="text"
                maxLength={20}
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                placeholder="Ví dụ: 0912345678"
                className="block w-full pl-9 pr-3 py-2 text-xs bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
              />
            </div>
          </div>

          {/* Doanh nghiệp liên kết nếu có */}
          {user.contractor && (
            <div className="p-3 bg-slate-50 rounded-xl border border-slate-200 space-y-1">
              <div className="flex items-center space-x-1.5 text-xs font-bold text-slate-700">
                <Building2 className="w-3.5 h-3.5 text-slate-500" />
                <span>Doanh nghiệp liên kết:</span>
              </div>
              <p className="text-xs text-slate-800 font-medium pl-5">
                {user.contractor.companyName}
              </p>
              {user.contractor.taxCode && (
                <p className="text-[11px] text-slate-500 pl-5">
                  Mã số thuế: {user.contractor.taxCode}
                </p>
              )}
            </div>
          )}

          {/* Hành động */}
          <div className="pt-2 flex items-center justify-end space-x-2.5">
            <button
              type="button"
              onClick={onClose}
              disabled={loading}
              className="px-4 py-2 text-xs font-semibold text-slate-600 hover:bg-slate-100 rounded-xl transition border border-slate-200 disabled:opacity-50"
            >
              Hủy bỏ
            </button>
            <button
              type="submit"
              disabled={loading}
              className="px-4 py-2 text-xs font-semibold rounded-xl transition shadow-xs bg-sky-600 hover:bg-sky-700 text-white focus:ring-2 focus:ring-offset-1 focus:ring-sky-600 disabled:opacity-50 flex items-center space-x-1.5"
            >
              {loading && (
                <div className="w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full animate-spin" />
              )}
              <span>Lưu thông tin</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default EditUserModal;
