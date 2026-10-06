import axiosClient from './axiosClient';

export const reportApi = {
  getDashboard: async () => {
    return await axiosClient.get('/reports/dashboard');
  },
  exportContractorsCsv: async () => {
    return await axiosClient.get('/reports/contractors/export', { responseType: 'blob' });
  },
  exportContractsCsv: async () => {
    return await axiosClient.get('/reports/contracts/export', { responseType: 'blob' });
  },
};
