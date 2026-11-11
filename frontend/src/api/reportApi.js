import axiosClient from './axiosClient';

export const reportApi = {
  // Backend tự xác định đơn vị mời thầu / vai trò Admin từ JWT claims (cô lập đa đơn vị)
  getDashboard: async () => {
    return await axiosClient.get('/reports/dashboard');
  },
  exportContractorsCsv: async () => {
    return await axiosClient.get('/reports/contractors/export', { responseType: 'blob' });
  },
  // Cô lập danh sách hợp đồng theo đơn vị của tài khoản Procurement
  exportContractsCsv: async () => {
    return await axiosClient.get('/reports/contracts/export', { responseType: 'blob' });
  },
  exportContractorsExcel: async () => {
    return await axiosClient.get('/reports/contractors/export-excel', { responseType: 'blob' });
  },
  // Cô lập danh sách hợp đồng Excel theo đơn vị của tài khoản Procurement
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
