import React, { useState } from 'react';
import { X, Ban, AlertTriangle, Building2, FileText } from 'lucide-react';
import toast from 'react-hot-toast';
import { evaluationApi } from '../../api/evaluationApi';
import LoadingSpinner from '../common/LoadingSpinner';

export const DisqualifySubmissionModal = ({
  isOpen,
  onClose,
  submission,
  onSuccess,
}) => {
  const [reason, setReason] = useState('');
  const [submitting, setSubmitting] = useState(false);

  if (!isOpen || !submission) return null;

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!reason.trim() || reason.trim().length < 5) {
      toast.error('Vui lòng nhập lý do loại bỏ hồ sơ dự thầu (tối thiểu 5 ký tự).');
      return;
    }

    try {
      setSubmitting(true);
      const subId = submission.id || submission.submissionId;
      await evaluationApi.disqualifySubmission(subId, reason.trim());
      toast.success(`Đã loại bỏ hồ sơ của "${submission.companyName || 'Nhà thầu'}" khỏi danh sách đánh giá!`);
      setReason('');
      onSuccess?.();
      onClose();
    } catch (err) {
      toast.error(err.message || 'Không thể loại bỏ hồ sơ dự thầu.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-xs">
      <div className="bg-white w-full max-w-lg rounded-2xl shadow-2xl border border-slate-200 overflow-hidden flex flex-col animate-in fade-in zoom-in-95 duration-150">
        {/* Header */}
        <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between bg-rose-50/50">
          <div className="flex items-center space-x-3">
            <div className="p-2 bg-rose-100 text-rose-600 rounded-xl">
              <Ban className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">
                Loại Bỏ Hồ Sơ Dự Thầu (Disqualify)
              </h3>
              <p className="text-xs text-rose-700">
                Xác nhận loại bỏ hồ sơ không đạt điều kiện tiên quyết theo Luật Đấu thầu
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

        {/* Body */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4 text-xs">
          {/* Thông tin nhà thầu bị loại */}
          <div className="p-3.5 bg-slate-50 rounded-xl border border-slate-200 space-y-1.5">
            <div className="flex items-center space-x-2 text-slate-800 font-bold">
              <Building2 className="w-4 h-4 text-slate-500" />
              <span>{submission.companyName || 'Doanh nghiệp dự thầu'}</span>
            </div>
            <div className="text-[11px] text-slate-500 font-mono flex items-center gap-3">
              <span>MST: <strong>{submission.taxCode || 'N/A'}</strong></span>
              <span>•</span>
              <span>Mã hồ sơ: <strong>#{submission.id || submission.submissionId}</strong></span>
            </div>
          </div>

          {/* Cảnh báo pháp lý */}
          <div className="p-3 bg-rose-50 border border-rose-200 rounded-xl text-rose-900 space-y-1">
            <div className="flex items-center space-x-1.5 font-bold">
              <AlertTriangle className="w-4 h-4 text-rose-600 flex-shrink-0" />
              <span>Hậu quả pháp lý khi loại hồ sơ:</span>
            </div>
            <p className="text-[11px] leading-relaxed">
              Hồ sơ này sẽ chuyển sang trạng thái <strong>Bị loại (Disqualified)</strong>. Toàn bộ điểm số chấm (nếu có) sẽ bị hủy bỏ, nhà thầu sẽ không được xếp hạng và không được quyền xét trúng thầu.
            </p>
          </div>

          {/* Ô nhập lý do */}
          <div className="space-y-1.5">
            <label className="font-bold text-slate-700 flex items-center space-x-1.5">
              <FileText className="w-4 h-4 text-slate-500" />
              <span>Căn cứ / Lý do loại bỏ hồ sơ <span className="text-rose-500">*</span></span>
            </label>
            <textarea
              rows={3}
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="Ví dụ: Không đáp ứng điều kiện tiên quyết về bảo lãnh dự thầu; hoặc thông tin kê khai năng lực không chính xác..."
              className="w-full px-3 py-2 border border-slate-200 rounded-xl focus:outline-hidden focus:ring-2 focus:ring-rose-500/20 focus:border-rose-500 text-xs"
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
              className="px-5 py-2 text-xs font-bold text-white bg-rose-600 hover:bg-rose-700 disabled:opacity-50 rounded-xl shadow-xs transition flex items-center space-x-1.5 cursor-pointer"
            >
              {submitting ? (
                <>
                  <LoadingSpinner size="sm" />
                  <span>Đang xử lý...</span>
                </>
              ) : (
                <>
                  <Ban className="w-4 h-4" />
                  <span>Xác nhận loại hồ sơ</span>
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default DisqualifySubmissionModal;
