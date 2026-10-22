import React, { useState } from 'react';
import { HelpCircle, X, Send, AlertTriangle, Clock, Paperclip, Building } from 'lucide-react';
import toast from 'react-hot-toast';
import { clarificationApi } from '../../api/clarificationApi';

export const RequestSubmissionClarificationModal = ({
  isOpen,
  onClose,
  packageId,
  packageCode,
  submissions = [],
  initialSubmission = null,
  onSuccess,
}) => {
  const [submissionId, setSubmissionId] = useState(initialSubmission?.id || '');
  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [attachmentUrl, setAttachmentUrl] = useState('');
  // Mặc định thời hạn giải trình là 48 giờ kể từ hiện tại
  const [deadline, setDeadline] = useState(() => {
    const d = new Date();
    d.setHours(d.getHours() + 48);
    return d.toISOString().slice(0, 16);
  });
  const [loading, setLoading] = useState(false);

  if (!isOpen) return null;

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!submissionId) {
      toast.error('Vui lòng chọn hồ sơ dự thầu cần yêu cầu làm rõ.');
      return;
    }
    if (!title.trim() || !content.trim()) {
      toast.error('Vui lòng nhập đầy đủ tiêu đề và nội dung yêu cầu giải trình.');
      return;
    }
    if (!deadline || new Date(deadline) <= new Date()) {
      toast.error('Thời hạn giải trình phải ở một mốc thời gian trong tương lai.');
      return;
    }

    setLoading(true);
    try {
      const res = await clarificationApi.createSubmissionClarification(packageId, {
        bidSubmissionId: Number(submissionId),
        title: title.trim(),
        questionContent: content.trim(),
        questionAttachmentUrl: attachmentUrl.trim() || null,
        deadline: new Date(deadline).toISOString(),
      });

      if (res && res.success) {
        toast.success(res.message || 'Đã gửi yêu cầu giải trình tới Nhà thầu!');
        setTitle('');
        setContent('');
        setAttachmentUrl('');
        onSuccess?.();
        onClose();
      } else {
        toast.error(res?.message || 'Không thể gửi yêu cầu giải trình.');
      }
    } catch (err) {
      toast.error(err?.response?.data?.message || 'Có lỗi xảy ra khi gửi yêu cầu làm rõ HSDT.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-sm animate-fade-in">
      <div className="relative w-full max-w-xl bg-slate-900 border border-slate-800 rounded-2xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
        <div className="flex items-center justify-between p-6 border-b border-slate-800 bg-slate-900/60">
          <div className="flex items-center gap-3">
            <div className="p-2.5 bg-amber-500/10 border border-amber-500/20 text-amber-400 rounded-xl">
              <Clock className="w-6 h-6" />
            </div>
            <div>
              <h3 className="text-lg font-bold text-slate-100">Yêu Cầu Làm Rõ / Giải Trình HSDT</h3>
              <p className="text-xs text-slate-400">
                Gói thầu: <span className="text-amber-400 font-semibold">{packageCode}</span> • Bảo mật Tổ chuyên gia & Nhà thầu
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
          <div className="p-3.5 bg-amber-500/10 border border-amber-500/20 rounded-xl flex items-start gap-3">
            <AlertTriangle className="w-5 h-5 text-amber-400 shrink-0 mt-0.5" />
            <p className="text-xs text-amber-300 leading-relaxed">
              Yêu cầu giải trình này sẽ được gửi trực tiếp và bảo mật riêng tư đến Nhà thầu được chọn. Chỉ Nhà thầu đó và Tổ chuyên gia/Bên mời thầu mới có quyền xem nội dung này.
            </p>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-1.5 flex items-center gap-1.5">
              <Building className="w-3.5 h-3.5 text-slate-400" /> Chọn Hồ sơ dự thầu cần làm rõ <span className="text-rose-400">*</span>
            </label>
            <select
              value={submissionId}
              onChange={(e) => setSubmissionId(e.target.value)}
              required
              className="w-full px-3.5 py-2.5 bg-slate-950 border border-slate-700 rounded-xl text-sm text-slate-100 focus:outline-none focus:ring-2 focus:ring-amber-500/50 transition-all"
            >
              <option value="">-- Chọn hồ sơ của nhà thầu --</option>
              {submissions.map((sub) => (
                <option key={sub.id} value={sub.id}>
                  {sub.contractor?.companyName || `Nhà thầu #${sub.contractorId}`} (Mã hồ sơ: #{sub.id})
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-1.5">
              Tiêu đề yêu cầu giải trình <span className="text-rose-400">*</span>
            </label>
            <input
              type="text"
              required
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="VD: Yêu cầu giải trình nội dung bảo hành và nhân sự chủ chốt..."
              className="w-full px-3.5 py-2.5 bg-slate-950 border border-slate-700 rounded-xl text-sm text-slate-100 placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-amber-500/50 transition-all"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-1.5 flex items-center gap-1.5">
              <Clock className="w-3.5 h-3.5 text-amber-400" /> Hạn chót nhà thầu phải nộp giải trình <span className="text-rose-400">*</span>
            </label>
            <input
              type="datetime-local"
              required
              value={deadline}
              onChange={(e) => setDeadline(e.target.value)}
              className="w-full px-3.5 py-2 bg-slate-950 border border-slate-700 rounded-xl text-sm text-slate-100 focus:outline-none focus:ring-2 focus:ring-amber-500/50 transition-all"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-1.5">
              Nội dung yêu cầu giải trình chi tiết <span className="text-rose-400">*</span>
            </label>
            <textarea
              required
              rows={4}
              value={content}
              onChange={(e) => setContent(e.target.value)}
              placeholder="Chỉ rõ các sai khác, tài liệu còn thiếu hoặc điểm cần làm rõ trong hồ sơ kỹ thuật/tài chính..."
              className="w-full px-3.5 py-2.5 bg-slate-950 border border-slate-700 rounded-xl text-sm text-slate-100 placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-amber-500/50 transition-all resize-none"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-1.5 flex items-center gap-1.5">
              <Paperclip className="w-3.5 h-3.5 text-slate-400" /> Tệp đính kèm văn bản yêu cầu (Tùy chọn)
            </label>
            <input
              type="text"
              value={attachmentUrl}
              onChange={(e) => setAttachmentUrl(e.target.value)}
              placeholder="https://... hoặc tên tệp đính kèm"
              className="w-full px-3.5 py-2 bg-slate-950 border border-slate-700 rounded-xl text-sm text-slate-100 placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-amber-500/50 transition-all"
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
              className="px-5 py-2.5 rounded-xl text-sm font-semibold bg-amber-600 text-slate-950 shadow-lg shadow-amber-600/20 hover:bg-amber-500 transition-all disabled:opacity-50 flex items-center gap-2 font-bold"
            >
              <Send className="w-4 h-4" /> {loading ? 'Đang gửi...' : 'Gửi yêu cầu giải trình'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default RequestSubmissionClarificationModal;
