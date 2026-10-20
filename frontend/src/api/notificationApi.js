import axiosClient from './axiosClient';

export const notificationApi = {
  /**
   * Lấy danh sách thông báo của tài khoản hiện tại (có phân trang)
   * @param {number} pageIndex
   * @param {number} pageSize
   */
  getNotifications: async (pageIndex = 1, pageSize = 20) => {
    return await axiosClient.get('/notifications', {
      params: { pageIndex, pageSize },
    });
  },

  /**
   * Lấy số lượng thông báo chưa đọc
   */
  getUnreadCount: async () => {
    return await axiosClient.get('/notifications/unread-count');
  },

  /**
   * Đánh dấu 1 thông báo là đã đọc
   * @param {number} id
   */
  markAsRead: async (id) => {
    return await axiosClient.patch(`/notifications/${id}/read`);
  },

  /**
   * Đánh dấu tất cả thông báo là đã đọc
   */
  markAllAsRead: async () => {
    return await axiosClient.post('/notifications/mark-all-read');
  },
};

export default notificationApi;
