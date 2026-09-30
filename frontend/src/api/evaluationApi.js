import axiosClient from './axiosClient';

export const evaluationApi = {
  getCriteriaByPackage: async (packageId) => {
    return await axiosClient.get(`/evaluations/packages/${packageId}/criteria`);
  },

  createCriteria: async (packageId, data) => {
    return await axiosClient.post(`/evaluations/packages/${packageId}/criteria`, data);
  },

  updateCriteria: async (criteriaId, data) => {
    return await axiosClient.put(`/evaluations/criteria/${criteriaId}`, data);
  },

  deleteCriteria: async (criteriaId) => {
    return await axiosClient.delete(`/evaluations/criteria/${criteriaId}`);
  },

  getSummary: async (packageId) => {
    return await axiosClient.get(`/evaluations/packages/${packageId}/summary`);
  },

  finalizeEvaluation: async (packageId) => {
    return await axiosClient.post(`/evaluations/packages/${packageId}/finalize`);
  },
};

export default evaluationApi;
