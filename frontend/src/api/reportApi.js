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
  exportContractorsExcel: async () => {
    return await axiosClient.get('/reports/contractors/export-excel', { responseType: 'blob' });
  },
  exportContractsExcel: async () => {
    return await axiosClient.get('/reports/contracts/export-excel', { responseType: 'blob' });
  },
};

export default reportApi;
