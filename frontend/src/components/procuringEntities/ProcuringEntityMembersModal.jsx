import React, { useState, useEffect, useCallback } from 'react';
import toast from 'react-hot-toast';
import {
  Users,
  UserPlus,
  Mail,
  ShieldCheck,
  X,
  Trash2,
  Edit2,
  CheckCircle2,
  Clock,
  AlertTriangle,
  RefreshCw,
  Building2,
  Send,
  UserX,
  KeyRound,
  Scale,
  FileCheck
} from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { procuringEntityApi } from '../../api/procuringEntityApi';
import { formatDate } from '../../utils/formatters';
import LoadingSpinner from '../common/LoadingSpinner';

const ROLE_OPTIONS = [
  { value: 1, name: 'Officer', label: 'Cán bộ kỹ thuật / Hiện trường (Maker)' },
  { value: 2, name: 'Finance', label: 'Kế toán / Kiểm soát tài chính' },
  { value: 0, name: 'Manager', label: 'Quản lý đơn vị / Điều hành' },
];

const ROLE_LABEL_MAP = {
  0: 'Quản lý đơn vị (Manager)',
  Manager: 'Quản lý đơn vị (Manager)',
  1: 'Cán bộ kỹ thuật / Hiện trường (Maker)',
  Officer: 'Cán bộ kỹ thuật / Hiện trường (Maker)',
  2: 'Kế toán / Kiểm soát tài chính',
  Finance: 'Kế toán / Kiểm soát tài chính',
  3: 'Người ký duyệt giải ngân (Checker)',
  Signer: 'Người ký duyệt giải ngân (Checker)',
};

const MEMBER_STATUS_MAP = {
  0: 'Đang hoạt động',
  Active: 'Đang hoạt động',
  1: 'Tạm ngưng',
  Suspended: 'Tạm ngưng',
  2: 'Đã thu hồi quyền',
  Revoked: 'Đã thu hồi quyền',
};

const INVITATION_STATUS_MAP = {
  0: 'Chờ chấp nhận',
  Pending: 'Chờ chấp nhận',
  1: 'Đã tham gia',
  Accepted: 'Đã tham gia',
  2: 'Đã hủy / Hết hạn',
  Revoked: 'Đã hủy / Hết hạn',
  Expired: 'Đã hết hạn',
};

const AUTHORITY_STATUS_MAP = {
  0: 'Chờ Admin xác minh',
  PendingVerification: 'Chờ Admin xác minh',
  1: 'Đang có hiệu lực',
  Active: 'Đang có hiệu lực',
  2: 'Đã từ chối',
  Rejected: 'Đã từ chối',
  3: 'Đã thu hồi',
  Revoked: 'Đã thu hồi',
  4: 'Đã hết hạn',
  Expired: 'Đã hết hạn',
};

const AUTHORITY_TYPE_MAP = {
  0: 'Phê duyệt thanh toán / Giải ngân (Checker)',
  DisbursementApproval: 'Phê duyệt thanh toán / Giải ngân (Checker)',
  1: 'Ký kết hợp đồng kinh tế',
  ContractSigning: 'Ký kết hợp đồng kinh tế',
  2: 'Người đại diện theo pháp luật',
  FullLegalRepresentative: 'Người đại diện theo pháp luật',
};

