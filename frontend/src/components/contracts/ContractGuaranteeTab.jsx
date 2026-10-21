import React, { useState } from 'react';
import toast from 'react-hot-toast';
import {
  ShieldCheck,
  Plus,
  AlertTriangle,
  Building,
  Calendar,
  DollarSign,
  Download,
  Eye,
  CheckCircle,
  Clock,
  Ban,
  FileText,
  Upload,
} from 'lucide-react';
import { contractApi } from '../../api/contractApi';
import { downloadSecureFile, previewSecureFile } from '../../utils/fileDownload';
import { formatCurrency, formatDate } from '../../utils/formatters';

const STATUS_BADGES = {
  PendingApproval: { label: 'Chờ duyệt', bg: 'bg-amber-50 text-amber-700 border-amber-200' },
  Valid: { label: 'Có hiệu lực', bg: 'bg-emerald-50 text-emerald-700 border-emerald-200' },
  Released: { label: 'Đã giải tỏa / hoàn trả', bg: 'bg-blue-50 text-blue-700 border-blue-200' },
  Claimed: { label: 'Bị tịch thu vi phạm', bg: 'bg-rose-50 text-rose-700 border-rose-200' },
  Expired: { label: 'Đã hết hạn', bg: 'bg-slate-100 text-slate-600 border-slate-200' },
};

