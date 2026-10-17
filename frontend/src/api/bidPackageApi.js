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

  changeStatus: async (id, newStatus) => {
    return await axiosClient.put(`/bid-packages/${id}/status`, { newStatus });
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
};

export default bidPackageApi;
