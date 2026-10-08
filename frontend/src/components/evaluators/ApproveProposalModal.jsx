import React, { useState } from 'react';
import { X, CheckCircle2, KeyRound, Copy, Check, Eye, EyeOff, FileText, ExternalLink, ShieldCheck } from 'lucide-react';
import toast from 'react-hot-toast';
import { evaluatorProposalApi } from '../../api/evaluatorProposalApi';

export const ApproveProposalModal = ({ isOpen, onClose, proposal, onSuccess }) => {
  const [password, setPassword] = useState('Eval@2026!');
  const [showPassword, setShowPassword] = useState(false);
  const [adminNotes, setAdminNotes] = useState('');
  const [copied, setCopied] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [downloadingCert, setDownloadingCert] = useState(false);

  if (!isOpen || !proposal) return null;

  // Tạo ngẫu nhiên một mật khẩu mạnh
  const generateRandomPassword = () => {
    const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%';
    let res = 'Eval@';
    for (let i = 0; i < 4; i++) {
      res += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    res += '!';
    setPassword(res);
  };

  const handleCopyPassword = () => {
    navigator.clipboard.writeText(password);
    setCopied(true);
    toast.success('Đã sao chép mật khẩu!');
    setTimeout(() => setCopied(false), 2000);
  };

  const handleViewCertificate = async () => {
    try {
      setDownloadingCert(true);
      toast.loading('Đang mở tệp chứng chỉ...', { id: 'view-cert' });
      const response = await evaluatorProposalApi.downloadCertificateFile(proposal.id);
      const fileBlob = new Blob([response], { type: 'application/pdf' });
      const fileUrl = window.URL.createObjectURL(fileBlob);
      window.open(fileUrl, '_blank');
      toast.success('Đã mở tệp chứng chỉ!', { id: 'view-cert' });
    } catch (err) {
      toast.error('Không thể mở tệp: ' + (err.message || 'Lỗi mạng'), { id: 'view-cert' });
    } finally {
      setDownloadingCert(false);
    }
  };

  const handleApprove = async (e) => {
    e.preventDefault();

    if (!password.trim() || password.length < 6) {
      toast.error('Vui lòng nhập mật khẩu khởi tạo có tối thiểu 6 ký tự.');
      return;
    }

    try {
      setSubmitting(true);
      const res = await evaluatorProposalApi.approveProposal(proposal.id, {
        password: password.trim(),
        adminNotes: adminNotes.trim() || undefined,
      });

      if (res && res.success) {
        toast.success(res.message || `Đã phê duyệt và cấp tài khoản cho ${proposal.fullName}!`);
        onSuccess?.();
        onClose();
      } else {
        toast.error(res?.message || 'Phê duyệt không thành công.');
      }
    } catch (err) {
      toast.error(err.message || 'Lỗi khi phê duyệt đề xuất.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/50 backdrop-blur-xs flex items-center justify-center p-4 animate-in fade-in duration-150">
      <div
        className="bg-white rounded-2xl max-w-lg w-full p-6 shadow-2xl border border-slate-100 relative space-y-5"
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
          <div className="w-12 h-12 rounded-xl bg-gradient-to-tr from-emerald-600 to-teal-600 text-white flex items-center justify-center flex-shrink-0 shadow-sm">
            <ShieldCheck className="w-6 h-6" />
          </div>
          <div>
            <h3 className="text-base font-bold text-slate-900">
              Phê duyệt & Khởi tạo Tài khoản Giám khảo
            </h3>
            <p className="text-xs text-slate-500">
              Admin ấn định mật khẩu ban đầu. Hệ thống sẽ tạo User với vai trò Evaluator.
            </p>
          </div>
        </div>

        {/* Khối tóm tắt thông tin Giám khảo */}
        <div className="bg-slate-50 rounded-xl p-3.5 border border-slate-200/80 space-y-2 text-xs">
          <div className="flex items-center justify-between pb-1.5 border-b border-slate-200">
            <div>
              <span className="font-bold text-slate-900 text-sm">{proposal.fullName}</span>
              <span className="text-slate-500 block font-mono text-[11px]">{proposal.email}</span>
            </div>
            {proposal.phone && (
              <span className="text-slate-600 font-mono">{proposal.phone}</span>
            )}
          </div>

          <div className="grid grid-cols-2 gap-2 text-[11px] text-slate-600">
            <div>
              <span className="text-slate-400 block">Chuyên môn:</span>
              <span className="font-semibold text-slate-800">{proposal.specialization || 'Chưa cập nhật'}</span>
            </div>
            <div>
              <span className="text-slate-400 block">Kinh nghiệm:</span>
              <span className="font-semibold text-slate-800">
                {proposal.experienceYears ? `${proposal.experienceYears} năm` : 'Chưa cập nhật'}
              </span>
            </div>
          </div>

          {proposal.certificateFile && (
            <div className="pt-2 border-t border-slate-200 flex items-center justify-between">
              <span className="text-[11px] text-slate-600 truncate max-w-[220px]" title={proposal.certificateName}>
                📜 {proposal.certificateName || 'Chứng chỉ nghiệp vụ'}
              </span>
              <button
                type="button"
                onClick={handleViewCertificate}
                disabled={downloadingCert}
                className="inline-flex items-center space-x-1 px-2.5 py-1 text-[11px] font-semibold text-sky-700 bg-white hover:bg-sky-50 rounded-lg border border-sky-200 transition shadow-2xs"
              >
                <ExternalLink className="w-3 h-3" />
                <span>{downloadingCert ? 'Đang mở...' : 'Xem chứng chỉ'}</span>
              </button>
            </div>
          )}
        </div>

        {/* Form phê duyệt & đặt mật khẩu */}
        <form onSubmit={handleApprove} className="space-y-4 text-xs">
          {/* Ô nhập mật khẩu */}
          <div className="space-y-1.5">
            <div className="flex items-center justify-between">
              <label className="font-semibold text-slate-700 block">
                Mật khẩu khởi tạo tài khoản <span className="text-rose-500">*</span>
              </label>
              <button
                type="button"
                onClick={generateRandomPassword}
                className="text-indigo-600 hover:text-indigo-800 font-semibold text-[11px] hover:underline"
              >
                Tạo ngẫu nhiên
              </button>
            </div>

            <div className="relative">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                <KeyRound className="w-4 h-4" />
              </div>
              <input
                type={showPassword ? 'text' : 'password'}
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Nhập mật khẩu (tối thiểu 6 ký tự)..."
                className="w-full rounded-xl border border-slate-200 pl-9 pr-20 py-2.5 font-mono text-xs focus:outline-hidden focus:ring-2 focus:ring-emerald-500 transition"
              />
              <div className="absolute inset-y-0 right-0 pr-2 flex items-center space-x-1">
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="p-1 text-slate-400 hover:text-slate-600 rounded"
                >
                  {showPassword ? <EyeOff className="w-3.5 h-3.5" /> : <Eye className="w-3.5 h-3.5" />}
                </button>
                <button
                  type="button"
                  onClick={handleCopyPassword}
                  className="p-1 text-slate-400 hover:text-slate-600 rounded"
                  title="Sao chép mật khẩu"
                >
                  {copied ? <Check className="w-3.5 h-3.5 text-emerald-600" /> : <Copy className="w-3.5 h-3.5" />}
                </button>
              </div>
            </div>
            <p className="text-[11px] text-slate-500">
              Tên tài khoản đăng nhập là: <strong className="font-mono text-slate-800">{proposal.email}</strong>
            </p>
          </div>

          {/* Ghi chú phê duyệt */}
          <div className="space-y-1">
            <label className="font-semibold text-slate-700 block">
              Ghi chú phê duyệt của Admin (tùy chọn):
            </label>
            <textarea
              rows={2}
              value={adminNotes}
              onChange={(e) => setAdminNotes(e.target.value)}
              placeholder="VD: Đã thẩm định chứng chỉ nghiệp vụ đấu thầu và đối chiếu lý lịch chuyên gia hợp lệ."
              className="w-full rounded-xl border border-slate-200 p-2.5 focus:outline-hidden focus:ring-2 focus:ring-emerald-500 transition resize-none text-xs"
            />
          </div>

          {/* Nút hành động */}
          <div className="pt-2 flex items-center justify-end space-x-2 border-t border-slate-100">
            <button
              type="button"
              onClick={onClose}
              disabled={submitting}
              className="px-4 py-2 font-semibold text-slate-600 hover:bg-slate-100 rounded-xl transition border border-slate-200"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={submitting}
              className="inline-flex items-center space-x-1.5 px-4 py-2 font-bold text-white bg-emerald-600 hover:bg-emerald-700 rounded-xl transition shadow-xs disabled:opacity-50"
            >
              <CheckCircle2 className="w-4 h-4" />
              <span>{submitting ? 'Đang tạo tài khoản...' : 'Phê duyệt & Tạo tài khoản'}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ApproveProposalModal;
