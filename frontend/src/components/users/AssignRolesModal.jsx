import React, { useState, useEffect } from 'react';
import { userApi } from '../../api/userApi';
import { ROLES, ROLE_CONFIG } from '../../utils/constants';
import toast from 'react-hot-toast';
import { Shield, X, AlertTriangle, Check, Info } from 'lucide-react';

export const AssignRolesModal = ({
  isOpen,
  onClose,
  user,
  onSuccess,
  availableRoles = [],
}) => {
  const [selectedRoles, setSelectedRoles] = useState([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (user && user.roles) {
      setSelectedRoles([...user.roles]);
    } else {
      setSelectedRoles([]);
    }
  }, [user]);

  if (!isOpen || !user) return null;

  const handleToggleRole = (roleName) => {
    if (selectedRoles.includes(roleName)) {
      if (selectedRoles.length === 1) {
        toast.error('Người dùng phải có ít nhất một vai trò.');
        return;
      }
      setSelectedRoles(selectedRoles.filter((r) => r !== roleName));
    } else {
      setSelectedRoles([...selectedRoles, roleName]);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (selectedRoles.length === 0) {
      toast.error('Vui lòng chọn ít nhất một vai trò.');
      return;
    }

    setLoading(true);
    try {
      const res = await userApi.assignRoles(user.id, selectedRoles);
      if (res && res.success) {
        toast.success(res.message || 'Cập nhật vai trò thành công!');
        if (onSuccess) onSuccess();
        onClose();
      } else {
        toast.error(res?.message || 'Không thể cập nhật vai trò');
      }
    } catch (error) {
      toast.error(error.message || 'Lỗi khi cập nhật vai trò');
    } finally {
      setLoading(false);
    }
  };

  // Fallback danh sách vai trò nếu availableRoles chưa load
  const roleList = availableRoles.length > 0
    ? availableRoles
    : Object.values(ROLES).map((name) => ({ name }));

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/50 backdrop-blur-xs flex items-center justify-center p-4 animate-in fade-in duration-150">
      <div
        className="bg-white rounded-2xl max-w-lg w-full p-6 shadow-xl border border-slate-100 relative space-y-5"
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

        {/* Tiêu đề & Thông tin tài khoản */}
        <div className="flex items-start space-x-3.5">
          <div className="p-2.5 rounded-xl border flex-shrink-0 bg-indigo-50 text-indigo-600 border-indigo-100">
            <Shield className="w-5 h-5" />
          </div>
          <div className="space-y-1 pr-6">
            <h3 className="text-base font-bold text-slate-900">
              Phân quyền tài khoản
            </h3>
            <p className="text-xs text-slate-500">
              Cấp hoặc điều chỉnh vai trò hệ thống cho <span className="font-semibold text-slate-800">{user.fullName}</span> ({user.email})
            </p>
          </div>
        </div>

        {/* Danh sách vai trò để chọn */}
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2.5 max-h-80 overflow-y-auto pr-1">
            {roleList.map((role) => {
              const roleName = role.name;
              const isSelected = selectedRoles.includes(roleName);
              const config = ROLE_CONFIG[roleName] || {
                label: roleName,
                badgeColor: 'bg-slate-100 text-slate-700 border-slate-200',
                description: role.description || '',
              };

              return (
                <div
                  key={roleName}
                  onClick={() => handleToggleRole(roleName)}
                  className={`p-3.5 rounded-xl border cursor-pointer transition flex items-start space-x-3 ${
                    isSelected
                      ? 'border-sky-500 bg-sky-50/40 shadow-xs'
                      : 'border-slate-200 hover:border-slate-300 hover:bg-slate-50/50'
                  }`}
                >
                  <div
                    className={`w-5 h-5 mt-0.5 rounded-md flex items-center justify-center border transition ${
                      isSelected
                        ? 'bg-sky-600 border-sky-600 text-white'
                        : 'border-slate-300 bg-white'
                    }`}
                  >
                    {isSelected && <Check className="w-3.5 h-3.5 stroke-[3]" />}
                  </div>

                  <div className="flex-1 space-y-1">
                    <div className="flex items-center space-x-2">
                      <span className="text-xs font-bold text-slate-900">
                        {config.label}
                      </span>
                      <span className={`text-[10px] font-medium px-1.5 py-0.2 rounded border ${config.badgeColor}`}>
                        {roleName}
                      </span>
                    </div>
                    <p className="text-[11px] text-slate-500 leading-relaxed">
                      {config.description || role.description || 'Không có mô tả chi tiết'}
                    </p>
                  </div>
                </div>
              );
            })}
          </div>

          {/* Ghi chú an toàn */}
          <div className="p-3 bg-amber-50 rounded-xl border border-amber-200/60 flex items-start space-x-2.5">
            <Info className="w-4 h-4 text-amber-600 flex-shrink-0 mt-0.5" />
            <p className="text-[11px] text-amber-800 leading-relaxed">
              Người dùng có thể sở hữu đồng thời nhiều vai trò. Người dùng cần đăng nhập lại hoặc làm mới trang để áp dụng ngay quyền hạn mới.
            </p>
          </div>

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
              className="px-4 py-2 text-xs font-semibold rounded-xl transition shadow-xs bg-slate-900 hover:bg-slate-800 text-white focus:ring-2 focus:ring-offset-1 focus:ring-slate-900 disabled:opacity-50 flex items-center space-x-1.5"
            >
              {loading && (
                <div className="w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full animate-spin" />
              )}
              <span>Lưu thay đổi</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default AssignRolesModal;
