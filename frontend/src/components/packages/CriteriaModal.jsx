import React, { useState, useEffect } from 'react';
import { X, Scale, AlertCircle, Sparkles } from 'lucide-react';
import toast from 'react-hot-toast';
import { evaluationApi } from '../../api/evaluationApi';
import { SINGLE_CRITERIA_SUGGESTIONS } from '../../utils/criteriaTemplates';

export const CriteriaModal = ({
  isOpen,
  onClose,
  packageId,
  initialData = null,
  currentTotalWeight = 0,
  onSuccess,
}) => {
  const isEditing = Boolean(initialData?.id);

  const [formData, setFormData] = useState({
    name: '',
    maxScore: 100,
    weight: '',
  });

  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState({});

  useEffect(() => {
    if (initialData) {
      setFormData({
        name: initialData.name || '',
        maxScore: initialData.maxScore || 100,
        weight: initialData.weight || '',
      });
    } else {
      // Khi tạo mới: Gợi ý trọng số còn lại để đủ 100% (nếu còn dư)
      const remainingWeight = Math.max(0, 100 - currentTotalWeight);
      setFormData({
        name: '',
        maxScore: 100,
        weight: remainingWeight > 0 ? remainingWeight : '',
      });
    }
    setErrors({});
  }, [initialData, isOpen, currentTotalWeight]);

  if (!isOpen) return null;

  // Xử lý chọn nhanh gợi ý tên tiêu chí mẫu lẻ (Chỉ điền tên, KHÔNG áp đặt điểm/trọng số)
  const handleSelectSuggestion = (suggestedName) => {
    setFormData((prev) => ({ ...prev, name: suggestedName }));
    if (errors.name) {
      setErrors((prev) => ({ ...prev, name: null }));
    }
  };

  const handleInputChange = (field, value) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => ({ ...prev, [field]: null }));
    }
  };

  // Trọng số còn lại để người dùng theo dõi và làm trần giới hạn
  const baseWeight = isEditing
    ? currentTotalWeight - Number(initialData?.weight || 0)
    : currentTotalWeight;
  const maxAllowedWeight = Math.max(0, 100 - baseWeight);
  const projectedTotal = baseWeight + (Number(formData.weight) || 0);

  const validate = () => {
    const errs = {};
    if (!formData.name.trim()) {
      errs.name = 'Vui lòng nhập tên tiêu chí.';
    } else if (formData.name.trim().length > 200) {
      errs.name = 'Tên tiêu chí không được vượt quá 200 ký tự.';
    }

    const maxScoreNum = Number(formData.maxScore);
    if (!formData.maxScore || isNaN(maxScoreNum) || maxScoreNum <= 0) {
      errs.maxScore = 'Thang điểm tối đa phải lớn hơn 0.';
    }

    const weightNum = Number(formData.weight);
    if (!formData.weight || isNaN(weightNum) || weightNum <= 0) {
      errs.weight = 'Trọng số phải lớn hơn 0%.';
    } else if (weightNum > maxAllowedWeight) {
      errs.weight = maxAllowedWeight > 0
        ? `Trọng số tối đa có thể nhập là ${maxAllowedWeight}% (để tổng không vượt quá 100%).`
        : 'Gói thầu đã đủ 100% trọng số. Không thể đặt thêm trọng số.';
    } else if (projectedTotal > 100) {
      errs.weight = `Tổng trọng số sẽ là ${projectedTotal}%, vượt quá mức tối đa 100%.`;
    }

    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;

    try {
      setLoading(true);
      const payload = {
        name: formData.name.trim(),
        maxScore: Number(formData.maxScore),
        weight: Number(formData.weight),
      };

      if (isEditing) {
        await evaluationApi.updateCriteria(initialData.id, payload);
        toast.success('Cập nhật tiêu chí thành công!');
      } else {
        await evaluationApi.createCriteria(packageId, payload);
        toast.success('Thêm tiêu chí thành công!');
      }

      onSuccess();
      onClose();
    } catch (err) {
      toast.error(err.message || 'Không thể lưu tiêu chí.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/50 backdrop-blur-xs flex items-center justify-center p-4 animate-in fade-in duration-150">
      <div
        className="bg-white rounded-2xl max-w-lg w-full shadow-2xl border border-slate-100 relative overflow-hidden flex flex-col"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header Modal */}
        <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between bg-slate-50/70">
          <div className="flex items-center space-x-2.5">
            <div className="p-2 bg-sky-50 text-sky-600 rounded-xl">
              <Scale className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">
                {isEditing ? 'Chỉnh sửa tiêu chí' : 'Thêm tiêu chí'}
              </h3>
            </div>
          </div>
          <button
            onClick={onClose}
            disabled={loading}
            className="text-slate-400 hover:text-slate-600 p-1.5 rounded-lg hover:bg-slate-200/60 transition disabled:opacity-50"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Thân Form */}
        <form onSubmit={handleSubmit} noValidate className="p-6 space-y-4.5 text-xs">
          {/* Cảnh báo khi gói thầu đã đầy 100% trọng số trong chế độ thêm mới */}
          {!isEditing && maxAllowedWeight <= 0 && (
            <div className="p-3 bg-amber-50 border border-amber-200 rounded-xl text-amber-800 text-[11px] flex items-center space-x-2">
              <AlertCircle className="w-4 h-4 text-amber-600 flex-shrink-0" />
              <span>Gói thầu đã đạt tối đa 100% trọng số. Bạn cần điều chỉnh hoặc xóa bớt tiêu chí hiện có trước khi thêm mới.</span>
            </div>
          )}

          {/* Gợi ý tiêu chí mẫu lẻ */}
          {!isEditing && maxAllowedWeight > 0 && (
            <div className="space-y-1.5 p-3 bg-sky-50/40 rounded-xl border border-sky-100/80">
              <div className="flex items-center space-x-1.5 text-sky-800 font-semibold text-[11px]">
                <Sparkles className="w-3.5 h-3.5 text-sky-600" />
                <span>Gợi ý tiêu chí:</span>
              </div>
              <div className="flex flex-wrap gap-1.5 pt-1">
                {SINGLE_CRITERIA_SUGGESTIONS.map((suggestion) => (
                  <button
                    key={suggestion}
                    type="button"
                    onClick={() => handleSelectSuggestion(suggestion)}
                    className="px-2 py-1 bg-white hover:bg-sky-100 text-slate-700 hover:text-sky-800 border border-slate-200 rounded-lg text-[10px] font-medium transition shadow-2xs"
                  >
                    + {suggestion}
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Tên tiêu chí */}
          <div className="space-y-1.5">
            <div className="flex items-center justify-between">
              <label className="font-semibold text-slate-800">
                Tên tiêu chí <span className="text-rose-500">*</span>
              </label>
              <span className="text-[11px] text-slate-400 font-mono">
                {formData.name.length}/200
              </span>
            </div>
            <input
              type="text"
              maxLength={200}
              value={formData.name}
              onChange={(e) => handleInputChange('name', e.target.value)}
              placeholder="VD: Giải pháp kỹ thuật"
              className={`w-full px-3.5 py-2.5 bg-white border rounded-xl text-xs focus:ring-2 focus:ring-sky-500/20 focus:border-sky-500 transition outline-none ${
                errors.name ? 'border-rose-400 bg-rose-50/30' : 'border-slate-200'
              }`}
            />
            {errors.name && <p className="text-rose-500 text-[11px]">{errors.name}</p>}
          </div>

          {/* Grid 2 cột: Thang điểm tối đa & Trọng số */}
          <div className="grid grid-cols-2 gap-4">
            {/* Điểm tối đa */}
            <div className="space-y-1.5">
              <label className="font-semibold text-slate-800">
                Điểm tối đa <span className="text-rose-500">*</span>
              </label>
              <input
                type="number"
                min="1"
                step="any"
                value={formData.maxScore}
                onChange={(e) => handleInputChange('maxScore', e.target.value)}
                placeholder="100"
                className={`w-full px-3.5 py-2.5 bg-white border rounded-xl text-xs font-mono font-bold focus:ring-2 focus:ring-sky-500/20 focus:border-sky-500 transition outline-none ${
                  errors.maxScore ? 'border-rose-400 bg-rose-50/30' : 'border-slate-200'
                }`}
              />
              {errors.maxScore && <p className="text-rose-500 text-[11px]">{errors.maxScore}</p>}
            </div>

            {/* Trọng số (%) */}
            <div className="space-y-1.5">
              <div className="flex items-center justify-between">
                <label className="font-semibold text-slate-800">
                  Trọng số (%) <span className="text-rose-500">*</span>
                </label>
                <span className="text-[10px] text-slate-400 font-medium">
                  Tối đa: <strong className="text-sky-600 font-mono">{maxAllowedWeight}%</strong>
                </span>
              </div>
              <div className="relative">
                <input
                  type="number"
                  min="0.1"
                  max={maxAllowedWeight > 0 ? maxAllowedWeight : 0}
                  step="any"
                  value={formData.weight}
                  onChange={(e) => handleInputChange('weight', e.target.value)}
                  placeholder={`VD: ${maxAllowedWeight > 0 ? Math.min(20, maxAllowedWeight) : 0}`}
                  className={`w-full pl-3.5 pr-8 py-2.5 bg-white border rounded-xl text-xs font-mono font-bold focus:ring-2 focus:ring-sky-500/20 focus:border-sky-500 transition outline-none ${
                    errors.weight || projectedTotal > 100
                      ? 'border-rose-400 bg-rose-50/30'
                      : 'border-slate-200'
                  }`}
                />
                <span className="absolute right-3 top-2.5 text-slate-400 font-bold">%</span>
              </div>
              {errors.weight && <p className="text-rose-500 text-[11px]">{errors.weight}</p>}
            </div>
          </div>

          {/* Dự báo tổng trọng số */}
          <div className="p-3 bg-slate-50 border border-slate-100 rounded-xl flex items-center justify-between">
            <span className="text-slate-600 font-medium">Tổng trọng số dự kiến:</span>
            <span
              className={`font-mono font-bold text-xs px-2.5 py-1 rounded-lg border ${
                projectedTotal === 100
                  ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
                  : projectedTotal > 100
                  ? 'bg-rose-50 text-rose-700 border-rose-200'
                  : 'bg-amber-50 text-amber-700 border-amber-200'
              }`}
            >
              {projectedTotal}% {projectedTotal > 100 && '(Vượt quá 100%)'}
            </span>
          </div>

          {projectedTotal > 100 && (
            <p className="text-rose-600 text-[11px] bg-rose-50 p-2.5 rounded-xl border border-rose-100 font-medium">
              Không thể lưu do tổng trọng số vượt quá 100%. Vui lòng giảm trọng số xuống tối đa {maxAllowedWeight}%.
            </p>
          )}

          {/* Footer nút hành động */}
          <div className="pt-3 border-t border-slate-100 flex items-center justify-end space-x-2.5">
            <button
              type="button"
              onClick={onClose}
              disabled={loading}
              className="px-4 py-2 text-xs font-semibold text-slate-600 hover:bg-slate-100 rounded-xl transition border border-slate-200 disabled:opacity-50"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={loading || projectedTotal > 100 || (!isEditing && maxAllowedWeight <= 0)}
              className="px-5 py-2 text-xs font-bold text-white bg-slate-900 hover:bg-slate-800 rounded-xl shadow-xs transition disabled:opacity-50 disabled:cursor-not-allowed flex items-center space-x-1.5"
            >
              {loading && (
                <svg className="animate-spin -ml-1 mr-1.5 h-3.5 w-3.5 text-white" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
              )}
              <span>{isEditing ? 'Lưu thay đổi' : 'Thêm tiêu chí'}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default CriteriaModal;
