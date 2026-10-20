import React, { useState, useEffect } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import {
  ArrowLeft,
  Scale,
  Trophy,
  CheckCircle,
  Clock,
  AlertCircle,
  Building2,
  Users,
  Award,
  Layers,
  ChevronRight,
  RefreshCw,
  RotateCcw,
  AlertTriangle
} from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { evaluationApi } from '../api/evaluationApi';
import { bidPackageApi } from '../api/bidPackageApi';
import StatusBadge from '../components/common/StatusBadge';
import LoadingSpinner from '../components/common/LoadingSpinner';
import ScorecardForm from '../components/evaluation/ScorecardForm';
import LeaderboardView from '../components/evaluation/LeaderboardView';
import FinalizeWinnerModal from '../components/evaluation/FinalizeWinnerModal';
import CommitteeScoringProgress from '../components/evaluation/CommitteeScoringProgress';
import { formatCurrency, formatNumber } from '../utils/formatters';

export const EvaluationPage = () => {
  const { packageId } = useParams();
  const navigate = useNavigate();
  const { hasRole, user } = useAuth();

  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);

  const [packageData, setPackageData] = useState(null);
  const [criteriaList, setCriteriaList] = useState([]);
  const [summary, setSummary] = useState(null);
  const [rankings, setRankings] = useState([]);
  const [evaluators, setEvaluators] = useState([]);

  const [activeTab, setActiveTab] = useState('scorecard'); // 'scorecard' | 'leaderboard'

  // Finalize Award Modal state
  const [isFinalizeModalOpen, setIsFinalizeModalOpen] = useState(false);
  const [candidateWinner, setCandidateWinner] = useState(null);
  const [candidateHasTie, setCandidateHasTie] = useState(false);
  const [finalizing, setFinalizing] = useState(false);

  // Revoke Award Modal state (R2-4)
  const [isRevokeModalOpen, setIsRevokeModalOpen] = useState(false);
  const [revokeReason, setRevokeReason] = useState('');
  const [revoking, setRevoking] = useState(false);

  const isAdminOrProcurement = hasRole(['Admin', 'Procurement']);
  const isEvaluatorOrAdmin = hasRole(['Admin', 'Evaluator']);
  const isAdmin = hasRole('Admin');
  const isAssignedEvaluator = evaluators.some((e) => Number(e.evaluatorId) === Number(user?.id));
  const canScore = isAssignedEvaluator;

  // Fetch all necessary evaluation data
  const fetchData = async (isBackground = false) => {
    if (!isBackground) setLoading(true);
    else setRefreshing(true);
    setError(null);

    try {
      const [resPkg, resCriteria, resSummary, resRankings, resEvaluators] = await Promise.all([
        bidPackageApi.getPackageById(packageId),
        evaluationApi.getCriteriaByPackage(packageId),
        evaluationApi.getSummary(packageId),
        evaluationApi.getRankings(packageId),
        bidPackageApi.getEvaluators(packageId).catch(() => ({ data: [] })),
      ]);

      if (resPkg?.data) {
        setPackageData(resPkg.data);
      }

      if (resCriteria?.data) {
        setCriteriaList(resCriteria.data || []);
      }

      if (resSummary?.data) {
        setSummary(resSummary.data);
      }

      if (resRankings?.data) {
        setRankings(resRankings.data || []);
      }

      if (resEvaluators?.data) {
        setEvaluators(resEvaluators.data || []);
      }
    } catch (err) {
      console.error('Error fetching evaluation data:', err);
      setError('Không thể tải thông tin thẩm định và chấm điểm gói thầu.');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    if (packageId) {
      fetchData();
    }
  }, [packageId]);

  // Handle open finalize modal
  const handleOpenFinalizeModal = (winnerSubmission, hasTie = false) => {
    setCandidateWinner(winnerSubmission);
    setCandidateHasTie(Boolean(hasTie));
    setIsFinalizeModalOpen(true);
  };

  // Handle finalize submission
  const handleConfirmFinalize = async (submissionId, decisionReason) => {
    try {
      setFinalizing(true);
      const res = await evaluationApi.finalizeEvaluation(packageId, {
        selectedSubmissionId: submissionId,
        decisionReason: decisionReason || null,
      });
      if (res?.success) {
        toast.success('Phê duyệt kết quả trúng thầu thành công!');
        setIsFinalizeModalOpen(false);
        // Refresh data
        await fetchData(true);
        setActiveTab('leaderboard');
      } else {
        toast.error(res?.message || 'Có lỗi xảy ra khi phê duyệt kết quả.');
      }
    } catch (err) {
      const msg = err.message || 'Không thể phê duyệt kết quả trao thầu.';
      toast.error(msg);
    } finally {
      setFinalizing(false);
    }
  };

  // Handle revoke award (R2-4)
  const handleConfirmRevoke = async () => {
    if (!revokeReason.trim() || revokeReason.trim().length < 5) {
      toast.error('Vui lòng nhập lý do thu hồi quyết định trao thầu (tối thiểu 5 ký tự).');
      return;
    }
    try {
      setRevoking(true);
      const res = await evaluationApi.revokeAward(packageId, revokeReason.trim());
      if (res?.success) {
        toast.success('Thu hồi kết quả phê duyệt trúng thầu thành công!');
        setIsRevokeModalOpen(false);
        setRevokeReason('');
        await fetchData(true);
      } else {
        toast.error(res?.message || 'Có lỗi xảy ra khi thu hồi kết quả.');
      }
    } catch (err) {
      const msg = err.message || 'Không thể thu hồi kết quả trao thầu.';
      toast.error(msg);
    } finally {
      setRevoking(false);
    }
  };

  if (loading) {
    return (
      <div className="py-24 flex items-center justify-center">
        <LoadingSpinner size="lg" message="Đang tải dữ liệu chấm điểm & bảng xếp hạng..." />
      </div>
    );
  }

  if (error || !packageData) {
    return (
      <div className="bg-white rounded-2xl border border-rose-200 p-8 text-center max-w-lg mx-auto space-y-4 my-10">
        <AlertCircle className="w-12 h-12 text-rose-500 mx-auto" />
        <h3 className="text-base font-bold text-slate-800">Không tìm thấy gói thầu</h3>
        <p className="text-xs text-slate-500">{error || 'Gói thầu không tồn tại hoặc bạn không có quyền truy cập.'}</p>
        <Link
          to="/packages"
          className="inline-flex items-center gap-2 px-4 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold rounded-xl transition-colors"
        >
          <ArrowLeft className="w-4 h-4" /> Quay lại danh sách gói thầu
        </Link>
      </div>
    );
  }

  const isAwarded = Boolean(
    packageData.isAwarded ||
    packageData.status === 'Awarded' ||
    summary?.isFinalized ||
    summary?.winningSubmissionId
  );
  const displayStatus =
    packageData.status === 'Contracted' || packageData.status === 3
      ? 'Contracted'
      : isAwarded
      ? 'Awarded'
      : packageData.status;

  const progressPercent =
    summary?.totalSubmissions > 0
      ? Math.round((summary.evaluatedSubmissions / summary.totalSubmissions) * 100)
      : 0;

  return (
    <div className="space-y-6 pb-12">
      {/* HEADER & NAVIGATION */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div className="space-y-1.5">
          <div className="flex items-center gap-2">
            <Link
              to={`/packages/${packageId}`}
              className="inline-flex items-center gap-1.5 text-xs font-semibold text-slate-500 hover:text-slate-800 transition-colors"
            >
              <ArrowLeft className="w-4 h-4" />
              <span>Gói thầu: {packageData.code}</span>
            </Link>
            <span className="text-slate-300">•</span>
            <span className="text-xs font-medium text-slate-400">Phân hệ Chấm điểm & Xếp hạng</span>
          </div>

          <div className="flex items-center gap-3">
            <h1 className="text-xl font-black text-slate-900 tracking-tight">
              {packageData.name}
            </h1>
            <StatusBadge status={displayStatus} />
          </div>
        </div>

        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => fetchData(true)}
            disabled={refreshing}
            className="inline-flex items-center gap-1.5 px-3 py-2 text-xs font-semibold text-slate-600 bg-white border border-slate-200 hover:bg-slate-50 rounded-xl shadow-2xs transition-colors cursor-pointer"
            title="Làm mới dữ liệu"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${refreshing ? 'animate-spin text-blue-600' : ''}`} />
            <span>Làm mới</span>
          </button>
        </div>
      </div>

      {/* BANNER THÔNG BÁO GÓI THẦU ĐÃ TRAO THẦU */}
      {isAwarded && (
        <div className="bg-gradient-to-r from-emerald-50 via-teal-50 to-emerald-50 border border-emerald-200 rounded-2xl p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-4 shadow-2xs">
          <div className="flex items-center gap-3.5">
            <div className="w-10 h-10 rounded-xl bg-emerald-100 text-emerald-700 flex items-center justify-center shrink-0">
              <CheckCircle className="w-5 h-5 text-emerald-600" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="text-xs font-bold text-emerald-900">
                  Gói thầu đã hoàn tất thẩm định & trao thầu
                </span>
                <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-100 text-emerald-800 border border-emerald-300">
                  Bảng điểm đã khóa
                </span>
              </div>
              <p className="text-xs text-emerald-700 mt-0.5">
                {summary?.winningContractorName ? (
                  <>Đơn vị trúng thầu: <strong>{summary.winningContractorName}</strong>. Dữ liệu bảng điểm đã được chốt và khóa lưu trữ hồ sơ.</>
                ) : (
                  <>Kết quả trúng thầu đã được phê duyệt. Dữ liệu bảng điểm đã được chốt và khóa lưu trữ hồ sơ.</>
                )}
              </p>
            </div>
          </div>
          <div className="flex items-center gap-2 shrink-0">
            {summary?.winningScore !== null && summary?.winningScore !== undefined && (
              <div className="flex items-center gap-2 bg-white/90 px-3.5 py-1.5 rounded-xl border border-emerald-200">
                <span className="text-[11px] text-emerald-700 font-medium">Điểm trúng thầu:</span>
                <span className="text-sm font-black text-emerald-800">{formatNumber(summary.winningScore)} / 100</span>
              </div>
            )}
            {isAdminOrProcurement && (
              <button
                type="button"
                onClick={() => {
                  setRevokeReason('');
                  setIsRevokeModalOpen(true);
                }}
                className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-white hover:bg-rose-50 text-rose-700 border border-rose-300 rounded-xl text-xs font-bold transition-all shadow-2xs hover:shadow-xs cursor-pointer"
                title="Thu hồi quyết định trao thầu để tổ chức thẩm định lại"
              >
                <RotateCcw className="w-3.5 h-3.5 text-rose-600" />
                <span>Thu hồi trao thầu</span>
              </button>
            )}
          </div>
        </div>
      )}

      {/* KPI PROGRESS & SUMMARY CARDS */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {/* Card 1: Tổng hồ sơ */}
        <div className="bg-white rounded-2xl p-4 border border-slate-200 shadow-2xs flex items-center gap-3.5">
          <div className="w-11 h-11 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center flex-shrink-0">
            <Users className="w-5 h-5" />
          </div>
          <div>
            <span className="text-[11px] font-medium text-slate-400 block uppercase tracking-wider">Hồ sơ tham gia</span>
            <div className="flex items-baseline gap-1.5">
              <span className="text-lg font-black text-slate-800">
                {summary?.totalSubmissions || rankings.length}
              </span>
              <span className="text-xs text-slate-500">nhà thầu</span>
            </div>
          </div>
        </div>

        {/* Card 2: Tiến độ chấm điểm */}
        <div className="bg-white rounded-2xl p-4 border border-slate-200 shadow-2xs space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-[11px] font-medium text-slate-400 uppercase tracking-wider">Tiến độ chấm thầu</span>
            <span className="text-xs font-bold text-blue-600">
              {summary?.evaluatedSubmissions || 0} / {summary?.totalSubmissions || 0} hồ sơ
            </span>
          </div>
          <div className="w-full bg-slate-100 rounded-full h-2 overflow-hidden">
            <div
              className={`h-full transition-all duration-300 ${
                progressPercent === 100 ? 'bg-emerald-500' : 'bg-blue-600'
              }`}
              style={{ width: `${progressPercent}%` }}
            />
          </div>
        </div>

        {/* Card 3: Điểm cao nhất */}
        <div className="bg-white rounded-2xl p-4 border border-slate-200 shadow-2xs flex items-center gap-3.5">
          <div className="w-11 h-11 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center flex-shrink-0">
            <Trophy className="w-5 h-5" />
          </div>
          <div>
            <span className="text-[11px] font-medium text-slate-400 block uppercase tracking-wider">Điểm cao nhất</span>
            <div className="flex items-baseline gap-1">
              <span className="text-lg font-black text-slate-800">
                {summary?.highestScore !== null && summary?.highestScore !== undefined
                  ? formatNumber(summary.highestScore)
                  : '—'}
              </span>
              <span className="text-xs text-slate-400">/ 100</span>
            </div>
          </div>
        </div>

        {/* Card 4: Điểm trung bình */}
        <div className="bg-white rounded-2xl p-4 border border-slate-200 shadow-2xs flex items-center gap-3.5">
          <div className="w-11 h-11 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center flex-shrink-0">
            <CheckCircle className="w-5 h-5" />
          </div>
          <div>
            <span className="text-[11px] font-medium text-slate-400 block uppercase tracking-wider">Điểm trung bình</span>
            <div className="flex items-baseline gap-1">
              <span className="text-lg font-black text-slate-800">
                {summary?.averageScore !== null && summary?.averageScore !== undefined
                  ? formatNumber(summary.averageScore)
                  : '—'}
              </span>
              <span className="text-xs text-slate-400">/ 100</span>
            </div>
          </div>
        </div>
      </div>

      {/* TAB NAVIGATION */}
      <div className="border-b border-slate-200 flex items-center space-x-6">
        <button
          type="button"
          onClick={() => setActiveTab('scorecard')}
          className={`pb-3 text-xs font-bold transition-all relative flex items-center gap-2 cursor-pointer ${
            activeTab === 'scorecard'
              ? 'text-blue-600 border-b-2 border-blue-600'
              : 'text-slate-500 hover:text-slate-800'
          }`}
        >
          <Scale className="w-4 h-4" />
          <span>Ma trận Chấm điểm (Scorecard)</span>
          <span className="px-1.5 py-0.5 rounded-full text-[10px] bg-slate-100 text-slate-600">
            {rankings.length}
          </span>
        </button>

        <button
          type="button"
          onClick={() => setActiveTab('leaderboard')}
          className={`pb-3 text-xs font-bold transition-all relative flex items-center gap-2 cursor-pointer ${
            activeTab === 'leaderboard'
              ? 'text-blue-600 border-b-2 border-blue-600'
              : 'text-slate-500 hover:text-slate-800'
          }`}
        >
          <Trophy className="w-4 h-4 text-amber-500" />
          <span>Bảng xếp hạng & Trao thầu</span>
          {isAwarded && (
            <span className="px-1.5 py-0.5 rounded-full text-[10px] bg-emerald-100 text-emerald-800 font-bold">
              Đã trao thầu
            </span>
          )}
        </button>
      </div>

      {/* Cảnh báo quyền truy cập khi Giám khảo không nằm trong Tổ chuyên gia */}
      {!canScore && (
        <div className="p-4 bg-amber-50 border border-amber-200 rounded-2xl flex items-center gap-3 text-amber-800 text-xs">
          <AlertCircle className="w-5 h-5 text-amber-600 flex-shrink-0" />
          <div>
            <p className="font-bold">Chế độ xem (Chỉ đọc)</p>
            <p className="mt-0.5 text-amber-700">
              Bạn không thuộc Tổ chuyên gia được bên mời thầu phân công chấm điểm cho gói thầu này. Bạn chỉ có thể xem tiêu chí và bảng xếp hạng tổng hợp theo Luật Đấu thầu.
            </p>
          </div>
        </div>
      )}

      {/* TAB CONTENT */}
      {activeTab === 'scorecard' && (
        <ScorecardForm
          packageData={packageData}
          criteriaList={criteriaList}
          submissions={rankings}
          onScoreSaved={() => fetchData(true)}
          readOnly={isAwarded || !canScore}
          canScore={canScore}
        />
      )}

      {activeTab === 'leaderboard' && (
        <div className="space-y-6">
          {/* Widget Tiến độ chấm điểm của Tổ Chuyên Gia (R2-2 & R2-12) */}
          {!isAwarded && (
            <CommitteeScoringProgress
              evaluators={evaluators}
              rankings={rankings}
            />
          )}

          <LeaderboardView
            packageData={packageData}
            rankings={rankings}
            criteriaList={criteriaList}
            summary={summary}
            onFinalizeClick={handleOpenFinalizeModal}
            isAdminOrProcurement={isAdminOrProcurement}
            isReadyToAward={
              rankings.filter((s) => s.status !== 'Withdrawn' && s.status !== 'Disqualified').length > 0 &&
              rankings.filter((s) => s.status !== 'Withdrawn' && s.status !== 'Disqualified').every((s) => s.status === 'Evaluated')
            }
            onDisqualifySuccess={() => fetchData(true)}
          />
        </div>
      )}

      {/* FINALIZE WINNER MODAL */}
      <FinalizeWinnerModal
        isOpen={isFinalizeModalOpen}
        onClose={() => setIsFinalizeModalOpen(false)}
        packageData={packageData}
        winningSubmission={candidateWinner}
        onConfirm={handleConfirmFinalize}
        loading={finalizing}
        hasTie={candidateHasTie}
      />

      {/* REVOKE AWARD MODAL (R2-4) */}
      {isRevokeModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-xs animate-in fade-in duration-200">
          <div className="bg-white rounded-2xl max-w-lg w-full p-6 shadow-2xl border border-slate-100 space-y-4">
            <div className="flex items-start gap-3.5">
              <div className="w-10 h-10 rounded-xl bg-rose-100 text-rose-600 flex items-center justify-center shrink-0">
                <AlertTriangle className="w-5 h-5 text-rose-600" />
              </div>
              <div className="space-y-1">
                <h3 className="text-base font-bold text-slate-800">
                  Thu hồi quyết định phê duyệt trao thầu
                </h3>
                <p className="text-xs text-slate-500">
                  Gói thầu: <strong className="text-slate-700">{packageData.name}</strong> ({packageData.code})
                </p>
              </div>
            </div>

            <div className="bg-amber-50 border border-amber-200 rounded-xl p-3 text-xs text-amber-800 space-y-1">
              <p className="font-semibold flex items-center gap-1.5">
                <AlertCircle className="w-4 h-4 text-amber-600 shrink-0" />
                Căn cứ Điều 64 Luật Đấu thầu 2023 & Quy chuẩn hệ thống:
              </p>
              <ul className="list-disc pl-4 space-y-0.5 text-[11px] text-amber-900">
                <li>Chỉ được thu hồi khi gói thầu <strong>chưa kích hoạt hợp đồng (Active / Completed)</strong>.</li>
                <li>Gói thầu sẽ quay lại trạng thái <strong>Evaluating (Chấm điểm)</strong>.</li>
                <li>Toàn bộ hồ sơ hợp lệ sẽ được khôi phục về trạng thái <strong>Evaluated</strong> để tiếp tục xét duyệt hoặc trao thầu cho nhà thầu khác.</li>
                <li>Hợp đồng dự thảo (Draft) nếu có sẽ tự động bị hủy bỏ.</li>
              </ul>
            </div>

            <div className="space-y-1.5">
              <label className="block text-xs font-bold text-slate-700">
                Lý do / Căn cứ thu hồi trao thầu <span className="text-rose-500">*</span>
              </label>
              <textarea
                value={revokeReason}
                onChange={(e) => setRevokeReason(e.target.value)}
                placeholder="Nhập chi tiết lý do thu hồi quyết định trao thầu (ví dụ: Nhà thầu không hoàn thiện bảo đảm thực hiện hợp đồng theo thời hạn, hoặc phát hiện sai sót số liệu...)"
                rows={4}
                className="w-full text-xs p-3 rounded-xl border border-slate-200 focus:outline-none focus:border-rose-500 focus:ring-2 focus:ring-rose-100 transition-all text-slate-800"
              />
              <span className="text-[10px] text-slate-400 block text-right">
                Tối thiểu 5 ký tự (Hiện tại: {revokeReason.trim().length})
              </span>
            </div>

            <div className="flex items-center justify-end gap-2.5 pt-2 border-t border-slate-100">
              <button
                type="button"
                onClick={() => setIsRevokeModalOpen(false)}
                disabled={revoking}
                className="px-4 py-2 text-xs font-semibold text-slate-600 hover:bg-slate-100 rounded-xl transition-colors cursor-pointer"
              >
                Hủy bỏ
              </button>
              <button
                type="button"
                onClick={handleConfirmRevoke}
                disabled={revoking || revokeReason.trim().length < 5}
                className="inline-flex items-center gap-1.5 px-4 py-2 bg-rose-600 hover:bg-rose-700 disabled:opacity-50 text-white text-xs font-bold rounded-xl transition-all shadow-xs cursor-pointer"
              >
                {revoking ? (
                  <>
                    <RefreshCw className="w-3.5 h-3.5 animate-spin" />
                    <span>Đang xử lý...</span>
                  </>
                ) : (
                  <>
                    <RotateCcw className="w-3.5 h-3.5" />
                    <span>Xác nhận thu hồi</span>
                  </>
                )}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default EvaluationPage;
