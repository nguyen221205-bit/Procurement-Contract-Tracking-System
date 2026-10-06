import React, { useState } from 'react';
import { Award, AlertTriangle, X, CheckCircle, ShieldCheck, Scale, FileText } from 'lucide-react';
import { formatCurrency, formatNumber } from '../../utils/formatters';

export const FinalizeWinnerModal = ({
  isOpen,
  onClose,
  packageData,
  winningSubmission,
  onConfirm,
  loading,
  hasTie = false,
}) => {
  const [decisionReason, setDecisionReason] = useState('');

  if (!isOpen || !winningSubmission) return null;

  const isNotRank1 = winningSubmission.rank && winningSubmission.rank > 1;

  const handleConfirm = () => {
    onConfirm(winningSubmission.submissionId, decisionReason);
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-xl border border-slate-200 w-full max-w-lg overflow-hidden animate-in fade-in zoom-in duration-200">
        {/* Header */}
        <div className="bg-gradient-to-r from-amber-500 to-amber-600 px-6 py-5 text-white flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <div className="p-2.5 bg-white/20 rounded-xl backdrop-blur-xs">
              <Award className="w-6 h-6 text-white" />
            </div>
            <div>
              <h3 className="text-lg font-bold">Phê duyệt Trao thầu</h3>
              <p className="text-xs text-amber-100">Xác nhận kết quả trúng thầu chính thức</p>
            </div>
          </div>
          <button
            onClick={onClose}
            disabled={loading}
            className="text-white/80 hover:text-white p-1 rounded-lg hover:bg-white/10 transition-colors cursor-pointer"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content */}
        <div className="p-6 space-y-4 max-h-[75vh] overflow-y-auto">
          {/* Cảnh báo đặc biệt nếu chọn khác Rank 1 hoặc có hòa điểm */}
          {hasTie && (
            <div className="bg-indigo-50 border border-indigo-200 rounded-xl p-3 text-xs text-indigo-900 flex items-start gap-2.5">
              <Scale className="w-4 h-4 text-indigo-600 flex-shrink-0 mt-0.5" />
              <div className="leading-relaxed">
                <strong>Tình huống hòa điểm:</strong> Có từ 2 nhà thầu đạt cùng điểm số cao nhất ({formatNumber(winningSubmission.totalScore)} điểm). Quyết định trao thầu này căn cứ trên thẩm quyền đánh giá các yếu tố bổ trợ của Chủ đầu tư / Bên mời thầu.
              </div>
            </div>
          )}

          {isNotRank1 && !hasTie && (
            <div className="bg-amber-50 border border-amber-300 rounded-xl p-3 text-xs text-amber-900 flex items-start gap-2.5">
              <AlertTriangle className="w-4 h-4 text-amber-600 flex-shrink-0 mt-0.5" />
              <div className="leading-relaxed">
                <strong>Lưu ý:</strong> Bạn đang chọn trao thầu cho đơn vị xếp <strong>Hạng #{winningSubmission.rank}</strong> thay vì đơn vị xếp Hạng 1. Vui lòng ghi rõ căn cứ lựa chọn bên dưới để lưu hồ sơ kiểm toán.
              </div>
            </div>
          )}

          {/* Winner Card */}
          <div className="bg-amber-50/70 border border-amber-200 rounded-xl p-4 space-y-2">
            <div className="flex items-center justify-between">
              <span className="text-xs font-semibold uppercase tracking-wider text-amber-800 flex items-center gap-1.5">
                <ShieldCheck className="w-4 h-4 text-amber-600" />
                Đơn vị được phê duyệt trao thầu
              </span>
              <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-bold bg-amber-200 text-amber-900">
                Thứ hạng #{winningSubmission.rank || 1}
              </span>
            </div>
            <p className="text-base font-bold text-slate-800">{winningSubmission.companyName}</p>
            <div className="grid grid-cols-2 gap-2 text-xs text-slate-600 pt-1 border-t border-amber-200/60">
              <div>
                <span className="text-slate-400">Mã số thuế:</span>{' '}
                <span className="font-medium text-slate-700">{winningSubmission.taxCode || 'Chưa cập nhật'}</span>
              </div>
              <div>
                <span className="text-slate-400">Điểm đánh giá:</span>{' '}
                <span className="font-bold text-amber-700 text-sm">
                  {formatNumber(winningSubmission.totalScore)} / 100
                </span>
              </div>
            </div>
          </div>

          {/* Ô nhập căn cứ / lý do phê duyệt lựa chọn */}
          <div className="space-y-1.5">
            <label className="text-xs font-bold text-slate-700 flex items-center justify-between">
              <span className="flex items-center gap-1.5">
                <FileText className="w-3.5 h-3.5 text-slate-500" />
                <span>Căn cứ / Lý do lựa chọn trao thầu:</span>
              </span>
              {isNotRank1 ? (
                <span className="text-[11px] text-rose-600 font-bold bg-rose-50 px-2 py-0.5 rounded-md border border-rose-200">
                  * Bắt buộc (Điều 61 Luật Đấu thầu)
                </span>
              ) : (
                <span className="text-[10px] text-slate-400 font-normal">(Tùy chọn ghi chú)</span>
              )}
            </label>
            <textarea
              rows={3}
              value={decisionReason}
              onChange={(e) => setDecisionReason(e.target.value)}
              placeholder={
                hasTie
                  ? 'Ví dụ: Hai bên bằng điểm 89.75, ưu tiên nhà thầu có tiến độ thực hiện ngắn hơn và cam kết bảo hành vượt trội theo điều 4 HSMT...'
                  : isNotRank1
                  ? 'Ví dụ: Nhà thầu xếp hạng 1 từ chối ký hợp đồng / Không đạt thỏa thuận thương thảo, chuyển quyền trao thầu cho đơn vị xếp kế tiếp...'
                  : 'Ghi chú phê duyệt kết quả lựa chọn nhà thầu (tùy chọn)...'
              }
              className={`w-full p-2.5 text-xs text-slate-800 bg-slate-50 border rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-amber-500 placeholder:text-slate-400 ${
                isNotRank1 && !decisionReason.trim()
                  ? 'border-amber-400 bg-amber-50/30'
                  : 'border-slate-300'
              }`}
            />
            {isNotRank1 && !decisionReason.trim() && (
              <p className="text-[11px] text-amber-700 font-medium">
                Vui lòng nhập lý do giải trình để kích hoạt nút Xác nhận trao thầu.
              </p>
            )}
          </div>

          {/* Package Info */}
          <div className="bg-slate-50 rounded-xl p-3 border border-slate-200 text-xs space-y-1 text-slate-600">
            <div className="flex justify-between">
              <span className="text-slate-400">Gói thầu:</span>
              <span className="font-semibold text-slate-800">{packageData?.code}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-400">Ngân sách dự toán:</span>
              <span className="font-bold text-emerald-700">
                {formatCurrency(packageData?.budget || 0)}
              </span>
            </div>
          </div>

          {/* Warning */}
          <div className="flex items-start space-x-2.5 p-3 bg-rose-50 border border-rose-200 rounded-xl text-rose-800 text-[11px] leading-relaxed">
            <AlertTriangle className="w-4 h-4 flex-shrink-0 text-rose-500 mt-0.5" />
            <div>
              <strong>Lưu ý:</strong> Sau khi phê duyệt, gói thầu sẽ chuyển sang <strong>Đã trao thầu (Awarded)</strong>, các hồ sơ khác chuyển thành <strong>Không trúng thầu (Rejected)</strong> và dữ liệu sẽ được bàn giao sang phân hệ <strong>Hợp đồng (Contracts)</strong>.
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="px-6 py-4 bg-slate-50 border-t border-slate-200 flex items-center justify-end space-x-3">
          <button
            type="button"
            onClick={onClose}
            disabled={loading}
            className="px-4 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-200 rounded-xl transition-colors cursor-pointer disabled:opacity-50"
          >
            Hủy bỏ
          </button>
          <button
            type="button"
            onClick={handleConfirm}
            disabled={loading || (isNotRank1 && !decisionReason.trim())}
            className={`inline-flex items-center space-x-2 px-5 py-2 text-xs font-bold text-white rounded-xl shadow-xs transition-colors cursor-pointer disabled:opacity-50 ${
              isNotRank1 && !decisionReason.trim()
                ? 'bg-slate-400 cursor-not-allowed'
                : 'bg-amber-600 hover:bg-amber-700'
            }`}
          >
            {loading ? (
              <>
                <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                <span>Đang xử lý phê duyệt...</span>
              </>
            ) : (
              <>
                <CheckCircle className="w-4 h-4" />
                <span>Xác nhận Trao thầu</span>
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
};

export default FinalizeWinnerModal;
