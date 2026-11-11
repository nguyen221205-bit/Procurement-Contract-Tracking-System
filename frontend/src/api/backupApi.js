import axiosClient from './axiosClient';

export const backupApi = {
  /**
   * Lấy tổng quan và danh sách tất cả các bản sao lưu (CSDL và tệp tải lên)
   */
  getOverview: async () => {
    return await axiosClient.get('/admin/backups');
  },

  /**
   * Tạo bản sao lưu toàn vẹn cho cơ sở dữ liệu SQL Server
   * @param {Object} data - { description }
   */
  createDatabaseBackup: async (data = {}) => {
    return await axiosClient.post('/admin/backups/database', data);
  },

  /**
   * Tạo bản sao lưu nén toàn bộ thư mục tệp đính kèm uploads thành ZIP
   * @param {Object} data - { description }
   */
  createDocumentsBackup: async (data = {}) => {
    return await axiosClient.post('/admin/backups/documents', data);
  },

  /**
   * Tải về tệp sao lưu (.bak hoặc .zip)
   * @param {string} fileName
   */
  downloadBackup: async (fileName) => {
    return await axiosClient.get(`/admin/backups/${encodeURIComponent(fileName)}/download`, {
      responseType: 'blob',
    });
  },

  /**
   * Phục hồi cơ sở dữ liệu SQL Server từ bản sao lưu đã chọn
   * @param {Object} data - { fileName, confirmPhrase: "RESTORE_DATABASE_CONFIRM" }
   */
  restoreDatabaseBackup: async (data) => {
    return await axiosClient.post('/admin/backups/database/restore', data);
  },

  /**
   * Xóa một bản sao lưu khỏi máy chủ
   * @param {string} fileName
   */
  deleteBackup: async (fileName) => {
    return await axiosClient.delete(`/admin/backups/${encodeURIComponent(fileName)}`);
  },
};

export default backupApi;
