// Role definitions matching Backend .NET 8 RBAC
export const ROLES = {
  ADMIN: 'Admin',
  PROCUREMENT: 'Procurement',
  EVALUATOR: 'Evaluator',
  CONTRACTOR: 'Contractor',
};

// Seed accounts for Quick 1-Click Demo
export const DEMO_ACCOUNTS = [
  {
    role: ROLES.ADMIN,
    name: 'Quản trị viên',
    email: 'admin@procurement.com',
    password: 'Admin@123',
    badgeColor: 'bg-rose-100 text-rose-700 border-rose-200',
    icon: 'Crown',
    desc: 'Xem báo cáo, duyệt trao thầu',
  },
  {
    role: ROLES.PROCUREMENT,
    name: 'Bên mời thầu',
    email: 'procurement@procurement.com',
    password: 'Admin@123',
    badgeColor: 'bg-indigo-100 text-indigo-700 border-indigo-200',
    icon: 'Briefcase',
    desc: 'Tạo gói thầu, duyệt nghiệm thu',
  },
  {
    role: ROLES.EVALUATOR,
    name: 'Giám khảo',
    email: 'evaluator@procurement.com',
    password: 'Admin@123',
    badgeColor: 'bg-amber-100 text-amber-700 border-amber-200',
    icon: 'Scale',
    desc: 'Chấm điểm hồ sơ thầu',
  },
  {
    role: ROLES.CONTRACTOR,
    name: 'Nhà thầu 1',
    email: 'contractor1@test.com',
    password: 'Admin@123',
    badgeColor: 'bg-emerald-100 text-emerald-700 border-emerald-200',
    icon: 'Building2',
    desc: 'Nộp thầu, cập nhật tiến độ',
  },
  {
    role: ROLES.CONTRACTOR,
    name: 'Nhà thầu 2',
    email: 'contractor2@test.com',
    password: 'Admin@123',
    badgeColor: 'bg-teal-100 text-teal-700 border-teal-200',
    icon: 'Building',
    desc: 'Nộp thầu cạnh tranh',
  },
];

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

