import axiosClient from './axiosClient';

export const auditLogApi = {
  /**
   * Tra cứu danh sách nhật ký kiểm toán (phân trang, lọc theo Action, Entity, User, khoảng thời gian)
   * @param {Object} params - { pageIndex, pageSize, action, entityType, entityId, userId, fromDate, toDate }
   */
  getAuditLogs: async (params = {}) => {
    return await axiosClient.get('/audit-logs', { params });
  },

  /**
   * Xem chi tiết một bản ghi nhật ký kiểm toán
   * @param {number} id - Mã định danh bản ghi kiểm toán
   */
  getAuditLogById: async (id) => {
    return await axiosClient.get(`/audit-logs/${id}`);
  },
};

export default auditLogApi;
