import axiosClient from './axiosClient';

export const procuringEntityApi = {
  // Lấy danh sách Bên mời thầu (phân trang, lọc theo keyword, taxCode, verificationStatus)
  getProcuringEntities: async (params = {}) => {
    return await axiosClient.get('/procuring-entities', { params });
  },

  // Lấy chi tiết thông tin hồ sơ Bên mời thầu theo ID
  getProcuringEntityById: async (id) => {
    return await axiosClient.get(`/procuring-entities/${id}`);
  },

  // Bên mời thầu tự xem thông tin hồ sơ của chính mình
  getMyProfile: async () => {
    return await axiosClient.get('/procuring-entities/me');
  },

  // Thẩm định và phê duyệt hoặc từ chối hồ sơ Bên mời thầu (Chỉ Admin)
  verifyProcuringEntity: async (id, data) => {
    return await axiosClient.patch(`/procuring-entities/${id}/verify`, data);
  },

  // Tải tệp Quyết định thành lập / Giấy phép (PDF)
  downloadEstablishmentFile: async (id) => {
    return await axiosClient.get(`/procuring-entities/${id}/establishment-file`, {
      responseType: 'blob',
    });
  },

  // Tải tệp Quyết định bổ nhiệm (nếu có)
  downloadAppointmentFile: async (id) => {
    return await axiosClient.get(`/procuring-entities/${id}/appointment-file`, {
      responseType: 'blob',
    });
  },

  // Quản lý thành viên & Lời mời nội bộ (Maker - Checker Membership)
  getMembers: async (entityId) => {
    return await axiosClient.get(`/procuring-entities/${entityId}/members`);
  },

  getInvitations: async (entityId) => {
    return await axiosClient.get(`/procuring-entities/${entityId}/invitations`);
  },

  sendInvitation: async (entityId, data) => {
    return await axiosClient.post(`/procuring-entities/${entityId}/invitations`, data);
  },

  revokeInvitation: async (entityId, invitationId) => {
    return await axiosClient.delete(`/procuring-entities/${entityId}/invitations/${invitationId}`);
  },

  updateMemberRole: async (entityId, memberId, data) => {
    return await axiosClient.put(`/procuring-entities/${entityId}/members/${memberId}`, data);
  },

  revokeMember: async (entityId, memberId) => {
    return await axiosClient.delete(`/procuring-entities/${entityId}/members/${memberId}`);
  },

  getInvitationDetails: async (token) => {
    return await axiosClient.get(`/procuring-entities/invitations/${token}`);
  },

  acceptInvitation: async (data) => {
    return await axiosClient.post('/procuring-entities/invitations/accept', data);
  },

  // Quản lý thẩm quyền ký duyệt (Signing Authorities - Transparency & Integrity Control)
  getSigningAuthorities: async (entityId) => {
    return await axiosClient.get(`/procuring-entities/${entityId}/signing-authorities`);
  },

  requestSigningAuthority: async (entityId, data) => {
    return await axiosClient.post(`/procuring-entities/${entityId}/signing-authorities`, data);
  },

  getPendingSigningAuthorities: async () => {
    return await axiosClient.get('/signing-authorities/pending');
  },

  getSigningAuthorityById: async (id) => {
    return await axiosClient.get(`/signing-authorities/${id}`);
  },

  verifySigningAuthority: async (id, data) => {
    return await axiosClient.put(`/signing-authorities/${id}/verify`, data);
  },

  revokeSigningAuthority: async (id, data) => {
    return await axiosClient.put(`/signing-authorities/${id}/revoke`, data);
  },
};

export default procuringEntityApi;
