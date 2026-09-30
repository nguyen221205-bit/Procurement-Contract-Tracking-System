import React, { useState } from 'react';
import toast from 'react-hot-toast';
import {
  Upload,
  FileText,
  X,
  CheckCircle,
  AlertCircle
} from 'lucide-react';
import { contractApi } from '../../api/contractApi';

export const UploadScannedModal = ({
  isOpen,
  onClose,
  contractId,
  contractNumber,
  onSuccess,
}) => {
  const [file, setFile] = useState(null);
  const [submitting, setSubmitting] = useState(false);

  if (!isOpen) return null;

  const handleFileChange = (e) => {
    const selected = e.target.files?.[0];
    if (selected) {
      if (selected.size > 20 * 1024 * 1024) {
        toast.error('Dung lượng file không được vượt quá 20MB.');
        return;
      }
      setFile(selected);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!file) {
      toast.error('Vui lòng chọn file bản scan hợp đồng đã ký.');
      return;
    }

    try {
      setSubmitting(true);
      await contractApi.uploadScannedFile(contractId, file);
      toast.success('Tải lên bản scan hợp đồng thành công!');
      onClose();
      if (onSuccess) onSuccess();
    } catch (err) {
      toast.error(err.message || 'Không thể tải lên bản scan hợp đồng.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-xl border border-slate-200 w-full max-w-md overflow-hidden animate-in fade-in zoom-in duration-200">
        {/* Header */}
        <div className="bg-gradient-to-r from-blue-600 to-indigo-700 px-6 py-4 text-white flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <div className="p-2 bg-white/20 rounded-xl backdrop-blur-xs">
              <Upload className="w-5 h-5 text-white" />
            </div>
            <div>
              <h3 className="text-base font-bold">Đính Kèm Bản Scan Hợp Đồng</h3>
              <p className="text-xs text-blue-100">Số hiệu: {contractNumber}</p>
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
          <div className="space-y-1">
            <label className="text-xs font-bold text-slate-700 block">
              Chọn tài liệu PDF hoặc bản scan đã ký đóng dấu <span className="text-rose-500">*</span>
            </label>
            <div className="border-2 border-dashed border-slate-200 hover:border-blue-400 rounded-2xl p-6 text-center transition-colors">
              <input
                type="file"
                id="contract-scan-upload"
                accept=".pdf,.png,.jpg,.jpeg"
                onChange={handleFileChange}
                className="hidden"
              />
              <label
                htmlFor="contract-scan-upload"
                className="cursor-pointer flex flex-col items-center justify-center space-y-2"
              >
                <div className="w-12 h-12 rounded-full bg-blue-50 text-blue-600 flex items-center justify-center">
                  <FileText className="w-6 h-6" />
                </div>
                {file ? (
                  <div className="space-y-1">
                    <p className="text-xs font-bold text-slate-900 truncate max-w-xs">{file.name}</p>
                    <p className="text-[11px] text-emerald-600 font-semibold">
                      {(file.size / (1024 * 1024)).toFixed(2)} MB - Đã chọn
                    </p>
                  </div>
                ) : (
                  <div className="space-y-1">
                    <p className="text-xs font-bold text-slate-700">Nhấp để tải lên hoặc kéo thả vào đây</p>
                    <p className="text-[11px] text-slate-400">Hỗ trợ định dạng: PDF, JPG, PNG (Tối đa 20MB)</p>
                  </div>
                )}
              </label>
            </div>
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
              disabled={submitting || !file}
              className="inline-flex items-center gap-1.5 px-5 py-2 text-xs font-bold text-white bg-blue-600 hover:bg-blue-700 disabled:bg-slate-300 rounded-xl shadow-xs transition-transform hover:scale-[1.02] cursor-pointer"
            >
              {submitting ? (
                <>
                  <span className="w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                  <span>Đang tải lên...</span>
                </>
              ) : (
                <>
                  <Upload className="w-4 h-4" />
                  <span>Xác nhận Tải lên</span>
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default UploadScannedModal;
