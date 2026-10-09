import axiosClient from './axiosClient';

export const evaluatorProposalApi = {
  // Lấy danh sách đề xuất Giám khảo (Admin: tất cả; Procurement: của mình)
  getProposals: async (params = {}) => {
    return await axiosClient.get('/evaluator-proposals', { params });
  },

  // Xem chi tiết đề xuất theo ID
  getProposalById: async (id) => {
    return await axiosClient.get(`/evaluator-proposals/${id}`);
  },

  // Bên mời thầu gửi đề xuất Giám khảo mới (multipart/form-data)
  createProposal: async (formData) => {
    return await axiosClient.post('/evaluator-proposals', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  // Quản trị viên (Admin) phê duyệt đề xuất và ấn định mật khẩu khởi tạo tài khoản
  approveProposal: async (id, data) => {
    return await axiosClient.post(`/evaluator-proposals/${id}/approve`, data);
  },

  // Quản trị viên (Admin) từ chối đề xuất kèm lý do giải trình
  rejectProposal: async (id, data) => {
    return await axiosClient.post(`/evaluator-proposals/${id}/reject`, data);
  },

  // Tải về hoặc xem tệp Chứng chỉ nghiệp vụ đấu thầu
  downloadCertificateFile: async (id) => {
    return await axiosClient.get(`/evaluator-proposals/${id}/certificate-file`, {
      responseType: 'blob',
    });
  },
};

export default evaluatorProposalApi;
