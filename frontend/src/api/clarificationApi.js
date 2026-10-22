import axiosClient from './axiosClient';

export const clarificationApi = {
  // Lấy danh sách làm rõ của gói thầu
  getClarifications: async (packageId, type = null) => {
    const params = type !== null && type !== undefined ? { type } : {};
    return await axiosClient.get(`/bid-packages/${packageId}/clarifications`, { params });
  },

  // Nhà thầu đặt câu hỏi làm rõ HSMT
  createPackageClarification: async (packageId, data) => {
    return await axiosClient.post(`/bid-packages/${packageId}/clarifications/package`, data);
  },

  // Giám khảo / Bên mời thầu yêu cầu làm rõ HSDT
  createSubmissionClarification: async (packageId, data) => {
    return await axiosClient.post(`/bid-packages/${packageId}/clarifications/submission`, data);
  },

  // Tải lên tệp đính kèm câu hỏi làm rõ hoặc giải trình (F5-8)
  uploadAttachment: async (file) => {
    const formData = new FormData();
    formData.append('file', file);
    return await axiosClient.post('/clarifications/attachments', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  // Phản hồi làm rõ HSMT hoặc gửi văn bản giải trình HSDT
  answerClarification: async (clarificationId, data) => {
    return await axiosClient.post(`/clarifications/${clarificationId}/answer`, data);
  },
};

export default clarificationApi;
