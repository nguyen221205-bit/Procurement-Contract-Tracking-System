import axiosClient from './axiosClient';

export const bidPackageApi = {
  getPackages: async (params = {}) => {
    return await axiosClient.get('/bid-packages', { params });
  },

  getAll: async (params = {}) => {
    return await axiosClient.get('/bid-packages', { params });
  },

  getPackageById: async (id) => {
    return await axiosClient.get(`/bid-packages/${id}`);
  },

  getById: async (id) => {
    return await axiosClient.get(`/bid-packages/${id}`);
  },

  createPackage: async (data) => {
    return await axiosClient.post('/bid-packages', data);
  },

  changeStatus: async (id, newStatus, reason = null) => {
    const payload =
      typeof newStatus === 'object' && newStatus !== null
        ? newStatus
        : { newStatus, reason };
    return await axiosClient.put(`/bid-packages/${id}/status`, payload);
  },

  uploadDocuments: async (id, formData) => {
    return await axiosClient.post(`/bid-packages/${id}/documents`, formData);
  },

  deleteDocument: async (packageId, documentId) => {
    return await axiosClient.delete(`/bid-packages/${packageId}/documents/${documentId}`);
  },

  getEvaluators: async (id) => {
    return await axiosClient.get(`/bid-packages/${id}/evaluators`);
  },

  assignEvaluator: async (id, data) => {
    return await axiosClient.post(`/bid-packages/${id}/evaluators`, data);
  },

  removeEvaluator: async (packageId, evaluatorId) => {
    return await axiosClient.delete(`/bid-packages/${packageId}/evaluators/${evaluatorId}`);
  },

  // Mở lại gói thầu đã đóng (Closed -> Open) kèm thời hạn mới và lý do gia hạn
  reopenPackage: async (id, data) => {
    return await axiosClient.put(`/bid-packages/${id}/status`, {
      newStatus: 0, // 0 = Open trong Enum BidPackageStatus
      newDeadline: data.newDeadline,
      reason: data.reason,
    });
  },

  // R4-8: Thay thế giám khảo trong Tổ chấm thầu (quy tắc N22)
  replaceEvaluator: async (packageId, data) => {
    return await axiosClient.post(`/bid-packages/${packageId}/evaluators/replace`, data);
  },

  // Lấy danh sách yêu cầu phê duyệt thay đổi trạng thái của gói thầu
  getStatusApprovals: async (packageId) => {
    return await axiosClient.get(`/bid-packages/${packageId}/status-approvals`);
  },

  // Lấy các yêu cầu phê duyệt trạng thái đang chờ xử lý (Toàn hệ thống cho Admin)
  getPendingStatusApprovals: async () => {
    return await axiosClient.get('/bid-packages/status-approvals/pending');
  },

  // Admin xem xét phê duyệt hoặc từ chối yêu cầu thay đổi trạng thái gói thầu
  reviewStatusApproval: async (requestId, data) => {
    return await axiosClient.post(`/bid-packages/status-approvals/${requestId}/review`, data);
  },
};

export default bidPackageApi;
