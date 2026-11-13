import React from 'react';
import { Download } from 'lucide-react';
import { formatDateTime } from '../../utils/formatters';
import { downloadSecureFile } from '../../utils/fileDownload';
import { renderSubmissionBidPrice } from './PackageSubmissionsTab';

const FILE_TYPE_LABELS = {
  Quotation: 'Báo giá tài chính',
  0: 'Báo giá tài chính',
  Capability: 'Hồ sơ năng lực',
  1: 'Hồ sơ năng lực',
  Schedule: 'Tiến độ thực hiện',
  2: 'Tiến độ thực hiện',
};

const resolveSubmissionFileTypeLabel = (file) => {
  return (
    FILE_TYPE_LABELS[file.fileTypeName] ||
    FILE_TYPE_LABELS[file.fileType] ||
    file.fileTypeName ||
    (file.fileType !== undefined ? `Loại #${file.fileType}` : 'Tài liệu')
  );
};

export const SubmissionDetailModal = ({
  submission,
  pkgStatus,
  isEvaluator,
  isAssignedEvaluator,
  isAdmin,
  isProcurement,
  onClose,
}) => {
  if (!submission) return null;

  const submissionFiles = submission.files || submission.submissionFiles || [];

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/50 backdrop-blur-xs flex items-center justify-center p-4">
      <div
        className="bg-white rounded-2xl max-w-lg w-full p-6 shadow-xl border border-slate-100 relative space-y-4"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between pb-3 border-b border-slate-100">
          <div>
            <h3 className="text-base font-bold text-slate-900">Chi tiết hồ sơ dự thầu</h3>
            <p className="text-xs text-slate-500 font-mono">
              {submission.companyName || `Nhà thầu #${submission.contractorId}`}
            </p>
          </div>
          <button
            onClick={onClose}
            className="text-slate-400 hover:text-slate-600 p-1 rounded-lg hover:bg-slate-100 transition"
          >
            ✕
          </button>
        </div>

        <div className="space-y-3 text-xs">
          <div className="grid grid-cols-3 gap-2 p-3 bg-slate-50 rounded-xl">
            <div>
              <span className="text-slate-400">Thời gian nộp:</span>
              <p className="font-semibold text-slate-800">{formatDateTime(submission.submittedAt)}</p>
            </div>
            <div>
              <span className="text-slate-400">Trạng thái:</span>
              <p className="font-semibold text-slate-800">{submission.status}</p>
            </div>
            <div>
              <span className="text-slate-400">Giá dự thầu:</span>
              <p className="font-semibold text-slate-900 font-mono">
                {renderSubmissionBidPrice({
                  pkgStatus,
                  bidPrice: submission.bidPrice,
                  isEvaluator,
                  isAssignedEvaluator,
                  isAdmin,
                  isProcurement,
                })}
              </p>
            </div>
          </div>

          <div className="space-y-2">
            <span className="font-bold text-slate-800">
              Tệp tài liệu đính kèm ({submissionFiles.length}):
            </span>
            {submissionFiles.length > 0 ? (
              <div className="divide-y divide-slate-100 border border-slate-100 rounded-xl overflow-hidden max-h-56 overflow-y-auto">
                {submissionFiles.map((file) => (
                  <div
                    key={file.id}
                    className="p-3 flex items-center justify-between hover:bg-slate-50 transition"
                  >
                    <div className="truncate pr-3">
                      <p className="font-medium text-slate-800 truncate" title={file.fileName}>
                        {file.fileName}
                      </p>
                      <div className="flex items-center gap-2 mt-0.5">
                        <span className="text-[10px] font-semibold px-2 py-0.5 rounded-md bg-sky-50 text-sky-700 border border-sky-100">
                          {resolveSubmissionFileTypeLabel(file)}
                        </span>
                        {file.uploadedAt && (
                          <span className="text-[10px] text-slate-400 font-mono">
                            {formatDateTime(file.uploadedAt)}
                          </span>
                        )}
                      </div>
                    </div>
                    <button
                      type="button"
                      onClick={() =>
                        downloadSecureFile(
                          `/api/submissions/files/${file.id}/download`,
                          file.fileName
                        )
                      }
                      className="px-3 py-1.5 bg-slate-100 hover:bg-sky-600 text-slate-700 hover:text-white rounded-xl text-xs font-semibold transition flex items-center space-x-1.5 shrink-0 cursor-pointer"
                    >
                      <Download className="w-3.5 h-3.5" />
                      <span>Tải</span>
                    </button>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-slate-400 italic py-2">Không có tệp đính kèm.</p>
            )}
          </div>
        </div>

        <div className="pt-2 flex justify-end">
          <button
            onClick={onClose}
            className="px-4 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-semibold rounded-xl transition"
          >
            Đóng
          </button>
        </div>
      </div>
    </div>
  );
};

export default SubmissionDetailModal;
