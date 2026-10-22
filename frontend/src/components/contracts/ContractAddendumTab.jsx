import React, { useState } from 'react';
import toast from 'react-hot-toast';
import {
  Layers,
  Plus,
  Calendar,
  DollarSign,
  FileText,
  Download,
  Eye,
  CheckCircle,
  Clock,
  Upload,
  TrendingUp,
  Check,
  X,
  XCircle,
} from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { contractApi } from '../../api/contractApi';
import { downloadSecureFile, previewSecureFile } from '../../utils/fileDownload';
import { formatCurrency, formatDate } from '../../utils/formatters';

const TYPE_COLORS = {
  TimelineExtension: 'bg-indigo-50 text-indigo-700 border-indigo-200',
  ValueAdjustment: 'bg-emerald-50 text-emerald-700 border-emerald-200',
  ScopeChange: 'bg-amber-50 text-amber-800 border-amber-200',
  GeneralTerms: 'bg-slate-100 text-slate-700 border-slate-200',
};

export const ContractAddendumTab = ({
  contract,
  isAdminOrProcurement,
  onOpenAddModal,
  onRefresh,
}) => {
  const { user } = useAuth();
  const isAdmin = user?.role === 'Admin';
  const addenda = contract?.addenda || [];
  const [uploadingId, setUploadingId] = useState(null);
  const [processingId, setProcessingId] = useState(null);

  const handleReviewAddendum = async (addendumId, isApproved) => {
    const confirmMsg = isApproved
      ? 'Bạn có chắc chắn muốn phê duyệt phụ lục này và áp dụng điều chỉnh vào hợp đồng?'
      : 'Bạn có chắc chắn muốn từ chối phụ lục này?';
    if (!window.confirm(confirmMsg)) return;

    setProcessingId(addendumId);
    try {
      await contractApi.approveAddendum(contract.id, addendumId, isApproved);
      toast.success(isApproved ? 'Đã phê duyệt phụ lục hợp đồng thành công!' : 'Đã từ chối phụ lục hợp đồng.');
      if (onRefresh) onRefresh();
    } catch (err) {
      console.error('Lỗi duyệt phụ lục:', err);
      toast.error(err.response?.data?.message || 'Không thể xử lý yêu cầu phê duyệt phụ lục');
    } finally {
      setProcessingId(null);
    }
  };

  // Thống kê
  const totalAdjustedValue = addenda.reduce((sum, a) => sum + (a.additionalValue || 0), 0);
  const totalExtensionDays = addenda.reduce((sum, a) => sum + (a.extensionDays || 0), 0);

  const handleUploadFile = async (addendumId, file) => {
    if (!file) return;
    setUploadingId(addendumId);
    try {
      await contractApi.uploadAddendumDocument(contract.id, addendumId, file);
      toast.success('Tải lên bản scan phụ lục hợp đồng thành công!');
      onRefresh();
    } catch (err) {
      console.error('Lỗi upload phụ lục:', err);
      toast.error('Không thể tải lên tệp scan phụ lục');
    } finally {
      setUploadingId(null);
    }
  };

  return (
    <div className="space-y-4">
      {/* Header & Button */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-100 pb-3">
        <div>
          <h3 className="text-sm font-bold text-slate-900 flex items-center gap-2">
            <Layers className="w-4 h-4 text-indigo-600" />
            <span>Quản Lý Phụ Lục Hợp Đồng (Addenda / Amendments)</span>
          </h3>
          <p className="text-xs text-slate-500">
            Điều chỉnh phạm vi, giá trị hoặc gia hạn thời gian thực hiện ({addenda.length} phụ lục đã ban hành)
          </p>
        </div>

        {isAdminOrProcurement && (
          <button
            type="button"
            onClick={onOpenAddModal}
            className="inline-flex items-center gap-1.5 px-3.5 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-bold rounded-xl shadow-xs transition-colors cursor-pointer shrink-0"
          >
            <Plus className="w-3.5 h-3.5" />
            <span>Lập Phụ Lục Hợp Đồng</span>
          </button>
        )}
      </div>

      {/* Thẻ thống kê điều chỉnh */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
        <div className="p-3.5 bg-slate-50 border border-slate-200 rounded-xl space-y-1">
          <span className="text-[11px] text-slate-500 font-semibold block">Tổng số phụ lục:</span>
          <p className="text-base font-black text-indigo-700">{addenda.length} văn bản</p>
        </div>

        <div className="p-3.5 bg-slate-50 border border-slate-200 rounded-xl space-y-1">
          <span className="text-[11px] text-slate-500 font-semibold block">Giá trị điều chỉnh lũy kế:</span>
          <p
            className={`text-base font-black ${
              totalAdjustedValue >= 0 ? 'text-emerald-700' : 'text-rose-700'
            }`}
          >
            {totalAdjustedValue > 0 ? `+${formatCurrency(totalAdjustedValue)}` : formatCurrency(totalAdjustedValue)}
          </p>
        </div>

        <div className="p-3.5 bg-slate-50 border border-slate-200 rounded-xl space-y-1">
          <span className="text-[11px] text-slate-500 font-semibold block">Thời gian gia hạn thêm:</span>
          <p className="text-base font-black text-slate-900">
            {totalExtensionDays > 0 ? `+${totalExtensionDays} ngày` : '0 ngày'}
          </p>
        </div>
      </div>

      {/* Danh sách phụ lục */}
      {addenda.length === 0 ? (
        <div className="py-12 border-2 border-dashed border-slate-200 rounded-2xl text-center space-y-3">
          <Layers className="w-10 h-10 text-slate-300 mx-auto" />
          <h4 className="text-xs font-bold text-slate-700">Chưa có phụ lục hợp đồng nào</h4>
          <p className="text-xs text-slate-400 max-w-sm mx-auto">
            Khi phát sinh bổ sung khối lượng, biến động đơn giá hoặc cần gia hạn tiến độ, Bên mời thầu sẽ tạo phụ lục điều chỉnh tại đây.
          </p>
          {isAdminOrProcurement && (
            <button
              type="button"
              onClick={onOpenAddModal}
              className="inline-flex items-center gap-1.5 px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-bold rounded-xl shadow-xs transition-colors cursor-pointer"
            >
              <Plus className="w-4 h-4" />
              <span>Lập phụ lục đầu tiên</span>
            </button>
          )}
        </div>
      ) : (
        <div className="space-y-3">
          {addenda.map((a, idx) => {
            const badgeClass = TYPE_COLORS[a.type] || 'bg-slate-100 text-slate-700 border-slate-200';
            const downloadUrl = `/api/contracts/${contract.id}/addenda/${a.id}/document/download`;

            return (
              <div
                key={a.id}
                className="p-4 bg-white rounded-2xl border border-slate-200 hover:border-slate-300 shadow-2xs space-y-2.5 transition-all"
              >
                <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
                  <div className="space-y-1 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="text-xs font-black text-indigo-700 bg-indigo-50 px-2.5 py-0.5 rounded-lg border border-indigo-200">
                        {a.addendumNumber}
                      </span>
                      <span className={`text-[11px] font-bold px-2 py-0.5 rounded-full border ${badgeClass}`}>
                        {a.typeName}
                      </span>
                      {a.status === 3 || a.statusName === 'Chờ phê duyệt' ? (
                        <span className="text-[11px] font-semibold text-amber-700 bg-amber-50 px-2 py-0.5 rounded-full border border-amber-200 flex items-center gap-1">
                          <Clock className="w-3 h-3" />
                          Chờ phê duyệt
                        </span>
                      ) : a.status === 2 || a.statusName === 'Bị từ chối' ? (
                        <span className="text-[11px] font-semibold text-rose-700 bg-rose-50 px-2 py-0.5 rounded-full border border-rose-200 flex items-center gap-1">
                          <XCircle className="w-3 h-3" />
                          Bị từ chối
                        </span>
                      ) : (
                        <span className="text-[11px] font-semibold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-full border border-emerald-200 flex items-center gap-1">
                          <CheckCircle className="w-3 h-3" />
                          {a.statusName || 'Đã phê duyệt'}
                        </span>
                      )}
                    </div>

                    <h4 className="text-xs font-bold text-slate-900 pt-0.5">{a.title}</h4>

                    <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 pt-1 text-xs">
                      <div>
                        <span className="text-slate-400 text-[11px] block">Ngày ký kết:</span>
                        <strong className="text-slate-800">{formatDate(a.signedDate)}</strong>
                      </div>
                      <div>
                        <span className="text-slate-400 text-[11px] block">Giá trị điều chỉnh:</span>
                        <strong
                          className={
                            a.additionalValue > 0
                              ? 'text-emerald-700'
                              : a.additionalValue < 0
                              ? 'text-rose-700'
                              : 'text-slate-600'
                          }
                        >
                          {a.additionalValue > 0
                            ? `+${formatCurrency(a.additionalValue)}`
                            : formatCurrency(a.additionalValue)}
                        </strong>
                      </div>
                      <div>
                        <span className="text-slate-400 text-[11px] block">Gia hạn thêm:</span>
                        <strong className="text-indigo-700">
                          {a.extensionDays > 0 ? `+${a.extensionDays} ngày` : 'Không'}
                        </strong>
                      </div>
                      <div>
                        <span className="text-slate-400 text-[11px] block">Ngày kết thúc mới:</span>
                        <strong className="text-slate-900">
                          {a.adjustedEndDate ? formatDate(a.adjustedEndDate) : 'Giữ nguyên'}
                        </strong>
                      </div>
                    </div>

                    {a.reason && (
                      <div className="p-2.5 bg-slate-50 rounded-xl border border-slate-100 text-[11px] text-slate-600">
                        <span className="font-bold text-slate-700">Lý do điều chỉnh:</span> {a.reason}
                      </div>
                    )}
                  </div>

                  {/* Cụm Action Buttons */}
                  <div className="flex flex-wrap items-center gap-2 shrink-0">
                    {isAdmin && (a.status === 3 || a.statusName === 'Chờ phê duyệt') && (
                      <div className="flex items-center gap-1">
                        <button
                          type="button"
                          disabled={processingId === a.id}
                          onClick={() => handleReviewAddendum(a.id, true)}
                          className="inline-flex items-center gap-1 px-2.5 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-lg shadow-2xs transition-colors cursor-pointer disabled:opacity-50"
                          title="Phê duyệt phụ lục"
                        >
                          <Check className="w-3.5 h-3.5" />
                          <span>Duyệt</span>
                        </button>
                        <button
                          type="button"
                          disabled={processingId === a.id}
                          onClick={() => handleReviewAddendum(a.id, false)}
                          className="inline-flex items-center gap-1 px-2 py-1.5 bg-rose-50 hover:bg-rose-100 text-rose-700 border border-rose-200 text-xs font-bold rounded-lg transition-colors cursor-pointer disabled:opacity-50"
                          title="Từ chối phụ lục"
                        >
                          <X className="w-3.5 h-3.5" />
                          <span>Từ chối</span>
                        </button>
                      </div>
                    )}
                    {a.scannedDocumentPath ? (
                      <div className="flex items-center gap-1.5">
                        <button
                          type="button"
                          onClick={() => previewSecureFile(downloadUrl, `PhuLuc_${a.addendumNumber}.pdf`)}
                          className="inline-flex items-center gap-1 px-2.5 py-1.5 bg-indigo-50 text-indigo-700 hover:bg-indigo-100 border border-indigo-200 text-xs font-semibold rounded-lg transition-colors cursor-pointer"
                          title="Xem trước tài liệu"
                        >
                          <Eye className="w-3.5 h-3.5" />
                          <span>Xem</span>
                        </button>
                        <button
                          type="button"
                          onClick={() => downloadSecureFile(downloadUrl, `PhuLuc_${a.addendumNumber}.pdf`)}
                          className="inline-flex items-center gap-1 px-2.5 py-1.5 bg-slate-100 text-slate-700 hover:bg-slate-200 border border-slate-200 text-xs font-semibold rounded-lg transition-colors cursor-pointer"
                          title="Tải về máy"
                        >
                          <Download className="w-3.5 h-3.5" />
                          <span>Tải</span>
                        </button>
                      </div>
                    ) : (
                      isAdminOrProcurement && (
                        <label className="inline-flex items-center gap-1 px-2.5 py-1.5 bg-slate-50 hover:bg-slate-100 border border-slate-300 text-slate-700 text-xs font-semibold rounded-lg cursor-pointer transition-colors">
                          <Upload className="w-3.5 h-3.5 text-indigo-600" />
                          <span>{uploadingId === a.id ? 'Đang tải...' : 'Đính scan'}</span>
                          <input
                            type="file"
                            accept=".pdf,.png,.jpg,.jpeg"
                            className="hidden"
                            disabled={uploadingId === a.id}
                            onChange={(e) => handleUploadFile(a.id, e.target.files?.[0])}
                          />
                        </label>
                      )
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};

export default ContractAddendumTab;
