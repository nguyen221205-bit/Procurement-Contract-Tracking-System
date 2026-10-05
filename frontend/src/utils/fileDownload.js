import axios from 'axios';

/**
 * Tải tệp an toàn có xác thực Bearer Token qua API
 * @param {string} endpointUrl Đường dẫn API (ví dụ: '/api/bid-packages/documents/1/download')
 * @param {string} defaultFileName Tên tệp lưu về máy
 */
export const downloadSecureFile = async (endpointUrl, defaultFileName) => {
  try {
    const token = sessionStorage.getItem('token') || localStorage.getItem('token');
    const fullUrl = endpointUrl.startsWith('http')
      ? endpointUrl
      : `http://localhost:5225${endpointUrl.startsWith('/') ? '' : '/'}${endpointUrl}`;

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
    // Nếu backend trả về blob chứa json error message
    if (error.response && error.response.data instanceof Blob) {
      const text = await error.response.data.text();
      try {
        const json = JSON.parse(text);
        alert(json.message || 'Không thể tải tệp.');
        return;
      } catch {
        // ignore
      }
    }
    alert(error.message || 'Không thể tải tệp tin từ hệ thống.');
  }
};
