import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { authApi } from '../api/authApi';
import FileUpload from '../components/common/FileUpload';
import toast from 'react-hot-toast';
import {
  ShieldCheck,
  Lock,
  Mail,
  User,
  Phone,
  Building2,
  FileSpreadsheet,
  MapPin,
  Eye,
  EyeOff,
  ArrowRight,
  Briefcase,
  FileText,
  BadgeCheck
} from 'lucide-react';

export const RegisterPage = () => {
  const navigate = useNavigate();
  const [roleType, setRoleType] = useState('contractor'); // 'contractor' | 'procuring_entity'
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);

  // Form data cho Nhà thầu
  const [contractorData, setContractorData] = useState({
    fullName: '',
    email: '',
    password: '',
    phone: '',
    companyName: '',
    taxCode: '',
    address: '',
  });
  const [licenseFile, setLicenseFile] = useState(null);

  // Form data cho Bên mời thầu
  const [procuringData, setProcuringData] = useState({
    fullName: '',
    email: '',
    password: '',
    phone: '',
    organizationName: '',
    organizationType: 'Ban Quản lý Dự án',
    taxCode: '',
    budgetCode: '',
    address: '',
    representativeName: '',
    representativeTitle: 'Giám đốc Ban QLDA',
    representativePhone: '',
  });
  const [establishmentFile, setEstablishmentFile] = useState(null);
  const [appointmentFile, setAppointmentFile] = useState(null);

  const handleContractorChange = (e) => {
    const { name, value } = e.target;
    setContractorData((prev) => ({ ...prev, [name]: value }));
  };

  const handleProcuringChange = (e) => {
    const { name, value } = e.target;
    setProcuringData((prev) => ({ ...prev, [name]: value }));
  };

  // Submit Nhà thầu
  const handleContractorSubmit = async (e) => {
    e.preventDefault();

    if (!contractorData.fullName || !contractorData.email || !contractorData.password || !contractorData.companyName || !contractorData.taxCode) {
      toast.error('Vui lòng điền đầy đủ các thông tin bắt buộc');
      return;
    }

    if (!licenseFile) {
      toast.error('Vui lòng tải lên Giấy phép kinh doanh');
      return;
    }

    setLoading(true);

    try {
      const data = new FormData();
      data.append('FullName', contractorData.fullName);
      data.append('Email', contractorData.email);
      data.append('Password', contractorData.password);
      if (contractorData.phone) data.append('Phone', contractorData.phone);
      data.append('CompanyName', contractorData.companyName);
      data.append('TaxCode', contractorData.taxCode);
      if (contractorData.address) data.append('Address', contractorData.address);
      data.append('BusinessLicenseFile', licenseFile);

      const response = await authApi.registerContractor(data);

      if (response && response.success) {
        toast.success('Đăng ký nhà thầu thành công! Vui lòng chờ phê duyệt.');
        navigate('/login');
      } else {
        toast.error(response?.message || 'Đăng ký không thành công');
      }
    } catch (error) {
      toast.error(error.message || 'Lỗi gửi yêu cầu đăng ký');
    } finally {
      setLoading(false);
    }
  };

  // Submit Bên mời thầu
  const handleProcuringSubmit = async (e) => {
    e.preventDefault();

    if (!procuringData.fullName || !procuringData.email || !procuringData.password || !procuringData.organizationName || !procuringData.taxCode || !procuringData.representativeName) {
      toast.error('Vui lòng điền đầy đủ các thông tin bắt buộc');
      return;
    }

    if (!establishmentFile) {
      toast.error('Vui lòng tải lên Quyết định thành lập / Giấy phép hoạt động');
      return;
    }

    setLoading(true);

    try {
      const data = new FormData();
      data.append('FullName', procuringData.fullName);
      data.append('Email', procuringData.email);
      data.append('Password', procuringData.password);
      if (procuringData.phone) data.append('Phone', procuringData.phone);
      data.append('OrganizationName', procuringData.organizationName);
      data.append('OrganizationType', procuringData.organizationType);
      data.append('TaxCode', procuringData.taxCode);
      if (procuringData.budgetCode) data.append('BudgetCode', procuringData.budgetCode);
      if (procuringData.address) data.append('Address', procuringData.address);
      data.append('RepresentativeName', procuringData.representativeName);
      data.append('RepresentativeTitle', procuringData.representativeTitle);
      if (procuringData.representativePhone) data.append('RepresentativePhone', procuringData.representativePhone);
      data.append('EstablishmentDecisionFile', establishmentFile);
      if (appointmentFile) data.append('AppointmentDecisionFile', appointmentFile);

      const response = await authApi.registerProcuringEntity(data);

      if (response && response.success) {
        toast.success(response.message || 'Đăng ký Bên mời thầu thành công! Hồ sơ đang chờ Quản trị viên thẩm định.');
        navigate('/login');
      } else {
        toast.error(response?.message || 'Đăng ký không thành công');
      }
    } catch (error) {
      toast.error(error.message || 'Lỗi gửi yêu cầu đăng ký');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col justify-center items-center py-10 px-4 sm:px-6">
      <div className="max-w-xl w-full space-y-6">
        {/* Tên hệ thống */}
        <div className="text-center space-y-2">
          <div className="inline-flex items-center justify-center w-12 h-12 rounded-xl bg-slate-900 text-sky-400 shadow-sm">
            <ShieldCheck className="w-6 h-6" />
          </div>
          <h1 className="text-base font-bold text-slate-700 tracking-tight">
            Procurement & Contract Tracking System
          </h1>
        </div>

        {/* Khung đăng ký */}
        <div className="bg-white p-7 sm:p-9 rounded-2xl border border-slate-200 shadow-sm space-y-6">
          <div>
            <h2 className="text-2xl font-bold text-slate-900">
              Đăng ký tài khoản
            </h2>
            <p className="text-xs text-slate-500 mt-1">
              Chọn vai trò của đơn vị bạn trên Hệ thống Mua sắm & Đấu thầu
            </p>
          </div>

          {/* Tab chọn loại tài khoản */}
          <div className="grid grid-cols-2 p-1 bg-slate-100 rounded-xl gap-1">
            <button
              type="button"
              onClick={() => setRoleType('contractor')}
              className={`py-2 px-3 text-xs font-semibold rounded-lg transition flex items-center justify-center space-x-2 ${
                roleType === 'contractor'
                  ? 'bg-white text-slate-900 shadow-sm'
                  : 'text-slate-500 hover:text-slate-800'
              }`}
            >
              <Building2 className="w-4 h-4" />
              <span>Nhà thầu tham gia thầu</span>
            </button>
            <button
              type="button"
              onClick={() => setRoleType('procuring_entity')}
              className={`py-2 px-3 text-xs font-semibold rounded-lg transition flex items-center justify-center space-x-2 ${
                roleType === 'procuring_entity'
                  ? 'bg-white text-sky-700 shadow-sm'
                  : 'text-slate-500 hover:text-slate-800'
              }`}
            >
              <Briefcase className="w-4 h-4" />
              <span>Bên mời thầu / Chủ đầu tư</span>
            </button>
          </div>

          {/* ============================================================== */}
          {/* FORM NHÀ THẦU */}
          {/* ============================================================== */}
          {roleType === 'contractor' && (
            <form onSubmit={handleContractorSubmit} className="space-y-5">
              {/* NHÓM 1: TÀI KHOẢN ĐẠI DIỆN */}
              <div className="space-y-3.5">
                <h3 className="text-xs font-bold uppercase tracking-wider text-slate-500 pb-1 border-b border-slate-100">
                  1. Tài khoản đại diện
                </h3>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Họ và tên <span className="text-rose-500">*</span>
                    </label>
                    <div className="relative rounded-lg">
                      <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                        <User className="w-4 h-4" />
                      </div>
                      <input
                        type="text"
                        required
                        name="fullName"
                        value={contractorData.fullName}
                        onChange={handleContractorChange}
                        placeholder="Nguyễn Văn A"
                        className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                      />
                    </div>
                  </div>

                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Email <span className="text-rose-500">*</span>
                    </label>
                    <div className="relative rounded-lg">
                      <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                        <Mail className="w-4 h-4" />
                      </div>
                      <input
                        type="email"
                        required
                        name="email"
                        value={contractorData.email}
                        onChange={handleContractorChange}
                        placeholder="email@company.vn"
                        className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                      />
                    </div>
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Mật khẩu <span className="text-rose-500">*</span>
                    </label>
                    <div className="relative rounded-lg">
                      <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                        <Lock className="w-4 h-4" />
                      </div>
                      <input
                        type={showPassword ? 'text' : 'password'}
                        required
                        name="password"
                        value={contractorData.password}
                        onChange={handleContractorChange}
                        placeholder="••••••••"
                        className="block w-full pl-9 pr-10 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                      />
                      <button
                        type="button"
                        onClick={() => setShowPassword(!showPassword)}
                        className="absolute inset-y-0 right-0 pr-3 flex items-center text-slate-400 hover:text-slate-600 transition"
                      >
                        {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                      </button>
                    </div>
                  </div>

                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Số điện thoại
                    </label>
                    <div className="relative rounded-lg">
                      <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                        <Phone className="w-4 h-4" />
                      </div>
                      <input
                        type="tel"
                        name="phone"
                        value={contractorData.phone}
                        onChange={handleContractorChange}
                        placeholder="0912 345 678"
                        className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                      />
                    </div>
                  </div>
                </div>
              </div>

              {/* NHÓM 2: THÔNG TIN DOANH NGHIỆP */}
              <div className="space-y-3.5 pt-1">
                <h3 className="text-xs font-bold uppercase tracking-wider text-slate-500 pb-1 border-b border-slate-100">
                  2. Thông tin doanh nghiệp
                </h3>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Tên công ty / Doanh nghiệp <span className="text-rose-500">*</span>
                    </label>
                    <div className="relative rounded-lg">
                      <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                        <Building2 className="w-4 h-4" />
                      </div>
                      <input
                        type="text"
                        required
                        name="companyName"
                        value={contractorData.companyName}
                        onChange={handleContractorChange}
                        placeholder="Công ty Cổ phần Xây dựng ABC"
                        className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                      />
                    </div>
                  </div>

                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Mã số thuế (MST) <span className="text-rose-500">*</span>
                    </label>
                    <div className="relative rounded-lg">
                      <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                        <FileSpreadsheet className="w-4 h-4" />
                      </div>
                      <input
                        type="text"
                        required
                        name="taxCode"
                        value={contractorData.taxCode}
                        onChange={handleContractorChange}
                        placeholder="0101234567"
                        className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                      />
                    </div>
                  </div>
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                    Địa chỉ trụ sở
                  </label>
                  <div className="relative rounded-lg">
                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                      <MapPin className="w-4 h-4" />
                    </div>
                    <input
                      type="text"
                      name="address"
                      value={contractorData.address}
                      onChange={handleContractorChange}
                      placeholder="Số 123 Đường Trần Phú, Quận Ba Đình, Hà Nội"
                      className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                    />
                  </div>
                </div>

                {/* TẢI LÊN GIẤY PHÉP KINH DOANH */}
                <FileUpload
                  file={licenseFile}
                  onFileSelect={(file) => setLicenseFile(file)}
                  onFileRemove={() => setLicenseFile(null)}
                  label="Giấy phép kinh doanh (GPKD)"
                  required={true}
                  accept=".pdf,.jpg,.jpeg,.png"
                  maxSizeMB={10}
                  helperText="PDF, JPG, PNG (Tối đa 10MB)"
                />
              </div>

              <div className="flex items-center justify-end text-xs pt-1">
                <Link to="/login" className="font-semibold text-sky-600 hover:text-sky-700 transition">
                  Đã có tài khoản? Đăng nhập →
                </Link>
              </div>

              <button
                type="submit"
                disabled={loading}
                className="w-full py-2.5 px-4 bg-slate-900 hover:bg-slate-800 text-white font-semibold text-sm rounded-lg shadow-sm hover:shadow focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-slate-900 transition flex items-center justify-center space-x-2 disabled:opacity-50"
              >
                {loading ? (
                  <>
                    <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
                    <span>Đang xử lý...</span>
                  </>
                ) : (
                  <>
                    <span>Đăng ký Nhà thầu</span>
                    <ArrowRight className="w-4 h-4" />
                  </>
                )}
              </button>
            </form>
          )}

          {/* ============================================================== */}
          {/* FORM BÊN MỜI THẦU / CHỦ ĐẦU TƯ */}
          {/* ============================================================== */}
          {roleType === 'procuring_entity' && (
            <form onSubmit={handleProcuringSubmit} className="space-y-5">
              {/* NHÓM 1: TÀI KHOẢN ĐẠI DIỆN */}
              <div className="space-y-3.5">
                <h3 className="text-xs font-bold uppercase tracking-wider text-sky-700 pb-1 border-b border-sky-100 flex items-center justify-between">
                  <span>1. Tài khoản đăng nhập & Cán bộ phụ trách</span>
                  <span className="text-[10px] text-slate-400 font-normal">Luật Đấu thầu 22/2023/QH15</span>
                </h3>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Họ tên cán bộ phụ trách <span className="text-rose-500">*</span>
                    </label>
                    <div className="relative rounded-lg">
                      <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                        <User className="w-4 h-4" />
                      </div>
                      <input
                        type="text"
                        required
                        name="fullName"
                        value={procuringData.fullName}
                        onChange={handleProcuringChange}
                        placeholder="Nguyễn Thị B"
                        className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                      />
                    </div>
                  </div>

                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Email công vụ / Đăng nhập <span className="text-rose-500">*</span>
                    </label>
                    <div className="relative rounded-lg">
                      <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                        <Mail className="w-4 h-4" />
                      </div>
                      <input
                        type="email"
                        required
                        name="email"
                        value={procuringData.email}
                        onChange={handleProcuringChange}
                        placeholder="procurement@agency.gov.vn"
                        className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                      />
                    </div>
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Mật khẩu <span className="text-rose-500">*</span>
                    </label>
                    <div className="relative rounded-lg">
                      <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                        <Lock className="w-4 h-4" />
                      </div>
                      <input
                        type={showPassword ? 'text' : 'password'}
                        required
                        name="password"
                        value={procuringData.password}
                        onChange={handleProcuringChange}
                        placeholder="••••••••"
                        className="block w-full pl-9 pr-10 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                      />
                      <button
                        type="button"
                        onClick={() => setShowPassword(!showPassword)}
                        className="absolute inset-y-0 right-0 pr-3 flex items-center text-slate-400 hover:text-slate-600 transition"
                      >
                        {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                      </button>
                    </div>
                  </div>

                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Số điện thoại liên hệ
                    </label>
                    <div className="relative rounded-lg">
                      <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                        <Phone className="w-4 h-4" />
                      </div>
                      <input
                        type="tel"
                        name="phone"
                        value={procuringData.phone}
                        onChange={handleProcuringChange}
                        placeholder="024 3822 5678"
                        className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                      />
                    </div>
                  </div>
                </div>
              </div>

              {/* NHÓM 2: THÔNG TIN PHÁP NHÂN CƠ QUAN / ĐƠN VỊ */}
              <div className="space-y-3.5 pt-1">
                <h3 className="text-xs font-bold uppercase tracking-wider text-sky-700 pb-1 border-b border-sky-100">
                  2. Thông tin Cơ quan / Đơn vị mời thầu
                </h3>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                    Tên đầy đủ của Cơ quan / Chủ đầu tư / Ban QLDA <span className="text-rose-500">*</span>
                  </label>
                  <div className="relative rounded-lg">
                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                      <Building2 className="w-4 h-4" />
                    </div>
                    <input
                      type="text"
                      required
                      name="organizationName"
                      value={procuringData.organizationName}
                      onChange={handleProcuringChange}
                      placeholder="Ban Quản lý Dự án Đầu tư Xây dựng Công trình Giao thông TP"
                      className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                    />
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Loại hình cơ quan <span className="text-rose-500">*</span>
                    </label>
                    <select
                      name="organizationType"
                      value={procuringData.organizationType}
                      onChange={handleProcuringChange}
                      className="block w-full px-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                    >
                      <option value="Ban Quản lý Dự án">Ban Quản lý Dự án (PMU)</option>
                      <option value="Cơ quan hành chính nhà nước">Cơ quan hành chính nhà nước</option>
                      <option value="Đơn vị sự nghiệp công lập">Đơn vị sự nghiệp công lập</option>
                      <option value="Doanh nghiệp nhà nước">Doanh nghiệp nhà nước</option>
                      <option value="Đơn vị mua sắm tập trung">Đơn vị mua sắm tập trung</option>
                      <option value="Doanh nghiệp tư nhân (Chủ đầu tư)">Doanh nghiệp tư nhân (Chủ đầu tư)</option>
                    </select>
                  </div>

                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Mã số thuế (MST) <span className="text-rose-500">*</span>
                    </label>
                    <div className="relative rounded-lg">
                      <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                        <FileSpreadsheet className="w-4 h-4" />
                      </div>
                      <input
                        type="text"
                        required
                        name="taxCode"
                        value={procuringData.taxCode}
                        onChange={handleProcuringChange}
                        placeholder="0100109106"
                        className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                      />
                    </div>
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Mã đơn vị ngân sách (Mã ĐVQHNS)
                    </label>
                    <input
                      type="text"
                      name="budgetCode"
                      value={procuringData.budgetCode}
                      onChange={handleProcuringChange}
                      placeholder="1054321 (tùy chọn)"
                      className="block w-full px-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                    />
                  </div>

                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Địa chỉ trụ sở chính
                    </label>
                    <div className="relative rounded-lg">
                      <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                        <MapPin className="w-4 h-4" />
                      </div>
                      <input
                        type="text"
                        name="address"
                        value={procuringData.address}
                        onChange={handleProcuringChange}
                        placeholder="Số 45 Lê Duẩn, Quận 1, TP.HCM"
                        className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                      />
                    </div>
                  </div>
                </div>
              </div>

              {/* NHÓM 3: NGƯỜI ĐẠI DIỆN PHÁP LUẬT */}
              <div className="space-y-3.5 pt-1">
                <h3 className="text-xs font-bold uppercase tracking-wider text-sky-700 pb-1 border-b border-sky-100">
                  3. Người đại diện pháp luật / Thủ trưởng đơn vị
                </h3>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Họ và tên người đại diện <span className="text-rose-500">*</span>
                    </label>
                    <input
                      type="text"
                      required
                      name="representativeName"
                      value={procuringData.representativeName}
                      onChange={handleProcuringChange}
                      placeholder="Trần Văn Minh"
                      className="block w-full px-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                    />
                  </div>

                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Chức vụ <span className="text-rose-500">*</span>
                    </label>
                    <input
                      type="text"
                      required
                      name="representativeTitle"
                      value={procuringData.representativeTitle}
                      onChange={handleProcuringChange}
                      placeholder="Giám đốc Ban QLDA / Giám đốc Sở"
                      className="block w-full px-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                    SĐT người đại diện (nếu khác SĐT cán bộ)
                  </label>
                  <input
                    type="tel"
                    name="representativePhone"
                    value={procuringData.representativePhone}
                    onChange={handleProcuringChange}
                    placeholder="0903 999 888"
                    className="block w-full px-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                  />
                </div>
              </div>

              {/* NHÓM 4: TÀI LIỆU PHÁP LÝ ĐÍNH KÈM */}
              <div className="space-y-3.5 pt-1">
                <h3 className="text-xs font-bold uppercase tracking-wider text-sky-700 pb-1 border-b border-sky-100 flex items-center justify-between">
                  <span>4. Tài liệu thẩm định pháp nhân</span>
                  <span className="text-[10px] text-amber-600 font-medium">Bắt buộc để Admin duyệt</span>
                </h3>

                <FileUpload
                  file={establishmentFile}
                  onFileSelect={(file) => setEstablishmentFile(file)}
                  onFileRemove={() => setEstablishmentFile(null)}
                  label="Quyết định thành lập / Giấy phép hoạt động (PDF)"
                  required={true}
                  accept=".pdf,.jpg,.jpeg,.png"
                  maxSizeMB={15}
                  helperText="File scan PDF quyết định thành lập cơ quan / Ban QLDA (Tối đa 15MB)"
                />

                <FileUpload
                  file={appointmentFile}
                  onFileSelect={(file) => setAppointmentFile(file)}
                  onFileRemove={() => setAppointmentFile(null)}
                  label="Quyết định bổ nhiệm người đứng đầu / Giấy ủy quyền (Tùy chọn)"
                  required={false}
                  accept=".pdf,.jpg,.jpeg,.png"
                  maxSizeMB={15}
                  helperText="Tệp scan quyết định bổ nhiệm hoặc giấy ủy quyền ký số (nếu có)"
                />
              </div>

              <div className="flex items-center justify-end text-xs pt-1">
                <Link to="/login" className="font-semibold text-sky-600 hover:text-sky-700 transition">
                  Đã có tài khoản? Đăng nhập →
                </Link>
              </div>

              <button
                type="submit"
                disabled={loading}
                className="w-full py-2.5 px-4 bg-sky-700 hover:bg-sky-800 text-white font-semibold text-sm rounded-lg shadow-sm hover:shadow focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-sky-700 transition flex items-center justify-center space-x-2 disabled:opacity-50"
              >
                {loading ? (
                  <>
                    <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
                    <span>Đang xử lý đăng ký...</span>
                  </>
                ) : (
                  <>
                    <span>Đăng ký Bên mời thầu</span>
                    <ArrowRight className="w-4 h-4" />
                  </>
                )}
              </button>
            </form>
          )}
        </div>
      </div>
    </div>
  );
};

export default RegisterPage;
