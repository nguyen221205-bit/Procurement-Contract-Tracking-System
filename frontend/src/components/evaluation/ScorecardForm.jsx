import React, { useState, useEffect } from 'react';
import toast from 'react-hot-toast';
import {
  FileText,
  UserCheck,
  CheckCircle2,
  AlertCircle,
  Save,
  Calculator,
  Building2,
  Clock,
  Award,
  ChevronRight,
  Info,
  Lock,
  Eye,
  Download,
  ExternalLink
} from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { evaluationApi } from '../../api/evaluationApi';
import { bidSubmissionApi } from '../../api/bidSubmissionApi';
import { formatCurrency, formatDate, formatNumber } from '../../utils/formatters';
import { previewSecureFile, downloadSecureFile } from '../../utils/fileDownload';

/**
 * Hàm trợ giúp chuyển đổi loại tài liệu dự thầu sang tiếng Việt kèm style badge
 * - 0 hoặc 'Quotation': Báo giá dự thầu (emerald)
 * - 1 hoặc 'Capability': Hồ sơ năng lực (blue)
 * - 2 hoặc 'Schedule': Đề xuất kỹ thuật & Tiến độ (indigo / purple)
 * - 3 hoặc 'Other': Tài liệu khác (slate)
 */
const getFileTypeBadge = (fileType, fileTypeName) => {
  const typeStr = String(fileTypeName || fileType || '').toLowerCase();
  const typeNum = Number(fileType);

  if (
    typeNum === 0 ||
    typeStr.includes('quotation') ||
    typeStr.includes('báo giá') ||
    typeStr.includes('tài chính')
  ) {
    return {
      label: 'Báo giá dự thầu',
      badgeClass: 'bg-emerald-50 text-emerald-700 border-emerald-200',
    };
  }
  if (
    typeNum === 1 ||
    typeStr.includes('capability') ||
    typeStr.includes('năng lực')
  ) {
    return {
      label: 'Hồ sơ năng lực',
      badgeClass: 'bg-blue-50 text-blue-700 border-blue-200',
    };
  }
  if (
    typeNum === 2 ||
    typeStr.includes('schedule') ||
    typeStr.includes('tiến độ') ||
    typeStr.includes('kỹ thuật')
  ) {
    return {
      label: 'Đề xuất kỹ thuật & Tiến độ',
      badgeClass: 'bg-indigo-50 text-indigo-700 border-indigo-200',
    };
  }
  return {
    label: fileTypeName || 'Tài liệu khác',
    badgeClass: 'bg-slate-100 text-slate-700 border-slate-200',
  };
};

