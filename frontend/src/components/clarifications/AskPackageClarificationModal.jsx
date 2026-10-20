import React, { useState } from 'react';
import { HelpCircle, X, Send, AlertCircle, FileText, Paperclip } from 'lucide-react';
import toast from 'react-hot-toast';
import { clarificationApi } from '../../api/clarificationApi';

export const AskPackageClarificationModal = ({ isOpen, onClose, packageId, packageCode, onSuccess }) => {
  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [attachmentUrl, setAttachmentUrl] = useState('');
  const [loading, setLoading] = useState(false);

  if (!isOpen) return null;

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
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-sm animate-fade-in">
      <div className="relative w-full max-w-xl bg-slate-900 border border-slate-800 rounded-2xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
        <div className="flex items-center justify-between p-6 border-b border-slate-800 bg-slate-900/60">
          <div className="flex items-center gap-3">
            <div className="p-2.5 bg-sky-500/10 border border-sky-500/20 text-sky-400 rounded-xl">
              <HelpCircle className="w-6 h-6" />
            </div>
            <div>
              <h3 className="text-lg font-bold text-slate-100">Gửi Yêu Cầu Làm Rõ HSMT</h3>
              <p className="text-xs text-slate-400">
                Gói thầu: <span className="text-sky-400 font-semibold">{packageCode}</span> • Công khai cho các bên
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-2 text-slate-400 hover:text-slate-200 hover:bg-slate-800 rounded-lg transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-4 overflow-y-auto">
          <div className="p-3.5 bg-sky-500/10 border border-sky-500/20 rounded-xl flex items-start gap-3">
            <AlertCircle className="w-5 h-5 text-sky-400 shrink-0 mt-0.5" />
            <p className="text-xs text-sky-300 leading-relaxed">
              Theo quy định của Luật Đấu thầu, nội dung thắc mắc và phản hồi làm rõ HSMT sẽ được công khai minh bạch để toàn bộ nhà thầu cùng nắm rõ thông tin.
            </p>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-1.5">
              Tiêu đề thắc mắc <span className="text-rose-400">*</span>
            </label>
            <input
              type="text"
              required
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="VD: Làm rõ quy định về bảo lãnh dự thầu tại Mục 2.3..."
              className="w-full px-3.5 py-2.5 bg-slate-950 border border-slate-700 rounded-xl text-sm text-slate-100 placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-sky-500/50 transition-all"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-1.5">
              Nội dung câu hỏi / yêu cầu giải thích <span className="text-rose-400">*</span>
            </label>
            <textarea
              required
              rows={5}
              value={content}
              onChange={(e) => setContent(e.target.value)}
              placeholder="Trình bày chi tiết các điểm chưa rõ trong Hồ sơ mời thầu, tiêu chuẩn kỹ thuật hoặc điều kiện tiên quyết..."
              className="w-full px-3.5 py-2.5 bg-slate-950 border border-slate-700 rounded-xl text-sm text-slate-100 placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-sky-500/50 transition-all resize-none"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-1.5 flex items-center gap-1.5">
              <Paperclip className="w-3.5 h-3.5 text-slate-400" /> Đường dẫn tài liệu tham chiếu (Tùy chọn)
            </label>
            <input
              type="text"
              value={attachmentUrl}
              onChange={(e) => setAttachmentUrl(e.target.value)}
              placeholder="https://... hoặc tên tệp đính kèm làm rõ"
              className="w-full px-3.5 py-2 bg-slate-950 border border-slate-700 rounded-xl text-sm text-slate-100 placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-sky-500/50 transition-all"
            />
          </div>

          <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-800">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2.5 rounded-xl text-sm font-medium text-slate-300 hover:bg-slate-800 transition-colors"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={loading}
              className="px-5 py-2.5 rounded-xl text-sm font-semibold bg-sky-600 text-white shadow-lg shadow-sky-600/20 hover:bg-sky-500 transition-all disabled:opacity-50 flex items-center gap-2"
            >
              <Send className="w-4 h-4" /> {loading ? 'Đang gửi...' : 'Gửi câu hỏi làm rõ'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default AskPackageClarificationModal;
