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
  AlertTriangle,
  Award,
  Sparkles,
} from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { contractApi } from '../api/contractApi';
import { downloadSecureFile, previewSecureFile } from '../utils/fileDownload';
import StatusBadge from '../components/common/StatusBadge';
import LoadingSpinner from '../components/common/LoadingSpinner';
import MilestoneModal from '../components/contracts/MilestoneModal';
import ApproveMilestoneModal from '../components/contracts/ApproveMilestoneModal';
import UploadScannedModal from '../components/contracts/UploadScannedModal';
import ChangeStatusModal from '../components/contracts/ChangeStatusModal';

// Nhóm B Components & Modals
import AddGuaranteeModal from '../components/contracts/AddGuaranteeModal';
import AddAddendumModal from '../components/contracts/AddAddendumModal';
import ContractorEvaluationModal from '../components/contracts/ContractorEvaluationModal';
import ContractGuaranteeTab from '../components/contracts/ContractGuaranteeTab';
import ContractAddendumTab from '../components/contracts/ContractAddendumTab';
import ContractorEvaluationTab from '../components/contracts/ContractorEvaluationTab';

import { formatCurrency, formatDate, formatPercent, formatNumber } from '../utils/formatters';

export const ContractDetailPage = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { hasRole, user } = useAuth();
  const isAdminOrProcurement = hasRole(['Admin', 'Procurement']);
  const isAdmin = hasRole('Admin');

  const [contract, setContract] = useState(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);

  // Active Tab: 'milestones' | 'guarantees' | 'addenda' | 'evaluation'
  const [activeTab, setActiveTab] = useState('milestones');

  // Modal states hiện có
  const [isMilestoneModalOpen, setIsMilestoneModalOpen] = useState(false);
  const [milestoneToEdit, setMilestoneToEdit] = useState(null);

  const [isApproveModalOpen, setIsApproveModalOpen] = useState(false);
  const [selectedMilestoneToApprove, setSelectedMilestoneToApprove] = useState(null);

  const [isUploadModalOpen, setIsUploadModalOpen] = useState(false);
  const [isChangeStatusModalOpen, setIsChangeStatusModalOpen] = useState(false);
  const [targetStatus, setTargetStatus] = useState(null);

  // Nhóm B Modal states
  const [isGuaranteeModalOpen, setIsGuaranteeModalOpen] = useState(false);
  const [isAddendumModalOpen, setIsAddendumModalOpen] = useState(false);
  const [isEvaluationModalOpen, setIsEvaluationModalOpen] = useState(false);

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
      console.error('Lỗi khi xóa mốc thanh toán:', err);
      toast.error(err.response?.data?.message || err.message || 'Không thể xóa mốc thanh toán');
    }
  };

  if (loading) {
    return (
      <div className="py-24 flex items-center justify-center">
        <LoadingSpinner text="Đang tải dữ liệu hợp đồng kinh tế..." />
      </div>
    );
  }

  if (error || !contract) {
    return (
      <div className="max-w-4xl mx-auto py-12 px-4 space-y-4">
        <div className="p-6 bg-rose-50 border border-rose-200 rounded-2xl flex items-start gap-4">
          <AlertCircle className="w-6 h-6 text-rose-600 flex-shrink-0 mt-0.5" />
          <div className="space-y-1">
            <h3 className="text-sm font-bold text-rose-900">Không tìm thấy hợp đồng</h3>
            <p className="text-xs text-rose-700">{error || 'Hợp đồng không tồn tại hoặc bạn không có quyền xem.'}</p>
          </div>
        </div>
        <Link
          to="/contracts"
          className="inline-flex items-center gap-2 text-xs font-bold text-blue-600 hover:text-blue-700"
        >
          <ArrowLeft className="w-4 h-4" />
          <span>Quay lại danh sách hợp đồng</span>
        </Link>
      </div>
    );
  }

  const milestones = contract.milestones || [];
  const sumMilestones = milestones.reduce((sum, m) => sum + (m.amount || 0), 0);
  const remainingBudgetToAllocate = contract.value - sumMilestones;
  const isBudgetFullyAllocated = Math.abs(remainingBudgetToAllocate) < 1;

  const isContractFinished = contract.status === 'Completed' || contract.status === 'Terminated' || contract.status === 2 || contract.status === 3;
  const hasNearExpiryGuarantees = contract.guarantees?.some((g) => g.isNearExpiry);
  const needsEvaluation = (contract.status === 'Completed' || contract.status === 2) && !contract.evaluation;

  return (
    <div className="space-y-6">
      {/* BREADCRUMB & BACK BUTTON */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div className="flex items-center gap-2 text-xs text-slate-500">
          <Link to="/contracts" className="hover:text-blue-600 transition-colors">
            Hợp đồng kinh tế
          </Link>
          <ChevronRight className="w-3.5 h-3.5 text-slate-400" />
          <span className="font-semibold text-slate-800">
            {contract.contractNumber}
          </span>
        </div>

        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => fetchData(true)}
            disabled={refreshing}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-white border border-slate-200 hover:bg-slate-50 text-slate-700 text-xs font-bold rounded-xl shadow-2xs transition-colors cursor-pointer"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${refreshing ? 'animate-spin text-blue-600' : ''}`} />
            <span>{refreshing ? 'Đang làm mới...' : 'Làm mới'}</span>
          </button>

          <Link
            to="/contracts"
            className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold rounded-xl transition-colors"
          >
            <ArrowLeft className="w-3.5 h-3.5" />
            <span>Danh sách</span>
          </Link>
        </div>
      </div>

      {/* SMART NOTIFICATION BANNERS */}
      {/* Banner 1: Nhắc nhở Đánh giá Nghiệm thu Nhà thầu */}
      {needsEvaluation && (
        <div className="p-4 bg-gradient-to-r from-amber-50 to-orange-50 border border-amber-200 rounded-2xl flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-xs">
          <div className="flex items-center gap-3">
            <div className="p-2.5 bg-amber-500 text-white rounded-xl shadow-xs">
              <Award className="w-5 h-5" />
            </div>
            <div>
              <h4 className="text-xs font-black text-amber-950 uppercase tracking-wider">
                Hợp đồng đã hoàn thành nghiệm thu
              </h4>
              <p className="text-xs text-amber-800">
                Vui lòng thực hiện đánh giá nghiệm thu 3 trụ cột (Chất lượng, Tiến độ, An toàn) để cập nhật Hồ sơ Uy tín Nhà thầu.
              </p>
            </div>
          </div>
          {isAdminOrProcurement && (
            <button
              type="button"
              onClick={() => setIsEvaluationModalOpen(true)}
              className="inline-flex items-center gap-1.5 px-4 py-2 bg-gradient-to-r from-amber-600 to-orange-600 hover:from-amber-700 hover:to-orange-700 text-white text-xs font-bold rounded-xl shadow-xs transition-colors shrink-0 cursor-pointer"
            >
              <Sparkles className="w-4 h-4" />
              <span>Đánh Giá Nghiệm Thu Ngay</span>
            </button>
          )}
        </div>
      )}

      {/* Banner 2: Cảnh báo Bảo lãnh sắp hết hạn */}
      {hasNearExpiryGuarantees && (
        <div className="p-4 bg-rose-50 border border-rose-200 rounded-2xl flex items-center justify-between gap-3">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-rose-600 text-white rounded-xl shadow-xs">
              <AlertTriangle className="w-5 h-5" />
            </div>
            <div className="text-xs space-y-0.5">
              <h4 className="font-bold text-rose-950 uppercase">
                Cảnh báo: Có chứng thư bảo lãnh sắp hết hạn trong 30 ngày
              </h4>
              <p className="text-rose-800">
                Theo Điều 68 Luật Đấu thầu 2023, Bên mời thầu cần yêu cầu nhà thầu nộp văn bản gia hạn chứng thư bảo lãnh để duy trì hiệu lực hợp đồng.
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={() => setActiveTab('guarantees')}
            className="px-3 py-1.5 bg-rose-100 hover:bg-rose-200 text-rose-900 text-xs font-bold rounded-lg border border-rose-300 transition-colors shrink-0 cursor-pointer"
          >
            Xem bảo lãnh
          </button>
        </div>
      )}

      {/* MAIN HEADER CARD */}
      <div className="bg-white rounded-2xl border border-slate-200 p-6 shadow-2xs space-y-5">
        <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
          <div className="space-y-2">
            <div className="flex flex-wrap items-center gap-2.5">
              <h1 className="text-xl font-black text-slate-900 tracking-tight flex items-center gap-2">
                <FileText className="w-6 h-6 text-blue-600" />
                <span>Hợp Đồng: {contract.contractNumber}</span>
              </h1>
              <StatusBadge status={contract.statusName || contract.status} type="contract" />
              {contract.evaluation && (
                <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-bold bg-amber-50 text-amber-800 border border-amber-300">
                  <Award className="w-3.5 h-3.5 text-amber-600" />
                  <span>Xếp Hạng {contract.evaluation.ratingGrade} ({contract.evaluation.totalScore}đ)</span>
                </span>
              )}
            </div>

            <p className="text-xs text-slate-600 flex flex-wrap items-center gap-2">
              <span>Gói thầu:</span>
              <strong className="text-blue-700 font-semibold">{contract.bidPackageName}</strong>
              <span className="text-slate-300">•</span>
              <span className="font-mono text-slate-500">Mã: {contract.bidPackageCode}</span>
            </p>
          </div>

          {/* Action buttons (Admin / Procurement) */}
          <div className="flex flex-wrap items-center gap-2.5">
            {isAdminOrProcurement && (
              <>
                {/* Đổi trạng thái: Chuyển sang Active */}
                {(contract.status === 'Draft' || contract.status === 0) && (
                  <button
                    type="button"
                    onClick={() => {
                      setTargetStatus('Active');
                      setIsChangeStatusModalOpen(true);
                    }}
                    className="inline-flex items-center gap-1.5 px-4 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-xl shadow-xs transition-colors cursor-pointer"
                    title="Kích hoạt hợp đồng có hiệu lực thi công"
                  >
                    <PlayCircle className="w-4 h-4" />
                    <span>Có Hiệu Lực (Active)</span>
                  </button>
                )}

                {/* Đổi trạng thái: Nghiệm thu hoàn thành */}
                {(contract.status === 'Active' || contract.status === 1) && (
                  <button
                    type="button"
                    onClick={() => {
                      setTargetStatus('Completed');
                      setIsChangeStatusModalOpen(true);
                    }}
                    className="inline-flex items-center gap-1.5 px-4 py-2.5 bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold rounded-xl shadow-xs transition-colors cursor-pointer"
                    title="Nghiệm thu hoàn tất toàn bộ hợp đồng"
                  >
                    <CheckCircle className="w-4 h-4" />
                    <span>Nghiệm Thu Hoàn Thành (Completed)</span>
                  </button>
                )}

                {/* Đổi trạng thái: Chấm dứt hợp đồng (Chỉ cho phép khi đang thi công Active) */}
                {(contract.status === 'Active' || contract.status === 1) && (
                  <button
                    type="button"
                    onClick={() => {
                      setTargetStatus('Terminated');
                      setIsChangeStatusModalOpen(true);
                    }}
                    className="inline-flex items-center gap-1.5 px-3 py-2.5 bg-rose-50 hover:bg-rose-100 text-rose-700 border border-rose-200 text-xs font-bold rounded-xl transition-colors cursor-pointer"
                    title="Chấm dứt hợp đồng do vi phạm hoặc điều kiện bất khả kháng (Yêu cầu căn cứ pháp lý)"
                  >
                    <Ban className="w-3.5 h-3.5" />
                    <span>Chấm dứt HĐ</span>
                  </button>
                )}

                {/* Khôi phục Active nếu lỡ Terminated - Chỉ dành riêng cho Admin */}
                {(contract.status === 'Terminated' || contract.status === 3) && isAdmin && (
                  <button
                    type="button"
                    onClick={() => {
                      setTargetStatus('Active');
                      setIsChangeStatusModalOpen(true);
                    }}
                    className="inline-flex items-center gap-1.5 px-3.5 py-2.5 bg-amber-50 hover:bg-amber-100 text-amber-800 border border-amber-200 text-xs font-bold rounded-xl transition-colors cursor-pointer"
                    title="Khôi phục hợp đồng về trạng thái Active (Yêu cầu giải trình căn cứ pháp lý)"
                  >
                    <RefreshCw className="w-3.5 h-3.5" />
                    <span>Khôi phục hiệu lực HĐ (Admin)</span>
                  </button>
                )}
              </>
            )}
          </div>
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
                {contract.totalAdjustedValue !== 0 && (
                  <span className="text-[10px] text-indigo-600 font-semibold block">
                    (Gồm {contract.totalAdjustedValue > 0 ? `+${formatCurrency(contract.totalAdjustedValue)}` : formatCurrency(contract.totalAdjustedValue)} phụ lục)
                  </span>
                )}
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

                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={() => previewSecureFile(`/api/contracts/${contract.id}/scanned-file/download`, `HopDong_${contract.contractNumber}.pdf`)}
                    className="inline-flex items-center gap-1 px-3 py-1.5 bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold rounded-lg shadow-2xs transition-colors cursor-pointer"
                  >
                    <ExternalLink className="w-3.5 h-3.5" />
                    <span>Xem file</span>
                  </button>

                  <button
                    type="button"
                    onClick={() => downloadSecureFile(`/api/contracts/${contract.id}/scanned-file/download`, `HopDong_${contract.contractNumber}.pdf`)}
                    className="inline-flex items-center gap-1 px-3 py-1.5 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold rounded-lg border border-slate-300 transition-colors cursor-pointer"
                    title="Tải tệp PDF về máy"
                  >
                    <Download className="w-3.5 h-3.5" />
                    <span>Tải về</span>
                  </button>

                  {isAdminOrProcurement && (
                    <button
                      type="button"
                      onClick={() => setIsUploadModalOpen(true)}
                      className="p-1.5 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition-colors cursor-pointer"
                      title="Tải lên tệp thay thế"
                    >
                      <Upload className="w-4 h-4" />
                    </button>
                  )}
                </div>
              </div>
            ) : (
              <div className="p-4 border border-dashed border-slate-200 rounded-xl text-center space-y-2">
                <p className="text-xs text-slate-500">Chưa tải lên bản scan hợp đồng có chữ ký đóng dấu.</p>
                {isAdminOrProcurement && (
                  <button
                    type="button"
                    onClick={() => setIsUploadModalOpen(true)}
                    className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-blue-50 hover:bg-blue-100 text-blue-700 text-xs font-bold rounded-lg border border-blue-200 transition-colors cursor-pointer"
                  >
                    <Upload className="w-3.5 h-3.5" />
                    <span>Tải lên ngay</span>
                  </button>
                )}
              </div>
            )}
          </div>
        </div>

        {/* RIGHT COLUMN: TABS (Milestones, Guarantees, Addenda, Evaluation) (7 cols) */}
        <div className="lg:col-span-7 space-y-5">
          {/* TAB NAVIGATION BAR */}
          <div className="bg-white rounded-2xl border border-slate-200 p-1.5 shadow-2xs flex flex-wrap gap-1">
            {/* Tab 1: Mốc Nghiệm thu & Giải ngân */}
            <button
              type="button"
              onClick={() => setActiveTab('milestones')}
              className={`flex-1 min-w-[130px] flex items-center justify-center gap-2 py-2.5 px-3 rounded-xl text-xs font-bold transition-all cursor-pointer ${
                activeTab === 'milestones'
                  ? 'bg-blue-600 text-white shadow-xs'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-slate-50'
              }`}
            >
              <Clock className="w-4 h-4 shrink-0" />
              <span>Mốc Nghiệm Thu</span>
              <span
                className={`text-[10px] px-1.5 py-0.2 rounded-full font-black ${
                  activeTab === 'milestones' ? 'bg-white/20 text-white' : 'bg-slate-100 text-slate-600'
                }`}
              >
                {milestones.length}
              </span>
            </button>

            {/* Tab 2: Bảo Lãnh Hợp Đồng */}
            <button
              type="button"
              onClick={() => setActiveTab('guarantees')}
              className={`flex-1 min-w-[130px] flex items-center justify-center gap-2 py-2.5 px-3 rounded-xl text-xs font-bold transition-all cursor-pointer ${
                activeTab === 'guarantees'
                  ? 'bg-blue-600 text-white shadow-xs'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-slate-50'
              }`}
            >
              <ShieldCheck className="w-4 h-4 shrink-0" />
              <span>Bảo Lãnh HĐ</span>
              <span
                className={`text-[10px] px-1.5 py-0.2 rounded-full font-black ${
                  activeTab === 'guarantees' ? 'bg-white/20 text-white' : 'bg-slate-100 text-slate-600'
                }`}
              >
                {contract.guarantees?.length || 0}
              </span>
            </button>

            {/* Tab 3: Phụ Lục Hợp Đồng */}
            <button
              type="button"
              onClick={() => setActiveTab('addenda')}
              className={`flex-1 min-w-[130px] flex items-center justify-center gap-2 py-2.5 px-3 rounded-xl text-xs font-bold transition-all cursor-pointer ${
                activeTab === 'addenda'
                  ? 'bg-blue-600 text-white shadow-xs'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-slate-50'
              }`}
            >
              <Layers className="w-4 h-4 shrink-0" />
              <span>Phụ Lục HĐ</span>
              <span
                className={`text-[10px] px-1.5 py-0.2 rounded-full font-black ${
                  activeTab === 'addenda' ? 'bg-white/20 text-white' : 'bg-slate-100 text-slate-600'
                }`}
              >
                {contract.addenda?.length || 0}
              </span>
            </button>

            {/* Tab 4: Đánh Giá Nghiệm Thu Nhà Thầu */}
            <button
              type="button"
              onClick={() => setActiveTab('evaluation')}
              className={`flex-1 min-w-[130px] flex items-center justify-center gap-2 py-2.5 px-3 rounded-xl text-xs font-bold transition-all cursor-pointer ${
                activeTab === 'evaluation'
                  ? 'bg-blue-600 text-white shadow-xs'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-slate-50'
              }`}
            >
              <Award className="w-4 h-4 shrink-0" />
              <span>Đánh Giá Uy Tín</span>
              {contract.evaluation ? (
                <span className="text-[10px] px-1.5 py-0.2 rounded-full font-black bg-emerald-500 text-white">
                  {contract.evaluation.ratingGrade}
                </span>
              ) : (
                <span
                  className={`text-[10px] px-1.5 py-0.2 rounded-full font-black ${
                    activeTab === 'evaluation' ? 'bg-white/20 text-white' : 'bg-slate-100 text-slate-600'
                  }`}
                >
                  0
                </span>
              )}
            </button>
          </div>

          {/* TAB CONTENTS */}
          <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-2xs">
            {/* TAB 1: MILESTONES (MỐC THANH TOÁN HIỆN CÓ) */}
            {activeTab === 'milestones' && (
              <div className="space-y-4">
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

                              <div className="space-y-1 flex-1">
                                <div className="flex flex-wrap items-center gap-2">
                                  <h4 className="text-xs font-bold text-slate-900">
                                    {m.title}
                                  </h4>
                                  <StatusBadge status={m.statusName || m.status} type="milestone" />
                                </div>

                                <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-slate-500 pt-0.5">
                                  <span>
                                    Hạn hoàn thành:{' '}
                                    <strong className="text-slate-800">
                                      {formatDate(m.dueDate)}
                                    </strong>
                                  </span>
                                  <span>•</span>
                                  <span>
                                    Số tiền giải ngân:{' '}
                                    <strong className="text-emerald-700 font-black">
                                      {formatCurrency(m.amount)}
                                    </strong>{' '}
                                    ({mPercent}%)
                                  </span>
                                </div>
                              </div>
                            </div>

                            {/* Cụm Action Buttons cho Mốc */}
                            <div className="flex items-center gap-1.5 self-end sm:self-center">
                              {/* Nút Nghiệm thu (Admin / Procurement) */}
                              {isAdminOrProcurement && isMilestonePending && (
                                <button
                                  type="button"
                                  onClick={() => {
                                    setSelectedMilestoneToApprove(m);
                                    setIsApproveModalOpen(true);
                                  }}
                                  className="inline-flex items-center gap-1 px-3 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-lg shadow-2xs transition-colors cursor-pointer"
                                  title="Phê duyệt nghiệm thu mốc thanh toán"
                                >
                                  <FileCheck className="w-3.5 h-3.5" />
                                  <span>Nghiệm thu</span>
                                </button>
                              )}

                              {/* Sửa mốc (Khi chưa Completed) */}
                              {isAdminOrProcurement && !isMilestoneCompleted && (
                                <button
                                  type="button"
                                  onClick={() => {
                                    setMilestoneToEdit(m);
                                    setIsMilestoneModalOpen(true);
                                  }}
                                  className="p-1.5 text-slate-500 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition-colors cursor-pointer"
                                  title="Chỉnh sửa mốc"
                                >
                                  <Edit3 className="w-3.5 h-3.5" />
                                </button>
                              )}

                              {/* Xóa mốc (Khi chưa Completed) */}
                              {isAdminOrProcurement && !isMilestoneCompleted && (
                                <button
                                  type="button"
                                  onClick={() => handleDeleteMilestone(m.id)}
                                  className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors cursor-pointer"
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
            )}

            {/* TAB 2: GUARANTEES (BẢO LÃNH THỰC HIỆN HỢP ĐỒNG) */}
            {activeTab === 'guarantees' && (
              <ContractGuaranteeTab
                contract={contract}
                isAdminOrProcurement={isAdminOrProcurement}
                onOpenAddModal={() => setIsGuaranteeModalOpen(true)}
                onRefresh={() => fetchData(true)}
              />
            )}

            {/* TAB 3: ADDENDA (PHỤ LỤC HỢP ĐỒNG) */}
            {activeTab === 'addenda' && (
              <ContractAddendumTab
                contract={contract}
                isAdminOrProcurement={isAdminOrProcurement}
                onOpenAddModal={() => setIsAddendumModalOpen(true)}
                onRefresh={() => fetchData(true)}
              />
            )}

            {/* TAB 4: EVALUATION (ĐÁNH GIÁ NGHIỆM THU NHÀ THẦU) */}
            {activeTab === 'evaluation' && (
              <ContractorEvaluationTab
                contract={contract}
                isAdminOrProcurement={isAdminOrProcurement}
                onOpenEvaluationModal={() => setIsEvaluationModalOpen(true)}
              />
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

      {/* 3. Modal Tải lên bản Scan PDF HĐ */}
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
        currentStatus={contract.status}
        onSuccess={() => fetchData(true)}
      />

      {/* 5. Modal Thêm Chứng thư Bảo lãnh (Nhóm B) */}
      <AddGuaranteeModal
        isOpen={isGuaranteeModalOpen}
        onClose={() => setIsGuaranteeModalOpen(false)}
        contract={contract}
        onSuccess={() => fetchData(true)}
      />

      {/* 6. Modal Lập Phụ lục Hợp đồng (Nhóm B) */}
      <AddAddendumModal
        isOpen={isAddendumModalOpen}
        onClose={() => setIsAddendumModalOpen(false)}
        contract={contract}
        onSuccess={() => fetchData(true)}
      />

      {/* 7. Modal Đánh giá Nghiệm thu Nhà thầu (Nhóm B) */}
      <ContractorEvaluationModal
        isOpen={isEvaluationModalOpen}
        onClose={() => setIsEvaluationModalOpen(false)}
        contract={contract}
        existingEvaluation={contract.evaluation}
        onSuccess={() => fetchData(true)}
      />
    </div>
  );
};

export default ContractDetailPage;
