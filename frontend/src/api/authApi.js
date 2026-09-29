import axiosClient from './axiosClient';

export const authApi = {
  login: async (email, password) => {
    return await axiosClient.post('/auth/login', { email, password });
  },

  registerContractor: async (data) => {
    return await axiosClient.post('/auth/register-contractor', data);
  },

  refreshToken: async (token, refreshToken) => {
    return await axiosClient.post('/auth/refresh-token', { token, refreshToken });
  },
};
