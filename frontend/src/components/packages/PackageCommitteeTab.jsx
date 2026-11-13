import React from 'react';
import { Users, CheckCircle2, AlertCircle, Plus, Trash2 } from 'lucide-react';
import LoadingSpinner from '../common/LoadingSpinner';
import { formatDate } from '../../utils/formatters';

export const PackageCommitteeTab = ({
  evaluators = [],
  evaluatorsLoading = false,
  isCommitteeValid = false,
  canManageCommittee = false,
  isEvaluating = false,
  currentUserId = null,
  onOpenAssignModal,
  onOpenDeleteEvaluator,
}) => {
  const statusBannerStyle = isCommitteeValid
    ? 'bg-emerald-50/70 border-emerald-200/80 text-emerald-800'
    : 'bg-amber-50 border-amber-200 text-amber-800';

  const statusBadgeStyle = isCommitteeValid
    ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
    : 'bg-amber-50 text-amber-700 border-amber-200';

  const StatusIcon = isCommitteeValid ? CheckCircle2 : AlertCircle;
  const statusIconColor = isCommitteeValid ? 'text-emerald-600' : 'text-amber-600';

  return (
    <div className="bg-white rounded-2xl border border-slate-200 shadow-xs p-6 space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 pb-3 border-b border-slate-100">
        <div>
          <h3 className="text-sm font-bold text-slate-900 flex items-center space-x-2">
            <Users className="w-4 h-4 text-sky-600" />
            <span>Tổ chuyên gia chấm thầu</span>
          </h3>
          <p className="text-xs text-slate-500 mt-0.5">Giám khảo tham gia chấm điểm gói thầu</p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <span
            className={`inline-flex items-center space-x-1.5 px-3 py-1.5 rounded-xl text-xs font-bold border ${statusBadgeStyle}`}
          >
            <StatusIcon className={`w-3.5 h-3.5 ${statusIconColor}`} />
            <span>
              {isCommitteeValid
                ? `Đủ điều kiện (${evaluators.length} thành viên)`
                : `Chưa đủ điều kiện (${evaluators.length} thành viên)`}
            </span>
          </span>

          {canManageCommittee && (
            <button
              type="button"
              onClick={onOpenAssignModal}
              className="px-3 py-1.5 bg-sky-600 hover:bg-sky-700 text-white rounded-xl text-xs font-semibold transition shadow-xs flex items-center space-x-1.5 cursor-pointer"
            >
              <Plus className="w-3.5 h-3.5" />
              <span>Phân công Giám khảo</span>
            </button>
          )}
        </div>
      </div>

      <div className={`p-3 rounded-xl border text-xs flex items-center space-x-2.5 ${statusBannerStyle}`}>
        <StatusIcon className={`w-4 h-4 ${statusIconColor} shrink-0`} />
        <p className="leading-normal">
          <strong>{isCommitteeValid ? 'Đạt yêu cầu:' : 'Chưa đủ điều kiện:'}</strong> Tối thiểu{' '}
          <strong>3 thành viên</strong> và là <strong>số lẻ (3, 5, 7...)</strong> để mở chấm thầu.
        </p>
      </div>

      {evaluatorsLoading ? (
        <div className="py-12 flex justify-center">
          <LoadingSpinner />
        </div>
      ) : evaluators.length > 0 ? (
        <div className="border border-slate-100 rounded-xl overflow-hidden divide-y divide-slate-100">
          {evaluators.map((evaluator, index) => {
            const isDeleteDisabled = isEvaluating && evaluator.hasSubmittedScores;
            return (
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
                      {evaluator.evaluatorId === currentUserId && (
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

                  {canManageCommittee && (
                    <button
                      type="button"
                      onClick={() => onOpenDeleteEvaluator(evaluator)}
                      disabled={isDeleteDisabled}
                      className={`p-1.5 rounded-lg border transition ${
                        isDeleteDisabled
                          ? 'text-slate-300 border-slate-200 cursor-not-allowed bg-slate-50'
                          : 'text-slate-400 hover:text-rose-600 hover:bg-rose-50 hover:border-rose-200 border-slate-200 cursor-pointer'
                      }`}
                      title={
                        isDeleteDisabled
                          ? 'Giám khảo đã nộp điểm, không thể xóa'
                          : 'Xóa giám khảo khỏi tổ chuyên gia'
                      }
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  )}
                </div>
              </div>
            );
          })}
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
              onClick={onOpenAssignModal}
              className="px-4 py-2 bg-sky-600 hover:bg-sky-700 text-white rounded-xl text-xs font-bold transition shadow-xs inline-flex items-center space-x-1.5 cursor-pointer"
            >
              <Plus className="w-4 h-4" />
              <span>Phân công Giám khảo ngay</span>
            </button>
          )}
        </div>
      )}
    </div>
  );
};

export default PackageCommitteeTab;
