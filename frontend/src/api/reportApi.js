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
  // R4-8: 3 API báo cáo thống kê chuyên sâu
  getProcurementSummary: async (params) => {
    return await axiosClient.get('/reports/procurement-summary', { params });
  },
  getContractorPerformance: async (params) => {
    return await axiosClient.get('/reports/contractor-performance', { params });
  },
  getContractsProgress: async (params) => {
    return await axiosClient.get('/reports/contracts-progress', { params });
  },
};

export default reportApi;
