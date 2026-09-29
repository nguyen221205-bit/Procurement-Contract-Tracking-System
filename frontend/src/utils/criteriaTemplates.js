// 1. Tiêu chí mẫu lẻ: Chỉ gợi ý tên & mô tả, KHÔNG áp đặt điểm/trọng số
export const SINGLE_CRITERIA_SUGGESTIONS = [
  'Giải pháp kỹ thuật',
  'Năng lực & kinh nghiệm',
  'Nhân sự & chuyên gia',
  'Tiến độ cam kết',
  'Bảo mật & chất lượng',
  'Chi phí & Giá dự thầu',
  'Bảo hành & Hỗ trợ kỹ thuật',
];

// 2. Bộ tiêu chí mẫu trọn gói (Full Template Sets) - Tổng trọng số luôn đạt đúng 100%
export const CRITERIA_PACKAGE_TEMPLATES = [
  {
    id: 'service',
    name: 'Dịch vụ & CNTT',
    description: 'Phần mềm, chuyển đổi số, vận hành hệ thống.',
    items: [
      { name: 'Giải pháp kỹ thuật', maxScore: 100, weight: 40 },
      { name: 'Năng lực & kinh nghiệm', maxScore: 100, weight: 20 },
      { name: 'Nhân sự', maxScore: 100, weight: 15 },
      { name: 'Tiến độ', maxScore: 100, weight: 10 },
      { name: 'Bảo mật & chất lượng', maxScore: 100, weight: 15 },
    ],
  },
  {
    id: 'goods',
    name: 'Mua sắm thiết bị',
    description: 'Phần cứng, máy chủ, thiết bị viễn thông.',
    items: [
      { name: 'Đáp ứng thông số kỹ thuật', maxScore: 100, weight: 45 },
      { name: 'Chi phí & Giá dự thầu', maxScore: 100, weight: 30 },
      { name: 'Năng lực nhà cung cấp', maxScore: 100, weight: 15 },
      { name: 'Bảo hành & Hỗ trợ kỹ thuật', maxScore: 100, weight: 10 },
    ],
  },
  {
    id: 'construction',
    name: 'Xây lắp & Thi công',
    description: 'Hạ tầng phòng máy, cáp mạng, công trình kỹ thuật.',
    items: [
      { name: 'Biện pháp thi công & Kỹ thuật', maxScore: 100, weight: 40 },
      { name: 'Năng lực thiết bị & Máy móc', maxScore: 100, weight: 25 },
      { name: 'Tiến độ & An toàn lao động', maxScore: 100, weight: 20 },
      { name: 'Chi phí & Giá dự thầu', maxScore: 100, weight: 15 },
    ],
  },
];