export const ScorecardForm = ({
  packageData,
  criteriaList = [],
  submissions = [],
  onScoreSaved,
  readOnly = false,
}) => {
  const { user, hasRole } = useAuth();
  const isAdminOrProcurement = hasRole(['Admin', 'Procurement']);

  const [selectedSubmissionId, setSelectedSubmissionId] = useState(
    submissions.length > 0 ? submissions[0].submissionId : null
  );
  // Evaluator selection: default to currently logged-in user
  const [viewEvaluatorId, setViewEvaluatorId] = useState(user?.id);
  const [scoresMap, setScoresMap] = useState({}); // { [criteriaId]: { score: number, comment: string } }
  const [saving, setSaving] = useState(false);
  const [submissionFiles, setSubmissionFiles] = useState([]);
  const [loadingFiles, setLoadingFiles] = useState(false);
  const [showFilesModal, setShowFilesModal] = useState(false);

  // Sync viewEvaluatorId with user.id when user loads
  useEffect(() => {
    if (user?.id && !viewEvaluatorId) {
      setViewEvaluatorId(user.id);
    }
  }, [user?.id]);

  // Sync selected submission when submissions list changes
  useEffect(() => {
    if (submissions.length > 0 && !selectedSubmissionId) {
      setSelectedSubmissionId(submissions[0].submissionId);
    }
  }, [submissions]);

  // Current selected submission
  const currentSubmission = submissions.find(
    (s) => s.submissionId === selectedSubmissionId
  );

  // Distinct evaluators who have scored this current submission
  const distinctEvaluators = [];
  const seenEvaluatorIds = new Set();
  (currentSubmission?.scores || []).forEach((s) => {
    if (!seenEvaluatorIds.has(s.evaluatorId)) {
      seenEvaluatorIds.add(s.evaluatorId);
      distinctEvaluators.push({
        id: s.evaluatorId,
        name: s.evaluatorName || `Giám khảo #${s.evaluatorId}`,
      });
    }
  });

  const targetEvaluatorId = viewEvaluatorId || user?.id;
  const isViewingOwnScorecard = Number(targetEvaluatorId) === Number(user?.id);
  const isFormReadOnly = readOnly || !isViewingOwnScorecard;

  // Lắng nghe phím Escape để đóng modal xem tài liệu
  useEffect(() => {
    if (!showFilesModal) return;

    const handleKeyDown = (e) => {
      if (e.key === 'Escape') {
        setShowFilesModal(false);
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [showFilesModal]);

  // When switching submission or target evaluator, load existing scores if any for THAT evaluator
  useEffect(() => {
    if (!currentSubmission || !criteriaList.length) return;

    const initialScores = {};
    criteriaList.forEach((c) => {
      // Find existing score strictly for targetEvaluatorId
      const existing = currentSubmission.scores?.find(
        (es) => es.criteriaId === c.id && es.evaluatorId === Number(targetEvaluatorId)
      );
      initialScores[c.id] = {
        score: existing ? existing.score : '',
        comment: existing ? existing.comment || '' : '',
      };
    });
    setScoresMap(initialScores);
  }, [selectedSubmissionId, currentSubmission, criteriaList, targetEvaluatorId]);

  // Fetch files for current submission
  useEffect(() => {
    const fetchFiles = async () => {
      if (!currentSubmission?.submissionId) {
        setSubmissionFiles([]);
        return;
      }
      try {
        setLoadingFiles(true);
        setSubmissionFiles([]); // Reset danh sách tệp cũ để tránh hiển thị sai giữa các nhà thầu
        const res = await bidSubmissionApi.getSubmissionById(currentSubmission.submissionId);
        if (res?.data) {
          setSubmissionFiles(res.data.files || []);
        }
      } catch (err) {
        console.error('Failed to load submission files:', err);
        toast.error('Không thể tải danh sách tài liệu đính kèm của nhà thầu này.');
      } finally {
        setLoadingFiles(false);
      }
    };

    fetchFiles();
  }, [currentSubmission?.submissionId]);

  // Handle score change
  const handleScoreChange = (criteriaId, val, maxScore) => {
    if (isFormReadOnly) return;
    const num = val === '' ? '' : parseFloat(val);
    if (num !== '' && (isNaN(num) || num < 0 || num > maxScore)) {
      return; // Out of bounds
    }
    setScoresMap((prev) => ({
      ...prev,
      [criteriaId]: {
        ...prev[criteriaId],
        score: val === '' ? '' : num,
      },
    }));
  };

  // Handle comment change
  const handleCommentChange = (criteriaId, text) => {
    if (isFormReadOnly) return;
    setScoresMap((prev) => ({
      ...prev,
      [criteriaId]: {
        ...prev[criteriaId],
        comment: text,
      },
    }));
  };

  const manualCriteria = criteriaList.filter((c) => !c.isPriceCriteria);

  // Calculate projected weighted total score
  const calculateTotalWeightedScore = () => {
    let sum = 0;
    criteriaList.forEach((c) => {
      const item = scoresMap[c.id];
      const score = item?.score !== '' && !isNaN(item?.score) ? Number(item.score) : 0;
      sum += (score * c.weight) / 100;
    });
    return Math.round(sum * 100) / 100;
  };

  // Count criteria with scores entered (chỉ tính tiêu chí cần chấm tay)
  const criteriaFilledCount = manualCriteria.filter((c) => {
    const s = scoresMap[c.id]?.score;
    return s !== '' && s !== undefined && !isNaN(s);
  }).length;

  const isComplete = manualCriteria.length > 0 && criteriaFilledCount === manualCriteria.length;

  // Submit Scorecard
  const handleSaveScorecard = async () => {
    if (readOnly || !isViewingOwnScorecard || !currentSubmission) return;

    if (!isComplete) {
      toast.error(`Vui lòng nhập điểm đầy đủ cho toàn bộ ${manualCriteria.length} tiêu chí chuyên môn.`);
      return;
    }

    const payload = {
      scores: manualCriteria.map((c) => ({
        criteriaId: c.id,
        score: Number(scoresMap[c.id]?.score || 0),
        comment: scoresMap[c.id]?.comment?.trim() || null,
      })),
    };

    try {
      setSaving(true);
      const res = await evaluationApi.scoreSubmission(currentSubmission.submissionId, payload);
      if (res?.success) {
        toast.success(`Đã lưu phiếu chấm điểm cho ${currentSubmission.companyName}!`);
        if (onScoreSaved) {
          onScoreSaved();
        }
      } else {
        toast.error(res?.message || 'Có lỗi xảy ra khi lưu phiếu chấm.');
      }
    } catch (err) {
      const errMsg = err.message || 'Không thể lưu phiếu chấm điểm.';
      toast.error(errMsg);
    } finally {
      setSaving(false);
    }
  };

  const getStatusBadge = (status, totalScore) => {
    switch (status) {
      case 'Selected':
        return (
          <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-bold bg-amber-100 text-amber-800 border border-amber-300">
            <Award className="w-3 h-3 mr-1 text-amber-600" /> Trúng thầu
          </span>
        );
      case 'Evaluated':
        return (
          <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-100 text-emerald-800 border border-emerald-300">
            <CheckCircle2 className="w-3 h-3 mr-1 text-emerald-600" /> Đã chấm ({formatNumber(totalScore)})
          </span>
        );
      case 'Rejected':
        return (
          <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-semibold bg-rose-100 text-rose-800 border border-rose-300">
            Không trúng thầu
          </span>
        );
      default:
        return (
          <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-700 border border-slate-300">
            <Clock className="w-3 h-3 mr-1 text-slate-500" /> Chờ chấm điểm
          </span>
        );
    }
  };

  if (!submissions || submissions.length === 0) {
    return (
      <div className="bg-white rounded-2xl border border-slate-200 p-12 text-center space-y-3">
        <Building2 className="w-12 h-12 text-slate-300 mx-auto" />
        <h4 className="text-base font-bold text-slate-800">Chưa có hồ sơ dự thầu</h4>
        <p className="text-xs text-slate-500 max-w-md mx-auto">
          Gói thầu này hiện chưa có nhà thầu nào nộp hồ sơ hoặc chưa có hồ sơ hợp lệ để tiến hành chấm điểm.
        </p>
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
      {/* CỘT TRÁI: DANH SÁCH NHÀ THẦU */}
      <div className="lg:col-span-4 space-y-3">
        <div className="flex items-center justify-between px-1">
          <h3 className="text-xs font-bold uppercase tracking-wider text-slate-500 flex items-center gap-1.5">
            <Building2 className="w-4 h-4 text-slate-400" />
            Nhà thầu tham gia ({submissions.length})
          </h3>
          <span className="text-[11px] text-slate-400">Chọn để chấm điểm</span>
        </div>

        <div className="space-y-2">
          {submissions.map((sub, index) => {
            const isSelected = sub.submissionId === selectedSubmissionId;
            const hasScore = sub.totalScore !== null && sub.totalScore !== undefined;

            // Kiểm tra trạng thái chấm điểm của chính người đang đăng nhập
            const myScores = sub.scores?.filter((es) => es.evaluatorId === user?.id) || [];
            const requiredCount = manualCriteria.length > 0 ? manualCriteria.length : criteriaList.length;
            const isScoredByMe = requiredCount > 0 && myScores.length >= requiredCount;
            const isPartiallyScored = myScores.length > 0 && myScores.length < requiredCount;
            const myTotalScore = myScores.reduce((sum, s) => {
              const crit = criteriaList.find((c) => c.id === s.criteriaId);
              const w = crit ? crit.weight : 0;
              return sum + (s.score * w) / 100;
            }, 0);
            const uniqueEvaluatorsCount = new Set(sub.scores?.map((s) => s.evaluatorId)).size;

            return (
              <button
                key={sub.submissionId}
                onClick={() => setSelectedSubmissionId(sub.submissionId)}
                type="button"
                className={`w-full text-left p-4 rounded-xl border transition-all cursor-pointer relative ${
                  isSelected
                    ? 'bg-blue-50/60 border-blue-500 shadow-xs ring-1 ring-blue-500/20'
                    : 'bg-white border-slate-200 hover:border-slate-300 hover:bg-slate-50/50'
                }`}
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="space-y-1 flex-1 min-w-0">
                    <div className="flex items-center gap-1.5">
                      <span className="w-5 h-5 rounded-full bg-slate-100 text-slate-600 text-[11px] font-bold flex items-center justify-center flex-shrink-0">
                        {sub.rank ? `#${sub.rank}` : index + 1}
                      </span>
                      <h4 className="text-xs font-bold text-slate-800 truncate" title={sub.companyName}>
                        {sub.companyName}
                      </h4>
                    </div>
                    <div className="text-[11px] text-slate-500 flex flex-wrap items-center gap-2 pl-6">
                      <span>MST: {sub.taxCode || 'N/A'}</span>
                      <span>•</span>
                      <span className="font-semibold text-emerald-700 font-mono">
                        {sub.bidPrice ? formatCurrency(sub.bidPrice) : '-'}
                      </span>
                    </div>
                  </div>
                  <ChevronRight
                    className={`w-4 h-4 transition-transform flex-shrink-0 mt-1 ${
                      isSelected ? 'text-blue-600 translate-x-0.5' : 'text-slate-300'
                    }`}
                  />
                </div>

                <div className="mt-3 pt-2.5 border-t border-slate-100 flex items-center justify-between text-xs pl-6">
                  <div>
                    {isScoredByMe ? (
                      <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-100 text-emerald-800 border border-emerald-300">
                        <CheckCircle2 className="w-3 h-3 mr-1 text-emerald-600" />
                        Bạn đã chấm ({formatNumber(myTotalScore)} đ)
                      </span>
                    ) : isPartiallyScored ? (
                      <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-semibold bg-sky-50 text-sky-800 border border-sky-300">
                        <Clock className="w-3 h-3 mr-1 text-sky-600" />
                        Đang chấm ({myScores.length}/{requiredCount})
                      </span>
                    ) : (
                      <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-semibold bg-amber-50 text-amber-800 border border-amber-300">
                        <Clock className="w-3 h-3 mr-1 text-amber-600" />
                        Bạn chưa chấm
                      </span>
                    )}
                  </div>
                  {hasScore && (
                    <div className="text-right">
                      <span className="text-[10px] text-slate-400 block uppercase font-medium">
                        Điểm TB ({uniqueEvaluatorsCount} GK)
                      </span>
                      <span className="text-sm font-extrabold text-blue-700">
                        {formatNumber(sub.totalScore)}
                        <span className="text-[10px] font-normal text-slate-400">/100</span>
                      </span>
                    </div>
                  )}
                </div>
              </button>
            );
          })}
        </div>
      </div>

      {/* CỘT PHẢI: MA TRẬN PHIẾU CHẤM ĐIỂM */}
      <div className="lg:col-span-8 space-y-4">
        {currentSubmission ? (
          <div className="bg-white rounded-2xl border border-slate-200 shadow-xs overflow-hidden">
            {/* Header thông tin nhà thầu được chọn */}
            <div className="p-5 bg-gradient-to-r from-slate-50 to-blue-50/40 border-b border-slate-200 flex flex-wrap items-center justify-between gap-4">
              <div>
                <div className="flex flex-wrap items-center gap-2">
                  <span className="text-[11px] font-bold uppercase tracking-wider text-blue-700 bg-blue-100 px-2.5 py-1 rounded-md flex items-center gap-1.5 shadow-2xs">
                    <UserCheck className="w-3.5 h-3.5 text-blue-600" />
                    <span>
                      {isViewingOwnScorecard
                        ? `Phiếu chấm của bạn: ${user?.fullName || 'Giám khảo'}`
                        : `Phiếu chấm của: ${distinctEvaluators.find((e) => e.id === Number(targetEvaluatorId))?.name || 'Giám khảo'}`}
                    </span>
                  </span>
                  <span className="text-xs text-slate-400 font-mono">Mã hồ sơ: #{currentSubmission.submissionId}</span>
                </div>
                <h3 className="text-base font-bold text-slate-800 mt-1.5">
                  {currentSubmission.companyName}
                </h3>
                <div className="flex flex-wrap items-center gap-2.5 mt-1 text-xs">
                  <span className="text-slate-500">MST: {currentSubmission.taxCode || 'Chưa cung cấp'}</span>
                  <span className="text-slate-300">•</span>
                  <span className="text-slate-700 font-medium">
                    Giá dự thầu: <strong className="text-emerald-700 font-mono text-xs">{currentSubmission.bidPrice ? formatCurrency(currentSubmission.bidPrice) : 'Chưa cập nhật'}</strong>
                  </span>
                  {packageData?.budget > 0 && currentSubmission.bidPrice && (
                    <span className="text-[10.5px] px-2 py-0.5 rounded-md bg-emerald-50 text-emerald-700 border border-emerald-200 font-semibold">
                      Tiết kiệm {formatCurrency(packageData.budget - currentSubmission.bidPrice)} ({(((packageData.budget - currentSubmission.bidPrice) / packageData.budget) * 100).toFixed(1)}%)
                    </span>
                  )}
                </div>

                {/* Dropdown xem phiếu giám khảo khác dành cho Admin / Procurement */}
                {isAdminOrProcurement && distinctEvaluators.length > 0 && (
                  <div className="flex items-center gap-2 mt-2 pt-2 border-t border-slate-200/60">
                    <span className="text-[11px] text-slate-600 font-semibold">Xem phiếu chấm của:</span>
                    <select
                      value={targetEvaluatorId}
                      onChange={(e) => setViewEvaluatorId(Number(e.target.value))}
                      className="text-xs bg-white border border-slate-300 rounded-lg px-2.5 py-1 font-semibold text-slate-800 shadow-2xs focus:outline-hidden focus:ring-1 focus:ring-blue-500 cursor-pointer"
                    >
                      <option value={user?.id}>Phiếu của tôi ({user?.fullName})</option>
                      {distinctEvaluators
                        .filter((e) => e.id !== user?.id)
                        .map((e) => (
                          <option key={e.id} value={e.id}>
                            Phiếu của: {e.name}
                          </option>
                        ))}
                    </select>
                  </div>
                )}
              </div>

              {/* Nút xem tài liệu hồ sơ */}
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={() => setShowFilesModal(true)}
                  className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-slate-700 bg-white border border-slate-300 hover:bg-slate-50 rounded-xl shadow-2xs transition-colors cursor-pointer"
                >
                  <FileText className="w-3.5 h-3.5 text-blue-600" />
                  <span>Xem tài liệu nộp ({submissionFiles.length})</span>
                </button>
              </div>
            </div>

            {/* Bảng nhập điểm tiêu chí */}
            <div className="p-5 space-y-4">
              {!isViewingOwnScorecard ? (
                <div className="bg-sky-50 border border-sky-200 rounded-xl p-3.5 text-xs text-sky-900 flex items-start gap-2.5">
                  <Info className="w-4 h-4 text-sky-600 flex-shrink-0 mt-0.5" />
                  <div className="leading-relaxed">
                    <strong>Chế độ xem phiếu chấm:</strong> Bạn đang xem phiếu điểm do giám khảo <strong>{distinctEvaluators.find((e) => e.id === Number(targetEvaluatorId))?.name}</strong> thực hiện. Chế độ này là chỉ đọc để đảm bảo tính độc lập.
                  </div>
                </div>
              ) : readOnly ? (
                <div className="bg-amber-50/90 border border-amber-200 rounded-xl p-3.5 text-xs text-amber-900 flex items-start gap-2.5">
                  <Lock className="w-4 h-4 text-amber-600 flex-shrink-0 mt-0.5" />
                  <div className="leading-relaxed">
                    <strong>Bảng điểm đã được khóa:</strong> Gói thầu đã hoàn tất trao thầu hoặc tài khoản của bạn ở chế độ chỉ đọc. Toàn bộ điểm số và nhận xét chuyên môn được lưu trữ cố định để bảo toàn tính toàn vẹn hồ sơ.
                  </div>
                </div>
              ) : (
                <div className="bg-blue-50/50 border border-blue-200/60 rounded-xl p-3.5 text-xs text-blue-800 flex items-start gap-2.5">
                  <Info className="w-4 h-4 text-blue-600 flex-shrink-0 mt-0.5" />
                  <div className="leading-relaxed">
                    <strong>Quy tắc chấm điểm chuẩn:</strong> Thang điểm từ 0 đến Điểm tối đa (MaxScore). Điểm quy đổi trọng số được tính tự động:{' '}
                    <code>Điểm quy đổi = Điểm số × Trọng số / 100</code>.
                  </div>
                </div>
              )}

              {/* Bảng danh sách tiêu chí */}
              <div className="border border-slate-200 rounded-xl overflow-hidden">
                <table className="w-full text-left text-xs border-collapse">
                  <thead>
                    <tr className="bg-slate-50 border-b border-slate-200 text-slate-600 font-bold uppercase tracking-wider text-[11px]">
                      <th className="py-3 px-4 w-12 text-center">STT</th>
                      <th className="py-3 px-4">Tiêu chí đánh giá</th>
                      <th className="py-3 px-4 text-center w-28">Trọng số (%)</th>
                      <th className="py-3 px-4 text-center w-36">Điểm chấm (0-Max)</th>
                      <th className="py-3 px-4 text-right w-28">Điểm quy đổi</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {criteriaList.map((crit, idx) => {
                      const item = scoresMap[crit.id] || { score: '', comment: '' };
                      const scoreVal = item.score !== '' && !isNaN(item.score) ? Number(item.score) : 0;
                      const weightedScore = (scoreVal * crit.weight) / 100;

                      return (
                        <React.Fragment key={crit.id}>
                          <tr className="hover:bg-slate-50/60 transition-colors">
                            <td className="py-3.5 px-4 text-center font-bold text-slate-400">
                              {idx + 1}
                            </td>
                            <td className="py-3.5 px-4">
                              <div className="flex items-center gap-1.5 flex-wrap">
                                <p className="font-bold text-slate-800 text-xs">{crit.name}</p>
                                {crit.isPriceCriteria && (
                                  <span className="px-1.5 py-0.5 rounded text-[10px] font-bold bg-amber-100 text-amber-800 border border-amber-200">
                                    Tự động (Giá)
                                  </span>
                                )}
                              </div>
                              <span className="text-[11px] text-slate-400">
                                Thang điểm tối đa: {formatNumber(crit.maxScore)} điểm
                              </span>
                            </td>
                            <td className="py-3.5 px-4 text-center">
                              <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-bold bg-slate-100 text-slate-700">
                                {crit.weight}%
                              </span>
                            </td>
                            <td className="py-3.5 px-4 text-center">
                              {crit.isPriceCriteria ? (
                                <div className="w-28 py-1.5 px-2 bg-amber-50/70 border border-amber-200 rounded-lg text-center text-[10px] font-semibold text-amber-800 mx-auto">
                                  Hệ thống tự tính
                                </div>
                              ) : (
                                <div className="relative inline-block w-28">
                                  <input
                                    type="number"
                                    min="0"
                                    max={crit.maxScore}
                                    step="0.5"
                                    placeholder="0.0"
                                    disabled={isFormReadOnly}
                                    value={item.score}
                                    onChange={(e) => handleScoreChange(crit.id, e.target.value, crit.maxScore)}
                                    className="w-full px-3 py-1.5 text-center text-xs font-bold text-slate-800 bg-white border border-slate-300 rounded-lg focus:outline-hidden focus:ring-2 focus:ring-blue-500 focus:border-blue-500 disabled:bg-slate-100 disabled:text-slate-500"
                                  />
                                </div>
                              )}
                            </td>
                            <td className="py-3.5 px-4 text-right font-bold text-blue-700">
                              {crit.isPriceCriteria
                                ? <span className="text-[11px] text-amber-700 font-semibold italic">Tự tính</span>
                                : item.score !== '' ? formatNumber(weightedScore) : '—'}
                            </td>
                          </tr>
                          {/* Dòng nhận xét cho tiêu chí */}
                          <tr className="bg-slate-50/30">
                            <td colSpan={5} className="px-4 pb-3 pt-0">
                              <div className="pl-8">
                                {crit.isPriceCriteria ? (
                                  <p className="text-[11px] text-amber-700/80 italic py-1">
                                    Tiêu chí Giá / Tài chính được tự động chấm điểm theo công thức tỷ lệ nghịch giá dự thầu (Luật Đấu thầu).
                                  </p>
                                ) : (
                                  <input
                                    type="text"
                                    placeholder="Ghi chú / Nhận xét chuyên môn cho tiêu chí này (tùy chọn)..."
                                    disabled={isFormReadOnly}
                                    value={item.comment}
                                    onChange={(e) => handleCommentChange(crit.id, e.target.value)}
                                    className="w-full px-3 py-1 text-[11px] text-slate-600 bg-white/70 border border-slate-200 rounded-md focus:bg-white focus:outline-hidden focus:ring-1 focus:ring-blue-400 disabled:bg-transparent disabled:border-transparent"
                                  />
                                )}
                              </div>
                            </td>
                          </tr>
                        </React.Fragment>
                      );
                    })}
                  </tbody>
                </table>
              </div>

              {/* Khối Tổng kết điểm dự kiến */}
              <div className="bg-slate-50 rounded-xl p-4 border border-slate-200 flex flex-wrap items-center justify-between gap-4">
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <Calculator className="w-4 h-4 text-slate-500" />
                    <span className="text-xs font-semibold text-slate-700">
                      {isViewingOwnScorecard ? 'Tổng điểm quy đổi dự kiến của bạn:' : 'Tổng điểm quy đổi của phiếu này:'}
                    </span>
                    <span className="text-xs text-slate-400">
                      ({criteriaFilledCount}/{criteriaList.length} tiêu chí đã nhập)
                    </span>
                  </div>
                  <p className="text-[11px] text-slate-500">
                    Điểm tổng hợp chung của gói thầu sẽ được hệ thống tính bình quân các giám khảo và tự động xếp hạng.
                  </p>
                </div>

                <div className="text-right">
                  <span className="text-2xl font-black text-blue-700">
                    {formatNumber(calculateTotalWeightedScore())}
                  </span>
                  <span className="text-xs font-medium text-slate-400"> / 100 điểm</span>
                </div>
              </div>

              {/* Nút Hành động */}
              {!readOnly && isViewingOwnScorecard && (
                <div className="flex items-center justify-end gap-3 pt-2">
                  <button
                    type="button"
                    onClick={handleSaveScorecard}
                    disabled={saving || !isComplete}
                    className="inline-flex items-center gap-2 px-5 py-2.5 text-xs font-bold text-white bg-blue-600 hover:bg-blue-700 rounded-xl shadow-xs transition-colors cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {saving ? (
                      <>
                        <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                        <span>Đang lưu phiếu chấm...</span>
                      </>
                    ) : (
                      <>
                        <Save className="w-4 h-4" />
                        <span>Lưu phiếu chấm điểm</span>
                      </>
                    )}
                  </button>
                </div>
              )}
            </div>
          </div>
        ) : null}
      </div>

      {/* Modal xem danh sách tệp đính kèm */}
      {showFilesModal && (
        <div
          className="fixed inset-0 z-50 bg-slate-900/50 backdrop-blur-2xs flex items-center justify-center p-4 animate-in fade-in duration-150"
          role="dialog"
          aria-modal="true"
          aria-labelledby="submission-files-modal-title"
          onClick={(e) => {
            if (e.target === e.currentTarget) setShowFilesModal(false);
          }}
        >
          <div className="bg-white rounded-2xl shadow-xl border border-slate-200 w-full max-w-xl overflow-hidden animate-in fade-in zoom-in-95 duration-150">
            <div className="p-4 bg-slate-50 border-b border-slate-200 flex items-center justify-between">
              <h4 id="submission-files-modal-title" className="text-sm font-bold text-slate-800 flex items-center gap-2">
                <FileText className="w-4 h-4 text-blue-600" />
                <span>Tài liệu dự thầu - {currentSubmission?.companyName}</span>
              </h4>
              <button
                type="button"
                onClick={() => setShowFilesModal(false)}
                className="text-slate-400 hover:text-slate-600 p-1.5 rounded-lg hover:bg-slate-100 transition-colors cursor-pointer"
                aria-label="Đóng modal"
              >
                ✕
              </button>
            </div>
            <div className="p-4 max-h-[28rem] overflow-y-auto space-y-2.5">
              {loadingFiles ? (
                <div className="text-center py-8 text-xs text-slate-400 space-y-2">
                  <div className="w-5 h-5 border-2 border-blue-600 border-t-transparent rounded-full animate-spin mx-auto" />
                  <p>Đang tải danh sách tài liệu...</p>
                </div>
              ) : submissionFiles.length === 0 ? (
                <div className="text-center py-8 text-xs text-slate-400">
                  <FileText className="w-8 h-8 text-slate-300 mx-auto mb-2" />
                  <p>Không tìm thấy tài liệu đính kèm nào cho hồ sơ này.</p>
                </div>
              ) : (
                submissionFiles.map((file, i) => {
                  const fileInfo = getFileTypeBadge(file.fileType, file.fileTypeName);
                  const downloadEndpoint = `/api/submissions/files/${file.id}/download`;

                  return (
                    <div
                      key={file.id || i}
                      className="flex items-center justify-between gap-3 p-3 bg-slate-50 hover:bg-slate-100/70 transition-colors rounded-xl border border-slate-200 text-xs"
                    >
                      <div className="flex items-center gap-2.5 min-w-0 flex-1">
                        <div className="w-8 h-8 rounded-lg bg-blue-100/60 text-blue-600 flex items-center justify-center shrink-0">
                          <FileText className="w-4 h-4" />
                        </div>
                        <div className="min-w-0 flex-1">
                          <p className="font-semibold text-slate-800 truncate" title={file.fileName || 'Tài liệu không tên'}>
                            {file.fileName || 'Tài liệu không tên'}
                          </p>
                          <div className="flex items-center gap-2 mt-0.5">
                            <span className={`inline-flex items-center px-2 py-0.5 rounded-md text-[10px] font-semibold border ${fileInfo.badgeClass}`}>
                              {fileInfo.label}
                            </span>
                            {file.uploadedAt && (
                              <span className="text-[10px] text-slate-400">
                                {formatDate(file.uploadedAt)}
                              </span>
                            )}
                          </div>
                        </div>
                      </div>

                      {/* Cụm nút hành động */}
                      <div className="flex items-center gap-1.5 shrink-0">
                        <button
                          type="button"
                          onClick={() => previewSecureFile(downloadEndpoint, file.fileName)}
                          className="inline-flex items-center gap-1 px-2.5 py-1.5 text-[11px] font-semibold rounded-lg transition-colors cursor-pointer bg-blue-50 text-blue-700 hover:bg-blue-100 border border-blue-200 shadow-2xs"
                          title="Xem trước tài liệu trong tab mới"
                        >
                          <Eye className="w-3.5 h-3.5" />
                          <span>Xem trước</span>
                        </button>

                        <button
                          type="button"
                          onClick={() => downloadSecureFile(downloadEndpoint, file.fileName)}
                          className="inline-flex items-center gap-1 px-2.5 py-1.5 text-[11px] font-semibold rounded-lg transition-colors cursor-pointer bg-slate-100 text-slate-700 hover:bg-slate-200 border border-slate-200 shadow-2xs"
                          title="Tải tệp tin về máy"
                        >
                          <Download className="w-3.5 h-3.5" />
                          <span>Tải về</span>
                        </button>
                      </div>
                    </div>
                  );
                })
              )}
            </div>
            <div className="p-3 bg-slate-50 border-t border-slate-200 flex items-center justify-between text-xs">
              <span className="text-[11px] text-slate-500 pl-1">
                Tổng số: <strong>{submissionFiles.length}</strong> tài liệu
              </span>
              <button
                type="button"
                onClick={() => setShowFilesModal(false)}
                className="px-4 py-1.5 text-xs font-semibold text-slate-600 hover:bg-slate-200 rounded-lg transition-colors cursor-pointer"
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default ScorecardForm;
