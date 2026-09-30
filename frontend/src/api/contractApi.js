import axiosClient from './axiosClient';

export const contractApi = {
  // Lấy danh sách hợp đồng (hỗ trợ phân trang, tìm kiếm, lọc trạng thái)
  getContracts: async (params = {}) => {
    return await axiosClient.get('/contracts', { params });
  },

  // Xem chi tiết hợp đồng kèm mốc thanh toán
  getContractById: async (id) => {
    return await axiosClient.get(`/contracts/${id}`);
  },

  // Tra cứu hợp đồng theo nhà thầu
  getContractsByContractor: async (contractorId) => {
    return await axiosClient.get(`/contracts/contractor/${contractorId}`);
  },

  // Lấy dữ liệu trúng thầu bàn giao sang hợp đồng (Pre-fill)
  getAwardedBidForContract: async (packageId) => {
    return await axiosClient.get(`/contracts/awarded-bid/${packageId}`);
  },

  // Tạo mới hợp đồng kinh tế từ kết quả trúng thầu
  createContract: async (data) => {
    return await axiosClient.post('/contracts', data);
  },

  // Cập nhật hợp đồng (khi ở trạng thái Draft)
  updateContract: async (id, data) => {
    return await axiosClient.put(`/contracts/${id}`, data);
  },

  // Chuyển trạng thái hợp đồng (Draft -> Active -> Completed / Terminated)
  changeStatus: async (id, newStatus, note = null) => {
    return await axiosClient.put(`/contracts/${id}/status`, { newStatus, note });
  },

  // Tải lên file PDF hợp đồng scan có chữ ký
  uploadScannedFile: async (id, file) => {
    const formData = new FormData();
    formData.append('file', file);
    return await axiosClient.post(`/contracts/${id}/scanned-file`, formData);
  },

  // Quản lý mốc thanh toán (Milestones)
  addMilestone: async (contractId, data) => {
    return await axiosClient.post(`/contracts/${contractId}/milestones`, data);
  },

  updateMilestone: async (contractId, milestoneId, data) => {
    return await axiosClient.put(`/contracts/${contractId}/milestones/${milestoneId}`, data);
  },

  deleteMilestone: async (contractId, milestoneId) => {
    return await axiosClient.delete(`/contracts/${contractId}/milestones/${milestoneId}`);
  },

  // Phê duyệt biên bản nghiệm thu mốc thanh toán
  approveMilestoneAcceptance: async (milestoneId, isApproved = true, note = '') => {
    return await axiosClient.put(`/contracts/milestones/${milestoneId}/acceptance`, {
      isApproved,
      note,
    });
  },

  // Báo cáo tiến độ (Nhà thầu)
  addProgressUpdate: async (contractId, data) => {
    return await axiosClient.post(`/contracts/${contractId}/progress`, data);
  },

  addMilestoneProgressUpdate: async (milestoneId, data) => {
    return await axiosClient.post(`/contracts/milestones/${milestoneId}/progress`, data);
  },
};

export default contractApi;