export const ProcuringEntityMembersModal = ({
  isOpen,
  onClose,
  entity,
  canManage: canManageProp,
  inline = false,
}) => {
  const { user: currentUser } = useAuth();
  const isAdmin = currentUser?.role === 'Admin';
  const [activeTab, setActiveTab] = useState('members'); // 'members' | 'invitations' | 'authorities'
  const [loading, setLoading] = useState(false);
  const [members, setMembers] = useState([]);
  const [invitations, setInvitations] = useState([]);
  const [authorities, setAuthorities] = useState([]);

  const isMemberManager = members.some(
    (m) =>
      m.userId === currentUser?.id &&
      (m.roleInEntity === 0 || m.roleInEntity === 'Manager') &&
      (m.status === 0 || m.status === 'Active')
  );
  const canManage =
    isAdmin ||
    entity?.userId === currentUser?.id ||
    isMemberManager ||
    Boolean(canManageProp && isAdmin);

  // Form đề nghị cấp thẩm quyền ký (Signing Authority Request)
  const [showReqAuthForm, setShowReqAuthForm] = useState(false);
  const [reqAuthUserId, setReqAuthUserId] = useState('');
  const [reqAuthType, setReqAuthType] = useState(0); // 0: DisbursementApproval, 1: ContractSigning, 2: FullLegalRepresentative
  const [reqAuthScope, setReqAuthScope] = useState('EntityWide');
  const [reqAuthValidFrom, setReqAuthValidFrom] = useState('');
  const [reqAuthValidTo, setReqAuthValidTo] = useState('');
  const [reqAuthDecisionNumber, setReqAuthDecisionNumber] = useState('');
  const [reqAuthLegalBasis, setReqAuthLegalBasis] = useState('');
  const [submittingAuth, setSubmittingAuth] = useState(false);

  // Form mời thành viên mới
  const [inviteEmail, setInviteEmail] = useState('');
  const [inviteRole, setInviteRole] = useState(1); // Mặc định: Officer (Maker)
  const [inviting, setInviting] = useState(false);

  // Modal đổi vai trò
  const [editingMember, setEditingMember] = useState(null);
  const [newRole, setNewRole] = useState(1);
  const [updatingRole, setUpdatingRole] = useState(false);

  const fetchMembersAndInvitations = useCallback(async () => {
    if (!entity?.id) return;
    setLoading(true);
    try {
      const [membersRes, invRes, authRes] = await Promise.all([
        procuringEntityApi.getMembers(entity.id),
        procuringEntityApi.getInvitations(entity.id).catch(() => ({ success: false, data: [] })),
        procuringEntityApi.getSigningAuthorities(entity.id).catch(() => ({ success: false, data: [] })),
      ]);

      if (membersRes?.success) {
        setMembers(membersRes.data || []);
      }
      if (invRes?.success) {
        setInvitations(invRes.data || []);
      }
      if (authRes?.success) {
        setAuthorities(authRes.data || []);
      }
    } catch (err) {
      toast.error('Không thể tải danh sách thành viên: ' + (err.message || 'Lỗi mạng'));
    } finally {
      setLoading(false);
    }
  }, [entity?.id]);

  useEffect(() => {
    if (isOpen && entity?.id) {
      fetchMembersAndInvitations();
    }
  }, [isOpen, entity?.id, fetchMembersAndInvitations]);

  const handleSendInvitation = async (e) => {
    e.preventDefault();
    if (!inviteEmail.trim()) {
      toast.error('Vui lòng nhập email người nhận.');
      return;
    }

    try {
      setInviting(true);
      const res = await procuringEntityApi.sendInvitation(entity.id, {
        email: inviteEmail.trim(),
        roleInEntity: Number(inviteRole),
      });

      if (res?.success) {
        toast.success(`Đã gửi thư mời tham gia tới ${inviteEmail.trim()}`);
        setInviteEmail('');
        fetchMembersAndInvitations();
        setActiveTab('invitations');
      } else {
        toast.error(res?.message || 'Không thể gửi thư mời.');
      }
    } catch (err) {
      toast.error(err.message || 'Có lỗi khi gửi lời mời.');
    } finally {
      setInviting(false);
    }
  };

  const handleRevokeInvitation = async (invitationId, email) => {
    if (!window.confirm(`Bạn có chắc chắn muốn hủy lời mời gửi tới ${email}?`)) {
      return;
    }

    try {
      const res = await procuringEntityApi.revokeInvitation(entity.id, invitationId);
      if (res?.success) {
        toast.success('Đã hủy lời mời thành công.');
        fetchMembersAndInvitations();
      } else {
        toast.error(res?.message || 'Không thể hủy lời mời.');
      }
    } catch (err) {
      toast.error(err.message || 'Lỗi khi hủy lời mời.');
    }
  };

  const handleUpdateRole = async (e) => {
    e.preventDefault();
    if (!editingMember) return;

    try {
      setUpdatingRole(true);
      const res = await procuringEntityApi.updateMemberRole(entity.id, editingMember.id, {
        roleInEntity: Number(newRole),
      });

      if (res?.success) {
        toast.success(`Đã cập nhật vai trò cho ${editingMember.fullName}`);
        setEditingMember(null);
        fetchMembersAndInvitations();
      } else {
        toast.error(res?.message || 'Không thể cập nhật vai trò.');
      }
    } catch (err) {
      toast.error(err.message || 'Lỗi khi cập nhật vai trò.');
    } finally {
      setUpdatingRole(false);
    }
  };

  const handleRevokeMember = async (member) => {
    if (
      !window.confirm(
        `Xác nhận thu hồi quyền của thành viên ${member.fullName} (${member.email}) khỏi đơn vị?`
      )
    ) {
      return;
    }

    try {
      const res = await procuringEntityApi.revokeMember(entity.id, member.id);
      if (res?.success) {
        toast.success('Đã thu hồi quyền thành viên.');
        fetchMembersAndInvitations();
      } else {
        toast.error(res?.message || 'Không thể thu hồi quyền.');
      }
    } catch (err) {
      toast.error(err.message || 'Lỗi khi thu hồi thành viên.');
    }
  };


  const handleRequestAuthority = async (e) => {
    e.preventDefault();
    if (!reqAuthUserId) {
      toast.error('Vui lòng chọn nhân sự được phân công thẩm quyền.');
      return;
    }
    if (!reqAuthDecisionNumber.trim()) {
      toast.error('Vui lòng nhập số quyết định hoặc văn bản ủy quyền.');
      return;
    }
    if (!reqAuthLegalBasis.trim()) {
      toast.error('Vui lòng nhập căn cứ pháp lý / nội dung ủy quyền.');
      return;
    }
    if (!reqAuthValidFrom || !reqAuthValidTo) {
      toast.error('Vui lòng chọn đầy đủ thời hạn hiệu lực.');
      return;
    }
    if (new Date(reqAuthValidTo) <= new Date(reqAuthValidFrom)) {
      toast.error('Ngày kết thúc hiệu lực phải sau ngày bắt đầu hiệu lực.');
      return;
    }

    try {
      setSubmittingAuth(true);
      const res = await procuringEntityApi.requestSigningAuthority(entity.id, {
        userId: Number(reqAuthUserId),
        authorityType: Number(reqAuthType),
        scope: reqAuthScope.trim() || 'EntityWide',
        validFrom: new Date(reqAuthValidFrom).toISOString(),
        validTo: new Date(reqAuthValidTo).toISOString(),
        decisionNumber: reqAuthDecisionNumber.trim(),
        legalBasis: reqAuthLegalBasis.trim(),
      });

      if (res?.success) {
        toast.success('Lập hồ sơ đề nghị cấp thẩm quyền ký thành công. Đang chờ Quản trị viên hệ thống thẩm định.');
        setShowReqAuthForm(false);
        setReqAuthDecisionNumber('');
        setReqAuthLegalBasis('');
        setReqAuthValidFrom('');
        setReqAuthValidTo('');
        fetchMembersAndInvitations();
      } else {
        toast.error(res?.message || 'Không thể gửi đề nghị thẩm quyền.');
      }
    } catch (err) {
      toast.error(err.message || 'Lỗi khi gửi đề nghị cấp thẩm quyền.');
    } finally {
      setSubmittingAuth(false);
    }
  };

  const handleVerifyAuthority = async (authorityId, isApproved) => {
    const note = window.prompt(
      isApproved
        ? 'Nhập ghi chú thẩm định phê duyệt (tùy chọn):'
        : 'Nhập lý do từ chối hồ sơ thẩm quyền:'
    );
    if (!isApproved && !note) {
      toast.error('Vui lòng nêu rõ lý do từ chối.');
      return;
    }

    try {
      const res = await procuringEntityApi.verifySigningAuthority(authorityId, {
        isApproved,
        verificationNotes: note || '',
      });
      if (res?.success) {
        toast.success(isApproved ? 'Đã phê duyệt thẩm quyền ký.' : 'Đã từ chối cấp thẩm quyền.');
        fetchMembersAndInvitations();
      } else {
        toast.error(res?.message || 'Không thể thẩm định hồ sơ.');
      }
    } catch (err) {
      toast.error(err.message || 'Lỗi khi thẩm định hồ sơ.');
    }
  };

  const handleRevokeAuthority = async (authorityId, name) => {
    const reason = window.prompt(`Xác nhận thu hồi thẩm quyền của ${name}. Vui lòng nhập lý do thu hồi:`);
    if (!reason) return;

    try {
      const res = await procuringEntityApi.revokeSigningAuthority(authorityId, {
        revocationReason: reason.trim(),
      });
      if (res?.success) {
        toast.success('Đã thu hồi thẩm quyền ký duyệt.');
        fetchMembersAndInvitations();
      } else {
        toast.error(res?.message || 'Không thể thu hồi thẩm quyền.');
      }
    } catch (err) {
      toast.error(err.message || 'Lỗi khi thu hồi thẩm quyền.');
    }
  };

  if ((!isOpen && !inline) || !entity) return null;

  const containerClasses = inline
    ? 'bg-white rounded-2xl shadow-2xs border border-slate-200 w-full overflow-hidden flex flex-col'
    : 'bg-white rounded-3xl shadow-2xl border border-slate-200 w-full max-w-4xl overflow-hidden animate-in fade-in zoom-in duration-200 flex flex-col max-h-[90vh]';

  const content = (
    <div className={containerClasses}>
      {/* Header */}
      <div className="px-6 py-4 bg-gradient-to-r from-slate-900 via-indigo-950 to-slate-900 text-white flex items-center justify-between flex-shrink-0">
        <div className="flex items-center space-x-3.5">
          <div className="p-2.5 bg-white/10 rounded-2xl backdrop-blur-md">
            <Users className="w-5 h-5 text-indigo-300" />
          </div>
          <div>
            <div className="flex items-center space-x-2">
              <h3 className="text-base font-bold">Danh sách Thành viên & Phân quyền Nghiệp vụ</h3>
              <span className="px-2 py-0.5 rounded-full text-[10px] font-black bg-indigo-500/20 text-indigo-200 border border-indigo-400/30">
                Maker - Checker
              </span>
            </div>
            <p className="text-xs text-slate-300">
              {entity.organizationName || entity.name} (MST: {entity.taxCode || '---'})
            </p>
          </div>
        </div>
        {!inline && onClose && (
          <button
            onClick={onClose}
            className="text-white/70 hover:text-white p-1.5 rounded-xl hover:bg-white/10 transition cursor-pointer"
          >
            <X className="w-5 h-5" />
          </button>
        )}
      </div>

        {/* Tab Navigation */}
        <div className="px-6 pt-4 pb-2 border-b border-slate-200 bg-slate-50/70 flex items-center justify-between flex-shrink-0">
          <div className="flex space-x-2">
            <button
              type="button"
              onClick={() => setActiveTab('members')}
              className={`px-4 py-2 rounded-xl text-xs font-bold transition flex items-center space-x-2 cursor-pointer ${
                activeTab === 'members'
                  ? 'bg-white text-indigo-700 shadow-xs border border-slate-200'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
              }`}
            >
              <Users className="w-4 h-4" />
              <span>Thành viên hiện tại ({members.length})</span>
            </button>
            <button
              type="button"
              onClick={() => setActiveTab('invitations')}
              className={`px-4 py-2 rounded-xl text-xs font-bold transition flex items-center space-x-2 cursor-pointer ${
                activeTab === 'invitations'
                  ? 'bg-white text-indigo-700 shadow-xs border border-slate-200'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
              }`}
            >
              <Mail className="w-4 h-4" />
              <span>Lời mời đã gửi ({invitations.length})</span>
            </button>
            <button
              type="button"
              onClick={() => setActiveTab('authorities')}
              className={`px-4 py-2 rounded-xl text-xs font-bold transition flex items-center space-x-2 cursor-pointer ${
                activeTab === 'authorities'
                  ? 'bg-white text-indigo-700 shadow-xs border border-slate-200'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
              }`}
            >
              <KeyRound className="w-4 h-4" />
              <span>Thẩm quyền ký duyệt ({authorities.length})</span>
            </button>
          </div>

          <button
            type="button"
            onClick={fetchMembersAndInvitations}
            disabled={loading}
            className="p-2 text-slate-500 hover:text-indigo-600 hover:bg-indigo-50 rounded-xl transition cursor-pointer"
            title="Làm mới"
          >
            <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
          </button>
        </div>

        {/* Body content */}
        <div className="p-6 overflow-y-auto flex-1 space-y-6">
          {/* TAB 1: THÀNH VIÊN HIỆN TẠI */}
          {activeTab === 'members' && (
            <div className="space-y-4">
              {/* Form mời thành viên (Nếu có quyền Quản lý) */}
              {canManage && (
                <form
                  onSubmit={handleSendInvitation}
                  className="p-4 bg-indigo-50/40 rounded-2xl border border-indigo-100 space-y-3"
                >
                  <div className="flex items-center space-x-2 text-xs font-bold text-indigo-900">
                    <UserPlus className="w-4 h-4 text-indigo-600" />
                    <span>Mời thành viên mới vào đơn vị</span>
                  </div>
                  <div className="grid grid-cols-1 sm:grid-cols-12 gap-3">
                    <div className="sm:col-span-6">
                      <input
                        type="email"
                        required
                        placeholder="Nhập địa chỉ email người nhận (VD: canbo@donvi.vn)"
                        value={inviteEmail}
                        onChange={(e) => setInviteEmail(e.target.value)}
                        className="w-full px-3.5 py-2.5 bg-white border border-slate-200 rounded-xl text-xs focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                      />
                    </div>
                    <div className="sm:col-span-4">
                      <select
                        value={inviteRole}
                        onChange={(e) => setInviteRole(Number(e.target.value))}
                        className="w-full px-3.5 py-2.5 bg-white border border-slate-200 rounded-xl text-xs focus:ring-2 focus:ring-indigo-500 focus:outline-none font-semibold text-slate-700"
                      >
                        {ROLE_OPTIONS.map((opt) => (
                          <option key={opt.value} value={opt.value}>
                            {opt.label}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className="sm:col-span-2">
                      <button
                        type="submit"
                        disabled={inviting}
                        className="w-full py-2.5 px-3 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl text-xs font-bold transition shadow-xs flex items-center justify-center space-x-1 disabled:opacity-50 cursor-pointer"
                      >
                        <Send className="w-3.5 h-3.5" />
                        <span>{inviting ? 'Gửi...' : 'Mời'}</span>
                      </button>
                    </div>
                  </div>
                </form>
              )}

              {/* Bảng thành viên */}
              {loading && members.length === 0 ? (
                <div className="py-12 text-center">
                  <LoadingSpinner />
                </div>
              ) : members.length === 0 ? (
                <div className="py-12 text-center text-slate-500 text-xs">
                  Chưa có thành viên nào được đăng ký trong đơn vị.
                </div>
              ) : (
                <div className="overflow-x-auto border border-slate-200 rounded-2xl shadow-2xs">
                  <table className="w-full text-left text-xs">
                    <thead className="bg-slate-50 border-b border-slate-200 text-slate-600 font-bold">
                      <tr>
                        <th className="py-3 px-4">Thành viên</th>
                        <th className="py-3 px-4">Vai trò Maker - Checker</th>
                        <th className="py-3 px-4">Trạng thái</th>
                        <th className="py-3 px-4">Ngày tham gia</th>
                        {canManage && <th className="py-3 px-4 text-right">Thao tác</th>}
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {members.map((m) => (
                        <tr key={m.id} className="hover:bg-slate-50/60 transition">
                          <td className="py-3.5 px-4">
                            <div className="font-bold text-slate-900">{m.fullName}</div>
                            <div className="text-[11px] text-slate-500">{m.email}</div>
                          </td>
                          <td className="py-3.5 px-4">
                            <span
                              className={`inline-flex items-center space-x-1 px-2.5 py-1 rounded-lg text-[11px] font-bold ${
                                m.roleInEntity === 0 || m.roleInEntity === 'Manager'
                                  ? 'bg-purple-50 text-purple-700 border border-purple-200'
                                  : m.roleInEntity === 1 || m.roleInEntity === 'Officer'
                                  ? 'bg-blue-50 text-blue-700 border border-blue-200'
                                  : m.roleInEntity === 3 || m.roleInEntity === 'Signer'
                                  ? 'bg-emerald-50 text-emerald-700 border border-emerald-200'
                                  : 'bg-amber-50 text-amber-700 border border-amber-200'
                              }`}
                            >
                              <ShieldCheck className="w-3 h-3" />
                              <span>{ROLE_LABEL_MAP[m.roleInEntityName] || ROLE_LABEL_MAP[m.roleInEntity] || m.roleInEntityName || m.roleInEntity}</span>
                            </span>
                          </td>
                          <td className="py-3.5 px-4">
                            <span
                              className={`inline-flex px-2 py-0.5 rounded-full text-[10px] font-bold ${
                                m.status === 'Active' || m.status === 0
                                  ? 'bg-emerald-100 text-emerald-800'
                                  : m.status === 'Suspended' || m.status === 1
                                  ? 'bg-amber-100 text-amber-800'
                                  : 'bg-rose-100 text-rose-800'
                              }`}
                            >
                              {MEMBER_STATUS_MAP[m.statusName] || MEMBER_STATUS_MAP[m.status] || m.statusName || m.status}
                            </span>
                          </td>
                          <td className="py-3.5 px-4 text-slate-500 whitespace-nowrap">
                            {formatDate(m.joinedAt)}
                          </td>
                          {canManage && (
                            <td className="py-3.5 px-4 text-right space-x-1">
                              <button
                                type="button"
                                onClick={() => {
                                  setEditingMember(m);
                                  const rValue =
                                    typeof m.roleInEntity === 'number'
                                      ? m.roleInEntity
                                      : ROLE_OPTIONS.find((r) => r.name === m.roleInEntity)?.value || 1;
                                  setNewRole(rValue);
                                }}
                                className="p-1.5 text-slate-500 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition cursor-pointer"
                                title="Đổi vai trò"
                              >
                                <Edit2 className="w-3.5 h-3.5" />
                              </button>
                              <button
                                type="button"
                                onClick={() => handleRevokeMember(m)}
                                className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition cursor-pointer"
                                title="Thu hồi quyền"
                              >
                                <UserX className="w-3.5 h-3.5" />
                              </button>
                            </td>
                          )}
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          )}

          {/* TAB 2: LỜI MỜI ĐÃ GỬI */}
          {activeTab === 'invitations' && (
            <div className="space-y-4">
              {loading && invitations.length === 0 ? (
                <div className="py-12 text-center">
                  <LoadingSpinner />
                </div>
              ) : invitations.length === 0 ? (
                <div className="py-12 text-center text-slate-500 text-xs">
                  Chưa có lời mời nào đang chờ xử lý.
                </div>
              ) : (
                <div className="overflow-x-auto border border-slate-200 rounded-2xl shadow-2xs">
                  <table className="w-full text-left text-xs">
                    <thead className="bg-slate-50 border-b border-slate-200 text-slate-600 font-bold">
                      <tr>
                        <th className="py-3 px-4">Email nhận</th>
                        <th className="py-3 px-4">Vai trò dự kiến</th>
                        <th className="py-3 px-4">Người gửi</th>
                        <th className="py-3 px-4">Trạng thái</th>
                        <th className="py-3 px-4">Hạn kích hoạt</th>
                        {canManage && <th className="py-3 px-4 text-right">Thao tác</th>}
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {invitations.map((inv) => (
                        <tr key={inv.id} className="hover:bg-slate-50/60 transition">
                          <td className="py-3.5 px-4 font-bold text-slate-900">{inv.email}</td>
                          <td className="py-3.5 px-4">
                            <span className="font-semibold text-indigo-700">
                              {ROLE_LABEL_MAP[inv.roleInEntityName] || ROLE_LABEL_MAP[inv.roleInEntity] || inv.roleInEntityName || inv.roleInEntity}
                            </span>
                          </td>
                          <td className="py-3.5 px-4 text-slate-600">{inv.invitedByName}</td>
                          <td className="py-3.5 px-4">
                            <span
                              className={`inline-flex px-2 py-0.5 rounded-full text-[10px] font-bold ${
                                inv.status === 'Accepted' || inv.status === 1
                                  ? 'bg-emerald-100 text-emerald-800'
                                  : inv.status === 'Pending' || inv.status === 0
                                  ? 'bg-amber-100 text-amber-800'
                                  : 'bg-rose-100 text-rose-800'
                              }`}
                            >
                              {INVITATION_STATUS_MAP[inv.statusName] || INVITATION_STATUS_MAP[inv.status] || inv.statusName || inv.status}
                            </span>
                          </td>
                          <td className="py-3.5 px-4 text-slate-500 whitespace-nowrap">
                            {formatDate(inv.expiresAt)}
                          </td>
                          {canManage && (
                            <td className="py-3.5 px-4 text-right">
                              {(inv.status === 'Pending' || inv.status === 0) && (
                                <button
                                  type="button"
                                  onClick={() => handleRevokeInvitation(inv.id, inv.email)}
                                  className="px-2.5 py-1 text-[11px] font-bold text-rose-600 hover:bg-rose-50 rounded-lg transition border border-rose-200 cursor-pointer"
                                >
                                  Hủy lời mời
                                </button>
                              )}
                            </td>
                          )}
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          )}

          {/* TAB 3: THẨM QUYỀN KÝ DUYỆT (SIGNING AUTHORITIES) */}
          {activeTab === 'authorities' && (
            <div className="space-y-4">
              {/* Nút bật form đề nghị thẩm quyền */}
              <div className="flex items-center justify-between">
                <div>
                  <h4 className="text-xs font-bold text-slate-800 flex items-center gap-1.5">
                    <KeyRound className="w-4 h-4 text-indigo-600" />
                    <span>Hồ Sơ Thẩm Quyền Ký Duyệt Giải Ngân (Checker)</span>
                  </h4>
                  <p className="text-[11px] text-slate-500">
                    Phân định thẩm quyền ký duyệt độc lập dựa trên Quyết định / Văn bản ủy quyền pháp lý
                  </p>
                </div>
                {canManage && (
                  <button
                    type="button"
                    onClick={() => setShowReqAuthForm(!showReqAuthForm)}
                    className="px-3.5 py-1.5 text-xs font-bold bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl shadow-xs transition flex items-center gap-1.5 cursor-pointer"
                  >
                    <Scale className="w-3.5 h-3.5" />
                    <span>{showReqAuthForm ? 'Đóng form' : 'Lập đề nghị thẩm quyền'}</span>
                  </button>
                )}
              </div>

              {/* Form lập đề nghị cấp thẩm quyền */}
              {showReqAuthForm && (
                <form
                  onSubmit={handleRequestAuthority}
                  className="p-4 bg-indigo-50/50 border border-indigo-200/80 rounded-2xl space-y-3 animate-in fade-in duration-150"
                >
                  <div className="flex items-center justify-between border-b border-indigo-100 pb-2">
                    <span className="text-xs font-bold text-indigo-900">
                      Hồ Sơ Đề Nghị Cấp Thẩm Quyền Ký Duyệt
                    </span>
                    <span className="text-[10px] font-bold text-amber-800 bg-amber-100 px-2 py-0.5 rounded-full">
                      Cần Admin phê duyệt
                    </span>
                  </div>

                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
                    <div>
                      <label className="font-bold text-slate-700 block mb-1">
                        Nhân sự được ủy quyền <span className="text-rose-500">*</span>
                      </label>
                      <select
                        value={reqAuthUserId}
                        onChange={(e) => setReqAuthUserId(e.target.value)}
                        required
                        className="w-full px-3 py-2 bg-white border border-slate-200 rounded-xl font-medium focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                      >
                        <option value="">-- Chọn thành viên đơn vị --</option>
                        {members
                          .filter((m) => m.status === 'Active' || m.status === 0)
                          .map((m) => (
                            <option key={m.userId} value={m.userId}>
                              {m.fullName} ({m.email})
                            </option>
                          ))}
                      </select>
                    </div>

                    <div>
                      <label className="font-bold text-slate-700 block mb-1">
                        Loại thẩm quyền <span className="text-rose-500">*</span>
                      </label>
                      <select
                        value={reqAuthType}
                        onChange={(e) => setReqAuthType(Number(e.target.value))}
                        className="w-full px-3 py-2 bg-white border border-slate-200 rounded-xl font-medium focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                      >
                        <option value={0}>Thẩm quyền Phê duyệt thanh toán / Giải ngân</option>
                        <option value={1}>Thẩm quyền Ký kết hợp đồng kinh tế</option>
                        <option value={2}>Người đại diện theo pháp luật của Bên mời thầu</option>
                      </select>
                    </div>

                    <div>
                      <label className="font-bold text-slate-700 block mb-1">
                        Số Quyết định / Văn bản ủy quyền <span className="text-rose-500">*</span>
                      </label>
                      <input
                        type="text"
                        value={reqAuthDecisionNumber}
                        onChange={(e) => setReqAuthDecisionNumber(e.target.value)}
                        placeholder="VD: QĐ-15/2026/TCT-UQ"
                        required
                        className="w-full px-3 py-2 bg-white border border-slate-200 rounded-xl font-medium focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                      />
                    </div>

                    <div>
                      <label className="font-bold text-slate-700 block mb-1">
                        Phạm vi thẩm quyền
                      </label>
                      <input
                        type="text"
                        value={reqAuthScope}
                        onChange={(e) => setReqAuthScope(e.target.value)}
                        placeholder="EntityWide hoặc Contract:{id}"
                        className="w-full px-3 py-2 bg-white border border-slate-200 rounded-xl font-medium focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                      />
                    </div>

                    <div>
                      <label className="font-bold text-slate-700 block mb-1">
                        Ngày bắt đầu hiệu lực <span className="text-rose-500">*</span>
                      </label>
                      <input
                        type="date"
                        value={reqAuthValidFrom}
                        onChange={(e) => setReqAuthValidFrom(e.target.value)}
                        required
                        className="w-full px-3 py-2 bg-white border border-slate-200 rounded-xl font-medium focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                      />
                    </div>

                    <div>
                      <label className="font-bold text-slate-700 block mb-1">
                        Ngày kết thúc hiệu lực <span className="text-rose-500">*</span>
                      </label>
                      <input
                        type="date"
                        value={reqAuthValidTo}
                        onChange={(e) => setReqAuthValidTo(e.target.value)}
                        required
                        className="w-full px-3 py-2 bg-white border border-slate-200 rounded-xl font-medium focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                      />
                    </div>

                    <div className="sm:col-span-2">
                      <label className="font-bold text-slate-700 block mb-1">
                        Căn cứ pháp lý / Nội dung phân quyền <span className="text-rose-500">*</span>
                      </label>
                      <textarea
                        value={reqAuthLegalBasis}
                        onChange={(e) => setReqAuthLegalBasis(e.target.value)}
                        rows={2}
                        placeholder="Căn cứ Điều lệ tổ chức và Quyết định số... về việc phân công quyền ký duyệt tài chính hợp đồng..."
                        required
                        className="w-full px-3 py-2 bg-white border border-slate-200 rounded-xl font-medium focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                      />
                    </div>
                  </div>

                  <div className="flex justify-end gap-2 pt-2 border-t border-indigo-100">
                    <button
                      type="button"
                      onClick={() => setShowReqAuthForm(false)}
                      disabled={submittingAuth}
                      className="px-3.5 py-1.5 text-xs font-semibold text-slate-600 hover:bg-white rounded-xl transition cursor-pointer"
                    >
                      Hủy
                    </button>
                    <button
                      type="submit"
                      disabled={submittingAuth}
                      className="px-4 py-1.5 text-xs font-bold bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl transition shadow-xs cursor-pointer flex items-center gap-1.5"
                    >
                      {submittingAuth ? 'Đang gửi...' : 'Gửi Đề Nghị Lên Admin'}
                    </button>
                  </div>
                </form>
              )}

              {/* Danh sách thẩm quyền */}
              {authorities.length === 0 ? (
                <div className="py-12 text-center text-slate-500 text-xs border-2 border-dashed border-slate-200 rounded-2xl">
                  Chưa có hồ sơ thẩm quyền ký nào được thiết lập.
                </div>
              ) : (
                <div className="space-y-3">
                  {authorities.map((a) => {
                    const isActive = a.status === 'Active' || a.status === 1;
                    const isPending = a.status === 'PendingVerification' || a.status === 0;
                    return (
                      <div
                        key={a.id}
                        className={`p-4 rounded-2xl border transition text-xs space-y-2.5 ${
                          isActive
                            ? 'bg-emerald-50/20 border-emerald-200'
                            : isPending
                            ? 'bg-amber-50/20 border-amber-200'
                            : 'bg-slate-50/60 border-slate-200'
                        }`}
                      >
                        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                          <div className="flex items-center space-x-2">
                            <span className="font-bold text-slate-900 text-sm">{a.fullName}</span>
                            <span className="text-slate-500">({a.email})</span>
                            <span
                              className={`px-2 py-0.5 rounded-full text-[10px] font-black ${
                                isActive
                                  ? 'bg-emerald-100 text-emerald-800'
                                  : isPending
                                  ? 'bg-amber-100 text-amber-800'
                                  : a.status === 'Revoked' || a.status === 3
                                  ? 'bg-rose-100 text-rose-800'
                                  : 'bg-slate-100 text-slate-700'
                              }`}
                            >
                              {AUTHORITY_STATUS_MAP[a.statusName] || AUTHORITY_STATUS_MAP[a.status] || a.statusName || a.status}
                            </span>
                          </div>

                          <div className="flex items-center space-x-2">
                            <span className="px-2 py-0.5 rounded-md text-[10px] font-bold bg-indigo-50 text-indigo-700 border border-indigo-200">
                              {AUTHORITY_TYPE_MAP[a.authorityTypeName] || AUTHORITY_TYPE_MAP[a.authorityType] || a.authorityTypeName || a.authorityType}
                            </span>
                            <span className="px-2 py-0.5 rounded-md text-[10px] font-bold bg-slate-100 text-slate-700 border border-slate-200">
                              Phạm vi: {a.scope === 'EntityWide' ? 'Toàn đơn vị' : a.scope}
                            </span>
                          </div>
                        </div>

                        <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 text-[11px] text-slate-600 bg-white/70 p-2.5 rounded-xl border border-slate-100">
                          <div>
                            • Số quyết định: <strong className="text-slate-800">{a.decisionNumber}</strong>
                          </div>
                          <div>
                            • Hiệu lực:{' '}
                            <strong className="text-slate-800">
                              {formatDate(a.validFrom)} — {formatDate(a.validTo)}
                            </strong>
                          </div>
                          <div className="sm:col-span-2">
                            • Căn cứ pháp lý: <span className="italic text-slate-700">{a.legalBasis}</span>
                          </div>
                          {a.verificationNotes && (
                            <div className="sm:col-span-2 text-indigo-800 font-medium">
                              • Thẩm định ({a.verifiedByName}): "{a.verificationNotes}"
                            </div>
                          )}
                          {a.revocationReason && (
                            <div className="sm:col-span-2 text-rose-800 font-medium">
                              • Lý do thu hồi: "{a.revocationReason}"
                            </div>
                          )}
                        </div>

                        {/* Action buttons cho Admin & Manager */}
                        <div className="flex justify-end items-center space-x-2 pt-1 border-t border-slate-100">
                          {isAdmin && isPending && (
                            <>
                              <button
                                type="button"
                                onClick={() => handleVerifyAuthority(a.id, true)}
                                className="px-3 py-1 text-[11px] font-bold bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg transition cursor-pointer"
                              >
                                Phê duyệt thẩm quyền
                              </button>
                              <button
                                type="button"
                                onClick={() => handleVerifyAuthority(a.id, false)}
                                className="px-3 py-1 text-[11px] font-bold text-rose-600 hover:bg-rose-50 border border-rose-200 rounded-lg transition cursor-pointer"
                              >
                                Từ chối
                              </button>
                            </>
                          )}

                          {(isActive || isPending) && (canManage || isAdmin) && (
                            <button
                              type="button"
                              onClick={() => handleRevokeAuthority(a.id, a.fullName)}
                              className="px-2.5 py-1 text-[11px] font-bold text-rose-600 hover:bg-rose-50 rounded-lg transition cursor-pointer"
                            >
                              Thu hồi
                            </button>
                          )}
                        </div>
                      </div>
                    );
                  })}
                </div>
              )}
            </div>
          )}
        </div>

        {/* Modal chỉnh sửa vai trò */}
        {editingMember && (
          <div className="fixed inset-0 z-60 bg-slate-900/40 backdrop-blur-xs flex items-center justify-center p-4">
            <div className="bg-white rounded-2xl shadow-xl border border-slate-200 max-w-sm w-full p-6 space-y-4">
              <div className="space-y-1">
                <h4 className="text-sm font-bold text-slate-900">Cập nhật vai trò thành viên</h4>
                <p className="text-xs text-slate-500">
                  {editingMember.fullName} ({editingMember.email})
                </p>
              </div>

              <form onSubmit={handleUpdateRole} className="space-y-4">
                <div className="space-y-1">
                  <label className="text-xs font-bold text-slate-700">Chọn vai trò mới</label>
                  <select
                    value={newRole}
                    onChange={(e) => setNewRole(Number(e.target.value))}
                    className="w-full px-3 py-2 bg-white border border-slate-200 rounded-xl text-xs font-semibold focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                  >
                    {ROLE_OPTIONS.map((opt) => (
                      <option key={opt.value} value={opt.value}>
                        {opt.label}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="flex justify-end space-x-2 pt-2">
                  <button
                    type="button"
                    onClick={() => setEditingMember(null)}
                    disabled={updatingRole}
                    className="px-3.5 py-2 text-xs font-semibold text-slate-600 hover:bg-slate-100 rounded-xl transition cursor-pointer"
                  >
                    Hủy
                  </button>
                  <button
                    type="submit"
                    disabled={updatingRole}
                    className="px-4 py-2 text-xs font-bold bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl transition shadow-xs cursor-pointer"
                  >
                    {updatingRole ? 'Đang lưu...' : 'Lưu thay đổi'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}
      </div>
  );

  if (inline) {
    return content;
  }

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
      {content}
    </div>
  );
};

export default ProcuringEntityMembersModal;
