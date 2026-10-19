import axiosClient from './axiosClient';

export const notificationApi = {
  /**
   * Lấy danh sách thông báo của tài khoản hiện tại (có phân trang)
   * @param {number} pageIndex
   * @param {number} pageSize
   */
  getNotifications: async (pageIndex = 1, pageSize = 20) => {
    const response = await axiosClient.get('/notifications', {
      params: { pageIndex, pageSize },
    });
    return response.data;
  },

  /**
   * Lấy số lượng thông báo chưa đọc
   */
  getUnreadCount: async () => {
    const response = await axiosClient.get('/notifications/unread-count');
    return response.data;
  },

  /**
   * Đánh dấu 1 thông báo là đã đọc
   * @param {number} id
   */
  markAsRead: async (id) => {
    const response = await axiosClient.patch(`/notifications/${id}/read`);
    return response.data;
  },

  /**
   * Đánh dấu tất cả thông báo là đã đọc
   */
  markAllAsRead: async () => {
    const response = await axiosClient.post('/notifications/mark-all-read');
    return response.data;
  },
};

export default notificationApi;
