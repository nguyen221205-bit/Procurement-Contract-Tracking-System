import React, { useState, useEffect } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import {
  ArrowLeft,
  FileText,
  Building2,
  Calendar,
  Clock,
  DollarSign,
  CheckCircle,
  AlertCircle,
  FileCheck,
  Plus,
  Edit3,
  Trash2,
  Upload,
  ExternalLink,
  ShieldCheck,
  PlayCircle,
  Ban,
  RefreshCw,
  Layers,
  ChevronRight,
  TrendingUp,
  Download,
  AlertTriangle
} from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { contractApi } from '../api/contractApi';
import { downloadSecureFile } from '../utils/fileDownload';
import StatusBadge from '../components/common/StatusBadge';
import LoadingSpinner from '../components/common/LoadingSpinner';
import MilestoneModal from '../components/contracts/MilestoneModal';
import ApproveMilestoneModal from '../components/contracts/ApproveMilestoneModal';
import UploadScannedModal from '../components/contracts/UploadScannedModal';
import ChangeStatusModal from '../components/contracts/ChangeStatusModal';
import { formatCurrency, formatDate, formatPercent, formatNumber } from '../utils/formatters';

export const ContractDetailPage = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { hasRole, user } = useAuth();
  const isAdminOrProcurement = hasRole(['Admin', 'Procurement']);

  const [contract, setContract] = useState(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);

  // Modal states
  const [isMilestoneModalOpen, setIsMilestoneModalOpen] = useState(false);
  const [milestoneToEdit, setMilestoneToEdit] = useState(null);

  const [isApproveModalOpen, setIsApproveModalOpen] = useState(false);
  const [selectedMilestoneToApprove, setSelectedMilestoneToApprove] = useState(null);

  const [isUploadModalOpen, setIsUploadModalOpen] = useState(false);
  const [isChangeStatusModalOpen, setIsChangeStatusModalOpen] = useState(false);
  const [targetStatus, setTargetStatus] = useState(null);

  // Fetch contract data
  const fetchData = async (isBackground = false) => {
    if (!isBackground) setLoading(true);
    else setRefreshing(true);
    setError(null);

    try {
      const res = await contractApi.getContractById(id);
      if (res?.data) {
        setContract(res.data);
      } else {
        setError('Không tìm thấy thông tin hợp đồng.');
      }
    } catch (err) {
      console.error('Lỗi khi tải thông tin hợp đồng:', err);
      setError(err.message || 'Không thể tải thông tin hợp đồng.');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    if (id) {
      fetchData();
    }
  }, [id]);

  // Handle milestone delete
  const handleDeleteMilestone = async (milestoneId) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa mốc thanh toán này khỏi hợp đồng?')) {
      return;
    }

    try {
      await contractApi.deleteMilestone(id, milestoneId);
      toast.success('Xóa mốc thanh toán thành công!');
      fetchData(true);
    } catch (err) {
      toast.error(err.message || 'Không thể xóa mốc thanh toán.');
    }
  };

  if (loading) {
    return (
      <div className="py-24 flex items-center justify-center">
        <LoadingSpinner size="lg" message="Đang tải dữ liệu chi tiết hợp đồng..." />
      </div>
    );
  }

  if (error || !contract) {
    return (
      <div className="bg-white rounded-2xl border border-rose-200 p-8 text-center max-w-lg mx-auto space-y-4 my-10">
        <AlertCircle className="w-12 h-12 text-rose-500 mx-auto" />
        <h3 className="text-base font-bold text-slate-800">Không tìm thấy hợp đồng</h3>
        <p className="text-xs text-slate-500">{error || 'Hợp đồng không tồn tại hoặc bạn không có quyền truy cập.'}</p>
        <Link
          to="/contracts"
          className="inline-flex items-center gap-2 px-4 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold rounded-xl transition-colors"
        >
          <ArrowLeft className="w-4 h-4" /> Quay lại danh sách hợp đồng
        </Link>
      </div>
    );
  }

  const milestones = contract.milestones || [];
  const sumMilestones = milestones.reduce((sum, m) => sum + (m.amount || 0), 0);
  const remainingBudgetToAllocate = Math.max(0, contract.value - sumMilestones);
  const isBudgetFullyAllocated = sumMilestones === contract.value;
  const isDraft = contract.status === 'Draft' || contract.status === 0;
  const isActive = contract.status === 'Active' || contract.status === 1;
  const isCompleted = contract.status === 'Completed' || contract.status === 2;
  const isTerminated = contract.status === 'Terminated' || contract.status === 3;
  const allMilestonesCompleted =
    milestones.length > 0 &&
    milestones.every((m) => m.status === 'Completed' || m.status === 2) &&
    contract.totalRemainingAmount === 0;

  return (
    <div className="space-y-6 pb-12">
      {/* HEADER SECTION */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div className="space-y-1.5">
          <div className="flex items-center gap-2">
            <Link
              to="/contracts"
              className="text-xs font-bold text-slate-500 hover:text-blue-600 flex items-center gap-1 transition-colors"
            >
              <ArrowLeft className="w-3.5 h-3.5" />
              <span>Quản lý Hợp đồng</span>
            </Link>
            <span className="text-slate-300">/</span>
            <span className="text-xs font-bold text-slate-700">{contract.contractNumber}</span>
          </div>

          <div className="flex items-center gap-3 flex-wrap">
            <h1 className="text-xl md:text-2xl font-black text-slate-900 tracking-tight">
              {contract.contractNumber}
            </h1>
            <StatusBadge status={contract.status} type="contract" />
          </div>

          <div className="flex items-center gap-2 text-xs text-slate-500">
            <span>Gói thầu liên kết:</span>
            <Link
              to={`/packages/${contract.bidPackageId}`}
              className="font-bold text-blue-600 hover:text-blue-800 hover:underline flex items-center gap-1"
            >
              <span>[{contract.bidPackageCode}] {contract.bidPackageName}</span>
              <ExternalLink className="w-3 h-3" />
            </Link>
          </div>
        </div>

        {/* Action Buttons */}
        <div className="flex items-center gap-2.5 flex-wrap">
          <button
            type="button"
            onClick={() => fetchData(true)}
            disabled={refreshing}
            className="p-2.5 bg-white border border-slate-200 hover:border-slate-300 rounded-xl text-slate-600 hover:text-slate-900 shadow-2xs transition-colors cursor-pointer"
            title="Làm mới dữ liệu"
          >
            <RefreshCw className={`w-4 h-4 ${refreshing ? 'animate-spin text-blue-600' : ''}`} />
          </button>

          {isAdminOrProcurement && (
            <>
              {/* Kích hoạt hợp đồng (Draft -> Active) */}
              {isDraft && (
                <button
                  type="button"
                  onClick={() => {
                    setTargetStatus('Active');
                    setIsChangeStatusModalOpen(true);
                  }}
                  className="inline-flex items-center gap-2 px-4 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-xl shadow-xs transition-transform hover:scale-[1.02] cursor-pointer"
                >
                  <PlayCircle className="w-4 h-4" />
                  <span>Kích Hoạt Hợp Đồng</span>
                </button>
              )}

              {/* Đính kèm bản scan PDF */}
              <button
                type="button"
                onClick={() => setIsUploadModalOpen(true)}
                className="inline-flex items-center gap-1.5 px-3.5 py-2.5 bg-white hover:bg-slate-50 text-slate-700 border border-slate-300 text-xs font-bold rounded-xl shadow-2xs transition-colors cursor-pointer"
              >
                <Upload className="w-3.5 h-3.5 text-blue-600" />
                <span>{contract.scannedFilePath ? 'Cập nhật bản Scan PDF' : 'Tải lên bản Scan PDF'}</span>
              </button>

              {/* Hoàn tất hợp đồng nếu đủ điều kiện */}
              {isActive && allMilestonesCompleted && (
                <button
                  type="button"
                  onClick={() => {
                    setTargetStatus('Completed');
                    setIsChangeStatusModalOpen(true);
                  }}
                  className="inline-flex items-center gap-2 px-4 py-2.5 bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold rounded-xl shadow-xs transition-transform hover:scale-[1.02] cursor-pointer"
                >
                  <CheckCircle className="w-4 h-4" />
                  <span>Nghiệm Thu Hoàn Thành HĐ</span>
                </button>
              )}

              {/* Chấm dứt hợp đồng nếu cần */}
              {isActive && (
                <button
                  type="button"
                  onClick={() => {
                    setTargetStatus('Terminated');
                    setIsChangeStatusModalOpen(true);
                  }}
                  className="p-2.5 bg-white hover:bg-rose-50 text-slate-400 hover:text-rose-600 border border-slate-200 hover:border-rose-200 rounded-xl transition-colors cursor-pointer"
                  title="Chấm dứt hợp đồng trước hạn"
                >
                  <Ban className="w-4 h-4" />
                </button>
              )}

              {/* Kích hoạt lại HĐ nếu đang Completed (Trường hợp cần bổ sung mốc giải ngân) */}
              {isCompleted && (
                <button
                  type="button"
                  onClick={() => {
                    setTargetStatus('Active');
                    setIsChangeStatusModalOpen(true);
                  }}
                  className="inline-flex items-center gap-1.5 px-3.5 py-2.5 bg-amber-50 hover:bg-amber-100 text-amber-800 border border-amber-200 text-xs font-bold rounded-xl transition-colors cursor-pointer"
                  title="Mở lại hợp đồng về trạng thái Active"
                >
                  <RefreshCw className="w-3.5 h-3.5" />
                  <span>Kích hoạt lại HĐ (Active)</span>
                </button>
              )}
            </>
          )}
        </div>
      </div>

      {/* 2-COLUMN MAIN CONTENT */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
        {/* LEFT COLUMN: Legal Info, Financial & Files (5 cols) */}
        <div className="lg:col-span-5 space-y-5">
          {/* Card 1: Tiến độ Giải ngân & Tài chính */}
          <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-2xs space-y-4">
            <div className="flex items-center justify-between border-b border-slate-100 pb-3">
              <h3 className="text-xs font-bold text-slate-800 uppercase tracking-wider flex items-center gap-2">
                <DollarSign className="w-4 h-4 text-emerald-600" />
                <span>Tiến Độ Giải Ngân & Tài Chính</span>
              </h3>
              <span className="text-xs font-black text-emerald-600 bg-emerald-50 px-2.5 py-0.5 rounded-full">
                {formatPercent(contract.disbursementRate)}
              </span>
            </div>

            {/* Progress bar */}
            <div className="space-y-1.5">
              <div className="w-full bg-slate-100 h-2.5 rounded-full overflow-hidden">
                <div
                  className="h-full bg-gradient-to-r from-emerald-500 to-teal-600 rounded-full transition-all duration-700"
                  style={{ width: `${Math.min(contract.disbursementRate || 0, 100)}%` }}
                />
              </div>
              <div className="flex justify-between text-[11px] text-slate-500">
                <span>0 đ</span>
                <span>{formatCurrency(contract.value)}</span>
              </div>
            </div>

            {/* Financial Stats */}
            <div className="grid grid-cols-2 gap-3 pt-2">
              <div className="p-3 bg-slate-50 rounded-xl space-y-0.5">
                <span className="text-[11px] text-slate-500 block">Tổng giá trị hợp đồng:</span>
                <p className="text-sm font-black text-slate-900 truncate">
                  {formatCurrency(contract.value)}
                </p>
              </div>

              <div className="p-3 bg-emerald-50/70 border border-emerald-100 rounded-xl space-y-0.5">
                <span className="text-[11px] text-emerald-700 font-semibold block">Đã giải ngân:</span>
                <p className="text-sm font-black text-emerald-700 truncate">
                  {formatCurrency(contract.totalDisbursedAmount)}
                </p>
              </div>

              <div className="p-3 bg-amber-50/70 border border-amber-100 rounded-xl space-y-0.5 col-span-2">
                <span className="text-[11px] text-amber-800 font-semibold block">Số tiền còn lại chưa giải ngân:</span>
                <p className="text-sm font-black text-amber-900 truncate">
                  {formatCurrency(contract.totalRemainingAmount)}
                </p>
              </div>
            </div>
          </div>

          {/* Card 2: Thông tin Hai Bên Ký kết */}
          <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-2xs space-y-3.5">
            <h3 className="text-xs font-bold text-slate-800 uppercase tracking-wider flex items-center gap-2 border-b border-slate-100 pb-3">
              <Building2 className="w-4 h-4 text-blue-600" />
              <span>Chủ Thể Tham Gia Hợp Đồng</span>
            </h3>

            {/* Bên A */}
            <div className="p-3.5 bg-slate-50 rounded-xl space-y-1 text-xs">
              <span className="text-[10px] font-bold text-blue-700 uppercase tracking-wider block">
                Bên A (Chủ đầu tư / Bên mời thầu)
              </span>
              <p className="font-bold text-slate-900">BAN QUẢN LÝ DỰ ÁN & MUA SẮM DOANH NGHIỆP</p>
              <p className="text-slate-500 text-[11px]">Đại diện pháp lý theo thẩm quyền được phê duyệt</p>
            </div>

            {/* Bên B */}
            <div className="p-3.5 bg-emerald-50/50 border border-emerald-100 rounded-xl space-y-1 text-xs">
              <span className="text-[10px] font-bold text-emerald-700 uppercase tracking-wider block">
                Bên B (Đơn vị trúng thầu)
              </span>
              <p className="font-bold text-slate-900">{contract.companyName}</p>
              <p className="text-slate-600 text-[11px]">Mã số thuế: <strong>{contract.taxCode || '0300588569'}</strong></p>
            </div>
          </div>

          {/* Card 3: Thời hạn & Điều khoản Hợp đồng */}
          <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-2xs space-y-3.5">
            <h3 className="text-xs font-bold text-slate-800 uppercase tracking-wider flex items-center gap-2 border-b border-slate-100 pb-3">
              <Calendar className="w-4 h-4 text-indigo-600" />
              <span>Thời Hạn & Điều Khoản</span>
            </h3>

            <div className="grid grid-cols-2 gap-3 text-xs">
              <div className="space-y-0.5">
                <span className="text-slate-500 text-[11px] block">Ngày bắt đầu hiệu lực:</span>
                <strong className="text-slate-800">{formatDate(contract.startDate)}</strong>
              </div>
              <div className="space-y-0.5">
                <span className="text-slate-500 text-[11px] block">Ngày kết thúc hiệu lực:</span>
                <strong className="text-slate-800">{formatDate(contract.endDate)}</strong>
              </div>
            </div>

            {contract.terms && (
              <div className="pt-2 border-t border-slate-100 space-y-1">
                <span className="text-[11px] font-bold text-slate-700 block">Nội dung điều khoản & cam kết:</span>
                <div className="p-3 bg-slate-50 rounded-xl text-xs text-slate-700 leading-relaxed whitespace-pre-line border border-slate-200/60 max-h-48 overflow-y-auto">
                  {contract.terms}
                </div>
              </div>
            )}
          </div>

          {/* Card 4: Bản scan Hợp đồng đã ký */}
          <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-2xs space-y-3">
            <div className="flex items-center justify-between border-b border-slate-100 pb-3">
              <h3 className="text-xs font-bold text-slate-800 uppercase tracking-wider flex items-center gap-2">
                <FileCheck className="w-4 h-4 text-blue-600" />
                <span>Bản Scan Hợp Đồng Đã Ký</span>
              </h3>
              {contract.scannedFilePath && (
                <span className="text-[10px] font-bold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-full">
                  Đã đính kèm
                </span>
              )}
            </div>

            {contract.scannedFilePath ? (
              <div className="p-4 bg-blue-50/50 border border-blue-200 rounded-xl flex items-center justify-between gap-3">
                <div className="flex items-center gap-3">
                  <div className="p-2.5 bg-blue-600 text-white rounded-xl shadow-2xs">
                    <FileText className="w-5 h-5" />
                  </div>
                  <div>
                    <p className="text-xs font-bold text-slate-800">Tài liệu hợp đồng có chữ ký scan</p>
                    <p className="text-[11px] text-slate-500">Định dạng file đính kèm hợp lệ</p>
                  </div>
                </div>

                <button
                  type="button"
                  onClick={() => downloadSecureFile(`/api/contracts/${contract.id}/scanned-file/download`, `HopDong_${contract.contractNumber}.pdf`)}
                  className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-white hover:bg-blue-50 text-blue-600 hover:text-blue-800 border border-blue-200 text-xs font-bold rounded-xl transition-colors cursor-pointer shadow-2xs"
                >
                  <Download className="w-3.5 h-3.5" />
                  <span>Tải file scan</span>
                </button>
              </div>
            ) : (
              <div className="p-5 border-2 border-dashed border-slate-200 rounded-xl text-center space-y-2">
                <FileText className="w-8 h-8 text-slate-300 mx-auto" />
                <p className="text-xs text-slate-500">Chưa có bản scan PDF hợp đồng có chữ ký</p>
                {isAdminOrProcurement && (
                  <button
                    type="button"
                    onClick={() => setIsUploadModalOpen(true)}
                    className="inline-flex items-center gap-1.5 px-3.5 py-1.5 bg-blue-50 hover:bg-blue-100 text-blue-700 text-xs font-bold rounded-xl transition-colors cursor-pointer"
                  >
                    <Upload className="w-3.5 h-3.5" />
                    <span>Tải lên ngay</span>
                  </button>
                )}
              </div>
            )}
          </div>
        </div>

        {/* RIGHT COLUMN: Milestones & Acceptance Management (7 cols) */}
        <div className="lg:col-span-7 space-y-5">
          <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-2xs space-y-4">
            {/* Header & Budget Allocation Status */}
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-100 pb-3">
              <div>
                <h3 className="text-sm font-bold text-slate-900 flex items-center gap-2">
                  <Clock className="w-4 h-4 text-blue-600" />
                  <span>Quản Lý Mốc Nghiệm Thu & Giải Ngân</span>
                </h3>
                <p className="text-xs text-slate-500">
                  {milestones.length} mốc thanh toán nghiệm thu đã thiết lập
                </p>
              </div>

              {isAdminOrProcurement && (
                <button
                  type="button"
                  onClick={() => {
                    setMilestoneToEdit(null);
                    setIsMilestoneModalOpen(true);
                  }}
                  disabled={isBudgetFullyAllocated && milestones.length > 0}
                  className="inline-flex items-center gap-1.5 px-3.5 py-2 bg-blue-600 hover:bg-blue-700 disabled:bg-slate-200 disabled:text-slate-400 disabled:cursor-not-allowed text-white text-xs font-bold rounded-xl shadow-xs transition-transform hover:scale-[1.02] cursor-pointer"
                >
                  <Plus className="w-3.5 h-3.5" />
                  <span>Thêm Mốc Nghiệm Thu</span>
                </button>
              )}
            </div>

            {/* Phân bổ ngân sách mốc vs Tổng hợp đồng */}
            <div className="p-3.5 bg-slate-50 border border-slate-200 rounded-xl space-y-2 text-xs">
              <div className="flex justify-between items-center font-bold">
                <span className="text-slate-700">Tình trạng phân bổ ngân sách các mốc:</span>
                {isBudgetFullyAllocated ? (
                  <span className="text-emerald-700 bg-emerald-100 px-2 py-0.5 rounded-full text-[11px] font-black flex items-center gap-1">
                    <CheckCircle className="w-3 h-3" />
                    Đã phân bổ đủ 100%
                  </span>
                ) : (
                  <span className="text-amber-800 bg-amber-100 px-2 py-0.5 rounded-full text-[11px] font-black">
                    Còn lại {formatCurrency(remainingBudgetToAllocate)} chưa chia mốc
                  </span>
                )}
              </div>

              <div className="w-full bg-slate-200 h-2 rounded-full overflow-hidden">
                <div
                  className={`h-full rounded-full transition-all duration-500 ${
                    isBudgetFullyAllocated ? 'bg-emerald-500' : 'bg-amber-500'
                  }`}
                  style={{
                    width: `${Math.min(
                      contract.value > 0 ? (sumMilestones / contract.value) * 100 : 0,
                      100
                    )}%`,
                  }}
                />
              </div>

              <div className="flex justify-between text-[11px] text-slate-500 pt-0.5">
                <span>Tổng các mốc: <strong>{formatCurrency(sumMilestones)}</strong></span>
                <span>Giá trị HĐ: <strong>{formatCurrency(contract.value)}</strong></span>
              </div>
            </div>

            {/* Danh sách mốc */}
            {milestones.length === 0 ? (
              <div className="py-12 border-2 border-dashed border-slate-200 rounded-2xl text-center space-y-3">
                <Clock className="w-10 h-10 text-slate-300 mx-auto" />
                <h4 className="text-xs font-bold text-slate-700">Chưa có mốc thanh toán nào</h4>
                <p className="text-xs text-slate-400 max-w-xs mx-auto">
                  Hợp đồng cần được phân bổ các mốc hoàn thành để tiến hành nghiệm thu và giải ngân tiền.
                </p>
                {isAdminOrProcurement && (
                  <button
                    type="button"
                    onClick={() => {
                      setMilestoneToEdit(null);
                      setIsMilestoneModalOpen(true);
                    }}
                    className="inline-flex items-center gap-1.5 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold rounded-xl shadow-xs transition-colors cursor-pointer"
                  >
                    <Plus className="w-4 h-4" />
                    <span>Thêm mốc đầu tiên</span>
                  </button>
                )}
              </div>
            ) : (
              <div className="space-y-3 pt-1">
                {milestones.map((m, index) => {
                  const mPercent =
                    contract.value > 0
                      ? ((m.amount / contract.value) * 100).toFixed(1)
                      : 0;

                  const isMilestoneCompleted =
                    m.status === 'Completed' || m.status === 2;
                  const isMilestonePending =
                    m.status === 'Pending' || m.status === 0;

                  return (
                    <div
                      key={m.id}
                      className={`p-4 rounded-2xl border transition-all ${
                        isMilestoneCompleted
                          ? 'bg-emerald-50/30 border-emerald-200 shadow-2xs'
                          : 'bg-white border-slate-200 hover:border-slate-300 shadow-2xs'
                      }`}
                    >
                      <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
                        {/* Index & Title */}
                        <div className="flex items-start gap-3 flex-1">
                          <div
                            className={`w-7 h-7 rounded-xl flex items-center justify-center font-black text-xs flex-shrink-0 shadow-2xs ${
                              isMilestoneCompleted
                                ? 'bg-emerald-600 text-white'
                                : 'bg-slate-100 text-slate-700 border border-slate-200'
                            }`}
                          >
                            {index + 1}
                          </div>

                          <div className="space-y-1">
                            <h4 className="text-xs font-bold text-slate-900 leading-snug">
                              {m.title}
                            </h4>
                            <div className="flex items-center gap-3 text-[11px] text-slate-500 flex-wrap">
                              <span className="flex items-center gap-1">
                                <Calendar className="w-3.5 h-3.5 text-slate-400" />
                                Hạn nghiệm thu: <strong className="text-slate-700">{formatDate(m.dueDate)}</strong>
                              </span>
                              <span className="text-slate-300">•</span>
                              <span className="text-blue-600 font-semibold">
                                Tỷ lệ: {mPercent}% hợp đồng
                              </span>
                            </div>
                          </div>
                        </div>

                        {/* Amount & Status Badge */}
                        <div className="flex sm:flex-col items-center sm:items-end justify-between sm:justify-start gap-1 flex-shrink-0">
                          <span className="text-sm font-black text-slate-900">
                            {formatCurrency(m.amount)}
                          </span>
                          <StatusBadge status={m.status} type="milestone" />
                        </div>
                      </div>

                      {/* Bottom action controls */}
                      <div className="mt-3 pt-3 border-t border-slate-100 flex items-center justify-between gap-2">
                        {/* Status message */}
                        <div className="text-[11px]">
                          {isMilestoneCompleted ? (
                            <span className="text-emerald-700 font-semibold flex items-center gap-1">
                              <CheckCircle className="w-3.5 h-3.5" />
                              Đã nghiệm thu đạt yêu cầu & kích hoạt giải ngân
                            </span>
                          ) : (
                            <span className="text-slate-400">
                              Chờ kiểm tra biên bản nghiệm thu
                            </span>
                          )}
                        </div>

                        {/* Action buttons */}
                        <div className="flex items-center gap-1.5">
                          {/* Nút Nghiệm thu mốc */}
                          {!isMilestoneCompleted && isAdminOrProcurement && (
                            <button
                              type="button"
                              onClick={() => {
                                setSelectedMilestoneToApprove(m);
                                setIsApproveModalOpen(true);
                              }}
                              className="inline-flex items-center gap-1 px-3 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-xl shadow-2xs transition-colors cursor-pointer"
                            >
                              <FileCheck className="w-3.5 h-3.5" />
                              <span>Nghiệm thu mốc</span>
                            </button>
                          )}

                          {/* Sửa mốc (chỉ khi Pending) */}
                          {isMilestonePending && isAdminOrProcurement && (
                            <button
                              type="button"
                              onClick={() => {
                                setMilestoneToEdit(m);
                                setIsMilestoneModalOpen(true);
                              }}
                              className="p-1.5 hover:bg-slate-100 text-slate-500 hover:text-slate-800 rounded-lg transition-colors cursor-pointer"
                              title="Chỉnh sửa mốc"
                            >
                              <Edit3 className="w-3.5 h-3.5" />
                            </button>
                          )}

                          {/* Xóa mốc (chỉ khi Pending) */}
                          {isMilestonePending && isAdminOrProcurement && (
                            <button
                              type="button"
                              onClick={() => handleDeleteMilestone(m.id)}
                              className="p-1.5 hover:bg-rose-50 text-slate-400 hover:text-rose-600 rounded-lg transition-colors cursor-pointer"
                              title="Xóa mốc"
                            >
                              <Trash2 className="w-3.5 h-3.5" />
                            </button>
                          )}
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* MODALS */}
      {/* 1. Modal Thêm/Sửa Mốc */}
      <MilestoneModal
        isOpen={isMilestoneModalOpen}
        onClose={() => setIsMilestoneModalOpen(false)}
        contractId={Number(id)}
        contractValue={contract.value}
        existingMilestones={milestones}
        milestoneToEdit={milestoneToEdit}
        onSuccess={() => fetchData(true)}
      />

      {/* 2. Modal Phê duyệt Nghiệm thu */}
      <ApproveMilestoneModal
        isOpen={isApproveModalOpen}
        onClose={() => setIsApproveModalOpen(false)}
        milestone={selectedMilestoneToApprove}
        onSuccess={() => fetchData(true)}
      />

      {/* 3. Modal Tải lên bản Scan PDF */}
      <UploadScannedModal
        isOpen={isUploadModalOpen}
        onClose={() => setIsUploadModalOpen(false)}
        contractId={Number(id)}
        contractNumber={contract.contractNumber}
        onSuccess={() => fetchData(true)}
      />

      {/* 4. Modal Thay đổi trạng thái HĐ (Active / Completed / Terminated) */}
      <ChangeStatusModal
        isOpen={isChangeStatusModalOpen}
        onClose={() => setIsChangeStatusModalOpen(false)}
        contractId={Number(id)}
        contractNumber={contract.contractNumber}
        targetStatus={targetStatus}
        onSuccess={() => fetchData(true)}
      />
    </div>
  );
};

export default ContractDetailPage;
