import React, { useState } from 'react';
import { formatDate } from '../../utils/formatters';
import StatusBadge from '../common/StatusBadge';
import { procuringEntityApi } from '../../api/procuringEntityApi';
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
  AlertTriangle,
  Briefcase,
  UserCheck
} from 'lucide-react';

export const VerifyProcuringEntityModal = ({
  isOpen,
  onClose,
  entity,
  onSuccess,
}) => {
  const [adminNotes, setAdminNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [downloadingFile, setDownloadingFile] = useState(false);

  if (!isOpen || !entity) return null;

  // Xem / Tải Quyết định thành lập (PDF)
  const handleViewEstablishmentFile = async () => {
    try {
      setDownloadingFile(true);
      toast.loading('Đang mở tệp Quyết định thành lập...', { id: 'view-est' });
      const response = await procuringEntityApi.downloadEstablishmentFile(entity.id);
      const fileBlob = new Blob([response], { type: 'application/pdf' });
      const fileUrl = window.URL.createObjectURL(fileBlob);
      window.open(fileUrl, '_blank');
      toast.success('Đã mở tệp tài liệu!', { id: 'view-est' });
    } catch (err) {
      toast.error('Không thể mở tệp: ' + (err.message || 'Lỗi kết nối'), { id: 'view-est' });
    } finally {
      setDownloadingFile(false);
    }
  };

  // Xem / Tải Quyết định bổ nhiệm (PDF, nếu có)
  const handleViewAppointmentFile = async () => {
    try {
      setDownloadingFile(true);
      toast.loading('Đang mở tệp Quyết định bổ nhiệm...', { id: 'view-app' });
      const response = await procuringEntityApi.downloadAppointmentFile(entity.id);
      const fileBlob = new Blob([response], { type: 'application/pdf' });
      const fileUrl = window.URL.createObjectURL(fileBlob);
      window.open(fileUrl, '_blank');
      toast.success('Đã mở tệp tài liệu!', { id: 'view-app' });
    } catch (err) {
      toast.error('Không thể mở tệp: ' + (err.message || 'Lỗi kết nối'), { id: 'view-app' });
    } finally {
      setDownloadingFile(false);
    }
  };

  // Thẩm định: Phê duyệt (isApproved = true) hoặc Từ chối (isApproved = false)
  const handleVerify = async (isApproved) => {
    if (!isApproved && !adminNotes.trim()) {
      toast.error('Vui lòng nhập lý do từ chối hồ sơ Bên mời thầu.');
      return;
    }

    try {
      setSubmitting(true);
      const actionName = isApproved ? 'Phê duyệt' : 'Từ chối';
      toast.loading(`Đang tiến hành ${actionName}...`, { id: 'verify-pe' });

      const res = await procuringEntityApi.verifyProcuringEntity(entity.id, {
        isApproved,
        adminNotes: adminNotes.trim() || null,
      });

      if (res && res.success) {
        toast.success(
          isApproved
            ? 'Đã phê duyệt hồ sơ pháp nhân Bên mời thầu thành công!'
            : 'Đã từ chối hồ sơ Bên mời thầu.',
          { id: 'verify-pe' }
        );
        onSuccess && onSuccess(res.data);
        onClose();
      } else {
        toast.error(res?.message || 'Thao tác không thành công', { id: 'verify-pe' });
      }
    } catch (err) {
      toast.error(err.message || 'Đã xảy ra lỗi hệ thống', { id: 'verify-pe' });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-sm overflow-y-auto">
      <div className="relative w-full max-w-2xl bg-white rounded-2xl shadow-xl border border-slate-200 overflow-hidden my-8">
        {/* Header Modal */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 bg-slate-50/50">
          <div className="flex items-center space-x-3">
            <div className="p-2 bg-sky-100 text-sky-700 rounded-xl">
              <Briefcase className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">
                Thẩm định Hồ sơ Bên Mời Thầu
              </h3>
              <p className="text-xs text-slate-500">
                Căn cứ Khoản 3 Điều 4 Luật Đấu thầu 22/2023/QH15 & Nghị định 24/2024/NĐ-CP
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 text-slate-400 hover:text-slate-600 hover:bg-slate-100 rounded-lg transition"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Nội dung Modal */}
        <div className="p-6 space-y-5 text-sm max-h-[75vh] overflow-y-auto">
          {/* Thông tin pháp nhân cơ quan */}
          <div className="p-4 bg-slate-50 rounded-xl border border-slate-200/60 space-y-3">
            <div className="flex items-start justify-between">
              <div>
                <span className="inline-block px-2 py-0.5 text-[11px] font-semibold bg-sky-100 text-sky-800 rounded-md mb-1">
                  {entity.organizationType}
                </span>
                <h4 className="text-base font-bold text-slate-900">
                  {entity.organizationName}
                </h4>
              </div>
              <StatusBadge status={entity.verificationStatus} />
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs pt-1">
              <div>
                <span className="text-slate-500 font-medium">Mã số thuế: </span>
                <span className="text-slate-800 font-bold font-mono">
                  {entity.taxCode || '---'}
                </span>
              </div>
              {entity.budgetCode && (
                <div>
                  <span className="text-slate-500 font-medium">Mã ĐVQHNS: </span>
                  <span className="text-slate-800 font-bold font-mono">
                    {entity.budgetCode}
                  </span>
                </div>
              )}
              <div className="sm:col-span-2 flex items-start space-x-1.5 text-slate-600">
                <MapPin className="w-4 h-4 text-slate-400 shrink-0 mt-0.5" />
                <span>{entity.address || 'Chưa cập nhật địa chỉ trụ sở'}</span>
              </div>
            </div>
          </div>

          {/* Người đại diện & Tài khoản */}
          <div className="p-4 bg-white rounded-xl border border-slate-200 space-y-2.5">
            <h5 className="text-xs font-bold uppercase tracking-wider text-slate-500 flex items-center space-x-1.5">
              <UserCheck className="w-4 h-4 text-slate-600" />
              <span>Người đại diện theo pháp luật / Lãnh đạo đơn vị</span>
            </h5>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
              <div>
                <span className="text-slate-500 font-medium">Họ và tên: </span>
                <span className="text-slate-800 font-semibold">{entity.representativeName}</span>
              </div>
              <div>
                <span className="text-slate-500 font-medium">Chức vụ: </span>
                <span className="text-slate-800 font-semibold">{entity.representativeTitle}</span>
              </div>
              <div className="flex items-center space-x-1.5 text-slate-700">
                <Mail className="w-3.5 h-3.5 text-slate-400" />
                <span>{entity.email}</span>
              </div>
              <div className="flex items-center space-x-1.5 text-slate-700">
                <Phone className="w-3.5 h-3.5 text-slate-400" />
                <span>{entity.representativePhone || entity.phone || 'Chưa có SĐT'}</span>
              </div>
            </div>
          </div>

          {/* Tài liệu pháp lý đính kèm */}
          <div className="space-y-2">
            <label className="block text-xs font-bold uppercase tracking-wider text-slate-600">
              Tài liệu pháp lý đính kèm để thẩm định
            </label>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
              <div className="p-3 bg-sky-50/50 border border-sky-100 rounded-xl flex items-center justify-between">
                <div className="flex items-center space-x-2.5">
                  <div className="p-2 bg-sky-100 text-sky-700 rounded-lg">
                    <FileText className="w-4 h-4" />
                  </div>
                  <div>
                    <p className="text-xs font-semibold text-slate-800">Quyết định thành lập / GP</p>
                    <p className="text-[10px] text-slate-500">File tài liệu pháp nhân gốc</p>
                  </div>
                </div>
                <button
                  type="button"
                  onClick={handleViewEstablishmentFile}
                  disabled={downloadingFile}
                  className="px-2.5 py-1.5 bg-sky-600 hover:bg-sky-700 text-white text-xs font-medium rounded-lg shadow-sm transition flex items-center space-x-1"
                >
                  <ExternalLink className="w-3.5 h-3.5" />
                  <span>Xem file</span>
                </button>
              </div>

              {entity.appointmentDecisionFile && (
                <div className="p-3 bg-slate-50 border border-slate-200 rounded-xl flex items-center justify-between">
                  <div className="flex items-center space-x-2.5">
                    <div className="p-2 bg-slate-200 text-slate-700 rounded-lg">
                      <FileText className="w-4 h-4" />
                    </div>
                    <div>
                      <p className="text-xs font-semibold text-slate-800">Quyết định bổ nhiệm</p>
                      <p className="text-[10px] text-slate-500">Văn bản người đứng đầu</p>
                    </div>
                  </div>
                  <button
                    type="button"
                    onClick={handleViewAppointmentFile}
                    disabled={downloadingFile}
                    className="px-2.5 py-1.5 bg-slate-700 hover:bg-slate-800 text-white text-xs font-medium rounded-lg shadow-sm transition flex items-center space-x-1"
                  >
                    <ExternalLink className="w-3.5 h-3.5" />
                    <span>Xem file</span>
                  </button>
                </div>
              )}
            </div>
          </div>

          {/* Ô nhập ghi chú / lý do từ chối */}
          <div className="space-y-1.5">
            <label className="block text-xs font-bold text-slate-700">
              Ghi chú thẩm định của Quản trị viên (Admin)
              <span className="text-slate-400 font-normal ml-1">
                (Bắt buộc nhập nếu từ chối phê duyệt hồ sơ)
              </span>
            </label>
            <textarea
              rows={3}
              value={adminNotes}
              onChange={(e) => setAdminNotes(e.target.value)}
              placeholder="Nhập nhận xét hoặc lý do từ chối (ví dụ: Quyết định thành lập mờ, thiếu con dấu pháp lý, mã số thuế không khớp...)"
              className="w-full px-3 py-2 text-xs bg-slate-50 border border-slate-300 rounded-xl focus:bg-white focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
            />
          </div>

          {/* Trạng thái duyệt trước đó nếu có */}
          {entity.adminNotes && (
            <div className="p-3 bg-amber-50 rounded-xl border border-amber-200 text-xs text-amber-900 space-y-1">
              <div className="font-semibold flex items-center space-x-1">
                <AlertTriangle className="w-3.5 h-3.5 text-amber-600" />
                <span>Ghi chú thẩm định hiện tại:</span>
              </div>
              <p className="italic text-slate-700">{entity.adminNotes}</p>
              {entity.reviewedByUserName && (
                <p className="text-[11px] text-slate-500">
                  Duyệt bởi: {entity.reviewedByUserName} • {formatDate(entity.reviewedAt)}
                </p>
              )}
            </div>
          )}
        </div>

        {/* Footer Actions */}
        <div className="flex items-center justify-between px-6 py-4 bg-slate-50 border-t border-slate-100">
          <button
            type="button"
            onClick={onClose}
            disabled={submitting}
            className="px-4 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-200/70 rounded-xl transition"
          >
            Đóng
          </button>

          <div className="flex items-center space-x-2.5">
            <button
              type="button"
              onClick={() => handleVerify(false)}
              disabled={submitting}
              className="px-4 py-2 bg-rose-50 hover:bg-rose-100 text-rose-700 border border-rose-200 text-xs font-bold rounded-xl transition flex items-center space-x-1.5 disabled:opacity-50"
            >
              <XCircle className="w-4 h-4 text-rose-600" />
              <span>Từ chối hồ sơ</span>
            </button>

            <button
              type="button"
              onClick={() => handleVerify(true)}
              disabled={submitting}
              className="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-xl shadow-sm hover:shadow transition flex items-center space-x-1.5 disabled:opacity-50"
            >
              <CheckCircle2 className="w-4 h-4" />
              <span>Phê duyệt hồ sơ</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default VerifyProcuringEntityModal;
