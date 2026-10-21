import React, { useState, useEffect, useCallback } from 'react';
import { contractorApi } from '../api/contractorApi';
import { reportApi } from '../api/reportApi';
import { useAuth } from '../context/AuthContext';
import { formatDate } from '../utils/formatters';
import StatusBadge from '../components/common/StatusBadge';
import LoadingSpinner from '../components/common/LoadingSpinner';
import VerifyContractorModal from '../components/contractors/VerifyContractorModal';
import ImportContractorsModal from '../components/contractors/ImportContractorsModal';
import ContractorCreditModal from '../components/contractors/ContractorCreditModal';
import toast from 'react-hot-toast';
import {
  Building2,
  Search,
  Filter,
  Download,
  Upload,
  FileSpreadsheet,
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
  ExternalLink,
  Award,
} from 'lucide-react';

const getContractorGrade = (rating) => {
  const r = Number(rating) || 0;
  if (r >= 85) return { grade: 'A', bg: 'bg-emerald-50 text-emerald-800 border-emerald-300' };
  if (r >= 70) return { grade: 'B', bg: 'bg-blue-50 text-blue-800 border-blue-300' };
  if (r >= 50) return { grade: 'C', bg: 'bg-amber-50 text-amber-800 border-amber-300' };
  if (r > 0) return { grade: 'D', bg: 'bg-rose-50 text-rose-800 border-rose-300' };
  return { grade: 'Chưa xếp', bg: 'bg-slate-100 text-slate-500 border-slate-200' };
};

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

  // Modal thẩm định & Import Excel & Credit Profile
  const [selectedContractor, setSelectedContractor] = useState(null);
  const [isImportModalOpen, setIsImportModalOpen] = useState(false);
  const [creditModalContractor, setCreditModalContractor] = useState(null);
  const [exporting, setExporting] = useState(false);
  const [exportingExcel, setExportingExcel] = useState(false);

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
      const blob = await reportApi.exportContractorsCsv({
        searchTerm: searchTerm.trim() || undefined,
        verificationStatus: statusFilter || undefined,
      });

      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `Danh_Sach_Nha_Thau_${new Date().toISOString().slice(0, 10)}.csv`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);

      toast.success('Xuất file CSV danh sách nhà thầu thành công!');
    } catch (err) {
      console.error('Lỗi xuất CSV nhà thầu:', err);
      toast.error('Không thể xuất file CSV: ' + (err.message || 'Lỗi hệ thống'));
    } finally {
      setExporting(false);
    }
  };

  // Xử lý xuất Excel
  const handleExportExcel = async () => {
    try {
      setExportingExcel(true);
      const blob = await reportApi.exportContractorsExcel({
        searchTerm: searchTerm.trim() || undefined,
        verificationStatus: statusFilter || undefined,
      });

      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `Danh_Sach_Nha_Thau_${new Date().toISOString().slice(0, 10)}.xlsx`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);

      toast.success('Xuất file Excel danh sách nhà thầu thành công!');
    } catch (err) {
      console.error('Lỗi xuất Excel nhà thầu:', err);
      toast.error('Không thể xuất file Excel: ' + (err.message || 'Lỗi hệ thống'));
    } finally {
      setExportingExcel(false);
    }
  };

  const handleQuickViewLicense = (e, contractorId) => {
    e.stopPropagation();
    window.open(`/api/contractors/${contractorId}/license`, '_blank', 'noopener,noreferrer');
  };

  const totalPages = Math.ceil(totalCount / pageSize) || 1;

  return (
    <div className="space-y-6">
      {/* Tiêu đề & Nút hành động */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold text-slate-900 flex items-center space-x-2">
            <Building2 className="w-6 h-6 text-sky-600" />
            <span>Quản lý hồ sơ nhà thầu</span>
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Thẩm định tính hợp lệ pháp lý, cấp quyền dự thầu và theo dõi hồ sơ năng lực nhà thầu
          </p>
        </div>

        <div className="flex items-center space-x-2">
          {/* Nút Nhập Excel */}
          <button
            onClick={() => setIsImportModalOpen(true)}
            className="inline-flex items-center space-x-1.5 px-3 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl text-xs font-semibold shadow-xs transition"
          >
            <Upload className="w-4 h-4" />
            <span>Nhập Excel</span>
          </button>

          {/* Nút Xuất Excel */}
          <button
            onClick={handleExportExcel}
            disabled={exportingExcel || contractors.length === 0}
            className="inline-flex items-center space-x-1.5 px-3 py-2 bg-teal-600 hover:bg-teal-700 disabled:opacity-50 text-white rounded-xl text-xs font-semibold shadow-xs transition"
          >
            <FileSpreadsheet className="w-4 h-4" />
            <span>{exportingExcel ? 'Đang xuất...' : 'Xuất Excel'}</span>
          </button>

          {/* Nút Xuất CSV */}
          <button
            onClick={handleExportCsv}
            disabled={exporting || contractors.length === 0}
            className="inline-flex items-center space-x-1.5 px-3 py-2 bg-white border border-slate-200 hover:bg-slate-50 disabled:opacity-50 text-slate-700 rounded-xl text-xs font-semibold shadow-2xs transition"
          >
            <Download className="w-4 h-4" />
            <span>{exporting ? 'Đang xuất...' : 'Xuất CSV'}</span>
          </button>
        </div>
      </div>

      {/* Thanh lọc & Tìm kiếm */}
      <div className="bg-white rounded-2xl border border-slate-200/80 p-4 shadow-2xs space-y-3">
        {/* Bộ lọc trạng thái tabs */}
        <div className="flex flex-wrap items-center gap-2 border-b border-slate-100 pb-3">
          <span className="text-xs font-semibold text-slate-500 mr-1 flex items-center space-x-1">
            <Filter className="w-3.5 h-3.5" />
            <span>Trạng thái:</span>
          </span>

          <button
            onClick={() => {
              setStatusFilter('');
              setPageIndex(1);
            }}
            className={`px-3 py-1.5 rounded-xl text-xs font-semibold transition ${
              statusFilter === ''
                ? 'bg-slate-900 text-white shadow-xs'
                : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
            }`}
          >
            Tất cả ({totalCount})
          </button>

          <button
            onClick={() => {
              setStatusFilter('Pending');
              setPageIndex(1);
            }}
            className={`inline-flex items-center space-x-1.5 px-3 py-1.5 rounded-xl text-xs font-semibold transition ${
              statusFilter === 'Pending'
                ? 'bg-amber-600 text-white shadow-xs'
                : 'bg-amber-50 text-amber-700 hover:bg-amber-100'
            }`}
          >
            <Clock className="w-3.5 h-3.5" />
            <span>Chờ thẩm định</span>
            {pendingCount > 0 && (
              <span className="px-1.5 py-0.2 rounded-full text-[10px] font-bold bg-amber-200 text-amber-900">
                {pendingCount}
              </span>
            )}
          </button>

          <button
            onClick={() => {
              setStatusFilter('Approved');
              setPageIndex(1);
            }}
            className={`inline-flex items-center space-x-1.5 px-3 py-1.5 rounded-xl text-xs font-semibold transition ${
              statusFilter === 'Approved'
                ? 'bg-emerald-600 text-white shadow-xs'
                : 'bg-emerald-50 text-emerald-700 hover:bg-emerald-100'
            }`}
          >
            <CheckCircle2 className="w-3.5 h-3.5" />
            <span>Đã thẩm định (Hợp lệ)</span>
          </button>

          <button
            onClick={() => {
              setStatusFilter('Rejected');
              setPageIndex(1);
            }}
            className={`inline-flex items-center space-x-1.5 px-3 py-1.5 rounded-xl text-xs font-semibold transition ${
              statusFilter === 'Rejected'
                ? 'bg-rose-600 text-white shadow-xs'
                : 'bg-rose-50 text-rose-700 hover:bg-rose-100'
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
                  <th className="py-3 px-4 text-center">Xếp hạng & Tín nhiệm</th>
                  <th className="py-3 px-4">Giấy phép ĐKKD</th>
                  <th className="py-3 px-4 text-center">Trạng thái</th>
                  <th className="py-3 px-4 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {contractors.map((c) => {
                  const gradeObj = getContractorGrade(c.rating);
                  const r = Number(c.rating) || 0;

                  return (
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

                      {/* Xếp hạng & Tín nhiệm (Nhóm B) */}
                      <td className="py-3.5 px-4 text-center">
                        <button
                          type="button"
                          onClick={() => setCreditModalContractor({ id: c.id, companyName: c.companyName })}
                          className="inline-flex flex-col items-center gap-1 group/btn cursor-pointer hover:opacity-85 transition p-1 rounded-lg hover:bg-slate-100/60"
                          title="Bấm để xem chi tiết hồ sơ năng lực & tín nhiệm"
                        >
                          <span
                            className={`inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-black border ${gradeObj.bg}`}
                          >
                            <Award className="w-3 h-3" />
                            <span>Hạng {gradeObj.grade}</span>
                          </span>
                          <span className="text-[10px] text-slate-500 font-semibold flex items-center gap-0.5">
                            <Star className="w-2.5 h-2.5 text-amber-500 fill-amber-500" />
                            {r > 0 ? `${r.toFixed(1)}/100` : 'Chưa đánh giá'}
                          </span>
                        </button>
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
                  );
                })}
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

      {/* Modal Nhập danh sách nhà thầu từ Excel */}
      <ImportContractorsModal
        isOpen={isImportModalOpen}
        onClose={() => setIsImportModalOpen(false)}
        onSuccess={() => {
          fetchContractors();
          fetchPendingCount();
        }}
      />

      {/* Modal Xem Hồ sơ Năng lực & Tín nhiệm Nhà thầu (Nhóm B) */}
      <ContractorCreditModal
        isOpen={Boolean(creditModalContractor)}
        onClose={() => setCreditModalContractor(null)}
        contractorId={creditModalContractor?.id}
        companyName={creditModalContractor?.companyName}
      />
    </div>
  );
};

export default ContractorsPage;
