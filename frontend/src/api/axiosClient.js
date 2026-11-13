import axios from 'axios';
import { encodeRequestEnums } from './enumContract';

const API_BASE_URL = import.meta.env?.VITE_API_BASE_URL || 'http://localhost:5225/api';

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
    const token = sessionStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    // Let browser set multipart/form-data boundary automatically when sending FormData
    if (config.data instanceof FormData) {
      delete config.headers['Content-Type'];
    }
    return encodeRequestEnums(config);
  },
  (error) => Promise.reject(error)
);

// Response interceptor: Extract data and handle errors
axiosClient.interceptors.response.use(
  (response) => {
    return response.data;
  },
  async (error) => {
    const originalRequest = error.config;
    const status = error.response?.status;
    let data = error.response?.data;

    // T04: Nếu responseType là blob nhưng server trả về JSON lỗi (VD: 400, 401, 403, 404), giải mã text và parse JSON
    if (data instanceof Blob && (data.type?.includes('application/json') || data.type === '')) {
      try {
        const text = await data.text();
        data = JSON.parse(text);
      } catch {
        // Giữ nguyên data nếu không parse được
      }
    }

    // Handle 401 Unauthorized
    if (status === 401 && !originalRequest._retry) {
      sessionStorage.removeItem('token');
      sessionStorage.removeItem('user');
      sessionStorage.removeItem('refreshToken');
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      localStorage.removeItem('refreshToken');
      
      if (window.location.pathname !== '/login') {
        window.location.href = '/login';
      }
    }

    let message = data?.message;
    if (!message && data?.errors) {
      if (Array.isArray(data.errors)) {
        message = data.errors.join(', ');
      } else if (typeof data.errors === 'object') {
        message = Object.values(data.errors).flat().join(', ');
      } else if (typeof data.errors === 'string') {
        message = data.errors;
      }
    }
    message = message || error.message || 'Đã xảy ra lỗi kết nối';
    const err = new Error(message);
    err.status = status;
    err.response = error.response;
    return Promise.reject(err);
  }
);

export default axiosClient;
