import React, { useState, useEffect, useCallback } from 'react';
import { procuringEntityApi } from '../api/procuringEntityApi';
import { useAuth } from '../context/AuthContext';
import { formatDate } from '../utils/formatters';
import StatusBadge from '../components/common/StatusBadge';
import LoadingSpinner from '../components/common/LoadingSpinner';
import VerifyProcuringEntityModal from '../components/procuringEntities/VerifyProcuringEntityModal';
import toast from 'react-hot-toast';
import {
  Briefcase,
  Search,
  Filter,
  CheckCircle2,
  Clock,
  XCircle,
  ChevronLeft,
  ChevronRight,
  ShieldCheck,
  MapPin,
  Mail,
  Phone,
  RotateCcw,
  ExternalLink,
  FileText,
  UserCheck
} from 'lucide-react';

export const ProcuringEntitiesPage = () => {
  const { user: currentUser } = useAuth();

  const [entities, setEntities] = useState([]);
  const [loading, setLoading] = useState(true);
  const [totalCount, setTotalCount] = useState(0);
  const [pageIndex, setPageIndex] = useState(1);
  const [pageSize] = useState(10);

  // Bộ lọc
  const [keyword, setKeyword] = useState('');
  const [statusFilter, setStatusFilter] = useState(''); // '', 'Pending', 'Approved', 'Rejected'
  const [typeFilter, setTypeFilter] = useState('');
  const [pendingCount, setPendingCount] = useState(0);

  // Modal thẩm định
  const [selectedEntity, setSelectedEntity] = useState(null);

  // Tải danh sách Bên mời thầu
  const fetchEntities = useCallback(async () => {
    setLoading(true);
    try {
      const params = {
        pageNumber: pageIndex,
        pageSize,
        keyword: keyword.trim() || undefined,
        verificationStatus: statusFilter || undefined,
        organizationType: typeFilter || undefined,
      };

      const res = await procuringEntityApi.getProcuringEntities(params);
      if (res && res.success && res.data) {
        setEntities(res.data.items || []);
        setTotalCount(res.data.totalCount || 0);
      }
    } catch (err) {
      console.error('Lỗi tải danh sách Bên mời thầu:', err);
      toast.error('Không thể tải danh sách: ' + (err.message || 'Lỗi mạng'));
    } finally {
      setLoading(false);
    }
  }, [pageIndex, pageSize, keyword, statusFilter, typeFilter]);

  // Đếm số lượng hồ sơ chờ duyệt (Pending)
  const fetchPendingCount = useCallback(async () => {
    try {
      const res = await procuringEntityApi.getProcuringEntities({
        pageNumber: 1,
        pageSize: 100,
        verificationStatus: 'Pending',
      });
      if (res && res.success && res.data) {
        setPendingCount(res.data.totalCount || 0);
      }
    } catch (err) {
      console.warn('Không thể lấy số lượng Pending:', err);
    }
  }, []);

  useEffect(() => {
    fetchEntities();
  }, [fetchEntities]);

  useEffect(() => {
    fetchPendingCount();
  }, [fetchPendingCount]);

  // Xem nhanh tệp Quyết định thành lập
  const handleViewEstablishmentFile = async (entityId) => {
    try {
      toast.loading('Đang mở tệp Quyết định thành lập...', { id: 'view-est-tbl' });
      const response = await procuringEntityApi.downloadEstablishmentFile(entityId);
      const fileBlob = new Blob([response], { type: 'application/pdf' });
      const fileUrl = window.URL.createObjectURL(fileBlob);
      window.open(fileUrl, '_blank');
      toast.success('Đã mở tệp tài liệu!', { id: 'view-est-tbl' });
    } catch (err) {
      toast.error('Không thể mở tệp: ' + (err.message || 'Lỗi mạng'), { id: 'view-est-tbl' });
    }
  };

  const totalPages = Math.ceil(totalCount / pageSize) || 1;

  return (
    <div className="space-y-6">
      {/* HEADER & TIÊU ĐỀ */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center space-x-2 text-sky-700 font-semibold text-xs mb-1">
            <Briefcase className="w-4 h-4" />
            <span>Phân hệ Quản trị Hệ thống (Admin Only)</span>
          </div>
          <h1 className="text-2xl font-bold text-slate-900 tracking-tight">
            Quản lý Bên mời thầu & Chủ đầu tư
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Thẩm định hồ sơ năng lực pháp nhân theo Khoản 3 Điều 4 Luật Đấu thầu số 22/2023/QH15 & Nghị định 24/2024/NĐ-CP
          </p>
        </div>

        {/* Thông tin thống kê nhanh */}
        <div className="flex items-center space-x-3">
          <div className="bg-white border border-slate-200/80 px-3.5 py-2 rounded-xl shadow-xs flex items-center space-x-3">
            <div className="p-2 bg-amber-50 text-amber-600 rounded-lg">
              <Clock className="w-4 h-4" />
            </div>
            <div>
              <div className="text-[11px] font-medium text-slate-500">Chờ thẩm định</div>
              <div className="text-base font-bold text-slate-900">{pendingCount}</div>
            </div>
          </div>
          <div className="bg-white border border-slate-200/80 px-3.5 py-2 rounded-xl shadow-xs flex items-center space-x-3">
            <div className="p-2 bg-sky-50 text-sky-600 rounded-lg">
              <ShieldCheck className="w-4 h-4" />
            </div>
            <div>
              <div className="text-[11px] font-medium text-slate-500">Tổng đơn vị</div>
              <div className="text-base font-bold text-slate-900">{totalCount}</div>
            </div>
          </div>
        </div>
      </div>

      {/* TABS LỌC TRẠNG THÁI */}
      <div className="flex items-center space-x-2 border-b border-slate-200 pb-1 overflow-x-auto">
        <button
          onClick={() => { setStatusFilter(''); setPageIndex(1); }}
          className={`px-3.5 py-2 text-xs font-semibold rounded-lg transition shrink-0 ${
            statusFilter === ''
              ? 'bg-slate-900 text-white shadow-xs'
              : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
          }`}
        >
          Tất cả ({totalCount})
        </button>
        <button
          onClick={() => { setStatusFilter('Pending'); setPageIndex(1); }}
          className={`px-3.5 py-2 text-xs font-semibold rounded-lg transition shrink-0 flex items-center space-x-1.5 ${
            statusFilter === 'Pending'
              ? 'bg-amber-500 text-white shadow-xs'
              : 'text-slate-600 hover:text-amber-700 hover:bg-amber-50'
          }`}
        >
          <Clock className="w-3.5 h-3.5" />
          <span>Chờ thẩm định</span>
          {pendingCount > 0 && (
            <span className={`px-1.5 py-0.2 rounded-full text-[10px] font-bold ${
              statusFilter === 'Pending' ? 'bg-amber-600 text-white' : 'bg-amber-100 text-amber-800'
            }`}>
              {pendingCount}
            </span>
          )}
        </button>
        <button
          onClick={() => { setStatusFilter('Approved'); setPageIndex(1); }}
          className={`px-3.5 py-2 text-xs font-semibold rounded-lg transition shrink-0 flex items-center space-x-1.5 ${
            statusFilter === 'Approved'
              ? 'bg-emerald-600 text-white shadow-xs'
              : 'text-slate-600 hover:text-emerald-700 hover:bg-emerald-50'
          }`}
        >
          <CheckCircle2 className="w-3.5 h-3.5" />
          <span>Đã phê duyệt</span>
        </button>
        <button
          onClick={() => { setStatusFilter('Rejected'); setPageIndex(1); }}
          className={`px-3.5 py-2 text-xs font-semibold rounded-lg transition shrink-0 flex items-center space-x-1.5 ${
            statusFilter === 'Rejected'
              ? 'bg-rose-600 text-white shadow-xs'
              : 'text-slate-600 hover:text-rose-700 hover:bg-rose-50'
          }`}
        >
          <XCircle className="w-3.5 h-3.5" />
          <span>Đã từ chối</span>
        </button>
      </div>

      {/* THANH TÌM KIẾM & BỘ LỌC */}
      <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-2xs space-y-3 sm:space-y-0 sm:flex sm:items-center sm:justify-between gap-3">
        <div className="relative flex-1">
          <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
          <input
            type="text"
            value={keyword}
            onChange={(e) => { setKeyword(e.target.value); setPageIndex(1); }}
            placeholder="Tìm theo tên đơn vị, MST, người đại diện, email..."
            className="w-full pl-9 pr-4 py-2 text-xs bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
          />
        </div>

        <div className="flex items-center space-x-2">
          <select
            value={typeFilter}
            onChange={(e) => { setTypeFilter(e.target.value); setPageIndex(1); }}
            className="px-3 py-2 text-xs bg-slate-50 border border-slate-200 rounded-lg text-slate-700 focus:bg-white focus:outline-none focus:ring-2 focus:ring-sky-500 transition"
          >
            <option value="">Tất cả loại hình</option>
            <option value="Ban Quản lý Dự án">Ban Quản lý Dự án</option>
            <option value="Cơ quan hành chính nhà nước">Cơ quan hành chính</option>
            <option value="Đơn vị sự nghiệp công lập">Đơn vị sự nghiệp công lập</option>
            <option value="Doanh nghiệp nhà nước">Doanh nghiệp nhà nước</option>
            <option value="Đơn vị mua sắm tập trung">Đơn vị mua sắm tập trung</option>
          </select>

          {(keyword || typeFilter || statusFilter) && (
            <button
              onClick={() => { setKeyword(''); setTypeFilter(''); setStatusFilter(''); setPageIndex(1); }}
              className="p-2 text-slate-500 hover:text-slate-800 hover:bg-slate-100 rounded-lg transition"
              title="Đặt lại bộ lọc"
            >
              <RotateCcw className="w-4 h-4" />
            </button>
          )}
        </div>
      </div>

      {/* BẢNG DỮ LIỆU */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-2xs overflow-hidden">
        {loading ? (
          <div className="py-16">
            <LoadingSpinner message="Đang tải danh sách Bên mời thầu..." />
          </div>
        ) : entities.length === 0 ? (
          <div className="py-16 text-center space-y-3">
            <div className="inline-flex p-3 bg-slate-100 text-slate-400 rounded-full">
              <Briefcase className="w-8 h-8" />
            </div>
            <p className="text-sm font-semibold text-slate-700">
              Không tìm thấy Bên mời thầu nào phù hợp
            </p>
            <p className="text-xs text-slate-400 max-w-sm mx-auto">
              Thử tìm kiếm với từ khóa khác hoặc xóa bộ lọc để hiển thị đầy đủ danh sách.
            </p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse text-xs">
              <thead>
                <tr className="bg-slate-50/75 border-b border-slate-200/80 text-[11px] font-bold text-slate-500 uppercase tracking-wider">
                  <th className="py-3 px-4">Cơ quan / Đơn vị mời thầu</th>
                  <th className="py-3 px-4">Mã số thuế & ĐVQHNS</th>
                  <th className="py-3 px-4">Người đại diện / Cán bộ</th>
                  <th className="py-3 px-4">Quyết định thành lập</th>
                  <th className="py-3 px-4">Trạng thái</th>
                  <th className="py-3 px-4">Ngày đăng ký</th>
                  <th className="py-3 px-4 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {entities.map((ent) => (
                  <tr key={ent.id} className="hover:bg-slate-50/60 transition group">
                    {/* Tên cơ quan & Loại hình */}
                    <td className="py-3.5 px-4 max-w-xs">
                      <div>
                        <span className="inline-block px-1.5 py-0.5 text-[10px] font-semibold bg-sky-50 text-sky-700 rounded mb-0.5">
                          {ent.organizationType}
                        </span>
                        <div className="font-bold text-slate-900 group-hover:text-sky-700 transition line-clamp-2">
                          {ent.organizationName}
                        </div>
                        {ent.address && (
                          <div className="flex items-center space-x-1 text-[11px] text-slate-400 mt-0.5 truncate">
                            <MapPin className="w-3 h-3 shrink-0" />
                            <span className="truncate">{ent.address}</span>
                          </div>
                        )}
                      </div>
                    </td>

                    {/* MST & Mã ngân sách */}
                    <td className="py-3.5 px-4">
                      <div className="font-mono font-bold text-slate-800">
                        {ent.taxCode}
                      </div>
                      {ent.budgetCode && (
                        <div className="text-[10px] text-slate-500">
                          NS: {ent.budgetCode}
                        </div>
                      )}
                    </td>

                    {/* Người đại diện & Liên hệ */}
                    <td className="py-3.5 px-4">
                      <div className="font-semibold text-slate-900">
                        {ent.representativeName}
                      </div>
                      <div className="text-[10px] text-slate-500">
                        {ent.representativeTitle}
                      </div>
                      <div className="flex items-center space-x-1 text-[11px] text-slate-500 mt-0.5">
                        <Mail className="w-3 h-3 text-slate-400" />
                        <span>{ent.email}</span>
                      </div>
                    </td>

                    {/* Tệp Quyết định thành lập */}
                    <td className="py-3.5 px-4">
                      <button
                        type="button"
                        onClick={() => handleViewEstablishmentFile(ent.id)}
                        className="inline-flex items-center space-x-1 px-2.5 py-1 bg-slate-100 hover:bg-sky-50 text-slate-700 hover:text-sky-700 font-medium rounded-lg transition"
                      >
                        <FileText className="w-3.5 h-3.5" />
                        <span>Xem PDF</span>
                        <ExternalLink className="w-3 h-3" />
                      </button>
                    </td>

                    {/* Trạng thái */}
                    <td className="py-3.5 px-4">
                      <StatusBadge status={ent.verificationStatus} />
                    </td>

                    {/* Ngày đăng ký */}
                    <td className="py-3.5 px-4 text-slate-500 whitespace-nowrap">
                      {formatDate(ent.createdAt)}
                    </td>

                    {/* Nút Thao tác Thẩm định */}
                    <td className="py-3.5 px-4 text-right">
                      <button
                        type="button"
                        onClick={() => setSelectedEntity(ent)}
                        className={`px-3 py-1.5 text-xs font-bold rounded-lg transition flex items-center space-x-1 ml-auto shadow-2xs ${
                          ent.verificationStatus === 'Pending'
                            ? 'bg-amber-500 hover:bg-amber-600 text-white'
                            : 'bg-slate-100 hover:bg-slate-200 text-slate-700'
                        }`}
                      >
                        <ShieldCheck className="w-3.5 h-3.5" />
                        <span>{ent.verificationStatus === 'Pending' ? 'Thẩm định ngay' : 'Xem / Đổi trạng thái'}</span>
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* PHÂN TRANG */}
        {totalCount > pageSize && (
          <div className="flex items-center justify-between px-4 py-3 border-t border-slate-100 bg-slate-50/50 text-xs text-slate-500">
            <div>
              Hiển thị <span className="font-semibold text-slate-700">{(pageIndex - 1) * pageSize + 1}</span> -{' '}
              <span className="font-semibold text-slate-700">{Math.min(pageIndex * pageSize, totalCount)}</span> trên{' '}
              <span className="font-semibold text-slate-700">{totalCount}</span> đơn vị
            </div>

            <div className="flex items-center space-x-1.5">
              <button
                disabled={pageIndex <= 1}
                onClick={() => setPageIndex((p) => Math.max(1, p - 1))}
                className="p-1.5 rounded-lg border border-slate-200 bg-white hover:bg-slate-50 disabled:opacity-40 transition"
              >
                <ChevronLeft className="w-4 h-4" />
              </button>
              <span className="px-2 font-medium">
                {pageIndex} / {totalPages}
              </span>
              <button
                disabled={pageIndex >= totalPages}
                onClick={() => setPageIndex((p) => Math.min(totalPages, p + 1))}
                className="p-1.5 rounded-lg border border-slate-200 bg-white hover:bg-slate-50 disabled:opacity-40 transition"
              >
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}
      </div>

      {/* MODAL THẨM ĐỊNH BÊN MỜI THẦU */}
      <VerifyProcuringEntityModal
        isOpen={Boolean(selectedEntity)}
        onClose={() => setSelectedEntity(null)}
        entity={selectedEntity}
        onSuccess={() => {
          fetchEntities();
          fetchPendingCount();
        }}
      />
    </div>
  );
};

export default ProcuringEntitiesPage;
