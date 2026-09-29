import axios from 'axios';

const API_BASE_URL = 'http://localhost:5225/api';

const axiosClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30000,
});

// Request interceptor: Attach JWT token
axiosClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    // Let browser set multipart/form-data boundary automatically when sending FormData
    if (config.data instanceof FormData) {
      delete config.headers['Content-Type'];
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor: Extract data and handle errors
axiosClient.interceptors.response.use(
  (response) => {
    return response.data;
  },
  (error) => {
    const originalRequest = error.config;
    const status = error.response?.status;
    const data = error.response?.data;

    // Handle 401 Unauthorized
    if (status === 401 && !originalRequest._retry) {
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      localStorage.removeItem('refreshToken');
      
      if (window.location.pathname !== '/login') {
        window.location.href = '/login';
      }
    }

    const message = data?.message || (data?.errors && data.errors.join(', ')) || error.message || 'Đã xảy ra lỗi kết nối';
    return Promise.reject(new Error(message));
  }
);

export default axiosClient;
