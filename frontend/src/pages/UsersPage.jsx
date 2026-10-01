import React, { useState, useEffect, useCallback } from 'react';
import { userApi } from '../api/userApi';
import { useAuth } from '../context/AuthContext';
import { ROLES, ROLE_CONFIG } from '../utils/constants';
import { formatDate } from '../utils/formatters';
import StatusBadge from '../components/common/StatusBadge';
import ConfirmModal from '../components/common/ConfirmModal';
import AssignRolesModal from '../components/users/AssignRolesModal';
import EditUserModal from '../components/users/EditUserModal';
import UserDetailModal from '../components/users/UserDetailModal';
import toast from 'react-hot-toast';
import {
  Users,
  Search,
  Filter,
  Shield,
  Edit3,
  Eye,
  Lock,
  Unlock,
  CheckCircle2,
  XCircle,
  Building2,
  RotateCcw,
  ChevronLeft,
  ChevronRight,
  ShieldAlert,
  UserCheck
} from 'lucide-react';

export const UsersPage = () => {
  const { user: currentUser } = useAuth();

  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [totalCount, setTotalCount] = useState(0);
  const [pageIndex, setPageIndex] = useState(1);
  const [pageSize] = useState(10);

  // Bộ lọc
  const [search, setSearch] = useState('');
  const [selectedRole, setSelectedRole] = useState('');
  const [selectedStatus, setSelectedStatus] = useState(''); // '', 'active', 'locked'
  const [availableRoles, setAvailableRoles] = useState([]);

  // Modals state
  const [detailUser, setDetailUser] = useState(null);
  const [editUser, setEditUser] = useState(null);
  const [rolesUser, setRolesUser] = useState(null);
  const [toggleUser, setToggleUser] = useState(null);
  const [toggleLoading, setToggleLoading] = useState(false);

  // Tải danh mục vai trò
  useEffect(() => {
    const fetchRoles = async () => {
      try {
        const res = await userApi.getRoles();
        if (res && res.success && res.data) {
          setAvailableRoles(res.data);
        }
      } catch (err) {
        console.error('Lỗi tải danh mục vai trò:', err);
      }
    };
    fetchRoles();
  }, []);

  // Tải danh sách người dùng
  const fetchUsers = useCallback(async () => {
    setLoading(true);
    try {
      const params = {
        pageIndex,
        pageSize,
      };

      if (search.trim()) params.search = search.trim();
      if (selectedRole) params.role = selectedRole;
      if (selectedStatus === 'active') params.isActive = true;
      if (selectedStatus === 'locked') params.isActive = false;

      const res = await userApi.getUsers(params);
      if (res && res.success && res.data) {
        setUsers(res.data.items || []);
        setTotalCount(res.data.totalCount || 0);
      } else {
        toast.error(res?.message || 'Không thể tải danh sách người dùng');
      }
    } catch (error) {
      toast.error(error.message || 'Lỗi kết nối khi tải danh sách người dùng');
    } finally {
      setLoading(false);
    }
  }, [pageIndex, pageSize, search, selectedRole, selectedStatus]);

  useEffect(() => {
    fetchUsers();
  }, [fetchUsers]);

  // Tìm kiếm form submit
  const handleSearchSubmit = (e) => {
    e.preventDefault();
    setPageIndex(1);
    fetchUsers();
  };

  // Reset bộ lọc
  const handleResetFilters = () => {
    setSearch('');
    setSelectedRole('');
    setSelectedStatus('');
    setPageIndex(1);
  };

  // Xử lý Khóa / Mở khóa tài khoản
  const handleConfirmToggleStatus = async () => {
    if (!toggleUser) return;

    if (currentUser && toggleUser.id === currentUser.id) {
      toast.error('Quản trị viên không thể tự khóa tài khoản của chính mình.');
      setToggleUser(null);
      return;
    }

    setToggleLoading(true);
    try {
      const res = await userApi.toggleUserStatus(toggleUser.id);
      if (res && res.success) {
        toast.success(res.message || 'Cập nhật trạng thái tài khoản thành công!');
        fetchUsers();
      } else {
        toast.error(res?.message || 'Không thể thay đổi trạng thái tài khoản');
      }
    } catch (error) {
      toast.error(error.message || 'Lỗi khi thay đổi trạng thái tài khoản');
    } finally {
      setToggleLoading(false);
      setToggleUser(null);
    }
  };

  // Tính toán số trang
  const totalPages = Math.ceil(totalCount / pageSize) || 1;

  // Thống kê nhanh
  const activeCount = users.filter((u) => u.isActive).length;
  const lockedCount = users.filter((u) => !u.isActive).length;
  const contractorCount = users.filter((u) => u.contractor !== null || u.roles?.includes(ROLES.CONTRACTOR)).length;

  return (
    <div className="space-y-6">
      {/* Tiêu đề & Tổng quan */}
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold text-slate-900 tracking-tight flex items-center space-x-2.5">
            <div className="p-2 rounded-xl bg-slate-900 text-sky-400">
              <Users className="w-5 h-5" />
            </div>
            <span>Quản trị người dùng & Phân quyền</span>
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Quản lý tài khoản, điều chỉnh vai trò và kiểm soát trạng thái hoạt động trong hệ thống
          </p>
        </div>
      </div>

      {/* Thẻ thống kê nhanh */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3.5">
        <div className="bg-white p-4 rounded-xl border border-slate-200/80 shadow-2xs space-y-1">
          <span className="text-[11px] font-semibold text-slate-500 block">Tổng tài khoản</span>
          <p className="text-xl font-bold text-slate-900">{totalCount}</p>
        </div>

        <div className="bg-white p-4 rounded-xl border border-slate-200/80 shadow-2xs space-y-1">
          <span className="text-[11px] font-semibold text-emerald-600 flex items-center space-x-1">
            <CheckCircle2 className="w-3.5 h-3.5" />
            <span>Đang hoạt động</span>
          </span>
          <p className="text-xl font-bold text-emerald-700">{activeCount}</p>
        </div>

        <div className="bg-white p-4 rounded-xl border border-slate-200/80 shadow-2xs space-y-1">
          <span className="text-[11px] font-semibold text-rose-600 flex items-center space-x-1">
            <XCircle className="w-3.5 h-3.5" />
            <span>Bị khóa</span>
          </span>
          <p className="text-xl font-bold text-rose-700">{lockedCount}</p>
        </div>

        <div className="bg-white p-4 rounded-xl border border-slate-200/80 shadow-2xs space-y-1">
          <span className="text-[11px] font-semibold text-indigo-600 flex items-center space-x-1">
            <Building2 className="w-3.5 h-3.5" />
            <span>Nhà thầu</span>
          </span>
          <p className="text-xl font-bold text-indigo-700">{contractorCount}</p>
        </div>
      </div>

      {/* Thanh tìm kiếm & Bộ lọc */}
      <div className="bg-white p-4 rounded-2xl border border-slate-200/80 shadow-2xs space-y-3">
        <form onSubmit={handleSearchSubmit} className="flex flex-col md:flex-row gap-3">
          {/* Ô tìm kiếm */}
          <div className="relative flex-1">
            <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
              <Search className="w-4 h-4" />
            </div>
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Tìm kiếm theo Tên, Email hoặc Số điện thoại..."
              className="block w-full pl-9 pr-3 py-2 text-xs bg-white border border-slate-300 rounded-xl text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
            />
          </div>

          {/* Lọc vai trò */}
          <div className="w-full md:w-48">
            <select
              value={selectedRole}
              onChange={(e) => {
                setSelectedRole(e.target.value);
                setPageIndex(1);
              }}
              className="block w-full px-3 py-2 text-xs bg-white border border-slate-300 rounded-xl text-slate-800 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
            >
              <option value="">Tất cả vai trò</option>
              {availableRoles.length > 0 ? (
                availableRoles.map((r) => (
                  <option key={r.name} value={r.name}>
                    {ROLE_CONFIG[r.name]?.label || r.name}
                  </option>
                ))
              ) : (
                Object.values(ROLES).map((r) => (
                  <option key={r} value={r}>
                    {ROLE_CONFIG[r]?.label || r}
                  </option>
                ))
              )}
            </select>
          </div>

          {/* Lọc trạng thái */}
          <div className="w-full md:w-44">
            <select
              value={selectedStatus}
              onChange={(e) => {
                setSelectedStatus(e.target.value);
                setPageIndex(1);
              }}
              className="block w-full px-3 py-2 text-xs bg-white border border-slate-300 rounded-xl text-slate-800 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
            >
              <option value="">Tất cả trạng thái</option>
              <option value="active">Đang hoạt động</option>
              <option value="locked">Đã bị khóa</option>
            </select>
          </div>

          {/* Nút hành động lọc */}
          <div className="flex items-center space-x-2">
            <button
              type="submit"
              className="px-4 py-2 bg-slate-900 hover:bg-slate-800 text-white rounded-xl text-xs font-semibold shadow-xs transition flex items-center space-x-1.5"
            >
              <Search className="w-3.5 h-3.5" />
              <span>Tìm</span>
            </button>

            {(search || selectedRole || selectedStatus) && (
              <button
                type="button"
                onClick={handleResetFilters}
                className="px-3 py-2 bg-slate-100 hover:bg-slate-200 text-slate-600 rounded-xl text-xs font-semibold transition flex items-center space-x-1"
                title="Xóa bộ lọc"
              >
                <RotateCcw className="w-3.5 h-3.5" />
                <span className="hidden sm:inline">Đặt lại</span>
              </button>
            )}
          </div>
        </form>
      </div>

      {/* Bảng danh sách người dùng */}
      <div className="bg-white rounded-2xl border border-slate-200/80 shadow-2xs overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs text-slate-600">
            <thead className="bg-slate-50/80 text-slate-500 font-semibold uppercase text-[10px] tracking-wider border-b border-slate-200">
              <tr>
                <th scope="col" className="px-5 py-3.5">Người dùng</th>
                <th scope="col" className="px-4 py-3.5">Số điện thoại</th>
                <th scope="col" className="px-4 py-3.5">Vai trò</th>
                <th scope="col" className="px-4 py-3.5">Doanh nghiệp</th>
                <th scope="col" className="px-4 py-3.5">Trạng thái</th>
                <th scope="col" className="px-4 py-3.5">Ngày tham gia</th>
                <th scope="col" className="px-5 py-3.5 text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {loading ? (
                <tr>
                  <td colSpan="7" className="px-5 py-12 text-center">
                    <div className="flex flex-col items-center justify-center space-y-2">
                      <div className="w-6 h-6 border-2 border-sky-600 border-t-transparent rounded-full animate-spin" />
                      <span className="text-xs text-slate-500">Đang tải danh sách người dùng...</span>
                    </div>
                  </td>
                </tr>
              ) : users.length === 0 ? (
                <tr>
                  <td colSpan="7" className="px-5 py-12 text-center text-slate-400">
                    <Users className="w-8 h-8 mx-auto text-slate-300 mb-2 stroke-[1.5]" />
                    <p className="text-xs font-medium text-slate-500">Không tìm thấy người dùng phù hợp</p>
                    <p className="text-[11px] text-slate-400 mt-0.5">Thử điều chỉnh lại từ khóa hoặc xóa bớt bộ lọc</p>
                  </td>
                </tr>
              ) : (
                users.map((user) => {
                  const isCurrentAdmin = currentUser && user.id === currentUser.id;

                  return (
                    <tr key={user.id} className="hover:bg-slate-50/80 transition">
                      {/* Người dùng: Avatar + Tên + Email */}
                      <td className="px-5 py-3.5">
                        <div className="flex items-center space-x-3">
                          <div className="w-8 h-8 rounded-full bg-slate-900 text-sky-400 flex items-center justify-center font-bold text-xs flex-shrink-0">
                            {user.fullName ? user.fullName.charAt(0).toUpperCase() : 'U'}
                          </div>
                          <div className="min-w-0">
                            <div className="flex items-center space-x-1.5">
                              <span className="font-bold text-slate-900 truncate">
                                {user.fullName}
                              </span>
                              {isCurrentAdmin && (
                                <span className="text-[10px] font-bold px-1.5 py-0.2 rounded bg-sky-100 text-sky-800 border border-sky-200">
                                  Bạn
                                </span>
                              )}
                            </div>
                            <span className="text-[11px] text-slate-500 block truncate" title={user.email}>
                              {user.email}
                            </span>
                          </div>
                        </div>
                      </td>

                      {/* Số điện thoại */}
                      <td className="px-4 py-3.5 whitespace-nowrap text-slate-600">
                        {user.phone || <span className="text-slate-400 italic">-</span>}
                      </td>

                      {/* Vai trò */}
                      <td className="px-4 py-3.5">
                        <div className="flex flex-wrap gap-1">
                          {user.roles && user.roles.length > 0 ? (
                            user.roles.map((r) => {
                              const config = ROLE_CONFIG[r] || {
                                label: r,
                                badgeColor: 'bg-slate-100 text-slate-700 border-slate-200',
                              };
                              return (
                                <span
                                  key={r}
                                  className={`text-[10px] font-semibold px-2 py-0.5 rounded border ${config.badgeColor}`}
                                >
                                  {config.label}
                                </span>
                              );
                            })
                          ) : (
                            <span className="text-slate-400 italic text-[11px]">-</span>
                          )}
                        </div>
                      </td>

                      {/* Doanh nghiệp */}
                      <td className="px-4 py-3.5">
                        {user.contractor ? (
                          <div className="space-y-0.5 max-w-xs">
                            <span className="font-semibold text-slate-800 block truncate" title={user.contractor.companyName}>
                              {user.contractor.companyName}
                            </span>
                            {user.contractor.taxCode && (
                              <span className="text-[10px] font-mono text-slate-500 block">
                                MST: {user.contractor.taxCode}
                              </span>
                            )}
                          </div>
                        ) : (
                          <span className="text-slate-400 italic text-[11px]">-</span>
                        )}
                      </td>

                      {/* Trạng thái */}
                      <td className="px-4 py-3.5 whitespace-nowrap">
                        <StatusBadge status={user.isActive} type="user_status" />
                      </td>

                      {/* Ngày tham gia */}
                      <td className="px-4 py-3.5 whitespace-nowrap text-slate-500 text-[11px]">
                        {formatDate(user.createdAt)}
                      </td>

                      {/* Thao tác */}
                      <td className="px-5 py-3.5 text-right whitespace-nowrap">
                        <div className="flex items-center justify-end space-x-1">
                          {/* Xem chi tiết */}
                          <button
                            type="button"
                            onClick={() => setDetailUser(user)}
                            className="p-1.5 text-slate-400 hover:text-sky-600 hover:bg-sky-50 rounded-lg transition"
                            title="Xem chi tiết"
                          >
                            <Eye className="w-4 h-4" />
                          </button>

                          {/* Sửa thông tin */}
                          <button
                            type="button"
                            onClick={() => setEditUser(user)}
                            className="p-1.5 text-slate-400 hover:text-amber-600 hover:bg-amber-50 rounded-lg transition"
                            title="Sửa thông tin"
                          >
                            <Edit3 className="w-4 h-4" />
                          </button>

                          {/* Phân quyền */}
                          <button
                            type="button"
                            onClick={() => setRolesUser(user)}
                            className="p-1.5 text-slate-400 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition"
                            title="Phân quyền vai trò"
                          >
                            <Shield className="w-4 h-4" />
                          </button>

                          {/* Khóa / Mở khóa */}
                          {user.isActive ? (
                            <button
                              type="button"
                              disabled={isCurrentAdmin}
                              onClick={() => setToggleUser(user)}
                              className={`p-1.5 rounded-lg transition ${
                                isCurrentAdmin
                                  ? 'text-slate-300 cursor-not-allowed'
                                  : 'text-slate-400 hover:text-rose-600 hover:bg-rose-50'
                              }`}
                              title={
                                isCurrentAdmin
                                  ? 'Không thể tự khóa tài khoản của chính mình'
                                  : 'Khóa tài khoản này'
                              }
                            >
                              <Lock className="w-4 h-4" />
                            </button>
                          ) : (
                            <button
                              type="button"
                              onClick={() => setToggleUser(user)}
                              className="p-1.5 text-slate-400 hover:text-emerald-600 hover:bg-emerald-50 rounded-lg transition"
                              title="Kích hoạt lại tài khoản này"
                            >
                              <Unlock className="w-4 h-4" />
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>

        {/* Phân trang */}
        <div className="px-5 py-3.5 bg-slate-50/50 border-t border-slate-200 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs text-slate-500">
          <div>
            <span>
              Hiển thị <strong className="text-slate-800">{users.length > 0 ? (pageIndex - 1) * pageSize + 1 : 0}</strong> đến{' '}
              <strong className="text-slate-800">{Math.min(pageIndex * pageSize, totalCount)}</strong> trong tổng số{' '}
              <strong className="text-slate-800">{totalCount}</strong> người dùng
            </span>
          </div>

          <div className="flex items-center space-x-1.5">
            <button
              type="button"
              disabled={pageIndex <= 1 || loading}
              onClick={() => setPageIndex((prev) => Math.max(prev - 1, 1))}
              className="p-1.5 rounded-lg border border-slate-200 hover:bg-white disabled:opacity-40 disabled:cursor-not-allowed transition"
              title="Trang trước"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>

            <span className="px-3 py-1 bg-white border border-slate-200 rounded-lg font-bold text-slate-800">
              {pageIndex} / {totalPages}
            </span>

            <button
              type="button"
              disabled={pageIndex >= totalPages || loading}
              onClick={() => setPageIndex((prev) => Math.min(prev + 1, totalPages))}
              className="p-1.5 rounded-lg border border-slate-200 hover:bg-white disabled:opacity-40 disabled:cursor-not-allowed transition"
              title="Trang kế tiếp"
            >
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>

      {/* Modal Xem chi tiết */}
      <UserDetailModal
        isOpen={!!detailUser}
        onClose={() => setDetailUser(null)}
        user={detailUser}
        onOpenEdit={(u) => setEditUser(u)}
        onOpenRoles={(u) => setRolesUser(u)}
      />

      {/* Modal Chỉnh sửa thông tin */}
      <EditUserModal
        isOpen={!!editUser}
        onClose={() => setEditUser(null)}
        user={editUser}
        onSuccess={fetchUsers}
      />

      {/* Modal Phân quyền vai trò */}
      <AssignRolesModal
        isOpen={!!rolesUser}
        onClose={() => setRolesUser(null)}
        user={rolesUser}
        availableRoles={availableRoles}
        onSuccess={fetchUsers}
      />

      {/* Modal Xác nhận Khóa / Kích hoạt tài khoản */}
      <ConfirmModal
        isOpen={!!toggleUser}
        onClose={() => setToggleUser(null)}
        onConfirm={handleConfirmToggleStatus}
        isLoading={toggleLoading}
        title={toggleUser?.isActive ? 'Khóa tài khoản người dùng' : 'Mở khóa tài khoản người dùng'}
        message={
          toggleUser?.isActive
            ? `Bạn có chắc chắn muốn khóa tài khoản "${toggleUser?.fullName}" (${toggleUser?.email})? Người dùng sẽ bị thu hồi phiên đăng nhập ngay lập tức.`
            : `Bạn có chắc chắn muốn kích hoạt lại tài khoản "${toggleUser?.fullName}" (${toggleUser?.email})? Người dùng có thể đăng nhập bình thường vào hệ thống.`
        }
        confirmText={toggleUser?.isActive ? 'Khóa tài khoản' : 'Mở khóa tài khoản'}
        cancelText="Hủy bỏ"
        confirmVariant={toggleUser?.isActive ? 'danger' : 'primary'}
      />
    </div>
  );
};

export default UsersPage;
