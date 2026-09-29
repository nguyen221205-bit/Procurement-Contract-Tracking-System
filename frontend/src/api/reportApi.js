import axiosClient from './axiosClient';

export const reportApi = {
  getDashboard: async () => {
    return await axiosClient.get('/reports/dashboard');
  },
};
