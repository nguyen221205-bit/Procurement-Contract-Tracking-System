import axiosClient from './axiosClient';

export const bidSubmissionApi = {
  // Dành cho Admin, Procurement, Evaluator
  getSubmissionsByPackage: async (bidPackageId) => {
    return await axiosClient.get(`/bid-packages/${bidPackageId}/submissions`);
  },

  // Dành cho Contractor: Xem danh sách hồ sơ của chính mình
  getMySubmissions: async (params = {}) => {
    return await axiosClient.get('/my-submissions', { params });
  },

  getSubmissionById: async (id) => {
    return await axiosClient.get(`/submissions/${id}`);
  },

  // Nộp hồ sơ dự thầu (FormData)
  submitBid: async (bidPackageId, formData) => {
    return await axiosClient.post(`/bid-packages/${bidPackageId}/submissions`, formData);
  },

  // Rút hồ sơ dự thầu trước thời hạn
  withdrawSubmission: async (id) => {
    return await axiosClient.delete(`/submissions/${id}`);
  },
};

export default bidSubmissionApi;
