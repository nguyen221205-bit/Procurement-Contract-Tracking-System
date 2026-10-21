import React, { useState, useEffect } from 'react';
import toast from 'react-hot-toast';
import { X, ShieldCheck, DollarSign, Calendar, Building, FileText, Upload } from 'lucide-react';
import { contractApi } from '../../api/contractApi';
import { formatCurrency } from '../../utils/formatters';

const POPULAR_BANKS = [
  'Ngân hàng TMCP Ngoại thương Việt Nam (Vietcombank)',
  'Ngân hàng TMCP Đầu tư và Phát triển Việt Nam (BIDV)',
  'Ngân hàng Nông nghiệp và Phát triển Nông thôn (Agribank)',
  'Ngân hàng TMCP Công thương Việt Nam (VietinBank)',
  'Ngân hàng TMCP Kỹ thương Việt Nam (Techcombank)',
  'Ngân hàng TMCP Quân đội (MBBank)',
  'Ngân hàng TMCP Á Châu (ACB)',
  'Ngân hàng TMCP Việt Nam Thịnh Vượng (VPBank)',
  'Ngân hàng TMCP Tiên Phong (TPBank)',
  'Ngân hàng TMCP Sài Gòn - Hà Nội (SHB)',
];

export const AddGuaranteeModal = ({ isOpen, onClose, contract, onSuccess }) => {
  const [submitting, setSubmitting] = useState(false);
  const [formData, setFormData] = useState({
    guaranteeNumber: '',
    bankName: '',
    amount: '',
    percentage: 3, // mặc định 3% theo Luật Đấu thầu
    effectiveDate: '',
    expiryDate: '',
    notes: '',
  });
  const [selectedFile, setSelectedFile] = useState(null);

  useEffect(() => {
    if (contract && isOpen) {
      const defaultPercentage = 3;
      const defaultAmount = Math.round(((contract.value || 0) * defaultPercentage) / 100);
      const today = new Date().toISOString().split('T')[0];
      // Mặc định hết hạn sau ngày kết thúc hợp đồng 30 ngày
      let expDate = '';
      if (contract.endDate) {
        const d = new Date(contract.endDate);
        d.setDate(d.getDate() + 30);
        expDate = d.toISOString().split('T')[0];
      }

      setFormData({
        guaranteeNumber: '',
        bankName: POPULAR_BANKS[0],
        amount: defaultAmount,
        percentage: defaultPercentage,
        effectiveDate: today,
        expiryDate: expDate,
        notes: 'Bảo lãnh thực hiện hợp đồng theo Điều 68 Luật Đấu thầu số 22/2023/QH15',
      });
      setSelectedFile(null);
    }
  }, [contract, isOpen]);

  if (!isOpen) return null;

  const handlePercentageChange = (p) => {
    const pct = parseFloat(p) || 0;
    const calculatedAmount = Math.round(((contract?.value || 0) * pct) / 100);
    setFormData((prev) => ({
      ...prev,
      percentage: pct,
      amount: calculatedAmount,
    }));
  };

  const handleAmountChange = (amt) => {
    const a = parseFloat(amt) || 0;
    const calculatedPct = contract?.value > 0 ? ((a / contract.value) * 100).toFixed(2) : 0;
    setFormData((prev) => ({
      ...prev,
      amount: a,
      percentage: parseFloat(calculatedPct),
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!formData.guaranteeNumber.trim()) {
      toast.error('Vui lòng nhập Số chứng thư bảo lãnh ngân hàng');
      return;
    }
    if (!formData.bankName.trim()) {
      toast.error('Vui lòng chọn hoặc nhập Ngân hàng phát hành');
      return;
    }
    if (formData.amount <= 0) {
      toast.error('Số tiền bảo lãnh phải lớn hơn 0');
      return;
    }
    if (!formData.effectiveDate || !formData.expiryDate) {
      toast.error('Vui lòng chọn ngày hiệu lực và ngày hết hạn');
      return;
    }
    if (new Date(formData.expiryDate) <= new Date(formData.effectiveDate)) {
      toast.error('Ngày hết hạn phải sau ngày có hiệu lực của chứng thư bảo lãnh');
      return;
    }

    setSubmitting(true);
    try {
      const res = await contractApi.addGuarantee(contract.id, {
        guaranteeNumber: formData.guaranteeNumber.trim(),
        bankName: formData.bankName.trim(),
        amount: Number(formData.amount),
        percentage: Number(formData.percentage),
        effectiveDate: new Date(formData.effectiveDate).toISOString(),
        expiryDate: new Date(formData.expiryDate).toISOString(),
        notes: formData.notes?.trim() || null,
      });

      if (res?.success) {
        const guaranteeId = res.data.id;
        // Nếu có đính kèm file, upload luôn
        if (selectedFile) {
          try {
            await contractApi.uploadGuaranteeDocument(contract.id, guaranteeId, selectedFile);
            toast.success('Đã lưu chứng thư bảo lãnh và đính kèm bản scan thành công!');
          } catch (fileErr) {
            console.error('Lỗi upload file bảo lãnh:', fileErr);
            toast.success('Đã lưu chứng thư bảo lãnh (tệp đính kèm chưa tải lên được).');
          }
        } else {
          toast.success('Đã lưu chứng thư bảo lãnh thành công!');
        }

        onSuccess();
        onClose();
      } else {
        toast.error(res?.message || 'Không thể tạo chứng thư bảo lãnh');
      }
    } catch (err) {
      console.error('Lỗi tạo bảo lãnh:', err);
      toast.error(err.response?.data?.message || err.message || 'Lỗi khi lưu chứng thư bảo lãnh');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-xs overflow-y-auto">
      <div className="bg-white rounded-2xl max-w-xl w-full border border-slate-200 shadow-2xl overflow-hidden my-8 animate-in fade-in zoom-in-95 duration-200">
        {/* Header */}
        <div className="px-6 py-4 bg-gradient-to-r from-blue-700 to-indigo-800 text-white flex items-center justify-between">
          <div className="flex items-center gap-2.5">
            <div className="p-2 bg-white/10 rounded-xl">
              <ShieldCheck className="w-5 h-5 text-blue-200" />
            </div>
            <div>
              <h3 className="text-base font-bold">Thêm Bảo Lãnh Thực Hiện Hợp Đồng</h3>
              <p className="text-xs text-blue-100">
                Theo Điều 14 & Điều 68 Luật Đấu thầu số 22/2023/QH15
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
          {/* Thông tin HĐ tóm tắt */}
          <div className="p-3 bg-slate-50 border border-slate-200 rounded-xl text-xs flex justify-between items-center">
            <div>
              <span className="text-slate-500">Hợp đồng:</span>{' '}
              <strong className="text-slate-900">{contract?.contractNumber}</strong>
            </div>
            <div>
              <span className="text-slate-500">Giá trị HĐ:</span>{' '}
              <strong className="text-emerald-700">{formatCurrency(contract?.value)}</strong>
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {/* Số chứng thư */}
            <div className="space-y-1">
              <label className="text-xs font-bold text-slate-700 flex items-center gap-1">
                Số chứng thư bảo lãnh <span className="text-rose-500">*</span>
              </label>
              <input
                type="text"
                required
                placeholder="VD: BL-VCB-2026-0899"
                value={formData.guaranteeNumber}
                onChange={(e) => setFormData({ ...formData, guaranteeNumber: e.target.value })}
                className="w-full px-3 py-2 text-xs border border-slate-300 rounded-xl focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
              />
            </div>

            {/* Ngân hàng phát hành */}
            <div className="space-y-1">
              <label className="text-xs font-bold text-slate-700 flex items-center gap-1">
                Ngân hàng phát hành <span className="text-rose-500">*</span>
              </label>
              <input
                list="bank-list"
                required
                placeholder="Chọn hoặc nhập tên ngân hàng"
                value={formData.bankName}
                onChange={(e) => setFormData({ ...formData, bankName: e.target.value })}
                className="w-full px-3 py-2 text-xs border border-slate-300 rounded-xl focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
              />
              <datalist id="bank-list">
                {POPULAR_BANKS.map((b) => (
                  <option key={b} value={b} />
                ))}
              </datalist>
            </div>
          </div>

          {/* Tỷ lệ % và Số tiền */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-1">
              <label className="text-xs font-bold text-slate-700 flex items-center gap-1">
                Tỷ lệ bảo đảm (%) <span className="text-slate-400 font-normal">(Luật quy định 2% - 10%)</span>
              </label>
              <div className="flex items-center gap-2">
                <input
                  type="number"
                  step="0.1"
                  min="0.1"
                  max="100"
                  required
                  value={formData.percentage}
                  onChange={(e) => handlePercentageChange(e.target.value)}
                  className="w-full px-3 py-2 text-xs border border-slate-300 rounded-xl focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-bold text-blue-700"
                />
                <div className="flex gap-1">
                  {[2, 3, 5].map((pct) => (
                    <button
                      key={pct}
                      type="button"
                      onClick={() => handlePercentageChange(pct)}
                      className={`px-2 py-1 text-[11px] font-bold rounded-lg border transition-colors ${
                        formData.percentage === pct
                          ? 'bg-blue-600 text-white border-blue-600'
                          : 'bg-slate-50 text-slate-600 border-slate-200 hover:bg-slate-100'
                      }`}
                    >
                      {pct}%
                    </button>
                  ))}
                </div>
              </div>
            </div>

            <div className="space-y-1">
              <label className="text-xs font-bold text-slate-700 flex items-center gap-1">
                Số tiền bảo lãnh (VNĐ) <span className="text-rose-500">*</span>
              </label>
              <input
                type="number"
                min="1000"
                step="1000"
                required
                value={formData.amount}
                onChange={(e) => handleAmountChange(e.target.value)}
                className="w-full px-3 py-2 text-xs border border-slate-300 rounded-xl focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-bold text-emerald-700"
              />
              <span className="text-[11px] text-slate-500 block truncate">
                {formatCurrency(formData.amount)}
              </span>
            </div>
          </div>

          {/* Ngày hiệu lực & Ngày hết hạn */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-1">
              <label className="text-xs font-bold text-slate-700 flex items-center gap-1">
                Ngày bắt đầu hiệu lực <span className="text-rose-500">*</span>
              </label>
              <input
                type="date"
                required
                value={formData.effectiveDate}
                onChange={(e) => setFormData({ ...formData, effectiveDate: e.target.value })}
                className="w-full px-3 py-2 text-xs border border-slate-300 rounded-xl focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
              />
            </div>

            <div className="space-y-1">
              <label className="text-xs font-bold text-slate-700 flex items-center gap-1">
                Ngày hết hạn bảo lãnh <span className="text-rose-500">*</span>
              </label>
              <input
                type="date"
                required
                value={formData.expiryDate}
                onChange={(e) => setFormData({ ...formData, expiryDate: e.target.value })}
                className="w-full px-3 py-2 text-xs border border-slate-300 rounded-xl focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
              />
            </div>
          </div>

          {/* Upload file scan */}
          <div className="space-y-1">
            <label className="text-xs font-bold text-slate-700 flex items-center gap-1">
              Đính kèm bản scan chứng thư bảo lãnh (PDF/Hình ảnh)
            </label>
            <div className="border border-dashed border-slate-300 rounded-xl p-3 bg-slate-50/50 flex items-center justify-between gap-3">
              <div className="flex items-center gap-2 text-xs text-slate-600 truncate">
                <Upload className="w-4 h-4 text-blue-600 shrink-0" />
                <span className="truncate">
                  {selectedFile ? selectedFile.name : 'Chưa chọn tệp scan đính kèm...'}
                </span>
              </div>
              <label className="px-3 py-1.5 bg-white border border-slate-300 text-slate-700 hover:bg-slate-100 rounded-lg text-xs font-semibold cursor-pointer shrink-0 transition-colors">
                Chọn tệp
                <input
                  type="file"
                  accept=".pdf,.png,.jpg,.jpeg"
                  className="hidden"
                  onChange={(e) => setSelectedFile(e.target.files?.[0] || null)}
                />
              </label>
            </div>
            {selectedFile && (
              <span className="text-[11px] text-emerald-600 font-semibold block">
                Dung lượng: {(selectedFile.size / 1024 / 1024).toFixed(2)} MB
              </span>
            )}
          </div>

          {/* Ghi chú */}
          <div className="space-y-1">
            <label className="text-xs font-bold text-slate-700">Ghi chú / Điều khoản bảo đảm</label>
            <textarea
              rows={2}
              value={formData.notes}
              onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
              placeholder="Ghi chú về nội dung bảo đảm hoặc điều kiện giải tỏa bảo lãnh..."
              className="w-full px-3 py-2 text-xs border border-slate-300 rounded-xl focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
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
              className="px-5 py-2 bg-blue-600 hover:bg-blue-700 disabled:bg-slate-300 text-white text-xs font-bold rounded-xl shadow-xs transition-colors flex items-center gap-1.5"
            >
              <ShieldCheck className="w-4 h-4" />
              <span>{submitting ? 'Đang lưu...' : 'Lưu Chứng Thư Bảo Lãnh'}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default AddGuaranteeModal;
