import React, { useState } from 'react';
import { X, RefreshCw, Calendar, FileText, AlertCircle } from 'lucide-react';
import toast from 'react-hot-toast';
import { bidPackageApi } from '../../api/bidPackageApi';
import LoadingSpinner from '../common/LoadingSpinner';

export const ReopenPackageModal = ({ isOpen, onClose, pkg, onSuccess }) => {
  const [newDeadline, setNewDeadline] = useState('');
  const [reason, setReason] = useState('');
  const [submitting, setSubmitting] = useState(false);

  if (!isOpen || !pkg) return null;

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!newDeadline) {
      toast.error('Vui lòng chọn thời hạn nộp thầu mới.');
      return;
    }

    const selectedTime = new Date(newDeadline).getTime();
    const now = new Date().getTime();
    if (selectedTime <= now) {
      toast.error('Thời hạn nộp thầu mới phải ở thời điểm tương lai.');
      return;
    }

    if (!reason.trim() || reason.trim().length < 10) {
      toast.error('Vui lòng nhập lý do mở lại & gia hạn gói thầu (tối thiểu 10 ký tự).');
      return;
    }

    try {
      setSubmitting(true);
      await bidPackageApi.reopenPackage(pkg.id, {
        newDeadline: new Date(newDeadline).toISOString(),
        reason: reason.trim(),
      });
      toast.success('Mở lại và gia hạn thời hạn nhận thầu thành công!');
      onSuccess?.();
      onClose();
    } catch (err) {
      toast.error(err.message || 'Không thể mở lại gói thầu.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-xs">
      <div className="bg-white w-full max-w-lg rounded-2xl shadow-2xl border border-slate-200 overflow-hidden flex flex-col animate-in fade-in zoom-in-95 duration-150">
        {/* Header */}
        <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between bg-slate-50/50">
          <div className="flex items-center space-x-3">
            <div className="p-2 bg-amber-50 text-amber-600 rounded-xl">
              <RefreshCw className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">
                Mở Lại & Gia Hạn Thời Hạn Nộp Thầu
              </h3>
              <p className="text-xs text-slate-500 font-mono">
                Gói thầu: {pkg.code}
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 text-slate-400 hover:text-slate-600 rounded-lg hover:bg-slate-100 transition cursor-pointer"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Form Body */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4 text-xs">
          <div className="p-3 bg-amber-50/80 border border-amber-200/80 rounded-xl text-amber-800 space-y-1">
            <div className="flex items-center space-x-1.5 font-bold">
              <AlertCircle className="w-4 h-4 text-amber-600 flex-shrink-0" />
              <span>Lưu ý pháp lý khi gia hạn thầu:</span>
            </div>
            <p className="text-[11px] leading-relaxed">
              Thao tác này sẽ chuyển trạng thái gói thầu từ <strong>Đã đóng (Closed)</strong> quay lại <strong>Đang mở (Open)</strong>, cho phép các nhà thầu tiếp tục gửi hồ sơ dự thầu đến thời hạn mới.
            </p>
          </div>

          <div className="space-y-1.5">
            <label className="font-bold text-slate-700 flex items-center space-x-1.5">
              <Calendar className="w-4 h-4 text-slate-500" />
              <span>Thời hạn nộp thầu mới (Bắt buộc) <span className="text-rose-500">*</span></span>
            </label>
            <input
              type="datetime-local"
              value={newDeadline}
              onChange={(e) => setNewDeadline(e.target.value)}
              className="w-full px-3 py-2 border border-slate-200 rounded-xl focus:outline-hidden focus:ring-2 focus:ring-amber-500/20 focus:border-amber-500 font-mono text-xs"
              required
            />
          </div>

          <div className="space-y-1.5">
            <label className="font-bold text-slate-700 flex items-center space-x-1.5">
              <FileText className="w-4 h-4 text-slate-500" />
              <span>Lý do / Căn cứ gia hạn (Tối thiểu 10 ký tự) <span className="text-rose-500">*</span></span>
            </label>
            <textarea
              rows={3}
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="Ví dụ: Gia hạn thêm thời gian để các nhà thầu hoàn thiện hồ sơ dự thầu và chứng thư số..."
              className="w-full px-3 py-2 border border-slate-200 rounded-xl focus:outline-hidden focus:ring-2 focus:ring-amber-500/20 focus:border-amber-500 text-xs"
              required
            />
          </div>

          {/* Footer */}
          <div className="pt-3 border-t border-slate-100 flex items-center justify-end space-x-2">
            <button
              type="button"
              onClick={onClose}
              disabled={submitting}
              className="px-4 py-2 text-xs font-semibold text-slate-600 hover:bg-slate-100 rounded-xl transition cursor-pointer"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={submitting}
              className="px-5 py-2 text-xs font-bold text-white bg-amber-500 hover:bg-amber-600 disabled:opacity-50 rounded-xl shadow-xs transition flex items-center space-x-1.5 cursor-pointer"
            >
              {submitting ? (
                <>
                  <LoadingSpinner size="sm" />
                  <span>Đang xử lý...</span>
                </>
              ) : (
                <>
                  <RefreshCw className="w-4 h-4" />
                  <span>Xác nhận mở lại gói thầu</span>
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ReopenPackageModal;
