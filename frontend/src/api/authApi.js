import axiosClient from './axiosClient';

export const authApi = {
  login: async (email, password) => {
    return await axiosClient.post('/auth/login', { email, password });
  },

  registerContractor: async (data) => {
    return await axiosClient.post('/auth/register-contractor', data);
  },

  registerProcuringEntity: async (data) => {
    return await axiosClient.post('/auth/register-procuring-entity', data);
  },

  refreshToken: async (token, refreshToken) => {
    return await axiosClient.post('/auth/refresh-token', { token, refreshToken });
  },

  lookupTaxCode: async (taxCode) => {
    return await axiosClient.get(`/auth/tax-lookup/${taxCode}`);
  },
};
