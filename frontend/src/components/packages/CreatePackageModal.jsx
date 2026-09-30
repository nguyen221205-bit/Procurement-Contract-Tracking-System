import React, { useState } from 'react';
import { X, Calendar, DollarSign, FileText, Upload, Plus, Trash2, Box, Hammer, Briefcase, Clock, AlertCircle } from 'lucide-react';
import toast from 'react-hot-toast';
import { bidPackageApi } from '../../api/bidPackageApi';
import { formatVND } from '../../utils/formatters';

const PACKAGE_TYPE_OPTIONS = [
  { value: 'Goods', label: 'Hàng hóa', icon: Box, color: 'text-sky-600 bg-sky-50 border-sky-200' },
  { value: 'Construction', label: 'Xây lắp', icon: Hammer, color: 'text-purple-600 bg-purple-50 border-purple-200' },
  { value: 'Service', label: 'Dịch vụ', icon: Briefcase, color: 'text-indigo-600 bg-indigo-50 border-indigo-200' },
];

export const CreatePackageModal = ({ isOpen, onClose, onSuccess }) => {
  const [formData, setFormData] = useState({
    name: '',
    code: '',
    type: 'Goods',
    budget: '',
    deadline: '',
    description: '',
  });

  const [files, setFiles] = useState([]);
  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState({});

  if (!isOpen) return null;

  // Tính ngày tối thiểu cho input datetime-local (thời điểm hiện tại)
  const now = new Date();
  const minDateTime = new Date(now.getTime() - now.getTimezoneOffset() * 60000)
    .toISOString()
    .slice(0, 16);

  // Xử lý chọn nhanh hạn nộp (+7, +15, +30 ngày)
  const handleShortcutDeadline = (days) => {
    const targetDate = new Date();
    targetDate.setDate(targetDate.getDate() + days);
    targetDate.setHours(17, 0, 0, 0); // Mặc định 17:00 chiều của ngày hết hạn

    const localFormatted = new Date(targetDate.getTime() - targetDate.getTimezoneOffset() * 60000)
      .toISOString()
      .slice(0, 16);

    setFormData((prev) => ({ ...prev, deadline: localFormatted }));
    if (errors.deadline) {
      setErrors((prev) => ({ ...prev, deadline: null }));
    }
  };

  const handleInputChange = (field, value) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => ({ ...prev, [field]: null }));
    }
  };

  // Quản lý tệp đính kèm HSMT ban đầu
  const handleFileChange = (e) => {
    const selectedFiles = Array.from(e.target.files || []);
    if (selectedFiles.length > 0) {
      setFiles((prev) => [...prev, ...selectedFiles]);
    }
    e.target.value = '';
  };

  const handleRemoveFile = (index) => {
    setFiles((prev) => prev.filter((_, idx) => idx !== index));
  };

  // Validate form
  const validateForm = () => {
    const newErrors = {};

    if (!formData.name.trim()) {
      newErrors.name = 'Vui lòng nhập tên gói thầu.';
    } else if (formData.name.trim().length > 300) {
      newErrors.name = 'Tên gói thầu không được vượt quá 300 ký tự.';
    }

    const budgetNum = Number(formData.budget);
    if (!formData.budget || isNaN(budgetNum) || budgetNum <= 0) {
      newErrors.budget = 'Ngân sách dự toán phải lớn hơn 0.';
    }

    if (!formData.deadline) {
      newErrors.deadline = 'Vui lòng chọn thời hạn nộp hồ sơ thầu.';
    } else {
      const selectedDeadline = new Date(formData.deadline);
      if (selectedDeadline <= new Date()) {
        newErrors.deadline = 'Thời hạn nộp thầu phải nằm trong tương lai.';
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // Gửi tạo gói thầu
  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validateForm()) return;

    try {
      setLoading(true);

      const payload = {
        name: formData.name.trim(),
        code: formData.code.trim() || undefined,
        type: formData.type,
        budget: Number(formData.budget),
        deadline: new Date(formData.deadline).toISOString(),
        description: formData.description.trim() || undefined,
      };

      // 1. Gọi API tạo gói thầu
      const res = await bidPackageApi.createPackage(payload);
      if (!res?.data?.id) {
        throw new Error(res?.message || 'Khởi tạo gói thầu thất bại');
      }

      const createdPackage = res.data;

      // 2. Nếu có tệp đính kèm ban đầu, tải lên API documents
      if (files.length > 0) {
        try {
          const docFormData = new FormData();
          files.forEach((file) => {
            docFormData.append('files', file);
          });
          await bidPackageApi.uploadDocuments(createdPackage.id, docFormData);
        } catch (uploadErr) {
          console.warn('Gói thầu đã tạo nhưng tải tài liệu HSMT gặp lỗi:', uploadErr);
          toast.error('Gói thầu đã tạo nhưng có lỗi khi tải lên tài liệu đính kèm.');
        }
      }

      toast.success(`Khởi tạo gói thầu ${createdPackage.code} thành công!`);

      // Reset form
      setFormData({
        name: '',
        code: '',
        type: 'Goods',
        budget: '',
        deadline: '',
        description: '',
      });
      setFiles([]);
      setErrors({});

      // Bàn giao cho component cha (chuyển hướng sang trang chi tiết theo Lựa chọn B)
      if (onSuccess) {
        onSuccess(createdPackage);
      }
    } catch (err) {
      toast.error(err.message || 'Không thể tạo gói thầu. Vui lòng thử lại.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/50 backdrop-blur-xs flex items-center justify-center p-4 animate-in fade-in duration-150">
      <div
        className="bg-white rounded-2xl max-w-2xl w-full shadow-2xl border border-slate-100 relative overflow-hidden flex flex-col max-h-[92vh]"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header Modal */}
        <div className="px-6 py-4.5 border-b border-slate-100 flex items-center justify-between bg-slate-50/70">
          <div>
            <h3 className="text-base font-bold text-slate-900">Tạo gói thầu mới</h3>
            <p className="text-xs text-slate-500 mt-0.5">Khởi tạo hồ sơ mời thầu mới trong hệ thống</p>
          </div>
          <button
            onClick={onClose}
            disabled={loading}
            className="text-slate-400 hover:text-slate-600 p-1.5 rounded-lg hover:bg-slate-200/60 transition disabled:opacity-50"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Thân Form cuộn được */}
        <form onSubmit={handleSubmit} noValidate className="p-6 overflow-y-auto space-y-5 text-xs flex-1">
          {/* Tên gói thầu */}
          <div className="space-y-1.5">
            <div className="flex items-center justify-between">
              <label className="font-semibold text-slate-800">
                Tên gói thầu <span className="text-rose-500">*</span>
              </label>
              <span className="text-[11px] text-slate-400 font-mono">
                {formData.name.length}/300
              </span>
            </div>
            <input
              type="text"
              maxLength={300}
              value={formData.name}
              onChange={(e) => handleInputChange('name', e.target.value)}
              placeholder="VD: Mua sắm thiết bị CNTT & Máy chủ Năm 2026"
              className={`w-full px-3.5 py-2.5 bg-white border rounded-xl text-xs focus:ring-2 focus:ring-sky-500/20 focus:border-sky-500 transition outline-none ${
                errors.name ? 'border-rose-400 bg-rose-50/30' : 'border-slate-200'
              }`}
            />
            {errors.name && <p className="text-rose-500 text-[11px]">{errors.name}</p>}
          </div>

          {/* Grid 2 cột: Mã gói thầu (tùy chọn) & Loại gói thầu */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {/* Mã gói thầu */}
            <div className="space-y-1.5">
              <label className="font-semibold text-slate-800">
                Mã gói thầu <span className="text-slate-400 font-normal">(Tùy chọn)</span>
              </label>
              <input
                type="text"
                maxLength={50}
                value={formData.code}
                onChange={(e) => handleInputChange('code', e.target.value.toUpperCase())}
                placeholder="Hệ thống tự sinh nếu để trống"
                className="w-full px-3.5 py-2.5 bg-white border border-slate-200 rounded-xl text-xs font-mono focus:ring-2 focus:ring-sky-500/20 focus:border-sky-500 transition outline-none"
              />
              <p className="text-[10px] text-slate-400">VD: PKG-20260929-1234</p>
            </div>

            {/* Loại gói thầu */}
            <div className="space-y-1.5">
              <label className="font-semibold text-slate-800">
                Loại gói thầu <span className="text-rose-500">*</span>
              </label>
              <div className="grid grid-cols-3 gap-2">
                {PACKAGE_TYPE_OPTIONS.map((opt) => {
                  const Icon = opt.icon;
                  const isSelected = formData.type === opt.value;
                  return (
                    <button
                      key={opt.value}
                      type="button"
                      onClick={() => handleInputChange('type', opt.value)}
                      className={`py-2 px-2 rounded-xl border text-center transition flex flex-col items-center space-y-1 ${
                        isSelected
                          ? `${opt.color} border-current font-bold shadow-xs ring-1 ring-current`
                          : 'border-slate-200 text-slate-600 hover:bg-slate-50'
                      }`}
                    >
                      <Icon className="w-4 h-4" />
                      <span className="text-[11px]">{opt.label}</span>
                    </button>
                  );
                })}
              </div>
            </div>
          </div>

          {/* Grid 2 cột: Ngân sách & Hạn nộp */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {/* Dự toán ngân sách */}
            <div className="space-y-1.5">
              <label className="font-semibold text-slate-800">
                Dự toán ngân sách (VNĐ) <span className="text-rose-500">*</span>
              </label>
              <div className="relative">
                <input
                  type="number"
                  min="0"
                  step="any"
                  value={formData.budget}
                  onChange={(e) => handleInputChange('budget', e.target.value)}
                  placeholder="VD: 1500000000"
                  className={`w-full pl-3.5 pr-8 py-2.5 bg-white border rounded-xl text-xs font-mono font-bold focus:ring-2 focus:ring-sky-500/20 focus:border-sky-500 transition outline-none ${
                    errors.budget ? 'border-rose-400 bg-rose-50/30' : 'border-slate-200'
                  }`}
                />
                <span className="absolute right-3 top-2.5 text-slate-400 font-bold">₫</span>
              </div>
              {/* Preview tiền tệ VNĐ */}
              <div className="flex items-center justify-between text-[11px]">
                <span className="text-slate-400">Quy đổi hiển thị:</span>
                <span className="font-bold text-slate-900 font-mono">
                  {formData.budget ? formatVND(Number(formData.budget)) : '0 ₫'}
                </span>
              </div>
              {errors.budget && <p className="text-rose-500 text-[11px]">{errors.budget}</p>}
            </div>

            {/* Thời hạn nộp thầu */}
            <div className="space-y-1.5">
              <label className="font-semibold text-slate-800">
                Hạn chót nộp hồ sơ <span className="text-rose-500">*</span>
              </label>
              <input
                type="datetime-local"
                min={minDateTime}
                value={formData.deadline}
                onChange={(e) => handleInputChange('deadline', e.target.value)}
                className={`w-full px-3.5 py-2.5 bg-white border rounded-xl text-xs focus:ring-2 focus:ring-sky-500/20 focus:border-sky-500 transition outline-none ${
                  errors.deadline ? 'border-rose-400 bg-rose-50/30' : 'border-slate-200'
                }`}
              />
              {/* Nút chọn nhanh hạn nộp */}
              <div className="flex items-center space-x-1.5 pt-0.5">
                <span className="text-[10px] text-slate-400">Chọn nhanh:</span>
                <button
                  type="button"
                  onClick={() => handleShortcutDeadline(7)}
                  className="px-2 py-0.5 bg-slate-100 hover:bg-slate-200 text-slate-600 rounded text-[10px] font-medium transition"
                >
                  +7 ngày
                </button>
                <button
                  type="button"
                  onClick={() => handleShortcutDeadline(15)}
                  className="px-2 py-0.5 bg-slate-100 hover:bg-slate-200 text-slate-600 rounded text-[10px] font-medium transition"
                >
                  +15 ngày
                </button>
                <button
                  type="button"
                  onClick={() => handleShortcutDeadline(30)}
                  className="px-2 py-0.5 bg-slate-100 hover:bg-slate-200 text-slate-600 rounded text-[10px] font-medium transition"
                >
                  +30 ngày
                </button>
              </div>
              {errors.deadline && <p className="text-rose-500 text-[11px]">{errors.deadline}</p>}
            </div>
          </div>

          {/* Phạm vi công việc & Mô tả chi tiết */}
          <div className="space-y-1.5">
            <div className="flex items-center justify-between">
              <label className="font-semibold text-slate-800">
                Phạm vi công việc & Mô tả chi tiết
              </label>
              <span className="text-[11px] text-slate-400 font-mono">
                {formData.description.length}/2000
              </span>
            </div>
            <textarea
              rows={3}
              maxLength={2000}
              value={formData.description}
              onChange={(e) => handleInputChange('description', e.target.value)}
              placeholder="Mô tả mục tiêu, yêu cầu kỹ thuật cơ bản, tiêu chuẩn bàn giao nghiệm thu..."
              className="w-full px-3.5 py-2.5 bg-white border border-slate-200 rounded-xl text-xs focus:ring-2 focus:ring-sky-500/20 focus:border-sky-500 transition outline-none resize-none leading-relaxed"
            />
          </div>

          {/* Đính kèm tài liệu HSMT ban đầu */}
          <div className="space-y-2 pt-1 border-t border-slate-100">
            <div className="flex items-center justify-between">
              <label className="font-semibold text-slate-800 flex items-center space-x-1.5">
                <FileText className="w-3.5 h-3.5 text-slate-500" />
                <span>Tài liệu HSMT đính kèm ban đầu</span>
                <span className="text-slate-400 font-normal">(Tùy chọn)</span>
              </label>
              <label className="cursor-pointer inline-flex items-center space-x-1 px-2.5 py-1 bg-sky-50 hover:bg-sky-100 text-sky-700 rounded-lg text-xs font-semibold transition">
                <Plus className="w-3.5 h-3.5" />
                <span>Thêm tệp</span>
                <input
                  type="file"
                  multiple
                  onChange={handleFileChange}
                  accept=".pdf,.doc,.docx,.xls,.xlsx,.zip,.rar"
                  className="hidden"
                />
              </label>
            </div>

            {files.length > 0 ? (
              <div className="space-y-1.5 max-h-36 overflow-y-auto">
                {files.map((file, idx) => (
                  <div
                    key={idx}
                    className="p-2 bg-slate-50 border border-slate-200/80 rounded-xl flex items-center justify-between text-xs"
                  >
                    <div className="flex items-center space-x-2 truncate pr-2">
                      <FileText className="w-3.5 h-3.5 text-slate-400 flex-shrink-0" />
                      <span className="font-medium text-slate-700 truncate" title={file.name}>
                        {file.name}
                      </span>
                      <span className="text-[10px] text-slate-400 font-mono flex-shrink-0">
                        ({(file.size / 1024 / 1024).toFixed(2)} MB)
                      </span>
                    </div>
                    <button
                      type="button"
                      onClick={() => handleRemoveFile(idx)}
                      className="text-slate-400 hover:text-rose-600 p-1 rounded transition"
                      title="Xóa tệp"
                    >
                      <Trash2 className="w-3.5 h-3.5" />
                    </button>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-[11px] text-slate-400 italic">
                Bạn có thể đính kèm file HSMT (PDF, DOCX, XLSX, ZIP) ngay bây giờ hoặc tải lên sau ở trang chi tiết.
              </p>
            )}
          </div>

          {/* Footer nút hành động */}
          <div className="pt-3 border-t border-slate-100 flex items-center justify-end space-x-3">
            <button
              type="button"
              onClick={onClose}
              disabled={loading}
              className="px-4 py-2 text-xs font-semibold text-slate-600 hover:bg-slate-100 rounded-xl transition border border-slate-200 disabled:opacity-50"
            >
              Hủy bỏ
            </button>
            <button
              type="submit"
              disabled={loading}
              className="px-5 py-2 text-xs font-bold text-white bg-slate-900 hover:bg-slate-800 rounded-xl shadow-xs transition disabled:opacity-50 flex items-center space-x-2"
            >
              {loading && (
                <svg className="animate-spin -ml-1 mr-1.5 h-3.5 w-3.5 text-white" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
              )}
              <span>Tạo gói thầu</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default CreatePackageModal;
