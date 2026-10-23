// Định dạng tiền tệ VNĐ
export const formatVND = (amount) => {
  if (amount === undefined || amount === null || isNaN(amount)) return '0 đ';
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(amount);
};

// Hàm chuyển đổi chuỗi ngày giờ an toàn sang Date đối tượng (múi giờ máy khách GMT+7)
export const parseDate = (dateString) => {
  if (!dateString) return null;
  if (dateString instanceof Date) {
    return isNaN(dateString.getTime()) ? null : dateString;
  }

  let str = String(dateString).trim();
  // Nếu là ISO string có 'T' và chưa có chỉ định timezone (không kết thúc bằng Z và không có offset +HH:mm/-HH:mm)
  // Backend lưu trữ UTC nên cần bổ sung hậu tố 'Z' để Date constructor chuyển đổi chính xác sang giờ địa phương
  if (str.includes('T') && !str.endsWith('Z') && !/[+-]\d{2}(:\d{2})?$/.test(str)) {
    str += 'Z';
  }

  const d = new Date(str);
  return isNaN(d.getTime()) ? null : d;
};

// Định dạng ngày tháng VN (dd/mm/yyyy)
export const formatDate = (dateString) => {
  const d = parseDate(dateString);
  if (!d) return '-';
  const day = String(d.getDate()).padStart(2, '0');
  const month = String(d.getMonth() + 1).padStart(2, '0');
  const year = d.getFullYear();
  return `${day}/${month}/${year}`;
};

// Định dạng ngày giờ VN (HH:mm - dd/MM/yyyy)
export const formatDateTime = (dateString) => {
  const d = parseDate(dateString);
  if (!d) return '-';
  const day = String(d.getDate()).padStart(2, '0');
  const month = String(d.getMonth() + 1).padStart(2, '0');
  const year = d.getFullYear();
  const hours = String(d.getHours()).padStart(2, '0');
  const minutes = String(d.getMinutes()).padStart(2, '0');
  return `${hours}:${minutes} - ${day}/${month}/${year}`;
};

// Định dạng tỷ lệ phần trăm
export const formatPercent = (val) => {
  if (val === undefined || val === null || isNaN(val)) return '0%';
  return `${Number(val).toFixed(1)}%`;
};

// Alias formatCurrency -> formatVND
export const formatCurrency = formatVND;

// Định dạng thời gian tương đối (vd: Vừa xong, 5 phút trước, 2 giờ trước)
export const formatRelativeTime = (dateString) => {
  const d = parseDate(dateString);
  if (!d) return '-';

  const now = new Date();
  const diffSec = Math.floor((now.getTime() - d.getTime()) / 1000);

  // Nếu chênh lệch âm nhẹ do lệch mili-giây giữa server và client
  if (diffSec < 45) return 'Vừa xong';
  const diffMin = Math.floor(diffSec / 60);
  if (diffMin < 60) return `${diffMin} phút trước`;
  const diffHour = Math.floor(diffMin / 60);
  if (diffHour < 24) return `${diffHour} giờ trước`;
  const diffDay = Math.floor(diffHour / 24);
  if (diffDay < 7) return `${diffDay} ngày trước`;
  return formatDate(dateString);
};

// Định dạng số chuẩn
export const formatNumber = (num, decimals = 2) => {
  if (num === undefined || num === null || isNaN(num)) return '0';
  return Number(num).toLocaleString('vi-VN', { maximumFractionDigits: decimals });
};
