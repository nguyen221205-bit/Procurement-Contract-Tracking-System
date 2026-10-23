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
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-amber-50 text-amber-800 border border-amber-200">
            <Clock className="w-3.5 h-3.5 text-amber-600 animate-spin" /> Đang chờ Admin phê duyệt
          </span>
        );
      case 'Approved':
      case 1:
        return (
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-800 border border-emerald-200">
            <CheckCircle className="w-3.5 h-3.5 text-emerald-600" /> Đã phê duyệt
          </span>
        );
      case 'Rejected':
      case 2:
        return (
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-rose-50 text-rose-800 border border-rose-200">
            <Ban className="w-3.5 h-3.5 text-rose-600" /> Đã từ chối
          </span>
        );
      default:
        return null;
    }
  };

  const isPending = request.status === 'Pending' || request.status === 0;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-xs animate-fade-in">
      <div className="relative w-full max-w-2xl bg-white border border-slate-200 rounded-2xl shadow-xl overflow-hidden flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="flex items-center justify-between p-5 sm:p-6 border-b border-slate-100 bg-slate-50/70">
          <div className="flex items-center gap-3">
            <div className="p-2.5 bg-blue-50 border border-blue-200 text-blue-600 rounded-xl shadow-2xs">
              <ShieldCheck className="w-5 h-5 sm:w-6 sm:h-6" />
            </div>
            <div>
              <h3 className="text-base sm:text-lg font-bold text-slate-900">
                Thẩm Quyền Phê Duyệt Trạng Thái Gói Thầu
              </h3>
              <p className="text-xs text-slate-500 mt-0.5">
                Quy trình kiểm soát thẩm quyền phòng ngừa lạm quyền trong đấu thầu
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-2 text-slate-400 hover:text-slate-600 hover:bg-slate-100 rounded-xl transition-colors cursor-pointer"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Body */}
        <div className="p-5 sm:p-6 space-y-5 overflow-y-auto bg-white">
          {/* Trạng thái hiện tại của yêu cầu */}
          <div className="flex items-center justify-between p-4 bg-slate-50/80 border border-slate-200/80 rounded-xl">
            <span className="text-xs font-bold text-slate-500 uppercase tracking-wider">Trạng thái hồ sơ:</span>
            {getStatusBadge(request.status)}
          </div>

          {/* Chi tiết yêu cầu */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
            <div className="p-4 bg-slate-50/80 border border-slate-200/80 rounded-xl">
              <p className="text-xs font-semibold text-slate-500 flex items-center gap-1.5 mb-1.5">
                <User className="w-3.5 h-3.5 text-blue-600" /> Bên mời thầu đề xuất:
              </p>
              <p className="font-bold text-slate-900 text-sm">{request.requestedByUserName || 'Bên mời thầu'}</p>
              <p className="text-xs text-slate-500 mt-1">Gửi lúc: {formatDateTime(request.createdAt)}</p>
            </div>

            <div className="p-4 bg-slate-50/80 border border-slate-200/80 rounded-xl">
              <p className="text-xs font-semibold text-slate-500 flex items-center gap-1.5 mb-1.5">
                <AlertTriangle className="w-3.5 h-3.5 text-amber-600" /> Thao tác đề xuất:
              </p>
              <p className="text-xs font-semibold text-slate-700 leading-normal">
                Chuyển từ <span className="font-bold text-slate-800 px-1.5 py-0.5 bg-slate-200/80 rounded">{request.currentStatusName || request.currentStatus}</span> sang{' '}
                <span className="font-bold text-emerald-700 px-1.5 py-0.5 bg-emerald-50 border border-emerald-200 rounded">{request.targetStatusName || request.targetStatus}</span>
              </p>
              {request.newDeadline && (
                <p className="text-xs font-semibold text-emerald-700 mt-1.5">
                  Hạn nộp thầu mới: {formatDateTime(request.newDeadline)}
                </p>
              )}
            </div>
          </div>

          {/* Lý do / Căn cứ pháp lý */}
          <div className="p-4 bg-slate-50/70 border border-slate-200/80 rounded-xl space-y-2">
            <p className="text-xs font-bold text-slate-700 uppercase tracking-wider flex items-center gap-1.5">
              <FileText className="w-4 h-4 text-blue-600" /> Căn cứ pháp lý / Lý do đề xuất:
            </p>
            <p className="text-xs text-slate-800 leading-relaxed whitespace-pre-line bg-white p-3.5 rounded-xl border border-slate-200 shadow-2xs font-medium">
              {request.reason || 'Không có ghi chú.'}
            </p>
          </div>

          {/* Nhận xét của Admin nếu đã xử lý */}
          {!isPending && (
            <div className="p-4 bg-slate-50 border border-slate-200 rounded-xl space-y-2">
              <p className="text-xs font-bold text-slate-700 uppercase tracking-wider">
                Ý kiến Chủ đầu tư / Admin ({request.reviewedByUserName || 'Admin'} - {formatDateTime(request.reviewedAt)}):
              </p>
              <p className="text-xs text-slate-700 italic bg-white p-3 rounded-xl border border-slate-200">
                "{request.adminComment || 'Không có nhận xét thêm'}"
              </p>
            </div>
          )}

          {/* Ô nhập nhận xét của Admin khi đang duyệt */}
          {isPending && isAdmin && (
            <div className="space-y-2 pt-3 border-t border-slate-100">
              <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider">
                Ý kiến thẩm định của Chủ đầu tư / Admin:
              </label>
              <textarea
                value={comment}
                onChange={(e) => setComment(e.target.value)}
                placeholder="Nhập ý kiến thẩm định, căn cứ phê duyệt hoặc lý do từ chối..."
                rows={3}
                className="w-full px-3.5 py-2.5 bg-white border border-slate-300 rounded-xl text-xs text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all resize-none shadow-2xs"
              />
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="p-4 sm:p-5 border-t border-slate-100 bg-slate-50/70 flex items-center justify-end gap-3">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 rounded-xl text-xs font-bold text-slate-700 bg-white border border-slate-300 hover:bg-slate-100 transition shadow-2xs cursor-pointer"
          >
            Đóng
          </button>

          {isPending && isAdmin && (
            <>
              <button
                type="button"
                disabled={loading}
                onClick={() => handleReview(false)}
                className="px-4 py-2 rounded-xl text-xs font-bold bg-rose-50 text-rose-700 border border-rose-200 hover:bg-rose-100 transition disabled:opacity-50 flex items-center gap-1.5 cursor-pointer shadow-2xs"
              >
                <Ban className="w-3.5 h-3.5" />
                <span>Từ chối</span>
              </button>

              <button
                type="button"
                disabled={loading}
                onClick={() => handleReview(true)}
                className="px-4 py-2 rounded-xl text-xs font-bold bg-emerald-600 text-white shadow-xs hover:bg-emerald-700 transition disabled:opacity-50 flex items-center gap-1.5 cursor-pointer"
              >
                <CheckCircle className="w-3.5 h-3.5" />
                <span>Chấp thuận phê duyệt</span>
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  );
};

export default ReviewApprovalModal;
