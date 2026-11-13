import React from 'react';
import { Scale, Layers, Plus, AlertCircle, Edit2, Trash2 } from 'lucide-react';

const WEIGHT_UNIT = '\u0025';

const resolveWeightBadgeStyle = (totalWeight) => {
  if (totalWeight === 100) {
    return 'bg-emerald-50 text-emerald-700 border-emerald-200';
  }
  if (totalWeight > 100) {
    return 'bg-rose-50 text-rose-700 border-rose-200';
  }
  return 'bg-amber-50 text-amber-700 border-amber-200';
};

export const PackageCriteriaTab = ({
  criteria = [],
  totalWeight = 0,
  canEditCriteria = false,
  onOpenTemplateModal,
  onOpenAddCriteria,
  onOpenEditCriteria,
  onOpenDeleteCriteria,
}) => {
  const isWeightFull = totalWeight >= 100;
  const isWeightExceeded = totalWeight > 100;

  return (
    <div className="bg-white rounded-2xl border border-slate-200 shadow-xs p-6 space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 pb-3 border-b border-slate-100">
        <div>
          <h3 className="text-sm font-bold text-slate-900 flex items-center space-x-2">
            <Scale className="w-4 h-4 text-sky-600" />
            <span>Tiêu chí đánh giá</span>
          </h3>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <span
            className={`inline-flex items-center space-x-1 px-3 py-1.5 rounded-xl text-xs font-bold border ${resolveWeightBadgeStyle(
              totalWeight
            )}`}
          >
            <span>{`Tổng: ${totalWeight}${WEIGHT_UNIT}`}</span>
          </span>

          {canEditCriteria && criteria.length > 0 && (
            <div className="flex items-center space-x-2">
              <button
                type="button"
                onClick={onOpenTemplateModal}
                className="px-3 py-1.5 bg-indigo-50 hover:bg-indigo-100 text-indigo-700 border border-indigo-200 rounded-xl text-xs font-semibold transition flex items-center space-x-1"
              >
                <Layers className="w-3.5 h-3.5" />
                <span>Áp dụng bộ mẫu</span>
              </button>

              <button
                type="button"
                onClick={onOpenAddCriteria}
                disabled={isWeightFull}
                className={`px-3 py-1.5 rounded-xl text-xs font-semibold transition shadow-xs flex items-center space-x-1 ${
                  isWeightFull
                    ? 'bg-slate-100 text-slate-400 border border-slate-200 cursor-not-allowed'
                    : 'bg-sky-600 hover:bg-sky-700 text-white'
                }`}
                title={isWeightFull ? `Đã đạt tối đa 100${WEIGHT_UNIT} trọng số` : 'Thêm tiêu chí'}
              >
                <Plus className="w-3.5 h-3.5" />
                <span>Thêm tiêu chí</span>
              </button>
            </div>
          )}
        </div>
      </div>

      {isWeightExceeded && (
        <div className="p-3 bg-rose-50 border border-rose-200 rounded-xl text-rose-800 text-xs flex items-center space-x-2">
          <AlertCircle className="w-4 h-4 text-rose-600 flex-shrink-0" />
          <span>
            Tổng trọng số đang là <strong>{`${totalWeight}${WEIGHT_UNIT}`}</strong>, vượt mức quy định (
            {`100${WEIGHT_UNIT}`}). Vui lòng chỉnh sửa hoặc xóa bớt tiêu chí.
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
                {canEditCriteria && <th className="py-3 px-4 w-24 text-right">Thao tác</th>}
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
                    {`${c.weight}${WEIGHT_UNIT}`}
                  </td>
                  {canEditCriteria && (
                    <td className="py-3 px-4 text-right space-x-1">
                      <button
                        type="button"
                        onClick={() => onOpenEditCriteria(c)}
                        className="p-1.5 text-slate-400 hover:text-sky-600 hover:bg-sky-50 rounded-lg transition"
                        title="Chỉnh sửa"
                      >
                        <Edit2 className="w-3.5 h-3.5" />
                      </button>
                      <button
                        type="button"
                        onClick={() => onOpenDeleteCriteria(c)}
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
                onClick={onOpenTemplateModal}
                className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl font-semibold shadow-xs transition flex items-center space-x-1.5"
              >
                <Layers className="w-4 h-4" />
                <span>Áp dụng bộ mẫu</span>
              </button>
              <button
                type="button"
                onClick={onOpenAddCriteria}
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
  );
};

export default PackageCriteriaTab;
