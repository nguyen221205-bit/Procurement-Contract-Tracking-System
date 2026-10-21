import React, { useState, useEffect } from 'react';
import toast from 'react-hot-toast';
import { X, Layers, Calendar, DollarSign, FileText, Upload, PlusCircle, AlertCircle } from 'lucide-react';
import { contractApi } from '../../api/contractApi';
import { formatCurrency, formatDate } from '../../utils/formatters';

const ADDENDUM_TYPES = [
  { value: 0, label: 'Gia hạn thời gian hoàn thành (Timeline Extension)' },
  { value: 1, label: 'Điều chỉnh giá trị hợp đồng (Value Adjustment)' },
  { value: 2, label: 'Thay đổi phạm vi / khối lượng (Scope Change)' },
  { value: 3, label: 'Điều chỉnh điều khoản chung khác (General Terms)' },
];

export const AddAddendumModal = ({ isOpen, onClose, contract, onSuccess }) => {
  const [submitting, setSubmitting] = useState(false);
  const [formData, setFormData] = useState({
    addendumNumber: '',
    title: '',
    type: 0,
    additionalValue: 0,
    extensionDays: 0,
    adjustedEndDate: '',
    reason: '',
    signedDate: new Date().toISOString().split('T')[0],
  });
  const [selectedFile, setSelectedFile] = useState(null);

  useEffect(() => {
    if (contract && isOpen) {
      const nextIndex = (contract.addenda?.length || 0) + 1;
      const suggestedNumber = `PL${nextIndex.toString().padStart(2, '0')}/${contract.contractNumber}`;
      const today = new Date().toISOString().split('T')[0];

      setFormData({
        addendumNumber: suggestedNumber,
        title: '',
        type: 0,
        additionalValue: 0,
        extensionDays: 30,
        adjustedEndDate: calculateAdjustedEndDate(contract.endDate, 30),
        reason: 'Điều chỉnh tiến độ thực hiện theo thỏa thuận thống nhất giữa Chủ đầu tư và Nhà thầu',
        signedDate: today,
      });
      setSelectedFile(null);
    }
  }, [contract, isOpen]);

  const calculateAdjustedEndDate = (baseEndDate, days) => {
    if (!baseEndDate) return '';
    const d = new Date(baseEndDate);
    d.setDate(d.getDate() + parseInt(days || 0, 10));
    return d.toISOString().split('T')[0];
  };

  const handleExtensionDaysChange = (days) => {
    const d = parseInt(days, 10) || 0;
    setFormData((prev) => ({
      ...prev,
      extensionDays: d,
      adjustedEndDate: calculateAdjustedEndDate(contract?.endDate, d),
    }));
  };

  if (!isOpen) return null;

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!formData.addendumNumber.trim()) {
      toast.error('Vui lòng nhập Số phụ lục hợp đồng');
      return;
    }
    if (!formData.title.trim()) {
      toast.error('Vui lòng nhập Tiêu đề phụ lục');
      return;
    }
    if (!formData.signedDate) {
      toast.error('Vui lòng chọn Ngày ký phụ lục');
      return;
    }

    setSubmitting(true);
    try {
      const payload = {
        addendumNumber: formData.addendumNumber.trim(),
        title: formData.title.trim(),
        type: Number(formData.type),
        additionalValue: Number(formData.additionalValue) || 0,
        extensionDays: Number(formData.extensionDays) || 0,
        adjustedEndDate: formData.adjustedEndDate ? new Date(formData.adjustedEndDate).toISOString() : null,
        reason: formData.reason?.trim() || null,
        signedDate: new Date(formData.signedDate).toISOString(),
      };

      const res = await contractApi.createAddendum(contract.id, payload);

      if (res?.success) {
        const addendumId = res.data.id;
        // Upload file nếu có
        if (selectedFile) {
          try {
            await contractApi.uploadAddendumDocument(contract.id, addendumId, selectedFile);
            toast.success('Đã lưu phụ lục và đính kèm bản scan thành công!');
          } catch (fileErr) {
            console.error('Lỗi upload file phụ lục:', fileErr);
            toast.success('Đã tạo phụ lục (tệp scan chưa tải lên được).');
          }
        } else {
          toast.success('Đã lập phụ lục hợp đồng thành công!');
        }

        onSuccess();
        onClose();
      } else {
        toast.error(res?.message || 'Không thể tạo phụ lục hợp đồng');
      }
    } catch (err) {
      console.error('Lỗi tạo phụ lục:', err);
      toast.error(err.response?.data?.message || err.message || 'Lỗi khi tạo phụ lục hợp đồng');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-xs overflow-y-auto">
      <div className="bg-white rounded-2xl max-w-xl w-full border border-slate-200 shadow-2xl overflow-hidden my-8 animate-in fade-in zoom-in-95 duration-200">
        {/* Header */}
        <div className="px-6 py-4 bg-gradient-to-r from-indigo-700 to-purple-800 text-white flex items-center justify-between">
          <div className="flex items-center gap-2.5">
            <div className="p-2 bg-white/10 rounded-xl">
              <Layers className="w-5 h-5 text-purple-200" />
            </div>
            <div>
              <h3 className="text-base font-bold">Lập Phụ Lục Hợp Đồng</h3>
              <p className="text-xs text-purple-100">
                Điều chỉnh khối lượng, giá trị hoặc gia hạn thời hạn hợp đồng
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-1.5 hover:bg-white/20 rounded-lg text-white/80 hover:text-white transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Form Body */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          {/* Thông tin HĐ gốc */}
          <div className="p-3 bg-slate-50 border border-slate-200 rounded-xl text-xs grid grid-cols-2 gap-2">
            <div>
              <span className="text-slate-500">Hợp đồng gốc:</span>{' '}
              <strong className="text-slate-900">{contract?.contractNumber}</strong>
            </div>
            <div>
              <span className="text-slate-500">Giá trị hiện tại:</span>{' '}
              <strong className="text-emerald-700">{formatCurrency(contract?.value)}</strong>
            </div>
            <div>
              <span className="text-slate-500">Ngày kết thúc hiện tại:</span>{' '}
              <strong className="text-slate-800">{formatDate(contract?.endDate)}</strong>
            </div>
            <div>
              <span className="text-slate-500">Nhà thầu:</span>{' '}
              <strong className="text-slate-900 truncate block">{contract?.companyName}</strong>
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {/* Số phụ lục */}
            <div className="space-y-1">
              <label className="text-xs font-bold text-slate-700 flex items-center gap-1">
                Số phụ lục <span className="text-rose-500">*</span>
              </label>
              <input
                type="text"
                required
                placeholder="VD: PL01/HD-2026-001"
                value={formData.addendumNumber}
                onChange={(e) => setFormData({ ...formData, addendumNumber: e.target.value })}
                className="w-full px-3 py-2 text-xs border border-slate-300 rounded-xl focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500"
              />
            </div>

            {/* Loại phụ lục */}
            <div className="space-y-1">
              <label className="text-xs font-bold text-slate-700 flex items-center gap-1">
                Phân loại điều chỉnh <span className="text-rose-500">*</span>
              </label>
              <select
                value={formData.type}
                onChange={(e) => setFormData({ ...formData, type: parseInt(e.target.value, 10) })}
                className="w-full px-3 py-2 text-xs border border-slate-300 rounded-xl focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 bg-white"
              >
                {ADDENDUM_TYPES.map((t) => (
                  <option key={t.value} value={t.value}>
                    {t.label}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {/* Tiêu đề */}
          <div className="space-y-1">
            <label className="text-xs font-bold text-slate-700 flex items-center gap-1">
              Tiêu đề phụ lục <span className="text-rose-500">*</span>
            </label>
            <input
              type="text"
              required
              placeholder="VD: Gia hạn thời gian hoàn thành gói thầu thêm 45 ngày do mặt bằng bàn giao chậm"
              value={formData.title}
              onChange={(e) => setFormData({ ...formData, title: e.target.value })}
              className="w-full px-3 py-2 text-xs border border-slate-300 rounded-xl focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 font-medium"
            />
          </div>

          {/* Nếu là Gia hạn hoặc Thay đổi phạm vi */}
          {(formData.type === 0 || formData.type === 2) && (
            <div className="p-3.5 bg-indigo-50/60 border border-indigo-100 rounded-xl space-y-3">
              <span className="text-[11px] font-bold text-indigo-900 block flex items-center gap-1.5">
                <Calendar className="w-3.5 h-3.5 text-indigo-600" />
                Thông tin gia hạn tiến độ
              </span>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div className="space-y-1">
                  <label className="text-xs text-slate-700 font-semibold">Số ngày gia hạn thêm</label>
                  <input
                    type="number"
                    min="0"
                    value={formData.extensionDays}
                    onChange={(e) => handleExtensionDaysChange(e.target.value)}
                    className="w-full px-3 py-2 text-xs border border-slate-300 rounded-xl focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 bg-white"
                  />
                </div>
                <div className="space-y-1">
                  <label className="text-xs text-slate-700 font-semibold">Ngày kết thúc mới sau gia hạn</label>
                  <input
                    type="date"
                    value={formData.adjustedEndDate}
                    onChange={(e) => setFormData({ ...formData, adjustedEndDate: e.target.value })}
                    className="w-full px-3 py-2 text-xs border border-slate-300 rounded-xl focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 bg-white font-bold text-indigo-700"
                  />
                </div>
              </div>
            </div>
          )}

          {/* Nếu là Điều chỉnh giá trị hoặc Thay đổi phạm vi */}
          {(formData.type === 1 || formData.type === 2) && (
            <div className="p-3.5 bg-emerald-50/60 border border-emerald-100 rounded-xl space-y-3">
              <span className="text-[11px] font-bold text-emerald-900 block flex items-center gap-1.5">
                <DollarSign className="w-3.5 h-3.5 text-emerald-600" />
                Thông tin điều chỉnh giá trị hợp đồng
              </span>
              <div className="space-y-1">
                <label className="text-xs text-slate-700 font-semibold">
                  Giá trị điều chỉnh (+ tăng, - giảm) (VNĐ)
                </label>
                <input
                  type="number"
                  step="1000"
                  value={formData.additionalValue}
                  onChange={(e) => setFormData({ ...formData, additionalValue: e.target.value })}
                  placeholder="Nhập số tiền phát sinh tăng hoặc giảm..."
                  className="w-full px-3 py-2 text-xs border border-slate-300 rounded-xl focus:ring-2 focus:ring-emerald-500/20 focus:border-emerald-500 bg-white font-bold text-emerald-800"
                />
                <div className="flex justify-between text-[11px] text-slate-600 pt-0.5">
                  <span>Điều chỉnh: <strong>{formatCurrency(formData.additionalValue)}</strong></span>
                  <span>
                    Tổng HĐ sau điều chỉnh:{' '}
                    <strong className="text-emerald-700">
                      {formatCurrency(Number(contract?.value || 0) + Number(formData.additionalValue || 0))}
                    </strong>
                  </span>
                </div>
              </div>
            </div>
          )}

          {/* Ngày ký & Lý do */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-1">
              <label className="text-xs font-bold text-slate-700 flex items-center gap-1">
                Ngày ký kết phụ lục <span className="text-rose-500">*</span>
              </label>
              <input
                type="date"
                required
                value={formData.signedDate}
                onChange={(e) => setFormData({ ...formData, signedDate: e.target.value })}
                className="w-full px-3 py-2 text-xs border border-slate-300 rounded-xl focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500"
              />
            </div>

            <div className="space-y-1">
              <label className="text-xs font-bold text-slate-700">Đính kèm bản scan phụ lục</label>
              <div className="border border-dashed border-slate-300 rounded-xl p-2 bg-slate-50 flex items-center justify-between gap-2">
                <span className="text-[11px] text-slate-500 truncate">
                  {selectedFile ? selectedFile.name : 'Chọn file PDF scan...'}
                </span>
                <label className="px-2.5 py-1 bg-white border border-slate-300 text-slate-700 hover:bg-slate-100 rounded-lg text-xs font-semibold cursor-pointer shrink-0 transition-colors">
                  Chọn
                  <input
                    type="file"
                    accept=".pdf,.png,.jpg,.jpeg"
                    className="hidden"
                    onChange={(e) => setSelectedFile(e.target.files?.[0] || null)}
                  />
                </label>
              </div>
            </div>
          </div>

          {/* Lý do / Căn cứ pháp lý */}
          <div className="space-y-1">
            <label className="text-xs font-bold text-slate-700">Căn cứ pháp lý & Lý do điều chỉnh</label>
            <textarea
              rows={2}
              value={formData.reason}
              onChange={(e) => setFormData({ ...formData, reason: e.target.value })}
              placeholder="Ghi rõ lý do điều chỉnh theo biên bản hiện trường hoặc công văn đề xuất..."
              className="w-full px-3 py-2 text-xs border border-slate-300 rounded-xl focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500"
            />
          </div>

          {/* Footer actions */}
          <div className="pt-3 border-t border-slate-100 flex justify-end gap-2.5">
            <button
              type="button"
              onClick={onClose}
              disabled={submitting}
              className="px-4 py-2 border border-slate-300 text-slate-700 text-xs font-bold rounded-xl hover:bg-slate-100 transition-colors"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={submitting}
              className="px-5 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:bg-slate-300 text-white text-xs font-bold rounded-xl shadow-xs transition-colors flex items-center gap-1.5"
            >
              <Layers className="w-4 h-4" />
              <span>{submitting ? 'Đang lưu...' : 'Ban Hành Phụ Lục Hợp Đồng'}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default AddAddendumModal;
