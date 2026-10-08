import React, { useState } from 'react';
import { formatDate } from '../../utils/formatters';
import StatusBadge from '../common/StatusBadge';
import { contractorApi } from '../../api/contractorApi';
import toast from 'react-hot-toast';
import {
  Building2,
  X,
  FileText,
  Download,
  ExternalLink,
  CheckCircle2,
  XCircle,
  Mail,
  Phone,
  MapPin,
  Calendar,
  Star,
  FileCheck2,
  AlertTriangle
} from 'lucide-react';

export const VerifyContractorModal = ({
  isOpen,
  onClose,
  contractor,
  onSuccess,
}) => {
  const [notes, setNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [downloadingFile, setDownloadingFile] = useState(false);

  if (!isOpen || !contractor) return null;

  // Xử lý xem/tải tệp Giấy phép đăng ký kinh doanh
  const handleViewLicenseFile = async () => {
    try {
      setDownloadingFile(true);
      toast.loading('Đang mở tệp Giấy phép kinh doanh...', { id: 'view-license' });
      const response = await contractorApi.downloadLicenseFile(contractor.id);
      const fileBlob = new Blob([response], { type: 'application/pdf' });
      const fileUrl = window.URL.createObjectURL(fileBlob);
      window.open(fileUrl, '_blank');
      toast.success('Đã mở tệp GPKD!', { id: 'view-license' });
    } catch (err) {
      toast.error('Không thể mở tệp GPKD: ' + (err.message || 'Lỗi kết nối'), { id: 'view-license' });
    } finally {
      setDownloadingFile(false);
    }
  };

  // Xử lý Thẩm định: Phê duyệt (isApproved = true) hoặc Từ chối (isApproved = false)
  const handleVerify = async (isApproved) => {
    if (!isApproved && !notes.trim()) {
      toast.error('Vui lòng nhập lý do từ chối hồ sơ năng lực nhà thầu.');
      return;
    }

    try {
      setSubmitting(true);
      const res = await contractorApi.verifyContractor(contractor.id, {
        isApproved,
        notes: notes.trim() || undefined,
      });

      if (res && res.success) {
        toast.success(
          isApproved
            ? `Đã phê duyệt và kích hoạt quyền dự thầu cho ${contractor.companyName}!`
            : `Đã từ chối phê duyệt hồ sơ của ${contractor.companyName}!`
        );
        onSuccess?.();
        onClose();
      } else {
        toast.error(res?.message || 'Thao tác không thành công.');
      }
    } catch (err) {
      toast.error(err.message || 'Lỗi khi cập nhật trạng thái thẩm định.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/50 backdrop-blur-xs flex items-center justify-center p-4 animate-in fade-in duration-150">
      <div
        className="bg-white rounded-2xl max-w-xl w-full p-6 shadow-2xl border border-slate-100 relative space-y-5"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Nút đóng */}
        <button
          onClick={onClose}
          disabled={submitting}
          className="absolute top-4 right-4 text-slate-400 hover:text-slate-600 p-1 rounded-lg hover:bg-slate-100 transition"
        >
          <X className="w-5 h-5" />
        </button>

        {/* Header Modal */}
        <div className="flex items-start space-x-3.5 pr-8">
          <div className="w-12 h-12 rounded-xl bg-gradient-to-tr from-sky-600 to-indigo-600 text-white flex items-center justify-center flex-shrink-0 shadow-sm">
            <Building2 className="w-6 h-6" />
          </div>
          <div className="space-y-1 flex-1">
            <div className="flex items-center space-x-2">
              <h3 className="text-base font-bold text-slate-900">
                Thẩm định Hồ sơ Doanh nghiệp
              </h3>
              <StatusBadge status={contractor.verificationStatus} type="contractor_verification" />
            </div>
            <p className="text-xs text-slate-500 font-mono">
              Nhà thầu #{contractor.id} • {contractor.companyName}
            </p>
          </div>
        </div>

        {/* Khối thông tin chi tiết Pháp lý & Liên hệ */}
        <div className="bg-slate-50 rounded-xl p-4 border border-slate-200/80 space-y-3 text-xs">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <span className="text-slate-400 block font-medium">Mã số thuế:</span>
              <span className="font-mono font-bold text-slate-800 text-sm">
                {contractor.taxCode || 'Chưa cập nhật'}
              </span>
            </div>
            <div>
              <span className="text-slate-400 block font-medium">Đánh giá uy tín:</span>
              <span className="inline-flex items-center space-x-1 font-bold text-amber-600">
                <Star className="w-3.5 h-3.5 fill-amber-400 text-amber-400" />
                <span>{contractor.rating?.toFixed(1) || '0.0'} / 5.0</span>
              </span>
            </div>
          </div>

          <div className="space-y-1">
            <span className="text-slate-400 block font-medium">Địa chỉ trụ sở:</span>
            <div className="flex items-start space-x-1.5 text-slate-700">
              <MapPin className="w-3.5 h-3.5 text-slate-400 flex-shrink-0 mt-0.5" />
              <span>{contractor.address || 'Chưa cập nhật'}</span>
            </div>
          </div>

          <div className="pt-2 border-t border-slate-200/70 grid grid-cols-1 sm:grid-cols-2 gap-2 text-slate-600">
            <div className="flex items-center space-x-1.5">
              <span className="text-slate-400">Đại diện:</span>
              <span className="font-semibold text-slate-800">{contractor.fullName || 'N/A'}</span>
            </div>
            <div className="flex items-center space-x-1.5">
              <Mail className="w-3.5 h-3.5 text-slate-400" />
              <span className="truncate">{contractor.email}</span>
            </div>
            {contractor.phone && (
              <div className="flex items-center space-x-1.5">
                <Phone className="w-3.5 h-3.5 text-slate-400" />
                <span>{contractor.phone}</span>
              </div>
            )}
            <div className="flex items-center space-x-1.5">
              <Calendar className="w-3.5 h-3.5 text-slate-400" />
              <span>Đăng ký: {formatDate(contractor.createdAt)}</span>
            </div>
          </div>
        </div>

        {/* Khối xem tệp Giấy phép đăng ký kinh doanh */}
        <div className="rounded-xl border border-sky-100 bg-sky-50/50 p-3.5 flex items-center justify-between">
          <div className="flex items-center space-x-2.5">
            <div className="p-2 bg-sky-100 text-sky-700 rounded-lg">
              <FileCheck2 className="w-5 h-5" />
            </div>
            <div>
              <p className="text-xs font-bold text-slate-800">
                Giấy phép đăng ký kinh doanh (GPKD)
              </p>
              <p className="text-[11px] text-slate-500">
                {contractor.businessLicenseFile
                  ? 'Đã tải lên tệp đính kèm điện tử'
                  : 'Chưa có tệp đính kèm'}
              </p>
            </div>
          </div>

          {contractor.businessLicenseFile ? (
            <button
              type="button"
              onClick={handleViewLicenseFile}
              disabled={downloadingFile}
              className="inline-flex items-center space-x-1.5 px-3 py-1.5 text-xs font-semibold text-sky-700 bg-white hover:bg-sky-50 rounded-lg border border-sky-200 shadow-2xs transition disabled:opacity-50"
            >
              <ExternalLink className="w-3.5 h-3.5" />
              <span>{downloadingFile ? 'Đang mở...' : 'Xem / Tải tệp'}</span>
            </button>
          ) : (
            <span className="text-[11px] text-slate-400 italic">Không có file</span>
          )}
        </div>

        {/* Ghi chú / Lý do thẩm định */}
        <div className="space-y-1.5">
          <label className="text-xs font-semibold text-slate-700 block">
            Ghi chú thẩm định / Lý do phê duyệt hoặc từ chối:
          </label>
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            disabled={submitting}
            placeholder="Nhập ghi chú thẩm định hoặc lý do nếu từ chối hồ sơ..."
            rows={3}
            className="w-full text-xs rounded-xl border border-slate-200 p-3 focus:outline-hidden focus:ring-2 focus:ring-sky-500 transition resize-none disabled:bg-slate-50"
          />
        </div>

        {/* Nút hành động dành riêng cho Admin */}
        <div className="pt-2 flex items-center justify-between border-t border-slate-100">
          <button
            type="button"
            onClick={onClose}
            disabled={submitting}
            className="px-4 py-2 text-xs font-semibold text-slate-600 hover:bg-slate-100 rounded-xl transition border border-slate-200"
          >
            Đóng
          </button>

          <div className="flex items-center space-x-2">
            <button
              type="button"
              onClick={() => handleVerify(false)}
              disabled={submitting}
              className="inline-flex items-center space-x-1.5 px-3.5 py-2 text-xs font-bold text-rose-700 bg-rose-50 hover:bg-rose-100 rounded-xl transition border border-rose-200 shadow-2xs disabled:opacity-50"
            >
              <XCircle className="w-4 h-4" />
              <span>Từ chối hồ sơ</span>
            </button>

            <button
              type="button"
              onClick={() => handleVerify(true)}
              disabled={submitting}
              className="inline-flex items-center space-x-1.5 px-4 py-2 text-xs font-bold text-white bg-emerald-600 hover:bg-emerald-700 rounded-xl transition shadow-xs disabled:opacity-50"
            >
              <CheckCircle2 className="w-4 h-4" />
              <span>Phê duyệt kích hoạt</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default VerifyContractorModal;
