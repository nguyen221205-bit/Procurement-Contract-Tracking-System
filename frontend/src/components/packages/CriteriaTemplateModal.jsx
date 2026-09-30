import React, { useState } from 'react';
import { X, CheckCircle2, Layers, AlertTriangle, ArrowRight } from 'lucide-react';
import toast from 'react-hot-toast';
import { evaluationApi } from '../../api/evaluationApi';
import { CRITERIA_PACKAGE_TEMPLATES } from '../../utils/criteriaTemplates';

export const CriteriaTemplateModal = ({
  isOpen,
  onClose,
  packageId,
  existingCriteria = [],
  onSuccess,
}) => {
  const [selectedTemplateId, setSelectedTemplateId] = useState('service');
  const [loading, setLoading] = useState(false);

  if (!isOpen) return null;

  const currentTemplate =
    CRITERIA_PACKAGE_TEMPLATES.find((t) => t.id === selectedTemplateId) ||
    CRITERIA_PACKAGE_TEMPLATES[0];

  const handleApplyTemplate = async () => {
    if (!currentTemplate || !packageId) return;

    // Nếu đã có tiêu chí, cảnh báo người dùng trước khi thay thế
    if (existingCriteria.length > 0) {
      const confirmReplace = window.confirm(
        `Thao tác này sẽ thay thế các tiêu chí hiện tại bằng bộ mẫu "${currentTemplate.name}". Bạn có muốn tiếp tục?`
      );
      if (!confirmReplace) return;
    }

    try {
      setLoading(true);

      // 1. Xóa các tiêu chí cũ nếu có
      if (existingCriteria.length > 0) {
        for (const crit of existingCriteria) {
          try {
            await evaluationApi.deleteCriteria(crit.id);
          } catch (err) {
            console.warn(`Lỗi khi xóa tiêu chí cũ #${crit.id}:`, err);
          }
        }
      }

      // 2. Tạo tuần tự các tiêu chí trong bộ mẫu mới
      for (const item of currentTemplate.items) {
        await evaluationApi.createCriteria(packageId, {
          name: item.name,
          maxScore: item.maxScore,
          weight: item.weight,
        });
      }

      toast.success(`Đã áp dụng bộ tiêu chí "${currentTemplate.name}"!`);
      onSuccess();
      onClose();
    } catch (err) {
      toast.error(err.message || 'Có lỗi xảy ra khi áp dụng bộ tiêu chí mẫu.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/50 backdrop-blur-xs flex items-center justify-center p-4 animate-in fade-in duration-150">
      <div
        className="bg-white rounded-2xl max-w-2xl w-full shadow-2xl border border-slate-100 relative overflow-hidden flex flex-col max-h-[90vh]"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header Modal */}
        <div className="px-6 py-4.5 border-b border-slate-100 flex items-center justify-between bg-slate-50/70">
          <div className="flex items-center space-x-2.5">
            <div className="p-2 bg-indigo-50 text-indigo-600 rounded-xl">
              <Layers className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">Bộ tiêu chí mẫu</h3>
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

        {/* Nội dung Modal */}
        <div className="p-6 overflow-y-auto space-y-5 text-xs flex-1">
          {/* Lựa chọn Bộ tiêu chí mẫu */}
          <div className="space-y-2">
            <label className="font-semibold text-slate-800">
              Chọn bộ mẫu:
            </label>
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-2.5">
              {CRITERIA_PACKAGE_TEMPLATES.map((tmpl) => {
                const isSelected = selectedTemplateId === tmpl.id;
                return (
                  <button
                    key={tmpl.id}
                    type="button"
                    onClick={() => setSelectedTemplateId(tmpl.id)}
                    className={`p-3 rounded-xl border text-left transition relative flex flex-col justify-between space-y-2 ${
                      isSelected
                        ? 'border-indigo-600 bg-indigo-50/50 ring-2 ring-indigo-500/20 shadow-xs'
                        : 'border-slate-200 hover:border-slate-300 hover:bg-slate-50'
                    }`}
                  >
                    <div>
                      <div className="flex items-center justify-between">
                        <span
                          className={`font-bold text-xs ${
                            isSelected ? 'text-indigo-900' : 'text-slate-800'
                          }`}
                        >
                          {tmpl.name}
                        </span>
                        {isSelected && <CheckCircle2 className="w-4 h-4 text-indigo-600 flex-shrink-0" />}
                      </div>
                      <p className="text-[10px] text-slate-500 mt-1 line-clamp-2 leading-relaxed">
                        {tmpl.description}
                      </p>
                    </div>
                    <span className="text-[10px] font-semibold text-slate-400 font-mono">
                      {tmpl.items.length} tiêu chí
                    </span>
                  </button>
                );
              })}
            </div>
          </div>

          {/* Bảng xem trước (Preview) chi tiết các tiêu chí trong bộ mẫu đang chọn */}
          <div className="space-y-2.5 pt-2 border-t border-slate-100">
            <div className="flex items-center justify-between">
              <span className="font-bold text-slate-900">
                Chi tiết: <span className="text-indigo-600">{currentTemplate.name}</span>
              </span>
              <span className="px-2.5 py-0.5 bg-emerald-50 text-emerald-700 border border-emerald-200 rounded-lg font-mono font-bold text-[11px]">
                Tổng: 100%
              </span>
            </div>

            <div className="border border-slate-100 rounded-xl overflow-hidden shadow-2xs">
              <table className="w-full text-left text-xs">
                <thead className="bg-slate-50 text-slate-600 border-b border-slate-100 font-semibold">
                  <tr>
                    <th className="py-2.5 px-3 w-10 text-center">STT</th>
                    <th className="py-2.5 px-3">Tên tiêu chí</th>
                    <th className="py-2.5 px-3 w-28 text-center">Điểm tối đa</th>
                    <th className="py-2.5 px-3 w-28 text-center">Trọng số</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-slate-700">
                  {currentTemplate.items.map((item, idx) => (
                    <tr key={idx} className="hover:bg-slate-50/60">
                      <td className="py-2.5 px-3 text-center font-mono text-slate-400">{idx + 1}</td>
                      <td className="py-2.5 px-3 font-semibold text-slate-800">{item.name}</td>
                      <td className="py-2.5 px-3 text-center font-mono">{item.maxScore} đ</td>
                      <td className="py-2.5 px-3 text-center font-mono font-extrabold text-indigo-600">
                        {item.weight}%
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          {/* Cảnh báo nếu đã có tiêu chí cũ */}
          {existingCriteria.length > 0 && (
            <div className="p-3 bg-amber-50 border border-amber-200 rounded-xl flex items-start space-x-2.5 text-amber-800">
              <AlertTriangle className="w-4 h-4 text-amber-600 flex-shrink-0 mt-0.5" />
              <p className="text-[11px] leading-relaxed">
                Áp dụng bộ mẫu sẽ thay thế các tiêu chí hiện tại của gói thầu.
              </p>
            </div>
          )}
        </div>

        {/* Footer Modal */}
        <div className="px-6 py-3.5 border-t border-slate-100 flex items-center justify-end space-x-2.5 bg-slate-50/50">
          <button
            type="button"
            onClick={onClose}
            disabled={loading}
            className="px-4 py-2 text-xs font-semibold text-slate-600 hover:bg-slate-100 rounded-xl transition border border-slate-200 disabled:opacity-50"
          >
            Hủy
          </button>
          <button
            type="button"
            onClick={handleApplyTemplate}
            disabled={loading}
            className="px-5 py-2 text-xs font-bold text-white bg-indigo-600 hover:bg-indigo-700 rounded-xl shadow-xs transition disabled:opacity-50 flex items-center space-x-1.5"
          >
            {loading && (
              <svg className="animate-spin -ml-1 mr-1.5 h-3.5 w-3.5 text-white" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
            )}
            <span>Áp dụng</span>
            <ArrowRight className="w-3.5 h-3.5 ml-1" />
          </button>
        </div>
      </div>
    </div>
  );
};

export default CriteriaTemplateModal;
