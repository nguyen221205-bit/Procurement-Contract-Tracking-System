import axiosClient from './axiosClient';

export const userApi = {
  // Lấy danh sách người dùng có phân trang, tìm kiếm và lọc
  getUsers: async (params = {}) => {
    return await axiosClient.get('/users', { params });
  },

  // Lấy chi tiết thông tin người dùng theo ID
  getUserById: async (id) => {
    return await axiosClient.get(`/users/${id}`);
  },

  // Cập nhật thông tin cơ bản của người dùng (FullName, Phone)
  updateUser: async (id, data) => {
    return await axiosClient.put(`/users/${id}`, data);
  },

  // Khóa hoặc kích hoạt tài khoản
  toggleUserStatus: async (id) => {
    return await axiosClient.patch(`/users/${id}/status`);
  },

  // Phân quyền / Gán danh sách vai trò cho người dùng
  assignRoles: async (id, roles) => {
    return await axiosClient.post(`/users/${id}/roles`, { roles });
  },

  // Lấy danh mục các vai trò có trong hệ thống
  getRoles: async () => {
    return await axiosClient.get('/roles');
  },
};

export default userApi;
