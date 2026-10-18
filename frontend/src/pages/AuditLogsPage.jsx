import React, { useState, useEffect, useCallback } from 'react';
import {
  Shield,
  Search,
  Filter,
  RotateCcw,
  Download,
  Calendar,
  Eye,
  Database,
  User,
  Globe,
  ChevronLeft,
  ChevronRight,
  Loader2,
  FileText,
  AlertCircle,
  Activity,
} from 'lucide-react';
import auditLogApi from '../api/auditLogApi';
import AuditLogDetailModal from '../components/audit/AuditLogDetailModal';
import { formatDateTime, formatRelativeTime } from '../utils/formatters';
import toast from 'react-hot-toast';

// Danh sách hành động phổ biến
const COMMON_ACTIONS = [
  { value: '', label: 'Tất cả hành động' },
  { value: 'LOGIN', label: 'LOGIN (Đăng nhập)' },
  { value: 'CREATE', label: 'CREATE (Khởi tạo)' },
  { value: 'UPDATE', label: 'UPDATE (Cập nhật)' },
  { value: 'DELETE', label: 'DELETE (Xóa)' },
  { value: 'VERIFY_CONTRACTOR', label: 'VERIFY_CONTRACTOR (Thẩm định nhà thầu)' },
  { value: 'VERIFY_PROCURING_ENTITY', label: 'VERIFY_PROCURING_ENTITY (Thẩm định bên mời thầu)' },
  { value: 'FINALIZE_AWARD', label: 'FINALIZE_AWARD (Trao thầu)' },
  { value: 'REVOKE_AWARD', label: 'REVOKE_AWARD (Thu hồi trao thầu)' },
  { value: 'DISQUALIFY_SUBMISSION', label: 'DISQUALIFY_SUBMISSION (Loại hồ sơ dự thầu)' },
  { value: 'SUBMIT_BID', label: 'SUBMIT_BID (Nộp hồ sơ dự thầu)' },
  { value: 'ASSIGN_EVALUATOR', label: 'ASSIGN_EVALUATOR (Phân công giám khảo)' },
];

// Danh sách loại thực thể (đã bổ sung ContractMilestone, Acceptance, ProgressUpdate - REV-09)
const ENTITY_TYPES = [
  { value: '', label: 'Tất cả thực thể' },
  { value: 'BidPackage', label: 'Gói thầu (BidPackage)' },
  { value: 'BidSubmission', label: 'Hồ sơ dự thầu (BidSubmission)' },
  { value: 'Contractor', label: 'Nhà thầu (Contractor)' },
  { value: 'ProcuringEntity', label: 'Bên mời thầu (ProcuringEntity)' },
  { value: 'Contract', label: 'Hợp đồng (Contract)' },
  { value: 'ContractMilestone', label: 'Giai đoạn hợp đồng (ContractMilestone)' },
  { value: 'Acceptance', label: 'Nghiệm thu (Acceptance)' },
  { value: 'ProgressUpdate', label: 'Tiến độ thực hiện (ProgressUpdate)' },
  { value: 'EvaluationScore', label: 'Điểm chấm thầu (EvaluationScore)' },
  { value: 'User', label: 'Người dùng (User)' },
];

const getActionBadge = (action) => {
  const upper = (action || '').toUpperCase();
  if (upper.includes('CREATE') || upper.includes('ADD') || upper.includes('REGISTER')) {
    return 'bg-emerald-50 text-emerald-700 border-emerald-200';
  }
  if (upper.includes('UPDATE') || upper.includes('EDIT')) {
    return 'bg-blue-50 text-blue-700 border-blue-200';
  }
  if (upper.includes('VERIFY') || upper.includes('APPROVE') || upper.includes('FINALIZE') || upper.includes('AWARD')) {
    return 'bg-indigo-50 text-indigo-700 border-indigo-200';
  }
  if (upper.includes('DELETE') || upper.includes('REVOKE') || upper.includes('DISQUALIFY') || upper.includes('REJECT')) {
    return 'bg-rose-50 text-rose-700 border-rose-200';
  }
  return 'bg-slate-100 text-slate-700 border-slate-200';
};

