import React, { useState } from 'react';
import toast from 'react-hot-toast';
import {
  CheckCircle,
  XCircle,
  X,
  FileCheck,
  DollarSign,
  Calendar,
  AlertTriangle,
  Send
} from 'lucide-react';
import { contractApi } from '../../api/contractApi';
import { formatCurrency, formatDate } from '../../utils/formatters';

export const ApproveMilestoneModal = ({
  isOpen,
  onClose,
  milestone,
  onSuccess,
}) => {
  const [isApproved, setIsApproved] = useState(true);
  const [note, setNote] = useState('');
  const [submitting, setSubmitting] = useState(false);

  if (!isOpen || !milestone) return null;

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!isApproved && !note.trim()) {
      toast.error('Vui lòng nêu rõ lý do hoặc nội dung yêu cầu khắc phục khi từ chối nghiệm thu.');
      return;
    }

    try {
      setSubmitting(true);
      const res = await contractApi.approveMilestoneAcceptance(
        milestone.id,
        isApproved,
        note.trim()
      );

      if (isApproved) {
        toast.success(
          `Đã phê duyệt nghiệm thu mốc thành công! Đã giải ngân ${formatCurrency(milestone.amount)}.`
        );
      } else {
        toast.success('Đã gửi thông báo từ chối nghiệm thu kèm yêu cầu khắc phục.');
      }

      onClose();
      if (onSuccess) onSuccess();
    } catch (err) {
      toast.error(err.message || 'Không thể phê duyệt nghiệm thu mốc thanh toán.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-xl border border-slate-200 w-full max-w-lg overflow-hidden animate-in fade-in zoom-in duration-200">
        {/* Header */}
        <div
          className={`px-6 py-4 text-white flex items-center justify-between ${
            isApproved
              ? 'bg-gradient-to-r from-emerald-600 to-teal-700'
              : 'bg-gradient-to-r from-rose-600 to-red-700'
          }`}
        >
          <div className="flex items-center space-x-3">
            <div className="p-2 bg-white/20 rounded-xl backdrop-blur-xs">
              <FileCheck className="w-5 h-5 text-white" />
            </div>
            <div>
              <h3 className="text-base font-bold">Biên Bản Nghiệm Thu Mốc Thanh Toán</h3>
              <p className="text-xs text-white/80">
                Thẩm định khối lượng hoàn thành & kích hoạt giải ngân
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            disabled={submitting}
            className="text-white/80 hover:text-white p-1 rounded-lg hover:bg-white/10 transition-colors cursor-pointer"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content Form */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          {/* Thông tin mốc nghiệm thu */}
          <div className="p-4 bg-slate-50 border border-slate-200 rounded-xl space-y-2 text-xs">
            <div className="space-y-0.5">
              <span className="text-slate-500 font-medium">Hạng mục nghiệm thu:</span>
              <p className="font-bold text-slate-900 text-sm">{milestone.title}</p>
            </div>
            <div className="grid grid-cols-2 gap-2 pt-2 border-t border-slate-200">
              <div>
                <span className="text-slate-500 block">Số tiền giải ngân:</span>
                <strong className="text-emerald-700 text-sm font-black">
                  {formatCurrency(milestone.amount)}
                </strong>
              </div>
              <div>
                <span className="text-slate-500 block">Hạn nghiệm thu:</span>
                <span className="text-slate-800 font-semibold">
                  {formatDate(milestone.dueDate)}
                </span>
              </div>
            </div>
          </div>

          {/* Chọn quyết định nghiệm thu */}
          <div className="space-y-2">
            <label className="text-xs font-bold text-slate-700">
              Kết quả đánh giá nghiệm thu <span className="text-rose-500">*</span>
            </label>
            <div className="grid grid-cols-2 gap-3">
              <button
                type="button"
                onClick={() => setIsApproved(true)}
                className={`p-3 rounded-xl border text-xs font-bold flex items-center justify-center gap-2 transition-all cursor-pointer ${
                  isApproved
                    ? 'border-emerald-500 bg-emerald-50 text-emerald-800 ring-2 ring-emerald-500/20 shadow-xs'
                    : 'border-slate-200 text-slate-600 hover:bg-slate-50'
                }`}
              >
                <CheckCircle className="w-4 h-4 text-emerald-600" />
                <span>Chấp thuận nghiệm thu</span>
              </button>

              <button
                type="button"
                onClick={() => setIsApproved(false)}
                className={`p-3 rounded-xl border text-xs font-bold flex items-center justify-center gap-2 transition-all cursor-pointer ${
                  !isApproved
                    ? 'border-rose-500 bg-rose-50 text-rose-800 ring-2 ring-rose-500/20 shadow-xs'
                    : 'border-slate-200 text-slate-600 hover:bg-slate-50'
                }`}
              >
                <XCircle className="w-4 h-4 text-rose-600" />
                <span>Từ chối / Yêu cầu sửa</span>
              </button>
            </div>
          </div>

          {/* Ý kiến thẩm định / Lý do từ chối */}
          <div className="space-y-1">
            <label className="text-xs font-bold text-slate-700 flex justify-between">
              <span>
                {isApproved
                  ? 'Ý kiến thẩm định biên bản nghiệm thu (Tùy chọn)'
                  : 'Lý do từ chối & Yêu cầu khắc phục (Bắt buộc)'}
              </span>
              <span className="text-[10px] text-slate-400 font-normal">Tối đa 1000 ký tự</span>
            </label>
            <textarea
              rows={3}
              required={!isApproved}
              value={note}
              onChange={(e) => setNote(e.target.value)}
              placeholder={
                isApproved
                  ? 'Biên bản kiểm tra đạt yêu cầu kỹ thuật, cho phép giải ngân theo quy định...'
                  : 'Nêu rõ các hạng mục chưa đạt yêu cầu, lỗi kỹ thuật cần khắc phục...'
              }
              className="w-full text-xs text-slate-800 bg-white border border-slate-300 rounded-xl p-3 focus:ring-2 focus:ring-blue-500 focus:outline-hidden leading-relaxed"
            />
          </div>

          {/* Cảnh báo giải ngân */}
          {isApproved ? (
            <div className="p-3 bg-emerald-50 border border-emerald-200 rounded-xl text-xs text-emerald-800 flex items-start gap-2">
              <CheckCircle className="w-4 h-4 text-emerald-600 flex-shrink-0 mt-0.5" />
              <span>
                Sau khi phê duyệt, số tiền <strong>{formatCurrency(milestone.amount)}</strong> sẽ được ghi nhận vào tổng số tiền đã giải ngân của hợp đồng.
              </span>
            </div>
          ) : (
            <div className="p-3 bg-rose-50 border border-rose-200 rounded-xl text-xs text-rose-800 flex items-start gap-2">
              <AlertTriangle className="w-4 h-4 text-rose-600 flex-shrink-0 mt-0.5" />
              <span>
                Mốc này sẽ được chuyển về trạng thái cần xử lý lại để nhà thầu khắc phục các yêu cầu.
              </span>
            </div>
          )}

          {/* Footer buttons */}
          <div className="pt-4 border-t border-slate-100 flex items-center justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              disabled={submitting}
              className="px-4 py-2.5 text-xs font-bold text-slate-600 hover:text-slate-800 hover:bg-slate-100 rounded-xl transition-colors cursor-pointer"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={submitting}
              className={`inline-flex items-center gap-1.5 px-6 py-2.5 text-xs font-bold text-white rounded-xl shadow-xs transition-transform hover:scale-[1.02] cursor-pointer ${
                isApproved
                  ? 'bg-emerald-600 hover:bg-emerald-700'
                  : 'bg-rose-600 hover:bg-rose-700'
              }`}
            >
              {submitting ? (
                <>
                  <span className="w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                  <span>Đang xử lý...</span>
                </>
              ) : (
                <>
                  <Send className="w-4 h-4" />
                  <span>{isApproved ? 'Xác nhận Nghiệm thu & Giải ngân' : 'Gửi Từ chối'}</span>
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ApproveMilestoneModal;