export const ContractGuaranteeTab = ({
  contract,
  isAdminOrProcurement,
  onOpenAddModal,
  onRefresh,
}) => {
  const guarantees = contract?.guarantees || [];
  const [updatingId, setUpdatingId] = useState(null);
  const [uploadingId, setUploadingId] = useState(null);

  // Tính toán thống kê
  const activeGuarantees = guarantees.filter((g) => g.status === 1 || g.status === 'Valid');
  const totalActiveAmount = activeGuarantees.reduce((sum, g) => sum + (g.amount || 0), 0);
  const activePct =
    contract?.value > 0 ? ((totalActiveAmount / contract.value) * 100).toFixed(1) : 0;
  const nearExpiryGuarantees = guarantees.filter((g) => g.isNearExpiry);

  const handleUpdateStatus = async (guaranteeId, newStatus) => {
    const statusText =
      newStatus === 1
        ? 'Xác nhận hiệu lực (Valid)'
        : newStatus === 2
        ? 'Giải tỏa / hoàn trả bảo lãnh (Released)'
        : newStatus === 3
        ? 'Tịch thu do vi phạm (Claimed)'
        : 'Đã hết hạn (Expired)';

    if (!window.confirm(`Bạn có chắc muốn chuyển chứng thư sang trạng thái: "${statusText}"?`)) {
      return;
    }

    setUpdatingId(guaranteeId);
    try {
      const res = await contractApi.updateGuaranteeStatus(contract.id, guaranteeId, newStatus);
      if (res?.success) {
        toast.success(`Cập nhật trạng thái chứng thư bảo lãnh thành công!`);
        onRefresh();
      } else {
        toast.error(res?.message || 'Không thể cập nhật trạng thái');
      }
    } catch (err) {
      console.error('Lỗi đổi trạng thái bảo lãnh:', err);
      toast.error('Lỗi khi cập nhật trạng thái chứng thư');
    } finally {
      setUpdatingId(null);
    }
  };

  const handleUploadFile = async (guaranteeId, file) => {
    if (!file) return;
    setUploadingId(guaranteeId);
    try {
      await contractApi.uploadGuaranteeDocument(contract.id, guaranteeId, file);
      toast.success('Tải lên bản scan chứng thư bảo lãnh thành công!');
      onRefresh();
    } catch (err) {
      console.error('Lỗi upload file:', err);
      toast.error('Không thể tải lên tệp scan bảo lãnh');
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
            <ShieldCheck className="w-4 h-4 text-blue-600" />
            <span>Quản Lý Bảo Lãnh Thực Hiện Hợp Đồng</span>
          </h3>
          <p className="text-xs text-slate-500">
            Căn cứ Điều 14 & Điều 68 Luật Đấu thầu số 22/2023/QH15 ({guarantees.length} chứng thư đã ghi nhận)
          </p>
        </div>

        {isAdminOrProcurement && (
          <button
            type="button"
            onClick={onOpenAddModal}
            className="inline-flex items-center gap-1.5 px-3.5 py-2 bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold rounded-xl shadow-xs transition-colors cursor-pointer shrink-0"
          >
            <Plus className="w-3.5 h-3.5" />
            <span>Thêm Chứng Thư Bảo Lãnh</span>
          </button>
        )}
      </div>

      {/* Cảnh báo bảo lãnh sắp hết hạn */}
      {nearExpiryGuarantees.length > 0 && (
        <div className="p-3.5 bg-amber-50 border border-amber-200 rounded-xl flex items-start gap-3">
          <AlertTriangle className="w-5 h-5 text-amber-600 shrink-0 mt-0.5" />
          <div className="text-xs space-y-1">
            <p className="font-bold text-amber-900">
              Cảnh báo: Có {nearExpiryGuarantees.length} chứng thư bảo lãnh sắp hết hạn trong vòng 30 ngày!
            </p>
            <p className="text-amber-800">
              Theo quy định, Bên mời thầu cần yêu cầu nhà thầu gia hạn bảo đảm thực hiện hợp đồng trước khi chứng thư hết hiệu lực để tránh rủi ro pháp lý.
            </p>
          </div>
        </div>
      )}

      {/* Thẻ tóm tắt thông số bảo lãnh */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
        <div className="p-3.5 bg-slate-50 border border-slate-200 rounded-xl space-y-1">
          <span className="text-[11px] text-slate-500 font-semibold block">Tổng tiền bảo lãnh hiệu lực:</span>
          <p className="text-base font-black text-blue-700">{formatCurrency(totalActiveAmount)}</p>
        </div>

        <div className="p-3.5 bg-slate-50 border border-slate-200 rounded-xl space-y-1">
          <span className="text-[11px] text-slate-500 font-semibold block">Tỷ lệ bảo đảm / Giá trị HĐ:</span>
          <div className="flex items-center gap-2">
            <p className="text-base font-black text-emerald-700">{activePct}%</p>
            <span className="text-[10px] text-slate-500">(Quy chuẩn 2% - 10%)</span>
          </div>
        </div>

        <div className="p-3.5 bg-slate-50 border border-slate-200 rounded-xl space-y-1">
          <span className="text-[11px] text-slate-500 font-semibold block">Tình trạng bảo đảm:</span>
          {activeGuarantees.length > 0 ? (
            <span className="inline-flex items-center gap-1 text-xs font-bold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-full border border-emerald-200">
              <CheckCircle className="w-3.5 h-3.5" />
              Đã bảo đảm hợp lệ
            </span>
          ) : (
            <span className="inline-flex items-center gap-1 text-xs font-bold text-rose-700 bg-rose-50 px-2 py-0.5 rounded-full border border-rose-200">
              <Ban className="w-3.5 h-3.5" />
              Chưa có bảo lãnh hiệu lực
            </span>
          )}
        </div>
      </div>

      {/* Danh sách chứng thư */}
      {guarantees.length === 0 ? (
        <div className="py-12 border-2 border-dashed border-slate-200 rounded-2xl text-center space-y-3">
          <ShieldCheck className="w-10 h-10 text-slate-300 mx-auto" />
          <h4 className="text-xs font-bold text-slate-700">Chưa ghi nhận chứng thư bảo lãnh nào</h4>
          <p className="text-xs text-slate-400 max-w-sm mx-auto">
            Hợp đồng cần được nộp bảo đảm thực hiện hợp đồng từ 2% - 10% giá trị hợp đồng trước khi khởi công thi công.
          </p>
          {isAdminOrProcurement && (
            <button
              type="button"
              onClick={onOpenAddModal}
              className="inline-flex items-center gap-1.5 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold rounded-xl shadow-xs transition-colors cursor-pointer"
            >
              <Plus className="w-4 h-4" />
              <span>Thêm chứng thư đầu tiên</span>
            </button>
          )}
        </div>
      ) : (
        <div className="space-y-3">
          {guarantees.map((g) => {
            const badge = STATUS_BADGES[g.statusName] || STATUS_BADGES.Valid;
            const downloadUrl = `/api/contracts/${contract.id}/guarantees/${g.id}/document/download`;

            return (
              <div
                key={g.id}
                className={`p-4 rounded-2xl border transition-all ${
                  g.isNearExpiry
                    ? 'bg-amber-50/40 border-amber-300 shadow-2xs'
                    : 'bg-white border-slate-200 hover:border-slate-300 shadow-2xs'
                }`}
              >
                <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
                  <div className="space-y-1.5 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="text-xs font-black text-slate-900 flex items-center gap-1">
                        <ShieldCheck className="w-4 h-4 text-blue-600" />
                        {g.guaranteeNumber}
                      </span>
                      <span
                        className={`text-[11px] font-bold px-2.5 py-0.5 rounded-full border ${badge.bg}`}
                      >
                        {g.statusName}
                      </span>
                      {g.isNearExpiry && (
                        <span className="text-[10px] font-bold text-amber-800 bg-amber-100 px-2 py-0.5 rounded-full flex items-center gap-1">
                          <AlertTriangle className="w-3 h-3 text-amber-600" />
                          Sắp hết hạn
                        </span>
                      )}
                    </div>

                    <div className="flex items-center gap-1.5 text-xs text-slate-600">
                      <Building className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                      <strong className="text-slate-800">{g.bankName}</strong>
                    </div>

                    <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 pt-1 text-xs">
                      <div>
                        <span className="text-slate-400 text-[11px] block">Số tiền bảo lãnh:</span>
                        <strong className="text-emerald-700">{formatCurrency(g.amount)}</strong>
                      </div>
                      <div>
                        <span className="text-slate-400 text-[11px] block">Tỷ lệ:</span>
                        <strong className="text-blue-700">{g.percentage}%</strong>
                      </div>
                      <div>
                        <span className="text-slate-400 text-[11px] block">Hiệu lực từ:</span>
                        <span className="text-slate-700">{formatDate(g.effectiveDate)}</span>
                      </div>
                      <div>
                        <span className="text-slate-400 text-[11px] block">Hết hạn vào:</span>
                        <strong className={g.isNearExpiry ? 'text-amber-700' : 'text-slate-900'}>
                          {formatDate(g.expiryDate)}
                        </strong>
                      </div>
                    </div>

                    {g.notes && (
                      <p className="text-[11px] text-slate-500 italic pt-1 border-t border-slate-100">
                        Ghi chú: {g.notes}
                      </p>
                    )}
                  </div>

                  {/* Cụm Action Buttons */}
                  <div className="flex flex-wrap sm:flex-col items-end gap-2 shrink-0">
                    {/* Tải/Xem file scan */}
                    {g.scannedDocumentPath ? (
                      <div className="flex items-center gap-1.5">
                        <button
                          type="button"
                          onClick={() => previewSecureFile(downloadUrl, `BaoLanh_${g.guaranteeNumber}.pdf`)}
                          className="inline-flex items-center gap-1 px-2.5 py-1.5 bg-blue-50 text-blue-700 hover:bg-blue-100 border border-blue-200 text-xs font-semibold rounded-lg transition-colors cursor-pointer"
                          title="Xem trước tài liệu"
                        >
                          <Eye className="w-3.5 h-3.5" />
                          <span>Xem</span>
                        </button>
                        <button
                          type="button"
                          onClick={() => downloadSecureFile(downloadUrl, `BaoLanh_${g.guaranteeNumber}.pdf`)}
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
                          <Upload className="w-3.5 h-3.5 text-blue-600" />
                          <span>{uploadingId === g.id ? 'Đang tải...' : 'Đính scan'}</span>
                          <input
                            type="file"
                            accept=".pdf,.png,.jpg,.jpeg"
                            className="hidden"
                            disabled={uploadingId === g.id}
                            onChange={(e) => handleUploadFile(g.id, e.target.files?.[0])}
                          />
                        </label>
                      )
                    )}

                    {/* Đổi trạng thái (Admin/Procurement) */}
                    {isAdminOrProcurement && (
                      <div className="flex items-center gap-1">
                        {g.status !== 1 && (
                          <button
                            type="button"
                            disabled={updatingId === g.id}
                            onClick={() => handleUpdateStatus(g.id, 1)}
                            className="px-2 py-1 text-[11px] font-bold text-emerald-700 bg-emerald-50 hover:bg-emerald-100 border border-emerald-200 rounded-md transition-colors"
                            title="Xác nhận chứng thư có hiệu lực"
                          >
                            Hiệu lực
                          </button>
                        )}
                        {g.status === 1 && (
                          <button
                            type="button"
                            disabled={updatingId === g.id}
                            onClick={() => handleUpdateStatus(g.id, 2)}
                            className="px-2 py-1 text-[11px] font-bold text-blue-700 bg-blue-50 hover:bg-blue-100 border border-blue-200 rounded-md transition-colors"
                            title="Giải tỏa hoàn trả sau nghiệm thu"
                          >
                            Giải tỏa
                          </button>
                        )}
                        {g.status === 1 && (
                          <button
                            type="button"
                            disabled={updatingId === g.id}
                            onClick={() => handleUpdateStatus(g.id, 3)}
                            className="px-2 py-1 text-[11px] font-bold text-rose-700 bg-rose-50 hover:bg-rose-100 border border-rose-200 rounded-md transition-colors"
                            title="Tịch thu do nhà thầu vi phạm nghĩa vụ"
                          >
                            Tịch thu
                          </button>
                        )}
                      </div>
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

export default ContractGuaranteeTab;
