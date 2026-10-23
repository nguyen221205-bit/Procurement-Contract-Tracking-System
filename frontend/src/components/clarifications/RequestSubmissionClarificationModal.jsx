import React, { useState } from 'react';
import { Clock, X, Send, AlertTriangle, FileText, Building, Paperclip, Upload } from 'lucide-react';
import toast from 'react-hot-toast';
import { clarificationApi } from '../../api/clarificationApi';

export const RequestSubmissionClarificationModal = ({
  isOpen,
  onClose,
  packageId,
  packageCode,
  submissions = [],
  onSuccess,
}) => {
  const [submissionId, setSubmissionId] = useState('');
  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [attachmentUrl, setAttachmentUrl] = useState('');
  const [deadline, setDeadline] = useState('');
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
    if (!submissionId || !title.trim() || !content.trim() || !deadline) {
      toast.error('Vui lòng điền đầy đủ các trường thông tin bắt buộc.');
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
        setDeadline('');
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
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-xs animate-fade-in">
      <div className="relative w-full max-w-xl bg-white border border-slate-200 rounded-2xl shadow-xl overflow-hidden flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-slate-100 bg-slate-50/60">
          <div className="flex items-center gap-3">
            <div className="p-2.5 bg-amber-50 border border-amber-200 text-amber-600 rounded-xl">
              <Clock className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">Yêu Cầu Làm Rõ / Giải Trình HSDT</h3>
              <p className="text-xs text-slate-500 mt-0.5">
                Gói thầu: <span className="font-mono font-bold text-amber-700">{packageCode}</span> • Bảo mật Tổ chuyên gia & Nhà thầu
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
          {/* Banner bảo mật */}
          <div className="p-3.5 bg-amber-50/70 border border-amber-200/80 rounded-xl flex items-start gap-2.5">
            <AlertTriangle className="w-4 h-4 text-amber-600 shrink-0 mt-0.5" />
            <p className="text-xs text-amber-900 leading-relaxed">
              Yêu cầu giải trình này sẽ được gửi trực tiếp và bảo mật riêng tư đến Nhà thầu được chọn. Chỉ Nhà thầu đó và Tổ chuyên gia/Bên mời thầu mới có quyền xem nội dung này.
            </p>
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5 flex items-center gap-1.5">
              <Building className="w-3.5 h-3.5 text-slate-400" />
              <span>Chọn Hồ sơ dự thầu cần làm rõ <span className="text-rose-500">*</span></span>
            </label>
            <select
              value={submissionId}
              onChange={(e) => setSubmissionId(e.target.value)}
              required
              className="w-full px-3.5 py-2.5 bg-white border border-slate-300 rounded-xl text-xs text-slate-900 focus:outline-none focus:ring-2 focus:ring-amber-500/30 focus:border-amber-500 transition-all"
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
            <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
              Tiêu đề yêu cầu giải trình <span className="text-rose-500">*</span>
            </label>
            <input
              type="text"
              required
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="VD: Yêu cầu giải trình nội dung bảo hành và nhân sự chủ chốt..."
              className="w-full px-3.5 py-2.5 bg-white border border-slate-300 rounded-xl text-xs text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-amber-500/30 focus:border-amber-500 transition-all"
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5 flex items-center gap-1.5">
              <Clock className="w-3.5 h-3.5 text-amber-600" />
              <span>Hạn chót nhà thầu phải nộp giải trình <span className="text-rose-500">*</span></span>
            </label>
            <input
              type="datetime-local"
              required
              value={deadline}
              onChange={(e) => setDeadline(e.target.value)}
              className="w-full px-3.5 py-2 bg-white border border-slate-300 rounded-xl text-xs text-slate-900 focus:outline-none focus:ring-2 focus:ring-amber-500/30 focus:border-amber-500 transition-all"
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
              Nội dung yêu cầu giải trình chi tiết <span className="text-rose-500">*</span>
            </label>
            <textarea
              required
              rows={4}
              value={content}
              onChange={(e) => setContent(e.target.value)}
              placeholder="Chỉ rõ các sai khác, tài liệu còn thiếu hoặc điểm cần làm rõ trong hồ sơ kỹ thuật/tài chính..."
              className="w-full px-3.5 py-2.5 bg-white border border-slate-300 rounded-xl text-xs text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-amber-500/30 focus:border-amber-500 transition-all resize-none"
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5 flex items-center gap-1.5">
              <Paperclip className="w-3.5 h-3.5 text-slate-400" />
              <span>Tệp đính kèm văn bản yêu cầu (Tùy chọn)</span>
            </label>
            <div className="flex gap-2">
              <input
                type="text"
                value={attachmentUrl}
                onChange={(e) => setAttachmentUrl(e.target.value)}
                placeholder="Đường dẫn hoặc tải file đính kèm bên cạnh..."
                className="flex-1 px-3.5 py-2.5 bg-white border border-slate-300 rounded-xl text-xs text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-amber-500/30 focus:border-amber-500 transition-all"
              />
              <label className="inline-flex items-center gap-1.5 px-3.5 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 border border-slate-300 rounded-xl text-xs font-semibold cursor-pointer transition-colors shrink-0 shadow-2xs">
                <Upload className="w-3.5 h-3.5 text-amber-600" />
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
              className="px-5 py-2.5 rounded-xl text-xs font-bold bg-amber-600 hover:bg-amber-700 text-white shadow-xs transition-all disabled:opacity-50 flex items-center gap-1.5 cursor-pointer"
            >
              <Send className="w-3.5 h-3.5" />
              <span>{loading ? 'Đang gửi...' : 'Gửi yêu cầu giải trình'}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default RequestSubmissionClarificationModal;
