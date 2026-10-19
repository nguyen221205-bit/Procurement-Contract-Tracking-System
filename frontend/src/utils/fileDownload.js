import axios from 'axios';
import toast from 'react-hot-toast';

/**
 * Lấy Base URL của hệ thống (bỏ hậu tố /api nếu có để ghép nối đúng với endpoint /api/...)
 */
const getBaseHostUrl = () => {
  const envUrl = import.meta.env?.VITE_API_BASE_URL;
  if (envUrl) {
    return envUrl.replace(/\/api\/?$/, '');
  }
  if (axios.defaults.baseURL) {
    return axios.defaults.baseURL.replace(/\/api\/?$/, '');
  }
  return 'http://localhost:5225';
};

/**
 * Xử lý trích xuất thông điệp lỗi từ response (kể cả khi responseType là blob)
 */
const extractErrorMessage = async (error, fallbackMessage) => {
  if (error.response && error.response.data instanceof Blob) {
    try {
      const text = await error.response.data.text();
      const json = JSON.parse(text);
      return json.message || (json.errors && json.errors.join(', ')) || fallbackMessage;
    } catch {
      // Bỏ qua lỗi parse JSON nếu dữ liệu trả về là nhị phân thực sự
    }
  }
  return error.response?.data?.message || error.message || fallbackMessage;
};

/**
 * Tải tệp an toàn có xác thực Bearer Token qua API
 * @param {string} endpointUrl Đường dẫn API (ví dụ: '/api/bid-packages/documents/1/download')
 * @param {string} defaultFileName Tên tệp lưu về máy
 */
export const downloadSecureFile = async (endpointUrl, defaultFileName) => {
  try {
    const token = sessionStorage.getItem('token') || localStorage.getItem('token');
    const baseHost = getBaseHostUrl();
    const fullUrl = endpointUrl.startsWith('http')
      ? endpointUrl
      : `${baseHost}${endpointUrl.startsWith('/') ? '' : '/'}${endpointUrl}`;

    const response = await axios.get(fullUrl, {
      responseType: 'blob',
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    });

    const blob = new Blob([response.data]);
    const downloadUrl = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = downloadUrl;
    link.download = defaultFileName || 'downloaded-file';
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(downloadUrl);
  } catch (error) {
    console.error('Lỗi khi tải tệp:', error);
    const message = await extractErrorMessage(error, 'Không thể tải tệp tin từ hệ thống.');
    toast.error(message);
  }
};

/**
 * Xem trước tệp an toàn có xác thực Bearer Token trong tab mới
 * Tránh bị chặn bởi Popup Blocker bằng cách mở sẵn tab trắng trong synchronous user gesture context
 * @param {string} endpointUrl Đường dẫn API (ví dụ: '/api/bid-packages/documents/1/download')
 * @param {string} fileName Tên tệp
 */
export const previewSecureFile = async (endpointUrl, fileName) => {
  // Mở trước tab trắng trong synchronous click gesture để bypass popup blocker
  const newTab = window.open('about:blank', '_blank');

  try {
    const token = sessionStorage.getItem('token') || localStorage.getItem('token');
    const baseHost = getBaseHostUrl();
    const fullUrl = endpointUrl.startsWith('http')
      ? endpointUrl
      : `${baseHost}${endpointUrl.startsWith('/') ? '' : '/'}${endpointUrl}`;

    const response = await axios.get(fullUrl, {
      responseType: 'blob',
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    });

    // Xác định MIME type tương ứng theo đuôi tệp hoặc header response
    const ext = fileName?.split('.').pop()?.toLowerCase();
    let mimeType = response.headers['content-type'];
    if (ext === 'pdf') mimeType = 'application/pdf';
    else if (ext === 'jpg' || ext === 'jpeg') mimeType = 'image/jpeg';
    else if (ext === 'png') mimeType = 'image/png';
    else if (ext === 'txt') mimeType = 'text/plain';

    const blob = mimeType ? new Blob([response.data], { type: mimeType }) : new Blob([response.data]);
    const fileUrl = window.URL.createObjectURL(blob);

    if (newTab) {
      newTab.location.href = fileUrl;
    } else {
      window.location.href = fileUrl;
    }

    // Thu hồi Object URL sau 60s để tránh tràn bộ nhớ
    setTimeout(() => {
      window.URL.revokeObjectURL(fileUrl);
    }, 60000);
  } catch (error) {
    // Đóng tab trắng nếu có lỗi xảy ra
    if (newTab) {
      newTab.close();
    }
    console.error('Lỗi khi mở xem trước tệp:', error);
    const message = await extractErrorMessage(error, 'Không thể mở xem trước tệp tin.');
    toast.error(message);
  }
};

export default {
  downloadSecureFile,
  previewSecureFile,
};
