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
  Send,
  ShieldAlert,
  ClipboardCheck,
  UserCheck,
  Download,
  Loader2
} from 'lucide-react';
import { contractApi } from '../../api/contractApi';
import { formatCurrency, formatDate } from '../../utils/formatters';
import { useAuth } from '../../context/AuthContext';

export const ApproveMilestoneModal = ({
  isOpen,
  onClose,
  milestone,
  onSuccess,
}) => {
  const { user: currentUser } = useAuth();
  const [isApproved, setIsApproved] = useState(true);
  const [note, setNote] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [downloadingSnapshot, setDownloadingSnapshot] = useState(false);
  const [downloadError, setDownloadError] = useState(null);

  const handleDownloadSnapshot = async (e) => {
    e.preventDefault();
    if (downloadingSnapshot || !milestone?.id || !milestone?.acceptanceId) return;
    setDownloadingSnapshot(true);
    setDownloadError(null);
    try {
      const blob = await contractApi.downloadAcceptanceSnapshotDocument(
        milestone.id,
        milestone.acceptanceId
      );
      const fileBlob = new Blob([blob], { type: 'application/pdf' });
      const blobUrl = window.URL.createObjectURL(fileBlob);
      const link = document.createElement('a');
      link.href = blobUrl;
      link.download = `NghiemThu_Moc${milestone.id}_Rev${milestone.revision || 1}.pdf`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      setTimeout(() => {
        window.URL.revokeObjectURL(blobUrl);
      }, 1000);
    } catch (err) {
      const errMsg = err.message || 'Không thể tải tài liệu snapshot.';
      setDownloadError(errMsg);
      toast.error(errMsg);
    } finally {
      setDownloadingSnapshot(false);
    }
  };

  if (!isOpen || !milestone) return null;

  // Kiểm tra điều kiện Maker - Checker, Thẩm quyền & Phiên bản mốc
  const isReviewInvalidated = milestone.acceptanceStatus === 4 || milestone.acceptanceStatusName === 'ReviewInvalidated';
  const hasTechnicalReview = Boolean(milestone.technicalReviewedBy);
  const isTechnicalPassed = milestone.isTechnicalPassed === true && !isReviewInvalidated;
  const isMakerSelfApproving = currentUser && milestone.technicalReviewedBy && currentUser.id === milestone.technicalReviewedBy;
  const isVersionMismatch = milestone.milestoneVersion && milestone.version && milestone.milestoneVersion !== milestone.version;
  const canDisburse = hasTechnicalReview && isTechnicalPassed && !isMakerSelfApproving && !isReviewInvalidated && !isVersionMismatch;

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (isApproved && !canDisburse) {
      if (!hasTechnicalReview || !isTechnicalPassed) {
        toast.error('Mốc thanh toán chưa được Cán bộ kỹ thuật kiểm tra và xác nhận ĐẠT tại hiện trường.');
        return;
      }
      if (isMakerSelfApproving) {
        toast.error('Vi phạm nguyên tắc kiểm soát chéo (Maker - Checker): Bạn là Cán bộ đã ký xác nhận kỹ thuật, không thể tự phê duyệt giải ngân.');
        return;
      }
    }

    if (!isApproved && !note.trim()) {
      toast.error('Vui lòng nêu rõ lý do hoặc nội dung yêu cầu khắc phục khi từ chối nghiệm thu.');
      return;
    }

    try {
      setSubmitting(true);
      // F02: Kiểm tra bắt buộc AcceptanceId và ExpectedRevision trước khi gửi
      if (!milestone.acceptanceId || !milestone.acceptanceRevision) {
        toast.error('Thiếu thông tin định danh biên bản nghiệm thu kỹ thuật (AcceptanceId/Revision). Vui lòng tải lại trang.');
        setSubmitting(false);
        return;
      }
      const expectedVersion = milestone.milestoneVersion ?? milestone.version;
      if (!expectedVersion) {
        toast.error('Thiếu thông tin phiên bản mốc thanh toán (ExpectedVersion). Vui lòng tải lại trang.');
        setSubmitting(false);
        return;
      }
      const res = await contractApi.approveMilestoneAcceptance(
        milestone.id,
        {
          isApproved,
          note: note.trim(),
          expectedVersion,
          acceptanceId: milestone.acceptanceId,
          expectedRevision: milestone.acceptanceRevision,
        }
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
      if (err.response?.status === 409 || err.statusCode === 409) {
        toast.error(err.response?.data?.message || 'Xung đột phiên bản: Dữ liệu đã bị thay đổi bởi người khác. Vui lòng đóng cửa sổ và tải lại trang.');
      } else {
        toast.error(err.response?.data?.message || err.message || 'Không thể phê duyệt nghiệm thu mốc thanh toán.');
      }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
      <div className="bg-white rounded-3xl shadow-xl border border-slate-200 w-full max-w-lg overflow-hidden animate-in fade-in zoom-in duration-200">
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
              <div className="flex items-center space-x-2">
                <h3 className="text-base font-bold">Biên Bản Nghiệm Thu & Giải Ngân</h3>
                <span className="px-2 py-0.5 rounded-full text-[10px] font-black bg-white/20 text-white">
                  Checker
                </span>
              </div>
              <p className="text-xs text-white/80">
                Lãnh đạo thẩm tra hồ sơ kỹ thuật & ký quyết định thanh toán
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
          <div className="p-4 bg-slate-50 border border-slate-200 rounded-2xl space-y-2 text-xs">
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

          {/* CẢNH BÁO NẾU BIÊN BẢN KỸ THUẬT ĐÃ BỊ VÔ HIỆU HÓA DO SỬA MỐC */}
          {isReviewInvalidated && (
            <div className="p-3.5 bg-rose-50 border border-rose-300 rounded-2xl text-xs text-rose-900 flex items-start space-x-2.5 animate-pulse">
              <ShieldAlert className="w-5 h-5 text-rose-600 flex-shrink-0 mt-0.5" />
              <div className="space-y-1">
                <div className="font-bold text-rose-800">
                  Biên bản kỹ thuật đã bị HỆ THỐNG VÔ HIỆU HÓA
                </div>
                <p className="text-[11px] leading-relaxed text-rose-700">
                  Nội dung mốc thanh toán đã bị chỉnh sửa (Số tiền/Tiêu đề/Hạn nộp) sau khi Cán bộ kỹ thuật kiểm tra.
                  Theo quy định liêm chính, Cán bộ kỹ thuật hiện trường (Maker) bắt buộc phải kiểm tra và xác nhận lại
                  trên phiên bản mốc hiện tại (v{milestone.version}) trước khi có thể phê duyệt giải ngân.
                </p>
              </div>
            </div>
          )}

          {/* DỮ LIỆU SNAPSHOT TẠI THỜI ĐIỂM MAKER DUYỆT (F02 & F04) */}
          {milestone.snapshotAmount != null && (
            <div className="p-3 bg-amber-50/80 border border-amber-200 rounded-2xl text-xs space-y-1.5">
              <div className="flex items-center justify-between font-bold text-amber-900">
                <span>Dữ liệu nghiệm thu kỹ thuật (Snapshot v{milestone.milestoneVersion} - Đợt r{milestone.acceptanceRevision || 1})</span>
                {isVersionMismatch && (
                  <span className="px-2 py-0.5 rounded-full text-[10px] font-black bg-rose-100 text-rose-800">
                    Lệch phiên bản (Hiện tại: v{milestone.version})
                  </span>
                )}
              </div>
              <div className="grid grid-cols-2 gap-2 text-[11px] text-amber-950 bg-white/70 p-2 rounded-xl">
                <div>
                  <span className="text-amber-800/80 block">Số tiền snapshot:</span>
                  <strong className="font-bold">{formatCurrency(milestone.snapshotAmount)}</strong>
                </div>
                <div>
                  <span className="text-amber-800/80 block">Hạn nộp snapshot:</span>
                  <span>{milestone.snapshotDueDate ? formatDate(milestone.snapshotDueDate) : 'N/A'}</span>
                </div>
              </div>
              {milestone.snapshotDocumentHash && (
                <div className="pt-1.5 border-t border-amber-200/60 text-[10px] text-amber-900/80 space-y-1">
                  <div className="flex items-center justify-between">
                    <span className="font-semibold">Mã băm SHA256 tài liệu:</span>
                    {milestone.snapshotDocumentPath && milestone.acceptanceId && (
                      <div className="flex flex-col items-end">
                        <button
                          type="button"
                          disabled={downloadingSnapshot}
                          onClick={handleDownloadSnapshot}
                          className="text-blue-600 hover:text-blue-800 underline font-semibold flex items-center gap-1 disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                          {downloadingSnapshot ? (
                            <>
                              <Loader2 className="w-3 h-3 animate-spin" />
                              <span>Đang tải...</span>
                            </>
                          ) : (
                            <>
                              <Download className="w-3 h-3" />
                              <span>Tải bản scan snapshot</span>
                            </>
                          )}
                        </button>
                        {downloadError && (
                          <span className="text-[10px] text-red-600 font-medium mt-0.5">{downloadError}</span>
                        )}
                      </div>
                    )}
                  </div>
                  <div className="font-mono break-all bg-white/60 p-1 rounded text-[9px] select-all">
                    {milestone.snapshotDocumentHash}
                  </div>
                </div>
              )}
            </div>
          )}

          {/* HỒ SƠ KIỂM TRA HIỆN TRƯỜNG (MAKER AUDIT TRAIL) */}
          <div className="p-3.5 bg-blue-50/60 border border-blue-200 rounded-2xl text-xs space-y-2">
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-1.5 font-bold text-blue-900">
                <ClipboardCheck className="w-4 h-4 text-blue-600" />
                <span>Hồ sơ Kiểm tra Kỹ thuật Hiện trường (Maker)</span>
              </div>
              {hasTechnicalReview ? (
                <span
                  className={`px-2 py-0.5 rounded-full text-[10px] font-black ${
                    isTechnicalPassed
                      ? 'bg-emerald-100 text-emerald-800'
                      : 'bg-rose-100 text-rose-800'
                  }`}
                >
                  {isTechnicalPassed ? 'ĐẠT KỸ THUẬT' : 'KHÔNG ĐẠT'}
                </span>
              ) : (
                <span className="px-2 py-0.5 rounded-full text-[10px] font-black bg-amber-100 text-amber-800">
                  CHƯA KIỂM TRA
                </span>
              )}
            </div>

            {hasTechnicalReview ? (
              <div className="text-[11px] text-slate-600 space-y-1 bg-white p-2.5 rounded-xl border border-blue-100">
                <div>
                  • Cán bộ kỹ thuật:{' '}
                  <strong className="text-slate-800">
                    {milestone.technicalReviewerName || `ID #${milestone.technicalReviewedBy}`}
                  </strong>{' '}
                  ({formatDate(milestone.technicalReviewedAt)})
                </div>
                <div>
                  • Nội dung kiểm tra:{' '}
                  <span className="italic text-slate-700">"{milestone.technicalNote || 'Không có ghi chú'}"</span>
                </div>
              </div>
            ) : (
              <p className="text-[11px] text-amber-800">
                Mốc này chưa được Cán bộ kỹ thuật kiểm tra hiện trường. Cần có biên bản kỹ thuật trước khi ký duyệt giải ngân.
              </p>
            )}
          </div>

          {/* CẢNH BÁO VI PHẠM MAKER - CHECKER NẾU TỰ DUYỆT */}
          {isMakerSelfApproving && (
            <div className="p-3.5 bg-rose-50 border border-rose-300 rounded-2xl text-xs text-rose-900 flex items-start space-x-2.5">
              <ShieldAlert className="w-5 h-5 text-rose-600 flex-shrink-0 mt-0.5" />
              <div className="space-y-1">
                <div className="font-bold text-rose-800">
                  Vi phạm nguyên tắc kiểm soát chéo (Maker - Checker)
                </div>
                <p className="text-[11px] leading-relaxed text-rose-700">
                  Bạn là người đã xác nhận kỹ thuật hiện trường cho mốc này. Quy định kiểm soát nội bộ nghiêm cấm
                  người kiểm tra kỹ thuật tự ký phê duyệt giải ngân. Thao tác này phải do Lãnh đạo hoặc Người có thẩm quyền khác thực hiện.
                </p>
              </div>
            </div>
          )}

          {/* Chọn quyết định nghiệm thu */}
          <div className="space-y-2">
            <label className="text-xs font-bold text-slate-700">
              Quyết định của Lãnh đạo phê duyệt <span className="text-rose-500">*</span>
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
                <span>Chấp thuận & Giải ngân</span>
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
                <span>Từ chối giải ngân</span>
              </button>
            </div>
          </div>

          {/* Ý kiến thẩm định / Lý do từ chối */}
          <div className="space-y-1">
            <label className="text-xs font-bold text-slate-700 flex justify-between">
              <span>
                {isApproved
                  ? 'Ý kiến phê duyệt thanh toán (Tùy chọn)'
                  : 'Lý do từ chối giải ngân & Yêu cầu khắc phục (Bắt buộc)'}
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
                  ? 'Căn cứ biên bản kiểm tra hiện trường đạt yêu cầu, chấp thuận thanh toán mốc...'
                  : 'Hồ sơ chưa đạt, yêu cầu rà soát lại khối lượng công việc hiện trường...'
              }
              className="w-full text-xs text-slate-800 bg-white border border-slate-300 rounded-xl p-3 focus:ring-2 focus:ring-blue-500 focus:outline-hidden leading-relaxed"
            />
          </div>

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
              disabled={submitting || (isApproved && !canDisburse)}
              className={`inline-flex items-center gap-1.5 px-6 py-2.5 text-xs font-bold text-white rounded-xl shadow-xs transition-transform hover:scale-[1.02] cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed ${
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
                  <span>{isApproved ? 'Ký duyệt Nghiệm thu & Giải ngân' : 'Gửi Từ chối'}</span>
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
