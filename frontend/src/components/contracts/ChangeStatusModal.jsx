import React, { useState } from 'react';
import toast from 'react-hot-toast';
import {
  AlertTriangle,
  CheckCircle,
  XCircle,
  X,
  PlayCircle,
  Archive,
  Ban
} from 'lucide-react';
import { contractApi } from '../../api/contractApi';

export const ChangeStatusModal = ({
  isOpen,
  onClose,
  contractId,
  contractNumber,
  targetStatus, // 'Active' | 'Completed' | 'Terminated'
  onSuccess,
}) => {
  const [note, setNote] = useState('');
  const [submitting, setSubmitting] = useState(false);

  if (!isOpen || !targetStatus) return null;

  const isActivating = targetStatus === 'Active' || targetStatus === 1;
  const isCompleting = targetStatus === 'Completed' || targetStatus === 2;
  const isTerminating = targetStatus === 'Terminated' || targetStatus === 3;

  const getModalConfig = () => {
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
      title: 'Chấm Dứt Hợp Đồng Trước Thời Hạn',
      subtitle: 'Dừng thực hiện hợp đồng theo thỏa thuận hoặc vi phạm',
      headerBg: 'bg-gradient-to-r from-rose-600 to-red-700',
      icon: Ban,
      confirmBtnText: 'Xác nhận Chấm dứt Hợp đồng',
      confirmBtnBg: 'bg-rose-600 hover:bg-rose-700',
      warningMsg:
        'CẢNH BÁO: Hành động này sẽ dừng hợp đồng ngay lập tức. Cần nêu rõ căn cứ pháp lý hoặc biên bản thỏa thuận chấm dứt.',
    };
  };

  const config = getModalConfig();
  const Icon = config.icon;

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (isTerminating && !note.trim()) {
      toast.error('Vui lòng nhập lý do chấm dứt hợp đồng.');
      return;
    }

    try {
      setSubmitting(true);
      await contractApi.changeStatus(contractId, targetStatus, note.trim());
      toast.success(
        isActivating
          ? 'Kích hoạt hợp đồng thành công! Gói thầu đã chuyển sang Contracted.'
          : isCompleting
          ? 'Hoàn thành hợp đồng thành công!'
          : 'Đã chấm dứt hợp đồng.'
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
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-xl border border-slate-200 w-full max-w-lg overflow-hidden animate-in fade-in zoom-in duration-200">
        {/* Header */}
        <div className={`${config.headerBg} px-6 py-4 text-white flex items-center justify-between`}>
          <div className="flex items-center space-x-3">
            <div className="p-2 bg-white/20 rounded-xl backdrop-blur-xs">
              <Icon className="w-5 h-5 text-white" />
            </div>
            <div>
              <h3 className="text-base font-bold">{config.title}</h3>
              <p className="text-xs text-white/80">Số hiệu: {contractNumber}</p>
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
          <div className="p-3.5 bg-slate-50 border border-slate-200 rounded-xl text-xs text-slate-700 leading-relaxed">
            <p className="font-medium">{config.warningMsg}</p>
          </div>

          <div className="space-y-1">
            <label className="text-xs font-bold text-slate-700 flex justify-between">
              <span>
                {isTerminating
                  ? 'Căn cứ / Lý do chấm dứt (Bắt buộc)'
                  : 'Ghi chú phê duyệt / Quyết định liên quan (Tùy chọn)'}
              </span>
              <span className="text-[10px] text-slate-400 font-normal">Tối đa 500 ký tự</span>
            </label>
            <textarea
              rows={3}
              required={isTerminating}
              value={note}
              onChange={(e) => setNote(e.target.value)}
              placeholder={
                isActivating
                  ? 'Hợp đồng đã hoàn tất thủ tục ký kết và bảo lãnh thực hiện hợp đồng...'
                  : isCompleting
                  ? 'Đã hoàn tất toàn bộ biên bản bàn giao, thanh lý và thanh toán đủ...'
                  : 'Căn cứ biên bản thanh lý số... do vi phạm tiến độ/thỏa thuận...'
              }
              className="w-full text-xs text-slate-800 bg-white border border-slate-300 rounded-xl p-3 focus:ring-2 focus:ring-blue-500 focus:outline-hidden leading-relaxed"
            />
          </div>

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
              disabled={submitting}
              className={`inline-flex items-center gap-1.5 px-5 py-2 text-xs font-bold text-white rounded-xl shadow-xs transition-transform hover:scale-[1.02] cursor-pointer ${config.confirmBtnBg}`}
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
