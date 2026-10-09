import React, { useState, useEffect, useCallback } from 'react';
import { contractorApi } from '../api/contractorApi';
import { reportApi } from '../api/reportApi';
import { useAuth } from '../context/AuthContext';
import { formatDate } from '../utils/formatters';
import StatusBadge from '../components/common/StatusBadge';
import LoadingSpinner from '../components/common/LoadingSpinner';
import VerifyContractorModal from '../components/contractors/VerifyContractorModal';
import toast from 'react-hot-toast';
import {
  Building2,
  Search,
  Filter,
  Download,
  Star,
  FileCheck2,
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
  ExternalLink
} from 'lucide-react';

export const ContractorsPage = () => {
  const { user: currentUser } = useAuth();

  const [contractors, setContractors] = useState([]);
  const [loading, setLoading] = useState(true);
  const [totalCount, setTotalCount] = useState(0);
  const [pageIndex, setPageIndex] = useState(1);
  const [pageSize] = useState(10);

  // Bộ lọc
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState(''); // '', 'Pending', 'Approved', 'Rejected'
  const [pendingCount, setPendingCount] = useState(0);

  // Modal thẩm định
  const [selectedContractor, setSelectedContractor] = useState(null);
  const [exporting, setExporting] = useState(false);

  // Tải danh sách nhà thầu
  const fetchContractors = useCallback(async () => {
    setLoading(true);
    try {
      const params = {
        pageIndex,
        pageSize,
        searchTerm: searchTerm.trim() || undefined,
        verificationStatus: statusFilter || undefined,
      };

      const res = await contractorApi.getContractors(params);
      if (res && res.success && res.data) {
        setContractors(res.data.items || []);
        setTotalCount(res.data.totalCount || 0);
      }
    } catch (err) {
      console.error('Lỗi tải danh sách nhà thầu:', err);
      toast.error('Không thể tải danh sách nhà thầu: ' + (err.message || 'Lỗi mạng'));
    } finally {
      setLoading(false);
    }
  }, [pageIndex, pageSize, searchTerm, statusFilter]);

  // Đếm số lượng nhà thầu chờ duyệt (Pending)
  const fetchPendingCount = useCallback(async () => {
    try {
      const res = await contractorApi.getContractors({
        pageIndex: 1,
        pageSize: 100,
        verificationStatus: 'Pending',
      });
      if (res && res.success && res.data) {
        setPendingCount(res.data.totalCount || 0);
      }
    } catch (err) {
      console.error('Lỗi lấy số lượng hồ sơ chờ duyệt:', err);
    }
  }, []);

  useEffect(() => {
    fetchContractors();
    fetchPendingCount();
  }, [fetchContractors, fetchPendingCount]);

  // Xử lý xuất CSV
  const handleExportCsv = async () => {
    try {
      setExporting(true);
      toast.loading('Đang xuất danh sách nhà thầu...', { id: 'export-csv' });
      const response = await reportApi.exportContractorsCsv();
      const blob = new Blob([response], { type: 'text/csv;charset=utf-8;' });
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `Danh_sach_nha_thau_${new Date().toISOString().slice(0, 10)}.csv`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
      toast.success('Xuất danh sách nhà thầu thành công!', { id: 'export-csv' });
    } catch (err) {
      toast.error('Lỗi khi xuất danh sách: ' + (err.message || 'Lỗi kết nối'), { id: 'export-csv' });
    } finally {
      setExporting(false);
    }
  };

  // Xem tệp GPKD trực tiếp
  const handleQuickViewLicense = async (e, contractorId) => {
    e.stopPropagation();
    try {
      toast.loading('Đang mở tệp GPKD...', { id: 'quick-license' });
      const response = await contractorApi.downloadLicenseFile(contractorId);
      const fileBlob = new Blob([response], { type: 'application/pdf' });
      const fileUrl = window.URL.createObjectURL(fileBlob);
      window.open(fileUrl, '_blank');
      toast.success('Đã mở tệp GPKD!', { id: 'quick-license' });
    } catch (err) {
      toast.error('Không thể mở tệp GPKD: ' + (err.message || 'Lỗi tải tệp'), { id: 'quick-license' });
    }
  };

  const totalPages = Math.ceil(totalCount / pageSize) || 1;

  return (
    <div className="space-y-6">
      {/* Header Phân hệ */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div className="space-y-1">
          <div className="flex items-center space-x-2.5">
            <div className="p-2 bg-indigo-50 text-indigo-600 rounded-xl">
              <Building2 className="w-6 h-6" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-slate-900">
                Quản lý Nhà thầu & Hồ sơ Năng lực
              </h1>
              <p className="text-xs text-slate-500">
                Thẩm định giấy phép kinh doanh, xét duyệt tư cách tham gia đấu thầu và đánh giá uy tín nhà thầu
              </p>
            </div>
          </div>
        </div>

        <div className="flex items-center space-x-2.5">
          <button
            onClick={handleExportCsv}
            disabled={exporting}
            className="inline-flex items-center space-x-1.5 px-3.5 py-2 text-xs font-semibold text-slate-700 bg-white hover:bg-slate-50 border border-slate-200 rounded-xl shadow-2xs transition disabled:opacity-50"
          >
            <Download className="w-4 h-4 text-slate-500" />
            <span>{exporting ? 'Đang xuất...' : 'Xuất danh sách (CSV)'}</span>
          </button>
        </div>
      </div>

      {/* Thanh điều hướng trạng thái & Tìm kiếm */}
      <div className="bg-white rounded-2xl p-4 border border-slate-200/80 shadow-2xs space-y-4">
        {/* Tab trạng thái */}
        <div className="flex flex-wrap items-center gap-2 border-b border-slate-100 pb-3">
          <button
            onClick={() => {
              setStatusFilter('');
              setPageIndex(1);
            }}
            className={`px-3 py-1.5 text-xs font-semibold rounded-lg transition ${
              statusFilter === ''
                ? 'bg-slate-900 text-white shadow-xs'
                : 'text-slate-600 hover:bg-slate-100'
            }`}
          >
            Tất cả nhà thầu
          </button>

          <button
            onClick={() => {
              setStatusFilter('Pending');
              setPageIndex(1);
            }}
            className={`inline-flex items-center space-x-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg transition ${
              statusFilter === 'Pending'
                ? 'bg-amber-500 text-white shadow-xs'
                : 'text-amber-700 bg-amber-50/70 hover:bg-amber-100 border border-amber-200/60'
            }`}
          >
            <Clock className="w-3.5 h-3.5" />
            <span>Chờ thẩm định</span>
            {pendingCount > 0 && (
              <span className="ml-1 px-1.5 py-0.2 rounded-full text-[10px] font-bold bg-amber-200 text-amber-900 animate-pulse">
                {pendingCount}
              </span>
            )}
          </button>

          <button
            onClick={() => {
              setStatusFilter('Approved');
              setPageIndex(1);
            }}
            className={`inline-flex items-center space-x-1 px-3 py-1.5 text-xs font-semibold rounded-lg transition ${
              statusFilter === 'Approved'
                ? 'bg-emerald-600 text-white shadow-xs'
                : 'text-emerald-700 bg-emerald-50/70 hover:bg-emerald-100 border border-emerald-200/60'
            }`}
          >
            <CheckCircle2 className="w-3.5 h-3.5" />
            <span>Đã kích hoạt</span>
          </button>

          <button
            onClick={() => {
              setStatusFilter('Rejected');
              setPageIndex(1);
            }}
            className={`inline-flex items-center space-x-1 px-3 py-1.5 text-xs font-semibold rounded-lg transition ${
              statusFilter === 'Rejected'
                ? 'bg-rose-600 text-white shadow-xs'
                : 'text-rose-700 bg-rose-50/70 hover:bg-rose-100 border border-rose-200/60'
            }`}
          >
            <XCircle className="w-3.5 h-3.5" />
            <span>Đã từ chối</span>
          </button>
        </div>

        {/* Ô tìm kiếm & Reset */}
        <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3">
          <div className="relative flex-1 max-w-md">
            <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Tìm kiếm theo Tên công ty hoặc Mã số thuế..."
              className="w-full text-xs pl-9 pr-3 py-2 rounded-xl border border-slate-200 focus:outline-hidden focus:ring-2 focus:ring-sky-500 transition"
            />
          </div>

          <div className="flex items-center space-x-2">
            {(searchTerm || statusFilter) && (
              <button
                onClick={() => {
                  setSearchTerm('');
                  setStatusFilter('');
                  setPageIndex(1);
                }}
                className="inline-flex items-center space-x-1 px-3 py-2 text-xs font-medium text-slate-500 hover:text-slate-800 hover:bg-slate-100 rounded-xl transition"
              >
                <RotateCcw className="w-3.5 h-3.5" />
                <span>Đặt lại</span>
              </button>
            )}

            <span className="text-xs text-slate-500 font-medium">
              Tổng số: <strong className="text-slate-800">{totalCount}</strong> nhà thầu
            </span>
          </div>
        </div>
      </div>

      {/* Bảng danh sách nhà thầu */}
      <div className="bg-white rounded-2xl border border-slate-200/80 shadow-2xs overflow-hidden">
        {loading ? (
          <div className="py-20 flex justify-center">
            <LoadingSpinner text="Đang tải danh sách nhà thầu..." />
          </div>
        ) : contractors.length === 0 ? (
          <div className="py-16 text-center space-y-2">
            <Building2 className="w-10 h-10 text-slate-300 mx-auto" />
            <p className="text-sm font-semibold text-slate-600">Không tìm thấy nhà thầu nào</p>
            <p className="text-xs text-slate-400">
              Hãy thử thay đổi điều kiện tìm kiếm hoặc bộ lọc trạng thái.
            </p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse text-xs">
              <thead>
                <tr className="bg-slate-50/80 border-b border-slate-200/80 text-slate-500 uppercase tracking-wider text-[11px] font-bold">
                  <th className="py-3 px-4">Nhà thầu / Doanh nghiệp</th>
                  <th className="py-3 px-4">Đại diện & Liên hệ</th>
                  <th className="py-3 px-4">Giấy phép ĐKKD</th>
                  <th className="py-3 px-4 text-center">Uy tín</th>
                  <th className="py-3 px-4 text-center">Trạng thái</th>
                  <th className="py-3 px-4 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {contractors.map((c) => (
                  <tr
                    key={c.id}
                    className="hover:bg-slate-50/60 transition group"
                  >
                    {/* Doanh nghiệp */}
                    <td className="py-3.5 px-4 max-w-xs">
                      <div className="space-y-1">
                        <div className="font-bold text-slate-900 leading-snug">
                          {c.companyName}
                        </div>
                        <div className="flex items-center space-x-2 text-[11px] text-slate-500">
                          <span>
                            MST: <strong className="font-mono text-slate-700">{c.taxCode || 'N/A'}</strong>
                          </span>
                          <span>•</span>
                          <span>ID #{c.id}</span>
                        </div>
                        {c.address && (
                          <div className="flex items-center space-x-1 text-[11px] text-slate-500 truncate" title={c.address}>
                            <MapPin className="w-3 h-3 text-slate-400 flex-shrink-0" />
                            <span className="truncate">{c.address}</span>
                          </div>
                        )}
                      </div>
                    </td>

                    {/* Đại diện & Liên hệ */}
                    <td className="py-3.5 px-4">
                      <div className="space-y-1 text-slate-600">
                        <div className="font-semibold text-slate-900">
                          {c.fullName || 'Chưa cập nhật'}
                        </div>
                        <div className="flex items-center space-x-1.5 text-[11px] text-slate-500">
                          <Mail className="w-3 h-3 text-slate-400 flex-shrink-0" />
                          <span className="truncate max-w-[150px]">{c.email}</span>
                        </div>
                        {c.phone && (
                          <div className="flex items-center space-x-1.5 text-[11px] text-slate-500">
                            <Phone className="w-3 h-3 text-slate-400 flex-shrink-0" />
                            <span>{c.phone}</span>
                          </div>
                        )}
                      </div>
                    </td>

                    {/* Giấy phép ĐKKD */}
                    <td className="py-3.5 px-4">
                      {c.businessLicenseFile ? (
                        <button
                          type="button"
                          onClick={(e) => handleQuickViewLicense(e, c.id)}
                          className="inline-flex items-center space-x-1 px-2.5 py-1 text-[11px] font-semibold text-sky-700 bg-sky-50 hover:bg-sky-100 rounded-lg border border-sky-200 transition"
                        >
                          <FileCheck2 className="w-3.5 h-3.5 text-sky-600" />
                          <span>Xem GPKD</span>
                          <ExternalLink className="w-3 h-3 text-sky-400" />
                        </button>
                      ) : (
                        <span className="text-slate-400 italic text-[11px]">Chưa tải lên</span>
                      )}
                    </td>

                    {/* Điểm uy tín */}
                    <td className="py-3.5 px-4 text-center">
                      <div className="inline-flex items-center space-x-1 bg-amber-50 px-2 py-0.5 rounded-md border border-amber-200/60 font-bold text-amber-700 text-xs">
                        <Star className="w-3.5 h-3.5 fill-amber-400 text-amber-400" />
                        <span>{c.rating?.toFixed(1) || '0.0'}</span>
                      </div>
                    </td>

                    {/* Trạng thái */}
                    <td className="py-3.5 px-4 text-center">
                      <StatusBadge
                        status={c.verificationStatus}
                        type="contractor_verification"
                      />
                    </td>

                    {/* Thao tác */}
                    <td className="py-3.5 px-4 text-right">
                      <button
                        onClick={() => setSelectedContractor(c)}
                        className={`inline-flex items-center space-x-1 px-3 py-1.5 rounded-lg text-xs font-bold transition shadow-2xs ${
                          c.verificationStatus === 'Pending'
                            ? 'bg-amber-600 hover:bg-amber-700 text-white ring-2 ring-amber-300'
                            : 'bg-slate-100 hover:bg-slate-200 text-slate-700'
                        }`}
                      >
                        <ShieldCheck className="w-3.5 h-3.5" />
                        <span>
                          {c.verificationStatus === 'Pending' ? 'Thẩm định ngay' : 'Xem / Sửa'}
                        </span>
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* Phân trang */}
        {totalCount > pageSize && (
          <div className="px-4 py-3 bg-slate-50/70 border-t border-slate-200/80 flex items-center justify-between text-xs text-slate-500">
            <span>
              Trang <strong>{pageIndex}</strong> / <strong>{totalPages}</strong> (Hiển thị {contractors.length} nhà thầu)
            </span>

            <div className="flex items-center space-x-1.5">
              <button
                disabled={pageIndex <= 1}
                onClick={() => setPageIndex((p) => p - 1)}
                className="p-1.5 rounded-lg border border-slate-200 bg-white hover:bg-slate-100 disabled:opacity-40 transition"
              >
                <ChevronLeft className="w-4 h-4" />
              </button>
              <button
                disabled={pageIndex >= totalPages}
                onClick={() => setPageIndex((p) => p + 1)}
                className="p-1.5 rounded-lg border border-slate-200 bg-white hover:bg-slate-100 disabled:opacity-40 transition"
              >
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Modal Thẩm định nhà thầu */}
      <VerifyContractorModal
        isOpen={Boolean(selectedContractor)}
        onClose={() => setSelectedContractor(null)}
        contractor={selectedContractor}
        onSuccess={() => {
          fetchContractors();
          fetchPendingCount();
        }}
      />
    </div>
  );
};

export default ContractorsPage;
