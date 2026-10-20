import React, { useState } from 'react';
import { MessageSquare, X, Send, AlertCircle, Clock, Paperclip, FileCheck } from 'lucide-react';
import { formatDateTime } from '../../utils/formatters';
import toast from 'react-hot-toast';
import { clarificationApi } from '../../api/clarificationApi';

export const AnswerClarificationModal = ({ isOpen, onClose, clarification, onSuccess }) => {
  const [answerContent, setAnswerContent] = useState('');
  const [attachmentUrl, setAttachmentUrl] = useState('');
  const [loading, setLoading] = useState(false);

  if (!isOpen || !clarification) return null;

  const isSubmissionClarification = clarification.type === 'SubmissionClarification' || clarification.type === 1;

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
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-sm animate-fade-in">
      <div className="relative w-full max-w-xl bg-slate-900 border border-slate-800 rounded-2xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
        <div className="flex items-center justify-between p-6 border-b border-slate-800 bg-slate-900/60">
          <div className="flex items-center gap-3">
            <div className={`p-2.5 rounded-xl border ${
              isSubmissionClarification
                ? 'bg-amber-500/10 border-amber-500/20 text-amber-400'
                : 'bg-emerald-500/10 border-emerald-500/20 text-emerald-400'
            }`}>
              <MessageSquare className="w-6 h-6" />
            </div>
            <div>
              <h3 className="text-lg font-bold text-slate-100">
                {isSubmissionClarification ? 'Nộp Văn Bản Giải Trình HSDT' : 'Phản Hồi Làm Rõ HSMT'}
              </h3>
              <p className="text-xs text-slate-400">
                {isSubmissionClarification ? 'Gửi trực tiếp tới Tổ chuyên gia chấm thầu' : 'Công khai cho các nhà thầu'}
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
          {/* Thông tin câu hỏi gốc */}
          <div className="p-4 bg-slate-950/60 border border-slate-800 rounded-xl space-y-2">
            <div className="flex items-center justify-between text-xs text-slate-400">
              <span>Người yêu cầu: <strong className="text-slate-200">{clarification.askedByUserName}</strong></span>
              <span>{formatDateTime(clarification.askedAt)}</span>
            </div>
            <p className="text-sm font-semibold text-slate-100">{clarification.title}</p>
            <p className="text-xs text-slate-300 leading-relaxed bg-slate-900/60 p-3 rounded-lg border border-slate-800/60 whitespace-pre-line">
              {clarification.questionContent}
            </p>
            {clarification.deadline && (
              <p className="text-xs text-amber-400 flex items-center gap-1.5 pt-1">
                <Clock className="w-3.5 h-3.5" /> Hạn chót giải trình: <strong>{formatDateTime(clarification.deadline)}</strong>
              </p>
            )}
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-1.5">
              Nội dung {isSubmissionClarification ? 'giải trình bổ sung' : 'phản hồi làm rõ'} <span className="text-rose-400">*</span>
            </label>
            <textarea
              required
              rows={5}
              value={answerContent}
              onChange={(e) => setAnswerContent(e.target.value)}
              placeholder={
                isSubmissionClarification
                  ? 'Trình bày chi tiết nội dung giải trình, căn cứ chứng minh hoặc giải thích kỹ thuật theo yêu cầu của Tổ chuyên gia...'
                  : 'Nêu rõ quan điểm của Bên mời thầu / Tổ chuyên gia để làm sáng tỏ các điều khoản trong HSMT...'
              }
              className="w-full px-3.5 py-2.5 bg-slate-950 border border-slate-700 rounded-xl text-sm text-slate-100 placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/50 transition-all resize-none"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-1.5 flex items-center gap-1.5">
              <Paperclip className="w-3.5 h-3.5 text-slate-400" /> Tệp tài liệu đính kèm minh chứng (Tùy chọn)
            </label>
            <input
              type="text"
              value={attachmentUrl}
              onChange={(e) => setAttachmentUrl(e.target.value)}
              placeholder="https://... hoặc tên tệp đính kèm văn bản giải trình"
              className="w-full px-3.5 py-2 bg-slate-950 border border-slate-700 rounded-xl text-sm text-slate-100 placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/50 transition-all"
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
              className="px-5 py-2.5 rounded-xl text-sm font-semibold bg-emerald-600 text-white shadow-lg shadow-emerald-600/20 hover:bg-emerald-500 transition-all disabled:opacity-50 flex items-center gap-2"
            >
              <Send className="w-4 h-4" /> {loading ? 'Đang gửi...' : 'Gửi phản hồi / Giải trình'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default AnswerClarificationModal;
