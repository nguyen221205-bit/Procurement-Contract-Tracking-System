import React, { useState } from 'react';
import { X, XCircle, AlertTriangle } from 'lucide-react';
import toast from 'react-hot-toast';
import { evaluatorProposalApi } from '../../api/evaluatorProposalApi';

export const RejectProposalModal = ({ isOpen, onClose, proposal, onSuccess }) => {
  const [adminNotes, setAdminNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);

  if (!isOpen || !proposal) return null;

  const handleReject = async (e) => {
    e.preventDefault();

    if (!adminNotes.trim() || adminNotes.trim().length < 5) {
      toast.error('Vui lòng nhập lý do từ chối rõ ràng (tối thiểu 5 ký tự).');
      return;
    }

    try {
      setSubmitting(true);
      const res = await evaluatorProposalApi.rejectProposal(proposal.id, {
        adminNotes: adminNotes.trim(),
      });

      if (res && res.success) {
        toast.success(res.message || `Đã từ chối đề xuất chỉ định ${proposal.fullName}.`);
        onSuccess?.();
        onClose();
      } else {
        toast.error(res?.message || 'Từ chối không thành công.');
      }
    } catch (err) {
      toast.error(err.message || 'Lỗi khi từ chối đề xuất.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/50 backdrop-blur-xs flex items-center justify-center p-4 animate-in fade-in duration-150">
      <div
        className="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl border border-slate-100 relative space-y-5"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Nút đóng */}
        <button
          onClick={onClose}
          disabled={submitting}
          className="absolute top-4 right-4 text-slate-400 hover:text-slate-600 p-1 rounded-lg hover:bg-slate-100 transition"
        >
          <X className="w-5 h-5" />
        </button>

        {/* Header Modal */}
        <div className="flex items-start space-x-3.5 pr-8">
          <div className="w-12 h-12 rounded-xl bg-gradient-to-tr from-rose-600 to-red-600 text-white flex items-center justify-center flex-shrink-0 shadow-sm">
            <XCircle className="w-6 h-6" />
          </div>
          <div>
            <h3 className="text-base font-bold text-slate-900">
              Từ chối Đề xuất Giám khảo
            </h3>
            <p className="text-xs text-slate-500">
              Nhập lý do từ chối để thông báo lại cho Bên mời thầu
            </p>
          </div>
        </div>

        {/* Tóm tắt */}
        <div className="bg-rose-50/60 rounded-xl p-3 border border-rose-100 text-xs text-slate-700">
          Đề xuất chuyên gia: <strong className="text-slate-900">{proposal.fullName}</strong> ({proposal.email})
        </div>

        {/* Form từ chối */}
        <form onSubmit={handleReject} className="space-y-4 text-xs">
          <div className="space-y-1">
            <label className="font-semibold text-slate-700 block">
              Lý do từ chối <span className="text-rose-500">*</span>
            </label>
            <textarea
              rows={3}
              required
              value={adminNotes}
              onChange={(e) => setAdminNotes(e.target.value)}
              placeholder="VD: Chứng chỉ nghiệp vụ đấu thầu đính kèm đã hết hiệu lực hoặc chưa đáp ứng đủ 3 năm kinh nghiệm theo quy định..."
              className="w-full rounded-xl border border-slate-200 p-3 focus:outline-hidden focus:ring-2 focus:ring-rose-500 transition resize-none text-xs"
            />
          </div>

          <div className="pt-2 flex items-center justify-end space-x-2 border-t border-slate-100">
            <button
              type="button"
              onClick={onClose}
              disabled={submitting}
              className="px-4 py-2 font-semibold text-slate-600 hover:bg-slate-100 rounded-xl transition border border-slate-200"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={submitting}
              className="inline-flex items-center space-x-1.5 px-4 py-2 font-bold text-white bg-rose-600 hover:bg-rose-700 rounded-xl transition shadow-xs disabled:opacity-50"
            >
              <XCircle className="w-4 h-4" />
              <span>{submitting ? 'Đang xử lý...' : 'Xác nhận từ chối'}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default RejectProposalModal;
