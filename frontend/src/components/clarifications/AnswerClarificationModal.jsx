import React, { useState } from 'react';
import { MessageSquare, X, Send, Clock, FileText, Paperclip, Upload } from 'lucide-react';
import toast from 'react-hot-toast';
import { formatDateTime } from '../../utils/formatters';
import { clarificationApi } from '../../api/clarificationApi';

export const AnswerClarificationModal = ({ isOpen, onClose, clarification, onSuccess }) => {
  const [answerContent, setAnswerContent] = useState('');
  const [attachmentUrl, setAttachmentUrl] = useState('');
  const [loading, setLoading] = useState(false);
  const [uploading, setUploading] = useState(false);

  if (!isOpen || !clarification) return null;

  const isSubmissionClarification =
    clarification.type === 'SubmissionClarification' || clarification.type === 1;

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
    if (!answerContent.trim()) {
      toast.error('Vui lòng nhập nội dung phản hồi / giải trình.');
      return;
    }

    setLoading(true);
    try {
      const res = await clarificationApi.answerClarification(clarification.id, {
        answerContent: answerContent.trim(),
        answerAttachmentUrl: attachmentUrl.trim() || null,
      });

      if (res && res.success) {
        toast.success(res.message || 'Gửi phản hồi / giải trình thành công!');
        setAnswerContent('');
        setAttachmentUrl('');
        onSuccess?.();
        onClose();
      } else {
        toast.error(res?.message || 'Không thể gửi phản hồi.');
      }
    } catch (err) {
      toast.error(err?.response?.data?.message || 'Có lỗi xảy ra khi gửi phản hồi.');
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
            <div
              className={`p-2.5 rounded-xl border ${
                isSubmissionClarification
                  ? 'bg-amber-50 border-amber-200 text-amber-600'
                  : 'bg-emerald-50 border-emerald-200 text-emerald-600'
              }`}
            >
              <MessageSquare className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">
                {isSubmissionClarification ? 'Nộp Văn Bản Giải Trình HSDT' : 'Phản Hồi Làm Rõ HSMT'}
              </h3>
              <p className="text-xs text-slate-500 mt-0.5">
                {isSubmissionClarification ? 'Gửi trực tiếp tới Tổ chuyên gia chấm thầu' : 'Công khai cho các nhà thầu'}
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
          {/* Thông tin câu hỏi gốc */}
          <div className="p-4 bg-slate-50 rounded-xl border border-slate-200/80 space-y-2">
            <div className="flex items-center justify-between text-xs text-slate-500">
              <span>
                Người yêu cầu: <strong className="text-slate-800">{clarification.askedByUserName}</strong>
              </span>
              <span className="font-medium">{formatDateTime(clarification.askedAt)}</span>
            </div>
            <p className="text-xs font-bold text-slate-900">{clarification.title}</p>
            <p className="text-xs text-slate-700 leading-relaxed bg-white p-3 rounded-lg border border-slate-200 whitespace-pre-line">
              {clarification.questionContent}
            </p>
            {clarification.deadline && (
              <p className="text-xs text-rose-600 font-semibold flex items-center gap-1.5 pt-1">
                <Clock className="w-3.5 h-3.5" /> Hạn chót giải trình: <strong>{formatDateTime(clarification.deadline)}</strong>
              </p>
            )}
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
              Nội dung {isSubmissionClarification ? 'giải trình bổ sung' : 'phản hồi làm rõ'} <span className="text-rose-500">*</span>
            </label>
            <textarea
              required
              rows={4}
              value={answerContent}
              onChange={(e) => setAnswerContent(e.target.value)}
              placeholder={
                isSubmissionClarification
                  ? 'Trình bày chi tiết nội dung giải trình, căn cứ chứng minh hoặc giải thích kỹ thuật theo yêu cầu của Tổ chuyên gia...'
                  : 'Nêu rõ quan điểm của Bên mời thầu / Tổ chuyên gia để làm sáng tỏ các điều khoản trong HSMT...'
              }
              className="w-full px-3.5 py-2.5 bg-white border border-slate-300 rounded-xl text-xs text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-emerald-500/30 focus:border-emerald-500 transition-all resize-none"
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5 flex items-center gap-1.5">
              <Paperclip className="w-3.5 h-3.5 text-slate-400" />
              <span>Tệp tài liệu đính kèm minh chứng (Tùy chọn)</span>
            </label>
            <div className="flex gap-2">
              <input
                type="text"
                value={attachmentUrl}
                onChange={(e) => setAttachmentUrl(e.target.value)}
                placeholder="Đường dẫn hoặc tải file đính kèm bên cạnh..."
                className="flex-1 px-3.5 py-2.5 bg-white border border-slate-300 rounded-xl text-xs text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-emerald-500/30 focus:border-emerald-500 transition-all"
              />
              <label className="inline-flex items-center gap-1.5 px-3.5 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 border border-slate-300 rounded-xl text-xs font-semibold cursor-pointer transition-colors shrink-0 shadow-2xs">
                <Upload className="w-3.5 h-3.5 text-emerald-600" />
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
              className={`px-5 py-2.5 rounded-xl text-xs font-bold text-white shadow-xs transition-all disabled:opacity-50 flex items-center gap-1.5 cursor-pointer ${
                isSubmissionClarification
                  ? 'bg-amber-600 hover:bg-amber-700'
                  : 'bg-emerald-600 hover:bg-emerald-700'
              }`}
            >
              <Send className="w-3.5 h-3.5" />
              <span>{loading ? 'Đang gửi...' : 'Gửi phản hồi / Giải trình'}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default AnswerClarificationModal;
