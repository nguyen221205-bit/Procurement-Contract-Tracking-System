import React, { useState, useEffect } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import {
  ArrowLeft,
  Calendar,
  Clock,
  DollarSign,
  FileText,
  Building,
  CheckCircle2,
  AlertCircle,
  Scale,
  Download,
  Lock,
  Unlock,
  ShieldCheck,
  FileCheck,
  Send,
  Eye,
  Trash2,
  ExternalLink,
  Plus,
  Upload,
  Edit2,
  Layers,
  Users,
  UserCheck,
  UserMinus,
} from 'lucide-react';
import toast from 'react-hot-toast';
import { useAuth } from '../context/AuthContext';
import { bidPackageApi } from '../api/bidPackageApi';
import { evaluationApi } from '../api/evaluationApi';
import { bidSubmissionApi } from '../api/bidSubmissionApi';
import StatusBadge from '../components/common/StatusBadge';
import LoadingSpinner from '../components/common/LoadingSpinner';
import ConfirmModal from '../components/common/ConfirmModal';
import CriteriaModal from '../components/packages/CriteriaModal';
import CriteriaTemplateModal from '../components/packages/CriteriaTemplateModal';
import AssignEvaluatorModal from '../components/packages/AssignEvaluatorModal';
import SubmitBidModal from '../components/submissions/SubmitBidModal';
import { PACKAGE_TYPES, ROLES } from '../utils/constants';
import { formatVND, formatCurrency, formatDate, formatDateTime } from '../utils/formatters';
import { downloadSecureFile } from '../utils/fileDownload';

const API_BASE_URL = 'http://localhost:5225';

