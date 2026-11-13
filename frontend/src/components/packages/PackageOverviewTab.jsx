import React from 'react';
import { FileText, Download, Eye } from 'lucide-react';
import { formatDate } from '../../utils/formatters';
import { downloadSecureFile, previewSecureFile } from '../../utils/fileDownload';

export const PackageOverviewTab = ({ pkg, isContractor }) => {
  const documents = pkg?.bidDocuments || [];

  return (
    <div className="space-y-6">
      {/* Mô tả chi tiết */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-xs p-6 space-y-3">
        <h3 className="text-sm font-bold text-slate-900 flex items-center space-x-2">
          <FileText className="w-4 h-4 text-sky-600" />
          <span>Mô tả & Phạm vi công việc</span>
        </h3>
        <div className="text-xs text-slate-600 leading-relaxed whitespace-pre-line bg-slate-50/60 p-4 rounded-xl border border-slate-100">
          {pkg?.description || 'Chưa có mô tả chi tiết cho gói thầu này.'}
        </div>
      </div>

      {/* Tài liệu mời thầu HSMT */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-xs p-6 space-y-4">
        <div className="flex items-center justify-between">
          <h3 className="text-sm font-bold text-slate-900 flex items-center space-x-2">
            <Download className="w-4 h-4 text-sky-600" />
            <span>Hồ sơ mời thầu đính kèm (HSMT)</span>
          </h3>
          <span className="text-xs text-slate-400 font-medium">{documents.length} tài liệu</span>
        </div>

        {documents.length > 0 ? (
          <div className="divide-y divide-slate-100 border border-slate-100 rounded-xl overflow-hidden">
            {documents.map((doc) => {
              const downloadUrl = `/api/bid-packages/documents/${doc.id}/download`;
              return (
                <div
                  key={doc.id}
                  className="p-3.5 flex items-center justify-between hover:bg-slate-50 transition"
                >
                  <div className="flex items-center space-x-3 truncate pr-4">
                    <div className="p-2 bg-sky-50 text-sky-600 rounded-lg flex-shrink-0">
                      <FileText className="w-4 h-4" />
                    </div>
                    <div className="truncate">
                      <p className="text-xs font-bold text-slate-800 truncate" title={doc.fileName}>
                        {doc.fileName}
                      </p>
                      <p className="text-[11px] text-slate-400">
                        Tải lên ngày: {formatDate(doc.uploadedAt)}
                      </p>
                    </div>
                  </div>

                  <div className="flex items-center space-x-2 flex-shrink-0">
                    <button
                      type="button"
                      onClick={() => previewSecureFile(downloadUrl, doc.fileName)}
                      className="px-2.5 py-1.5 bg-sky-50 hover:bg-sky-600 text-sky-700 hover:text-white rounded-lg text-xs font-semibold transition flex items-center space-x-1.5 cursor-pointer"
                      title="Xem trước tài liệu HSMT"
                    >
                      <Eye className="w-3.5 h-3.5" />
                      <span>Xem trước</span>
                    </button>
                    {!isContractor && (
                      <button
                        type="button"
                        onClick={() => downloadSecureFile(downloadUrl, doc.fileName)}
                        className="px-2.5 py-1.5 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-lg text-xs font-semibold transition flex items-center space-x-1.5 cursor-pointer"
                        title="Tải tệp về máy"
                      >
                        <Download className="w-3.5 h-3.5" />
                        <span>Tải về</span>
                      </button>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        ) : (
          <div className="py-8 text-center text-xs text-slate-400 bg-slate-50/50 rounded-xl border border-dashed border-slate-200">
            Chưa có tài liệu HSMT nào được đính kèm cho gói thầu này.
          </div>
        )}
      </div>
    </div>
  );
};

export default PackageOverviewTab;
