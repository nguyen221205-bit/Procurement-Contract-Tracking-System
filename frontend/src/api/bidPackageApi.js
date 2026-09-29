import axiosClient from './axiosClient';

export const bidPackageApi = {
  getPackages: async (params = {}) => {
    return await axiosClient.get('/bid-packages', { params });
  },

  getPackageById: async (id) => {
    return await axiosClient.get(`/bid-packages/${id}`);
  },

  createPackage: async (data) => {
    return await axiosClient.post('/bid-packages', data);
  },

  changeStatus: async (id, newStatus) => {
    return await axiosClient.put(`/bid-packages/${id}/status`, { newStatus });
  },

  uploadDocuments: async (id, formData) => {
    return await axiosClient.post(`/bid-packages/${id}/documents`, formData);
  },

  deleteDocument: async (packageId, documentId) => {
    return await axiosClient.delete(`/bid-packages/${packageId}/documents/${documentId}`);
  },
};

export default bidPackageApi;
