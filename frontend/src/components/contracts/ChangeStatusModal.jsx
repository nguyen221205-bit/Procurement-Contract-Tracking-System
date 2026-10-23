import React, { useState, useEffect } from 'react';
import toast from 'react-hot-toast';
import {
  AlertTriangle,
  CheckCircle,
  XCircle,
  X,
  PlayCircle,
  Archive,
  Ban,
  ShieldAlert,
  FileCheck2,
  FileText,
  Lock
} from 'lucide-react';
import { contractApi } from '../../api/contractApi';

export const ChangeStatusModal = ({
  isOpen,
  onClose,
  contractId,
  contractNumber,
  targetStatus, // 'Active' | 'Completed' | 'Terminated'
  currentStatus, // trạng thái hiện tại của hợp đồng
  onSuccess,
}) => {
  const [note, setNote] = useState('');
  const [terminationCategory, setTerminationCategory] = useState('ContractorBreach');
  const [decisionNumber, setDecisionNumber] = useState('');
  const [confirmInput, setConfirmInput] = useState('');
  const [confirmAgreement, setConfirmAgreement] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setNote('');
      setTerminationCategory('ContractorBreach');
      setDecisionNumber('');
      setConfirmInput('');
      setConfirmAgreement(false);
    }
  }, [isOpen]);

  if (!isOpen || !targetStatus) return null;

  const isActivating = targetStatus === 'Active' || targetStatus === 1;
  const isCompleting = targetStatus === 'Completed' || targetStatus === 2;
  const isTerminating = targetStatus === 'Terminated' || targetStatus === 3;
  const isReactivating = isActivating && (currentStatus === 'Terminated' || currentStatus === 3);

  const getModalConfig = () => {
    if (isReactivating) {
      return {
        title: 'Khôi Phục Hiệu Lực Hợp Đồng (Reactivate)',
        subtitle: 'Khôi phục hợp đồng đã chấm dứt sau giải quyết khiếu nại hoặc hủy quyết định',
        headerBg: 'bg-gradient-to-r from-amber-600 to-amber-700',
        icon: PlayCircle,
        confirmBtnText: 'Xác nhận Khôi phục HĐ',
        confirmBtnBg: 'bg-amber-600 hover:bg-amber-700',
        warningMsg:
          'CHÚ Ý: Thao tác này khôi phục hợp đồng về trạng thái Active và tự động tính toán lại điểm tín nhiệm cho nhà thầu. Yêu cầu nhập lý do giải trình chi tiết (tối thiểu 20 ký tự).',
      };
    }
    if (isActivating) {
      return {
        title: 'Kích Hoạt Hợp Đồng (Active)',
        subtitle: 'Xác nhận hợp đồng chính thức có hiệu lực thi hành',
        headerBg: 'bg-gradient-to-r from-emerald-600 to-teal-700',
        icon: PlayCircle,
        confirmBtnText: 'Xác nhận Kích hoạt HĐ',
        confirmBtnBg: 'bg-emerald-600 hover:bg-emerald-700',
        warningMsg:
          'Khi kích hoạt, hợp đồng sẽ có hiệu lực chính thức. Hệ thống sẽ tự động chuyển trạng thái Gói thầu liên kết sang "Đã ký hợp đồng" (Contracted) để khép lại chu trình đấu thầu.',
      };
    }
    if (isCompleting) {
      return {
        title: 'Nghiệm Thu Hoàn Thành Hợp Đồng',
        subtitle: 'Đóng hợp đồng sau khi hoàn thành 100% nghĩa vụ',
        headerBg: 'bg-gradient-to-r from-blue-600 to-indigo-700',
        icon: CheckCircle,
        confirmBtnText: 'Xác nhận Hoàn tất Hợp đồng',
        confirmBtnBg: 'bg-blue-600 hover:bg-blue-700',
        warningMsg:
          'Xác nhận toàn bộ công việc, nghĩa vụ bảo hành và các mốc thanh toán của hợp đồng đã được nghiệm thu đầy đủ và hợp lệ.',
      };
    }
    return {
      title: 'Quyết Định Chấm Dứt Hợp Đồng Trước Thời Hạn',
      subtitle: 'Quy trình thẩm định và ban hành quyết định chấm dứt hợp đồng',
      headerBg: 'bg-gradient-to-r from-rose-700 via-red-700 to-rose-800',
      icon: ShieldAlert,
      confirmBtnText: 'Ban Hành Quyết Định Chấm Dứt HĐ',
      confirmBtnBg: 'bg-rose-700 hover:bg-rose-800',
      warningMsg:
        'CẢNH BÁO PHÁP LÝ: Hành động chấm dứt hợp đồng sẽ dừng toàn bộ quá trình thi công và giải ngân. Nhà thầu sẽ bị đánh giá vi phạm và trừ điểm uy tín trong toàn hệ thống. Căn cứ Luật Đấu thầu số 22/2023/QH15, quyết định chấm dứt bắt buộc phải có đầy đủ căn cứ pháp lý và văn bản đính kèm.',
    };
  };

  const config = getModalConfig();
  const Icon = config.icon;

  const isConfirmationCodeValid = confirmInput.trim() === String(contractNumber || '').trim();
  const isFormValid = (() => {
    if (isTerminating) {
      return (
        terminationCategory &&
        decisionNumber.trim().length >= 3 &&
        note.trim().length >= 20 &&
        isConfirmationCodeValid &&
        confirmAgreement
      );
    }
    if (isReactivating) {
      return note.trim().length >= 20;
    }
    return true;
  })();

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (isTerminating) {
      if (!decisionNumber.trim()) {
        toast.error('Vui lòng nhập Số hiệu Quyết định / Văn bản pháp lý.');
        return;
      }
      if (note.trim().length < 20) {
        toast.error('Lý do giải trình chấm dứt phải có tối thiểu 20 ký tự.');
        return;
      }
      if (!isConfirmationCodeValid) {
        toast.error('Mã hợp đồng xác nhận chưa chính xác.');
        return;
      }
      if (!confirmAgreement) {
        toast.error('Vui lòng xác nhận cam kết trách nhiệm pháp lý trước khi thực hiện.');
        return;
      }
    }

    if (isReactivating && note.trim().length < 20) {
      toast.error('Lý do khôi phục hợp đồng phải có tối thiểu 20 ký tự giải trình.');
      return;
    }

    try {
      setSubmitting(true);
      await contractApi.changeStatus(
        contractId,
        targetStatus,
        note.trim(),
        isTerminating ? terminationCategory : null,
        isTerminating ? decisionNumber.trim() : null
      );
      toast.success(
        isTerminating
          ? 'Đã ban hành quyết định chấm dứt hợp đồng theo đúng quy định pháp lý.'
          : isReactivating
          ? 'Khôi phục hợp đồng thành công!'
          : isActivating
          ? 'Kích hoạt hợp đồng thành công! Gói thầu đã chuyển sang Contracted.'
          : 'Hoàn thành hợp đồng thành công!'
      );
      onClose();
      if (onSuccess) onSuccess();
    } catch (err) {
      toast.error(err.message || 'Không thể thay đổi trạng thái hợp đồng.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-3 sm:p-4">
      <div className="bg-white rounded-2xl shadow-2xl border border-slate-200 w-full max-w-xl overflow-hidden animate-in fade-in zoom-in duration-200">
        {/* Header */}
        <div className={`${config.headerBg} px-6 py-4 text-white flex items-center justify-between`}>
          <div className="flex items-center space-x-3">
            <div className="p-2.5 bg-white/20 rounded-xl backdrop-blur-xs">
              <Icon className="w-5 h-5 text-white" />
            </div>
            <div>
              <h3 className="text-base font-bold">{config.title}</h3>
              <p className="text-xs text-white/80">Số hiệu: {contractNumber}</p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            disabled={submitting}
            className="text-white/80 hover:text-white p-1 rounded-lg hover:bg-white/10 transition-colors cursor-pointer"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content Form */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          <div className={`p-3.5 rounded-xl text-xs leading-relaxed ${
            isTerminating
              ? 'bg-rose-50 border border-rose-200 text-rose-900'
              : isReactivating
              ? 'bg-amber-50 border border-amber-200 text-amber-900'
              : 'bg-slate-50 border border-slate-200 text-slate-700'
          }`}>
            <p className="font-medium">{config.warningMsg}</p>
          </div>

          {/* CÁC TRƯỜNG PHÁP LÝ RIÊNG BIỆT KHI CHẤM DỨT HỢP ĐỒNG */}
          {isTerminating && (
            <div className="space-y-3.5 bg-slate-50/80 p-4 rounded-xl border border-slate-200">
              <div className="space-y-1">
                <label className="text-xs font-bold text-slate-700 flex items-center gap-1.5">
                  <span>Căn cứ pháp lý chấm dứt hợp đồng</span>
                  <span className="text-rose-500">*</span>
                </label>
                <select
                  value={terminationCategory}
                  onChange={(e) => setTerminationCategory(e.target.value)}
                  className="w-full text-xs text-slate-800 bg-white border border-slate-300 rounded-xl p-2.5 focus:ring-2 focus:ring-rose-500 focus:outline-hidden"
                >
                  <option value="ContractorBreach">
                    Nhà thầu vi phạm nghiêm trọng hợp đồng (Chậm tiến độ, chất lượng không đạt)
                  </option>
                  <option value="ForceMajeure">
                    Sự kiện bất khả kháng (Thiên tai, dịch bệnh, chính sách thay đổi)
                  </option>
                  <option value="MutualAgreement">
                    Hai bên thống nhất thỏa thuận chấm dứt / thanh lý trước hạn
                  </option>
                  <option value="OwnerDecision">
                    Quyết định đơn phương của Chủ đầu tư / Người có thẩm quyền
                  </option>
                </select>
              </div>

              <div className="space-y-1">
                <label className="text-xs font-bold text-slate-700 flex items-center justify-between">
                  <span className="flex items-center gap-1.5">
                    <span>Số hiệu Quyết định / Văn bản pháp lý</span>
                    <span className="text-rose-500">*</span>
                  </span>
                  <span className="text-[10px] text-slate-400">Ví dụ: 125/QĐ-BQLDA</span>
                </label>
                <input
                  type="text"
                  required
                  value={decisionNumber}
                  onChange={(e) => setDecisionNumber(e.target.value)}
                  placeholder="Nhập số quyết định / biên bản chấm dứt hợp đồng..."
                  className="w-full text-xs text-slate-800 bg-white border border-slate-300 rounded-xl p-2.5 focus:ring-2 focus:ring-rose-500 focus:outline-hidden font-mono"
                />
              </div>
            </div>
          )}

          {/* Ô NHẬP LÝ DO / GIẢI TRÌNH */}
          <div className="space-y-1">
            <label className="text-xs font-bold text-slate-700 flex justify-between items-center">
              <span className="flex items-center gap-1">
                <span>
                  {isTerminating
                    ? 'Nội dung / Lý do giải trình chi tiết'
                    : isReactivating
                    ? 'Lý do giải trình khôi phục hiệu lực'
                    : 'Ghi chú phê duyệt / Quyết định liên quan'}
                </span>
                {(isTerminating || isReactivating) && <span className="text-rose-500">* (Tối thiểu 20 ký tự)</span>}
              </span>
              <span className={`text-[10px] ${
                (isTerminating || isReactivating) && note.trim().length < 20
                  ? 'text-rose-600 font-bold'
                  : 'text-slate-400'
              }`}>
                {note.trim().length} / 500 ký tự
              </span>
            </label>
            <textarea
              rows={3}
              required={isTerminating || isReactivating}
              value={note}
              onChange={(e) => setNote(e.target.value)}
              placeholder={
                isTerminating
                  ? 'Nêu rõ hành vi vi phạm của nhà thầu hoặc căn cứ thỏa thuận chấm dứt theo quy định (tối thiểu 20 ký tự)...'
                  : isReactivating
                  ? 'Nêu rõ lý do khôi phục hiệu lực hợp đồng (ví dụ: giải quyết khiếu nại thành công, khắc phục vi phạm)...'
                  : isActivating
                  ? 'Hợp đồng đã hoàn tất thủ tục ký kết và bảo lãnh thực hiện hợp đồng...'
                  : 'Đã hoàn tất toàn bộ biên bản bàn giao, thanh lý và thanh toán đủ...'
              }
              className="w-full text-xs text-slate-800 bg-white border border-slate-300 rounded-xl p-3 focus:ring-2 focus:ring-blue-500 focus:outline-hidden leading-relaxed"
            />
          </div>

          {/* CƠ CHẾ XÁC NHẬN AN TOÀN 2 BƯỚC CHO CHẤM DỨT HỢP ĐỒNG */}
          {isTerminating && (
            <div className="space-y-3 pt-2 border-t border-slate-100">
              <div className="bg-rose-50/60 p-3 rounded-xl border border-rose-200/80 space-y-2">
                <label className="text-[11px] font-bold text-rose-900 block">
                  Xác nhận an toàn (Anti-Mistake): Vui lòng gõ lại chính xác Mã hợp đồng bên dưới để mở khóa:
                </label>
                <div className="flex items-center gap-2">
                  <span className="px-2.5 py-1.5 bg-white border border-rose-200 rounded-lg text-xs font-mono font-bold text-slate-800 select-all">
                    {contractNumber}
                  </span>
                  <input
                    type="text"
                    value={confirmInput}
                    onChange={(e) => setConfirmInput(e.target.value)}
                    placeholder="Gõ lại mã hợp đồng để xác nhận..."
                    className="flex-1 text-xs text-slate-800 bg-white border border-rose-300 rounded-lg p-2 focus:ring-2 focus:ring-rose-500 focus:outline-hidden font-mono"
                  />
                </div>
                {confirmInput && !isConfirmationCodeValid && (
                  <p className="text-[10px] text-rose-600 font-medium">
                    Mã xác nhận chưa khớp với mã hợp đồng.
                  </p>
                )}
              </div>

              <label className="flex items-start gap-2 text-xs text-slate-700 cursor-pointer pt-1">
                <input
                  type="checkbox"
                  checked={confirmAgreement}
                  onChange={(e) => setConfirmAgreement(e.target.checked)}
                  className="mt-0.5 rounded-sm border-slate-300 text-rose-600 focus:ring-rose-500"
                />
                <span className="leading-snug select-none">
                  Tôi đã kiểm tra kỹ hồ sơ pháp lý, chịu hoàn toàn trách nhiệm về quyết định chấm dứt hợp đồng này và hiểu rõ quyết định không thể hoàn tác tùy tiện.
                </span>
              </label>
            </div>
          )}

          <div className="pt-4 border-t border-slate-100 flex items-center justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              disabled={submitting}
              className="px-4 py-2 text-xs font-bold text-slate-600 hover:text-slate-800 hover:bg-slate-100 rounded-xl transition-colors cursor-pointer"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={submitting || !isFormValid}
              className={`inline-flex items-center gap-1.5 px-5 py-2.5 text-xs font-bold text-white rounded-xl shadow-xs transition-all cursor-pointer disabled:opacity-40 disabled:cursor-not-allowed ${config.confirmBtnBg}`}
            >
              {submitting ? (
                <>
                  <span className="w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                  <span>Đang xử lý...</span>
                </>
              ) : (
                <>
                  <Icon className="w-4 h-4" />
                  <span>{config.confirmBtnText}</span>
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ChangeStatusModal;
