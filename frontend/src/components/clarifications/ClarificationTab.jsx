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
  Download,
} from 'lucide-react';
import { formatDateTime } from '../../utils/formatters';
import { clarificationApi } from '../../api/clarificationApi';
import { previewSecureFile, downloadSecureFile } from '../../utils/fileDownload';
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
    <div className="bg-white rounded-2xl border border-slate-200 shadow-xs p-6 space-y-6">
      {/* 1. Header & Tiêu đề Phân hệ */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-slate-100">
        <div>
          <h3 className="text-sm font-bold text-slate-900 flex items-center space-x-2">
            <HelpCircle className="w-4 h-4 text-sky-600" />
            <span>Hỏi - Đáp & Làm rõ hồ sơ (Clarification Q&A)</span>
          </h3>
          <p className="text-xs text-slate-500 mt-0.5">
            Kênh làm rõ chính thức về Hồ sơ mời thầu (HSMT) và giải trình Hồ sơ dự thầu (HSDT) theo Luật Đấu thầu
          </p>
        </div>

        {/* Action Buttons ở Header */}
        <div className="flex items-center gap-2">
          {subTab === 'package' && isContractor && (
            <button
              type="button"
              onClick={() => setIsAskPackageModalOpen(true)}
              className="px-3.5 py-2 bg-sky-600 hover:bg-sky-700 text-white text-xs font-bold rounded-xl transition-all flex items-center gap-1.5 shadow-xs cursor-pointer"
            >
              <Plus className="w-4 h-4" />
              <span>Đặt câu hỏi làm rõ HSMT</span>
            </button>
          )}

          {subTab === 'submission' && (isAdmin || isProcurement || isEvaluator) && (
            <button
              type="button"
              onClick={() => setIsRequestSubmissionModalOpen(true)}
              className="px-3.5 py-2 bg-amber-600 hover:bg-amber-700 text-white text-xs font-bold rounded-xl transition-all flex items-center gap-1.5 shadow-xs cursor-pointer"
            >
              <Plus className="w-4 h-4" />
              <span>Yêu cầu giải trình HSDT</span>
            </button>
          )}
        </div>
      </div>

      {/* 2. Sub-Tabs Segmented Switcher & Làm mới */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        {/* Switcher 2 Tab */}
        <div className="inline-flex p-1 bg-slate-100 border border-slate-200/80 rounded-xl">
          <button
            type="button"
            onClick={() => setSubTab('package')}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg text-xs font-semibold transition-all cursor-pointer ${
              subTab === 'package'
                ? 'bg-white text-sky-700 shadow-xs font-bold border border-slate-200/80'
                : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            <HelpCircle className="w-4 h-4 text-sky-600" />
            <span>Làm rõ HSMT</span>
            <span
              className={`px-2 py-0.5 rounded-full text-[11px] font-bold ${
                subTab === 'package'
                  ? 'bg-sky-100 text-sky-800'
                  : 'bg-slate-200 text-slate-600'
              }`}
            >
              {packageClarifications.length}
            </span>
          </button>

          <button
            type="button"
            onClick={() => setSubTab('submission')}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg text-xs font-semibold transition-all cursor-pointer ${
              subTab === 'submission'
                ? 'bg-white text-amber-700 shadow-xs font-bold border border-slate-200/80'
                : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            <Lock className="w-3.5 h-3.5 text-amber-600" />
            <span>Giải trình HSDT</span>
            <span
              className={`px-2 py-0.5 rounded-full text-[11px] font-bold ${
                subTab === 'submission'
                  ? 'bg-amber-100 text-amber-800'
                  : 'bg-slate-200 text-slate-600'
              }`}
            >
              {submissionClarifications.length}
            </span>
          </button>
        </div>

        {/* Nút Làm mới danh sách */}
        <button
          type="button"
          onClick={fetchClarifications}
          disabled={loading}
          className="px-3.5 py-2 bg-slate-50 hover:bg-slate-100 text-slate-600 hover:text-slate-900 rounded-xl text-xs font-semibold border border-slate-200 transition-colors flex items-center gap-1.5 self-start sm:self-auto cursor-pointer"
          title="Tải lại dữ liệu làm rõ mới nhất"
        >
          <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin text-sky-600' : 'text-slate-400'}`} />
          <span>Làm mới danh sách</span>
        </button>
      </div>

      {/* 3. Dải Banner Nguyên Tắc & Quy Định Pháp Lý */}
      {subTab === 'package' ? (
        <div className="p-4 bg-sky-50/70 border border-sky-200/80 rounded-xl flex items-start gap-3">
          <HelpCircle className="w-5 h-5 text-sky-600 shrink-0 mt-0.5" />
          <p className="text-xs text-sky-900 leading-relaxed">
            <strong className="text-sky-950 font-bold">Nguyên tắc công khai:</strong> Mọi thắc mắc và văn bản làm rõ Hồ sơ mời thầu (HSMT) được hiển thị công khai cho toàn bộ các nhà thầu tham gia để đảm bảo tính minh bạch, cạnh tranh lành mạnh theo quy định của Luật Đấu thầu.
          </p>
        </div>
      ) : (
        <div className="p-4 bg-amber-50/70 border border-amber-200/80 rounded-xl flex items-start gap-3">
          <Lock className="w-5 h-5 text-amber-600 shrink-0 mt-0.5" />
          <p className="text-xs text-amber-900 leading-relaxed">
            <strong className="text-amber-950 font-bold">Nguyên tắc bảo mật chuyên biệt:</strong> Quá trình làm rõ Hồ sơ dự thầu (HSDT) chỉ diễn ra riêng tư giữa Tổ chuyên gia / Bên mời thầu và Nhà thầu sở hữu hồ sơ. Tuyệt đối không để lộ thông tin dự thầu cho các nhà thầu khác.
          </p>
        </div>
      )}

      {/* 4. Danh sách các câu hỏi / yêu cầu làm rõ */}
      {loading ? (
        <div className="py-16 text-center text-slate-500 space-y-3">
          <RefreshCw className="w-8 h-8 animate-spin mx-auto text-sky-600" />
          <p className="text-xs font-semibold">Đang tải dữ liệu làm rõ thầu...</p>
        </div>
      ) : currentList.length === 0 ? (
        /* Empty State Chuẩn UI/UX Pro Max */
        <div className="py-14 px-6 text-center bg-slate-50/50 rounded-2xl border border-dashed border-slate-200 flex flex-col items-center justify-center space-y-3">
          <div
            className={`w-14 h-14 rounded-2xl flex items-center justify-center shadow-xs mb-1 ${
              subTab === 'package'
                ? 'bg-sky-100 text-sky-600'
                : 'bg-amber-100 text-amber-600'
            }`}
          >
            <MessageSquare className="w-7 h-7" />
          </div>

          <div>
            <p className="text-sm font-bold text-slate-800">
              {subTab === 'package'
                ? 'Chưa có yêu cầu làm rõ HSMT nào'
                : 'Chưa có yêu cầu giải trình HSDT nào'}
            </p>
            <p className="text-xs text-slate-500 mt-1 max-w-md leading-relaxed mx-auto">
              {subTab === 'package'
                ? 'Nhà thầu có thể gửi câu hỏi thắc mắc về tiêu chí đánh giá, phạm vi công việc hoặc các điều khoản trong hồ sơ mời thầu.'
                : 'Tổ chuyên gia chấm thầu sẽ gửi yêu cầu giải trình nếu phát hiện các tài liệu cần làm sáng tỏ trong hồ sơ dự thầu.'}
            </p>
          </div>

          {/* Quick CTA button */}
          {subTab === 'package' && isContractor && (
            <button
              type="button"
              onClick={() => setIsAskPackageModalOpen(true)}
              className="mt-2 px-4 py-2 bg-sky-600 hover:bg-sky-700 text-white rounded-xl text-xs font-bold transition shadow-xs flex items-center gap-1.5 cursor-pointer"
            >
              <Plus className="w-4 h-4" />
              <span>Gửi câu hỏi làm rõ HSMT ngay</span>
            </button>
          )}

          {subTab === 'submission' && (isAdmin || isProcurement || isEvaluator) && (
            <button
              type="button"
              onClick={() => setIsRequestSubmissionModalOpen(true)}
              className="mt-2 px-4 py-2 bg-amber-600 hover:bg-amber-700 text-white rounded-xl text-xs font-bold transition shadow-xs flex items-center gap-1.5 cursor-pointer"
            >
              <Plus className="w-4 h-4" />
              <span>Tạo yêu cầu giải trình HSDT</span>
            </button>
          )}
        </div>
      ) : (
        /* Danh Sách Card Câu Hỏi / Giải Trình */
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
                className="p-5 bg-white border border-slate-200 rounded-xl space-y-4 hover:border-slate-300 transition-all shadow-2xs"
              >
                {/* Header card */}
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-slate-100 pb-3">
                  <div className="flex items-center gap-2.5 flex-wrap">
                    <span
                      className={`px-2.5 py-1 rounded-full text-xs font-bold border ${
                        isAnswered
                          ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
                          : 'bg-amber-50 text-amber-700 border-amber-200'
                      }`}
                    >
                      {item.statusName || (isAnswered ? 'Đã phản hồi' : 'Chờ phản hồi')}
                    </span>
                    <h4 className="text-sm font-bold text-slate-900">{item.title}</h4>
                  </div>

                  <div className="text-xs text-slate-400 flex items-center gap-1.5 font-medium">
                    <Clock className="w-3.5 h-3.5 text-slate-400" />
                    <span>{formatDateTime(item.askedAt)}</span>
                  </div>
                </div>

                {/* Metadata người hỏi / đối tượng */}
                <div className="flex flex-wrap items-center gap-4 text-xs text-slate-500">
                  <span className="flex items-center gap-1.5">
                    <User className="w-3.5 h-3.5 text-sky-600" />
                    <span>Người gửi:</span>
                    <strong className="text-slate-800">{item.askedByUserName}</strong>
                  </span>

                  {item.contractorName && (
                    <span className="flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-slate-100 text-slate-700 font-semibold border border-slate-200/60">
                      <Building className="w-3.5 h-3.5 text-amber-600" />
                      Nhà thầu: {item.contractorName}
                    </span>
                  )}

                  {item.deadline && (
                    <span className="flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-rose-50 text-rose-700 font-bold border border-rose-200/80">
                      <Clock className="w-3.5 h-3.5 text-rose-600" />
                      Hạn giải trình: {formatDateTime(item.deadline)}
                    </span>
                  )}
                </div>

                {/* Hộp nội dung câu hỏi */}
                <div className="p-4 bg-slate-50/80 rounded-xl border border-slate-100 space-y-2">
                  <p className="text-[11px] font-bold text-slate-500 uppercase tracking-wider">
                    {subTab === 'package' ? 'Nội dung thắc mắc:' : 'Nội dung yêu cầu giải trình:'}
                  </p>
                  <p className="text-xs text-slate-800 leading-relaxed whitespace-pre-line">
                    {item.questionContent}
                  </p>
                  {item.questionAttachmentUrl && (
                    <div className="pt-2">
                      <button
                        type="button"
                        onClick={() => previewSecureFile(`/api/clarifications/attachments/download?filePath=${encodeURIComponent(item.questionAttachmentUrl)}`, 'Tep_dinh_kem_cau_hoi')}
                        className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-white border border-slate-200 hover:bg-sky-50 text-sky-700 rounded-lg text-xs font-semibold transition cursor-pointer"
                      >
                        <Paperclip className="w-3.5 h-3.5 text-sky-600" />
                        <span>Xem tệp đính kèm câu hỏi</span>
                      </button>
                    </div>
                  )}
                </div>

                {/* Hộp nội dung phản hồi nếu đã có */}
                {isAnswered && (
                  <div className="p-4 bg-emerald-50/50 border border-emerald-200/70 rounded-xl space-y-2">
                    <div className="flex items-center justify-between text-xs text-emerald-800 font-medium">
                      <span className="flex items-center gap-1.5 font-bold">
                        <CheckCircle2 className="w-4 h-4 text-emerald-600" />
                        <span>
                          {subTab === 'package' ? 'Bên mời thầu phản hồi' : 'Nhà thầu nộp văn bản giải trình'}:{' '}
                          <strong className="text-emerald-950">{item.answeredByUserName}</strong>
                        </span>
                      </span>
                      <span className="text-[11px] text-emerald-700">{formatDateTime(item.answeredAt)}</span>
                    </div>

                    <p className="text-xs text-slate-800 leading-relaxed whitespace-pre-line pt-1">
                      {item.answerContent}
                    </p>

                    {item.answerAttachmentUrl && (
                      <div className="pt-2">
                        <button
                          type="button"
                          onClick={() => previewSecureFile(`/api/clarifications/attachments/download?filePath=${encodeURIComponent(item.answerAttachmentUrl)}`, 'Van_ban_giai_trinh_phan_hoi')}
                          className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-white border border-emerald-200 hover:bg-emerald-100/60 text-emerald-800 rounded-lg text-xs font-bold transition cursor-pointer shadow-2xs"
                        >
                          <Paperclip className="w-3.5 h-3.5 text-emerald-600" />
                          <span>Xem văn bản giải trình / phản hồi đính kèm</span>
                        </button>
                      </div>
                    )}
                  </div>
                )}

                {/* Nút hành động trả lời nếu chưa phản hồi */}
                {canAnswer && (
                  <div className="flex items-center justify-end pt-1">
                    <button
                      type="button"
                      onClick={() => setSelectedClarificationToAnswer(item)}
                      className={`px-4 py-2 rounded-xl text-xs font-bold flex items-center gap-1.5 transition-all shadow-xs cursor-pointer ${
                        subTab === 'package'
                          ? 'bg-sky-600 text-white hover:bg-sky-700'
                          : 'bg-emerald-600 text-white hover:bg-emerald-700'
                      }`}
                    >
                      <Send className="w-3.5 h-3.5" />
                      <span>{subTab === 'package' ? 'Phản hồi làm rõ HSMT' : 'Nộp văn bản giải trình HSDT'}</span>
                    </button>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}

      {/* Modals nghiệp vụ */}
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