export const PackageDetailPage = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user, hasRole } = useAuth();

  // State dữ liệu
  const [pkg, setPkg] = useState(null);
  const [criteria, setCriteria] = useState([]);
  const [submissions, setSubmissions] = useState([]);
  const [mySubmission, setMySubmission] = useState(null);
  const [loading, setLoading] = useState(true);

  // Tab navigation
  const [activeTab, setActiveTab] = useState('info'); // 'info', 'criteria', 'submissions'

  // Quick Action Modal State (Admin / Procurement)
  const [statusModal, setStatusModal] = useState({
    isOpen: false,
    targetStatus: null,
    title: '',
    message: '',
    confirmVariant: 'primary',
  });
  const [statusLoading, setStatusLoading] = useState(false);

  // Withdraw Submission Modal State (Contractor)
  const [withdrawModal, setWithdrawModal] = useState(false);
  const [withdrawLoading, setWithdrawLoading] = useState(false);

  // Submit Bid Modal State (Contractor)
  const [isSubmitBidModalOpen, setIsSubmitBidModalOpen] = useState(false);

  // View Submission Details Modal (Admin/Procurement/Evaluator/Contractor)
  const [selectedSubmission, setSelectedSubmission] = useState(null);
  const [submissionDetailLoading, setSubmissionDetailLoading] = useState(false);

  // Criteria Management States (Admin / Procurement)
  const [criteriaModalState, setCriteriaModalState] = useState({ isOpen: false, initialData: null });
  const [isTemplateModalOpen, setIsTemplateModalOpen] = useState(false);
  const [deleteCriteriaModalState, setDeleteCriteriaModalState] = useState({
    isOpen: false,
    criteriaId: null,
    criteriaName: '',
  });
  const [deleteCriteriaLoading, setDeleteCriteriaLoading] = useState(false);

  // Committee / Evaluator Management States
  const [evaluators, setEvaluators] = useState([]);
  const [evaluatorsLoading, setEvaluatorsLoading] = useState(false);
  const [isAssignEvaluatorModalOpen, setIsAssignEvaluatorModalOpen] = useState(false);
  const [deleteEvaluatorModalState, setDeleteEvaluatorModalState] = useState({
    isOpen: false,
    evaluator: null,
  });
  const [deleteEvaluatorLoading, setDeleteEvaluatorLoading] = useState(false);

  // Vai trò người dùng (sử dụng hasRole chuẩn từ AuthContext)
  const isAdmin = hasRole(ROLES.ADMIN);
  const isProcurement = hasRole(ROLES.PROCUREMENT);
  const isEvaluator = hasRole(ROLES.EVALUATOR);
  const isContractor = hasRole(ROLES.CONTRACTOR);
  const canManageStatus = isAdmin || isProcurement;
  const canViewSubmissions = isAdmin || isProcurement || isEvaluator;

  // Kiểm tra tính hợp lệ của Tổ chuyên gia theo Luật Đấu thầu: Tối thiểu 3 thành viên và là số lẻ
  const isCommitteeValid = evaluators.length >= 3 && evaluators.length % 2 === 1;
  const canManageCommittee = (isAdmin || isProcurement) && 
    String(pkg?.status) !== 'Contracted' && String(pkg?.status) !== '4' &&
    String(pkg?.status) !== 'Awarded' && String(pkg?.status) !== '3';

  // Tải dữ liệu ban đầu
  const loadPackageData = async () => {
    try {
      setLoading(true);
      const resPkg = await bidPackageApi.getPackageById(id);
      if (resPkg?.data) {
        setPkg(resPkg.data);
      }

      // 1. Tải tiêu chí đánh giá (tất cả mọi vai trò đều được xem)
      try {
        const resCriteria = await evaluationApi.getCriteriaByPackage(id);
        if (resCriteria?.data) {
          setCriteria(resCriteria.data);
        }
      } catch (err) {
        console.warn('Chưa có tiêu chí đánh giá:', err);
      }

      // 2. Tải danh sách hồ sơ (Admin, Procurement, Evaluator)
      if (hasRole([ROLES.ADMIN, ROLES.PROCUREMENT, ROLES.EVALUATOR])) {
        try {
          const resSubs = await bidSubmissionApi.getSubmissionsByPackage(id);
          if (resSubs?.data) {
            setSubmissions(resSubs.data);
          }
        } catch (err) {
          console.warn('Chưa thể lấy danh sách hồ sơ:', err);
        }
      }

      // 3. Nếu là Nhà thầu (Contractor): Kiểm tra xem chính mình đã nộp hồ sơ gói này chưa
      if (hasRole(ROLES.CONTRACTOR)) {
        try {
          const resMySubs = await bidSubmissionApi.getMySubmissions({ pageSize: 50 });
          const items = resMySubs?.data?.items || resMySubs?.data || [];
          const matched = items.find((s) => s.bidPackageId === Number(id));
          setMySubmission(matched || null);
        } catch (err) {
          console.warn('Lỗi kiểm tra hồ sơ nhà thầu:', err);
        }
      }

      // 4. Tải danh sách Tổ chuyên gia (Admin, Procurement, Evaluator)
      if (hasRole([ROLES.ADMIN, ROLES.PROCUREMENT, ROLES.EVALUATOR])) {
        try {
          setEvaluatorsLoading(true);
          const resEval = await bidPackageApi.getEvaluators(id);
          if (resEval?.data) {
            setEvaluators(resEval.data);
          }
        } catch (err) {
          console.warn('Lỗi tải danh sách giám khảo:', err);
        } finally {
          setEvaluatorsLoading(false);
        }
      }
    } catch (error) {
      toast.error(error.message || 'Không thể tải thông tin gói thầu');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadPackageData();
  }, [id, user]);

  // Xử lý Quick Action chuyển trạng thái kế tiếp (Admin & Procurement)
  const handleOpenStatusModal = () => {
    if (!pkg) return;
    const statusStr = String(pkg.status || '');

    if (statusStr === 'Open' || statusStr === '0') {
      setStatusModal({
        isOpen: true,
        targetStatus: 'Closed',
        title: 'Xác nhận đóng nhận hồ sơ thầu',
        message:
          'Bạn có chắc chắn muốn đóng nhận hồ sơ gói thầu này? Sau khi đóng thầu, hệ thống sẽ ngừng nhận hồ sơ mới từ nhà thầu và bạn có thể chuyển tiếp sang giai đoạn Chấm điểm.',
        confirmVariant: 'warning',
      });
    } else if (statusStr === 'Closed' || statusStr === '1') {
      if (totalWeight !== 100) {
        toast.error(`Không thể chuyển sang Chấm điểm: Tổng trọng số bộ tiêu chí phải đạt đúng 100% (Hiện tại: ${totalWeight}%).`);
        return;
      }
      if (!isCommitteeValid) {
        toast.error(`Cần tối thiểu 3 giám khảo và là số lẻ (3, 5, 7...) để mở chấm thầu (Hiện có: ${evaluators.length} người).`);
        return;
      }
      setStatusModal({
        isOpen: true,
        targetStatus: 'Evaluating',
        title: 'Xác nhận chuyển sang giai đoạn chấm điểm',
        message:
          'Bạn có chắc chắn muốn chuyển gói thầu sang giai đoạn Chấm điểm? Tổ chuyên gia gồm các giám khảo được chỉ định sẽ có quyền truy cập vào phòng chấm điểm để đánh giá các hồ sơ.',
        confirmVariant: 'primary',
      });
    }
  };

  const handleConfirmStatusChange = async () => {
    if (!statusModal.targetStatus) return;
    try {
      setStatusLoading(true);
      await bidPackageApi.changeStatus(pkg.id, statusModal.targetStatus);
      toast.success('Chuyển trạng thái gói thầu thành công');
      setStatusModal((prev) => ({ ...prev, isOpen: false }));
      loadPackageData();
    } catch (error) {
      toast.error(error.message || 'Không thể cập nhật trạng thái');
    } finally {
      setStatusLoading(false);
    }
  };

  // Quản lý Tiêu chí đánh giá (Admin & Procurement)
  const handleOpenAddCriteria = () => {
    if (totalWeight >= 100) {
      toast.error('Gói thầu đã đạt tối đa 100% trọng số. Vui lòng chỉnh sửa hoặc xóa bớt tiêu chí hiện có.');
      return;
    }
    setCriteriaModalState({ isOpen: true, initialData: null });
  };

  const handleOpenEditCriteria = (crit) => {
    setCriteriaModalState({ isOpen: true, initialData: crit });
  };

  const handleOpenDeleteCriteria = (crit) => {
    setDeleteCriteriaModalState({
      isOpen: true,
      criteriaId: crit.id,
      criteriaName: crit.name,
    });
  };

  const handleConfirmDeleteCriteria = async () => {
    if (!deleteCriteriaModalState.criteriaId) return;
    try {
      setDeleteCriteriaLoading(true);
      await evaluationApi.deleteCriteria(deleteCriteriaModalState.criteriaId);
      toast.success('Xóa tiêu chí đánh giá thành công!');
      setDeleteCriteriaModalState({ isOpen: false, criteriaId: null, criteriaName: '' });
      loadPackageData();
    } catch (err) {
      toast.error(err.message || 'Không thể xóa tiêu chí đánh giá.');
    } finally {
      setDeleteCriteriaLoading(false);
    }
  };

  // Quản lý Tổ chuyên gia (Admin & Procurement)
  const handleOpenDeleteEvaluator = (evaluator) => {
    setDeleteEvaluatorModalState({
      isOpen: true,
      evaluator,
    });
  };

  const handleConfirmDeleteEvaluator = async () => {
    if (!deleteEvaluatorModalState.evaluator) return;
    try {
      setDeleteEvaluatorLoading(true);
      await bidPackageApi.removeEvaluator(pkg.id, deleteEvaluatorModalState.evaluator.evaluatorId);
      toast.success('Đã xóa giám khảo khỏi Tổ chuyên gia.');
      setDeleteEvaluatorModalState({ isOpen: false, evaluator: null });
      loadPackageData();
    } catch (err) {
      toast.error(err.message || 'Không thể xóa giám khảo khỏi Tổ chuyên gia.');
    } finally {
      setDeleteEvaluatorLoading(false);
    }
  };

  // Xử lý rút hồ sơ dự thầu (Contractor)
  const handleWithdrawSubmission = async () => {
    if (!mySubmission) return;
    try {
      setWithdrawLoading(true);
      await bidSubmissionApi.withdrawSubmission(mySubmission.id);
      toast.success('Rút hồ sơ dự thầu thành công');
      setWithdrawModal(false);
      setMySubmission(null);
      loadPackageData();
    } catch (error) {
      toast.error(error.message || 'Không thể rút hồ sơ');
    } finally {
      setWithdrawLoading(false);
    }
  };

  // Mở modal xem chi tiết một hồ sơ đã nộp
  const handleViewSubmissionDetail = async (submissionId) => {
    try {
      setSubmissionDetailLoading(true);
      const res = await bidSubmissionApi.getSubmissionById(submissionId);
      if (res?.data) {
        setSelectedSubmission(res.data);
      }
    } catch (error) {
      toast.error(error.message || 'Không thể tải chi tiết hồ sơ');
    } finally {
      setSubmissionDetailLoading(false);
    }
  };

  // Tính thời hạn còn lại
  const getDeadlineCountdown = (deadlineStr) => {
    if (!deadlineStr) return { text: 'Không xác định', isExpired: true };
    const deadline = new Date(deadlineStr);
    const now = new Date();
    const diffTime = deadline.getTime() - now.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

    if (diffDays < 0) return { text: 'Đã hết hạn nộp thầu', isExpired: true };
    if (diffDays === 0) return { text: 'Hết hạn trong ngày hôm nay', isExpired: false };
    return { text: `Còn ${diffDays} ngày để nộp`, isExpired: false };
  };

  if (loading) {
    return (
      <div className="py-20 flex justify-center">
        <LoadingSpinner size="lg" text="Đang tải thông tin gói thầu..." />
      </div>
    );
  }

  if (!pkg) {
    return (
      <div className="bg-white p-12 rounded-2xl border border-slate-200 text-center space-y-4 max-w-md mx-auto my-12">
        <AlertCircle className="w-12 h-12 text-slate-400 mx-auto" />
        <h3 className="text-base font-bold text-slate-800">Không tìm thấy gói thầu</h3>
        <p className="text-xs text-slate-500">Gói thầu không tồn tại hoặc bạn không có quyền truy cập.</p>
        <Link
          to="/packages"
          className="inline-flex items-center space-x-1.5 px-4 py-2 bg-slate-900 text-white text-xs font-semibold rounded-xl hover:bg-slate-800 transition"
        >
          <ArrowLeft className="w-3.5 h-3.5" />
          <span>Về danh sách gói thầu</span>
        </Link>
      </div>
    );
  }

  const typeConfig = PACKAGE_TYPES[pkg.type] || {
    label: pkg.type || 'Hàng hóa',
    color: 'bg-slate-50 text-slate-700 border-slate-200',
  };

  const deadlineInfo = getDeadlineCountdown(pkg.deadline);
  const totalWeight = criteria.reduce((sum, c) => sum + Number(c.weight || 0), 0);
  const isWeightFull = totalWeight >= 100;
  const isWeightExceeded = totalWeight > 100;
  const statusStr = String(pkg.status || '');
  const isOpen = statusStr === 'Open' || statusStr === '0';
  const isClosed = statusStr === 'Closed' || statusStr === '1';
  const isEvaluating = statusStr === 'Evaluating' || statusStr === '2';
  const isContracted = statusStr === 'Contracted' || statusStr === '3';
  const isAwarded = Boolean(pkg.isAwarded || statusStr === 'Awarded');
  const displayStatus = isContracted
    ? 'Contracted'
    : isAwarded
    ? 'Awarded'
    : pkg.status;
  const canEditCriteria = (isAdmin || isProcurement) && (isOpen || isClosed);

  return (
    <div className="space-y-6 pb-12">
      {/* 1. Breadcrumb & Navigation */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-2 text-xs text-slate-500">
          <Link to="/packages" className="hover:text-slate-900 transition flex items-center space-x-1">
            <ArrowLeft className="w-3.5 h-3.5" />
            <span>Danh sách gói thầu</span>
          </Link>
          <span>/</span>
          <span className="font-mono font-medium text-slate-800">{pkg.code}</span>
        </div>
      </div>

      {/* 2. Package Banner & Title Header */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-xs p-6 space-y-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center space-x-2.5">
            <span className="px-2.5 py-1 bg-slate-100 text-slate-800 font-mono text-xs font-bold rounded-lg border border-slate-200">
              {pkg.code}
            </span>
            <span className={`text-xs px-2.5 py-1 rounded-lg font-medium border ${typeConfig.color}`}>
              {typeConfig.label}
            </span>
            <StatusBadge status={displayStatus} />
          </div>

          <div className="text-xs text-slate-500 flex items-center space-x-4">
            <span className="flex items-center space-x-1.5">
              <Calendar className="w-3.5 h-3.5 text-slate-400" />
              <span>Đăng ngày: {formatDate(pkg.createdAt)}</span>
            </span>
            <span className="flex items-center space-x-1.5">
              <Building className="w-3.5 h-3.5 text-slate-400" />
              <span>Chủ đầu tư: <strong className="text-slate-700">{pkg.createdByName || 'Hệ thống'}</strong></span>
            </span>
          </div>
        </div>

        <h1 className="text-xl md:text-2xl font-black text-slate-900 leading-snug">
          {pkg.name}
        </h1>
      </div>

      {/* 3. Main Grid Layout (70% Content Left / 30% Sticky Sidebar Right) */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 items-start">
        {/* === CỘT TRÁI (70%): NỘI DUNG TABS === */}
        <div className="lg:col-span-2 space-y-6">
          {/* Tab Navigation Header */}
          <div className="bg-white rounded-2xl border border-slate-200 shadow-xs p-1.5 flex items-center space-x-1">
            <button
              onClick={() => setActiveTab('info')}
              className={`flex-1 py-2 px-3 text-xs font-bold rounded-xl transition flex items-center justify-center space-x-2 ${
                activeTab === 'info'
                  ? 'bg-sky-600 text-white shadow-xs'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-slate-50'
              }`}
            >
              <FileText className="w-4 h-4" />
              <span>Thông tin chung & HSMT</span>
            </button>

            <button
              onClick={() => setActiveTab('criteria')}
              className={`flex-1 py-2 px-3 text-xs font-bold rounded-xl transition flex items-center justify-center space-x-2 ${
                activeTab === 'criteria'
                  ? 'bg-sky-600 text-white shadow-xs'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-slate-50'
              }`}
            >
              <Scale className="w-4 h-4" />
              <span>Tiêu chí đánh giá ({criteria.length})</span>
            </button>

            {/* Tab 3: CHỈ hiển thị với Admin, Procurement, Evaluator (Ẩn với Contractor) */}
            {canViewSubmissions && (
              <button
                onClick={() => setActiveTab('submissions')}
                className={`flex-1 py-2 px-3 text-xs font-bold rounded-xl transition flex items-center justify-center space-x-2 ${
                  activeTab === 'submissions'
                    ? 'bg-sky-600 text-white shadow-xs'
                    : 'text-slate-600 hover:text-slate-900 hover:bg-slate-50'
                }`}
              >
                {isOpen ? <Lock className="w-4 h-4 text-amber-500" /> : <FileCheck className="w-4 h-4" />}
                <span>
                  Hồ sơ đã nộp {isOpen ? '(Niêm phong)' : `(${submissions.length})`}
                </span>
              </button>
            )}

            {/* Tab 4: TỔ CHUYÊN GIA / BAN GIÁM KHẢO (Admin, Procurement, Evaluator) */}
            {canViewSubmissions && (
              <button
                onClick={() => setActiveTab('committee')}
                className={`flex-1 py-2 px-3 text-xs font-bold rounded-xl transition flex items-center justify-center space-x-1.5 ${
                  activeTab === 'committee'
                    ? 'bg-sky-600 text-white shadow-xs'
                    : 'text-slate-600 hover:text-slate-900 hover:bg-slate-50'
                }`}
              >
                <Users className="w-4 h-4" />
                <span>Tổ chuyên gia ({evaluators.length})</span>
                {evaluators.length > 0 && (
                  <span
                    className={`w-2 h-2 rounded-full ${
                      isCommitteeValid ? 'bg-emerald-400' : 'bg-amber-400'
                    }`}
                    title={isCommitteeValid ? 'Đủ điều kiện pháp lý' : 'Chưa đủ điều kiện'}
                  />
                )}
              </button>
            )}
          </div>

          {/* TAB 1: THÔNG TIN CHUNG & TÀI LIỆU HSMT */}
          {activeTab === 'info' && (
            <div className="space-y-6">
              {/* Mô tả chi tiết */}
              <div className="bg-white rounded-2xl border border-slate-200 shadow-xs p-6 space-y-3">
                <h3 className="text-sm font-bold text-slate-900 flex items-center space-x-2">
                  <FileText className="w-4 h-4 text-sky-600" />
                  <span>Mô tả & Phạm vi công việc</span>
                </h3>
                <div className="text-xs text-slate-600 leading-relaxed whitespace-pre-line bg-slate-50/60 p-4 rounded-xl border border-slate-100">
                  {pkg.description || 'Chưa có mô tả chi tiết cho gói thầu này.'}
                </div>
              </div>

              {/* Tài liệu mời thầu HSMT */}
              <div className="bg-white rounded-2xl border border-slate-200 shadow-xs p-6 space-y-4">
                <div className="flex items-center justify-between">
                  <h3 className="text-sm font-bold text-slate-900 flex items-center space-x-2">
                    <Download className="w-4 h-4 text-sky-600" />
                    <span>Hồ sơ mời thầu đính kèm (HSMT)</span>
                  </h3>
                  <span className="text-xs text-slate-400 font-medium">
                    {pkg.bidDocuments?.length || 0} tài liệu
                  </span>
                </div>

                {pkg.bidDocuments && pkg.bidDocuments.length > 0 ? (
                  <div className="divide-y divide-slate-100 border border-slate-100 rounded-xl overflow-hidden">
                    {pkg.bidDocuments.map((doc) => (
                      <div
                        key={doc.id}
                        className="p-3.5 flex items-center justify-between hover:bg-slate-50 transition"
                      >
                        <div className="flex items-center space-x-3 truncate pr-4">
                          <div className="p-2 bg-sky-50 text-sky-600 rounded-lg flex-shrink-0">
                            <FileText className="w-4 h-4" />
                          </div>
                          <div className="truncate">
                            <p className="text-xs font-bold text-slate-800 truncate" title={doc.fileName}>
                              {doc.fileName}
                            </p>
                            <p className="text-[11px] text-slate-400">
                              Tải lên ngày: {formatDate(doc.uploadedAt)}
                            </p>
                          </div>
                        </div>

                        <button
                          type="button"
                          onClick={() => downloadSecureFile(`/api/bid-packages/documents/${doc.id}/download`, doc.fileName)}
                          className="px-3 py-1.5 bg-slate-100 hover:bg-sky-600 text-slate-700 hover:text-white rounded-lg text-xs font-semibold transition flex items-center space-x-1.5 flex-shrink-0 cursor-pointer"
                        >
                          <Download className="w-3.5 h-3.5" />
                          <span>Tải về</span>
                        </button>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="py-8 text-center text-xs text-slate-400 bg-slate-50/50 rounded-xl border border-dashed border-slate-200">
                    Chưa có tài liệu HSMT nào được đính kèm cho gói thầu này.
                  </div>
                )}
              </div>
            </div>
          )}

          {/* TAB 2: TIÊU CHÍ ĐÁNH GIÁ */}
          {activeTab === 'criteria' && (
            <div className="bg-white rounded-2xl border border-slate-200 shadow-xs p-6 space-y-4">
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 pb-3 border-b border-slate-100">
                <div>
                  <h3 className="text-sm font-bold text-slate-900 flex items-center space-x-2">
                    <Scale className="w-4 h-4 text-sky-600" />
                    <span>Tiêu chí đánh giá</span>
                  </h3>
                </div>

                <div className="flex flex-wrap items-center gap-2">
                  {/* Badge tổng trọng số */}
                  <span
                    className={`inline-flex items-center space-x-1 px-3 py-1.5 rounded-xl text-xs font-bold border ${
                      totalWeight === 100
                        ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
                        : totalWeight > 100
                        ? 'bg-rose-50 text-rose-700 border-rose-200'
                        : 'bg-amber-50 text-amber-700 border-amber-200'
                    }`}
                  >
                    <span>Tổng: {totalWeight}%</span>
                  </span>

                  {/* Nút hành động ở trên CHỈ hiển thị khi đã có tiêu chí trong danh sách */}
                  {canEditCriteria && criteria.length > 0 && (
                    <div className="flex items-center space-x-2">
                      <button
                        type="button"
                        onClick={() => setIsTemplateModalOpen(true)}
                        className="px-3 py-1.5 bg-indigo-50 hover:bg-indigo-100 text-indigo-700 border border-indigo-200 rounded-xl text-xs font-semibold transition flex items-center space-x-1"
                      >
                        <Layers className="w-3.5 h-3.5" />
                        <span>Áp dụng bộ mẫu</span>
                      </button>

                      <button
                        type="button"
                        onClick={handleOpenAddCriteria}
                        disabled={isWeightFull}
                        className={`px-3 py-1.5 rounded-xl text-xs font-semibold transition shadow-xs flex items-center space-x-1 ${
                          isWeightFull
                            ? 'bg-slate-100 text-slate-400 border border-slate-200 cursor-not-allowed'
                            : 'bg-sky-600 hover:bg-sky-700 text-white'
                        }`}
                        title={isWeightFull ? 'Đã đạt tối đa 100% trọng số' : 'Thêm tiêu chí'}
                      >
                        <Plus className="w-3.5 h-3.5" />
                        <span>Thêm tiêu chí</span>
                      </button>
                    </div>
                  )}
                </div>
              </div>

              {/* Dải cảnh báo nếu tổng trọng số vượt quá 100% */}
              {isWeightExceeded && (
                <div className="p-3 bg-rose-50 border border-rose-200 rounded-xl text-rose-800 text-xs flex items-center space-x-2">
                  <AlertCircle className="w-4 h-4 text-rose-600 flex-shrink-0" />
                  <span>
                    Tổng trọng số đang là <strong>{totalWeight}%</strong>, vượt mức quy định (100%). Vui lòng chỉnh sửa hoặc xóa bớt tiêu chí.
                  </span>
                </div>
              )}

              {criteria.length > 0 ? (
                <div className="border border-slate-100 rounded-xl overflow-hidden">
                  <table className="w-full text-left text-xs">
                    <thead className="bg-slate-50 text-slate-600 border-b border-slate-100 font-semibold">
                      <tr>
                        <th className="py-3 px-4 w-12 text-center">STT</th>
                        <th className="py-3 px-4">Tên tiêu chí</th>
                        <th className="py-3 px-4 w-28 text-center">Điểm tối đa</th>
                        <th className="py-3 px-4 w-28 text-center">Trọng số</th>
                        {canEditCriteria && (
                          <th className="py-3 px-4 w-24 text-right">Thao tác</th>
                        )}
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100 text-slate-700">
                      {criteria.map((c, idx) => (
                        <tr key={c.id || idx} className="hover:bg-slate-50/70 transition">
                          <td className="py-3 px-4 text-center font-mono text-slate-400">{idx + 1}</td>
                          <td className="py-3 px-4 font-semibold text-slate-800">{c.name}</td>
                          <td className="py-3 px-4 text-center font-mono font-bold text-slate-700">
                            {c.maxScore} đ
                          </td>
                          <td className="py-3 px-4 text-center font-mono font-extrabold text-sky-600">
                            {c.weight}%
                          </td>
                          {canEditCriteria && (
                            <td className="py-3 px-4 text-right space-x-1">
                              <button
                                type="button"
                                onClick={() => handleOpenEditCriteria(c)}
                                className="p-1.5 text-slate-400 hover:text-sky-600 hover:bg-sky-50 rounded-lg transition"
                                title="Chỉnh sửa"
                              >
                                <Edit2 className="w-3.5 h-3.5" />
                              </button>
                              <button
                                type="button"
                                onClick={() => handleOpenDeleteCriteria(c)}
                                className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition"
                                title="Xóa"
                              >
                                <Trash2 className="w-3.5 h-3.5" />
                              </button>
                            </td>
                          )}
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <div className="py-12 text-center text-xs text-slate-500 bg-slate-50/50 rounded-xl border border-dashed border-slate-200 space-y-3">
                  <p className="text-slate-400 font-medium">Chưa có tiêu chí đánh giá</p>
                  {canEditCriteria && (
                    <div className="flex flex-wrap items-center justify-center gap-2 pt-1">
                      <button
                        type="button"
                        onClick={() => setIsTemplateModalOpen(true)}
                        className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl font-semibold shadow-xs transition flex items-center space-x-1.5"
                      >
                        <Layers className="w-4 h-4" />
                        <span>Áp dụng bộ mẫu</span>
                      </button>
                      <button
                        type="button"
                        onClick={handleOpenAddCriteria}
                        className="px-4 py-2 bg-white hover:bg-slate-100 text-slate-700 border border-slate-300 rounded-xl font-semibold transition flex items-center space-x-1"
                      >
                        <Plus className="w-3.5 h-3.5" />
                        <span>Thêm tiêu chí</span>
                      </button>
                    </div>
                  )}
                </div>
              )}
            </div>
          )}

          {/* TAB 3: HỒ SƠ THẦU ĐÃ NỘP (Admin / Procurement / Evaluator) */}
          {activeTab === 'submissions' && canViewSubmissions && (
            <div className="bg-white rounded-2xl border border-slate-200 shadow-xs p-6 space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <h3 className="text-sm font-bold text-slate-900 flex items-center space-x-2">
                    <FileCheck className="w-4 h-4 text-sky-600" />
                    <span>Danh sách hồ sơ dự thầu đã tiếp nhận</span>
                  </h3>
                  <p className="text-xs text-slate-500 mt-0.5">
                    Hồ sơ kỹ thuật và tài chính do các nhà thầu gửi lên hệ thống.
                  </p>
                </div>
                {isOpen ? (
                  <span className="text-xs font-bold px-2.5 py-1 bg-amber-50 text-amber-700 border border-amber-200 rounded-lg flex items-center space-x-1">
                    <Lock className="w-3.5 h-3.5" />
                    <span>Đang niêm phong</span>
                  </span>
                ) : (
                  <span className="text-xs font-bold px-2.5 py-1 bg-slate-100 text-slate-700 rounded-lg">
                    {submissions.length} hồ sơ
                  </span>
                )}
              </div>

              {isOpen ? (
                <div className="p-6 bg-gradient-to-br from-amber-50/80 via-amber-50/40 to-white rounded-2xl border border-amber-200/90 text-amber-900 space-y-4">
                  <div className="flex items-start space-x-3.5">
                    <div className="p-2.5 bg-amber-100 text-amber-700 rounded-xl mt-0.5 shrink-0 shadow-xs">
                      <Lock className="w-5 h-5" />
                    </div>
                    <div className="space-y-1.5 flex-1">
                      <div className="flex items-center space-x-2">
                        <h4 className="text-sm font-bold text-amber-900">
                          Hồ sơ dự thầu đang trong trạng thái niêm phong bảo mật
                        </h4>
                        <span className="text-[10px] px-2 py-0.5 bg-amber-200/70 text-amber-800 rounded-full font-bold">
                          Sealed Bids
                        </span>
                      </div>
                      <p className="text-xs text-amber-800 leading-relaxed">
                        Theo quy định tại <strong>Luật Đấu thầu</strong> và nguyên tắc bảo mật thông tin, khi gói thầu đang trong giai đoạn tiếp nhận hồ sơ (<strong>Đang mở</strong>), toàn bộ hồ sơ do các nhà thầu gửi lên được <strong>niêm phong điện tử tự động</strong>. Bên mời thầu, Giám khảo hay Quản trị viên đều không được phép mở xem trước danh sách nhằm chống lộ giá thầu và đảm bảo tính công bằng, minh bạch.
                      </p>
                      <p className="text-xs text-amber-900/90 font-medium">
                        Hệ thống đã ghi nhận hồ sơ nộp thành công vào cơ sở dữ liệu. Sau khi kết thúc thời gian nhận hồ sơ, Bên mời thầu thực hiện thao tác <strong>"Đóng nhận hồ sơ thầu"</strong> để mở niêm phong và truy cập danh sách chấm điểm.
                      </p>
                    </div>
                  </div>

                  {canManageStatus && (
                    <div className="pt-3 border-t border-amber-200/60 flex flex-wrap items-center justify-between gap-3">
                      <div className="flex items-center space-x-1.5 text-xs text-amber-700">
                        <Clock className="w-3.5 h-3.5 text-amber-600" />
                        <span>Hạn nộp thầu: <strong>{formatDate(pkg.bidClosingDate)}</strong></span>
                      </div>
                      <button
                        onClick={handleOpenStatusModal}
                        className="px-4 py-2 bg-amber-600 hover:bg-amber-700 active:scale-95 text-white text-xs font-bold rounded-xl shadow-xs transition flex items-center space-x-2"
                      >
                        <Lock className="w-3.5 h-3.5" />
                        <span>Đóng nhận hồ sơ thầu (Mở niêm phong ngay)</span>
                      </button>
                    </div>
                  )}
                </div>
              ) : submissions.length > 0 ? (
                <div className="border border-slate-100 rounded-xl overflow-hidden">
                  <table className="w-full text-left text-xs">
                    <thead className="bg-slate-50 text-slate-600 border-b border-slate-100 font-semibold">
                      <tr>
                        <th className="py-3 px-4 w-12 text-center">STT</th>
                        <th className="py-3 px-4">Doanh nghiệp / Nhà thầu</th>
                        <th className="py-3 px-4">Thời gian nộp</th>
                        <th className="py-3 px-4 text-right">Giá dự thầu</th>
                        <th className="py-3 px-4 text-center">Số tệp</th>
                        <th className="py-3 px-4 text-center">Trạng thái</th>
                        <th className="py-3 px-4 text-center">Điểm / Hạng</th>
                        <th className="py-3 px-4 text-right">Chi tiết</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100 text-slate-700">
                      {submissions.map((sub, idx) => (
                        <tr key={sub.id} className="hover:bg-slate-50/70 transition">
                          <td className="py-3 px-4 text-center font-mono text-slate-400">{idx + 1}</td>
                          <td className="py-3 px-4 font-bold text-slate-800">
                            {sub.companyName || 'Nhà thầu #' + sub.contractorId}
                          </td>
                          <td className="py-3 px-4 text-slate-500 font-mono text-[11px]">
                            {formatDateTime(sub.submittedAt)}
                          </td>
                          <td className="py-3 px-4 text-right font-mono">
                            {(() => {
                              const isPublic = pkg?.status === 'Awarded' || pkg?.status === 'Contracted' || String(pkg?.status) === '3' || String(pkg?.status) === '4';
                              const canEvaluatorView = pkg?.status === 'Evaluating' && (isEvaluator || isAssignedEvaluator || isAdmin || isProcurement);
                              if (isPublic) {
                                return (
                                  <span className="font-bold text-emerald-700">
                                    {sub.bidPrice ? formatCurrency(sub.bidPrice) : '-'}
                                  </span>
                                );
                              }
                              if (canEvaluatorView) {
                                return (
                                  <span className="font-bold text-sky-700">
                                    {sub.bidPrice ? formatCurrency(sub.bidPrice) : '-'}
                                  </span>
                                );
                              }
                              return (
                                <span className="text-amber-600 font-medium text-[11px] inline-flex items-center space-x-1">
                                  <Lock className="w-3 h-3" />
                                  <span>Niêm phong</span>
                                </span>
                              );
                            })()}
                          </td>
                          <td className="py-3 px-4 text-center font-mono font-medium">
                            {sub.fileCount} tệp
                          </td>
                          <td className="py-3 px-4 text-center">
                            <span className="px-2 py-0.5 rounded-full text-[10px] font-semibold bg-sky-50 text-sky-700 border border-sky-200">
                              {sub.status || 'Đã nộp'}
                            </span>
                          </td>
                          <td className="py-3 px-4 text-center font-mono">
                            {sub.totalScore != null ? (
                              <span className="font-bold text-slate-900">
                                {sub.totalScore} đ
                                {sub.rank && (
                                  <span className="ml-1 text-sky-600 font-extrabold">(# {sub.rank})</span>
                                )}
                              </span>
                            ) : (
                              <span className="text-slate-400">-</span>
                            )}
                          </td>
                          <td className="py-3 px-4 text-right">
                            <button
                              onClick={() => handleViewSubmissionDetail(sub.id)}
                              className="p-1.5 text-slate-500 hover:text-sky-600 hover:bg-sky-50 rounded-lg transition"
                              title="Xem chi tiết tệp hồ sơ"
                            >
                              <Eye className="w-4 h-4" />
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <div className="py-8 text-center text-xs text-slate-400 bg-slate-50/50 rounded-xl border border-dashed border-slate-200">
                  Chưa có nhà thầu nào nộp hồ sơ cho gói thầu này.
                </div>
              )}
            </div>
          )}

          {/* TAB 4: TỔ CHUYÊN GIA / BAN GIÁM KHẢO */}
          {activeTab === 'committee' && (
            <div className="bg-white rounded-2xl border border-slate-200 shadow-xs p-6 space-y-4">
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 pb-3 border-b border-slate-100">
                <div>
                  <h3 className="text-sm font-bold text-slate-900 flex items-center space-x-2">
                    <Users className="w-4 h-4 text-sky-600" />
                    <span>Tổ chuyên gia chấm thầu</span>
                  </h3>
                  <p className="text-xs text-slate-500 mt-0.5">
                    Giám khảo tham gia chấm điểm gói thầu
                  </p>
                </div>

                <div className="flex flex-wrap items-center gap-2">
                  {/* Badge điều kiện */}
                  <span
                    className={`inline-flex items-center space-x-1.5 px-3 py-1.5 rounded-xl text-xs font-bold border ${
                      isCommitteeValid
                        ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
                        : 'bg-amber-50 text-amber-700 border-amber-200'
                    }`}
                  >
                    {isCommitteeValid ? (
                      <>
                        <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600" />
                        <span>Đủ điều kiện ({evaluators.length} thành viên)</span>
                      </>
                    ) : (
                      <>
                        <AlertCircle className="w-3.5 h-3.5 text-amber-600" />
                        <span>Chưa đủ điều kiện ({evaluators.length} thành viên)</span>
                      </>
                    )}
                  </span>

                  {/* Nút Phân công Giám khảo (Admin / Procurement) */}
                  {canManageCommittee && (
                    <button
                      type="button"
                      onClick={() => setIsAssignEvaluatorModalOpen(true)}
                      className="px-3 py-1.5 bg-sky-600 hover:bg-sky-700 text-white rounded-xl text-xs font-semibold transition shadow-xs flex items-center space-x-1.5 cursor-pointer"
                    >
                      <Plus className="w-3.5 h-3.5" />
                      <span>Phân công Giám khảo</span>
                    </button>
                  )}
                </div>
              </div>

              {/* Dải thông tin quy định */}
              <div
                className={`p-3 rounded-xl border text-xs flex items-center space-x-2.5 ${
                  isCommitteeValid
                    ? 'bg-emerald-50/70 border-emerald-200/80 text-emerald-800'
                    : 'bg-amber-50 border-amber-200 text-amber-800'
                }`}
              >
                {isCommitteeValid ? (
                  <CheckCircle2 className="w-4 h-4 text-emerald-600 shrink-0" />
                ) : (
                  <AlertCircle className="w-4 h-4 text-amber-600 shrink-0" />
                )}
                <p className="leading-normal">
                  {isCommitteeValid ? (
                    <>
                      <strong>Đạt yêu cầu:</strong> Tối thiểu <strong>3 thành viên</strong> và là <strong>số lẻ (3, 5, 7...)</strong> để mở chấm thầu.
                    </>
                  ) : (
                    <>
                      <strong>Chưa đủ điều kiện:</strong> Cần tối thiểu <strong>3 thành viên</strong> và là <strong>số lẻ (3, 5, 7...)</strong> để mở chấm thầu.
                    </>
                  )}
                </p>
              </div>

              {/* Danh sách thành viên tổ chuyên gia */}
              {evaluatorsLoading ? (
                <div className="py-12 flex justify-center">
                  <LoadingSpinner />
                </div>
              ) : evaluators.length > 0 ? (
                <div className="border border-slate-100 rounded-xl overflow-hidden divide-y divide-slate-100">
                  {evaluators.map((evaluator, index) => (
                    <div
                      key={evaluator.evaluatorId}
                      className="p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-3 hover:bg-slate-50/50 transition"
                    >
                      <div className="flex items-center space-x-3 truncate">
                        <div className="w-9 h-9 rounded-xl bg-sky-100 text-sky-700 font-black text-xs flex items-center justify-center flex-shrink-0">
                          {evaluator.fullName ? evaluator.fullName.charAt(0).toUpperCase() : `#${index + 1}`}
                        </div>
                        <div className="truncate">
                          <div className="flex items-center space-x-2">
                            <span className="text-xs font-bold text-slate-800 truncate">
                              {evaluator.fullName}
                            </span>
                            {evaluator.evaluatorId === user?.id && (
                              <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-sky-50 text-sky-700 border border-sky-200">
                                Bạn
                              </span>
                            )}
                          </div>
                          <div className="text-[11px] text-slate-500 flex flex-wrap items-center gap-x-2 gap-y-0.5 mt-0.5">
                            <span>{evaluator.email}</span>
                            {evaluator.phone && <span>• {evaluator.phone}</span>}
                            <span>• Phân công: {formatDate(evaluator.assignedAt)}</span>
                            {evaluator.assignedByName && <span>bởi {evaluator.assignedByName}</span>}
                          </div>
                        </div>
                      </div>

                      <div className="flex items-center space-x-3 self-end sm:self-center flex-shrink-0">
                        {/* Trạng thái chấm điểm */}
                        {evaluator.hasSubmittedScores ? (
                          <span className="px-2.5 py-1 rounded-lg text-[11px] font-bold bg-emerald-50 text-emerald-700 border border-emerald-200 flex items-center space-x-1">
                            <CheckCircle2 className="w-3.5 h-3.5" />
                            <span>Đã chấm điểm</span>
                          </span>
                        ) : (
                          <span className="px-2.5 py-1 rounded-lg text-[11px] font-medium bg-slate-100 text-slate-600 border border-slate-200">
                            Chưa chấm điểm
                          </span>
                        )}

                        {/* Nút xóa (Admin & Procurement) */}
                        {canManageCommittee && (
                          <button
                            type="button"
                            onClick={() => handleOpenDeleteEvaluator(evaluator)}
                            disabled={isEvaluating && evaluator.hasSubmittedScores}
                            className={`p-1.5 rounded-lg border transition ${
                              isEvaluating && evaluator.hasSubmittedScores
                                ? 'text-slate-300 border-slate-200 cursor-not-allowed bg-slate-50'
                                : 'text-slate-400 hover:text-rose-600 hover:bg-rose-50 hover:border-rose-200 border-slate-200 cursor-pointer'
                            }`}
                            title={
                              isEvaluating && evaluator.hasSubmittedScores
                                ? 'Giám khảo đã nộp điểm, không thể xóa'
                                : 'Xóa giám khảo khỏi tổ chuyên gia'
                            }
                          >
                            <Trash2 className="w-4 h-4" />
                          </button>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="py-10 text-center space-y-3 bg-slate-50/50 rounded-xl border border-dashed border-slate-200 p-6">
                  <div className="w-10 h-10 rounded-full bg-slate-100 text-slate-400 mx-auto flex items-center justify-center">
                    <Users className="w-5 h-5" />
                  </div>
                  <div>
                    <p className="text-xs font-bold text-slate-700">Chưa có giám khảo nào</p>
                    <p className="text-[11px] text-slate-400 mt-0.5">
                      Cần tối thiểu 3 giám khảo (số lẻ) để mở chấm thầu.
                    </p>
                  </div>
                  {canManageCommittee && (
                    <button
                      type="button"
                      onClick={() => setIsAssignEvaluatorModalOpen(true)}
                      className="px-4 py-2 bg-sky-600 hover:bg-sky-700 text-white rounded-xl text-xs font-bold transition shadow-xs inline-flex items-center space-x-1.5 cursor-pointer"
                    >
                      <Plus className="w-4 h-4" />
                      <span>Phân công Giám khảo ngay</span>
                    </button>
                  )}
                </div>
              )}
            </div>
          )}
        </div>

        {/* === CỘT PHẢI (30%): SIDEBAR TÀI CHÍNH & HÀNH ĐỘNG THEO ROLE === */}
        <div className="space-y-6">
          {/* Card 1: Hộp Tài chính & Hạn nộp */}
          <div className="bg-white rounded-2xl border border-slate-200 shadow-xs p-6 space-y-4">
            <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400">
              Thông số gói thầu
            </h3>

            {/* Ngân sách */}
            <div className="p-4 bg-slate-50/80 rounded-xl border border-slate-100 space-y-1">
              <span className="text-xs text-slate-500 font-medium">Ngân sách dự toán</span>
              <p className="text-xl font-black text-slate-900 font-mono tracking-tight">
                {formatVND(pkg.budget)}
              </p>
            </div>

            {/* Hạn nộp & đếm ngược */}
            <div className="space-y-2 pt-2 border-t border-slate-100 text-xs">
              <div className="flex items-center justify-between text-slate-500">
                <span className="flex items-center space-x-1.5">
                  <Clock className="w-3.5 h-3.5 text-slate-400" />
                  <span>Hạn nộp thầu:</span>
                </span>
                <span className="font-semibold text-slate-800">{formatDate(pkg.deadline)}</span>
              </div>

              <div
                className={`p-2.5 rounded-xl border text-center font-medium ${
                  deadlineInfo.isExpired
                    ? 'bg-rose-50 text-rose-700 border-rose-100'
                    : 'bg-emerald-50 text-emerald-700 border-emerald-100'
                }`}
              >
                {deadlineInfo.text}
              </div>
            </div>

            {/* Thống kê hồ sơ */}
            <div className="pt-2 border-t border-slate-100 flex items-center justify-between text-xs text-slate-500">
              <span>Số hồ sơ đã tiếp nhận:</span>
              <span className="font-mono font-bold text-slate-800">{pkg.submissionsCount || 0}</span>
            </div>
          </div>

          {/* Card 2: HỘP HÀNH ĐỘNG THEO VAI TRÒ (Bám sát Role Matrix) */}
          <div className="bg-white rounded-2xl border border-slate-200 shadow-xs p-6 space-y-4">
            <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400">
              Thao tác nghiệp vụ
            </h3>

            {/* 1. VAI TRÒ ADMIN & PROCUREMENT: Quick Action chuyển trạng thái */}
            {canManageStatus && (
              <div className="space-y-3">
                <div className="text-xs text-slate-600">
                  <span className="font-semibold">Trạng thái hiện tại: </span>
                  <StatusBadge status={displayStatus} />
                </div>

                {isOpen && (
                  <div className="space-y-2">
                    <button
                      onClick={handleOpenStatusModal}
                      className="w-full py-2.5 px-4 bg-amber-500 hover:bg-amber-600 text-white rounded-xl text-xs font-bold transition shadow-xs flex items-center justify-center space-x-2"
                    >
                      <Lock className="w-4 h-4" />
                      <span>Đóng nhận thầu</span>
                    </button>
                    <p className="text-[11px] text-slate-400 text-center">
                      Ngừng nhận hồ sơ để chuẩn bị chuyển sang giai đoạn chấm điểm.
                    </p>
                  </div>
                )}

                {isClosed && (
                  <div className="space-y-2">
                    <button
                      onClick={handleOpenStatusModal}
                      className="w-full py-2.5 px-4 bg-sky-600 hover:bg-sky-700 text-white rounded-xl text-xs font-bold transition shadow-xs flex items-center justify-center space-x-2 cursor-pointer"
                    >
                      <Scale className="w-4 h-4" />
                      <span>Chuyển sang Chấm điểm</span>
                    </button>
                    {totalWeight !== 100 ? (
                      <p className="text-[11px] text-amber-600 font-medium text-center">
                        Cần hoàn thiện tiêu chí đúng 100% trước khi mở chấm thầu (Hiện tại: {totalWeight}%).
                      </p>
                    ) : !isCommitteeValid ? (
                      <p className="text-[11px] text-amber-600 font-medium text-center">
                        Tổ chuyên gia chưa đủ điều kiện: cần tối thiểu 3 thành viên và là số lẻ (Hiện tại: {evaluators.length} thành viên).
                      </p>
                    ) : (
                      <p className="text-[11px] text-slate-400 text-center">
                        Mở quyền truy cập phòng chấm thầu cho Tổ chuyên gia đã phân công ({evaluators.length} thành viên).
                      </p>
                    )}
                  </div>
                )}

                {isEvaluating && (
                  <div className="space-y-2">
                    {isAdmin ? (
                      <Link
                        to={`/evaluation/${pkg.id}`}
                        className="w-full py-2.5 px-4 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl text-xs font-bold transition shadow-xs flex items-center justify-center space-x-2"
                      >
                        <Scale className="w-4 h-4" />
                        <span>Vào phòng chấm & phê duyệt trúng thầu</span>
                      </Link>
                    ) : (
                      <div className="p-3 bg-indigo-50 border border-indigo-100 rounded-xl text-xs text-indigo-700 text-center">
                        Gói thầu đang trong giai đoạn chấm điểm bởi Ban giám khảo.
                      </div>
                    )}
                  </div>
                )}

                {isContracted && (
                  <div className="space-y-2">
                    <div className="p-3 bg-emerald-50 border border-emerald-100 rounded-xl text-xs text-emerald-700 text-center font-medium">
                      Gói thầu đã hoàn tất đấu thầu và ký hợp đồng.
                    </div>
                    <Link
                      to="/contracts"
                      className="w-full py-2 px-3 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-xl text-xs font-bold transition flex items-center justify-center space-x-1.5"
                    >
                      <span>Xem danh sách hợp đồng</span>
                      <ExternalLink className="w-3.5 h-3.5" />
                    </Link>
                  </div>
                )}
              </div>
            )}

            {/* 2. VAI TRÒ EVALUATOR: Nút vào phòng chấm điểm */}
            {isEvaluator && (
              <div className="space-y-3">
                {isEvaluating ? (
                  isAssignedEvaluator || isAdmin ? (
                    <div className="space-y-2">
                      <Link
                        to={`/evaluation/${pkg.id}`}
                        className="w-full py-3 px-4 bg-sky-600 hover:bg-sky-700 text-white rounded-xl text-xs font-bold transition shadow-sm flex items-center justify-center space-x-2"
                      >
                        <Scale className="w-4 h-4" />
                        <span>Vào phòng chấm điểm gói thầu</span>
                      </Link>
                      <p className="text-[11px] text-emerald-600 font-medium text-center">
                        Bạn là thành viên trong Tổ chuyên gia chấm điểm gói thầu này.
                      </p>
                    </div>
                  ) : (
                    <div className="p-3 bg-amber-50 border border-amber-200 rounded-xl text-xs text-amber-800 text-center">
                      <p className="font-semibold">Bạn không thuộc Tổ chuyên gia</p>
                      <p className="text-[11px] text-amber-700 mt-1">
                        Chỉ các giám khảo được chỉ định trong Tổ chuyên gia mới có quyền chấm điểm gói thầu này.
                      </p>
                    </div>
                  )
                ) : (
                  <div className="p-3.5 bg-slate-50 border border-slate-200 rounded-xl text-xs text-slate-500 text-center">
                    Gói thầu hiện chưa ở giai đoạn chấm điểm (Trạng thái: <strong>{pkg.status}</strong>).
                  </div>
                )}
              </div>
            )}

            {/* 3. VAI TRÒ CONTRACTOR: Xem tình trạng nộp / Nút nộp thầu */}
            {isContractor && (
              <div className="space-y-3">
                {mySubmission ? (
                  // Đã nộp hồ sơ
                  <div className="p-4 bg-emerald-50/70 border border-emerald-200 rounded-xl space-y-3">
                    <div className="flex items-center space-x-2 text-emerald-800 font-bold text-xs">
                      <CheckCircle2 className="w-4 h-4 text-emerald-600 flex-shrink-0" />
                      <span>Bạn đã nộp hồ sơ dự thầu</span>
                    </div>

                    <div className="text-[11px] text-emerald-700 space-y-1">
                      <p>Thời gian nộp: <strong>{formatDateTime(mySubmission.submittedAt)}</strong></p>
                      <p>Số tài liệu đính kèm: <strong>{mySubmission.fileCount} tệp</strong></p>
                      <p>Trạng thái: <strong>{mySubmission.status || 'Đã ghi nhận'}</strong></p>
                    </div>

                    {/* Cho phép rút hồ sơ nếu gói thầu còn đang Open */}
                    {isOpen && !deadlineInfo.isExpired && (
                      <button
                        onClick={() => setWithdrawModal(true)}
                        className="w-full mt-2 py-1.5 px-3 bg-white border border-rose-200 hover:bg-rose-50 text-rose-600 rounded-lg text-xs font-semibold transition"
                      >
                        Rút hồ sơ dự thầu
                      </button>
                    )}
                  </div>
                ) : (
                  // Chưa nộp hồ sơ
                  <div className="space-y-3">
                    {isOpen && !deadlineInfo.isExpired ? (
                      <div className="space-y-2">
                        <button
                          onClick={() => setIsSubmitBidModalOpen(true)}
                          className="w-full py-3 px-4 bg-sky-600 hover:bg-sky-700 text-white rounded-xl text-xs font-bold transition shadow-xs flex items-center justify-center space-x-2"
                        >
                          <Send className="w-4 h-4" />
                          <span>Nộp hồ sơ dự thầu ngay</span>
                        </button>
                        <p className="text-[11px] text-slate-400 text-center">
                          Vui lòng chuẩn bị file đề xuất kỹ thuật và bảng giá dự thầu.
                        </p>
                      </div>
                    ) : (
                      <div className="p-3.5 bg-slate-50 border border-slate-200 rounded-xl text-xs text-slate-500 text-center">
                        Gói thầu đã đóng hoặc hết hạn tiếp nhận hồ sơ mới.
                      </div>
                    )}
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* 4. MODALS */}
      {/* Quick Action Status Change Confirm Modal */}
      <ConfirmModal
        isOpen={statusModal.isOpen}
        onClose={() => setStatusModal((prev) => ({ ...prev, isOpen: false }))}
        onConfirm={handleConfirmStatusChange}
        title={statusModal.title}
        message={statusModal.message}
        confirmText="Xác nhận chuyển"
        cancelText="Hủy bỏ"
        confirmVariant={statusModal.confirmVariant}
        isLoading={statusLoading}
      />

      {/* Withdraw Submission Confirm Modal */}
      <ConfirmModal
        isOpen={withdrawModal}
        onClose={() => setWithdrawModal(false)}
        onConfirm={handleWithdrawSubmission}
        title="Xác nhận rút hồ sơ dự thầu"
        message="Bạn có chắc chắn muốn rút hồ sơ đã nộp cho gói thầu này? Sau khi rút, bạn vẫn có thể nộp lại hồ sơ mới nếu gói thầu còn hạn tiếp nhận."
        confirmText="Xác nhận rút hồ sơ"
        cancelText="Hủy bỏ"
        confirmVariant="danger"
        isLoading={withdrawLoading}
      />

      {/* View Submission Detail Modal */}
      {selectedSubmission && (
        <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/50 backdrop-blur-xs flex items-center justify-center p-4">
          <div
            className="bg-white rounded-2xl max-w-lg w-full p-6 shadow-xl border border-slate-100 relative space-y-4"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between pb-3 border-b border-slate-100">
              <div>
                <h3 className="text-base font-bold text-slate-900">Chi tiết hồ sơ dự thầu</h3>
                <p className="text-xs text-slate-500 font-mono">
                  {selectedSubmission.companyName || 'Nhà thầu #' + selectedSubmission.contractorId}
                </p>
              </div>
              <button
                onClick={() => setSelectedSubmission(null)}
                className="text-slate-400 hover:text-slate-600 p-1 rounded-lg hover:bg-slate-100 transition"
              >
                ✕
              </button>
            </div>

            <div className="space-y-3 text-xs">
              <div className="grid grid-cols-3 gap-2 p-3 bg-slate-50 rounded-xl">
                <div>
                  <span className="text-slate-400">Thời gian nộp:</span>
                  <p className="font-semibold text-slate-800">{formatDateTime(selectedSubmission.submittedAt)}</p>
                </div>
                <div>
                  <span className="text-slate-400">Trạng thái:</span>
                  <p className="font-semibold text-slate-800">{selectedSubmission.status}</p>
                </div>
                <div>
                  <span className="text-slate-400">Giá dự thầu:</span>
                  <p className="font-semibold text-slate-900 font-mono">
                    {(() => {
                      const isPublic = pkg?.status === 'Awarded' || pkg?.status === 'Contracted' || String(pkg?.status) === '3' || String(pkg?.status) === '4';
                      const canEvaluatorView = pkg?.status === 'Evaluating' && (isEvaluator || isAssignedEvaluator || isAdmin || isProcurement);
                      if (isPublic) return <span className="text-emerald-700 font-bold">{selectedSubmission.bidPrice ? formatCurrency(selectedSubmission.bidPrice) : '-'}</span>;
                      if (canEvaluatorView) return <span className="text-sky-700 font-bold">{selectedSubmission.bidPrice ? formatCurrency(selectedSubmission.bidPrice) : '-'}</span>;
                      return <span className="text-amber-600 text-[11px]">Niêm phong</span>;
                    })()}
                  </p>
                </div>
              </div>

              {/* Danh sách tệp đính kèm trong hồ sơ */}
              <div className="space-y-2">
                {(() => {
                  const submissionFiles = selectedSubmission.files || selectedSubmission.submissionFiles || [];
                  return (
                    <>
                      <span className="font-bold text-slate-800">
                        Tệp tài liệu đính kèm ({submissionFiles.length}):
                      </span>
                      {submissionFiles.length > 0 ? (
                        <div className="divide-y divide-slate-100 border border-slate-100 rounded-xl overflow-hidden max-h-56 overflow-y-auto">
                          {submissionFiles.map((file) => {
                            const fileTypeLabel =
                              file.fileTypeName === 'Quotation' || file.fileType === 0
                                ? 'Báo giá tài chính'
                                : file.fileTypeName === 'Capability' || file.fileType === 1
                                ? 'Hồ sơ năng lực'
                                : file.fileTypeName === 'Schedule' || file.fileType === 2
                                ? 'Tiến độ thực hiện'
                                : file.fileTypeName || (file.fileType !== undefined ? `Loại #${file.fileType}` : 'Tài liệu');

                            return (
                              <div
                                key={file.id}
                                className="p-3 flex items-center justify-between hover:bg-slate-50 transition"
                              >
                                <div className="truncate pr-3">
                                  <p
                                    className="font-medium text-slate-800 truncate"
                                    title={file.fileName}
                                  >
                                    {file.fileName}
                                  </p>
                                  <div className="flex items-center gap-2 mt-0.5">
                                    <span className="text-[10px] font-semibold px-2 py-0.5 rounded-md bg-sky-50 text-sky-700 border border-sky-100">
                                      {fileTypeLabel}
                                    </span>
                                    {file.uploadedAt && (
                                      <span className="text-[10px] text-slate-400 font-mono">
                                        {formatDateTime(file.uploadedAt)}
                                      </span>
                                    )}
                                  </div>
                                </div>
                                <button
                                  type="button"
                                  onClick={() => downloadSecureFile(`/api/submissions/files/${file.id}/download`, file.fileName)}
                                  className="px-3 py-1.5 bg-slate-100 hover:bg-sky-600 text-slate-700 hover:text-white rounded-xl text-xs font-semibold transition flex items-center space-x-1.5 shrink-0 cursor-pointer"
                                >
                                  <Download className="w-3.5 h-3.5" />
                                  <span>Tải</span>
                                </button>
                              </div>
                            );
                          })}
                        </div>
                      ) : (
                        <p className="text-slate-400 italic py-2">Không có tệp đính kèm.</p>
                      )}
                    </>
                  );
                })()}
              </div>
            </div>

            <div className="pt-2 flex justify-end">
              <button
                onClick={() => setSelectedSubmission(null)}
                className="px-4 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-semibold rounded-xl transition"
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Criteria Management Modals */}
      <CriteriaModal
        isOpen={criteriaModalState.isOpen}
        onClose={() => setCriteriaModalState({ isOpen: false, initialData: null })}
        packageId={pkg?.id}
        initialData={criteriaModalState.initialData}
        currentTotalWeight={totalWeight}
        onSuccess={loadPackageData}
      />

      <CriteriaTemplateModal
        isOpen={isTemplateModalOpen}
        onClose={() => setIsTemplateModalOpen(false)}
        packageId={pkg?.id}
        existingCriteria={criteria}
        onSuccess={loadPackageData}
      />

      <ConfirmModal
        isOpen={deleteCriteriaModalState.isOpen}
        onClose={() =>
          setDeleteCriteriaModalState({ isOpen: false, criteriaId: null, criteriaName: '' })
        }
        onConfirm={handleConfirmDeleteCriteria}
        title="Xác nhận xóa tiêu chí đánh giá"
        message={`Bạn có chắc chắn muốn xóa tiêu chí "${deleteCriteriaModalState.criteriaName}" khỏi gói thầu?`}
        confirmText="Xóa tiêu chí"
        cancelText="Hủy bỏ"
        confirmVariant="danger"
        isLoading={deleteCriteriaLoading}
      />

      {/* Committee Evaluator Management Modals */}
      <AssignEvaluatorModal
        isOpen={isAssignEvaluatorModalOpen}
        onClose={() => setIsAssignEvaluatorModalOpen(false)}
        packageId={pkg?.id}
        existingEvaluators={evaluators}
        onSuccess={loadPackageData}
      />

      <ConfirmModal
        isOpen={deleteEvaluatorModalState.isOpen}
        onClose={() => setDeleteEvaluatorModalState({ isOpen: false, evaluator: null })}
        onConfirm={handleConfirmDeleteEvaluator}
        title="Xác nhận xóa giám khảo khỏi Tổ chuyên gia"
        message={`Bạn có chắc chắn muốn xóa giám khảo "${deleteEvaluatorModalState.evaluator?.fullName}" khỏi Tổ chuyên gia của gói thầu này?`}
        confirmText="Xóa thành viên"
        cancelText="Hủy bỏ"
        confirmVariant="danger"
        isLoading={deleteEvaluatorLoading}
      />

      {/* Contractor Submit Bid Modal */}
      <SubmitBidModal
        isOpen={isSubmitBidModalOpen}
        onClose={() => setIsSubmitBidModalOpen(false)}
        pkg={pkg}
        onSuccess={loadPackageData}
      />
    </div>
  );
};

export default PackageDetailPage;
