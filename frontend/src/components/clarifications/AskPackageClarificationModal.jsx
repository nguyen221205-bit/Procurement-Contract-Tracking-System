import React, { useState } from 'react';
import { HelpCircle, X, Send, AlertCircle, FileText, Paperclip, Upload } from 'lucide-react';
import toast from 'react-hot-toast';
import { clarificationApi } from '../../api/clarificationApi';

export const AskPackageClarificationModal = ({ isOpen, onClose, packageId, packageCode, onSuccess }) => {
  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [attachmentUrl, setAttachmentUrl] = useState('');
  const [loading, setLoading] = useState(false);
  const [uploading, setUploading] = useState(false);

  if (!isOpen) return null;

  const handleFileUpload = async (e) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploading(true);
    try {
      const res = await clarificationApi.uploadAttachment(file);
      if (res && res.data) {
        setAttachmentUrl(res.data);
        toast.success(`Đã tải lên tệp ${file.name} thành công!`);
      }
    } catch (err) {
      console.error(err);
      toast.error('Không thể tải lên tệp đính kèm');
    } finally {
      setUploading(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!title.trim() || !content.trim()) {
      toast.error('Vui lòng nhập đầy đủ tiêu đề và nội dung câu hỏi.');
      return;
    }

    setLoading(true);
    try {
      const res = await clarificationApi.createPackageClarification(packageId, {
        title: title.trim(),
        questionContent: content.trim(),
        questionAttachmentUrl: attachmentUrl.trim() || null,
      });

      if (res && res.success) {
        toast.success(res.message || 'Gửi câu hỏi làm rõ HSMT thành công!');
        setTitle('');
        setContent('');
        setAttachmentUrl('');
        onSuccess?.();
        onClose();
      } else {
        toast.error(res?.message || 'Không thể gửi câu hỏi làm rõ.');
      }
    } catch (err) {
      toast.error(err?.response?.data?.message || 'Có lỗi xảy ra khi gửi yêu cầu làm rõ.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-xs animate-fade-in">
      <div className="relative w-full max-w-xl bg-white border border-slate-200 rounded-2xl shadow-xl overflow-hidden flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-slate-100 bg-slate-50/60">
          <div className="flex items-center gap-3">
            <div className="p-2.5 bg-sky-50 border border-sky-200 text-sky-600 rounded-xl">
              <HelpCircle className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">Gửi Yêu Cầu Làm Rõ HSMT</h3>
              <p className="text-xs text-slate-500 mt-0.5">
                Gói thầu: <span className="font-mono font-bold text-sky-700">{packageCode}</span> • Công khai cho các bên
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-1.5 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg transition-colors cursor-pointer"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Form Body */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4 overflow-y-auto">
          {/* Banner hướng dẫn */}
          <div className="p-3.5 bg-sky-50/70 border border-sky-200/80 rounded-xl flex items-start gap-2.5">
            <AlertCircle className="w-4 h-4 text-sky-600 shrink-0 mt-0.5" />
            <p className="text-xs text-sky-900 leading-relaxed">
              Theo quy định của Luật Đấu thầu, nội dung thắc mắc và văn bản phản hồi làm rõ HSMT sẽ được công khai minh bạch để toàn bộ nhà thầu cùng nắm rõ thông tin.
            </p>
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
              Tiêu đề thắc mắc <span className="text-rose-500">*</span>
            </label>
            <input
              type="text"
              required
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="VD: Làm rõ quy định về bảo lãnh dự thầu tại Mục 2.3..."
              className="w-full px-3.5 py-2.5 bg-white border border-slate-300 rounded-xl text-xs text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-500 transition-all"
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
              Nội dung câu hỏi / yêu cầu giải thích <span className="text-rose-500">*</span>
            </label>
            <textarea
              required
              rows={4}
              value={content}
              onChange={(e) => setContent(e.target.value)}
              placeholder="Trình bày chi tiết các điểm chưa rõ trong Hồ sơ mời thầu, tiêu chuẩn kỹ thuật hoặc điều kiện tiên quyết..."
              className="w-full px-3.5 py-2.5 bg-white border border-slate-300 rounded-xl text-xs text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-500 transition-all resize-none"
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5 flex items-center gap-1.5">
              <Paperclip className="w-3.5 h-3.5 text-slate-400" />
              <span>Đường dẫn tài liệu tham chiếu (Tùy chọn)</span>
            </label>
            <div className="flex gap-2">
              <input
                type="text"
                value={attachmentUrl}
                onChange={(e) => setAttachmentUrl(e.target.value)}
                placeholder="Đường dẫn hoặc tải file đính kèm bên cạnh..."
                className="flex-1 px-3.5 py-2.5 bg-white border border-slate-300 rounded-xl text-xs text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-500 transition-all"
              />
              <label className="inline-flex items-center gap-1.5 px-3.5 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 border border-slate-300 rounded-xl text-xs font-semibold cursor-pointer transition-colors shrink-0 shadow-2xs">
                <Upload className="w-3.5 h-3.5 text-sky-600" />
                <span>{uploading ? 'Đang tải...' : 'Chọn tệp'}</span>
                <input
                  type="file"
                  accept=".pdf,.doc,.docx,.xls,.xlsx,.png,.jpg,.jpeg,.zip,.rar"
                  className="hidden"
                  disabled={uploading}
                  onChange={handleFileUpload}
                />
              </label>
            </div>
            {attachmentUrl && (
              <p className="text-[11px] text-emerald-600 font-medium mt-1">
                ✓ Đã đính kèm tệp: {attachmentUrl}
              </p>
            )}
          </div>

          {/* Footer Actions */}
          <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2.5 rounded-xl text-xs font-semibold text-slate-700 bg-slate-100 hover:bg-slate-200 transition-colors cursor-pointer"
            >
              Hủy bỏ
            </button>
            <button
              type="submit"
              disabled={loading}
              className="px-5 py-2.5 rounded-xl text-xs font-bold bg-sky-600 text-white shadow-xs hover:bg-sky-700 transition-all disabled:opacity-50 flex items-center gap-1.5 cursor-pointer"
            >
              <Send className="w-3.5 h-3.5" />
              <span>{loading ? 'Đang gửi...' : 'Gửi câu hỏi làm rõ'}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default AskPackageClarificationModal;
