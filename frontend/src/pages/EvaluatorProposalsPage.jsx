import React, { useState, useEffect, useCallback } from 'react';
import { evaluatorProposalApi } from '../api/evaluatorProposalApi';
import { useAuth } from '../context/AuthContext';
import { formatDate } from '../utils/formatters';
import { ROLES } from '../utils/constants';
import StatusBadge from '../components/common/StatusBadge';
import LoadingSpinner from '../components/common/LoadingSpinner';
import ProposeEvaluatorModal from '../components/evaluators/ProposeEvaluatorModal';
import ApproveProposalModal from '../components/evaluators/ApproveProposalModal';
import RejectProposalModal from '../components/evaluators/RejectProposalModal';
import toast from 'react-hot-toast';
import {
  UserCheck,
  UserPlus,
  Search,
  Clock,
  CheckCircle2,
  XCircle,
  FileText,
  ExternalLink,
  ChevronLeft,
  ChevronRight,
  ShieldCheck,
  RotateCcw,
  Building2,
  Mail,
  Phone,
  Briefcase,
  Award,
  AlertCircle
} from 'lucide-react';

export const EvaluatorProposalsPage = () => {
  const { user: currentUser, hasRole } = useAuth();
  const isAdmin = hasRole([ROLES.ADMIN]);
  const isProcurement = hasRole([ROLES.PROCUREMENT]);

  const [proposals, setProposals] = useState([]);
  const [loading, setLoading] = useState(true);
  const [totalCount, setTotalCount] = useState(0);
  const [pageIndex, setPageIndex] = useState(1);
  const [pageSize] = useState(10);

  // Bộ lọc
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState(''); // '', 'Pending', 'Approved', 'Rejected'
  const [pendingCount, setPendingCount] = useState(0);

  // Modals state
  const [isProposeModalOpen, setIsProposeModalOpen] = useState(false);
  const [approvingProposal, setApprovingProposal] = useState(null);
  const [rejectingProposal, setRejectingProposal] = useState(null);

  // Tải danh sách đề xuất
  const fetchProposals = useCallback(async () => {
    setLoading(true);
    try {
      const params = {
        pageIndex,
        pageSize,
        searchTerm: searchTerm.trim() || undefined,
        status: statusFilter || undefined,
      };

      const res = await evaluatorProposalApi.getProposals(params);
      if (res && res.success && res.data) {
        setProposals(res.data.items || []);
        setTotalCount(res.data.totalCount || 0);
      }
    } catch (err) {
      console.error('Lỗi tải danh sách đề xuất giám khảo:', err);
      toast.error('Không thể tải danh sách đề xuất: ' + (err.message || 'Lỗi mạng'));
    } finally {
      setLoading(false);
    }
  }, [pageIndex, pageSize, searchTerm, statusFilter]);

  // Đếm số lượng đề xuất Pending
  const fetchPendingCount = useCallback(async () => {
    try {
      const res = await evaluatorProposalApi.getProposals({
        pageIndex: 1,
        pageSize: 100,
        status: 'Pending',
      });
      if (res && res.success && res.data) {
        setPendingCount(res.data.totalCount || 0);
      }
    } catch (err) {
      console.error('Lỗi lấy số lượng đề xuất chờ duyệt:', err);
    }
  }, []);

  useEffect(() => {
    fetchProposals();
    fetchPendingCount();
  }, [fetchProposals, fetchPendingCount]);

  // Xem tệp chứng chỉ trực tiếp
  const handleViewCertFile = async (e, proposalId) => {
    e.stopPropagation();
    try {
      toast.loading('Đang mở tệp chứng chỉ...', { id: 'quick-cert' });
      const response = await evaluatorProposalApi.downloadCertificateFile(proposalId);
      const fileBlob = new Blob([response], { type: 'application/pdf' });
      const fileUrl = window.URL.createObjectURL(fileBlob);
      window.open(fileUrl, '_blank');
      toast.success('Đã mở tệp chứng chỉ!', { id: 'quick-cert' });
    } catch (err) {
      toast.error('Không thể mở tệp: ' + (err.message || 'Lỗi mạng'), { id: 'quick-cert' });
    }
  };

  const totalPages = Math.ceil(totalCount / pageSize) || 1;

  return (
    <div className="space-y-6">
      {/* Header Phân hệ */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div className="space-y-1">
          <div className="flex items-center space-x-2.5">
            <div className="p-2 bg-indigo-50 text-indigo-600 rounded-xl">
              <Award className="w-6 h-6" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-slate-900">
                {isAdmin
                  ? 'Thẩm định & Cấp tài khoản Giám khảo'
                  : 'Đề xuất Giám khảo / Tổ chuyên gia'}
              </h1>
              <p className="text-xs text-slate-500">
                {isAdmin
                  ? 'Xem xét đề xuất từ Bên mời thầu, kiểm tra Chứng chỉ nghiệp vụ đấu thầu và ấn định mật khẩu tạo tài khoản'
                  : 'Gửi đề xuất chuyên gia kèm Chứng chỉ nghiệp vụ chuyên môn về đấu thầu cho Quản trị viên (Admin) phê duyệt'}
              </p>
            </div>
          </div>
        </div>

        {/* Nút gửi đề xuất (Bên mời thầu hoặc Admin) */}
        {isProcurement && (
          <button
            onClick={() => setIsProposeModalOpen(true)}
            className="inline-flex items-center space-x-2 px-4 py-2.5 text-xs font-bold text-white bg-indigo-600 hover:bg-indigo-700 rounded-xl shadow-xs transition"
          >
            <UserPlus className="w-4 h-4" />
            <span>Đề xuất Giám khảo mới</span>
          </button>
        )}
      </div>

      {/* Thanh công cụ lọc & Tìm kiếm */}
      <div className="bg-white rounded-2xl p-4 border border-slate-200/80 shadow-2xs space-y-4">
        {/* Tab trạng thái */}
        <div className="flex flex-wrap items-center gap-2 border-b border-slate-100 pb-3">
          <button
            onClick={() => {
              setStatusFilter('');
              setPageIndex(1);
            }}
            className={`px-3 py-1.5 text-xs font-semibold rounded-lg transition ${
              statusFilter === ''
                ? 'bg-slate-900 text-white shadow-xs'
                : 'text-slate-600 hover:bg-slate-100'
            }`}
          >
            Tất cả đề xuất
          </button>

          <button
            onClick={() => {
              setStatusFilter('Pending');
              setPageIndex(1);
            }}
            className={`inline-flex items-center space-x-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg transition ${
              statusFilter === 'Pending'
                ? 'bg-amber-500 text-white shadow-xs'
                : 'text-amber-700 bg-amber-50/70 hover:bg-amber-100 border border-amber-200/60'
            }`}
          >
            <Clock className="w-3.5 h-3.5" />
            <span>Chờ thẩm định</span>
            {pendingCount > 0 && (
              <span className="ml-1 px-1.5 py-0.2 rounded-full text-[10px] font-bold bg-amber-200 text-amber-900 animate-pulse">
                {pendingCount}
              </span>
            )}
          </button>

          <button
            onClick={() => {
              setStatusFilter('Approved');
              setPageIndex(1);
            }}
            className={`inline-flex items-center space-x-1 px-3 py-1.5 text-xs font-semibold rounded-lg transition ${
              statusFilter === 'Approved'
                ? 'bg-emerald-600 text-white shadow-xs'
                : 'text-emerald-700 bg-emerald-50/70 hover:bg-emerald-100 border border-emerald-200/60'
            }`}
          >
            <CheckCircle2 className="w-3.5 h-3.5" />
            <span>Đã phê duyệt</span>
          </button>

          <button
            onClick={() => {
              setStatusFilter('Rejected');
              setPageIndex(1);
            }}
            className={`inline-flex items-center space-x-1 px-3 py-1.5 text-xs font-semibold rounded-lg transition ${
              statusFilter === 'Rejected'
                ? 'bg-rose-600 text-white shadow-xs'
                : 'text-rose-700 bg-rose-50/70 hover:bg-rose-100 border border-rose-200/60'
            }`}
          >
            <XCircle className="w-3.5 h-3.5" />
            <span>Đã từ chối</span>
          </button>
        </div>

        {/* Ô tìm kiếm */}
        <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3">
          <div className="relative flex-1 max-w-md">
            <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Tìm kiếm theo Tên chuyên gia, Email, Chuyên môn, Nơi công tác..."
              className="w-full text-xs pl-9 pr-3 py-2 rounded-xl border border-slate-200 focus:outline-hidden focus:ring-2 focus:ring-sky-500 transition"
            />
          </div>

          <div className="flex items-center space-x-2">
            {(searchTerm || statusFilter) && (
              <button
                onClick={() => {
                  setSearchTerm('');
                  setStatusFilter('');
                  setPageIndex(1);
                }}
                className="inline-flex items-center space-x-1 px-3 py-2 text-xs font-medium text-slate-500 hover:text-slate-800 hover:bg-slate-100 rounded-xl transition"
              >
                <RotateCcw className="w-3.5 h-3.5" />
                <span>Đặt lại</span>
              </button>
            )}

            <span className="text-xs text-slate-500 font-medium">
              Tổng số: <strong className="text-slate-800">{totalCount}</strong> đề xuất
            </span>
          </div>
        </div>
      </div>

      {/* Bảng danh sách đề xuất */}
      <div className="bg-white rounded-2xl border border-slate-200/80 shadow-2xs overflow-hidden">
        {loading ? (
          <div className="py-20 flex justify-center">
            <LoadingSpinner text="Đang tải danh sách đề xuất..." />
          </div>
        ) : proposals.length === 0 ? (
          <div className="py-16 text-center space-y-2">
            <UserCheck className="w-10 h-10 text-slate-300 mx-auto" />
            <p className="text-sm font-semibold text-slate-600">Không tìm thấy đề xuất nào</p>
            <p className="text-xs text-slate-400">
              {isProcurement
                ? 'Bạn chưa gửi đề xuất giám khảo nào. Hãy bấm "Đề xuất Giám khảo mới" để bắt đầu.'
                : 'Hiện tại chưa có đề xuất nào phù hợp với bộ lọc.'}
            </p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse text-xs">
              <thead>
                <tr className="bg-slate-50/80 border-b border-slate-200/80 text-slate-500 uppercase tracking-wider text-[11px] font-bold">
                  <th className="py-3 px-4">Chuyên gia / Giám khảo</th>
                  <th className="py-3 px-4">Chuyên môn & Đơn vị</th>
                  <th className="py-3 px-4">Chứng chỉ nghiệp vụ</th>
                  <th className="py-3 px-4">Người đề xuất</th>
                  <th className="py-3 px-4 text-center">Trạng thái</th>
                  <th className="py-3 px-4 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {proposals.map((p) => (
                  <tr key={p.id} className="hover:bg-slate-50/60 transition group">
                    {/* Chuyên gia */}
                    <td className="py-3.5 px-4 max-w-xs">
                      <div className="space-y-1">
                        <div className="font-bold text-slate-900 leading-snug">
                          {p.fullName}
                        </div>
                        <div className="flex items-center space-x-1.5 text-[11px] text-slate-500">
                          <Mail className="w-3 h-3 text-slate-400 flex-shrink-0" />
                          <span className="font-mono text-slate-700 truncate">{p.email}</span>
                        </div>
                        {p.phone && (
                          <div className="flex items-center space-x-1.5 text-[11px] text-slate-500">
                            <Phone className="w-3 h-3 text-slate-400 flex-shrink-0" />
                            <span className="font-mono">{p.phone}</span>
                          </div>
                        )}
                      </div>
                    </td>

                    {/* Chuyên môn & Đơn vị */}
                    <td className="py-3.5 px-4 max-w-xs">
                      <div className="space-y-1 text-slate-600">
                        <div className="font-semibold text-slate-900">
                          {p.specialization || 'Chưa cập nhật'}
                        </div>
                        {p.workplace && (
                          <div className="flex items-start space-x-1 text-[11px] text-slate-500 truncate" title={p.workplace}>
                            <Briefcase className="w-3 h-3 text-slate-400 flex-shrink-0 mt-0.5" />
                            <span className="truncate">{p.workplace}</span>
                          </div>
                        )}
                        {p.experienceYears && (
                          <div className="text-[11px] text-slate-500">
                            Kinh nghiệm: <strong className="text-slate-700">{p.experienceYears} năm</strong>
                          </div>
                        )}
                      </div>
                    </td>

                    {/* Chứng chỉ nghiệp vụ */}
                    <td className="py-3.5 px-4 max-w-xs">
                      <div className="space-y-1">
                        <div className="text-[11px] font-medium text-slate-700 truncate" title={p.certificateName}>
                          {p.certificateName || 'Chứng chỉ đấu thầu'}
                        </div>
                        {p.certificateFile ? (
                          <button
                            type="button"
                            onClick={(e) => handleViewCertFile(e, p.id)}
                            className="inline-flex items-center space-x-1 px-2.5 py-1 text-[11px] font-semibold text-sky-700 bg-sky-50 hover:bg-sky-100 rounded-lg border border-sky-200 transition"
                          >
                            <FileText className="w-3.5 h-3.5 text-sky-600" />
                            <span>Xem tệp chứng chỉ</span>
                            <ExternalLink className="w-3 h-3 text-sky-400" />
                          </button>
                        ) : (
                          <span className="text-slate-400 italic text-[11px]">Không có file</span>
                        )}
                      </div>
                    </td>

                    {/* Người đề xuất */}
                    <td className="py-3.5 px-4">
                      <div className="space-y-0.5 text-[11px] text-slate-600">
                        <span className="font-semibold text-slate-800 block">
                          {p.proposerName || 'Bên mời thầu'}
                        </span>
                        <span className="text-slate-400 block">
                          {formatDate(p.createdAt)}
                        </span>
                      </div>
                    </td>

                    {/* Trạng thái */}
                    <td className="py-3.5 px-4 text-center">
                      <div className="space-y-1">
                        <StatusBadge
                          status={p.status}
                          type="evaluator_proposal"
                        />
                        {p.adminNotes && (
                          <p className="text-[10px] text-slate-500 italic truncate max-w-[150px]" title={p.adminNotes}>
                            "{p.adminNotes}"
                          </p>
                        )}
                      </div>
                    </td>

                    {/* Thao tác */}
                    <td className="py-3.5 px-4 text-right">
                      {isAdmin && p.status === 'Pending' ? (
                        <div className="inline-flex items-center space-x-1.5">
                          <button
                            onClick={() => setRejectingProposal(p)}
                            className="px-2.5 py-1.5 rounded-lg text-xs font-bold text-rose-700 bg-rose-50 hover:bg-rose-100 border border-rose-200 transition"
                          >
                            Từ chối
                          </button>
                          <button
                            onClick={() => setApprovingProposal(p)}
                            className="px-3 py-1.5 rounded-lg text-xs font-bold text-white bg-emerald-600 hover:bg-emerald-700 shadow-2xs transition"
                          >
                            Phê duyệt
                          </button>
                        </div>
                      ) : (
                        <span className="text-[11px] text-slate-400">
                          {p.status === 'Approved'
                            ? `Đã duyệt (${p.reviewedByName || 'Admin'})`
                            : p.status === 'Rejected'
                            ? `Đã từ chối`
                            : 'Đang chờ xử lý'}
                        </span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* Phân trang */}
        {totalCount > pageSize && (
          <div className="px-4 py-3 bg-slate-50/70 border-t border-slate-200/80 flex items-center justify-between text-xs text-slate-500">
            <span>
              Trang <strong>{pageIndex}</strong> / <strong>{totalPages}</strong> (Hiển thị {proposals.length} đề xuất)
            </span>

            <div className="flex items-center space-x-1.5">
              <button
                disabled={pageIndex <= 1}
                onClick={() => setPageIndex((p) => p - 1)}
                className="p-1.5 rounded-lg border border-slate-200 bg-white hover:bg-slate-100 disabled:opacity-40 transition"
              >
                <ChevronLeft className="w-4 h-4" />
              </button>
              <button
                disabled={pageIndex >= totalPages}
                onClick={() => setPageIndex((p) => p + 1)}
                className="p-1.5 rounded-lg border border-slate-200 bg-white hover:bg-slate-100 disabled:opacity-40 transition"
              >
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Modal đề xuất cho Procurement */}
      <ProposeEvaluatorModal
        isOpen={isProposeModalOpen}
        onClose={() => setIsProposeModalOpen(false)}
        onSuccess={() => {
          fetchProposals();
          fetchPendingCount();
        }}
      />

      {/* Modal Admin phê duyệt */}
      <ApproveProposalModal
        isOpen={Boolean(approvingProposal)}
        onClose={() => setApprovingProposal(null)}
        proposal={approvingProposal}
        onSuccess={() => {
          fetchProposals();
          fetchPendingCount();
        }}
      />

      {/* Modal Admin từ chối */}
      <RejectProposalModal
        isOpen={Boolean(rejectingProposal)}
        onClose={() => setRejectingProposal(null)}
        proposal={rejectingProposal}
        onSuccess={() => {
          fetchProposals();
          fetchPendingCount();
        }}
      />
    </div>
  );
};

export default EvaluatorProposalsPage;
