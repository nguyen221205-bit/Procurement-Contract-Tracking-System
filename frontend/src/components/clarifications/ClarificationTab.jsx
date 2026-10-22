import React, { useState, useEffect } from 'react';
import {
  HelpCircle,
  MessageSquare,
  Clock,
  CheckCircle2,
  AlertCircle,
  Plus,
  Send,
  Paperclip,
  User,
  Building,
  Shield,
  FileText,
  Lock,
  ExternalLink,
  RefreshCw,
} from 'lucide-react';
import { formatDateTime } from '../../utils/formatters';
import { clarificationApi } from '../../api/clarificationApi';
import AskPackageClarificationModal from './AskPackageClarificationModal';
import RequestSubmissionClarificationModal from './RequestSubmissionClarificationModal';
import AnswerClarificationModal from './AnswerClarificationModal';
import toast from 'react-hot-toast';

export const ClarificationTab = ({
  packageId,
  packageCode,
  isAdmin,
  isProcurement,
  isEvaluator,
  isContractor,
  submissions = [],
  mySubmission = null,
}) => {
  const [subTab, setSubTab] = useState('package'); // 'package' (HSMT) hoặc 'submission' (HSDT)
  const [clarifications, setClarifications] = useState([]);
  const [loading, setLoading] = useState(false);

  // Modals state
  const [isAskPackageModalOpen, setIsAskPackageModalOpen] = useState(false);
  const [isRequestSubmissionModalOpen, setIsRequestSubmissionModalOpen] = useState(false);
  const [selectedClarificationToAnswer, setSelectedClarificationToAnswer] = useState(null);

  const fetchClarifications = async () => {
    setLoading(true);
    try {
      const res = await clarificationApi.getClarifications(packageId);
      if (res && res.success && Array.isArray(res.data)) {
        setClarifications(res.data);
      }
    } catch (err) {
      toast.error('Không thể tải danh sách làm rõ thầu');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (packageId) {
      fetchClarifications();
    }
  }, [packageId]);

  // Lọc dữ liệu theo subTab
  const packageClarifications = clarifications.filter(
    (c) => c.type === 'PackageClarification' || c.type === 0
  );
  const submissionClarifications = clarifications.filter(
    (c) => c.type === 'SubmissionClarification' || c.type === 1
  );

  const currentList = subTab === 'package' ? packageClarifications : submissionClarifications;

  return (
    <div className="space-y-6">
      {/* Header & Sub-tabs */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-5 bg-slate-900/60 border border-slate-800 rounded-2xl">
        {/* Navigation tabs */}
        <div className="flex items-center gap-2 p-1.5 bg-slate-950/80 border border-slate-800 rounded-xl">
          <button
            onClick={() => setSubTab('package')}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg text-xs font-semibold transition-all ${
              subTab === 'package'
                ? 'bg-sky-600 text-white shadow-md shadow-sky-600/30'
                : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            <HelpCircle className="w-4 h-4" />
            Làm rõ HSMT ({packageClarifications.length})
          </button>

          <button
            onClick={() => setSubTab('submission')}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg text-xs font-semibold transition-all ${
              subTab === 'submission'
                ? 'bg-amber-600 text-slate-950 font-bold shadow-md shadow-amber-600/30'
                : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            <Lock className="w-3.5 h-3.5" />
            Giải trình HSDT ({submissionClarifications.length})
          </button>
        </div>

        {/* Action Buttons */}
        <div className="flex items-center gap-2">
          <button
            onClick={fetchClarifications}
            disabled={loading}
            className="p-2 text-slate-400 hover:text-slate-200 hover:bg-slate-800 rounded-xl transition-colors border border-slate-800"
            title="Làm mới danh sách"
          >
            <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
          </button>

          {subTab === 'package' && isContractor && (
            <button
              onClick={() => setIsAskPackageModalOpen(true)}
              className="px-4 py-2 bg-sky-600 text-white text-xs font-semibold rounded-xl hover:bg-sky-500 transition-all flex items-center gap-2 shadow-lg shadow-sky-600/20"
            >
              <Plus className="w-4 h-4" /> Đặt câu hỏi làm rõ HSMT
            </button>
          )}

          {subTab === 'submission' && (isAdmin || isProcurement || isEvaluator) && (
            <button
              onClick={() => setIsRequestSubmissionModalOpen(true)}
              className="px-4 py-2 bg-amber-600 text-slate-950 text-xs font-bold rounded-xl hover:bg-amber-500 transition-all flex items-center gap-2 shadow-lg shadow-amber-600/20"
            >
              <Plus className="w-4 h-4" /> Yêu cầu giải trình HSDT
            </button>
          )}
        </div>
      </div>

      {/* Thông tin mô tả quy định */}
      {subTab === 'package' ? (
        <div className="p-4 bg-sky-500/10 border border-sky-500/20 rounded-2xl flex items-start gap-3">
          <HelpCircle className="w-5 h-5 text-sky-400 shrink-0 mt-0.5" />
          <p className="text-xs text-sky-300 leading-relaxed">
            <strong>Nguyên tắc công khai:</strong> Mọi thắc mắc và văn bản làm rõ Hồ sơ mời thầu (HSMT) được hiển thị công khai cho toàn bộ các nhà thầu tham gia để đảm bảo tính minh bạch, cạnh tranh lành mạnh theo quy định của Luật Đấu thầu.
          </p>
        </div>
      ) : (
        <div className="p-4 bg-amber-500/10 border border-amber-500/20 rounded-2xl flex items-start gap-3">
          <Lock className="w-5 h-5 text-amber-400 shrink-0 mt-0.5" />
          <p className="text-xs text-amber-300 leading-relaxed">
            <strong>Nguyên tắc bảo mật chuyên biệt:</strong> Quá trình làm rõ Hồ sơ dự thầu (HSDT) chỉ diễn ra riêng tư giữa Tổ chuyên gia / Bên mời thầu và Nhà thầu sở hữu hồ sơ. Tuyệt đối không để lộ thông tin dự thầu cho các nhà thầu khác.
          </p>
        </div>
      )}

      {/* Danh sách các câu hỏi / yêu cầu làm rõ */}
      {loading ? (
        <div className="flex flex-col items-center justify-center p-12 text-slate-400">
          <RefreshCw className="w-8 h-8 animate-spin mb-3 text-sky-500" />
          <p className="text-sm">Đang tải dữ liệu làm rõ thầu...</p>
        </div>
      ) : currentList.length === 0 ? (
        <div className="flex flex-col items-center justify-center p-12 bg-slate-900/40 border border-slate-800 rounded-2xl text-center">
          <MessageSquare className="w-12 h-12 text-slate-600 mb-3" />
          <p className="text-base font-semibold text-slate-300">
            {subTab === 'package' ? 'Chưa có yêu cầu làm rõ HSMT nào' : 'Chưa có yêu cầu giải trình HSDT nào'}
          </p>
          <p className="text-xs text-slate-500 mt-1 max-w-md">
            {subTab === 'package'
              ? 'Nhà thầu có thể gửi câu hỏi thắc mắc về tiêu chí đánh giá hoặc các điều khoản trong hồ sơ mời thầu.'
              : 'Tổ chuyên gia chấm thầu sẽ gửi yêu cầu giải trình nếu phát hiện các tài liệu cần làm sáng tỏ trong hồ sơ dự thầu.'}
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {currentList.map((item) => {
            const isAnswered = item.status === 'Answered' || item.status === 1;
            const canAnswer =
              !isAnswered &&
              ((subTab === 'package' && (isAdmin || isProcurement)) ||
                (subTab === 'submission' && (isContractor || isAdmin)));

            return (
              <div
                key={item.id}
                className="p-5 bg-slate-900/60 border border-slate-800 rounded-2xl space-y-4 hover:border-slate-700/80 transition-all shadow-sm"
              >
                {/* Header card */}
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-slate-800/80 pb-3">
                  <div className="flex items-center gap-2.5">
                    <span
                      className={`px-2.5 py-1 rounded-full text-xs font-semibold border ${
                        isAnswered
                          ? 'bg-emerald-500/10 text-emerald-400 border-emerald-500/30'
                          : 'bg-amber-500/10 text-amber-400 border-amber-500/30'
                      }`}
                    >
                      {item.statusName || (isAnswered ? 'Đã phản hồi' : 'Chờ phản hồi')}
                    </span>
                    <h4 className="text-sm font-bold text-slate-100">{item.title}</h4>
                  </div>

                  <div className="text-xs text-slate-400 flex items-center gap-2">
                    <Clock className="w-3.5 h-3.5 text-slate-500" />
                    <span>{formatDateTime(item.askedAt)}</span>
                  </div>
                </div>

                {/* Metadata người hỏi / đối tượng */}
                <div className="flex flex-wrap items-center gap-4 text-xs text-slate-400">
                  <span className="flex items-center gap-1.5">
                    <User className="w-3.5 h-3.5 text-indigo-400" />
                    Người gửi: <strong className="text-slate-200">{item.askedByUserName}</strong>
                  </span>

                  {item.contractorName && (
                    <span className="flex items-center gap-1.5 px-2.5 py-0.5 rounded-lg bg-slate-800 text-slate-300 font-medium">
                      <Building className="w-3.5 h-3.5 text-amber-400" />
                      Nhà thầu: {item.contractorName}
                    </span>
                  )}

                  {item.deadline && (
                    <span className="flex items-center gap-1.5 text-amber-400 font-semibold">
                      <Clock className="w-3.5 h-3.5" />
                      Hạn giải trình: {formatDateTime(item.deadline)}
                    </span>
                  )}
                </div>

                {/* Nội dung câu hỏi */}
                <div className="p-3.5 bg-slate-950/60 rounded-xl border border-slate-800/80 space-y-2">
                  <p className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
                    {subTab === 'package' ? 'Nội dung thắc mắc:' : 'Nội dung yêu cầu giải trình:'}
                  </p>
                  <p className="text-sm text-slate-200 leading-relaxed whitespace-pre-line">
                    {item.questionContent}
                  </p>
                  {item.questionAttachmentUrl && (
                    <div className="pt-2">
                      <a
                        href={item.questionAttachmentUrl}
                        target="_blank"
                        rel="noreferrer"
                        className="inline-flex items-center gap-1.5 text-xs text-sky-400 hover:text-sky-300 hover:underline"
                      >
                        <Paperclip className="w-3.5 h-3.5" /> Tệp đính kèm câu hỏi
                      </a>
                    </div>
                  )}
                </div>

                {/* Nội dung phản hồi nếu đã có */}
                {isAnswered && (
                  <div className="p-4 bg-emerald-950/20 border border-emerald-500/30 rounded-xl space-y-2">
                    <div className="flex items-center justify-between text-xs text-emerald-400 font-medium">
                      <span className="flex items-center gap-1.5">
                        <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                        {subTab === 'package' ? 'Bên mời thầu phản hồi' : 'Nhà thầu nộp văn bản giải trình'}:{' '}
                        <strong className="text-slate-100">{item.answeredByUserName}</strong>
                      </span>
                      <span>{formatDateTime(item.answeredAt)}</span>
                    </div>

                    <p className="text-sm text-slate-100 leading-relaxed whitespace-pre-line pt-1">
                      {item.answerContent}
                    </p>

                    {item.answerAttachmentUrl && (
                      <div className="pt-2">
                        <a
                          href={item.answerAttachmentUrl}
                          target="_blank"
                          rel="noreferrer"
                          className="inline-flex items-center gap-1.5 text-xs text-emerald-400 hover:text-emerald-300 hover:underline font-semibold"
                        >
                          <Paperclip className="w-3.5 h-3.5" /> Tệp đính kèm văn bản giải trình / phản hồi
                        </a>
                      </div>
                    )}
                  </div>
                )}

                {/* Nút hành động trả lời nếu chưa phản hồi */}
                {canAnswer && (
                  <div className="flex items-center justify-end pt-1">
                    <button
                      onClick={() => setSelectedClarificationToAnswer(item)}
                      className={`px-4 py-2 rounded-xl text-xs font-semibold flex items-center gap-1.5 transition-all shadow-md ${
                        subTab === 'package'
                          ? 'bg-sky-600 text-white hover:bg-sky-500 shadow-sky-600/20'
                          : 'bg-emerald-600 text-white hover:bg-emerald-500 shadow-emerald-600/20'
                      }`}
                    >
                      <Send className="w-3.5 h-3.5" />
                      {subTab === 'package' ? 'Phản hồi làm rõ HSMT' : 'Nộp văn bản giải trình HSDT'}
                    </button>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}

      {/* Modals */}
      <AskPackageClarificationModal
        isOpen={isAskPackageModalOpen}
        onClose={() => setIsAskPackageModalOpen(false)}
        packageId={packageId}
        packageCode={packageCode}
        onSuccess={fetchClarifications}
      />

      <RequestSubmissionClarificationModal
        isOpen={isRequestSubmissionModalOpen}
        onClose={() => setIsRequestSubmissionModalOpen(false)}
        packageId={packageId}
        packageCode={packageCode}
        submissions={submissions}
        onSuccess={fetchClarifications}
      />

      <AnswerClarificationModal
        isOpen={Boolean(selectedClarificationToAnswer)}
        onClose={() => setSelectedClarificationToAnswer(null)}
        clarification={selectedClarificationToAnswer}
        onSuccess={fetchClarifications}
      />
    </div>
  );
};

export default ClarificationTab;
