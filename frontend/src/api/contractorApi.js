import axiosClient from './axiosClient';

export const contractorApi = {
  // Lấy danh sách nhà thầu (phân trang, tìm kiếm, lọc theo VerificationStatus, Rating)
  getContractors: async (params = {}) => {
    return await axiosClient.get('/contractors', { params });
  },

  // Lấy chi tiết thông tin hồ sơ nhà thầu theo ID
  getContractorById: async (id) => {
    return await axiosClient.get(`/contractors/${id}`);
  },

  // Thẩm định và phê duyệt hoặc từ chối hồ sơ năng lực nhà thầu (Chỉ Admin)
  verifyContractor: async (id, data) => {
    return await axiosClient.patch(`/contractors/${id}/verify`, data);
  },

  // Cập nhật điểm uy tín nhà thầu (Rating)
  updateRating: async (id, data) => {
    return await axiosClient.patch(`/contractors/${id}/rating`, data);
  },

  // Tính toán lại rating nhà thầu dựa trên lịch sử hợp đồng và nghiệm thu
  recalculateRating: async (id) => {
    return await axiosClient.post(`/contractors/${id}/recalculate-rating`);
  },

  // Tải tệp Giấy phép đăng ký kinh doanh (GPKD)
  downloadLicenseFile: async (id) => {
    return await axiosClient.get(`/contractors/${id}/license-file`, {
      responseType: 'blob',
    });
  },
};

export default contractorApi;