// Hàm phòng chống CSV Formula Injection (CWE-1236) - REV-03
const sanitizeCsvCell = (val) => {
  if (val === null || val === undefined) return '""';
  let str = String(val);
  if (/^[=+\-@\t\r]/.test(str)) {
    str = `'${str}`;
  }
  return `"${str.replace(/"/g, '""')}"`;
};

export const AuditLogsPage = () => {
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [totalCount, setTotalCount] = useState(0);
  const [pageIndex, setPageIndex] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [totalPages, setTotalPages] = useState(1);

  // REV-02: Tách biệt draft filter input và applied filter
  const [filterInputs, setFilterInputs] = useState({
    action: '',
    entityType: '',
    entityId: '',
    userId: '',
    fromDate: '',
    toDate: '',
  });

  const [appliedFilters, setAppliedFilters] = useState({
    action: '',
    entityType: '',
    entityId: '',
    userId: '',
    fromDate: '',
    toDate: '',
  });

  // Modal chi tiết
  const [selectedLog, setSelectedLog] = useState(null);
  const [isDetailOpen, setIsDetailOpen] = useState(false);

  // Tải danh sách Audit Logs (Chỉ phụ thuộc pageIndex, pageSize, appliedFilters)
  const fetchAuditLogs = useCallback(async () => {
    setLoading(true);
    try {
      const params = {
        pageIndex,
        pageSize,
        action: appliedFilters.action?.trim() || undefined,
        entityType: appliedFilters.entityType?.trim() || undefined,
        entityId: appliedFilters.entityId ? parseInt(appliedFilters.entityId, 10) : undefined,
        userId: appliedFilters.userId ? parseInt(appliedFilters.userId, 10) : undefined,
        // REV-04: Sửa lệch múi giờ, định dạng rõ ràng ngày bắt đầu và kết thúc
        fromDate: appliedFilters.fromDate ? `${appliedFilters.fromDate}T00:00:00` : undefined,
        toDate: appliedFilters.toDate ? `${appliedFilters.toDate}T23:59:59.999` : undefined,
      };

      const res = await auditLogApi.getAuditLogs(params);
      if (res && res.success && res.data) {
        setLogs(res.data.items || []);
        setTotalCount(res.data.totalCount || 0);
        setTotalPages(res.data.totalPages || 1);
      } else {
        toast.error(res?.message || 'Không thể tải nhật ký kiểm toán');
      }
    } catch {
      toast.error('Lỗi kết nối khi tải danh sách nhật ký kiểm toán');
    } finally {
      setLoading(false);
    }
  }, [pageIndex, pageSize, appliedFilters]);

  useEffect(() => {
    fetchAuditLogs();
  }, [fetchAuditLogs]);

  // Cập nhật giá trị nhập vào form
  const handleInputChange = (field, value) => {
    setFilterInputs((prev) => ({
      ...prev,
      [field]: value,
    }));
  };

  // Xử lý tìm kiếm và validation ngày tháng (REV-04)
  const handleApplyFilter = (e) => {
    e.preventDefault();
    if (filterInputs.fromDate && filterInputs.toDate) {
      if (filterInputs.fromDate > filterInputs.toDate) {
        toast.error('Từ ngày không được lớn hơn Đến ngày');
        return;
      }
    }
    setAppliedFilters({ ...filterInputs });
    setPageIndex(1);
  };

  // Đặt lại bộ lọc
  const handleResetFilter = () => {
    const emptyFilters = {
      action: '',
      entityType: '',
      entityId: '',
      userId: '',
      fromDate: '',
      toDate: '',
    };
    setFilterInputs(emptyFilters);
    setAppliedFilters(emptyFilters);
    setPageIndex(1);
  };

  // Xem chi tiết
  const handleViewDetail = (log) => {
    setSelectedLog(log);
    setIsDetailOpen(true);
  };

  // Xuất file CSV (REV-03 & REV-10)
  const handleExportCsv = () => {
    if (logs.length === 0) {
      toast.error('Không có dữ liệu để xuất file');
      return;
    }

    const headers = [
      'ID',
      'Thời gian',
      'User ID',
      'Họ và tên',
      'Email',
      'Hành động (Action)',
      'Thực thể (Entity)',
      'Mã thực thể (Entity ID)',
      'Địa chỉ IP',
      'Giá trị cũ (OldValues)',
      'Giá trị mới (NewValues)',
    ];

    const rows = logs.map((l) => [
      sanitizeCsvCell(l.id),
      sanitizeCsvCell(formatDateTime(l.timestamp)),
      sanitizeCsvCell(l.userId ?? ''),
      sanitizeCsvCell(l.userFullName || ''),
      sanitizeCsvCell(l.userEmail || ''),
      sanitizeCsvCell(l.action || ''),
      sanitizeCsvCell(l.entityType || ''),
      sanitizeCsvCell(l.entityId ?? ''),
      sanitizeCsvCell(l.ipAddress || ''),
      sanitizeCsvCell(l.oldValues || ''),
      sanitizeCsvCell(l.newValues || ''),
    ]);

    const csvContent =
      '\uFEFF' + // UTF-8 BOM for Excel
      [headers.map(sanitizeCsvCell).join(','), ...rows.map((e) => e.join(','))].join('\r\n');

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.setAttribute('href', url);
    link.setAttribute(
      'download',
      `Audit_Logs_${new Date().toISOString().slice(0, 10)}.csv`
    );
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    // REV-10: Dọn dẹp URL Object sau 1000ms tương thích Safari/Firefox
    setTimeout(() => {
      URL.revokeObjectURL(url);
    }, 1000);

    toast.success('Đã xuất file CSV nhật ký kiểm toán');
  };

  return (
    <div className="space-y-6 animate-in fade-in duration-200">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-white p-6 rounded-2xl border border-slate-200 shadow-xs">
        <div className="flex items-center space-x-4">
          <div className="w-12 h-12 rounded-2xl bg-slate-900 text-sky-400 flex items-center justify-center shadow-xs flex-shrink-0">
            <Activity className="w-6 h-6" />
          </div>
          <div>
            <div className="flex items-center space-x-3">
              <h1 className="text-xl font-bold text-slate-900">
                Nhật ký kiểm toán hệ thống
              </h1>
              <span className="px-2.5 py-0.5 rounded-full text-xs font-bold bg-sky-50 text-sky-700 border border-sky-200">
                Audit Trail
              </span>
            </div>
            <p className="text-xs text-slate-500 mt-1">
              Hệ thống giám sát và truy vết bất biến mọi thao tác nhạy cảm, phê duyệt hồ sơ và sửa đổi dữ liệu.
            </p>
          </div>
        </div>

        {/* Action Buttons */}
        <div className="flex items-center space-x-2.5">
          <button
            type="button"
            onClick={fetchAuditLogs}
            disabled={loading}
            aria-label="Làm mới danh sách nhật ký kiểm toán"
            className="flex items-center space-x-1.5 px-3.5 py-2 text-xs font-semibold rounded-xl border border-slate-200 bg-white text-slate-700 hover:bg-slate-50 transition shadow-2xs"
          >
            <RotateCcw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
            <span>Làm mới</span>
          </button>

          <button
            type="button"
            onClick={handleExportCsv}
            disabled={logs.length === 0}
            aria-label="Xuất dữ liệu nhật ký kiểm toán ra tệp CSV"
            className="flex items-center space-x-1.5 px-4 py-2 text-xs font-semibold rounded-xl bg-slate-900 text-white hover:bg-slate-800 transition shadow-2xs disabled:opacity-50"
          >
            <Download className="w-3.5 h-3.5" />
            <span>Xuất CSV</span>
          </button>
        </div>
      </div>

      {/* Filter Toolbar */}
      <form
        onSubmit={handleApplyFilter}
        className="bg-white p-5 rounded-2xl border border-slate-200 shadow-xs space-y-4"
      >
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3">
          {/* Action Filter */}
          <div>
            <label htmlFor="filter-action" className="block text-xs font-semibold text-slate-700 mb-1">
              Hành động
            </label>
            <select
              id="filter-action"
              aria-label="Lọc theo hành động"
              value={filterInputs.action}
              onChange={(e) => handleInputChange('action', e.target.value)}
              className="w-full text-xs bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-800 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:bg-white"
            >
              {COMMON_ACTIONS.map((a) => (
                <option key={a.value} value={a.value}>
                  {a.label}
                </option>
              ))}
            </select>
          </div>

          {/* Entity Type Filter */}
          <div>
            <label htmlFor="filter-entity-type" className="block text-xs font-semibold text-slate-700 mb-1">
              Thực thể
            </label>
            <select
              id="filter-entity-type"
              aria-label="Lọc theo loại thực thể"
              value={filterInputs.entityType}
              onChange={(e) => handleInputChange('entityType', e.target.value)}
              className="w-full text-xs bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-800 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:bg-white"
            >
              {ENTITY_TYPES.map((e) => (
                <option key={e.value} value={e.value}>
                  {e.label}
                </option>
              ))}
            </select>
          </div>

          {/* Entity ID Filter */}
          <div>
            <label htmlFor="filter-entity-id" className="block text-xs font-semibold text-slate-700 mb-1">
              Mã thực thể (ID)
            </label>
            <input
              id="filter-entity-id"
              aria-label="Lọc theo mã thực thể ID"
              type="number"
              placeholder="Vd: 1074"
              value={filterInputs.entityId}
              onChange={(e) => handleInputChange('entityId', e.target.value)}
              className="w-full text-xs bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-800 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:bg-white"
            />
          </div>

          {/* User ID Filter */}
          <div>
            <label htmlFor="filter-user-id" className="block text-xs font-semibold text-slate-700 mb-1">
              User ID
            </label>
            <input
              id="filter-user-id"
              aria-label="Lọc theo mã người dùng User ID"
              type="number"
              placeholder="Vd: 1"
              value={filterInputs.userId}
              onChange={(e) => handleInputChange('userId', e.target.value)}
              className="w-full text-xs bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-800 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:bg-white"
            />
          </div>

          {/* From Date */}
          <div>
            <label htmlFor="filter-from-date" className="block text-xs font-semibold text-slate-700 mb-1">
              Từ ngày
            </label>
            <input
              id="filter-from-date"
              aria-label="Lọc từ ngày"
              type="date"
              value={filterInputs.fromDate}
              onChange={(e) => handleInputChange('fromDate', e.target.value)}
              className="w-full text-xs bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-800 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:bg-white"
            />
          </div>

          {/* To Date */}
          <div>
            <label htmlFor="filter-to-date" className="block text-xs font-semibold text-slate-700 mb-1">
              Đến ngày
            </label>
            <input
              id="filter-to-date"
              aria-label="Lọc đến ngày"
              type="date"
              value={filterInputs.toDate}
              onChange={(e) => handleInputChange('toDate', e.target.value)}
              className="w-full text-xs bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-800 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:bg-white"
            />
          </div>
        </div>

        {/* Buttons */}
        <div className="flex items-center justify-between pt-2 border-t border-slate-100">
          <div className="text-xs text-slate-500">
            Hiển thị <span className="font-bold text-slate-800">{logs.length}</span> / tổng số{' '}
            <span className="font-bold text-slate-800">{totalCount}</span> bản ghi
          </div>

          <div className="flex items-center space-x-2">
            <button
              type="button"
              onClick={handleResetFilter}
              aria-label="Đặt lại toàn bộ bộ lọc"
              className="px-3.5 py-1.5 text-xs font-semibold rounded-xl border border-slate-200 text-slate-600 hover:bg-slate-100 transition"
            >
              Đặt lại
            </button>
            <button
              type="submit"
              aria-label="Áp dụng bộ lọc tìm kiếm"
              className="flex items-center space-x-1.5 px-4 py-1.5 text-xs font-semibold rounded-xl bg-sky-600 text-white hover:bg-sky-700 shadow-2xs transition"
            >
              <Search className="w-3.5 h-3.5" />
              <span>Tìm kiếm</span>
            </button>
          </div>
        </div>
      </form>

      {/* Audit Log Table */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-xs overflow-hidden">
        {loading ? (
          <div className="p-16 flex flex-col items-center justify-center space-y-3">
            <Loader2 className="w-8 h-8 text-sky-600 animate-spin" />
            <p className="text-xs font-medium text-slate-500">
              Đang tải danh sách nhật ký kiểm toán...
            </p>
          </div>
        ) : logs.length === 0 ? (
          <div className="p-16 text-center space-y-3">
            <div className="w-12 h-12 rounded-2xl bg-slate-100 text-slate-400 mx-auto flex items-center justify-center">
              <FileText className="w-6 h-6" />
            </div>
            <h3 className="text-sm font-bold text-slate-800">
              Không tìm thấy bản ghi kiểm toán nào
            </h3>
            <p className="text-xs text-slate-500 max-w-sm mx-auto">
              Không có sự kiện kiểm toán nào phù hợp với điều kiện bộ lọc hiện tại. Thử thay đổi hoặc đặt lại bộ lọc.
            </p>
            <button
              type="button"
              onClick={handleResetFilter}
              aria-label="Xóa toàn bộ bộ lọc để xem tất cả"
              className="inline-flex items-center space-x-1.5 px-4 py-2 text-xs font-semibold rounded-xl bg-slate-900 text-white hover:bg-slate-800 transition"
            >
              <RotateCcw className="w-3.5 h-3.5" />
              <span>Xóa bộ lọc</span>
            </button>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs border-collapse">
              <thead>
                <tr className="bg-slate-50/80 border-b border-slate-200 text-slate-700">
                  <th className="py-3 px-4 font-bold w-16">ID</th>
                  <th className="py-3 px-4 font-bold w-44">Thời gian</th>
                  <th className="py-3 px-4 font-bold w-64">Người thực hiện</th>
                  <th className="py-3 px-4 font-bold w-48">Hành động</th>
                  <th className="py-3 px-4 font-bold w-44">Thực thể</th>
                  <th className="py-3 px-4 font-bold w-32">Địa chỉ IP</th>
                  <th className="py-3 px-4 font-bold text-right w-24">Chi tiết</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 font-sans">
                {logs.map((log) => {
                  const badgeClass = getActionBadge(log.action);
                  return (
                    <tr
                      key={log.id}
                      className="hover:bg-slate-50/70 transition group cursor-pointer"
                      onClick={() => handleViewDetail(log)}
                    >
                      {/* ID */}
                      <td className="py-3 px-4 font-mono font-semibold text-slate-500">
                        #{log.id}
                      </td>

                      {/* Timestamp */}
                      <td className="py-3 px-4">
                        <div className="font-semibold text-slate-800">
                          {formatDateTime(log.timestamp)}
                        </div>
                        <div className="text-[11px] text-slate-400">
                          {formatRelativeTime(log.timestamp)}
                        </div>
                      </td>

                      {/* User / Performer */}
                      <td className="py-3 px-4">
                        <div className="font-bold text-slate-900 truncate max-w-xs">
                          {log.userFullName || 'System / Anonymous'}
                        </div>
                        <div className="text-[11px] text-slate-500 truncate max-w-xs">
                          {log.userEmail || `User ID: ${log.userId ?? 'N/A'}`}
                        </div>
                      </td>

                      {/* Action Badge */}
                      <td className="py-3 px-4">
                        <span
                          className={`inline-block px-2.5 py-1 rounded-full text-[11px] font-bold border tracking-wide ${badgeClass}`}
                        >
                          {log.action}
                        </span>
                      </td>

                      {/* Entity */}
                      <td className="py-3 px-4">
                        <div className="font-bold text-slate-800 flex items-center space-x-1">
                          <span>{log.entityType || '-'}</span>
                          {log.entityId && (
                            <span className="font-mono text-xs text-sky-600">
                              #{log.entityId}
                            </span>
                          )}
                        </div>
                      </td>

                      {/* IP Address */}
                      <td className="py-3 px-4 font-mono text-[11px] text-slate-500">
                        {log.ipAddress || 'Internal'}
                      </td>

                      {/* Actions */}
                      <td className="py-3 px-4 text-right">
                        <button
                          type="button"
                          onClick={(e) => {
                            e.stopPropagation();
                            handleViewDetail(log);
                          }}
                          aria-label={`Xem chi tiết bản ghi kiểm toán số ${log.id}`}
                          className="p-1.5 rounded-lg text-slate-400 hover:text-slate-900 hover:bg-slate-200 transition"
                          title="Xem chi tiết thay đổi"
                        >
                          <Eye className="w-4 h-4" />
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}

        {/* Pagination Bar */}
        {!loading && logs.length > 0 && (
          <div className="p-4 bg-slate-50 border-t border-slate-200 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs">
            <div className="flex items-center space-x-3 text-slate-600">
              <span>
                Trang <span className="font-bold text-slate-900">{pageIndex}</span> /{' '}
                <span className="font-bold text-slate-900">{totalPages}</span> (Tổng số{' '}
                <span className="font-bold text-slate-900">{totalCount}</span> bản ghi)
              </span>

              <div className="flex items-center space-x-1.5">
                <span className="text-slate-400">|</span>
                <label htmlFor="page-size-select" className="text-slate-600">
                  Số dòng:
                </label>
                <select
                  id="page-size-select"
                  aria-label="Chọn số dòng hiển thị mỗi trang"
                  value={pageSize}
                  onChange={(e) => {
                    setPageSize(parseInt(e.target.value, 10));
                    setPageIndex(1);
                  }}
                  className="bg-white border border-slate-200 rounded-lg px-2 py-1 text-xs text-slate-800 focus:outline-none"
                >
                  <option value={10}>10</option>
                  <option value={20}>20</option>
                  <option value={50}>50</option>
                  <option value={100}>100</option>
                </select>
              </div>
            </div>

            <div className="flex items-center space-x-1">
              <button
                type="button"
                onClick={() => setPageIndex((p) => Math.max(1, p - 1))}
                disabled={pageIndex <= 1}
                aria-label="Chuyển đến trang trước"
                className="flex items-center space-x-1 px-3 py-1.5 rounded-xl border border-slate-200 bg-white text-slate-700 hover:bg-slate-100 disabled:opacity-40 disabled:cursor-not-allowed transition font-semibold"
              >
                <ChevronLeft className="w-4 h-4" />
                <span>Trước</span>
              </button>

              <button
                type="button"
                onClick={() => setPageIndex((p) => Math.min(totalPages, p + 1))}
                disabled={pageIndex >= totalPages}
                aria-label="Chuyển đến trang tiếp theo"
                className="flex items-center space-x-1 px-3 py-1.5 rounded-xl border border-slate-200 bg-white text-slate-700 hover:bg-slate-100 disabled:opacity-40 disabled:cursor-not-allowed transition font-semibold"
              >
                <span>Sau</span>
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Detail Modal */}
      {selectedLog && (
        <AuditLogDetailModal
          log={selectedLog}
          isOpen={isDetailOpen}
          onClose={() => {
            setIsDetailOpen(false);
            setSelectedLog(null);
          }}
        />
      )}
    </div>
  );
};

export default AuditLogsPage;
