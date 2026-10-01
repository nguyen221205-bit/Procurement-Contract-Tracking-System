// Role definitions matching Backend .NET 8 RBAC
export const ROLES = {
  ADMIN: 'Admin',
  PROCUREMENT: 'Procurement',
  EVALUATOR: 'Evaluator',
  CONTRACTOR: 'Contractor',
};

export const ROLE_CONFIG = {
  [ROLES.ADMIN]: {
    label: 'Quản trị viên',
    badgeColor: 'bg-rose-50 text-rose-700 border-rose-200',
    description: 'Toàn quyền cấu hình, quản trị người dùng & phê duyệt hệ thống',
  },
  [ROLES.PROCUREMENT]: {
    label: 'Bên mời thầu',
    badgeColor: 'bg-indigo-50 text-indigo-700 border-indigo-200',
    description: 'Lập gói thầu, cấu hình tiêu chí & nghiệm thu mốc giải ngân',
  },
  [ROLES.EVALUATOR]: {
    label: 'Giám khảo',
    badgeColor: 'bg-amber-50 text-amber-700 border-amber-200',
    description: 'Chấm điểm hồ sơ dự thầu độc lập theo các tiêu chí',
  },
  [ROLES.CONTRACTOR]: {
    label: 'Nhà thầu',
    badgeColor: 'bg-emerald-50 text-emerald-700 border-emerald-200',
    description: 'Nộp hồ sơ dự thầu, ký kết hợp đồng & cập nhật tiến độ mốc',
  },
};


// Bid Package Lifecycle Status khớp 100% Enum Backend (BidPackageStatus.cs)
export const PACKAGE_STATUS = {
  OPEN: 'Open',
  CLOSED: 'Closed',
  EVALUATING: 'Evaluating',
  AWARDED: 'Awarded',
  CONTRACTED: 'Contracted',
};

export const PACKAGE_STATUS_CONFIG = {
  Open: { label: 'Đang mở thầu', badgeColor: 'bg-emerald-50 text-emerald-700 border-emerald-200' },
  0: { label: 'Đang mở thầu', badgeColor: 'bg-emerald-50 text-emerald-700 border-emerald-200' },
  Evaluating: { label: 'Đang chấm điểm', badgeColor: 'bg-amber-50 text-amber-700 border-amber-200' },
  2: { label: 'Đang chấm điểm', badgeColor: 'bg-amber-50 text-amber-700 border-amber-200' },
  Awarded: { label: 'Đã trao thầu', badgeColor: 'bg-emerald-50 text-emerald-700 border-emerald-200' },
  Contracted: { label: 'Đã ký hợp đồng', badgeColor: 'bg-blue-50 text-blue-700 border-blue-200' },
  3: { label: 'Đã ký hợp đồng', badgeColor: 'bg-blue-50 text-blue-700 border-blue-200' },
  Closed: { label: 'Đã đóng thầu', badgeColor: 'bg-slate-100 text-slate-700 border-slate-200' },
  1: { label: 'Đã đóng thầu', badgeColor: 'bg-slate-100 text-slate-700 border-slate-200' },
};

// Bid Package Types khớp Enum Backend (BidPackageType.cs)
export const PACKAGE_TYPES = {
  Goods: { label: 'Hàng hóa', color: 'bg-sky-50 text-sky-700 border-sky-200' },
  0: { label: 'Hàng hóa', color: 'bg-sky-50 text-sky-700 border-sky-200' },
  Construction: { label: 'Xây lắp', color: 'bg-purple-50 text-purple-700 border-purple-200' },
  1: { label: 'Xây lắp', color: 'bg-purple-50 text-purple-700 border-purple-200' },
  Service: { label: 'Dịch vụ', color: 'bg-indigo-50 text-indigo-700 border-indigo-200' },
  2: { label: 'Dịch vụ', color: 'bg-indigo-50 text-indigo-700 border-indigo-200' },
};

// Contract Lifecycle Status khớp Enum Backend (ContractStatus.cs)
export const CONTRACT_STATUS = {
  DRAFT: 'Draft',
  ACTIVE: 'Active',
  COMPLETED: 'Completed',
  TERMINATED: 'Terminated',
};

export const CONTRACT_STATUS_CONFIG = {
  Draft: { label: 'Bản thảo / Nháp', badgeColor: 'bg-slate-100 text-slate-700 border-slate-300' },
  0: { label: 'Bản thảo / Nháp', badgeColor: 'bg-slate-100 text-slate-700 border-slate-300' },
  Active: { label: 'Đang thực hiện', badgeColor: 'bg-emerald-50 text-emerald-700 border-emerald-200' },
  1: { label: 'Đang thực hiện', badgeColor: 'bg-emerald-50 text-emerald-700 border-emerald-200' },
  Completed: { label: 'Đã hoàn thành', badgeColor: 'bg-blue-50 text-blue-700 border-blue-200' },
  2: { label: 'Đã hoàn thành', badgeColor: 'bg-blue-50 text-blue-700 border-blue-200' },
  Terminated: { label: 'Đã chấm dứt', badgeColor: 'bg-rose-50 text-rose-700 border-rose-200' },
  3: { label: 'Đã chấm dứt', badgeColor: 'bg-rose-50 text-rose-700 border-rose-200' },
};

// Milestone Status khớp Enum Backend (MilestoneStatus.cs)
export const MILESTONE_STATUS = {
  PENDING: 'Pending',
  IN_PROGRESS: 'InProgress',
  COMPLETED: 'Completed',
  OVERDUE: 'Overdue',
};

export const MILESTONE_STATUS_CONFIG = {
  Pending: { label: 'Chờ nghiệm thu', badgeColor: 'bg-slate-100 text-slate-600 border-slate-200' },
  0: { label: 'Chờ nghiệm thu', badgeColor: 'bg-slate-100 text-slate-600 border-slate-200' },
  InProgress: { label: 'Đang thực hiện', badgeColor: 'bg-amber-50 text-amber-700 border-amber-200' },
  1: { label: 'Đang thực hiện', badgeColor: 'bg-amber-50 text-amber-700 border-amber-200' },
  Completed: { label: 'Đã nghiệm thu', badgeColor: 'bg-emerald-50 text-emerald-700 border-emerald-200' },
  2: { label: 'Đã nghiệm thu', badgeColor: 'bg-emerald-50 text-emerald-700 border-emerald-200' },
  Overdue: { label: 'Quá hạn', badgeColor: 'bg-rose-50 text-rose-700 border-rose-200' },
  3: { label: 'Quá hạn', badgeColor: 'bg-rose-50 text-rose-700 border-rose-200' },
};

