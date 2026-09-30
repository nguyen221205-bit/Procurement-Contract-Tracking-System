import React, { useState, useEffect } from 'react';
import toast from 'react-hot-toast';
import {
  Calendar,
  DollarSign,
  FileText,
  X,
  AlertCircle,
  Plus,
  Save,
  CheckCircle2,
  Clock
} from 'lucide-react';
import { contractApi } from '../../api/contractApi';
import { formatCurrency, formatNumber } from '../../utils/formatters';

export const MilestoneModal = ({
  isOpen,
  onClose,
  contractId,
  contractValue,
  existingMilestones = [],
  milestoneToEdit = null,
  onSuccess,
}) => {
  const isEditing = Boolean(milestoneToEdit);

  const [title, setTitle] = useState('');
  const [dueDate, setDueDate] = useState('');
  const [amount, setAmount] = useState('');
  const [submitting, setSubmitting] = useState(false);

  // Remaining budget available for this milestone
  const otherMilestonesTotal = existingMilestones
    .filter((m) => !isEditing || m.id !== milestoneToEdit.id)
    .reduce((sum, m) => sum + (m.amount || 0), 0);

  const maxAllowedAmount = Math.max(0, contractValue - otherMilestonesTotal);

  useEffect(() => {
    if (isOpen) {
      if (milestoneToEdit) {
        setTitle(milestoneToEdit.title || '');
        setDueDate(
          milestoneToEdit.dueDate ? milestoneToEdit.dueDate.split('T')[0] : ''
        );
        setAmount(milestoneToEdit.amount ? milestoneToEdit.amount.toString() : '');
      } else {
        // Pre-fill suggested title
        const nextIndex = existingMilestones.length + 1;
        setTitle(`Nghiệm thu Giai đoạn ${nextIndex}: `);

        // Pre-fill default due date (+30 days)
        const nextMonth = new Date();
        nextMonth.setDate(nextMonth.getDate() + 30 * nextIndex);
        setDueDate(nextMonth.toISOString().split('T')[0]);

        // Pre-fill remaining amount if > 0
        if (maxAllowedAmount > 0) {
          setAmount(maxAllowedAmount.toString());
        } else {
          setAmount('');
        }
      }
    }
  }, [isOpen, milestoneToEdit, existingMilestones, contractValue]);

  if (!isOpen) return null;

  const numAmount = Number(amount) || 0;
  const percentOfContract =
    contractValue > 0 ? ((numAmount / contractValue) * 100).toFixed(1) : 0;

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!title.trim()) {
      toast.error('Vui lòng nhập tiêu đề mốc nghiệm thu.');
      return;
    }

    if (!dueDate) {
      toast.error('Vui lòng chọn ngày đến hạn nghiệm thu.');
      return;
    }

    if (!numAmount || numAmount <= 0) {
      toast.error('Số tiền mốc thanh toán phải lớn hơn 0.');
      return;
    }

    if (numAmount > maxAllowedAmount) {
      toast.error(
        `Số tiền mốc (${formatCurrency(numAmount)}) vượt quá ngân sách khả dụng còn lại (${formatCurrency(maxAllowedAmount)}).`
      );
      return;
    }

    try {
      setSubmitting(true);
      const payload = {
        title: title.trim(),
        dueDate: new Date(dueDate).toISOString(),
        amount: numAmount,
      };

      if (isEditing) {
        await contractApi.updateMilestone(contractId, milestoneToEdit.id, payload);
        toast.success('Cập nhật mốc thanh toán thành công!');
      } else {
        await contractApi.addMilestone(contractId, payload);
        toast.success('Thêm mốc thanh toán mới thành công!');
      }

      onClose();
      if (onSuccess) onSuccess();
    } catch (err) {
      toast.error(err.message || 'Không thể lưu mốc thanh toán.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-xl border border-slate-200 w-full max-w-lg overflow-hidden animate-in fade-in zoom-in duration-200">
        {/* Header */}
        <div className="bg-gradient-to-r from-blue-600 to-indigo-700 px-6 py-4 text-white flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <div className="p-2 bg-white/20 rounded-xl backdrop-blur-xs">
              <Clock className="w-5 h-5 text-white" />
            </div>
            <div>
              <h3 className="text-base font-bold">
                {isEditing ? 'Cập Nhật Mốc Nghiệm Thu' : 'Thêm Mốc Nghiệm Thu Mới'}
              </h3>
              <p className="text-xs text-blue-100">
                Phân bổ đợt thanh toán & hạn nghiệm thu theo hợp đồng
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
          {/* Thông tin ngân sách mốc */}
          <div className="p-3.5 bg-slate-50 border border-slate-200 rounded-xl space-y-1.5 text-xs">
            <div className="flex justify-between items-center text-slate-600">
              <span>Tổng giá trị hợp đồng:</span>
              <strong className="text-slate-900 font-bold">{formatCurrency(contractValue)}</strong>
            </div>
            <div className="flex justify-between items-center text-slate-600">
              <span>Đã phân bổ cho các mốc khác:</span>
              <span className="font-semibold text-slate-800">{formatCurrency(otherMilestonesTotal)}</span>
            </div>
            <div className="flex justify-between items-center border-t border-slate-200 pt-1.5 font-bold text-slate-900">
              <span className="text-emerald-700">Ngân sách còn lại cho mốc này:</span>
              <span className="text-emerald-700">{formatCurrency(maxAllowedAmount)}</span>
            </div>
          </div>

          {/* Tiêu đề mốc */}
          <div className="space-y-1">
            <label className="text-xs font-bold text-slate-700">
              Tên / Nội dung mốc nghiệm thu <span className="text-rose-500">*</span>
            </label>
            <input
              type="text"
              required
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="VD: Nghiệm thu Giai đoạn 1: Bàn giao hạ tầng Cloud..."
              className="w-full text-xs font-medium text-slate-800 bg-white border border-slate-300 rounded-xl px-3.5 py-2.5 focus:ring-2 focus:ring-blue-500 focus:outline-hidden"
            />
          </div>

          {/* Ngày đến hạn */}
          <div className="space-y-1">
            <label className="text-xs font-bold text-slate-700 flex items-center gap-1">
              <Calendar className="w-3.5 h-3.5 text-slate-500" />
              <span>Hạn nghiệm thu (DueDate) <span className="text-rose-500">*</span></span>
            </label>
            <input
              type="date"
              required
              value={dueDate}
              onChange={(e) => setDueDate(e.target.value)}
              className="w-full text-xs text-slate-800 bg-white border border-slate-300 rounded-xl px-3.5 py-2.5 focus:ring-2 focus:ring-blue-500 focus:outline-hidden"
            />
          </div>

          {/* Số tiền mốc */}
          <div className="space-y-1">
            <div className="flex justify-between items-center text-xs">
              <label className="font-bold text-slate-700">
                Giá trị giải ngân của mốc (VNĐ) <span className="text-rose-500">*</span>
              </label>
              <span className="text-[11px] font-bold text-blue-600">
                {percentOfContract}% tổng hợp đồng
              </span>
            </div>
            <input
              type="number"
              required
              min={1}
              max={maxAllowedAmount}
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              placeholder="Nhập số tiền..."
              className="w-full text-xs font-bold text-slate-800 bg-white border border-slate-300 rounded-xl px-3.5 py-2.5 focus:ring-2 focus:ring-blue-500 focus:outline-hidden"
            />
            <div className="text-[11px] text-emerald-600 font-semibold text-right">
              {formatCurrency(numAmount)}
            </div>
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
              disabled={submitting}
              className="inline-flex items-center gap-1.5 px-5 py-2.5 bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold rounded-xl shadow-xs transition-transform hover:scale-[1.02] cursor-pointer"
            >
              {submitting ? (
                <>
                  <span className="w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                  <span>Đang lưu...</span>
                </>
              ) : (
                <>
                  {isEditing ? <Save className="w-4 h-4" /> : <Plus className="w-4 h-4" />}
                  <span>{isEditing ? 'Lưu cập nhật' : 'Thêm mốc thanh toán'}</span>
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default MilestoneModal;
