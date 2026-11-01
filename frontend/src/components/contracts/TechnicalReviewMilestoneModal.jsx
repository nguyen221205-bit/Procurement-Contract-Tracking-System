import React, { useState } from 'react';
import toast from 'react-hot-toast';
import {
  CheckCircle2,
  XCircle,
  X,
  ClipboardCheck,
  Calendar,
  DollarSign,
  AlertTriangle,
  Send,
  ShieldAlert
} from 'lucide-react';
import { contractApi } from '../../api/contractApi';
import { formatCurrency, formatDate } from '../../utils/formatters';

export const TechnicalReviewMilestoneModal = ({
  isOpen,
  onClose,
  milestone,
  onSuccess,
}) => {
  const [isPassed, setIsPassed] = useState(true);
  const [note, setNote] = useState('');
  const [submitting, setSubmitting] = useState(false);

  if (!isOpen || !milestone) return null;

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!note.trim()) {
      toast.error('Vui lòng nhập ghi chú kiểm tra hiện trường hoặc số biên bản kỹ thuật.');
      return;
    }

    try {
      setSubmitting(true);
      const res = await contractApi.technicalReviewMilestone(
        milestone.id,
        isPassed,
        note.trim()
      );

      if (res && res.success) {
        if (isPassed) {
          toast.success(
            'Xác nhận kỹ thuật ĐẠT thành công! Hồ sơ đã được chuyển lên Lãnh đạo phê duyệt giải ngân.'
          );
        } else {
          toast.success('Đã ghi nhận kết quả kiểm tra kỹ thuật KHÔNG ĐẠT.');
        }

        onClose();
        if (onSuccess) onSuccess();
      } else {
        toast.error(res?.message || 'Không thể ghi nhận kết quả kiểm tra kỹ thuật.');
      }
    } catch (err) {
      toast.error(err.message || 'Lỗi khi xác nhận kỹ thuật mốc.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
      <div className="bg-white rounded-3xl shadow-2xl border border-slate-200 w-full max-w-lg overflow-hidden animate-in fade-in zoom-in duration-200">
        {/* Header */}
        <div
          className={`px-6 py-5 text-white flex items-center justify-between ${
            isPassed
              ? 'bg-gradient-to-r from-blue-700 via-indigo-700 to-sky-700'
              : 'bg-gradient-to-r from-rose-700 via-red-700 to-amber-700'
          }`}
        >
          <div className="flex items-center space-x-3.5">
            <div className="p-2.5 bg-white/20 rounded-2xl backdrop-blur-md">
              <ClipboardCheck className="w-5 h-5 text-white" />
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <h3 className="text-base font-bold">Xác Nhận Kỹ Thuật Hiện Trường</h3>
                <span className="px-2 py-0.5 rounded-full text-[10px] font-black bg-white/20 text-white">
                  Maker
                </span>
              </div>
              <p className="text-xs text-white/80">
                Kiểm tra khối lượng, chất lượng trước khi trình Lãnh đạo giải ngân
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            disabled={submitting}
            className="text-white/80 hover:text-white p-1 rounded-lg hover:bg-white/10 transition cursor-pointer"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content Form */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          {/* Thông tin mốc nghiệm thu */}
          <div className="p-4 bg-slate-50 border border-slate-200 rounded-2xl space-y-2 text-xs">
            <div className="space-y-0.5">
              <span className="text-slate-500 font-semibold uppercase tracking-wider text-[10px]">
                Hạng mục kiểm tra hiện trường:
              </span>
              <p className="font-bold text-slate-900 text-sm">{milestone.title}</p>
            </div>
            <div className="grid grid-cols-2 gap-2 pt-2 border-t border-slate-200">
              <div className="flex items-center space-x-1.5 text-slate-600">
                <Calendar className="w-3.5 h-3.5 text-slate-400" />
                <span>Hạn chót: <strong>{formatDate(milestone.dueDate)}</strong></span>
              </div>
              <div className="flex items-center space-x-1.5 text-slate-600">
                <DollarSign className="w-3.5 h-3.5 text-slate-400" />
                <span>Giá trị: <strong className="text-emerald-700">{formatCurrency(milestone.amount)}</strong></span>
              </div>
            </div>
          </div>

          {/* Lựa chọn Đạt / Không Đạt */}
          <div className="space-y-1.5">
            <label className="text-xs font-bold text-slate-700">Kết luận kiểm tra kỹ thuật</label>
            <div className="grid grid-cols-2 gap-3">
              <button
                type="button"
                onClick={() => setIsPassed(true)}
                className={`py-3 px-4 rounded-xl border flex items-center justify-center space-x-2 text-xs font-bold transition-all cursor-pointer ${
                  isPassed
                    ? 'bg-blue-50/70 border-blue-500 text-blue-700 shadow-xs'
                    : 'bg-white border-slate-200 text-slate-600 hover:bg-slate-50'
                }`}
              >
                <CheckCircle2 className={`w-4 h-4 ${isPassed ? 'text-blue-600' : 'text-slate-400'}`} />
                <span>ĐẠT YÊU CẦU KỸ THUẬT</span>
              </button>

              <button
                type="button"
                onClick={() => setIsPassed(false)}
                className={`py-3 px-4 rounded-xl border flex items-center justify-center space-x-2 text-xs font-bold transition-all cursor-pointer ${
                  !isPassed
                    ? 'bg-rose-50/70 border-rose-500 text-rose-700 shadow-xs'
                    : 'bg-white border-slate-200 text-slate-600 hover:bg-slate-50'
                }`}
              >
                <XCircle className={`w-4 h-4 ${!isPassed ? 'text-rose-600' : 'text-slate-400'}`} />
                <span>KHÔNG ĐẠT / CẦN SỬA</span>
              </button>
            </div>
          </div>

          {/* Maker-Checker notice */}
          <div className="p-3.5 bg-indigo-50/60 border border-indigo-200 rounded-xl text-xs text-indigo-900 flex items-start space-x-2.5">
            <ShieldAlert className="w-4 h-4 text-indigo-600 flex-shrink-0 mt-0.5" />
            <div className="space-y-0.5 leading-relaxed text-[11px]">
              <span className="font-bold">Quy tắc phân tách trách nhiệm Maker - Checker:</span>
              <p>
                Sau khi bạn xác nhận kỹ thuật mốc này, hệ thống sẽ ghi nhận bạn là <strong>Người kiểm tra kỹ thuật (Maker)</strong>.
                Bạn sẽ <strong>không được phép</strong> tự ký quyết định giải ngân tài chính cho chính mốc này (phải do Lãnh đạo/Checker phê duyệt).
              </p>
            </div>
          </div>

          {/* Ghi chú kiểm tra */}
          <div className="space-y-1">
            <label className="text-xs font-bold text-slate-700">
              Biên bản kiểm tra hiện trường & Ghi chú kỹ thuật <span className="text-rose-500">*</span>
            </label>
            <textarea
              required
              rows={3}
              value={note}
              onChange={(e) => setNote(e.target.value)}
              placeholder="VD: Đã kiểm tra cao độ móng, bê tông đạt mác theo phiếu thí nghiệm số 12/TN-2026. Hồ sơ bản vẽ hoàn công đầy đủ."
              className="w-full p-3 bg-white border border-slate-200 rounded-xl text-xs focus:ring-2 focus:ring-blue-500 focus:outline-none"
            />
          </div>

          {/* Action buttons */}
          <div className="flex items-center justify-end space-x-2 pt-2">
            <button
              type="button"
              onClick={onClose}
              disabled={submitting}
              className="px-4 py-2.5 text-xs font-semibold text-slate-600 hover:bg-slate-100 rounded-xl transition cursor-pointer"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={submitting}
              className={`px-5 py-2.5 text-xs font-bold text-white rounded-xl transition shadow-xs flex items-center space-x-1.5 cursor-pointer disabled:opacity-50 ${
                isPassed ? 'bg-blue-600 hover:bg-blue-700' : 'bg-rose-600 hover:bg-rose-700'
              }`}
            >
              <Send className="w-3.5 h-3.5" />
              <span>{submitting ? 'Đang lưu...' : 'Ghi nhận kết quả kỹ thuật'}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default TechnicalReviewMilestoneModal;
