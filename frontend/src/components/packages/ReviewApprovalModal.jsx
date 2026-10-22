import React, { useState } from 'react';
import { ShieldCheck, X, AlertTriangle, CheckCircle, Ban, Clock, User, FileText } from 'lucide-react';
import { formatDateTime } from '../../utils/formatters';
import toast from 'react-hot-toast';
import { bidPackageApi } from '../../api/bidPackageApi';

export const ReviewApprovalModal = ({ isOpen, onClose, request, isAdmin, onSuccess }) => {
  const [comment, setComment] = useState('');
  const [loading, setLoading] = useState(false);

  if (!isOpen || !request) return null;

  const handleReview = async (isApproved) => {
    if (!isApproved && !comment.trim()) {
      toast.error('Vui lòng nhập lý do từ chối yêu cầu.');
      return;
    }

    setLoading(true);
    try {
      const res = await bidPackageApi.reviewStatusApproval(request.id, {
        isApproved,
        adminComment: comment.trim() || null,
      });

      if (res && res.success) {
        toast.success(res.message || (isApproved ? 'Đã phê duyệt yêu cầu!' : 'Đã từ chối yêu cầu!'));
        onSuccess?.();
        onClose();
      } else {
        toast.error(res?.message || 'Có lỗi xảy ra khi xử lý yêu cầu.');
      }
    } catch (err) {
      toast.error(err?.response?.data?.message || 'Lỗi khi gửi đánh giá phê duyệt.');
    } finally {
      setLoading(false);
    }
  };

  const getStatusBadge = (status) => {
    switch (status) {
      case 'Pending':
      case 0:
        return (
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-amber-500/10 text-amber-400 border border-amber-500/30">
            <Clock className="w-3.5 h-3.5 animate-spin" /> Đang chờ Admin phê duyệt
          </span>
        );
      case 'Approved':
      case 1:
        return (
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-emerald-500/10 text-emerald-400 border border-emerald-500/30">
            <CheckCircle className="w-3.5 h-3.5" /> Đã phê duyệt
          </span>
        );
      case 'Rejected':
      case 2:
        return (
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-rose-500/10 text-rose-400 border border-rose-500/30">
            <Ban className="w-3.5 h-3.5" /> Đã từ chối
          </span>
        );
      default:
        return null;
    }
  };

  const isPending = request.status === 'Pending' || request.status === 0;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-sm animate-fade-in">
      <div className="relative w-full max-w-2xl bg-slate-900 border border-slate-800 rounded-2xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-slate-800 bg-slate-900/60">
          <div className="flex items-center gap-3">
            <div className="p-2.5 bg-indigo-500/10 border border-indigo-500/20 text-indigo-400 rounded-xl">
              <ShieldCheck className="w-6 h-6" />
            </div>
            <div>
              <h3 className="text-lg font-bold text-slate-100">
                Thẩm Quyền Phê Duyệt Trạng Thái Gói Thầu
              </h3>
              <p className="text-xs text-slate-400">
                Quy trình kiểm soát thẩm quyền phòng ngừa lạm quyền trong đấu thầu
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-2 text-slate-400 hover:text-slate-200 hover:bg-slate-800 rounded-lg transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Body */}
        <div className="p-6 space-y-6 overflow-y-auto">
          {/* Trạng thái hiện tại của yêu cầu */}
          <div className="flex items-center justify-between p-4 bg-slate-950/60 border border-slate-800/80 rounded-xl">
            <span className="text-xs font-medium text-slate-400 uppercase tracking-wider">Trạng thái hồ sơ:</span>
            {getStatusBadge(request.status)}
          </div>

          {/* Chi tiết yêu cầu */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
            <div className="p-3.5 bg-slate-800/40 border border-slate-800 rounded-xl">
              <p className="text-xs text-slate-400 flex items-center gap-1.5 mb-1">
                <User className="w-3.5 h-3.5 text-indigo-400" /> Bên mời thầu đề xuất:
              </p>
              <p className="font-semibold text-slate-200">{request.requestedByUserName || 'Bên mời thầu'}</p>
              <p className="text-xs text-slate-500 mt-1">Gửi lúc: {formatDateTime(request.createdAt)}</p>
            </div>

            <div className="p-3.5 bg-slate-800/40 border border-slate-800 rounded-xl">
              <p className="text-xs text-slate-400 flex items-center gap-1.5 mb-1">
                <AlertTriangle className="w-3.5 h-3.5 text-amber-400" /> Thao tác đề xuất:
              </p>
              <p className="font-semibold text-amber-300">
                Chuyển từ <span className="underline">{request.currentStatusName || request.currentStatus}</span> sang{' '}
                <span className="underline font-bold text-emerald-400">{request.targetStatusName || request.targetStatus}</span>
              </p>
              {request.newDeadline && (
                <p className="text-xs text-emerald-400 mt-1">
                  Hạn nộp thầu mới: {formatDateTime(request.newDeadline)}
                </p>
              )}
            </div>
          </div>

          {/* Lý do / Căn cứ pháp lý */}
          <div className="p-4 bg-slate-800/30 border border-slate-800/80 rounded-xl space-y-2">
            <p className="text-xs font-semibold text-slate-400 uppercase tracking-wider flex items-center gap-1.5">
              <FileText className="w-4 h-4 text-blue-400" /> Căn cứ pháp lý / Lý do đề xuất:
            </p>
            <p className="text-sm text-slate-200 leading-relaxed whitespace-pre-line bg-slate-950/40 p-3 rounded-lg border border-slate-800/50">
              {request.reason || 'Không có ghi chú.'}
            </p>
          </div>

          {/* Nhận xét của Admin nếu đã xử lý */}
          {!isPending && (
            <div className="p-4 bg-slate-950/60 border border-slate-800 rounded-xl space-y-2">
              <p className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
                Ý kiến Chủ đầu tư / Admin ({request.reviewedByUserName || 'Admin'} - {formatDateTime(request.reviewedAt)}):
              </p>
              <p className="text-sm text-slate-300 italic">
                "{request.adminComment || 'Không có nhận xét thêm'}"
              </p>
            </div>
          )}

          {/* Ô nhập nhận xét của Admin khi đang duyệt */}
          {isPending && isAdmin && (
            <div className="space-y-2 pt-2 border-t border-slate-800">
              <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider">
                Ý kiến thẩm định của Chủ đầu tư / Admin:
              </label>
              <textarea
                value={comment}
                onChange={(e) => setComment(e.target.value)}
                placeholder="Nhập ý kiến thẩm định, căn cứ phê duyệt hoặc lý do từ chối..."
                rows={3}
                className="w-full px-3.5 py-2.5 bg-slate-950 border border-slate-700 rounded-xl text-sm text-slate-100 placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/50 transition-all resize-none"
              />
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="p-6 border-t border-slate-800 bg-slate-900/60 flex items-center justify-end gap-3">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2.5 rounded-xl text-sm font-medium text-slate-300 hover:bg-slate-800 transition-colors"
          >
            Đóng
          </button>

          {isPending && isAdmin && (
            <>
              <button
                type="button"
                disabled={loading}
                onClick={() => handleReview(false)}
                className="px-4 py-2.5 rounded-xl text-sm font-semibold bg-rose-600/20 text-rose-300 border border-rose-500/30 hover:bg-rose-600/30 hover:border-rose-500 transition-all disabled:opacity-50 flex items-center gap-2"
              >
                <Ban className="w-4 h-4" /> Từ chối
              </button>

              <button
                type="button"
                disabled={loading}
                onClick={() => handleReview(true)}
                className="px-5 py-2.5 rounded-xl text-sm font-semibold bg-emerald-600 text-white shadow-lg shadow-emerald-600/20 hover:bg-emerald-500 transition-all disabled:opacity-50 flex items-center gap-2"
              >
                <CheckCircle className="w-4 h-4" /> Chấp thuận phê duyệt
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  );
};

export default ReviewApprovalModal;
