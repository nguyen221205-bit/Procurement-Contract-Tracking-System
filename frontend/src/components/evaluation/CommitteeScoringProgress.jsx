import React from 'react';
import { Users, CheckCircle2, AlertCircle } from 'lucide-react';

export const CommitteeScoringProgress = ({
  evaluators = [],
  rankings = [],
}) => {
  // Lọc các hồ sơ còn hợp lệ (không Withdrawn và không Disqualified)
  const activeSubmissions = rankings.filter(
    (s) => s.status !== 'Withdrawn' && s.status !== 'Disqualified'
  );

  // Đếm số lượng hồ sơ đã hoàn thành chấm điểm (trạng thái Evaluated)
  const evaluatedCount = activeSubmissions.filter((s) => s.status === 'Evaluated').length;
  const totalActive = activeSubmissions.length;
  const progressPercent = totalActive > 0 ? Math.round((evaluatedCount / totalActive) * 100) : 0;
  const isAllCompleted = totalActive > 0 && evaluatedCount === totalActive;

  return (
    <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs p-5 space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div className="flex items-center space-x-3">
          <div className="p-2.5 bg-indigo-50 text-indigo-600 rounded-xl">
            <Users className="w-5 h-5" />
          </div>
          <div>
            <h3 className="text-sm font-bold text-slate-900 flex items-center space-x-2">
              <span>Tiến Độ Đánh Giá Của Tổ Chuyên Gia</span>
              <span
                className={`px-2 py-0.5 rounded-full text-[10px] font-bold ${
                  isAllCompleted
                    ? 'bg-emerald-100 text-emerald-800'
                    : 'bg-amber-100 text-amber-800'
                }`}
              >
                {isAllCompleted ? 'Đã hoàn thành 100%' : `Đang chấm: ${progressPercent}%`}
              </span>
            </h3>
            <p className="text-xs text-slate-500">
              Tổ chuyên gia gồm {evaluators.length} thành viên đánh giá {totalActive} hồ sơ dự thầu hợp lệ
            </p>
          </div>
        </div>

        {/* Chỉ số hoàn thành */}
        <div className="flex items-center space-x-4 text-xs font-mono">
          <div className="text-right">
            <span className="text-slate-400 block text-[10px]">HỒ SƠ ĐÃ CHẤM XONG</span>
            <span className="font-bold text-slate-800 text-sm">
              {evaluatedCount} / {totalActive}
            </span>
          </div>
        </div>
      </div>

      {/* Thanh tiến độ trực quan */}
      <div className="w-full bg-slate-100 h-2.5 rounded-full overflow-hidden">
        <div
          className={`h-full transition-all duration-500 rounded-full ${
            isAllCompleted ? 'bg-emerald-500' : 'bg-indigo-600'
          }`}
          style={{ width: `${progressPercent}%` }}
        />
      </div>

      {/* Cảnh báo nếu chưa hoàn thành */}
      {!isAllCompleted && (
        <div className="flex items-center space-x-2 text-[11px] text-amber-800 bg-amber-50/80 p-2.5 rounded-xl border border-amber-200/70">
          <AlertCircle className="w-4 h-4 text-amber-600 flex-shrink-0" />
          <span>
            <strong>Ràng buộc pháp lý (NĐ 24/2024/NĐ-CP):</strong> Còn{' '}
            <strong>{totalActive - evaluatedCount} hồ sơ</strong> chưa hoàn tất chấm điểm bởi đầy đủ giám khảo. Nút phê duyệt trao thầu sẽ tạm khóa cho đến khi tiến độ đạt 100%.
          </span>
        </div>
      )}
    </div>
  );
};

export default CommitteeScoringProgress;
