import React, { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import {
  ArrowLeft,
  Calendar,
  Clock,
  FileText,
  Building,
  CheckCircle2,
  AlertCircle,
  Scale,
  Lock,
  FileCheck,
  Send,
  ExternalLink,
  Users,
  RefreshCw,
  HelpCircle,
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
import ReopenPackageModal from '../components/packages/ReopenPackageModal';
import SubmitBidModal from '../components/submissions/SubmitBidModal';
import DisqualifySubmissionModal from '../components/evaluation/DisqualifySubmissionModal';
import ClarificationTab from '../components/clarifications/ClarificationTab';
import ReviewApprovalModal from '../components/packages/ReviewApprovalModal';
import PackageOverviewTab from '../components/packages/PackageOverviewTab';
import PackageCriteriaTab from '../components/packages/PackageCriteriaTab';
import PackageSubmissionsTab from '../components/packages/PackageSubmissionsTab';
import PackageCommitteeTab from '../components/packages/PackageCommitteeTab';
import SubmissionDetailModal from '../components/packages/SubmissionDetailModal';
import { PACKAGE_TYPES, ROLES } from '../utils/constants';
import { formatVND, formatDate, formatDateTime } from '../utils/formatters';

const WEIGHT_UNIT = '\u0025';

const STATUS_MODAL_BUILDERS = {
  Open: (pkg) => {
    const isEarly = pkg.requiresEarlyCloseReason === true;
    return {
      isOpen: true,
      targetStatus: 'Closed',
      title: isEarly ? 'Đóng nhận thầu sớm' : 'Xác nhận đóng nhận hồ sơ thầu',
      message: isEarly
        ? 'Gói thầu vẫn đang trong thời hạn tiếp nhận hồ sơ. Bạn có chắc chắn muốn đóng nhận thầu sớm để chuyển sang bước chấm điểm?'
        : 'Bạn có chắc chắn muốn đóng nhận hồ sơ gói thầu này? Sau khi đóng, hệ thống sẽ ngừng nhận hồ sơ mới từ nhà thầu.',
      confirmVariant: 'warning',
      confirmText: isEarly ? 'Đóng nhận thầu ngay' : 'Xác nhận đóng thầu',
      requiresReason: isEarly,
      reason: '',
    };
  },
  Closed: (pkg) => {
    if (!pkg.canStartEvaluation) {
      toast.error(pkg.evaluationBlockingReasons?.join(' ') || 'Gói thầu chưa đủ điều kiện chấm điểm.');
      return null;
    }
    return {
      isOpen: true,
      targetStatus: 'Evaluating',
      title: 'Chuyển sang giai đoạn chấm điểm',
      message:
        'Bạn có chắc chắn muốn chuyển gói thầu sang giai đoạn Chấm điểm? Các giám khảo trong Tổ chuyên gia sẽ có quyền truy cập để đánh giá hồ sơ.',
      confirmVariant: 'primary',
      confirmText: 'Bắt đầu chấm điểm',
      requiresReason: false,
      reason: '',
    };
  },
};
STATUS_MODAL_BUILDERS['0'] = STATUS_MODAL_BUILDERS.Open;
STATUS_MODAL_BUILDERS['1'] = STATUS_MODAL_BUILDERS.Closed;

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

export const PackageDetailPage = () => {
  const { id } = useParams();
  const { user, hasRole } = useAuth();

  // State dữ liệu
  const [pkg, setPkg] = useState(null);
  const [criteria, setCriteria] = useState([]);
  const [submissions, setSubmissions] = useState([]);
  const [mySubmission, setMySubmission] = useState(null);
  const [loading, setLoading] = useState(true);

  // Status Approval Workflow States (Thẩm quyền phê duyệt đóng/mở/hủy thầu)
  const [approvalRequests, setApprovalRequests] = useState([]);
  const [selectedApprovalRequest, setSelectedApprovalRequest] = useState(null);
  const [isApprovalModalOpen, setIsApprovalModalOpen] = useState(false);

  // Tab navigation
  const [activeTab, setActiveTab] = useState('info');

  // Quick Action Modal State (Admin / Procurement)
  const [statusModal, setStatusModal] = useState({
    isOpen: false,
    targetStatus: null,
    title: '',
    message: '',
    confirmVariant: 'primary',
    confirmText: 'Xác nhận',
    requiresReason: false,
    reason: '',
  });
  const [statusLoading, setStatusLoading] = useState(false);
  const [isReopenModalOpen, setIsReopenModalOpen] = useState(false);

  // Withdraw Submission Modal State (Contractor)
  const [withdrawModal, setWithdrawModal] = useState(false);
  const [withdrawLoading, setWithdrawLoading] = useState(false);

  // Submit Bid Modal State (Contractor)
  const [isSubmitBidModalOpen, setIsSubmitBidModalOpen] = useState(false);

  // View Submission Details Modal (Admin/Procurement/Evaluator/Contractor)
  const [selectedSubmission, setSelectedSubmission] = useState(null);
  const [, setSubmissionDetailLoading] = useState(false);
  const [disqualifyTarget, setDisqualifyTarget] = useState(null);

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

  // Vai trò người dùng
  const isAdmin = hasRole(ROLES.ADMIN);
  const isProcurement = hasRole(ROLES.PROCUREMENT);
  const isEvaluator = hasRole(ROLES.EVALUATOR);
  const isContractor = hasRole(ROLES.CONTRACTOR);
  const canManageStatus =
    typeof pkg?.canManagePackage === 'boolean'
      ? pkg.canManagePackage
      : isAdmin || (isProcurement && Number(pkg?.createdBy) === Number(user?.id));
  const canViewSubmissions = isAdmin || isProcurement || isEvaluator;

  // Kiểm tra chuyên gia được phân công vào Tổ chuyên gia gói thầu
  const isAssignedEvaluator = Boolean(
    user?.id &&
      ((Array.isArray(pkg?.evaluatorIds) && pkg.evaluatorIds.includes(user.id)) ||
        evaluators.some(
          (e) =>
            (e.evaluatorId === user.id || e.userId === user.id || e.id === user.id) &&
            e.isActive !== false
        ))
  );

  // Kiểm tra tính hợp lệ của Tổ chuyên gia do Backend trả về
  const isCommitteeValid = pkg?.isCommitteeValid === true;
  const canManageCommittee = canManageStatus && pkg?.canEditCommittee === true;

  // Tải dữ liệu ban đầu
  const loadPackageData = async () => {
    try {
      setLoading(true);
      const resPkg = await bidPackageApi.getPackageById(id);
      if (resPkg?.data) {
        setPkg(resPkg.data);
      }

      // 1. Tải tiêu chí đánh giá
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

      // 3. Nếu là Nhà thầu (Contractor): Truy vấn trực tiếp hồ sơ của nhà thầu cho gói thầu này từ Backend
      if (hasRole(ROLES.CONTRACTOR)) {
        try {
          const resMySub = await bidSubmissionApi.getMySubmissionByPackage(id);
          setMySubmission(resMySub?.data || null);
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

      // 5. Tải danh sách yêu cầu phê duyệt trạng thái gói thầu (Admin, Procurement)
      if (hasRole([ROLES.ADMIN, ROLES.PROCUREMENT])) {
        try {
          const resApprovals = await bidPackageApi.getStatusApprovals(id);
          if (resApprovals?.data) {
            setApprovalRequests(resApprovals.data);
          }
        } catch (err) {
          console.warn('Lỗi tải yêu cầu phê duyệt trạng thái:', err);
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

  // Xử lý Quick Action chuyển trạng thái kế tiếp qua bảng tra cứu STATUS_MODAL_BUILDERS
  const handleOpenStatusModal = () => {
    if (!pkg) return;
    const statusStr = String(pkg.status ?? '');
    const builder = STATUS_MODAL_BUILDERS[statusStr];
    if (!builder) return;

    const modalConfig = builder(pkg);
    if (modalConfig) {
      setStatusModal(modalConfig);
    }
  };

  const handleConfirmStatusChange = async () => {
    if (!statusModal.targetStatus) return;

    if (statusModal.requiresReason && (!statusModal.reason || !statusModal.reason.trim())) {
      toast.error('Vui lòng nhập lý do đóng thầu sớm.');
      return;
    }

    try {
      setStatusLoading(true);
      const res = await bidPackageApi.changeStatus(
        pkg.id,
        statusModal.targetStatus,
        statusModal.reason?.trim() || null
      );
      toast.success(res?.message || 'Thao tác cập nhật trạng thái hoàn tất');
      setStatusModal((prev) => ({ ...prev, isOpen: false, reason: '' }));
      loadPackageData();
    } catch (error) {
      toast.error(error.message || 'Không thể cập nhật trạng thái');
    } finally {
      setStatusLoading(false);
    }
  };

  // Quản lý Tiêu chí đánh giá (Admin & Procurement)
  const totalWeight = criteria.reduce((sum, c) => sum + Number(c.weight || 0), 0);

  const handleOpenAddCriteria = () => {
    if (totalWeight >= 100) {
      toast.error(
        `Gói thầu đã đạt tối đa 100${WEIGHT_UNIT} trọng số. Vui lòng chỉnh sửa hoặc xóa bớt tiêu chí hiện có.`
      );
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
  const statusStr = String(pkg.status ?? '');
  const isOpen = statusStr === 'Open' || statusStr === '0';
  const isClosed = statusStr === 'Closed' || statusStr === '1';
  const isEvaluating = statusStr === 'Evaluating' || statusStr === '2';
  const isContracted = statusStr === 'Contracted' || statusStr === '3';
  const isAwarded = Boolean(pkg.isAwarded || statusStr === 'Awarded');
  const displayStatus = isContracted ? 'Contracted' : isAwarded ? 'Awarded' : pkg.status;
  const canEditCriteria = canManageStatus && (isOpen || isClosed);
  const pendingApproval = approvalRequests.find((r) => r.status === 'Pending' || r.status === 0);

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

      {/* Banner Cảnh báo Yêu cầu Phê duyệt Trạng thái đang chờ xử lý */}
      {pendingApproval && (
        <div className="p-4 rounded-2xl bg-amber-50/80 border border-amber-200 flex flex-col sm:flex-row sm:items-center justify-between gap-4 animate-fade-in shadow-xs">
          <div className="flex items-start sm:items-center gap-3">
            <div className="p-2.5 bg-amber-100 text-amber-700 rounded-xl shrink-0 mt-0.5 sm:mt-0">
              <AlertCircle className="w-5 h-5 animate-pulse" />
            </div>
            <div>
              <h4 className="text-xs sm:text-sm font-bold text-amber-950">
                Gói thầu đang có yêu cầu thay đổi trạng thái chờ Chủ đầu tư / Admin phê duyệt
              </h4>
              <p className="text-xs text-amber-900/90 mt-0.5">
                Bên mời thầu (<strong className="text-slate-900 font-bold">{pendingApproval.requestedByUserName}</strong>) đề xuất:{' '}
                <span className="font-bold text-amber-950 px-1.5 py-0.5 bg-amber-100/80 border border-amber-300/60 rounded">
                  {pendingApproval.targetStatusName || pendingApproval.targetStatus}
                </span>
                . Căn cứ: "{pendingApproval.reason}"
              </p>
            </div>
          </div>
          <button
            onClick={() => {
              setSelectedApprovalRequest(pendingApproval);
              setIsApprovalModalOpen(true);
            }}
            className="px-4 py-2 rounded-xl text-xs font-bold bg-amber-600 text-white hover:bg-amber-700 transition-all shrink-0 shadow-xs cursor-pointer"
          >
            {isAdmin ? 'Xem xét & Phê duyệt ngay' : 'Xem chi tiết yêu cầu'}
          </button>
        </div>
      )}

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
              <span>
                Chủ đầu tư: <strong className="text-slate-700">{pkg.createdByName || 'Hệ thống'}</strong>
              </span>
            </span>
          </div>
        </div>

        <h1 className="text-xl md:text-2xl font-black text-slate-900 leading-snug">{pkg.name}</h1>
      </div>

      {/* 3. Main Grid Layout */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 items-start">
        {/* === CỘT TRÁI: NỘI DUNG TABS === */}
        <div className="lg:col-span-2 space-y-6">
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
                <span>Hồ sơ đã nộp {isOpen ? '(Niêm phong)' : `(${submissions.length})`}</span>
              </button>
            )}

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

            <button
              onClick={() => setActiveTab('clarifications')}
              className={`flex-1 py-2 px-3 text-xs font-bold rounded-xl transition flex items-center justify-center space-x-1.5 ${
                activeTab === 'clarifications'
                  ? 'bg-sky-600 text-white shadow-xs'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-slate-50'
              }`}
            >
              <HelpCircle className="w-4 h-4" />
              <span>Hỏi - Đáp & Làm rõ (Q&A)</span>
            </button>
          </div>

          {activeTab === 'info' && <PackageOverviewTab pkg={pkg} isContractor={isContractor} />}

          {activeTab === 'criteria' && (
            <PackageCriteriaTab
              criteria={criteria}
              totalWeight={totalWeight}
              canEditCriteria={canEditCriteria}
              onOpenTemplateModal={() => setIsTemplateModalOpen(true)}
              onOpenAddCriteria={handleOpenAddCriteria}
              onOpenEditCriteria={handleOpenEditCriteria}
              onOpenDeleteCriteria={handleOpenDeleteCriteria}
            />
          )}

          {activeTab === 'submissions' && canViewSubmissions && (
            <PackageSubmissionsTab
              pkg={pkg}
              submissions={submissions}
              isOpen={isOpen}
              canManageStatus={canManageStatus}
              isEvaluator={isEvaluator}
              isAssignedEvaluator={isAssignedEvaluator}
              isAdmin={isAdmin}
              isProcurement={isProcurement}
              onOpenStatusModal={handleOpenStatusModal}
              onViewSubmissionDetail={handleViewSubmissionDetail}
              onSelectDisqualifyTarget={setDisqualifyTarget}
            />
          )}

          {activeTab === 'committee' && (
            <PackageCommitteeTab
              evaluators={evaluators}
              evaluatorsLoading={evaluatorsLoading}
              isCommitteeValid={isCommitteeValid}
              canManageCommittee={canManageCommittee}
              isEvaluating={isEvaluating}
              currentUserId={user?.id}
              onOpenAssignModal={() => setIsAssignEvaluatorModalOpen(true)}
              onOpenDeleteEvaluator={handleOpenDeleteEvaluator}
            />
          )}

          {activeTab === 'clarifications' && (
            <ClarificationTab
              packageId={Number(id)}
              packageCode={pkg.code}
              isAdmin={isAdmin}
              isProcurement={isProcurement}
              isEvaluator={isEvaluator}
              isContractor={isContractor}
              submissions={submissions}
              mySubmission={mySubmission}
            />
          )}
        </div>

        {/* === CỘT PHẢI: SIDEBAR TÀI CHÍNH & HÀNH ĐỘNG THEO ROLE === */}
        <div className="space-y-6">
          <div className="bg-white rounded-2xl border border-slate-200 shadow-xs p-6 space-y-4">
            <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400">
              Thông số gói thầu
            </h3>

            <div className="p-4 bg-slate-50/80 rounded-xl border border-slate-100 space-y-1">
              <span className="text-xs text-slate-500 font-medium">Ngân sách dự toán</span>
              <p className="text-xl font-black text-slate-900 font-mono tracking-tight">
                {formatVND(pkg.budget)}
              </p>
            </div>

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

            <div className="pt-2 border-t border-slate-100 flex items-center justify-between text-xs text-slate-500">
              <span>Số hồ sơ đã tiếp nhận:</span>
              <span className="font-mono font-bold text-slate-800">{pkg.submissionsCount || 0}</span>
            </div>
          </div>

          <div className="bg-white rounded-2xl border border-slate-200 shadow-xs p-6 space-y-4">
            <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400">
              Thao tác nghiệp vụ
            </h3>

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

                    <button
                      type="button"
                      onClick={() => setIsReopenModalOpen(true)}
                      className="w-full py-2.5 px-4 bg-amber-50 hover:bg-amber-100 text-amber-700 border border-amber-200 rounded-xl text-xs font-bold transition shadow-2xs flex items-center justify-center space-x-2 cursor-pointer"
                    >
                      <RefreshCw className="w-4 h-4" />
                      <span>Mở lại & Gia hạn nộp thầu</span>
                    </button>

                    {totalWeight !== 100 ? (
                      <p className="text-[11px] text-amber-600 font-medium text-center">
                        {`Cần hoàn thiện tiêu chí đúng 100${WEIGHT_UNIT} trước khi mở chấm thầu (Hiện tại: ${totalWeight}${WEIGHT_UNIT}).`}
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

            {isContractor && (
              <div className="space-y-3">
                {mySubmission ? (
                  <div className="p-4 bg-emerald-50/70 border border-emerald-200 rounded-xl space-y-3">
                    <div className="flex items-center space-x-2 text-emerald-800 font-bold text-xs">
                      <CheckCircle2 className="w-4 h-4 text-emerald-600 flex-shrink-0" />
                      <span>Bạn đã nộp hồ sơ dự thầu</span>
                    </div>

                    <div className="text-[11px] text-emerald-700 space-y-1">
                      <p>
                        Thời gian nộp: <strong>{formatDateTime(mySubmission.submittedAt)}</strong>
                      </p>
                      <p>
                        Số tài liệu đính kèm: <strong>{mySubmission.fileCount} tệp</strong>
                      </p>
                      <p>
                        Trạng thái: <strong>{mySubmission.status || 'Đã ghi nhận'}</strong>
                      </p>
                    </div>

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
      <ConfirmModal
        isOpen={statusModal.isOpen}
        onClose={() => setStatusModal((prev) => ({ ...prev, isOpen: false, reason: '' }))}
        onConfirm={handleConfirmStatusChange}
        title={statusModal.title}
        message={statusModal.message}
        confirmText={statusModal.confirmText || 'Xác nhận'}
        cancelText="Hủy bỏ"
        confirmVariant={statusModal.confirmVariant}
        isLoading={statusLoading}
      >
        {statusModal.requiresReason && (
          <div className="pt-2 space-y-2 text-left">
            <label className="block text-xs font-semibold text-slate-700">
              Lý do đóng thầu sớm <span className="text-rose-500">*</span>
            </label>
            <textarea
              rows={3}
              value={statusModal.reason || ''}
              onChange={(e) => setStatusModal((prev) => ({ ...prev, reason: e.target.value }))}
              placeholder="Nhập lý do đóng nhận thầu (ví dụ: đã tiếp nhận đủ hồ sơ theo kế hoạch, rút ngắn thời gian để chuyển bước chấm điểm...)"
              className="w-full px-3 py-2 text-xs border border-slate-300 rounded-xl focus:ring-2 focus:ring-amber-500 focus:border-amber-500 transition resize-none outline-none text-slate-800 placeholder-slate-400"
              autoFocus
            />
            <p className="text-[11px] text-slate-400">
              Lý do này sẽ được ghi nhận vào lịch sử hoạt động của gói thầu.
            </p>
          </div>
        )}
      </ConfirmModal>

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

      <SubmissionDetailModal
        submission={selectedSubmission}
        pkgStatus={pkg?.status}
        isEvaluator={isEvaluator}
        isAssignedEvaluator={isAssignedEvaluator}
        isAdmin={isAdmin}
        isProcurement={isProcurement}
        onClose={() => setSelectedSubmission(null)}
      />

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

      <AssignEvaluatorModal
        isOpen={isAssignEvaluatorModalOpen}
        onClose={() => setIsAssignEvaluatorModalOpen(false)}
        packageId={pkg?.id}
        pkg={pkg}
        submissions={submissions}
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

      <SubmitBidModal
        isOpen={isSubmitBidModalOpen}
        onClose={() => setIsSubmitBidModalOpen(false)}
        pkg={pkg}
        onSuccess={loadPackageData}
      />

      <DisqualifySubmissionModal
        isOpen={Boolean(disqualifyTarget)}
        onClose={() => setDisqualifyTarget(null)}
        submission={disqualifyTarget}
        onSuccess={loadPackageData}
      />

      <ReopenPackageModal
        isOpen={isReopenModalOpen}
        onClose={() => setIsReopenModalOpen(false)}
        pkg={pkg}
        onSuccess={loadPackageData}
      />

      <ReviewApprovalModal
        isOpen={isApprovalModalOpen}
        onClose={() => {
          setIsApprovalModalOpen(false);
          setSelectedApprovalRequest(null);
        }}
        request={selectedApprovalRequest}
        isAdmin={isAdmin}
        onSuccess={loadPackageData}
      />
    </div>
  );
};

export default PackageDetailPage;
