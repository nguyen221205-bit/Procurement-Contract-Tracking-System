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
};

export default procuringEntityApi;
