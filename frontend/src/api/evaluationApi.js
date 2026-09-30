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

  getRankings: async (packageId) => {
    return await axiosClient.get(`/evaluations/packages/${packageId}/rankings`);
  },

  scoreSubmission: async (submissionId, data) => {
    return await axiosClient.post(`/evaluations/submissions/${submissionId}/scores`, data);
  },

  getScoresBySubmission: async (submissionId) => {
    return await axiosClient.get(`/evaluations/submissions/${submissionId}/scores`);
  },

  finalizeEvaluation: async (packageId, selectedSubmissionId) => {
    return await axiosClient.post(`/evaluations/packages/${packageId}/finalize?selectedSubmissionId=${selectedSubmissionId}`);
  },

  getAwardedBid: async (packageId) => {
    return await axiosClient.get(`/evaluations/packages/${packageId}/awarded-bid`);
  },
};

export default evaluationApi;
