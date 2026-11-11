import React, { useCallback, useEffect, useMemo, useState } from 'react';
import toast from 'react-hot-toast';
import { procuringEntityApi } from '../api/procuringEntityApi';
import { formatDate, formatDateTime } from '../utils/formatters';
import LoadingSpinner from '../components/common/LoadingSpinner';
import {
  Building2,
  Calendar,
  CheckCircle2,
  Clock,
  ExternalLink,
  FileCheck,
  FileText,
  KeyRound,
  RefreshCw,
  Search,
  ShieldCheck,
  UserCheck,
  X,
  XCircle,
} from 'lucide-react';

const AUTHORITY_TYPE_LABELS = {
  0: 'Phê duyệt giải ngân',
  DisbursementApproval: 'Phê duyệt giải ngân',
  1: 'Ký kết hợp đồng',
  ContractSigning: 'Ký kết hợp đồng',
  2: 'Đại diện theo pháp luật',
  FullLegalRepresentative: 'Đại diện theo pháp luật',
};

const isSafeDocumentUrl = (url) => {
  if (!url || typeof url !== 'string') return false;
  const trimmed = url.trim();
  return /^https?:\/\//i.test(trimmed) || trimmed.startsWith('/');
};

export const PendingSigningAuthoritiesPage = () => {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');

  // Modal state cho thao tác Chấp thuận / Từ chối
  const [reviewTarget, setReviewTarget] = useState(null); // { item, isApproved }
  const [verificationNotes, setVerificationNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const fetchPendingAuthorities = useCallback(async (isManual = false) => {
    if (isManual) {
      setRefreshing(true);
    } else {
      setLoading(true);
    }

    try {
      const res = await procuringEntityApi.getPendingSigningAuthorities();
      if (res && res.success) {
        setItems(Array.isArray(res.data) ? res.data : []);
      } else {
        toast.error(res?.message || 'Không thể tải danh sách hồ sơ ủy quyền ký chờ thẩm định.');
      }
    } catch (err) {
      toast.error(err?.message || 'Lỗi kết nối khi tải danh sách ủy quyền ký.');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => {
    fetchPendingAuthorities(false);
  }, [fetchPendingAuthorities]);

  const filteredItems = useMemo(() => {
    const q = searchTerm.trim().toLowerCase();
    if (!q) return items;
    return items.filter((item) => {
      const haystack = [
        item.procuringEntityName,
        item.fullName,
        item.email,
        item.decisionNumber,
        item.authorityTypeName,
        item.legalBasis,
        item.requestedByName,
      ]
        .filter(Boolean)
        .join(' ')
        .toLowerCase();
      return haystack.includes(q);
    });
  }, [items, searchTerm]);

  const summary = useMemo(() => {
    const total = items.length;
    const disbursementCount = items.filter(
      (i) => i.authorityType === 0 || i.authorityType === 'DisbursementApproval'
    ).length;
    const contractOrFullCount = total - disbursementCount;
    return { total, disbursementCount, contractOrFullCount };
  }, [items]);

  const openReviewModal = (item, isApproved) => {
    setReviewTarget({ item, isApproved });
    setVerificationNotes('');
  };

  const closeReviewModal = () => {
    if (submitting) return;
    setReviewTarget(null);
    setVerificationNotes('');
  };

  const handleConfirmVerify = async (e) => {
    e.preventDefault();
    if (!reviewTarget) return;

    const { item, isApproved } = reviewTarget;
    const trimmedNotes = verificationNotes.trim();

    if (!isApproved && !trimmedNotes) {
      toast.error('Vui lòng nhập lý do từ chối hoặc yêu cầu bổ sung hồ sơ.');
      return;
    }

    setSubmitting(true);
    const toastId = toast.loading(
      isApproved ? 'Đang phê duyệt thẩm quyền ký...' : 'Đang ghi nhận từ chối hồ sơ...'
    );

    try {
      const res = await procuringEntityApi.verifySigningAuthority(item.id, {
        isApproved,
        verificationNotes: trimmedNotes || null,
      });

      if (res && res.success) {
        toast.success(
          res.message ||
            (isApproved
              ? 'Đã phê duyệt thẩm quyền ký thành công.'
              : 'Đã từ chối hồ sơ đề nghị cấp thẩm quyền ký.'),
          { id: toastId }
        );
        setItems((prev) => prev.filter((x) => x.id !== item.id));
        setReviewTarget(null);
        setVerificationNotes('');
      } else {
        toast.error(res?.message || 'Thao tác thẩm định không thành công.', { id: toastId });
      }
    } catch (err) {
      toast.error(err?.message || 'Lỗi máy chủ khi thẩm định thẩm quyền ký.', { id: toastId });
    } finally {
      setSubmitting(false);
    }
  };

  const resolveAuthorityLabel = (item) =>
    item.authorityTypeName ||
    AUTHORITY_TYPE_LABELS[item.authorityType] ||
    String(item.authorityType || 'Thẩm quyền ký');

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h2 className="text-2xl font-black text-slate-900 tracking-tight flex items-center gap-2.5">
            <KeyRound className="w-6 h-6 text-indigo-600" />
            <span>Thẩm định Ủy quyền Ký tập trung</span>
          </h2>
          <p className="text-xs text-slate-500 mt-1">
            Kiểm tra hồ sơ pháp lý và phê duyệt đề nghị cấp thẩm quyền ký kết hợp đồng / phê duyệt giải ngân từ các Bên mời thầu toàn quốc
          </p>
        </div>

        <button
          type="button"
          onClick={() => fetchPendingAuthorities(true)}
          disabled={refreshing}
          className="self-start sm:self-auto px-3.5 py-2 bg-white hover:bg-slate-50 text-slate-700 border border-slate-200 rounded-xl text-xs font-semibold shadow-xs transition flex items-center gap-1.5 cursor-pointer disabled:opacity-50"
        >
          <RefreshCw className={`w-3.5 h-3.5 ${refreshing ? 'animate-spin text-indigo-600' : 'text-slate-500'}`} />
          <span>{refreshing ? 'Đang tải...' : 'Làm mới danh sách'}</span>
        </button>
      </div>

      {/* Thẻ tóm tắt nhanh */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="bg-white p-4 rounded-2xl border border-amber-200/80 shadow-2xs flex items-center gap-3.5">
          <div className="w-10 h-10 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center shrink-0">
            <Clock className="w-5 h-5" />
          </div>
          <div>
            <p className="text-xs font-medium text-slate-500">Hồ sơ chờ thẩm định</p>
            <h4 className="text-lg font-extrabold text-slate-900 mt-0.5">{summary.total} hồ sơ</h4>
          </div>
        </div>

        <div className="bg-white p-4 rounded-2xl border border-slate-200 shadow-2xs flex items-center gap-3.5">
          <div className="w-10 h-10 rounded-xl bg-indigo-50 text-indigo-600 flex items-center justify-center shrink-0">
            <FileCheck className="w-5 h-5" />
          </div>
          <div>
            <p className="text-xs font-medium text-slate-500">Phê duyệt giải ngân</p>
            <h4 className="text-lg font-extrabold text-slate-900 mt-0.5">{summary.disbursementCount} đề nghị</h4>
          </div>
        </div>

        <div className="bg-white p-4 rounded-2xl border border-slate-200 shadow-2xs flex items-center gap-3.5">
          <div className="w-10 h-10 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center shrink-0">
            <ShieldCheck className="w-5 h-5" />
          </div>
          <div>
            <p className="text-xs font-medium text-slate-500">Ký hợp đồng / Đại diện PL</p>
            <h4 className="text-lg font-extrabold text-slate-900 mt-0.5">{summary.contractOrFullCount} đề nghị</h4>
          </div>
        </div>
      </div>

      {/* Thanh tìm kiếm */}
      <div className="bg-white p-4 rounded-2xl border border-slate-200 shadow-2xs flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div className="relative flex-1 max-w-md">
          <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
          <input
            type="text"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            placeholder="Tìm theo Bên mời thầu, người được ủy quyền, số quyết định..."
            className="w-full pl-9 pr-3 py-2 text-xs bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500"
          />
        </div>
        <span className="text-xs text-slate-500">
          Hiển thị <strong className="text-slate-800">{filteredItems.length}</strong> / {items.length} hồ sơ chờ duyệt
        </span>
      </div>

      {/* Danh sách hồ sơ */}
      {loading ? (
        <div className="py-20 flex justify-center bg-white rounded-2xl border border-slate-200">
          <LoadingSpinner size="lg" text="Đang tải danh sách hồ sơ ủy quyền ký chờ thẩm định..." />
        </div>
      ) : filteredItems.length === 0 ? (
        <div className="bg-white rounded-2xl border border-slate-200 p-12 text-center space-y-2">
          <div className="w-12 h-12 rounded-2xl bg-emerald-50 text-emerald-600 mx-auto flex items-center justify-center">
            <CheckCircle2 className="w-6 h-6" />
          </div>
          <h3 className="text-sm font-bold text-slate-800">
            {items.length === 0
              ? 'Không có hồ sơ ủy quyền ký nào đang chờ thẩm định'
              : 'Không tìm thấy hồ sơ khớp từ khóa tìm kiếm'}
          </h3>
          <p className="text-xs text-slate-500">
            {items.length === 0
              ? 'Tất cả đề nghị cấp thẩm quyền ký từ các Bên mời thầu đều đã được xử lý.'
              : 'Thử thay đổi hoặc xóa từ khóa tìm kiếm để xem lại toàn bộ danh sách.'}
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {filteredItems.map((item) => {
            const safeUrl = isSafeDocumentUrl(item.documentUrl);
            const scopeDisplay =
              item.scope === 'EntityWide'
                ? 'Toàn đơn vị'
                : item.scope?.startsWith('Contract:')
                ? `Hợp đồng #${item.scope.replace('Contract:', '')}`
                : item.scope || 'Toàn đơn vị';

            return (
              <div
                key={item.id}
                className="bg-white rounded-2xl border border-slate-200 shadow-2xs p-5 space-y-4 hover:border-indigo-200 transition"
              >
                {/* Top row: Entity + Person + Status/Type */}
                <div className="flex flex-col lg:flex-row lg:items-start justify-between gap-3">
                  <div className="space-y-1.5">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-slate-100 text-slate-800 text-xs font-bold">
                        <Building2 className="w-3.5 h-3.5 text-slate-500" />
                        <span>{item.procuringEntityName || `Đơn vị #${item.procuringEntityId}`}</span>
                      </span>
                      <span className="px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-amber-50 text-amber-800 border border-amber-200">
                        {item.statusName || 'Chờ thẩm định'}
                      </span>
                      <span className="px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-indigo-50 text-indigo-700 border border-indigo-200">
                        {resolveAuthorityLabel(item)}
                      </span>
                      <span className="px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-slate-50 text-slate-600 border border-slate-200">
                        Phạm vi: {scopeDisplay}
                      </span>
                    </div>

                    <div className="flex flex-wrap items-center gap-2 pt-0.5">
                      <UserCheck className="w-4 h-4 text-indigo-600 shrink-0" />
                      <span className="text-sm font-bold text-slate-900">{item.fullName}</span>
                      {item.email && <span className="text-xs text-slate-500">({item.email})</span>}
                    </div>
                  </div>

                  {/* Action buttons */}
                  <div className="flex flex-wrap items-center gap-2 shrink-0">
                    <button
                      type="button"
                      onClick={() => openReviewModal(item, true)}
                      className="px-3.5 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl text-xs font-bold shadow-xs transition flex items-center gap-1.5 cursor-pointer"
                    >
                      <CheckCircle2 className="w-4 h-4" />
                      <span>Thẩm định Chấp thuận</span>
                    </button>
                    <button
                      type="button"
                      onClick={() => openReviewModal(item, false)}
                      className="px-3.5 py-2 bg-white hover:bg-rose-50 text-rose-600 border border-rose-200 rounded-xl text-xs font-bold transition flex items-center gap-1.5 cursor-pointer"
                    >
                      <XCircle className="w-4 h-4" />
                      <span>Từ chối / Yêu cầu bổ sung</span>
                    </button>
                  </div>
                </div>

                {/* Detail grid */}
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3 p-3.5 bg-slate-50/80 rounded-xl border border-slate-100 text-xs">
                  <div>
                    <span className="text-[11px] font-medium text-slate-500 block">Số quyết định ủy quyền</span>
                    <span className="font-mono font-bold text-slate-900 mt-0.5 block">
                      {item.decisionNumber || '-'}
                    </span>
                  </div>

                  <div>
                    <span className="text-[11px] font-medium text-slate-500 block">Ngày bắt đầu</span>
                    <span className="font-semibold text-slate-800 mt-0.5 inline-flex items-center gap-1">
                      <Calendar className="w-3.5 h-3.5 text-slate-400" />
                      {formatDate(item.validFrom)}
                    </span>
                  </div>

                  <div>
                    <span className="text-[11px] font-medium text-slate-500 block">Ngày hết hạn</span>
                    <span className="font-semibold text-slate-800 mt-0.5 inline-flex items-center gap-1">
                      <Calendar className="w-3.5 h-3.5 text-slate-400" />
                      {formatDate(item.validTo)}
                    </span>
                  </div>

                  <div>
                    <span className="text-[11px] font-medium text-slate-500 block">Tệp quyết định đính kèm</span>
                    {safeUrl ? (
                      <a
                        href={item.documentUrl.trim()}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="mt-0.5 inline-flex items-center gap-1 font-bold text-sky-600 hover:text-sky-700 hover:underline"
                      >
                        <FileText className="w-3.5 h-3.5" />
                        <span>Xem tài liệu</span>
                        <ExternalLink className="w-3 h-3" />
                      </a>
                    ) : (
                      <span className="mt-0.5 text-slate-400 italic block">
                        {item.documentUrl ? 'Đường dẫn tài liệu không hợp lệ' : 'Chưa đính kèm liên kết tệp'}
                      </span>
                    )}
                  </div>

                  <div className="md:col-span-2 lg:col-span-3">
                    <span className="text-[11px] font-medium text-slate-500 block">Căn cứ pháp lý / Nội dung ủy quyền</span>
                    <p className="text-slate-700 mt-0.5 leading-relaxed">{item.legalBasis || '-'}</p>
                  </div>

                  <div>
                    <span className="text-[11px] font-medium text-slate-500 block">Người gửi đề nghị</span>
                    <span className="text-slate-700 font-medium mt-0.5 block">
                      {item.requestedByName || '-'}
                    </span>
                    <span className="text-[11px] text-slate-400 block">
                      {formatDateTime(item.requestedAt)}
                    </span>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Modal xác nhận thẩm định */}
      {reviewTarget && (
        <div className="fixed inset-0 z-50 bg-slate-900/50 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl shadow-xl border border-slate-200 max-w-lg w-full overflow-hidden">
            <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between">
              <div className="flex items-center gap-2">
                {reviewTarget.isApproved ? (
                  <CheckCircle2 className="w-5 h-5 text-emerald-600" />
                ) : (
                  <XCircle className="w-5 h-5 text-rose-600" />
                )}
                <h3 className="text-sm font-bold text-slate-900">
                  {reviewTarget.isApproved
                    ? 'Xác nhận Chấp thuận Ủy quyền Ký'
                    : 'Từ chối / Yêu cầu Bổ sung Hồ sơ'}
                </h3>
              </div>
              <button
                type="button"
                onClick={closeReviewModal}
                disabled={submitting}
                className="p-1 text-slate-400 hover:text-slate-600 rounded-lg cursor-pointer"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            <form onSubmit={handleConfirmVerify} className="p-6 space-y-4 text-xs">
              <div className="p-3.5 rounded-xl bg-slate-50 border border-slate-200 space-y-1.5">
                <p className="text-slate-700">
                  <span className="text-slate-500">Bên mời thầu:</span>{' '}
                  <strong className="text-slate-900">{reviewTarget.item.procuringEntityName}</strong>
                </p>
                <p className="text-slate-700">
                  <span className="text-slate-500">Người được ủy quyền:</span>{' '}
                  <strong className="text-slate-900">{reviewTarget.item.fullName}</strong>{' '}
                  {reviewTarget.item.email ? `(${reviewTarget.item.email})` : ''}
                </p>
                <p className="text-slate-700">
                  <span className="text-slate-500">Số quyết định &amp; Loại quyền:</span>{' '}
                  <strong className="font-mono text-slate-900">{reviewTarget.item.decisionNumber}</strong> —{' '}
                  {resolveAuthorityLabel(reviewTarget.item)}
                </p>
                <p className="text-slate-700">
                  <span className="text-slate-500">Hiệu lực:</span>{' '}
                  <strong>
                    {formatDate(reviewTarget.item.validFrom)} đến {formatDate(reviewTarget.item.validTo)}
                  </strong>
                </p>
              </div>

              <div className="space-y-1.5">
                <label className="font-bold text-slate-700 block">
                  {reviewTarget.isApproved ? 'Ghi chú thẩm định (tùy chọn)' : 'Lý do từ chối / Yêu cầu bổ sung'}{' '}
                  {!reviewTarget.isApproved && <span className="text-rose-500">*</span>}
                </label>
                <textarea
                  value={verificationNotes}
                  onChange={(e) => setVerificationNotes(e.target.value)}
                  rows={3}
                  required={!reviewTarget.isApproved}
                  maxLength={1000}
                  placeholder={
                    reviewTarget.isApproved
                      ? 'Nhập ghi chú xác nhận đối chiếu văn bản ủy quyền (nếu có)...'
                      : 'Nêu rõ lý do từ chối hoặc tài liệu cần Bên mời thầu bổ sung...'
                  }
                  className="w-full px-3 py-2 bg-white border border-slate-300 rounded-xl focus:outline-none focus:ring-2 focus:ring-indigo-500"
                />
              </div>

              <div className="flex justify-end gap-2 pt-2 border-t border-slate-100">
                <button
                  type="button"
                  onClick={closeReviewModal}
                  disabled={submitting}
                  className="px-4 py-2 text-xs font-semibold text-slate-600 hover:bg-slate-100 rounded-xl transition cursor-pointer"
                >
                  Hủy bỏ
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className={`px-4 py-2 text-xs font-bold text-white rounded-xl transition shadow-xs cursor-pointer disabled:opacity-50 ${
                    reviewTarget.isApproved
                      ? 'bg-emerald-600 hover:bg-emerald-700'
                      : 'bg-rose-600 hover:bg-rose-700'
                  }`}
                >
                  {submitting
                    ? 'Đang xử lý...'
                    : reviewTarget.isApproved
                    ? 'Xác nhận Chấp thuận'
                    : 'Xác nhận Từ chối'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default PendingSigningAuthoritiesPage;

